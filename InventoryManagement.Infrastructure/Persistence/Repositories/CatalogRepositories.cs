using InventoryManagement.Application.Common;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(AppDbContext db) : Repository<Category>(db), ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => await Db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        return Db.Categories.AnyAsync(c => c.Name == normalized && (excludeId == null || c.Id != excludeId), ct);
    }

    public Task<bool> HasProductsAsync(Guid categoryId, CancellationToken ct = default)
        => Db.Products.AnyAsync(p => p.CategoryId == categoryId, ct);
}

internal sealed class ProductRepository(AppDbContext db) : Repository<Product>(db), IProductRepository
{
    public Task<Product?> GetDetailsAsync(Guid id, CancellationToken ct = default)
        => Db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return Db.Products.AnyAsync(p => p.Sku == normalized, ct);
    }

    public async Task<PagedResult<Product>> SearchAsync(ProductFilter f, CancellationToken ct = default)
    {
        var query = Db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Supplier).AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = f.Search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
        }
        if (f.CategoryId is { } categoryId) query = query.Where(p => p.CategoryId == categoryId);
        if (f.SupplierId is { } supplierId) query = query.Where(p => p.SupplierId == supplierId);
        if (f.IsActive is { } isActive) query = query.Where(p => p.IsActive == isActive);
        if (f.MinPrice is { } min) query = query.Where(p => p.UnitPrice >= min);
        if (f.MaxPrice is { } max) query = query.Where(p => p.UnitPrice <= max);

        var total = await query.CountAsync(ct);
        var items = await ApplySort(query, f.SortBy, f.Descending)
            .Skip(f.Skip).Take(f.EffectivePageSize)
            .ToListAsync(ct);

        return new PagedResult<Product>(items, f.EffectivePage, f.EffectivePageSize, total);
    }

    // Whitelisted sort columns only (never build ORDER BY from raw user input). Id is a stable tiebreaker for paging.
    private static IOrderedQueryable<Product> ApplySort(IQueryable<Product> q, string? sortBy, bool desc)
        => (sortBy?.ToLowerInvariant(), desc) switch
        {
            ("sku", false) => q.OrderBy(p => p.Sku).ThenBy(p => p.Id),
            ("sku", true) => q.OrderByDescending(p => p.Sku).ThenBy(p => p.Id),
            ("price", false) => q.OrderBy(p => p.UnitPrice).ThenBy(p => p.Id),
            ("price", true) => q.OrderByDescending(p => p.UnitPrice).ThenBy(p => p.Id),
            ("createdat", false) => q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            ("createdat", true) => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            (_, true) => q.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
            _ => q.OrderBy(p => p.Name).ThenBy(p => p.Id)
        };
}
