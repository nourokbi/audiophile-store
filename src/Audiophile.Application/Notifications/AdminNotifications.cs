using Audiophile.Application.Abstractions;
using Audiophile.Application.Common;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Audiophile.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Audiophile.Application.Notifications;

public sealed record AdminNotificationDto(
    Guid Id, AdminNotificationKind Kind, string Title, string Message, Guid? OrderId, Guid? ProductId, DateTime CreatedAt, bool IsRead);

public sealed record AdminNotificationList(IReadOnlyList<AdminNotificationDto> Items, int UnreadCount)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;
}

// Turns order and stock events into the dashboard's notifications, and lets an admin read them
public sealed class AdminNotifications(IAppDbContext db)
{
    public async Task OnOrderStatusChangedAsync(OrderStatusChanged change, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Payment)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == change.OrderId, ct);
        if (order is null)
            return;

        var number = OrderNumber.Of(order.Id);
        var total = OrderEmails.Money(order.TotalAmount);
        var paidByCard = order.PaymentMethod == PaymentMethod.Card;

        // An order the admin never saw confirmed, such as an unpaid card order, needs no notification when it is cancelled
        AdminNotification? notification = change.To switch
        {
            OrderStatus.Confirmed => new(AdminNotificationKind.NewOrder, $"New order {number}",
                $"{order.User.Name} ordered {order.OrderItems.Sum(i => i.Quantity)} items for {total}, " +
                (paidByCard ? "paid by card." : "cash on delivery."), order.Id),
            OrderStatus.Cancelled when paidByCard && order.Payment?.Status is PaymentStatus.Succeeded or PaymentStatus.Refunded =>
                new(AdminNotificationKind.RefundNeeded, $"Order {number} cancelled, refund needed",
                    $"Send {total} back from the Fawaterak dashboard, then mark the order refunded.", order.Id),
            OrderStatus.Cancelled when change.From != OrderStatus.Pending =>
                new(AdminNotificationKind.OrderCancelled, $"Order {number} cancelled",
                    $"{order.User.Name}'s order of {total} was cancelled and its items are back in stock.", order.Id),
            _ => null,
        };

        if (notification is null)
            return;

        db.AdminNotifications.Add(notification);
        await db.SaveChangesAsync(ct);
    }

    public async Task OnStockLowAsync(ProductStockLow low, CancellationToken ct)
    {
        var name = low.ProductName.Length > 150 ? low.ProductName[..150] + "…" : low.ProductName;
        db.AdminNotifications.Add(new AdminNotification(AdminNotificationKind.LowStock, $"Low stock: {name}",
            low.Stock == 0 ? "It just sold out, restock it to keep selling." : $"Only {low.Stock} left, restock it soon.",
            productId: low.ProductId));
        await db.SaveChangesAsync(ct);
    }

    // The newest first, with how many are still unread in total
    public async Task<AdminNotificationList> ListAsync(int limit, CancellationToken ct)
    {
        if (limit is < 1 or > AdminNotificationList.MaxLimit)
            throw new ArgumentException($"limit must be between 1 and {AdminNotificationList.MaxLimit}.");

        var items = await db.AdminNotifications.AsNoTracking()
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Take(limit)
            .Select(n => new AdminNotificationDto(n.Id, n.Kind, n.Title, n.Message, n.OrderId, n.ProductId, n.CreatedAt, n.ReadAt != null))
            .ToListAsync(ct);
        var unread = await db.AdminNotifications.CountAsync(n => n.ReadAt == null, ct);
        return new AdminNotificationList(items, unread);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken ct)
    {
        var notification = await db.AdminNotifications.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new NotFoundException($"Notification {id} was not found.");
        notification.MarkRead();
        await db.SaveChangesAsync(ct);
    }

    public Task MarkAllReadAsync(CancellationToken ct) =>
        db.AdminNotifications
            .Where(n => n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
}
