using InventoryManagement.Application.Common;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;

namespace InventoryManagement.Application.Caching;

public sealed class CachedProductService(IProductService inner, ICacheService cache) : IProductService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public Task<PagedResult<ProductDto>> SearchAsync(ProductFilter f, CancellationToken ct = default)
    {
        var key = CacheKey.Hash("list", f.EffectivePage, f.EffectivePageSize, f.Search, f.SortBy, f.Descending,
            f.CategoryId, f.SupplierId, f.IsActive, f.MinPrice, f.MaxPrice);

        return cache.GetOrCreateAsync(CacheGroups.Products, key, token => inner.SearchAsync(f, token), Ttl, ct);
    }

    // Not-found throws inside the factory, so misses are never cached (no negative caching).
    public Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        => cache.GetOrCreateAsync(CacheGroups.Products, $"id:{id}", token => inner.GetByIdAsync(id, token), Ttl, ct);

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var result = await inner.CreateAsync(request, ct);
        await cache.InvalidateGroupAsync(CacheGroups.Products, ct);
        return result;
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var result = await inner.UpdateAsync(id, request, ct);
        await cache.InvalidateGroupAsync(CacheGroups.Products, ct);
        return result;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await cache.InvalidateGroupAsync(CacheGroups.Products, ct);
    }
}
