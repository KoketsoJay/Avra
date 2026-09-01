using MediatR;

namespace OrderingService.Domain.SeedWork;

/// <summary>
/// Base class for domain entities. Identity — not property values — decides equality, and
/// entities collect the domain events they raise until the unit of work dispatches them.
/// </summary>
public abstract class Entity
{
    private int? _requestedHashCode;
    private int _id;
    private List<INotification>? _domainEvents;

    public virtual int Id
    {
        get => _id;
        protected set => _id = value;
    }

    public IReadOnlyCollection<INotification>? DomainEvents => _domainEvents?.AsReadOnly();

    public void AddDomainEvent(INotification eventItem)
    {
        _domainEvents ??= [];
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(INotification eventItem) => _domainEvents?.Remove(eventItem);

    public void ClearDomainEvents() => _domainEvents?.Clear();

    /// <summary>An entity is transient until the database has assigned it an identity.</summary>
    public bool IsTransient() => Id == default;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return !other.IsTransient() && !IsTransient() && other.Id == Id;
    }

    public override int GetHashCode()
    {
        if (IsTransient())
        {
            return base.GetHashCode();
        }

        // XOR keeps the hash stable and away from the raw identity value.
        _requestedHashCode ??= Id.GetHashCode() ^ 31;
        return _requestedHashCode.Value;
    }

    public static bool operator ==(Entity? left, Entity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
