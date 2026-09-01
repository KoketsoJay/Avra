namespace OrderingService.Domain.SeedWork;

/// <summary>
/// Marks the only entity in an aggregate that the outside world may hold a reference to.
/// Repositories exist per aggregate root, never per entity.
/// </summary>
public interface IAggregateRoot;

/// <summary>
/// The transactional boundary of the domain model.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists changes and dispatches the domain events raised while doing so.</summary>
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Persistence contract for an aggregate root.
/// </summary>
/// <typeparam name="T">The aggregate root type.</typeparam>
public interface IRepository<T>
    where T : IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}
