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
  // Turns the customer's cart into an order, run again once if a product's stock changed at the same moment
  public Task<PlaceOrderResult> PlaceOrderAsync(
      Guid userId, PlaceOrderRequest request, string idempotencyKey, CancellationToken ct) =>
      db.RetryOnStockConflictAsync(logger, async () =>
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
      });

  // The customer cancels their own order, a paid card order is refused because the refund is done by support
  public Task<OrderDto> CancelOrderAsync(Guid userId, Guid orderId, CancellationToken ct) =>
      db.RetryOnStockConflictAsync(logger, async () =>
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

  // Every customer's orders for the admin, newest first, with the count per status for the same search and dates
  public async Task<AdminOrderPage> ListAllOrdersAsync(AdminOrderQuery q, CancellationToken ct)
  {
    if (q.Page < 1)
      throw new ArgumentException("page must be 1 or more.");
    if (q.PageSize is < 1 or > AdminOrderQuery.MaxPageSize)
      throw new ArgumentException($"pageSize must be between 1 and {AdminOrderQuery.MaxPageSize}.");
    if (q.From > q.To)
      throw new ArgumentException("from cannot be after to.");

    var query = db.Orders.AsNoTracking();

    // The days are UTC days, and To moves to the start of the next day so its whole day is included
    if (q.From is { } from)
    {
      var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
      query = query.Where(o => o.CreatedAt >= start);
    }
    if (q.To is { } to)
    {
      var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
      query = query.Where(o => o.CreatedAt < end);
    }

    // The customer's name or email anywhere, or the order number typed from its start
    if (!string.IsNullOrWhiteSpace(q.Search))
    {
      if (q.Search.Length > AdminOrderQuery.MaxSearchLength)
        throw new ArgumentException($"q cannot be longer than {AdminOrderQuery.MaxSearchLength} characters.");

      var term = q.Search.Trim().ToLower();
      query = query.Where(o => o.User.Name.ToLower().Contains(term)
                               || o.User.Email!.ToLower().Contains(term)
                               || o.Id.ToString().StartsWith(term));
    }

    // Counted before the status filter, and a status with no orders still gets its 0
    var counts = await query
        .GroupBy(o => o.Status)
        .Select(g => new { Status = g.Key, Count = g.Count() })
        .ToDictionaryAsync(c => c.Status, c => c.Count, ct);
    var statusCounts = Enum.GetValues<OrderStatus>().ToDictionary(s => s, s => counts.GetValueOrDefault(s));

    if (q.Status is { } status)
      query = query.Where(o => o.Status == status);

    var totalCount = await query.CountAsync(ct);
    var items = await query
        .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
        .Skip((q.Page - 1) * q.PageSize)
        .Take(q.PageSize)
        .Select(o => new AdminOrderSummaryDto(
            o.Id,
            o.CreatedAt,
            o.UpdatedAt,
            o.User.Name,
            o.User.Email!,
            o.Status,
            o.PaymentMethod,
            o.Payment == null ? null : o.Payment.Status,
            o.SubTotal,
            o.ShippingFee,
            o.TotalAmount,
            o.OrderItems.Sum(i => i.Quantity),
            Order.NextStatusesFor(o.Status, o.PaymentMethod, o.Payment == null ? null : o.Payment.Status)))
        .ToListAsync(ct);

    return new AdminOrderPage(items, q.Page, q.PageSize, totalCount, statusCounts);
  }

  public async Task<AdminOrderDto> GetAnyOrderAsync(Guid orderId, CancellationToken ct) =>
      AdminOrderDto.From(await OrdersWithDetailsAndCustomer().AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct)
          ?? throw new NotFoundException($"Order {orderId} was not found."));

  // The admin moves an order one step, a cancel returns the stock and a move the order does not allow is a 409
  public Task<AdminOrderDto> ChangeOrderStatusAsync(Guid orderId, OrderStatus next, CancellationToken ct) =>
      db.RetryOnStockConflictAsync(logger, async () =>
      {
        var order = await OrdersWithDetailsAndCustomer().FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} was not found.");

        if (next == OrderStatus.Cancelled)
          order.Cancel();
        else
          order.ChangeStatus(next);

        await db.SaveChangesAsync(ct);
        return AdminOrderDto.From(order);
      });

  private IQueryable<Order> OrdersWithDetails() => db.Orders
      .Include(o => o.Payment)
      .Include(o => o.OrderItems).ThenInclude(i => i.Product);

  private IQueryable<Order> OrdersWithDetailsAndCustomer() => OrdersWithDetails().Include(o => o.User);
}
