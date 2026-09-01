namespace OrderingService.Domain.AggregatesModel.BuyerAggregate;

/// <summary>
/// Accepted card networks.
/// </summary>
public enum CardType
{
    Amex = 1,
    Visa = 2,
    MasterCard = 3,
}

/// <summary>
/// How a buyer settles an order. Only <see cref="Card"/> captures a stored payment method;
/// the others are settled out of band, so no card details are collected for them.
/// </summary>
public static class PaymentMethods
{
    public const string Card = "Card";
    public const string BankTransfer = "BankTransfer";
    public const string CashOnDelivery = "CashOnDelivery";

    public static bool RequiresCard(string? paymentMethod) =>
        string.IsNullOrWhiteSpace(paymentMethod) || string.Equals(paymentMethod, Card, StringComparison.OrdinalIgnoreCase);
}
