using Audiophile.Domain.Common;
using Audiophile.Domain.Enums;

namespace Audiophile.Domain.Entities;

// An order placed from the cart, its totals are fixed when it is placed and its status only moves along the allowed steps
public sealed class Order
{
    public const int IdempotencyKeyMaxLength = 128;

    // The statuses an order may move to from each status
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedNextStatuses = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [],
        [OrderStatus.Cancelled] = [],
    };

    private readonly List<OrderItem> _orderItems = [];

    private Order() { } // EF Core

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; private set; }

    // A copy of the address taken when the order is placed, so editing the address later does not change this order
    public string ShippingFullName { get; private set; } = null!;
    public string ShippingPhone { get; private set; } = null!;
    public string ShippingStreet { get; private set; } = null!;
    public string ShippingCity { get; private set; } = null!;
    public string ShippingCountry { get; private set; } = null!;
    public string? ShippingPostalCode { get; private set; }

    public decimal SubTotal { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public string IdempotencyKey { get; private set; } = null!;

    public User User { get; private set; } = null!;
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public Payment? Payment { get; private set; }

    public bool CanBeCancelled => AllowedNextStatuses[Status].Contains(OrderStatus.Cancelled);

    // The statuses this order may move to now, so the payment must be loaded
    public IReadOnlyList<OrderStatus> NextStatuses => NextStatusesFor(Status, PaymentMethod, Payment?.Status);

    // A card order waits for its payment before it can be confirmed, static so a database query can call it with its columns
    public static IReadOnlyList<OrderStatus> NextStatusesFor(OrderStatus status, PaymentMethod paymentMethod, PaymentStatus? paymentStatus) =>
        paymentMethod == PaymentMethod.Card && paymentStatus != PaymentStatus.Succeeded
            ? AllowedNextStatuses[status].Where(s => s != OrderStatus.Confirmed).ToList()
            : AllowedNextStatuses[status];

    // Income is a paid order that was not cancelled, a cancelled paid card order is waiting for its refund
    public static bool CountsAsRevenue(OrderStatus status, PaymentStatus? paymentStatus) =>
        status != OrderStatus.Cancelled && paymentStatus == PaymentStatus.Succeeded;

    // Creates the order with its payment, copies the address, and takes every item out of stock
    public static Order Place(
        Guid userId,
        PaymentMethod paymentMethod,
        Address address,
        IReadOnlyCollection<(Product Product, int Quantity)> items,
        string idempotencyKey)
    {
        if (items.Count == 0)
            throw new ArgumentException("An order needs at least one item.", nameof(items));

        var order = new Order
        {
            UserId = Guard.NotEmpty(userId, nameof(userId)),
            PaymentMethod = Guard.Defined(paymentMethod, nameof(paymentMethod)),
            IdempotencyKey = Guard.Text(idempotencyKey, 1, IdempotencyKeyMaxLength, nameof(idempotencyKey)),
            ShippingFullName = address.FullAddressName,
            ShippingPhone = address.Phone,
            ShippingStreet = address.Street,
            ShippingCity = address.City,
            ShippingCountry = address.Country,
            ShippingPostalCode = address.PostalCode,
        };

        foreach (var (product, quantity) in items)
        {
            if (!product.IsActive)
                throw new InvalidOperationException($"'{product.Name}' is no longer sold.");

            product.DecreaseStock(quantity);
            order._orderItems.Add(new OrderItem(order.Id, product, quantity));
        }

        order.SubTotal = order._orderItems.Sum(i => i.LineTotal);
        order.ShippingFee = ShippingPolicy.FeeFor(order.SubTotal);
        order.TotalAmount = order.SubTotal + order.ShippingFee;
        order.Payment = new Payment(order.Id, paymentMethod, order.TotalAmount);
        return order;
    }

    public void Confirm() => ChangeStatus(OrderStatus.Confirmed);

    // Puts the items back in stock and fails a payment that is still pending, so the payment and products must be loaded
    public void Cancel()
    {
        ChangeStatus(OrderStatus.Cancelled);

        if (Payment?.Status == PaymentStatus.Pending)
            Payment.MarkFailed();

        foreach (var item in _orderItems)
            item.Product.Restock(item.Quantity);
    }

    public void ChangeStatus(OrderStatus next)
    {
        if (!NextStatuses.Contains(next))
            throw new InvalidOperationException(AllowedNextStatuses[Status].Contains(next)
                ? "This card order is not paid yet, it is confirmed once the payment succeeds."
                : $"An order that is {Status} cannot become {next}.");

        Status = next;
        UpdatedAt = DateTime.UtcNow;

        if (next == OrderStatus.Delivered && PaymentMethod == PaymentMethod.CashOnDelivery)
            Payment!.MarkCashCollected();
    }
}
