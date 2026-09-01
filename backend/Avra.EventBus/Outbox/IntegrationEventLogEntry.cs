using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Avra.EventBus.Abstractions;

namespace Avra.EventBus.Outbox;

/// <summary>
/// Where an event sits in the outbox workflow.
/// </summary>
public enum EventStateEnum
{
    NotPublished = 0,
    InProgress = 1,
    Published = 2,
    PublishedFailed = 3,
}

/// <summary>
/// An integration event persisted in the producing service's own database, inside the same
/// transaction as the state change that caused it. This is what makes "changed state and told
/// the world" atomic.
/// </summary>
public class IntegrationEventLogEntry
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private IntegrationEventLogEntry()
    {
    }

    public IntegrationEventLogEntry(IntegrationEvent integrationEvent, Guid transactionId)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        EventId = integrationEvent.Id;
        EventTypeName = integrationEvent.GetType().FullName ?? integrationEvent.GetType().Name;
        CreationTime = integrationEvent.CreationDate;
        Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions);
        State = EventStateEnum.NotPublished;
        TimesSent = 0;
        TransactionId = transactionId;
    }

    public Guid EventId { get; private set; }

    public string EventTypeName { get; private set; } = string.Empty;

    /// <summary>Short type name, used to resolve the CLR type when replaying the outbox.</summary>
    [NotMapped]
    public string EventTypeShortName => EventTypeName[(EventTypeName.LastIndexOf('.') + 1)..];

    [NotMapped]
    public IntegrationEvent? IntegrationEvent { get; private set; }

    public EventStateEnum State { get; set; }

    public int TimesSent { get; set; }

    public DateTime CreationTime { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public Guid TransactionId { get; private set; }

    /// <summary>Rehydrates the stored payload once the CLR type is known.</summary>
    public IntegrationEventLogEntry DeserializeJsonContent(Type type)
    {
        IntegrationEvent = JsonSerializer.Deserialize(Content, type, SerializerOptions) as IntegrationEvent;
        return this;
    }
}
