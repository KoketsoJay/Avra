using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderingService.Domain.SeedWork;

namespace OrderingService.Infrastructure;

/// <summary>
/// Bridges the aggregates' collected domain events into MediatR.
/// </summary>
public static class MediatorExtension
{
    /// <summary>
    /// Publishes every domain event raised by tracked entities, clearing them first so a handler
    /// that saves again cannot replay the same event.
    /// </summary>
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, DbContext context)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(context);

        var domainEntities = context.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents?.Count > 0)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents!)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent);
        }
    }
}
