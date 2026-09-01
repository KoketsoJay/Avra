using System.Runtime.Serialization;
using MediatR;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;

namespace OrderingService.Application.Commands;

/// <summary>
/// A line submitted with a new order. Prices are taken from the basket snapshot the catalogue
/// produced, never from raw client input.
/// </summary>
public class OrderItemDTO
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public int Units { get; set; }

    public string? PictureUrl { get; set; }
}

/// <summary>
/// Starts an order from an accepted checkout. Handled once per request id thanks to
/// <see cref="IdentifiedCommand{T, R}"/>.
/// </summary>
[DataContract]
public class CreateOrderCommand : IRequest<bool>
{
    private readonly List<OrderItemDTO> _orderItems = [];

    public CreateOrderCommand()
    {
    }

    public CreateOrderCommand(
        List<OrderItemDTO> basketItems,
        string userId,
        string userName,
        string city,
        string street,
        string state,
        string country,
        string zipcode,
        string paymentMethod,
        string cardNumber,
        string cardHolderName,
        DateTime cardExpiration,
        string cardSecurityNumber,
        int cardTypeId)
    {
        _orderItems = basketItems ?? [];
        UserId = userId;
        UserName = userName;
        City = city;
        Street = street;
        State = state;
        Country = country;
        ZipCode = zipcode;
        PaymentMethod = paymentMethod;
        CardNumber = cardNumber;
        CardHolderName = cardHolderName;
        CardExpiration = cardExpiration;
        CardSecurityNumber = cardSecurityNumber;
        CardTypeId = cardTypeId;
    }

    [DataMember]
    public IEnumerable<OrderItemDTO> OrderItems => _orderItems;

    [DataMember]
    public string UserId { get; private set; } = string.Empty;

    [DataMember]
    public string UserName { get; private set; } = string.Empty;

    [DataMember]
    public string City { get; private set; } = string.Empty;

    [DataMember]
    public string Street { get; private set; } = string.Empty;

    [DataMember]
    public string State { get; private set; } = string.Empty;

    [DataMember]
    public string Country { get; private set; } = string.Empty;

    [DataMember]
    public string ZipCode { get; private set; } = string.Empty;

    /// <summary>Card, BankTransfer or CashOnDelivery. Card details are only required for Card.</summary>
    [DataMember]
    public string PaymentMethod { get; private set; } = PaymentMethods.Card;

    [DataMember]
    public string CardNumber { get; private set; } = string.Empty;

    [DataMember]
    public string CardHolderName { get; private set; } = string.Empty;

    [DataMember]
    public DateTime CardExpiration { get; private set; }

    [DataMember]
    public string CardSecurityNumber { get; private set; } = string.Empty;

    [DataMember]
    public int CardTypeId { get; private set; }
}

/// <summary>Marks the grace period as elapsed so stock validation can begin.</summary>
public record SetAwaitingValidationOrderStatusCommand(int OrderNumber) : IRequest<bool>;

/// <summary>Records the catalogue's confirmation that every line is in stock.</summary>
public record SetStockConfirmedOrderStatusCommand(int OrderNumber) : IRequest<bool>;

/// <summary>Cancels an order because the catalogue rejected one or more lines.</summary>
public record SetStockRejectedOrderStatusCommand(int OrderNumber, List<int> OrderStockItems) : IRequest<bool>;

/// <summary>Records a successful payment.</summary>
public record SetPaidOrderStatusCommand(int OrderNumber) : IRequest<bool>;

/// <summary>Cancels an order at the buyer's request.</summary>
public record CancelOrderCommand(int OrderNumber) : IRequest<bool>;

/// <summary>Marks a paid order as dispatched with its carrier and tracking reference.</summary>
public record ShipOrderCommand(int OrderNumber, string Carrier, string TrackingNumber) : IRequest<bool>;
