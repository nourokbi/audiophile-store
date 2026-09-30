using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;

namespace Audiophile.Application.Orders;

public sealed record PlaceOrderItem(Guid ProductId, int Quantity);

public sealed record ShippingRequest(
    string FullName, string Phone, string Street, string City, string Country, string? PostalCode);

// Only product ids and quantities. Prices are always read from the catalogue.
public sealed record PlaceOrderRequest(
    IReadOnlyList<PlaceOrderItem> Items, PaymentMethod PaymentMethod, ShippingRequest Shipping);

public sealed record PlaceOrderResult(OrderDto Order, bool Created);

public sealed record RefundRequest(decimal? Amount);

public sealed record PaymentEventDto(
    DateTime ReceivedAt, string Provider, string EventType, string ExternalId, string RawPayload);

public sealed record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record PaymentDto(
    PaymentStatus Status,
    decimal Amount,
    decimal RefundedAmount,
    string? Provider,
    string? ProviderOrderId,
    string? ProviderTransactionId,
    string? CheckoutUrl,
    string? PaymentCode);

public sealed record OrderDto(
    Guid Id,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    decimal SubTotal,
    decimal ShippingFee,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemDto> Items,
    PaymentDto? Payment)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.Status,
        order.PaymentMethod,
        order.SubTotal,
        order.ShippingFee,
        order.TotalAmount,
        order.CreatedAt,
        order.OrderItems
            .Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.LineTotal))
            .ToList(),
        order.Payment is { } p
            ? new PaymentDto(p.Status, p.Amount, p.RefundedAmount, p.Provider, p.ProviderOrderId,
                p.ProviderTransactionId,
                p.Status == PaymentStatus.Pending ? p.CheckoutUrl : null,
                p.Status == PaymentStatus.Pending ? p.PaymentCode : null)
            : null);
}
