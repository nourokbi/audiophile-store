using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Application.Payments;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Audiophile.Application.Orders;

// Places orders from the customer's cart and reads them back
public sealed class OrderService(IAppDbContext db, PaymentService payments, ILogger<OrderService> logger)
{
  public Task<PlaceOrderResult> PlaceOrderAsync(
      Guid userId, PlaceOrderRequest request, string idempotencyKey, CancellationToken ct) =>
      RetryOnStockConflictAsync(() => TryPlaceOrderAsync(userId, request, idempotencyKey, ct));

  // The customer cancels their own order, a paid card order is refused because the refund is done by support
  public Task<OrderDto> CancelOrderAsync(Guid userId, Guid orderId, CancellationToken ct) =>
      RetryOnStockConflictAsync(async () =>
      {
        var order = await OrdersWithDetails().FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
            ?? throw new NotFoundException($"Order {orderId} was not found.");

        if (order.Payment?.Status == PaymentStatus.Succeeded)
          throw new ConflictException("This order is already paid, please contact support to cancel it and get a refund.");
        if (!order.CanBeCancelled)
          throw new ConflictException($"An order that is {order.Status} can no longer be cancelled.");

        order.Cancel();
        await db.SaveChangesAsync(ct);
        return OrderDto.From(order);
      });

  private async Task<PlaceOrderResult> TryPlaceOrderAsync(
      Guid userId, PlaceOrderRequest request, string idempotencyKey, CancellationToken ct)
  {
    // A repeated request with the same key gets back the order it already created instead of a second one
    var existingOrder = await OrdersWithDetails()
        .FirstOrDefaultAsync(o => o.UserId == userId && o.IdempotencyKey == idempotencyKey, ct);
    if (existingOrder is not null)
      return new PlaceOrderResult(OrderDto.From(existingOrder), Created: false);

    var address = await db.Addresses
        .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId && a.IsActive, ct)
        ?? throw new NotFoundException($"Address {request.AddressId} was not found.");

    var cart = await db.Carts
        .Include(c => c.CartItems).ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(c => c.UserId == userId, ct);
    if (cart is null || cart.IsEmpty)
      throw new ConflictException("Your cart is empty.");

    var cartProducts = cart.CartItems.Select(i => (i.Product, i.Quantity)).ToList();
    var order = Order.Place(userId, request.PaymentMethod, address, cartProducts, idempotencyKey);
    db.Orders.Add(order);

    if (request.PaymentMethod == PaymentMethod.CashOnDelivery)
    {
      order.Confirm();
      cart.Clear();
      await db.SaveChangesAsync(ct);
      return new PlaceOrderResult(OrderDto.From(order), Created: true);
    }

    // Saved before calling Fawaterak because its webhook can arrive within seconds and must find the order
    await db.SaveChangesAsync(ct);

    try
    {
      await payments.OpenCheckoutAsync(order, ct);
      cart.Clear();
      await db.SaveChangesAsync(ct);
    }
    catch (Exception ex) when (ex is GatewayException or GatewayTimeoutException)
    {
      // Nobody can pay without a payment page, so the order is cancelled and its stock returned
      logger.LogWarning(ex, "Could not open a checkout for order {OrderId}", order.Id);
      order.Cancel();
      await db.SaveChangesAsync(CancellationToken.None);
      throw;
    }

    return new PlaceOrderResult(OrderDto.From(order), Created: true);
  }

  public async Task<OrderDto> GetOrderAsync(Guid userId, Guid orderId, CancellationToken ct)
  {
    var order = await OrdersWithDetails().FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
        ?? throw new NotFoundException($"Order {orderId} was not found.");
    return OrderDto.From(order);
  }

  public async Task<IReadOnlyList<OrderDto>> ListOrdersAsync(Guid userId, CancellationToken ct)
  {
    var orders = await OrdersWithDetails().Where(o => o.UserId == userId).ToListAsync(ct);
    return orders.OrderByDescending(o => o.CreatedAt).Select(OrderDto.From).ToList();
  }

  // Runs the work once more on fresh data when another request changed the same stock, a second conflict becomes a 409
  private async Task<T> RetryOnStockConflictAsync<T>(Func<Task<T>> work)
  {
    try
    {
      return await work();
    }
    catch (DbUpdateConcurrencyException)
    {
      logger.LogInformation("Stock changed by another request, trying once more");
      db.ChangeTracker.Clear();
      return await work();
    }
  }

  private IQueryable<Order> OrdersWithDetails() => db.Orders
      .Include(o => o.Payment)
      .Include(o => o.OrderItems).ThenInclude(i => i.Product);
}
