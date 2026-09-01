using System.ComponentModel.DataAnnotations;

namespace Avra.EventBus.Sql;

/// <summary>
/// Settings for the Azure SQL backed broker. Every service publishes into, and polls from,
/// one shared broker database.
/// </summary>
public sealed class SqlEventBusOptions
{
    public const string SectionName = "EventBus";

    /// <summary>Connection string of the shared broker database.</summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Logical consumer group. Each service uses its own name so every service receives its own
    /// copy of an event, while multiple instances of the same service compete for it.
    /// </summary>
    [Required]
    public string SubscriberName { get; set; } = string.Empty;

    /// <summary>How often the dispatcher polls the broker table.</summary>
    [Range(typeof(TimeSpan), "00:00:00.100", "00:05:00")]
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Maximum number of messages claimed per poll.</summary>
    [Range(1, 500)]
    public int BatchSize { get; set; } = 50;

    /// <summary>Attempts made before a message is parked as poisoned.</summary>
    [Range(1, 20)]
    public int MaxDeliveryAttempts { get; set; } = 5;

    /// <summary>How long delivered messages are retained before clean-up.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "30.00:00:00")]
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);
}
