using Microsoft.EntityFrameworkCore;
using CommunityService.Models;

namespace CommunityService.Data;

/// <summary>
/// Database context for Community Service
/// </summary>
public class CommunityDbContext : DbContext
{
    public CommunityDbContext(DbContextOptions<CommunityDbContext> options) : base(options)
    {
    }

    public DbSet<Community> Communities { get; set; }
    public DbSet<CommunityMember> CommunityMembers { get; set; }
    public DbSet<CommunityPost> CommunityPosts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Community configuration
        modelBuilder.Entity<Community>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Must match CommunityMember/CommunityPost.CommunityId exactly or the foreign keys fail.
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreatorId).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsPublic);
            entity.HasIndex(e => e.Status);
        });

        // CommunityMember configuration
        modelBuilder.Entity<CommunityMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CommunityId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.Community)
                .WithMany(c => c.Members)
                .HasForeignKey(e => e.CommunityId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CommunityId);
            entity.HasIndex(e => e.UserId);
        });

        // CommunityPost configuration
        modelBuilder.Entity<CommunityPost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CommunityId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.UserName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Content).HasMaxLength(5000).IsRequired();
            entity.HasOne(e => e.Community)
                .WithMany(c => c.Posts)
                .HasForeignKey(e => e.CommunityId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.CommunityId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Seed sample communities
        // Fixed so the seed stays deterministic across schema creation and future migrations.
        var seededAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Community>().HasData(
            new Community
            {
                Id = "community-1",
                Name = "Yoga Enthusiasts",
                Description = "Shared practice, accountability, and weekly movement prompts.",
                Category = "Yoga",
                CreatorId = "user-1",
                MemberCount = 150,
                ImageUrl = "avra_29.jpg",
                IsPublic = true,
                Status = "Active",
                CreatedAt = seededAt
            },
            new Community
            {
                Id = "community-2",
                Name = "Healthy Eaters",
                Description = "Recipes, rituals, and everyday nourishment for busy women.",
                Category = "Nutrition",
                CreatorId = "user-2",
                MemberCount = 200,
                ImageUrl = "avra_32.jpg",
                IsPublic = true,
                Status = "Active",
                CreatedAt = seededAt
            },
            new Community
            {
                Id = "community-3",
                Name = "Meditation & Mindfulness",
                Description = "A gentle circle for quiet, reflection, and mental clarity.",
                Category = "Meditation",
                CreatorId = "user-3",
                MemberCount = 100,
                ImageUrl = "avra_2.jpg",
                IsPublic = true,
                Status = "Active",
                CreatedAt = seededAt
            }
        );
    }
}
