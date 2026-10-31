using InventoryManagement.Application.Common;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence.Repositories;

internal sealed class InventoryRepository(AppDbContext db) : Repository<InventoryItem>(db), IInventoryRepository
{
    public Task<InventoryItem?> GetAsync(Guid warehouseId, Guid productId, CancellationToken ct = default)
        => Db.Inventory.FirstOrDefaultAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId, ct);

    public async Task<IReadOnlyList<InventoryDto>> GetByProductAsync(Guid productId, CancellationToken ct = default)
        => await Db.Inventory.AsNoTracking()
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.Warehouse.Name)
            .Select(i => new InventoryDto(i.ProductId, i.Product.Sku, i.Product.Name,
                i.WarehouseId, i.Warehouse.Name, i.Quantity, i.LastUpdated))
            .ToListAsync(ct);

    public async Task<PagedResult<LowStockItemDto>> GetLowStockAsync(Guid? warehouseId, PagedQuery query, CancellationToken ct = default)
    {
        var q = Db.Inventory.AsNoTracking()
            .Where(i => i.Product.IsActive && i.Quantity <= i.Product.ReorderLevel);

        if (warehouseId is { } wid) q = q.Where(i => i.WarehouseId == wid);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(i => i.Product.Name.Contains(term) || i.Product.Sku.Contains(term));
        }

        var total = await q.CountAsync(ct);

        // Most severe shortage first; Id as a stable tiebreaker.
        var items = await q
            .OrderByDescending(i => i.Product.ReorderLevel - i.Quantity)
            .ThenBy(i => i.Id)
            .Skip(query.Skip).Take(query.EffectivePageSize)
            .Select(i => new LowStockItemDto(i.ProductId, i.Product.Sku, i.Product.Name,
                i.WarehouseId, i.Warehouse.Name, i.Quantity, i.Product.ReorderLevel,
                i.Product.ReorderLevel - i.Quantity))
            .ToListAsync(ct);

        return new PagedResult<LowStockItemDto>(items, query.EffectivePage, query.EffectivePageSize, total);
    }
}
