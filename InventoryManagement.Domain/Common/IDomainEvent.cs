namespace InventoryManagement.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
