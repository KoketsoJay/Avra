namespace OrderingService.Domain.Exceptions;

/// <summary>
/// Signals that a domain invariant was violated. It derives from <see cref="InvalidOperationException"/>
/// so the shared problem-details handler already surfaces it as a 400 rather than a 500.
/// </summary>
public class OrderingDomainException : InvalidOperationException
{
    public OrderingDomainException()
    {
    }

    public OrderingDomainException(string message)
        : base(message)
    {
    }

    public OrderingDomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
