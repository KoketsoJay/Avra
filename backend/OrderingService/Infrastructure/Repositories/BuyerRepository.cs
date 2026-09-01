using Microsoft.EntityFrameworkCore;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;
using OrderingService.Domain.SeedWork;

namespace OrderingService.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for the Buyer aggregate.
/// </summary>
public class BuyerRepository(OrderingContext context) : IBuyerRepository
{
    public IUnitOfWork UnitOfWork => context;

    public Buyer Add(Buyer buyer)
    {
        ArgumentNullException.ThrowIfNull(buyer);

        return buyer.IsTransient()
            ? context.Buyers.Add(buyer).Entity
            : buyer;
    }

    public Buyer Update(Buyer buyer) => context.Buyers.Update(buyer).Entity;

    public async Task<Buyer?> FindAsync(string buyerIdentityGuid)
    {
        var buyer = await context.Buyers
            .Include(b => b.PaymentMethods)
            .SingleOrDefaultAsync(b => b.IdentityGuid == buyerIdentityGuid);

        return buyer;
    }

    public async Task<Buyer?> FindByIdAsync(int id) =>
        await context.Buyers
            .Include(b => b.PaymentMethods)
            .SingleOrDefaultAsync(b => b.Id == id);
}
