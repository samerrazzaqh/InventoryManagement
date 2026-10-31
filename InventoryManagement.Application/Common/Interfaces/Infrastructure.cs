using InventoryManagement.Domain.Common;

namespace InventoryManagement.Application.Common.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// Cache-aside read. If the cache is unavailable the factory result is returned directly (fail-open),
    /// so a Redis outage degrades performance but never availability.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string group, string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>O(1) invalidation of every key in a group (version bump - no SCAN/KEYS).</summary>
    Task InvalidateGroupAsync(string group, CancellationToken ct = default);
}

public interface ITokenDenylist
{
    Task DenyAsync(string jti, TimeSpan timeToLive, CancellationToken ct = default);
    Task<bool> IsDeniedAsync(string jti, CancellationToken ct = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? IpAddress { get; }
    /// <summary>"jti" and expiry of the access token used for the current request (for logout denylisting).</summary>
    string? Jti { get; }
    DateTimeOffset? TokenExpiresAt { get; }
}

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="operation"/> inside one atomic database transaction, wrapped in the provider's
    /// retry strategy, with automatic retry on optimistic-concurrency conflicts. The delegate must be re-runnable.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
}
