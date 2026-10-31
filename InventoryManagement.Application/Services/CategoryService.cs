using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Exceptions;
using MapsterMapper;

namespace InventoryManagement.Application.Services;

public sealed class CategoryService(ICategoryRepository categories, IUnitOfWork uow, IMapper mapper) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await categories.GetAllAsync(ct);
        return all.Select(c => mapper.Map<CategoryDto>(c)).ToList();
    }

    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        => mapper.Map<CategoryDto>(await categories.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Category), id));

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (await categories.NameExistsAsync(request.Name, null, ct))
            throw new ConflictException($"A category named '{request.Name}' already exists.");

        var category = Category.Create(request.Name, request.Description);
        await categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Category), id);

        if (await categories.NameExistsAsync(request.Name, id, ct))
            throw new ConflictException($"A category named '{request.Name}' already exists.");

        category.Update(request.Name, request.Description, request.IsActive);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CategoryDto>(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await categories.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Category), id);

        if (await categories.HasProductsAsync(id, ct))
            throw new ConflictException("The category still contains products. Deactivate it instead of deleting.");

        categories.Remove(category);
        await uow.SaveChangesAsync(ct);
    }
}
