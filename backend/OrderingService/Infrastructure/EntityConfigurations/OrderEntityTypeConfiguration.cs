using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderingService.Domain.AggregatesModel.OrderAggregate;

namespace OrderingService.Infrastructure.EntityConfigurations;

/// <summary>
/// Maps the Order aggregate. The domain model exposes read-only properties, so the mapping is
/// driven by backing fields — persistence never reshapes the model.
/// </summary>
public class OrderEntityTypeConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).UseHiLo("orderseq");

        builder.OwnsOne(o => o.Address, address =>
        {
            address.WithOwner();
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("State").HasMaxLength(100);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100);
            address.Property(a => a.ZipCode).HasColumnName("ZipCode").HasMaxLength(20);
        });
        builder.Navigation(o => o.Address).IsRequired();

        builder.Property<DateTime>("_orderDate")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("OrderDate")
            .IsRequired();

        builder.Property<int?>("_buyerId")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("BuyerId")
            .IsRequired(false);

        builder.Property<int?>("_paymentMethodId")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("PaymentMethodId")
            .IsRequired(false);

        builder.Property<string>("_description")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Description")
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(o => o.Status)
            .HasConversion<int>()
            .HasColumnName("OrderStatusId")
            .IsRequired();

        builder.Property(o => o.Carrier).HasMaxLength(100).IsRequired(false);
        builder.Property(o => o.TrackingNumber).HasMaxLength(100).IsRequired(false);
        builder.Property(o => o.ShippedAt).IsRequired(false);

        builder.HasMany(o => o.OrderItems)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Order.OrderItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex("_buyerId");
        builder.HasIndex(o => o.Status);
    }
}

/// <summary>
/// Maps order lines. They are only reachable through the Order aggregate root.
/// </summary>
public class OrderItemEntityTypeConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("orderitems");
        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.Id).UseHiLo("orderitemseq");

        builder.Property<int>("OrderId").IsRequired();

        builder.Property<decimal>("_discount")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Discount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(oi => oi.ProductId).HasColumnName("ProductId").IsRequired();

        builder.Property<string>("_productName")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("ProductName")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property<decimal>("_unitPrice")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("UnitPrice")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property<int>("_units")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Units")
            .IsRequired();

        builder.Property<string>("_pictureUrl")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("PictureUrl")
            .IsRequired(false);
    }
}
