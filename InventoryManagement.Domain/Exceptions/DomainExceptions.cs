namespace InventoryManagement.Domain.Exceptions;

/// <summary>Base type for every business-rule violation. Mapped to HTTP 422 by the API layer.</summary>
public class DomainException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class NotFoundException(string entity, object key)
    : DomainException($"{entity} with identifier '{key}' was not found.");

/// <summary>Mapped to HTTP 409.</summary>
public class ConflictException(string message, Exception? inner = null) : DomainException(message, inner);

/// <summary>Optimistic-concurrency failure (row changed by someone else). Mapped to HTTP 409.</summary>
public sealed class ConcurrencyException(string message, Exception? inner = null) : ConflictException(message, inner);

public sealed class InsufficientStockException(Guid productId, Guid warehouseId, int available, int requested)
    : DomainException($"Insufficient stock for product {productId} in warehouse {warehouseId}: available {available}, requested {requested}.")
{
    public Guid ProductId { get; } = productId;
    public Guid WarehouseId { get; } = warehouseId;
    public int Available { get; } = available;
    public int Requested { get; } = requested;
}

/// <summary>Bad credentials / invalid or reused refresh token. Mapped to HTTP 401.</summary>
public sealed class AuthenticationFailedException(string message) : DomainException(message);
