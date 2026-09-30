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

public sealed class OrderService(
    IAppDbContext db,
    IPaymentGatewayResolver gateways,
    PaymentResultApplier applier,
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

        if (!payment.UsesGateway)
        {
            order.Confirm();                                // M4-12: cash on delivery needs no gateway
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            return new PlaceOrderResult(OrderDto.From(order), Created: true);
        }

        // Save before calling the gateway: the webhook can arrive within seconds and
        // must find the order already in the database.
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        try
        {
            var gateway = gateways.Default;
            var session = await gateway.CreateCheckoutAsync(BuildCheckoutRequest(order, payment), ct);
            payment.LinkToProvider(
                gateway.Name, session.ProviderReference, session.ProviderOrderId, session.CheckoutUrl, session.PaymentCode);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is GatewayException or GatewayTimeoutException)
        {
            // The customer never received a checkout link, so nobody can pay this
            // order. Release the stock and report the failure.
            logger.LogWarning(ex, "Could not open a checkout for order {OrderId}", order.Id);
            payment.MarkFailed();
            OrderStock.CancelAndRelease(order);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        return new PlaceOrderResult(OrderDto.From(order), Created: true);
    }

    public async Task<OrderDto> GetOrderAsync(Guid userId, Guid orderId, CancellationToken ct)
    {
        var order = await LoadOrders().FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
            ?? throw new NotFoundException($"Order {orderId} was not found.");
        return OrderDto.From(order);
    }

    // Everything the gateway told us about this order's payment, oldest first: the
    // audit trail support reads when a customer says "I paid but…".
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

    // Full refund when amount is null, partial otherwise. A full
    // refund of an order that has not shipped also cancels it and restores stock.
    public async Task<OrderDto> RefundAsync(Guid orderId, decimal? amount, CancellationToken ct)
    {
        var order = await LoadGatewayOrderAsync(orderId, ct);
        var payment = order.Payment!;

        if (!payment.IsSettled || payment.ProviderTransactionId is null)
            throw new ConflictException($"Order {orderId} has no successful payment to refund.");

        var refundAmount = amount ?? payment.RefundableAmount;
        if (refundAmount <= 0 || refundAmount > payment.RefundableAmount)
            throw new ConflictException(
                $"Refund must be between 0.01 and {payment.RefundableAmount} {Currency}.");

        // No automatic retry. If this times out, the refund may already have happened
        // at the gateway; retrying blindly could refund the customer twice.
        var result = await gateways.Get(payment.Provider!).RefundAsync(payment.ProviderTransactionId, (long)(refundAmount * 100), ct);
        if (!result.Success)
            throw new GatewayException($"The gateway refused the refund: {result.Message}");

        payment.RecordRefund(refundAmount);
        if (payment.Status == PaymentStatus.Refunded && order.CanBeCancelled)
            OrderStock.CancelAndRelease(order);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Refunded {Amount} {Currency} on order {OrderId} (gateway tx {TransactionId})",
            refundAmount, Currency, orderId, result.TransactionId);
        return OrderDto.From(order);
    }

    // Cancels a same-day card payment before settlement. Nothing is charged.
    public async Task<OrderDto> VoidAsync(Guid orderId, CancellationToken ct)
    {
        var order = await LoadGatewayOrderAsync(orderId, ct);
        var payment = order.Payment!;

        if (payment.Status != PaymentStatus.Succeeded || payment.ProviderTransactionId is null)
            throw new ConflictException("Only a successful payment with no refunds can be voided.");
        if (payment.PaymentMethod != PaymentMethod.Card)
            throw new ConflictException("Only card payments can be voided; use a refund instead.");
        if (!order.CanBeCancelled)
            throw new ConflictException($"An order that is {order.Status} cannot be voided.");

        var result = await gateways.Get(payment.Provider!).VoidAsync(payment.ProviderTransactionId, ct);
        if (!result.Success)
            throw new GatewayException($"The gateway refused the void: {result.Message}");

        payment.MarkVoided();
        OrderStock.CancelAndRelease(order);
        await db.SaveChangesAsync(ct);
        return OrderDto.From(order);
    }

    // Asks the gateway for the payment's real status and applies it. For when a
    // webhook was missed, or before support changes anything by hand.
    public async Task<OrderDto> SyncWithGatewayAsync(Guid orderId, CancellationToken ct)
    {
        var order = await LoadGatewayOrderAsync(orderId, ct);
        var payment = order.Payment!;

        if (payment.Provider is null)
            throw new ConflictException("This payment never reached the gateway.");

        var transactions = await gateways.Get(payment.Provider).FindTransactionsAsync(payment.ProviderTransactionId, payment.ProviderOrderId, ct);
        foreach (var tx in transactions.Where(t => !t.IsChildTransaction))
            await applier.ApplyAsync(payment.Provider, "inquiry", tx, JsonSerializer.Serialize(tx), ct);

        return await GetAnyOrderAsync(orderId, ct);
    }

    // Closes gateway payments nobody finished in time and releases their stock.
    // Asks the gateway first, so a payment whose webhook was lost is confirmed, not cancelled.
    public async Task<int> ExpireStalePaymentsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidates = await db.Payments
            .Where(p => p.Status == PaymentStatus.Pending && p.PaymentMethod != PaymentMethod.CashOnDelivery)
            .Select(p => new { p.OrderId, p.PaymentMethod, p.CreatedAt })
            .ToListAsync(ct);

        var expired = 0;
        foreach (var c in candidates.Where(c => now - c.CreatedAt > PaymentWindow(c.PaymentMethod)))
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

    private static CheckoutRequest BuildCheckoutRequest(Order order, Payment payment)
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
                Email: DemoUser.Email,
                Phone: order.ShippingPhone,
                Street: order.ShippingStreet,
                City: order.ShippingCity,
                Country: order.ShippingCountry));
    }
}
