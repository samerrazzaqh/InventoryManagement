using InventoryManagement.Api.Authorization;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[Authorize]
public sealed class CategoriesController(ICategoryService categories) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken ct)
        => Ok(await categories.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Get(Guid id, CancellationToken ct)
        => Ok(await categories.GetByIdAsync(id, ct));

    [HttpPost, Authorize(Policies.ManageCatalog)]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var created = await categories.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}"), Authorize(Policies.ManageCatalog)]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryRequest request, CancellationToken ct)
        => Ok(await categories.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}"), Authorize(Policies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
