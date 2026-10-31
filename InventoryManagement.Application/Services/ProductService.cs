using InventoryManagement.Application.Common;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Exceptions;
using MapsterMapper;

namespace InventoryManagement.Application.Services;

public sealed class ProductService(
    IProductRepository products,
    IRepository<Category> categories,
    IRepository<Supplier> suppliers,
    IUnitOfWork uow,
    IMapper mapper) : IProductService
{
    public async Task<PagedResult<ProductDto>> SearchAsync(ProductFilter filter, CancellationToken ct = default)
    {
        var page = await products.SearchAsync(filter, ct);
        return page.Map(p => mapper.Map<ProductDto>(p));
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await products.GetDetailsAsync(id, ct) ?? throw new NotFoundException(nameof(Product), id);
        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        if (await products.SkuExistsAsync(request.Sku, ct))
            throw new ConflictException($"SKU '{request.Sku.Trim().ToUpperInvariant()}' already exists.");

        await EnsureReferencesExistAsync(request.CategoryId, request.SupplierId, ct);

        var product = Product.Create(request.Sku, request.Name, request.Description,
            request.CategoryId, request.SupplierId, request.UnitPrice, request.ReorderLevel);

        await products.AddAsync(product, ct);
        await uow.SaveChangesAsync(ct);          // unique index on Sku is the final guard against races
        return await GetByIdAsync(product.Id, ct);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Product), id);
        await EnsureReferencesExistAsync(request.CategoryId, request.SupplierId, ct);

        product.Update(request.Name, request.Description, request.CategoryId, request.SupplierId,
            request.UnitPrice, request.ReorderLevel, request.IsActive);

        await uow.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await products.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Product), id);
        product.Deactivate();
        await uow.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesExistAsync(Guid categoryId, Guid supplierId, CancellationToken ct)
    {
        if (await categories.GetByIdAsync(categoryId, ct) is null) throw new NotFoundException(nameof(Category), categoryId);
        if (await suppliers.GetByIdAsync(supplierId, ct) is null) throw new NotFoundException(nameof(Supplier), supplierId);
    }
}
