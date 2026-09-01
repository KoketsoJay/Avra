using OrderingService.Domain.SeedWork;

namespace OrderingService.Domain.AggregatesModel.BuyerAggregate;

/// <summary>
/// Persistence contract for the Buyer aggregate root.
/// </summary>
public interface IBuyerRepository : IRepository<Buyer>
{
    Buyer Add(Buyer buyer);

    Buyer Update(Buyer buyer);

    Task<Buyer?> FindAsync(string buyerIdentityGuid);

    Task<Buyer?> FindByIdAsync(int id);
}
