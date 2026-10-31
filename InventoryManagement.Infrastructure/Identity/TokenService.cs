using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManagement.Infrastructure.Identity;

internal sealed class TokenService(IOptions<JwtSettings> options, TimeProvider clock) : ITokenService
{
    private readonly JwtSettings _settings = options.Value;
    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.SigningKey)),
        SecurityAlgorithms.HmacSha256);
    private readonly JsonWebTokenHandler _handler = new();

    public AccessTokenResult CreateAccessToken(Guid userId, string email, string fullName, IEnumerable<string> roles)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Email, email),
            new("name", fullName)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = _credentials
        };

        return new AccessTokenResult(_handler.CreateToken(descriptor), expires, jti);
    }

    public RefreshTokenValue GenerateRefreshToken()
    {
        var raw = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));   // 512 bits of entropy
        return new RefreshTokenValue(raw, Hash(raw));
    }

    /// <summary>
    /// Plain SHA-256 is appropriate (and required for indexed lookup) because the input is already a
    /// high-entropy random value - unlike passwords, it cannot be brute-forced or dictionary-attacked.
    /// </summary>
    public string Hash(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
