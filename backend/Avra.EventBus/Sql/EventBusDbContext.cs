using Microsoft.EntityFrameworkCore;

namespace Avra.EventBus.Sql;

/// <summary>
/// EF Core context for the shared Azure SQL broker database.
/// </summary>
public class EventBusDbContext(DbContextOptions<EventBusDbContext> options) : DbContext(options)
{
    public DbSet<EventBusMessage> Messages => Set<EventBusMessage>();

    public DbSet<EventBusDelivery> Deliveries => Set<EventBusDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventBusMessage>(entity =>
        {
            entity.ToTable("EventBusMessages");
            entity.HasKey(e => e.Sequence);
            entity.Property(e => e.Sequence).ValueGeneratedOnAdd();
            entity.Property(e => e.MessageId).IsRequired();
            entity.Property(e => e.EventName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.EnqueuedAt).IsRequired();
            entity.HasIndex(e => e.EventName);
            entity.HasIndex(e => e.EnqueuedAt);
        });

        modelBuilder.Entity<EventBusDelivery>(entity =>
        {
            entity.ToTable("EventBusDeliveries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Subscriber).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Error).HasMaxLength(2000);
            // Guarantees a message is handled once per subscriber even with many instances running.
            entity.HasIndex(e => new { e.MessageSequence, e.Subscriber }).IsUnique();
            entity.HasIndex(e => new { e.Subscriber, e.State });
        });
    }
}
