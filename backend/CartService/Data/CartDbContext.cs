using Microsoft.EntityFrameworkCore;
using CartService.Models;

namespace CartService.Data;

/// <summary>
/// Database context for Cart Service
/// </summary>
public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options)
    {
    }

    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cart configuration
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
        });

        // CartItem configuration
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}
