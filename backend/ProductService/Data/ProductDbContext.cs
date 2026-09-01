using Avra.EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data;

/// <summary>
/// Database context for Product Service
/// </summary>
public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Integration events are written here so a price change and its announcement commit together.
        modelBuilder.UseIntegrationEventLogs();

        // Seed initial data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Fixed so the seed stays deterministic across schema creation and future migrations.
        var seededAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Apparel", Description = "Everyday pieces designed for the woman becoming" },
            new Category { Id = 2, Name = "Activewear", Description = "Movement-ready sets for running, studio and flow" },
            new Category { Id = 3, Name = "Wellness", Description = "Curated ritual kits and wellness boxes" },
            new Category { Id = 4, Name = "Accessories", Description = "Lifestyle accessories that carry the becoming" }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "The Piece Short Set",
                Description = "Effortless two-piece set for the woman becoming.",
                Price = 890m,
                Category = "Apparel",
                Stock = 40,
                RestockThreshold = 10,
                MaxStockThreshold = 120,
                Tag = "Bestseller",
                Image1 = "avra_30.jpg",
                Image2 = "avra_11.jpg",
                CreatedAt = seededAt
            },
            new Product
            {
                Id = 2,
                Name = "The AVRA Run Set",
                Description = "Run for more than the finish line.",
                Price = 750m,
                Category = "Activewear",
                Stock = 55,
                RestockThreshold = 12,
                MaxStockThreshold = 150,
                Image1 = "avra_49.jpg",
                Image2 = "avra_8.jpg",
                CreatedAt = seededAt
            },
            new Product
            {
                Id = 3,
                Name = "Stillness Ritual Bodysuit",
                Description = "Sculpted, intentional comfort. Stillness without guilt.",
                Price = 650m,
                Category = "Apparel",
                Stock = 60,
                RestockThreshold = 12,
                MaxStockThreshold = 150,
                Tag = "New",
                Image1 = "avra_46.jpg",
                Image2 = "avra_1.jpg",
                CreatedAt = seededAt
            },
            new Product
            {
                Id = 4,
                Name = "The Becoming Box",
                Description = "A curated wellness ritual kit for her becoming.",
                Price = 1200m,
                Category = "Wellness",
                Stock = 25,
                RestockThreshold = 8,
                MaxStockThreshold = 80,
                Tag = "Limited",
                Image1 = "avra_47.jpg",
                Image2 = "avra_59.jpg",
                CreatedAt = seededAt
            },
            new Product
            {
                Id = 5,
                Name = "Movement Dress",
                Description = "Flow through every season of her becoming.",
                Price = 950m,
                Category = "Activewear",
                Stock = 35,
                RestockThreshold = 10,
                MaxStockThreshold = 120,
                Image1 = "avra_50.jpg",
                Image2 = "avra_45.jpg",
                CreatedAt = seededAt
            },
            new Product
            {
                Id = 6,
                Name = "The AVRA Lifestyle Set",
                Description = "Carry your becoming with you, everywhere.",
                Price = 1100m,
                Category = "Accessories",
                Stock = 30,
                RestockThreshold = 8,
                MaxStockThreshold = 100,
                Image1 = "avra_44.jpg",
                Image2 = "avra_32.jpg",
                CreatedAt = seededAt
            }
        );
    }
}
