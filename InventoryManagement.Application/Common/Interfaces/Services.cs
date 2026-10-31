using InventoryManagement.Application.Dtos;

namespace InventoryManagement.Application.Common.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> SearchAsync(ProductFilter filter, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    /// <summary>Soft delete: products are referenced by the stock ledger and can never be physically removed.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IStockService
{
    Task<StockTransactionDto> StockInAsync(StockMovementRequest request, CancellationToken ct = default);
    Task<StockTransactionDto> StockOutAsync(StockMovementRequest request, CancellationToken ct = default);
    Task<StockTransactionDto> TransferAsync(StockTransferRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryDto>> GetStockByProductAsync(Guid productId, CancellationToken ct = default);
    Task<PagedResult<LowStockItemDto>> GetLowStockAsync(Guid? warehouseId, PagedQuery query, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    /// <summary>Rotates the refresh token: the presented token is revoked and a new pair is issued.</summary>
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt, string Jti);

public sealed record RefreshTokenValue(string Raw, string Hash);

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(Guid userId, string email, string fullName, IEnumerable<string> roles);
    /// <summary>256+ bits of CSPRNG entropy. <c>Raw</c> goes to the client; only <c>Hash</c> is persisted.</summary>
    RefreshTokenValue GenerateRefreshToken();
    string Hash(string rawToken);
}
