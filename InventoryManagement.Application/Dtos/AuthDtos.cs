namespace InventoryManagement.Application.Dtos;

public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);

public sealed record UserInfo(Guid Id, string Email, string FullName, IReadOnlyList<string> Roles);

public sealed record AuthResponse(
    string AccessToken, DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken, DateTimeOffset RefreshTokenExpiresAt,
    UserInfo User);
