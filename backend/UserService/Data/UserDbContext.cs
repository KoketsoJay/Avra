using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data;

/// <summary>
/// Database context for User Service
/// </summary>
public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserMembership> UserMemberships { get; set; }
    public DbSet<MembershipPlan> MembershipPlans { get; set; }
    public DbSet<MembershipPlanFeature> MembershipPlanFeatures { get; set; }
    public DbSet<MemberDiscount> MemberDiscounts { get; set; }
    public DbSet<MembershipInvoice> MembershipInvoices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Must match UserMembership.UserId exactly or SQL Server rejects the foreign key.
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // UserMembership configuration
        modelBuilder.Entity<UserMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PlanName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MonthlyPrice).HasPrecision(18, 2);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            // The billing worker sweeps on this column every cycle.
            entity.HasIndex(e => e.NextBillingDate);
        });

        modelBuilder.Entity<MembershipInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PlanName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.HasOne(e => e.Membership)
                .WithMany()
                .HasForeignKey(e => e.MembershipId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.MembershipId);
            entity.HasIndex(e => e.Status);
        });

        // MembershipPlan configuration
        modelBuilder.Entity<MembershipPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Tagline).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MonthlyPrice).HasPrecision(18, 2);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<MembershipPlanFeature>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlanId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Text).HasMaxLength(200).IsRequired();
            entity.HasOne(e => e.Plan)
                .WithMany(p => p.Features)
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.PlanId);
        });

        // MemberDiscount configuration
        modelBuilder.Entity<MemberDiscount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Percentage).HasPrecision(5, 2);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.GrantedBy).HasMaxLength(255);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // One override per member, so granting is an upsert rather than a growing history.
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        SeedMembershipPlans(modelBuilder);
    }

    private static void SeedMembershipPlans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MembershipPlan>().HasData(
            new MembershipPlan { Id = "plan-avra-way", Name = "The AVRA Way", Tagline = "Begin your becoming", MonthlyPrice = 299m, DiscountPercentage = 10m, IsHighlighted = false, SortOrder = 1 },
            new MembershipPlan { Id = "plan-avra-club", Name = "The AVRA Club", Tagline = "Live with intention", MonthlyPrice = 499m, DiscountPercentage = 20m, IsHighlighted = true, SortOrder = 2 },
            new MembershipPlan { Id = "plan-avra-elite", Name = "The AVRA Elite", Tagline = "Become her, fully", MonthlyPrice = 890m, DiscountPercentage = 30m, IsHighlighted = false, SortOrder = 3 }
        );

        string[] wayFeatures = ["Access to AVRA Community", "Monthly wellness newsletter", "10% member discount", "1 digital wellness guide/month", "Early event access"];
        string[] clubFeatures = ["Everything in The AVRA Way", "Bi-weekly live sessions", "20% member discount", "3 digital guides/month", "Priority event registration", "Club-exclusive collections"];
        string[] eliteFeatures = ["Everything in The AVRA Club", "Monthly 1:1 coaching session", "30% member discount", "Unlimited digital resources", "Complimentary retreat access", "Quarterly curated gift box"];

        var features = new List<MembershipPlanFeature>();
        var id = 1;
        foreach (var (planId, texts) in new[]
        {
            ("plan-avra-way", wayFeatures),
            ("plan-avra-club", clubFeatures),
            ("plan-avra-elite", eliteFeatures),
        })
        {
            for (var order = 0; order < texts.Length; order++)
            {
                features.Add(new MembershipPlanFeature { Id = id++, PlanId = planId, Text = texts[order], SortOrder = order });
            }
        }

        modelBuilder.Entity<MembershipPlanFeature>().HasData(features);
    }
}
