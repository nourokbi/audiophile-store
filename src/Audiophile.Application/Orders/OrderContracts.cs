using Audiophile.Domain.Entities;
using Audiophile.Domain.Enums;

namespace Audiophile.Application.Orders;

// The customer picks a saved address and a payment method, the products come from their cart
public sealed record PlaceOrderRequest(Guid AddressId, PaymentMethod PaymentMethod);

public sealed record PlaceOrderResult(OrderDto Order, bool Created);

public sealed record OrderItemDto(
    Guid ProductId, string ProductName, string ProductImageUrl, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record PaymentDto(
    PaymentStatus Status,
    decimal Amount,
    string? Provider,
    string? ProviderOrderId,
    string? ProviderTransactionId,
    string? CheckoutUrl);

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
          .Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.ProductImageUrl, i.Quantity, i.UnitPrice, i.LineTotal))
          .ToList(),
      order.Payment is { } p
          ? new PaymentDto(p.Status, p.Amount, p.Provider, p.ProviderOrderId, p.ProviderTransactionId,
              p.Status == PaymentStatus.Pending ? p.CheckoutUrl : null)
          : null);
}

// Filters for the admin orders list, From and To are whole days and To includes its own day
public sealed record AdminOrderQuery(
    OrderStatus? Status,
    DateOnly? From,
    DateOnly? To,
    string? Search,
    int Page = 1,
    int PageSize = AdminOrderQuery.DefaultPageSize)
{
  public const int DefaultPageSize = 10;
  public const int MaxPageSize = 50;
  public const int MaxSearchLength = 100;
}

public sealed record ShippingDto(string FullName, string Phone, string Street, string City, string Country, string? PostalCode);

// One row of the admin orders list, ItemCount adds up the quantities
public sealed record AdminOrderSummaryDto(
    Guid Id,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    PaymentStatus? PaymentStatus,
    decimal SubTotal,
    decimal ShippingFee,
    decimal TotalAmount,
    int ItemCount,
    IReadOnlyList<OrderStatus> NextStatuses);

// One order for the admin, the summary fields plus where it goes and what is in it
public sealed record AdminOrderDto(
    Guid Id,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    PaymentStatus? PaymentStatus,
    decimal SubTotal,
    decimal ShippingFee,
    decimal TotalAmount,
    int ItemCount,
    IReadOnlyList<OrderStatus> NextStatuses,
    ShippingDto Shipping,
    IReadOnlyList<OrderItemDto> Items)
{
  // The user, payment and items must be loaded
  public static AdminOrderDto From(Order order) => new(
      order.Id,
      order.CreatedAt,
      order.UpdatedAt,
      order.User.Name,
      order.User.Email!,
      order.Status,
      order.PaymentMethod,
      order.Payment?.Status,
      order.SubTotal,
      order.ShippingFee,
      order.TotalAmount,
      order.OrderItems.Sum(i => i.Quantity),
      order.NextStatuses,
      new ShippingDto(order.ShippingFullName, order.ShippingPhone, order.ShippingStreet, order.ShippingCity,
          order.ShippingCountry, order.ShippingPostalCode),
      order.OrderItems
          .Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.ProductImageUrl, i.Quantity, i.UnitPrice, i.LineTotal))
          .ToList());
}

// One page of orders plus how many orders each status has, the counts ignore the status filter so every tab keeps its number
public sealed record AdminOrderPage(
    IReadOnlyList<AdminOrderSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyDictionary<OrderStatus, int> StatusCounts)
{
  public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record ChangeStatusRequest(OrderStatus Status);

// The business numbers for the admin overview and income pages, every period is whole Cairo days ending today
// Revenue counts paid orders that were not cancelled, the category and product totals leave the shipping out
public sealed record OrderStatsDto(
    int Days,
    decimal Revenue,
    decimal PreviousRevenue,
    int OrderCount,
    int PaidOrderCount,
    int ActiveOrders,
    int CustomerCount,
    IReadOnlyList<DailySales> PerDay,
    IReadOnlyList<PaymentMethodSales> ByPaymentMethod,
    IReadOnlyList<CategorySales> ByCategory,
    IReadOnlyList<ProductSales> TopProducts,
    IReadOnlyDictionary<OrderStatus, int> StatusCounts)
{
  public const int MaxDays = 365;
  public const int TopProductCount = 5;
}

public sealed record DailySales(DateOnly Day, int Orders, decimal Revenue);

public sealed record PaymentMethodSales(PaymentMethod PaymentMethod, int Orders, decimal Revenue);

public sealed record CategorySales(ProductCategory Category, int UnitsSold, decimal Revenue);

public sealed record ProductSales(Guid ProductId, string ProductName, int UnitsSold, decimal Revenue);
