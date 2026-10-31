using InventoryManagement.Application.Dtos;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Application.Common.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    /// <summary>Returns the TRACKED entity (use for updates).</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default);
    Task<bool> HasProductsAsync(Guid categoryId, CancellationToken ct = default);
}

public interface IProductRepository : IRepository<Product>
{
    /// <summary>Read-only projection including Category and Supplier.</summary>
    Task<Product?> GetDetailsAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Product>> SearchAsync(ProductFilter filter, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
}

public interface IInventoryRepository : IRepository<InventoryItem>
{
    Task<InventoryItem?> GetAsync(Guid warehouseId, Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryDto>> GetByProductAsync(Guid productId, CancellationToken ct = default);
    Task<PagedResult<LowStockItemDto>> GetLowStockAsync(Guid? warehouseId, PagedQuery query, CancellationToken ct = default);
}
