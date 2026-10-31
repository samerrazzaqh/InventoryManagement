using System.Security.Claims;
using InventoryManagement.Application.Common.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;

namespace InventoryManagement.Api.Services;

internal sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
    public string? Email => User?.FindFirstValue(JwtRegisteredClaimNames.Email);
    public string? Jti => User?.FindFirstValue(JwtRegisteredClaimNames.Jti);
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public DateTimeOffset? TokenExpiresAt =>
        long.TryParse(User?.FindFirstValue(JwtRegisteredClaimNames.Exp), out var exp)
            ? DateTimeOffset.FromUnixTimeSeconds(exp)
            : null;
}
