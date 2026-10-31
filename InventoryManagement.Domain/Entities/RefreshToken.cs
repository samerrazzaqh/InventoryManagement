using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// Only the SHA-256 hash of the token is stored; a database leak therefore cannot be replayed.
/// Tokens belong to a "family" (one login session). Rotation links tokens inside a family so that
/// presenting an already-used token can be detected as theft and revokes the whole family.
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public Guid FamilyId { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }
    public DateTimeOffset ExpiresOn { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTimeOffset? RevokedOn { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public bool IsRevoked => RevokedOn is not null;
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresOn;
    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    public static RefreshToken Create(Guid userId, string tokenHash, Guid familyId,
        DateTimeOffset now, DateTimeOffset expiresOn, string? ip) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        FamilyId = familyId,
        CreatedOn = now,
        ExpiresOn = expiresOn,
        CreatedByIp = ip
    };

    public void Revoke(DateTimeOffset now, string? ip, string reason, string? replacedByTokenHash = null)
    {
        if (IsRevoked) return;
        RevokedOn = now;
        RevokedByIp = ip;
        RevokedReason = reason;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
