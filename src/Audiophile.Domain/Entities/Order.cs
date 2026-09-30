using Audiophile.Domain.Common;
using Audiophile.Domain.Enums;
using Audiophile.Domain.ValueObjects;

namespace Audiophile.Domain.Entities;

// The order aggregate. Totals are derived from the lines at placement and never
// assigned from outside, and status only moves along the transitions declared
// below (M4-15), so an order cannot be talked into an impossible shape.
public sealed class Order
{
    public const int IdempotencyKeyMaxLength = 128;

    // The state machine, written once. Everything else asks this table.
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
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

    private Order(
        Guid userId,
        PaymentMethod paymentMethod,
        ShippingDetails shipping,
        IReadOnlyCollection<OrderLine> lines,
        decimal shippingFee,
        decimal discountAmount,
        string idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(shipping);
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
            throw new ArgumentException("An order needs at least one line.", nameof(lines));

        if (lines.Select(l => l.ProductId).Distinct().Count() != lines.Count)
            throw new ArgumentException("An order cannot contain the same product twice.", nameof(lines));

        UserId = Guard.NotEmpty(userId, nameof(userId));
        PaymentMethod = Guard.Defined(paymentMethod, nameof(paymentMethod));
        IdempotencyKey = Guard.Text(idempotencyKey, 1, IdempotencyKeyMaxLength, nameof(idempotencyKey));

        ShippingFullName = shipping.FullName;
        ShippingPhone = shipping.Phone;
        ShippingStreet = shipping.Street;
        ShippingCity = shipping.City;
        ShippingCountry = shipping.Country;
        ShippingPostalCode = shipping.PostalCode;

        foreach (var line in lines)
            _orderItems.Add(new OrderItem(Id, line));

        ShippingFee = Guard.Money(shippingFee, nameof(shippingFee));
        DiscountAmount = Guard.Money(discountAmount, nameof(discountAmount));
        SubTotal = _orderItems.Sum(i => i.LineTotal);

        if (DiscountAmount > SubTotal)
            throw new ArgumentException(
                "The discount cannot be larger than the order's subtotal.", nameof(discountAmount));

        TotalAmount = SubTotal - DiscountAmount + ShippingFee;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; private set; }

    // Snapshots — frozen at purchase time so a later edit to the address
    // cannot rewrite where an old order was shipped.
    public string ShippingFullName { get; private set; } = null!;
    public string ShippingPhone { get; private set; } = null!;
    public string ShippingStreet { get; private set; } = null!;
    public string ShippingCity { get; private set; } = null!;
    public string ShippingCountry { get; private set; } = null!;
    public string? ShippingPostalCode { get; private set; }

    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public string IdempotencyKey { get; private set; } = null!;

    public User User { get; private set; } = null!;
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public Payment? Payment { get; private set; }

    public bool HasShipped => Status is OrderStatus.Shipped or OrderStatus.Delivered;

    // M4-14 — the customer's "can I still cancel this?".
    public bool CanBeCancelled => CanTransitionTo(OrderStatus.Cancelled);

    // M4-02 — the only way an order comes into existence.
    public static Order Place(
        Guid userId,
        PaymentMethod paymentMethod,
        ShippingDetails shipping,
        IReadOnlyCollection<OrderLine> lines,
        decimal shippingFee,
        string idempotencyKey,
        decimal discountAmount = 0m) =>
        new(userId, paymentMethod, shipping, lines, shippingFee, discountAmount, idempotencyKey);

    public bool CanTransitionTo(OrderStatus next) => AllowedTransitions[Status].Contains(next);

    // Re-declaring the status an order already holds is a no-op rather than an
    // error, because payment gateways deliver the same event more than once (M4-11).
    public void TransitionTo(OrderStatus next)
    {
        Guard.Defined(next, nameof(next));

        if (next == Status)
            return;

        if (!CanTransitionTo(next))
            throw new InvalidOperationException($"An order that is {Status} cannot become {next}.");

        Status = next;
        Touch();
    }

    public void Confirm() => TransitionTo(OrderStatus.Confirmed);

    public void StartProcessing() => TransitionTo(OrderStatus.Processing);

    public void Ship() => TransitionTo(OrderStatus.Shipped);

    public void Deliver() => TransitionTo(OrderStatus.Delivered);

    public void Cancel() => TransitionTo(OrderStatus.Cancelled);

    // Creates the payment record for this order. The amount comes from the order,
    // never from a caller, so the two can never disagree.
    public Payment StartPayment()
    {
        if (Payment is not null)
            throw new InvalidOperationException("This order already has a payment.");

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Payment cannot be started for a {Status} order.");

        Payment = new Payment(Id, PaymentMethod, TotalAmount);
        Touch();
        return Payment;
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
