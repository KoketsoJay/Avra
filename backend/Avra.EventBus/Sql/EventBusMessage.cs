namespace Avra.EventBus.Sql;

/// <summary>
/// A published integration event as it sits in the broker table.
/// </summary>
public class EventBusMessage
{
    /// <summary>Monotonic identity column. Consumers process strictly in this order.</summary>
    public long Sequence { get; set; }

    public Guid MessageId { get; set; }

    /// <summary>Wire name of the event, matched against a subscriber's registered types.</summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>JSON payload of the event.</summary>
    public string Content { get; set; } = string.Empty;

    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Per-subscriber delivery record. Its unique index is what makes claiming a message atomic
/// and delivery idempotent across competing instances.
/// </summary>
public class EventBusDelivery
{
    public long Id { get; set; }

    public long MessageSequence { get; set; }

    public string Subscriber { get; set; } = string.Empty;

    public DeliveryState State { get; set; } = DeliveryState.InProgress;

    public int Attempts { get; set; }

    public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? Error { get; set; }
}

/// <summary>Lifecycle of a single message/subscriber pair.</summary>
public enum DeliveryState
{
    InProgress = 0,
    Completed = 1,
    Failed = 2,
    Poisoned = 3,
}
