namespace InventoryManagement.Domain.Common;

public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Time-ordered GUID (v7): globally unique AND index-friendly for clustered keys.</summary>
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    public void MarkCreated(DateTimeOffset at, string by) { CreatedAt = at; CreatedBy = by; }

    public void MarkUpdated(DateTimeOffset at, string by) { UpdatedAt = at; UpdatedBy = by; }
}
