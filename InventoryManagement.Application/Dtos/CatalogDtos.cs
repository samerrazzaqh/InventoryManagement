using InventoryManagement.Application.Common;

namespace InventoryManagement.Application.Dtos;

public sealed record CategoryDto(Guid Id, string Name, string? Description, bool IsActive);
public sealed record CreateCategoryRequest(string Name, string? Description);
public sealed record UpdateCategoryRequest(string Name, string? Description, bool IsActive);

public sealed record ProductDto(
    Guid Id, string Sku, string Name, string? Description,
    Guid CategoryId, string CategoryName,
    Guid SupplierId, string SupplierName,
    decimal UnitPrice, int ReorderLevel, bool IsActive);

public sealed record CreateProductRequest(
    string Sku, string Name, string? Description,
    Guid CategoryId, Guid SupplierId, decimal UnitPrice, int ReorderLevel);

public sealed record UpdateProductRequest(
    string Name, string? Description,
    Guid CategoryId, Guid SupplierId, decimal UnitPrice, int ReorderLevel, bool IsActive);

public sealed record ProductFilter : PagedQuery
{
    public Guid? CategoryId { get; init; }
    public Guid? SupplierId { get; init; }
    public bool? IsActive { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
}
