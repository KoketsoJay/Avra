using Microsoft.EntityFrameworkCore;
using EventsService.Models;

namespace EventsService.Data;

/// <summary>
/// Database context for Events Service
/// </summary>
public class EventsDbContext : DbContext
{
    public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }
    public DbSet<EventTicket> EventTickets { get; set; }
    public DbSet<Promotion> Promotions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Event configuration
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Must match EventRegistration.EventId exactly or SQL Server rejects the foreign key.
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Organizer).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsFeatured);
            entity.HasIndex(e => e.StartDate);
        });

        // EventRegistration configuration
        modelBuilder.Entity<EventRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.EventId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.UserEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.AmountPaid).HasPrecision(10, 2);
            entity.HasOne(e => e.Event)
                .WithMany(e => e.Registrations)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });

        // EventTicket configuration
        modelBuilder.Entity<EventTicket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.RegistrationId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.EventId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.TicketNumber).HasMaxLength(64).IsRequired();
            entity.Property(e => e.QrCodePayload).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.QrCodePngBase64).HasMaxLength(16384).IsRequired();
            entity.HasOne(e => e.Registration)
                .WithMany(r => r.Tickets)
                .HasForeignKey(e => e.RegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Event)
                .WithMany()
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.RegistrationId);
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TicketNumber).IsUnique();
        });

        // Promotion configuration
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PromoCode).HasMaxLength(50);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.HasIndex(e => e.PromoCode).IsUnique().HasFilter("[PromoCode] IS NOT NULL");
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EndDate);
        });
    }
}
