using OrderingService.Domain.Exceptions;
using OrderingService.Domain.SeedWork;

namespace OrderingService.Domain.AggregatesModel.OrderAggregate;

/// <summary>
/// A line on an order. Part of the Order aggregate: it can only be reached through the root.
/// </summary>
public class OrderItem : Entity
{
    private string _productName = string.Empty;
    private string? _pictureUrl;
    private decimal _unitPrice;
    private decimal _discount;
    private int _units;

    private OrderItem()
    {
    }

    public OrderItem(int productId, string productName, decimal unitPrice, decimal discount, string? pictureUrl, int units = 1)
    {
        if (units <= 0)
        {
            throw new OrderingDomainException("Invalid number of units");
        }

        if (unitPrice * units < discount)
        {
            throw new OrderingDomainException("The total of order item is lower than applied discount");
        }

        ProductId = productId;
        _productName = productName;
        _unitPrice = unitPrice;
        _discount = discount;
        _units = units;
        _pictureUrl = pictureUrl;
    }

    public int ProductId { get; private set; }

    public string ProductName => _productName;

    public string? PictureUrl => _pictureUrl;

    public decimal UnitPrice => _unitPrice;

    public decimal Discount => _discount;

    public int Units => _units;

    public void SetNewDiscount(decimal discount)
    {
        if (discount < 0)
        {
            throw new OrderingDomainException("Discount is not valid");
        }

        _discount = discount;
    }

    public void AddUnits(int units)
    {
        if (units < 0)
        {
            throw new OrderingDomainException("Invalid units");
        }

        _units += units;
    }
}
