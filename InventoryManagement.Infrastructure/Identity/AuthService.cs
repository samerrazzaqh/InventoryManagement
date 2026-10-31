using FluentValidation;
using FluentValidation.Results;
using InventoryManagement.Application.Common;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Dtos;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Exceptions;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Infrastructure.Identity;

internal sealed class AuthService(
    UserManager<ApplicationUser> users,
    AppDbContext db,
    ITokenService tokens,
    ITokenDenylist denylist,
    ICurrentUserService currentUser,
    IOptions<JwtSettings> jwtOptions,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password.";
    private const string InvalidRefreshToken = "Invalid or expired refresh token.";

    private readonly JwtSettings _jwt = jwtOptions.Value;

    // ------------------------------------------------------------------ register
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = clock.GetUtcNow()
        };

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            throw new ValidationException(created.Errors.Select(e => new ValidationFailure(e.Code, e.Description)));

        // Self-registration is always least-privilege. Admins promote users through a dedicated admin endpoint.
        await users.AddToRoleAsync(user, Roles.Staff);

        var (response, _) = await CreateSessionAsync(user, familyId: Guid.CreateVersion7(), ct);
        await db.SaveChangesAsync(ct);
        return response;
    }

    // ------------------------------------------------------------------ login
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());

        // Identical message for "unknown user" and "wrong password" - no account enumeration.
        if (user is null || !user.IsActive)
            throw new AuthenticationFailedException(InvalidCredentials);

        if (await users.IsLockedOutAsync(user))
            throw new AuthenticationFailedException("Account temporarily locked due to repeated failed sign-ins.");

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);      // counts toward lockout
            throw new AuthenticationFailedException(InvalidCredentials);
        }

        await users.ResetAccessFailedCountAsync(user);

        var (response, _) = await CreateSessionAsync(user, familyId: Guid.CreateVersion7(), ct);
        await db.SaveChangesAsync(ct);
        return response;
    }

    // ------------------------------------------------------------------ refresh (rotation + reuse detection)
    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var hash = tokens.Hash(request.RefreshToken);

        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new AuthenticationFailedException(InvalidRefreshToken);

        if (stored.IsRevoked)
        {
            // A rotated/revoked token is being replayed: either the legitimate client or an attacker holds a stale copy.
            // We cannot tell which, so the whole session (family) is terminated and the user must sign in again.
            logger.LogWarning("Refresh-token reuse detected for user {UserId}, family {FamilyId}. Revoking family.",
                stored.UserId, stored.FamilyId);
            await RevokeFamilyAsync(stored.FamilyId, "Reuse detected", now, ct);
            throw new AuthenticationFailedException(InvalidRefreshToken);
        }

        if (stored.IsExpired(now))
            throw new AuthenticationFailedException(InvalidRefreshToken);

        var user = await users.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user))
        {
            await RevokeFamilyAsync(stored.FamilyId, "User disabled or locked", now, ct);
            throw new AuthenticationFailedException(InvalidRefreshToken);
        }

        // Rotation: revoke the presented token and issue its successor in the SAME family, in ONE SaveChanges (atomic).
        var (response, newHash) = await CreateSessionAsync(user, stored.FamilyId, ct);
        stored.Revoke(now, currentUser.IpAddress, "Rotated", replacedByTokenHash: newHash);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Two requests raced to rotate the same token (rowversion mismatch): only one may win.
            throw new AuthenticationFailedException(InvalidRefreshToken);
        }

        return response;
    }

    // ------------------------------------------------------------------ logout
    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var hash = tokens.Hash(request.RefreshToken);

        var stored = await db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        // Only the owner may end a session; silently ignore unknown tokens (idempotent logout).
        if (stored is not null && stored.UserId == currentUser.UserId)
            await RevokeFamilyAsync(stored.FamilyId, "Logout", now, ct);

        // Kill the current access token immediately instead of waiting up to 15 minutes for it to expire.
        if (currentUser.Jti is { } jti && currentUser.TokenExpiresAt is { } expiresAt)
            await denylist.DenyAsync(jti, expiresAt - now, ct);
    }

    // ------------------------------------------------------------------ helpers
    /// <summary>Builds tokens and stages the new refresh-token row (NOT saved - the caller commits atomically).</summary>
    private async Task<(AuthResponse Response, string RefreshHash)> CreateSessionAsync(
        ApplicationUser user, Guid familyId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var roles = (await users.GetRolesAsync(user)).ToList();

        var access = tokens.CreateAccessToken(user.Id, user.Email!, user.FullName, roles);
        var refresh = tokens.GenerateRefreshToken();
        var refreshExpires = now.AddDays(_jwt.RefreshTokenDays);

        await db.RefreshTokens.AddAsync(
            RefreshToken.Create(user.Id, refresh.Hash, familyId, now, refreshExpires, currentUser.IpAddress), ct);

        var response = new AuthResponse(access.Token, access.ExpiresAt, refresh.Raw, refreshExpires,
            new UserInfo(user.Id, user.Email!, user.FullName, roles));

        return (response, refresh.Hash);
    }

    private Task<int> RevokeFamilyAsync(Guid familyId, string reason, DateTimeOffset now, CancellationToken ct)
        => db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedOn == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedOn, (DateTimeOffset?)now)
                .SetProperty(t => t.RevokedReason, reason)
                .SetProperty(t => t.RevokedByIp, currentUser.IpAddress), ct);
}
