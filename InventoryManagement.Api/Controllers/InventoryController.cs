using InventoryManagement.Api.Authorization;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[Authorize]
public sealed class InventoryController(IStockService stock) : ApiControllerBase
{
    [HttpPost("stock-in"), Authorize(Policies.MoveStock)]
    public async Task<ActionResult<StockTransactionDto>> StockIn(StockMovementRequest request, CancellationToken ct)
        => Ok(await stock.StockInAsync(request, ct));

    [HttpPost("stock-out"), Authorize(Policies.MoveStock)]
    public async Task<ActionResult<StockTransactionDto>> StockOut(StockMovementRequest request, CancellationToken ct)
        => Ok(await stock.StockOutAsync(request, ct));

    [HttpPost("transfer"), Authorize(Policies.ManageCatalog)]
    public async Task<ActionResult<StockTransactionDto>> Transfer(StockTransferRequest request, CancellationToken ct)
        => Ok(await stock.TransferAsync(request, ct));

    [HttpGet("products/{productId:guid}")]
    public async Task<ActionResult<IReadOnlyList<InventoryDto>>> GetStock(Guid productId, CancellationToken ct)
        => Ok(await stock.GetStockByProductAsync(productId, ct));

    /// <summary>Items at or below their product's reorder level, worst shortage first. Optional warehouse filter.</summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<PagedResult<LowStockItemDto>>> LowStock(
        [FromQuery] Guid? warehouseId, [FromQuery] PagedQuery query, CancellationToken ct)
        => Ok(await stock.GetLowStockAsync(warehouseId, query, ct));
}
