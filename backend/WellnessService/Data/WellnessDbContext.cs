using Microsoft.EntityFrameworkCore;
using WellnessService.Models;

namespace WellnessService.Data;

/// <summary>
/// Database context for Wellness Service
/// </summary>
public class WellnessDbContext : DbContext
{
    public WellnessDbContext(DbContextOptions<WellnessDbContext> options) : base(options)
    {
    }

    public DbSet<WellnessPlan> WellnessPlans { get; set; }
    public DbSet<UserProgress> UserProgresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // WellnessPlan configuration
        modelBuilder.Entity<WellnessPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Must match UserProgress.PlanId exactly or SQL Server rejects the foreign key.
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Level).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.IsActive);
        });

        // UserProgress configuration
        modelBuilder.Entity<UserProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PlanId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Weight).HasPrecision(5, 2);
            entity.HasOne(e => e.Plan)
                .WithMany(p => p.UserProgresses)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PlanId);
            entity.HasIndex(e => e.Status);
        });

        // Seed sample wellness plans
        // Fixed so the seed stays deterministic across schema creation and future migrations.
        var seededAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<WellnessPlan>().HasData(
            new WellnessPlan
            {
                Id = "plan-1",
                Name = "30-Day Yoga Challenge",
                Description = "A guided reset for strength, breath, and daily stillness.",
                Category = "Yoga",
                Level = "Beginner",
                DurationDays = 30,
                ImageUrl = "avra_8.jpg",
                Price = 29.99m,
                IsActive = true,
                CreatedAt = seededAt
            },
            new WellnessPlan
            {
                Id = "plan-2",
                Name = "Weight Loss Program",
                Description = "Movement and nourishment guidance for sustainable energy.",
                Category = "Fitness",
                Level = "Intermediate",
                DurationDays = 84,
                ImageUrl = "avra_32.jpg",
                Price = 99.99m,
                IsActive = true,
                CreatedAt = seededAt
            },
            new WellnessPlan
            {
                Id = "plan-3",
                Name = "Meditation Mastery",
                Description = "A simple practice arc for clarity, rest, and emotional grounding.",
                Category = "Meditation",
                Level = "Beginner",
                DurationDays = 21,
                ImageUrl = "avra_1.jpg",
                Price = 19.99m,
                IsActive = true,
                CreatedAt = seededAt
            }
        );
    }
}
