using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Avra.EventBus.Outbox;

/// <summary>
/// Runs a unit of work inside an explicit transaction under the context's retry strategy.
/// Required because EF Core refuses user-initiated transactions while retries are enabled.
/// </summary>
public sealed class ResilientTransaction
{
    private readonly DbContext _context;

    private ResilientTransaction(DbContext context) => _context = context;

    public static ResilientTransaction New(DbContext context) => new(context);

    public async Task ExecuteAsync(Func<IDbContextTransaction, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await action(transaction);
            await transaction.CommitAsync();
        });
    }
}
