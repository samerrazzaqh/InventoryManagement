using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InventoryManagement.Api.Controllers;

[EnableRateLimiting("auth")]
public sealed class AuthController(IAuthService auth) : ApiControllerBase
{
    [HttpPost("register"), AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
        => Ok(await auth.RegisterAsync(request, ct));

    [HttpPost("login"), AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
        => Ok(await auth.LoginAsync(request, ct));

    /// <summary>Exchanges a valid refresh token for a NEW access + refresh pair. The old refresh token is invalidated.</summary>
    [HttpPost("refresh"), AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
        => Ok(await auth.RefreshAsync(request, ct));

    /// <summary>Revokes the whole session (all refresh tokens of the family) and denylists the current access token.</summary>
    [HttpPost("logout"), Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request, ct);
        return NoContent();
    }
}
