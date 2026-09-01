using Microsoft.EntityFrameworkCore;

namespace Avra.EventBus.Outbox;

/// <summary>
/// Adds the outbox table to a service's own <see cref="DbContext"/> so events and business state
/// commit together.
/// </summary>
public static class IntegrationEventLogModelBuilderExtensions
{
    public static ModelBuilder UseIntegrationEventLogs(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<IntegrationEventLogEntry>(entity =>
        {
            entity.ToTable("IntegrationEventLog");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventTypeName).IsRequired().HasMaxLength(512);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreationTime).IsRequired();
            entity.Property(e => e.State).IsRequired();
            entity.Property(e => e.TimesSent).IsRequired();
            entity.Property(e => e.TransactionId).IsRequired();
            entity.HasIndex(e => new { e.TransactionId, e.State });
        });

        return modelBuilder;
    }
}
