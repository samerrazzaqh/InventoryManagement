using InventoryManagement.Application.Common;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Exceptions;
using MapsterMapper;

namespace InventoryManagement.Application.Services;

/// <summary>
/// Every movement = (inventory row change(s) + one immutable ledger entry) committed in ONE database transaction.
/// Concurrent updates to the same stock row are detected by the rowversion token; the whole unit of work is
/// re-run (re-reading fresh state) up to a few times before the conflict surfaces as HTTP 409.
/// </summary>
public sealed class StockService(
    IInventoryRepository inventory,
    IRepository<StockTransaction> ledger,
    IRepository<Product> products,
    IRepository<Warehouse> warehouses,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    TimeProvider clock,
    IMapper mapper) : IStockService
{
    private string Actor => currentUser.UserId?.ToString() ?? "system";

    public Task<StockTransactionDto> StockInAsync(StockMovementRequest request, CancellationToken ct = default) =>
        uow.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.GetUtcNow();
            await GetProductAsync(request.ProductId, requireActive: true, token);
            await EnsureWarehouseAsync(request.WarehouseId, token);

            var item = await GetOrCreateItemAsync(request.WarehouseId, request.ProductId, now, token);
            item.Increase(request.Quantity, now);

            var tx = StockTransaction.StockIn(request.ProductId, request.WarehouseId, request.Quantity,
                Reference(request.ReferenceNumber, "IN", now), request.Notes, Actor, now);

            return await CommitAsync(tx, token);
        }, ct);

    public Task<StockTransactionDto> StockOutAsync(StockMovementRequest request, CancellationToken ct = default) =>
        uow.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.GetUtcNow();
            var product = await GetProductAsync(request.ProductId, requireActive: false, token);
            await EnsureWarehouseAsync(request.WarehouseId, token);

            var item = await inventory.GetAsync(request.WarehouseId, request.ProductId, token)
                ?? throw new InsufficientStockException(request.ProductId, request.WarehouseId, 0, request.Quantity);
            item.Decrease(request.Quantity, product.ReorderLevel, now);

            var tx = StockTransaction.StockOut(request.ProductId, request.WarehouseId, request.Quantity,
                Reference(request.ReferenceNumber, "OUT", now), request.Notes, Actor, now);

            return await CommitAsync(tx, token);
        }, ct);

    public Task<StockTransactionDto> TransferAsync(StockTransferRequest request, CancellationToken ct = default) =>
        uow.ExecuteInTransactionAsync(async token =>
        {
            if (request.SourceWarehouseId == request.DestinationWarehouseId)
                throw new DomainException("Source and destination warehouses must be different.");

            var now = clock.GetUtcNow();
            var product = await GetProductAsync(request.ProductId, requireActive: true, token);
            await EnsureWarehouseAsync(request.SourceWarehouseId, token);
            await EnsureWarehouseAsync(request.DestinationWarehouseId, token);

            var source = await inventory.GetAsync(request.SourceWarehouseId, request.ProductId, token)
                ?? throw new InsufficientStockException(request.ProductId, request.SourceWarehouseId, 0, request.Quantity);
            var destination = await GetOrCreateItemAsync(request.DestinationWarehouseId, request.ProductId, now, token);

            source.Decrease(request.Quantity, product.ReorderLevel, now);
            destination.Increase(request.Quantity, now);

            var tx = StockTransaction.Transfer(request.ProductId, request.SourceWarehouseId, request.DestinationWarehouseId,
                request.Quantity, Reference(request.ReferenceNumber, "TRF", now), request.Notes, Actor, now);

            return await CommitAsync(tx, token);
        }, ct);

    public Task<IReadOnlyList<InventoryDto>> GetStockByProductAsync(Guid productId, CancellationToken ct = default)
        => inventory.GetByProductAsync(productId, ct);

    public Task<PagedResult<LowStockItemDto>> GetLowStockAsync(Guid? warehouseId, PagedQuery query, CancellationToken ct = default)
        => inventory.GetLowStockAsync(warehouseId, query, ct);

    // ---------------------------------------------------------------- helpers

    private async Task<StockTransactionDto> CommitAsync(StockTransaction tx, CancellationToken ct)
    {
        await ledger.AddAsync(tx, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<StockTransactionDto>(tx);
    }

    private async Task<Product> GetProductAsync(Guid id, bool requireActive, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Product), id);
        if (requireActive && !product.IsActive)
            throw new DomainException($"Product '{product.Sku}' is inactive and cannot receive stock.");
        return product;
    }

    private async Task EnsureWarehouseAsync(Guid id, CancellationToken ct)
    {
        var warehouse = await warehouses.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Warehouse), id);
        if (!warehouse.IsActive) throw new DomainException($"Warehouse '{warehouse.Name}' is inactive.");
    }

    private async Task<InventoryItem> GetOrCreateItemAsync(Guid warehouseId, Guid productId, DateTimeOffset now, CancellationToken ct)
    {
        var item = await inventory.GetAsync(warehouseId, productId, ct);
        if (item is not null) return item;

        item = InventoryItem.Create(warehouseId, productId, now);
        await inventory.AddAsync(item, ct);
        return item;
    }

    private static string Reference(string? supplied, string prefix, DateTimeOffset now)
        => string.IsNullOrWhiteSpace(supplied)
            ? $"{prefix}-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
            : supplied.Trim();
}
