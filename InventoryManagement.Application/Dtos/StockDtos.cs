using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Application.Dtos;

/// <summary>Used for both stock-in and stock-out (the endpoint decides the direction).</summary>
public sealed record StockMovementRequest(Guid ProductId, Guid WarehouseId, int Quantity, string? ReferenceNumber, string? Notes);

public sealed record StockTransferRequest(Guid ProductId, Guid SourceWarehouseId, Guid DestinationWarehouseId,
    int Quantity, string? ReferenceNumber, string? Notes);

public sealed record StockTransactionDto(
    Guid Id, Guid ProductId, Guid? SourceWarehouseId, Guid? DestinationWarehouseId,
    int Quantity, TransactionType Type, string ReferenceNumber, string? Notes,
    string CreatedBy, DateTimeOffset CreatedAt);

public sealed record InventoryDto(Guid ProductId, string Sku, string ProductName,
    Guid WarehouseId, string WarehouseName, int Quantity, DateTimeOffset LastUpdated);

public sealed record LowStockItemDto(Guid ProductId, string Sku, string ProductName,
    Guid WarehouseId, string WarehouseName, int Quantity, int ReorderLevel, int Shortage);
