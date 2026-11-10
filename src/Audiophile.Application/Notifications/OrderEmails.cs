using System.Globalization;
using System.Net;
using Audiophile.Application.Abstractions;
using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;
using Audiophile.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Audiophile.Application.Notifications;

// Where the links in emails point, the shop's address as customers see it
public sealed class ShopOptions
{
    public const string SectionName = "Shop";

    public string Url { get; set; } = "http://localhost:5173";
}

// Writes and sends the customer's email for each step of their order, called by the message bus consumers
public sealed class OrderEmails(IAppDbContext db, IEmailSender sender, IOptions<ShopOptions> shop, ILogger<OrderEmails> logger)
{
    public async Task SendStatusEmailAsync(OrderStatusChanged change, CancellationToken ct)
    {
        var order = await LoadAsync(change.OrderId, ct);
        if (order is null)
            return;

        if (StatusEmail(order, change) is not { } email)
            return;

        await SendAsync(order, email.Subject, email.Message, ct);
    }

    public async Task SendRefundEmailAsync(OrderRefunded refund, CancellationToken ct)
    {
        var order = await LoadAsync(refund.OrderId, ct);
        if (order is null)
            return;

        var number = OrderNumber.Of(order.Id);
        await SendAsync(order, $"Refund for order {number} sent",
            $"We sent {Money(refund.Amount)} back to the card you paid with. Your bank can take up to 14 days to show it.", ct);
    }

    // The email for each status, null when the customer gets none, such as a card order whose payment page never opened
    private static (string Subject, string Message)? StatusEmail(Order order, OrderStatusChanged change)
    {
        var number = OrderNumber.Of(order.Id);
        var paidByCard = order.PaymentMethod == PaymentMethod.Card;

        return change.To switch
        {
            OrderStatus.Confirmed when paidByCard => ($"Payment received, order {number} is confirmed",
                $"We received your payment of {Money(order.TotalAmount)} and confirmed your order. We'll email you at every step."),
            OrderStatus.Confirmed => ($"Order {number} is confirmed",
                $"Thanks for your order. You'll pay {Money(order.TotalAmount)} in cash when it arrives, and we'll email you at every step."),
            OrderStatus.Processing => ($"We're preparing order {number}",
                "Your order is being checked and packed."),
            OrderStatus.Shipped => ($"Order {number} is on its way",
                $"Your order left our warehouse and is on its way to {order.ShippingCity}."),
            OrderStatus.Delivered => ($"Order {number} was delivered",
                "Your order was delivered. Enjoy the sound."),
            OrderStatus.Cancelled when paidByCard && order.Payment?.CheckoutUrl is null => null,
            OrderStatus.Cancelled when paidByCard && order.Payment?.Status is PaymentStatus.Succeeded or PaymentStatus.Refunded =>
                ($"Order {number} was cancelled",
                $"Your order was cancelled. Your payment of {Money(order.TotalAmount)} will go back to your card, and we'll email you when it's sent."),
            OrderStatus.Cancelled when change.From == OrderStatus.Pending && paidByCard => ($"Order {number} was cancelled",
                "Your order was cancelled before it was paid, so nothing was charged."),
            OrderStatus.Cancelled => ($"Order {number} was cancelled",
                "Your order was cancelled. If you didn't ask for this, reply to this email."),
            _ => null,
        };
    }

    private async Task SendAsync(Order order, string subject, string message, CancellationToken ct)
    {
        var link = $"{shop.Value.Url.TrimEnd('/')}/orders/{order.Id}";
        var greeting = $"Hi {order.User.Name},";

        var text = string.Join("\n", [
            greeting, "", message, "",
            .. order.OrderItems.Select(i => $"{i.Quantity} x {i.ProductName}  {Money(i.LineTotal)}"),
            $"Shipping  {Money(order.ShippingFee)}",
            $"Total  {Money(order.TotalAmount)}", "",
            $"See your order: {link}", "", "Audiophile",
        ]);

        await sender.SendAsync(new EmailMessage(order.User.Email!, order.User.Name, subject,
            Html(greeting, message, order, link), text), ct);
        logger.LogInformation("Sent \"{Subject}\" for order {OrderId}", subject, order.Id);
    }

    private static string Html(string greeting, string message, Order order, string link)
    {
        static string E(string value) => WebUtility.HtmlEncode(value);

        var rows = string.Concat(order.OrderItems.Select(i =>
            $"<tr><td style=\"padding:6px 0\">{i.Quantity} &times; {E(i.ProductName)}</td>" +
            $"<td style=\"padding:6px 0;text-align:right\">{Money(i.LineTotal)}</td></tr>"));

        return $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;color:#191919">
              <h1 style="font-size:20px;letter-spacing:4px;text-transform:uppercase">Audiophile</h1>
              <p>{E(greeting)}</p>
              <p>{E(message)}</p>
              <table style="width:100%;border-collapse:collapse;border-top:1px solid #ddd;margin-top:16px">
                {rows}
                <tr><td style="padding:6px 0;color:#666">Shipping</td><td style="padding:6px 0;text-align:right;color:#666">{Money(order.ShippingFee)}</td></tr>
                <tr><td style="padding:6px 0;font-weight:bold">Total</td><td style="padding:6px 0;text-align:right;font-weight:bold">{Money(order.TotalAmount)}</td></tr>
              </table>
              <p style="margin-top:24px">
                <a href="{E(link)}" style="background:#d87d4a;color:#fff;padding:12px 24px;text-decoration:none;text-transform:uppercase;font-size:13px;letter-spacing:1px">See your order</a>
              </p>
            </div>
            """;
    }

    private async Task<Order?> LoadAsync(Guid orderId, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Payment)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            logger.LogWarning("Order {OrderId} was not found, so no email was sent", orderId);
        return order;
    }

    internal static string Money(decimal amount) => "EGP " + amount.ToString("N2", CultureInfo.InvariantCulture);
}

// The short number customers and admins see, the last 8 characters of the id like the shop shows
public static class OrderNumber
{
    public static string Of(Guid orderId) => "#" + orderId.ToString("N")[^8..].ToUpperInvariant();
}
