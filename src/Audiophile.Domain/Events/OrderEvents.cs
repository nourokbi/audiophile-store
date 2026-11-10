using Audiophile.Domain.Common;
using Audiophile.Domain.Enums;

namespace Audiophile.Domain.Events;

// Every status step of an order, the customer's emails and the admin's notifications start here
public sealed record OrderStatusChanged(Guid OrderId, OrderStatus From, OrderStatus To, DateTime OccurredAt) : IDomainEvent;

// The admin sent a paid order's money back to the customer
public sealed record OrderRefunded(Guid OrderId, decimal Amount, DateTime OccurredAt) : IDomainEvent;

// A sale took a product's stock down to the low stock limit
public sealed record ProductStockLow(Guid ProductId, string ProductName, int Stock, DateTime OccurredAt) : IDomainEvent;
