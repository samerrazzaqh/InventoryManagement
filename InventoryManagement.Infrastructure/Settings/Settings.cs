using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Infrastructure.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; set; } = string.Empty;
    [Required] public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 key, >= 32 chars. NEVER commit it: user-secrets locally, Key Vault / env var in production.</summary>
    [Required, MinLength(32)] public string SigningKey { get; set; } = string.Empty;

    [Range(1, 60)] public int AccessTokenMinutes { get; set; } = 15;
    [Range(1, 90)] public int RefreshTokenDays { get; set; } = 7;
}

public sealed class CacheSettings
{
    public const string SectionName = "Cache";

    [Required] public string InstanceName { get; set; } = "inventory";
    [Range(1, 1440)] public int DefaultTtlMinutes { get; set; } = 5;
}

public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
}
