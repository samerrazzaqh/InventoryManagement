using System.Text.Json;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace InventoryManagement.Infrastructure.Caching;

/// <summary>
/// Cache-aside on Redis with group versioning.
/// Key layout:   {instance}:{group}:v{N}:{key}      version counter:   {instance}:{group}:ver
/// Invalidating a group = INCR the counter. Old entries become unreachable immediately and expire by TTL,
/// so there is no SCAN/KEYS (which is O(N) and blocks Redis) and no partial-invalidation window.
/// Redis failures are logged and bypassed (fail-open): the database remains the source of truth.
/// </summary>
internal sealed class RedisCacheService(
    IConnectionMultiplexer redis,
    IOptions<CacheSettings> options,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly CacheSettings _settings = options.Value;

    public async Task<T> GetOrCreateAsync<T>(string group, string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null, CancellationToken ct = default)
    {
        IDatabase? db = null;
        string? cacheKey = null;

        try
        {
            db = redis.GetDatabase();
            var version = await GetVersionAsync(db, group);
            cacheKey = $"{_settings.InstanceName}:{group}:v{version}:{key}";

            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue)
                return JsonSerializer.Deserialize<T>((string)cached!, Json)!;
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException or JsonException)
        {
            logger.LogWarning(ex, "Cache read failed for {Group}/{Key}; serving from the database.", group, key);
            db = null;   // do not attempt the write-back either
        }

        var value = await factory(ct);

        if (db is not null && cacheKey is not null && value is not null)
        {
            try
            {
                var expiry = WithJitter(ttl ?? TimeSpan.FromMinutes(_settings.DefaultTtlMinutes));
                await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(value, Json), expiry);
            }
            catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
            {
                logger.LogWarning(ex, "Cache write failed for {Group}/{Key}.", group, key);
            }
        }

        return value;
    }

    public async Task InvalidateGroupAsync(string group, CancellationToken ct = default)
    {
        try
        {
            await redis.GetDatabase().StringIncrementAsync(VersionKey(group));
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            // The write already succeeded; stale reads are bounded by the TTL. Alert on this log line.
            logger.LogError(ex, "Cache invalidation FAILED for group {Group}; entries may be stale until TTL expiry.", group);
        }
    }

    private string VersionKey(string group) => $"{_settings.InstanceName}:{group}:ver";

    private async Task<long> GetVersionAsync(IDatabase db, string group)
    {
        var value = await db.StringGetAsync(VersionKey(group));
        return value.HasValue && value.TryParse(out long version) ? version : 0;
    }

    /// <summary>+0-10% random TTL so entries created together do not all expire (and stampede the DB) together.</summary>
    private static TimeSpan WithJitter(TimeSpan ttl) => TimeSpan.FromSeconds(ttl.TotalSeconds * (1 + Random.Shared.NextDouble() * 0.1));
}
