using System.Text.Json.Serialization;

namespace Avra.EventBus.Abstractions;

/// <summary>
/// Base contract for anything published across service boundaries. Integration events are
/// immutable facts about something that already happened — never commands.
/// </summary>
public record IntegrationEvent
{
    /// <summary>Stable identifier used for de-duplication by consumers.</summary>
    [JsonInclude]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>UTC instant the event was created by the producing service.</summary>
    [JsonInclude]
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
}
