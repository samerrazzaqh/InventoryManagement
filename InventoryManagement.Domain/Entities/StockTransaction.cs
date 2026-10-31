using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Events;
using InventoryManagement.Domain.Exceptions;

namespace InventoryManagement.Domain.Entities;

/// <summary>Immutable, append-only ledger entry. Never updated or deleted.</summary>
public sealed class StockTransaction : BaseEntity
{
    private StockTransaction() { }

    public Guid ProductId { get; private set; }
    public Guid? SourceWarehouseId { get; private set; }
    public Guid? DestinationWarehouseId { get; private set; }
    public int Quantity { get; private set; }
    public TransactionType Type { get; private set; }
    public string ReferenceNumber { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public Product Product { get; private set; } = null!;
    public Warehouse? SourceWarehouse { get; private set; }
    public Warehouse? DestinationWarehouse { get; private set; }

    public static StockTransaction StockIn(Guid productId, Guid warehouseId, int quantity,
        string reference, string? notes, string createdBy, DateTimeOffset now)
        => Build(TransactionType.In, productId, null, warehouseId, quantity, reference, notes, createdBy, now);

    public static StockTransaction StockOut(Guid productId, Guid warehouseId, int quantity,
        string reference, string? notes, string createdBy, DateTimeOffset now)
        => Build(TransactionType.Out, productId, warehouseId, null, quantity, reference, notes, createdBy, now);

    public static StockTransaction Transfer(Guid productId, Guid sourceWarehouseId, Guid destinationWarehouseId,
        int quantity, string reference, string? notes, string createdBy, DateTimeOffset now)
    {
        if (sourceWarehouseId == destinationWarehouseId)
            throw new DomainException("Source and destination warehouses must be different.");

        return Build(TransactionType.Transfer, productId, sourceWarehouseId, destinationWarehouseId,
            quantity, reference, notes, createdBy, now);
    }

    private static StockTransaction Build(TransactionType type, Guid productId, Guid? source, Guid? destination,
        int quantity, string reference, string? notes, string createdBy, DateTimeOffset now)
    {
        var tx = new StockTransaction
        {
            Type = type,
            ProductId = Guard.NotEmpty(productId, nameof(productId)),
            SourceWarehouseId = source,
            DestinationWarehouseId = destination,
            Quantity = Guard.Positive(quantity, nameof(quantity)),
            ReferenceNumber = Guard.NotBlank(reference, nameof(ReferenceNumber), 50),
            Notes = Guard.Optional(notes, nameof(Notes), 500),
            CreatedBy = createdBy,
            CreatedAt = now
        };
        tx.Raise(new StockMovedEvent(tx.Id, productId, type, quantity));
        return tx;
    }
}
