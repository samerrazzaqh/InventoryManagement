using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Events;
using InventoryManagement.Domain.Exceptions;

namespace InventoryManagement.Domain.Entities;

/// <summary>Stock level of one product in one warehouse. Guarded by a rowversion for optimistic concurrency.</summary>
public sealed class InventoryItem : BaseEntity
{
    private InventoryItem() { }

    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public DateTimeOffset LastUpdated { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public Warehouse Warehouse { get; private set; } = null!;
    public Product Product { get; private set; } = null!;

    public static InventoryItem Create(Guid warehouseId, Guid productId, DateTimeOffset now) => new()
    {
        WarehouseId = warehouseId,
        ProductId = productId,
        Quantity = 0,
        LastUpdated = now
    };

    public void Increase(int quantity, DateTimeOffset now)
    {
        Guard.Positive(quantity, nameof(quantity));
        Quantity = checked(Quantity + quantity);
        LastUpdated = now;
    }

    public void Decrease(int quantity, int reorderLevel, DateTimeOffset now)
    {
        Guard.Positive(quantity, nameof(quantity));
        if (quantity > Quantity)
            throw new InsufficientStockException(ProductId, WarehouseId, Quantity, quantity);

        Quantity -= quantity;
        LastUpdated = now;

        if (Quantity <= reorderLevel)
            Raise(new LowStockDetectedEvent(ProductId, WarehouseId, Quantity, reorderLevel));
    }
}
