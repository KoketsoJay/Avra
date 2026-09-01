using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderingService.Infrastructure.Idempotency;

/// <summary>
/// A record of a command already accepted from a client. Retries carrying the same request id
/// are recognised and ignored instead of creating duplicate orders.
/// </summary>
public class ClientRequest
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime Time { get; set; }
}

/// <summary>
/// Records and looks up client request ids for idempotent command handling.
/// </summary>
public interface IRequestManager
{
    Task<bool> ExistAsync(Guid id);

    Task CreateRequestForCommandAsync<T>(Guid id);
}

/// <summary>
/// EF Core configuration for the idempotency ledger.
/// </summary>
public class ClientRequestEntityTypeConfiguration : IEntityTypeConfiguration<ClientRequest>
{
    public void Configure(EntityTypeBuilder<ClientRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("requests");
        builder.HasKey(cr => cr.Id);
        builder.Property(cr => cr.Name).HasMaxLength(200).IsRequired();
        builder.Property(cr => cr.Time).IsRequired();
    }
}
