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

        // Seed sample events
        // Fixed so the seed stays deterministic across schema creation and future migrations.
        var seededAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                Id = "event-1",
                Title = "Stillness Weekend Retreat",
                Description = "A full weekend immersion of movement, breathwork and quiet in the Drakensberg.",
                Category = "Retreat",
                Location = "Drakensberg, KZN",
                StartDate = new DateTime(2026, 9, 15, 14, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc),
                MaxAttendees = 40,
                Price = 3500m,
                Organizer = "AVRA",
                ImageUrl = "avra_45.jpg",
                IsFeatured = true,
                CreatedAt = seededAt,
                Status = EventStatus.Active
            },
            new Event
            {
                Id = "event-2",
                Title = "Morning Run Series",
                Description = "A weekly community run across our cities. Every pace welcome.",
                Category = "Community Run",
                Location = "Multiple Cities",
                StartDate = new DateTime(2026, 9, 5, 6, 30, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 9, 5, 8, 0, 0, DateTimeKind.Utc),
                MaxAttendees = 200,
                Price = 0m,
                Organizer = "AVRA",
                ImageUrl = "avra_10.jpg",
                IsFeatured = true,
                CreatedAt = seededAt,
                Status = EventStatus.Active
            },
            new Event
            {
                Id = "event-3",
                Title = "Women's Circle",
                Description = "An evening of shared reflection, journaling and honest conversation.",
                Category = "Community",
                Location = "Online & In-Person",
                StartDate = new DateTime(2026, 10, 5, 19, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 10, 5, 21, 0, 0, DateTimeKind.Utc),
                MaxAttendees = 60,
                Price = 250m,
                Organizer = "AVRA",
                ImageUrl = "avra_2.jpg",
                IsFeatured = false,
                CreatedAt = seededAt,
                Status = EventStatus.Active
            },
            new Event
            {
                Id = "event-4",
                Title = "Holistic Living Workshop",
                Description = "A full-day workshop on nourishment, rest and building rituals that last.",
                Category = "Workshop",
                Location = "Johannesburg, GP",
                StartDate = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 10, 12, 16, 0, 0, DateTimeKind.Utc),
                MaxAttendees = 50,
                Price = 850m,
                Organizer = "AVRA",
                ImageUrl = "avra_51.jpg",
                IsFeatured = false,
                CreatedAt = seededAt,
                Status = EventStatus.Active
            }
        );
    }
}
