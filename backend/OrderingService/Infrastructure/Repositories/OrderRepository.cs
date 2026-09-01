using Microsoft.EntityFrameworkCore;
using OrderingService.Domain.AggregatesModel.OrderAggregate;
using OrderingService.Domain.SeedWork;

namespace OrderingService.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for the Order aggregate. Loading an order always brings its lines with it
/// — an aggregate is retrieved and stored as a whole.
/// </summary>
public class OrderRepository(OrderingContext context) : IOrderRepository
{
    public IUnitOfWork UnitOfWork => context;

    public Order Add(Order order) => context.Orders.Add(order).Entity;

    public void Update(Order order) => context.Entry(order).State = EntityState.Modified;

    public async Task<Order?> GetAsync(int orderId)
    {
        var order = await context.Orders.FindAsync(orderId);
        order ??= context.Orders.Local.FirstOrDefault(o => o.Id == orderId);

        if (order is not null)
        {
            // Address is an owned type and always travels with the root; only lines need loading.
            await context.Entry(order).Collection(o => o.OrderItems).LoadAsync();
        }

        return order;
    }
}
