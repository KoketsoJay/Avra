using OrderingService.Domain.SeedWork;

namespace OrderingService.Domain.AggregatesModel.OrderAggregate;

/// <summary>
/// Persistence contract for the Order aggregate root.
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    Order Add(Order order);

    void Update(Order order);

    Task<Order?> GetAsync(int orderId);
}
