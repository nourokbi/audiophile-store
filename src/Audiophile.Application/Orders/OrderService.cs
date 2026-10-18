using System.Text.Json;
using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Application.Payments;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Audiophile.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Orders;

// Checkout and everything that happens to an order's payment afterwards.
//
// HOW AN ONLINE PAYMENT FLOWS (follow the "FLOW n" comments across the files):
//   FLOW 1  OrdersController.Place                  POST /orders arrives
//   FLOW 2  OrderService.PlaceOrderAsync            cart → order, stock reserved, saved as Pending
//   FLOW 3  OrderService.BuildCheckoutRequest       our order translated into a gateway request
//   FLOW 4  FawaterakGateway.CreateCheckoutAsync    POST invoiceInitPay → hosted payment URL
//   FLOW 5  (the customer pays on Fawaterak's page; none of our code runs)
//   FLOW 6  FawaterakController.Webhook             Fawaterak calls us: "invoice paid"
//   FLOW 7  FawaterakGateway.ParseWebhookAsync      check the signature, ask Fawaterak to confirm
//   FLOW 8  PaymentResultApplier.ApplyAsync         payment Succeeded, order Confirmed
//   FLOW 9  FawaterakController.Return              the customer's browser comes back
// Safety nets when a webhook never arrives: SyncWithGatewayAsync (an admin asks Fawaterak)
// and ExpireStalePaymentsAsync (PaymentExpiryWorker, every 5 minutes).
public sealed class OrderService(
    IAppDbContext db,
    IFawaterakGateway fawaterak,
    PaymentResultApplier applier,
    ILogger<OrderService> logger)
{
  public const string Currency = "EGP";

  // How long a customer has to finish paying on Fawaterak's page before the order is
  // cancelled and its stock released (PaymentExpiryWorker).
  public static readonly TimeSpan PaymentWindow = TimeSpan.FromMinutes(30);

  // FLOW 2: called by OrdersController.Place (FLOW 1).
  public async Task<PlaceOrderResult> PlaceOrderAsync(
      Guid userId, PlaceOrderRequest request, string idempotencyKey, CancellationToken ct)
  {
    // 2a. Same Idempotency-Key as before → the order already exists. Return it instead of
    //     charging twice (a double click, or the client retrying after a timeout).
    var existing = await LoadOrders()
        .FirstOrDefaultAsync(o => o.UserId == userId && o.IdempotencyKey == idempotencyKey, ct);
    if (existing is not null)
      return new PlaceOrderResult(OrderDto.From(existing), Created: false);

    // 2b. Load the user's address; someone else's, or a deleted one, is "not found"
    var address = await db.Addresses
        .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId && a.IsActive, ct)
        ?? throw new NotFoundException($"Address {request.AddressId} was not found.");

    // 2c. Load the cart with its items
    var cart = await db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == userId, ct);
    if (cart is null || cart.IsEmpty)
      throw new ConflictException("Your cart is empty.");

    // 2d. Load every product in the cart in one query (not one query per item)
    var productIds = cart.CartItems.Select(ci => ci.ProductId).ToList();
    var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

    // 2e. Convert cart items to order lines: price re-read from the product, stock decreased
    var lines = new List<OrderLine>();
    foreach (var item in cart.CartItems)
    {
      if (!products.TryGetValue(item.ProductId, out var product) || !product.IsActive)
        throw new ConflictException($"A product in your cart is no longer sold: {item.ProductId}.");

      product.DecreaseStock(item.Quantity);          // throws → 409 when not enough stock
      lines.Add(OrderLine.FromProduct(product, item.Quantity));
    }

    // 2f. Create the order; the address is copied, so editing it later doesn't change this order
    var subTotal = lines.Sum(l => l.LineTotal);
    var order = Order.Place(userId, request.PaymentMethod, address.ToShippingDetails(), lines,
        ShippingPolicy.FeeFor(subTotal), idempotencyKey);

    // 2g. Create the payment for the order (status Pending, amount = order total)
    var payment = order.StartPayment();
    db.Orders.Add(order);

    // 2h. Cash on delivery needs no gateway: confirm, empty the cart, save. Stock, order and
    //     cart are written by one SaveChanges, so they change together or not at all.
    if (!payment.UsesGateway)
    {
      order.Confirm();
      cart.Clear();
      await db.SaveChangesAsync(ct);
      return new PlaceOrderResult(OrderDto.From(order), Created: true);
    }

    // 2i. Card: save the order (still Pending) BEFORE calling Fawaterak.
    //     The webhook (FLOW 6) can arrive within seconds and must find the order.
    await db.SaveChangesAsync(ct);

    try
    {
      // 2j. FLOW 3 + FLOW 4: ask Fawaterak for an invoice and its hosted payment page.
      var email = await db.Users.Where(u => u.Id == userId).Select(u => u.Email).FirstAsync(ct);
      var session = await fawaterak.CreateCheckoutAsync(BuildCheckoutRequest(order, payment, email!), ct);

      // 2k. Remember Fawaterak's invoice id (needed by FLOW 7 and the safety nets) and the
      //     payment URL the client sends the customer to. The cart is emptied only now,
      //     so a failed gateway call leaves the customer's cart as it was.
      payment.LinkToProvider(
          IFawaterakGateway.Provider, session.ProviderReference, session.ProviderOrderId, session.CheckoutUrl);
      cart.Clear();
      await db.SaveChangesAsync(ct);
    }
    catch (Exception ex) when (ex is GatewayException or GatewayTimeoutException)
    {
      // The customer never received a payment link, so nobody can pay this order.
      // Cancel it, give the stock back, and report the failure (502 / 504).
      logger.LogWarning(ex, "Could not open a checkout for order {OrderId}", order.Id);
      payment.MarkFailed();
      OrderStock.CancelAndRelease(order);
      await db.SaveChangesAsync(CancellationToken.None);
      throw;
    }

    // 2l. Back to the controller → 201 with payment.checkoutUrl. FLOW 5 starts in the browser.
    return new PlaceOrderResult(OrderDto.From(order), Created: true);
  }

  public async Task<OrderDto> GetOrderAsync(Guid userId, Guid orderId, CancellationToken ct)
  {
    var order = await LoadOrders().FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
        ?? throw new NotFoundException($"Order {orderId} was not found.");
    return OrderDto.From(order);
  }

  // Everything Fawaterak told us about this order's payment, oldest first: the
  // audit trail support reads when a customer says "I paid but…". Admin only.
  public async Task<IReadOnlyList<PaymentEventDto>> GetPaymentEventsAsync(Guid orderId, CancellationToken ct)
  {
    var paymentId = await db.Payments.Where(p => p.OrderId == orderId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException($"Order {orderId} has no payment.");

    var events = await db.PaymentEvents.AsNoTracking().Where(e => e.PaymentId == paymentId).ToListAsync(ct);
    return events
        .OrderBy(e => e.ReceivedAt)
        .Select(e => new PaymentEventDto(e.ReceivedAt, e.Provider, e.EventType, e.ExternalId, e.RawPayload))
        .ToList();
  }

  public async Task<IReadOnlyList<OrderDto>> ListOrdersAsync(Guid userId, CancellationToken ct)
  {
    var orders = await LoadOrders().Where(o => o.UserId == userId).ToListAsync(ct);
    return orders.OrderByDescending(o => o.CreatedAt).Select(OrderDto.From).ToList();
  }

  // SAFETY NET 1: asks Fawaterak for the invoice's real status and applies it, through the
  // same PaymentResultApplier as the webhook (FLOW 8). For when a webhook was missed
  // (e.g. no ngrok running locally), or before support changes anything by hand. Admin only.
  public async Task<OrderDto> SyncWithGatewayAsync(Guid orderId, CancellationToken ct)
  {
    var order = await LoadGatewayOrderAsync(orderId, ct);
    var payment = order.Payment!;

    if (payment.ProviderOrderId is null)
      throw new ConflictException("This payment never reached Fawaterak.");

    var invoice = await fawaterak.GetInvoiceAsync(payment.ProviderOrderId, ct);
    await applier.ApplyAsync(IFawaterakGateway.Provider, "inquiry", invoice, JsonSerializer.Serialize(invoice), ct);

    return await GetAnyOrderAsync(orderId, ct);
  }

  // SAFETY NET 2: run by PaymentExpiryWorker every 5 minutes. Closes gateway payments nobody
  // finished in time and releases their stock. Asks Fawaterak first, so a payment whose
  // webhook was lost is confirmed, not cancelled.
  public async Task<int> ExpireStalePaymentsAsync(CancellationToken ct)
  {
    var now = DateTime.UtcNow;
    var candidates = await db.Payments
        .Where(p => p.Status == PaymentStatus.Pending && p.PaymentMethod != PaymentMethod.CashOnDelivery)
        .Select(p => new { p.OrderId, p.CreatedAt })
        .ToListAsync(ct);

    var expired = 0;
    foreach (var c in candidates.Where(c => now - c.CreatedAt > PaymentWindow))
    {
      try
      {
        var synced = await SyncWithGatewayAsync(c.OrderId, ct);
        if (synced.Payment?.Status != PaymentStatus.Pending)
          continue;                          // the gateway knew better; nothing to expire
      }
      catch (Exception ex) when (ex is GatewayException or GatewayTimeoutException)
      {
        logger.LogWarning(ex, "Could not check order {OrderId} with the gateway; will retry later", c.OrderId);
        continue;                              // never cancel on an unknown outcome
      }
      catch (ConflictException)
      {
        // Never linked to the gateway; nothing to ask. Fall through and expire.
      }

      var order = await LoadOrders().FirstAsync(o => o.Id == c.OrderId, ct);
      order.Payment!.MarkFailed();
      OrderStock.CancelAndRelease(order);
      await db.SaveChangesAsync(ct);
      expired++;
      logger.LogInformation("Order {OrderId} expired unpaid; stock released", c.OrderId);
    }

    return expired;
  }

  private IQueryable<Order> LoadOrders() => db.Orders
      .Include(o => o.Payment)
      .Include(o => o.OrderItems).ThenInclude(i => i.Product);

  private async Task<OrderDto> GetAnyOrderAsync(Guid orderId, CancellationToken ct) =>
      OrderDto.From(await LoadOrders().AsNoTracking().FirstAsync(o => o.Id == orderId, ct));

  private async Task<Order> LoadGatewayOrderAsync(Guid orderId, CancellationToken ct)
  {
    var order = await LoadOrders().FirstOrDefaultAsync(o => o.Id == orderId, ct)
        ?? throw new NotFoundException($"Order {orderId} was not found.");

    if (order.Payment is null || !order.Payment.UsesGateway)
      throw new ConflictException("This order was not paid through a payment gateway.");

    return order;
  }

  // FLOW 3: our order in the gateway's neutral shape (still no Fawaterak field names here;
  // FawaterakGateway turns this into Fawaterak's JSON in FLOW 4). Amounts are in piasters.
  private static CheckoutRequest BuildCheckoutRequest(Order order, Payment payment, string email)
  {
    var nameParts = order.ShippingFullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    var items = order.OrderItems
        .Select(i => new CheckoutItem(i.ProductName, (long)(i.UnitPrice * 100), i.Quantity))
        .ToList();

    if (order.ShippingFee > 0)
      items.Add(new CheckoutItem("Shipping", (long)(order.ShippingFee * 100), 1));

    return new CheckoutRequest(
        MerchantReference: order.Id.ToString(),
        AmountMinor: payment.AmountInMinorUnits,
        Currency: Currency,
        Method: payment.PaymentMethod,
        Items: items,
        Customer: new CheckoutCustomer(
            FirstName: nameParts[0],
            LastName: nameParts.Length > 1 ? nameParts[1] : nameParts[0],
            Email: email,
            Phone: order.ShippingPhone,
            Street: order.ShippingStreet,
            City: order.ShippingCity,
            Country: order.ShippingCountry));
  }
}
