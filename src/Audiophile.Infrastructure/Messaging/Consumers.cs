using Audiophile.Application.Notifications;
using Audiophile.Domain.Events;
using MassTransit;

namespace Audiophile.Infrastructure.Messaging;

// Each consumer has its own queue, so every event reaches both the customer's email and the admin's bell
internal sealed class CustomerEmailConsumer(OrderEmails emails) : IConsumer<OrderStatusChanged>, IConsumer<OrderRefunded>
{
    public Task Consume(ConsumeContext<OrderStatusChanged> context) =>
        emails.SendStatusEmailAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<OrderRefunded> context) =>
        emails.SendRefundEmailAsync(context.Message, context.CancellationToken);
}

internal sealed class AdminNotificationConsumer(AdminNotifications notifications)
    : IConsumer<OrderStatusChanged>, IConsumer<ProductStockLow>
{
    public Task Consume(ConsumeContext<OrderStatusChanged> context) =>
        notifications.OnOrderStatusChangedAsync(context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<ProductStockLow> context) =>
        notifications.OnStockLowAsync(context.Message, context.CancellationToken);
}
