using OrderingService.Domain.Exceptions;
using OrderingService.Domain.SeedWork;

namespace OrderingService.Domain.AggregatesModel.BuyerAggregate;

/// <summary>
/// A card a buyer has used before. Only the last four digits are retained — never the full number
/// and never the CVV, neither of which leaves the basket service.
/// </summary>
public class PaymentMethod : Entity
{
    private readonly string _alias = string.Empty;
    private readonly string _cardNumber = string.Empty;
    private readonly string _cardHolderName = string.Empty;
    private readonly DateTime _expiration;
    private readonly int _cardTypeId;

    protected PaymentMethod()
    {
    }

    public PaymentMethod(int cardTypeId, string alias, string cardNumber, string cardHolderName, DateTime expiration)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            throw new OrderingDomainException(nameof(cardNumber));
        }

        if (string.IsNullOrWhiteSpace(cardHolderName))
        {
            throw new OrderingDomainException(nameof(cardHolderName));
        }

        if (expiration < DateTime.UtcNow)
        {
            throw new OrderingDomainException(nameof(expiration));
        }

        _cardTypeId = cardTypeId;
        _alias = alias;
        _cardNumber = cardNumber;
        _cardHolderName = cardHolderName;
        _expiration = expiration;
    }

    public int CardTypeId => _cardTypeId;

    public string CardNumber => _cardNumber;

    /// <summary>True when this stored card is the one being used for a payment.</summary>
    public bool IsEqualTo(int cardTypeId, string cardNumber, DateTime expiration) =>
        _cardTypeId == cardTypeId &&
        string.Equals(_cardNumber, cardNumber, StringComparison.Ordinal) &&
        _expiration == expiration;
}
