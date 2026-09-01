using System.Reflection;
using Avra.EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Avra.EventBus.Outbox;

/// <summary>
/// Outbox backed by the owning service's <typeparamref name="TContext"/>, so writing an event
/// enlists in the ambient business transaction.
/// </summary>
/// <typeparam name="TContext">The service's EF Core context.</typeparam>
public class IntegrationEventLogService<TContext> : IIntegrationEventLogService
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly List<Type> _eventTypes;

    public IntegrationEventLogService(TContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // Event contracts live either in the shared bus assembly or in the service itself.
        _eventTypes = new[] { typeof(IntegrationEvent).Assembly, typeof(TContext).Assembly }
            .Distinct()
            .SelectMany(assembly => SafeGetTypes(assembly))
            .Where(type => type.IsSubclassOf(typeof(IntegrationEvent)) && !type.IsAbstract)
            .Distinct()
            .ToList();
    }

    public async Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId)
    {
        var result = await _context.Set<IntegrationEventLogEntry>()
            .Where(e => e.TransactionId == transactionId && e.State == EventStateEnum.NotPublished)
            .OrderBy(e => e.CreationTime)
            .ToListAsync();

        return result
            .Select(e =>
            {
                var type = _eventTypes.Find(t => t.Name == e.EventTypeShortName);
                return type is null ? e : e.DeserializeJsonContent(type);
            })
            .Where(e => e.IntegrationEvent is not null)
            .ToList();
    }

    public Task SaveEventAsync(IntegrationEvent integrationEvent, Guid transactionId)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (transactionId == Guid.Empty)
        {
            throw new InvalidOperationException("An ambient transaction is required to record an integration event.");
        }

        _context.Set<IntegrationEventLogEntry>().Add(new IntegrationEventLogEntry(integrationEvent, transactionId));
        return _context.SaveChangesAsync();
    }

    public Task MarkEventAsPublishedAsync(Guid eventId) => UpdateEventStatusAsync(eventId, EventStateEnum.Published);

    public Task MarkEventAsInProgressAsync(Guid eventId) => UpdateEventStatusAsync(eventId, EventStateEnum.InProgress);

    public Task MarkEventAsFailedAsync(Guid eventId) => UpdateEventStatusAsync(eventId, EventStateEnum.PublishedFailed);

    private async Task UpdateEventStatusAsync(Guid eventId, EventStateEnum status)
    {
        var logEntry = await _context.Set<IntegrationEventLogEntry>().SingleOrDefaultAsync(e => e.EventId == eventId);
        if (logEntry is null)
        {
            return;
        }

        logEntry.State = status;
        if (status == EventStateEnum.InProgress)
        {
            logEntry.TimesSent++;
        }

        await _context.SaveChangesAsync();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
