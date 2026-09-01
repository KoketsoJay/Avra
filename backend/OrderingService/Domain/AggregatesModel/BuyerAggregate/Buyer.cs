using OrderingService.Domain.Events;
using OrderingService.Domain.Exceptions;
using OrderingService.Domain.SeedWork;

namespace OrderingService.Domain.AggregatesModel.BuyerAggregate;

/// <summary>
/// The Buyer aggregate root: the ordering service's own view of a customer, together with the
/// payment methods they have used.
/// </summary>
public class Buyer : Entity, IAggregateRoot
{
    private readonly List<PaymentMethod> _paymentMethods = [];

    protected Buyer() => _paymentMethods = [];

    public Buyer(string identity, string name)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new OrderingDomainException(nameof(identity));
        }

        IdentityGuid = identity;
        Name = name;
        _paymentMethods = [];
    }

    /// <summary>The user id issued by the identity provider.</summary>
    public string IdentityGuid { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public IEnumerable<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

    /// <summary>
    /// Returns the stored card matching the supplied details, adding it first if it is new.
    /// </summary>
    public PaymentMethod VerifyOrAddPaymentMethod(
        int cardTypeId,
        string alias,
        string cardNumber,
        string cardHolderName,
        DateTime expiration,
        int orderId)
    {
        var existingPayment = _paymentMethods.Find(p => p.IsEqualTo(cardTypeId, cardNumber, expiration));

        if (existingPayment is not null)
        {
            AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, existingPayment, orderId));
            return existingPayment;
        }

        var payment = new PaymentMethod(cardTypeId, alias, cardNumber, cardHolderName, expiration);
        _paymentMethods.Add(payment);
        AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, payment, orderId));

        return payment;
    }

    /// <summary>
    /// Confirms the buyer for an order settled without a card, so the order still gets a buyer
    /// but no payment instrument is stored.
    /// </summary>
    public void VerifyWithoutPaymentMethod(int orderId) =>
        AddDomainEvent(new BuyerAndPaymentMethodVerifiedDomainEvent(this, null, orderId));
}
