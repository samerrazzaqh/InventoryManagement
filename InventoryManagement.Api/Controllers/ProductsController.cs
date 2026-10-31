using InventoryManagement.Api.Authorization;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[Authorize]
public sealed class ProductsController(IProductService products) : ApiControllerBase
{
    /// <summary>Paged catalog. Query: page, pageSize, search, sortBy (name|sku|price|createdAt), descending, categoryId, supplierId, isActive, minPrice, maxPrice.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> Search([FromQuery] ProductFilter filter, CancellationToken ct)
        => Ok(await products.SearchAsync(filter, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken ct)
        => Ok(await products.GetByIdAsync(id, ct));

    [HttpPost, Authorize(Policies.ManageCatalog)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var created = await products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}"), Authorize(Policies.ManageCatalog)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductRequest request, CancellationToken ct)
        => Ok(await products.UpdateAsync(id, request, ct));

    /// <summary>Soft delete (deactivates the product).</summary>
    [HttpDelete("{id:guid}"), Authorize(Policies.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await products.DeleteAsync(id, ct);
        return NoContent();
    }
}
