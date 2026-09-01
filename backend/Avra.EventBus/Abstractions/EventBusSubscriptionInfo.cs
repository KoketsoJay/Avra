using System.Text.Json;

namespace Avra.EventBus.Abstractions;

/// <summary>
/// The set of event names this process consumes, plus the serialiser settings used on the wire.
/// Registered as a singleton option so the dispatcher can filter broker rows cheaply.
/// </summary>
public sealed class EventBusSubscriptionInfo
{
    /// <summary>Maps the wire event name to the CLR type to deserialise into.</summary>
    public Dictionary<string, Type> EventTypes { get; } = new(StringComparer.Ordinal);

    /// <summary>Serialiser settings shared by producers and consumers.</summary>
    public JsonSerializerOptions JsonSerializerOptions { get; } = new(JsonSerializerDefaults.Web);
}
