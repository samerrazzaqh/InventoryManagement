using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Events;

public sealed record LowStockDetectedEvent(Guid ProductId, Guid WarehouseId, int Quantity, int ReorderLevel) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

public sealed record StockMovedEvent(Guid TransactionId, Guid ProductId, TransactionType Type, int Quantity) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
