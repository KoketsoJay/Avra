using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;

namespace OrderingService.Infrastructure.EntityConfigurations;

/// <summary>
/// Maps the Buyer aggregate.
/// </summary>
public class BuyerEntityTypeConfiguration : IEntityTypeConfiguration<Buyer>
{
    public void Configure(EntityTypeBuilder<Buyer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("buyers");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).UseHiLo("buyerseq");

        builder.Property(b => b.IdentityGuid).HasMaxLength(200).IsRequired();
        builder.HasIndex(b => b.IdentityGuid).IsUnique();

        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();

        builder.HasMany(b => b.PaymentMethods)
            .WithOne()
            .HasForeignKey("BuyerId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Buyer.PaymentMethods))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>
/// Maps stored payment methods. Card numbers are persisted masked by the application layer and
/// the security number is never stored at all.
/// </summary>
public class PaymentMethodEntityTypeConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("paymentmethods");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseHiLo("paymentseq");

        builder.Property<int>("BuyerId").IsRequired();

        builder.Property<string>("_cardHolderName")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("CardHolderName")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property<string>("_alias")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Alias")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property<string>("_cardNumber")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("CardNumber")
            .HasMaxLength(25)
            .IsRequired();

        builder.Property<DateTime>("_expiration")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Expiration")
            .IsRequired();

        builder.Property<int>("_cardTypeId")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("CardTypeId")
            .IsRequired();
    }
}
