using Audiophile.Domain.Common;
using Audiophile.Domain.Enums;

namespace Audiophile.Domain.Entities;

// A message for the dashboard's bell, shared by every admin so one admin reading it marks it read for all
public sealed class AdminNotification
{
    public const int TitleMaxLength = 200;
    public const int MessageMaxLength = 1000;

    private AdminNotification() { } // EF Core

    public AdminNotification(AdminNotificationKind kind, string title, string message, Guid? orderId = null, Guid? productId = null)
    {
        Kind = Guard.Defined(kind, nameof(kind));
        Title = Guard.Text(title, 1, TitleMaxLength, nameof(title));
        Message = Guard.Text(message, 1, MessageMaxLength, nameof(message));
        OrderId = orderId;
        ProductId = productId;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public AdminNotificationKind Kind { get; private set; }
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public Guid? OrderId { get; private set; }
    public Guid? ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; private set; }

    public void MarkRead() => ReadAt ??= DateTime.UtcNow;
}
