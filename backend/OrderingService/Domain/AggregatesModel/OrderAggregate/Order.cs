using OrderingService.Domain.Exceptions;
using OrderingService.Domain.SeedWork;
using OrderingService.Domain.Events;

namespace OrderingService.Domain.AggregatesModel.OrderAggregate;

/// <summary>
/// The Order aggregate root. All order state transitions go through here so the lifecycle
/// invariants live in one place, and every transition raises the domain event that continues
/// the distributed workflow.
/// </summary>
public class Order : Entity, IAggregateRoot
{
    private readonly List<OrderItem> _orderItems = [];

    private DateTime _orderDate;
    private int? _buyerId;
    private int? _paymentMethodId;
    private string? _description;

    protected Order() => _orderItems = [];

    public Order(
        string userId,
        string userName,
        Address address,
        string paymentMethod,
        int cardTypeId,
        string cardNumber,
        string cardSecurityNumber,
        string cardHolderName,
        DateTime cardExpiration,
        int? buyerId = null,
        int? paymentMethodId = null)
    {
        _buyerId = buyerId;
        _paymentMethodId = paymentMethodId;
        Status = OrderStatus.Submitted;
        _orderDate = DateTime.UtcNow;
        Address = address;

        AddOrderStartedDomainEvent(userId, userName, paymentMethod, cardTypeId, cardNumber, cardSecurityNumber, cardHolderName, cardExpiration);
    }

    public Address Address { get; private set; } = null!;

    public int? GetBuyerId => _buyerId;

    public OrderStatus Status { get; private set; }

    public DateTime OrderDate => _orderDate;

    public string? Description => _description;

    /// <summary>Carrier that took the parcel. Set when the order is dispatched.</summary>
    public string? Carrier { get; private set; }

    /// <summary>Carrier tracking reference. Set when the order is dispatched.</summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>When the order left the warehouse.</summary>
    public DateTime? ShippedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    /// <summary>
    /// Adds a line, merging with an existing line for the same product and keeping the best
    /// discount the shopper has been offered.
    /// </summary>
    public void AddOrderItem(int productId, string productName, decimal unitPrice, decimal discount, string? pictureUrl, int units = 1)
    {
        var existingOrderForProduct = _orderItems.Find(o => o.ProductId == productId);

        if (existingOrderForProduct is not null)
        {
            if (discount > existingOrderForProduct.Discount)
            {
                existingOrderForProduct.SetNewDiscount(discount);
            }

            existingOrderForProduct.AddUnits(units);
            return;
        }

        _orderItems.Add(new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units));
    }

    public void SetBuyerId(int id) => _buyerId = id;

    public void SetPaymentMethodId(int id) => _paymentMethodId = id;

    public void SetAwaitingValidationStatus()
    {
        if (Status != OrderStatus.Submitted)
        {
            throw new OrderingDomainException(StatusChangeError(OrderStatus.AwaitingValidation));
        }

        Status = OrderStatus.AwaitingValidation;
        AddDomainEvent(new OrderStatusChangedToAwaitingValidationDomainEvent(Id, _orderItems));
    }

    public void SetStockConfirmedStatus()
    {
        if (Status != OrderStatus.AwaitingValidation)
        {
            throw new OrderingDomainException(StatusChangeError(OrderStatus.StockConfirmed));
        }

        Status = OrderStatus.StockConfirmed;
        _description = "All the items were confirmed with available stock.";
        AddDomainEvent(new OrderStatusChangedToStockConfirmedDomainEvent(Id));
    }

    public void SetPaidStatus()
    {
        if (Status != OrderStatus.StockConfirmed)
        {
            throw new OrderingDomainException(StatusChangeError(OrderStatus.Paid));
        }

        Status = OrderStatus.Paid;
        _description = "The payment was performed at a simulated \"American Bank checking bank account ending on XX35071\".";
        AddDomainEvent(new OrderStatusChangedToPaidDomainEvent(Id, OrderItems));
    }

    /// <summary>
    /// Records dispatch. The carrier and tracking reference are part of the transition because an
    /// order is not shippable without them — the buyer has to be able to follow the parcel.
    /// </summary>
    /// <param name="carrier">Carrier that collected the parcel.</param>
    /// <param name="trackingNumber">Carrier tracking reference.</param>
    public void SetShippedStatus(string carrier, string trackingNumber)
    {
        if (Status != OrderStatus.Paid)
        {
            throw new OrderingDomainException(StatusChangeError(OrderStatus.Shipped));
        }

        if (string.IsNullOrWhiteSpace(carrier))
        {
            throw new OrderingDomainException("A carrier is required to ship an order.");
        }

        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            throw new OrderingDomainException("A tracking number is required to ship an order.");
        }

        Status = OrderStatus.Shipped;
        Carrier = carrier.Trim();
        TrackingNumber = trackingNumber.Trim();
        ShippedAt = DateTime.UtcNow;
        _description = $"The order was shipped with {Carrier}, tracking {TrackingNumber}.";
        AddDomainEvent(new OrderShippedDomainEvent(this));
    }

    public void SetCancelledStatus()
    {
        // Once paid or shipped the money and the goods have moved; cancelling becomes a return.
        if (Status is OrderStatus.Paid or OrderStatus.Shipped)
        {
            throw new OrderingDomainException(StatusChangeError(OrderStatus.Cancelled));
        }

        Status = OrderStatus.Cancelled;
        _description = "The order was cancelled.";
        AddDomainEvent(new OrderCancelledDomainEvent(this));
    }

    /// <summary>
    /// Cancels the order because the catalogue could not fulfil one or more lines.
    /// </summary>
    public void SetCancelledStatusWhenStockIsRejected(IEnumerable<int> orderStockRejectedItems)
    {
        ArgumentNullException.ThrowIfNull(orderStockRejectedItems);

        if (Status != OrderStatus.AwaitingValidation)
        {
            throw new OrderingDomainException(StatusChangeError(OrderStatus.Cancelled));
        }

        Status = OrderStatus.Cancelled;

        var itemsStockRejectedProductNames = OrderItems
            .Where(i => orderStockRejectedItems.Contains(i.ProductId))
            .Select(i => i.ProductName);

        _description = $"The product items don't have stock: ({string.Join(", ", itemsStockRejectedProductNames)}).";
        AddDomainEvent(new OrderCancelledDomainEvent(this));
    }

    public decimal GetTotal() => _orderItems.Sum(o => o.Units * o.UnitPrice);

    private void AddOrderStartedDomainEvent(
        string userId,
        string userName,
        string paymentMethod,
        int cardTypeId,
        string cardNumber,
        string cardSecurityNumber,
        string cardHolderName,
        DateTime cardExpiration) =>
        AddDomainEvent(new OrderStartedDomainEvent(
            this, userId, userName, paymentMethod, cardTypeId, cardNumber, cardSecurityNumber, cardHolderName, cardExpiration));

    private string StatusChangeError(OrderStatus target) =>
        $"Is not possible to change the order status from {Status} to {target}.";
}
