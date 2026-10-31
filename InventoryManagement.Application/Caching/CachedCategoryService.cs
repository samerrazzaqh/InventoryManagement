using InventoryManagement.Application.Common;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;

namespace InventoryManagement.Application.Caching;

/// <summary>
/// Decorator (SRP/OCP): the real <c>CategoryService</c> knows nothing about caching.
/// Reads are cache-aside; every write bumps the group version, which atomically invalidates all cached entries.
/// A category rename changes <c>ProductDto.CategoryName</c>, so the products group is invalidated as well.
/// </summary>
public sealed class CachedCategoryService(ICategoryService inner, ICacheService cache) : ICategoryService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    public Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
        => cache.GetOrCreateAsync(CacheGroups.Categories, "all", token => inner.GetAllAsync(token), Ttl, ct);

    public Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        => cache.GetOrCreateAsync(CacheGroups.Categories, $"id:{id}", token => inner.GetByIdAsync(id, token), Ttl, ct);

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var result = await inner.CreateAsync(request, ct);
        await InvalidateAsync(ct);
        return result;
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var result = await inner.UpdateAsync(id, request, ct);
        await InvalidateAsync(ct);
        return result;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await inner.DeleteAsync(id, ct);
        await InvalidateAsync(ct);
    }

    private async Task InvalidateAsync(CancellationToken ct)
    {
        await cache.InvalidateGroupAsync(CacheGroups.Categories, ct);
        await cache.InvalidateGroupAsync(CacheGroups.Products, ct);
    }
}
