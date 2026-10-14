// using System.Text.Json;                 // PAYMENT GATEWAY (off): used by SyncWithGatewayAsync
using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
// using Audiophile.Application.Payments;  // PAYMENT GATEWAY (off): IFawaterakGateway, PaymentResultApplier
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Audiophile.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Orders;

// PAYMENT GATEWAY (off): the Fawaterak parts of this class are commented out while the
// cart and checkout are built. Only cash on delivery can be placed for now.
// To turn online payment back on, uncomment every block marked "PAYMENT GATEWAY (off)" in:
//   Application/Orders/OrderService.cs, Application/DependencyInjection.cs,
//   Infrastructure/DependencyInjection.cs, Infrastructure/Payments/PaymentExpiryWorker.cs,
//   API/Controllers/OrdersController.cs and API/Controllers/FawaterakController.cs.
public sealed class OrderService(
    IAppDbContext db,
    // IFawaterakGateway fawaterak,        // PAYMENT GATEWAY (off)
    // PaymentResultApplier applier,       // PAYMENT GATEWAY (off)
    ILogger<OrderService> logger)
{
  public const string Currency = "EGP";

  public static TimeSpan PaymentWindow(PaymentMethod method) => method switch
  {
    PaymentMethod.Kiosk => TimeSpan.FromHours(48),   // they have to walk to an outlet
    _ => TimeSpan.FromMinutes(30),
  };


  public async Task<PlaceOrderResult> PlaceOrderAsync(
      Guid userId, PlaceOrderRequest request, string idempotencyKey, CancellationToken ct)
  {
    var existing = await LoadOrders()
        .FirstOrDefaultAsync(o => o.UserId == userId && o.IdempotencyKey == idempotencyKey, ct);
    if (existing is not null)
      return new PlaceOrderResult(OrderDto.From(existing), Created: false);

    if (request.Items is not { Count: > 0 })
      throw new ArgumentException("An order needs at least one item.", nameof(request));

    // PAYMENT GATEWAY (off): card, wallet and kiosk need Fawaterak, so they are refused for now.
    if (request.PaymentMethod != PaymentMethod.CashOnDelivery)
      throw new ArgumentException("Online payment is turned off for now: choose CashOnDelivery.", nameof(request));

    var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
    var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

    var lines = new List<OrderLine>();
    foreach (var item in request.Items)
    {
      if (!products.TryGetValue(item.ProductId, out var product) || !product.IsActive)
        throw new NotFoundException($"Product {item.ProductId} does not exist.");

      product.DecreaseStock(item.Quantity);          // throws → 409 when not enough stock
      lines.Add(OrderLine.FromProduct(product, item.Quantity));
    }

    var s = request.Shipping;
    var shipping = new ShippingDetails(s.FullName, s.Phone, s.Street, s.City, s.Country, s.PostalCode);
    var subTotal = lines.Sum(l => l.LineTotal);

    var order = Order.Place(userId, request.PaymentMethod, shipping, lines,
        ShippingPolicy.FeeFor(subTotal), idempotencyKey);
    var payment = order.StartPayment();
    // Cash On Delivery needs no gateway
    if (!payment.UsesGateway)
    {
      order.Confirm();
      db.Orders.Add(order);
      await db.SaveChangesAsync(ct);
      return new PlaceOrderResult(OrderDto.From(order), Created: true);
    }

    // PAYMENT GATEWAY (off): opening a Fawaterak checkout for card, wallet and kiosk.
    // // Save before calling Fawaterak: the webhook can arrive within seconds and
    // // must find the order already in the database.
    // db.Orders.Add(order);
    // await db.SaveChangesAsync(ct);
    //
    // try
    // {
    //     var session = await fawaterak.CreateCheckoutAsync(BuildCheckoutRequest(order, payment), ct);
    //     payment.LinkToProvider(
    //         IFawaterakGateway.Provider, session.ProviderReference, session.ProviderOrderId, session.CheckoutUrl, session.PaymentCode);
    //     await db.SaveChangesAsync(ct);
    // }
    // catch (Exception ex) when (ex is GatewayException or GatewayTimeoutException)
    // {
    //     // The customer never received a checkout link, so nobody can pay this
    //     // order. Release the stock and report the failure.
    //     logger.LogWarning(ex, "Could not open a checkout for order {OrderId}", order.Id);
    //     payment.MarkFailed();
    //     OrderStock.CancelAndRelease(order);
    //     await db.SaveChangesAsync(CancellationToken.None);
    //     throw;
    // }
    //
    // return new PlaceOrderResult(OrderDto.From(order), Created: true);

    // Not reachable while only cash on delivery is accepted (checked above).
    logger.LogError("Order for {Method} reached the gateway path while it is turned off", request.PaymentMethod);
    throw new InvalidOperationException("Online payment is turned off.");
  }

  public async Task<OrderDto> GetOrderAsync(Guid userId, Guid orderId, CancellationToken ct)
  {
    var order = await LoadOrders().FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
        ?? throw new NotFoundException($"Order {orderId} was not found.");
    return OrderDto.From(order);
  }

  // PAYMENT GATEWAY (off): the payment audit trail and the gateway safety nets.
  // // Everything Fawaterak told us about this order's payment, oldest first: the
  // // audit trail support reads when a customer says "I paid but…".
  // public async Task<IReadOnlyList<PaymentEventDto>> GetPaymentEventsAsync(Guid orderId, CancellationToken ct)
  // {
  //     var paymentId = await db.Payments.Where(p => p.OrderId == orderId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct)
  //         ?? throw new NotFoundException($"Order {orderId} has no payment.");
  //
  //     var events = await db.PaymentEvents.AsNoTracking().Where(e => e.PaymentId == paymentId).ToListAsync(ct);
  //     return events
  //         .OrderBy(e => e.ReceivedAt)
  //         .Select(e => new PaymentEventDto(e.ReceivedAt, e.Provider, e.EventType, e.ExternalId, e.RawPayload))
  //         .ToList();
  // }

  public async Task<IReadOnlyList<OrderDto>> ListOrdersAsync(Guid userId, CancellationToken ct)
  {
    var orders = await LoadOrders().Where(o => o.UserId == userId).ToListAsync(ct);
    return orders.OrderByDescending(o => o.CreatedAt).Select(OrderDto.From).ToList();
  }

  // PAYMENT GATEWAY (off)
  // // Asks Fawaterak for the invoice's real status and applies it. For when a
  // // webhook was missed, or before support changes anything by hand.
  // public async Task<OrderDto> SyncWithGatewayAsync(Guid orderId, CancellationToken ct)
  // {
  //     var order = await LoadGatewayOrderAsync(orderId, ct);
  //     var payment = order.Payment!;
  //
  //     if (payment.ProviderOrderId is null)
  //         throw new ConflictException("This payment never reached Fawaterak.");
  //
  //     var invoice = await fawaterak.GetInvoiceAsync(payment.ProviderOrderId, ct);
  //     await applier.ApplyAsync(IFawaterakGateway.Provider, "inquiry", invoice, JsonSerializer.Serialize(invoice), ct);
  //
  //     return await GetAnyOrderAsync(orderId, ct);
  // }
  //
  // // Closes gateway payments nobody finished in time and releases their stock.
  // // Asks Fawaterak first, so a payment whose webhook was lost is confirmed, not cancelled.
  // public async Task<int> ExpireStalePaymentsAsync(CancellationToken ct)
  // {
  //     var now = DateTime.UtcNow;
  //     var candidates = await db.Payments
  //         .Where(p => p.Status == PaymentStatus.Pending && p.PaymentMethod != PaymentMethod.CashOnDelivery)
  //         .Select(p => new { p.OrderId, p.PaymentMethod, p.CreatedAt })
  //         .ToListAsync(ct);
  //
  //     var expired = 0;
  //     foreach (var c in candidates.Where(c => now - c.CreatedAt > PaymentWindow(c.PaymentMethod)))
  //     {
  //         try
  //         {
  //             var synced = await SyncWithGatewayAsync(c.OrderId, ct);
  //             if (synced.Payment?.Status != PaymentStatus.Pending)
  //                 continue;                          // the gateway knew better; nothing to expire
  //         }
  //         catch (Exception ex) when (ex is GatewayException or GatewayTimeoutException)
  //         {
  //             logger.LogWarning(ex, "Could not check order {OrderId} with the gateway; will retry later", c.OrderId);
  //             continue;                              // never cancel on an unknown outcome
  //         }
  //         catch (ConflictException)
  //         {
  //             // Never linked to the gateway; nothing to ask. Fall through and expire.
  //         }
  //
  //         var order = await LoadOrders().FirstAsync(o => o.Id == c.OrderId, ct);
  //         order.Payment!.MarkFailed();
  //         OrderStock.CancelAndRelease(order);
  //         await db.SaveChangesAsync(ct);
  //         expired++;
  //         logger.LogInformation("Order {OrderId} expired unpaid; stock released", c.OrderId);
  //     }
  //
  //     return expired;
  // }

  private IQueryable<Order> LoadOrders() => db.Orders
      .Include(o => o.Payment)
      .Include(o => o.OrderItems).ThenInclude(i => i.Product);

  // PAYMENT GATEWAY (off): helpers used only by the gateway code above.
  // private async Task<OrderDto> GetAnyOrderAsync(Guid orderId, CancellationToken ct) =>
  //     OrderDto.From(await LoadOrders().AsNoTracking().FirstAsync(o => o.Id == orderId, ct));
  //
  // private async Task<Order> LoadGatewayOrderAsync(Guid orderId, CancellationToken ct)
  // {
  //     var order = await LoadOrders().FirstOrDefaultAsync(o => o.Id == orderId, ct)
  //         ?? throw new NotFoundException($"Order {orderId} was not found.");
  //
  //     if (order.Payment is null || !order.Payment.UsesGateway)
  //         throw new ConflictException("This order was not paid through a payment gateway.");
  //
  //     return order;
  // }
  //
  // private static CheckoutRequest BuildCheckoutRequest(Order order, Payment payment)
  // {
  //     var nameParts = order.ShippingFullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
  //     var items = order.OrderItems
  //         .Select(i => new CheckoutItem(i.ProductName, (long)(i.UnitPrice * 100), i.Quantity))
  //         .ToList();
  //
  //     if (order.ShippingFee > 0)
  //         items.Add(new CheckoutItem("Shipping", (long)(order.ShippingFee * 100), 1));
  //
  //     return new CheckoutRequest(
  //         MerchantReference: order.Id.ToString(),
  //         AmountMinor: payment.AmountInMinorUnits,
  //         Currency: Currency,
  //         Method: payment.PaymentMethod,
  //         Items: items,
  //         Customer: new CheckoutCustomer(
  //             FirstName: nameParts[0],
  //             LastName: nameParts.Length > 1 ? nameParts[1] : nameParts[0],
  //             Email: order.User.Email!,          // load the user with the order when this is turned back on
  //             Phone: order.ShippingPhone,
  //             Street: order.ShippingStreet,
  //             City: order.ShippingCity,
  //             Country: order.ShippingCountry));
  // }
}
