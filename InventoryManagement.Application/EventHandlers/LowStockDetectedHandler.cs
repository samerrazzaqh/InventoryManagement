using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Events;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.EventHandlers;

/// <summary>
/// Extension point for the low-stock "alert mechanism" (e-mail, Teams/Slack webhook, outbox message, SignalR push...).
/// Handlers run after the stock transaction has been persisted and must never fail the request.
/// </summary>
public sealed class LowStockDetectedHandler(ILogger<LowStockDetectedHandler> logger) : IDomainEventHandler<LowStockDetectedEvent>
{
    public Task HandleAsync(LowStockDetectedEvent e, CancellationToken ct)
    {
        logger.LogWarning("LOW STOCK: product {ProductId} in warehouse {WarehouseId} is at {Quantity} (reorder level {ReorderLevel}).",
            e.ProductId, e.WarehouseId, e.Quantity, e.ReorderLevel);
        return Task.CompletedTask;
    }
}
