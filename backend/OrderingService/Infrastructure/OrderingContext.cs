using Avra.EventBus.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;
using OrderingService.Domain.AggregatesModel.OrderAggregate;
using OrderingService.Domain.SeedWork;
using OrderingService.Infrastructure.EntityConfigurations;
using OrderingService.Infrastructure.Idempotency;

namespace OrderingService.Infrastructure;

/// <summary>
/// The ordering write model. Doubles as the unit of work: saving changes also dispatches the
/// domain events the aggregates raised, so side effects run inside the same transaction.
/// </summary>
public class OrderingContext(DbContextOptions<OrderingContext> options, IMediator mediator)
    : DbContext(options), IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Buyer> Buyers => Set<Buyer>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<ClientRequest> ClientRequests => Set<ClientRequest>();

    public IDbContextTransaction? GetCurrentTransaction() => _currentTransaction;

    public bool HasActiveTransaction => _currentTransaction is not null;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new OrderEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BuyerEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentMethodEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ClientRequestEntityTypeConfiguration());

        // The outbox lives in this database so events commit with the state that produced them.
        modelBuilder.UseIntegrationEventLogs();
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch first: handlers may add further changes that must land in the same commit.
        await mediator.DispatchDomainEventsAsync(this);
        await base.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IDbContextTransaction?> BeginTransactionAsync()
    {
        if (_currentTransaction is not null)
        {
            return null;
        }

        _currentTransaction = await Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction != _currentTransaction)
        {
            throw new InvalidOperationException($"Transaction {transaction.TransactionId} is not current");
        }

        try
        {
            await SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.RollbackAsync();
            }
        }
        finally
        {
            if (_currentTransaction is not null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }
}
