using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace InventoryManagement.Infrastructure.Caching;

/// <summary>
/// Immediate access-token revocation (logout). Entries live exactly as long as the token would have remained valid,
/// so the set never grows unbounded. Trade-off: if Redis is down the check fails OPEN (tokens are only 15 min long);
/// switch to fail-closed here if your threat model demands it.
/// </summary>
internal sealed class RedisTokenDenylist(IConnectionMultiplexer redis, IOptions<CacheSettings> options, ILogger<RedisTokenDenylist> logger)
    : ITokenDenylist
{
    private string Key(string jti) => $"{options.Value.InstanceName}:denylist:{jti}";

    public async Task DenyAsync(string jti, TimeSpan timeToLive, CancellationToken ct = default)
    {
        if (timeToLive <= TimeSpan.Zero) return;
        try { await redis.GetDatabase().StringSetAsync(Key(jti), "1", timeToLive); }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            logger.LogError(ex, "Could not denylist access token {Jti}.", jti);
        }
    }

    public async Task<bool> IsDeniedAsync(string jti, CancellationToken ct = default)
    {
        try { return await redis.GetDatabase().KeyExistsAsync(Key(jti)); }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            logger.LogError(ex, "Denylist check failed for {Jti}; failing open.", jti);
            return false;
        }
    }
}
