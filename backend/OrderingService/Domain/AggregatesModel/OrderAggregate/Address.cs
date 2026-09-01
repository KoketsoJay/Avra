using OrderingService.Domain.SeedWork;

namespace OrderingService.Domain.AggregatesModel.OrderAggregate;

/// <summary>
/// A postal address. Immutable by design — changing where an order ships means a new value.
/// </summary>
public class Address : ValueObject
{
    private Address()
    {
    }

    public Address(string street, string city, string state, string country, string zipCode)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
    }

    public string Street { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    public string ZipCode { get; private set; } = string.Empty;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}
