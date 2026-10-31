using InventoryManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Identity;

/// <summary>
/// Deletes tokens that expired or were revoked more than 30 days ago. The grace period keeps recently revoked
/// tokens around so that reuse detection still works for stolen-token replays.
/// </summary>
internal sealed class RefreshTokenCleanupService(IServiceScopeFactory scopes, TimeProvider clock, ILogger<RefreshTokenCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var cutoff = clock.GetUtcNow() - Retention;

                var removed = await db.RefreshTokens
                    .Where(t => t.ExpiresOn < cutoff || (t.RevokedOn != null && t.RevokedOn < cutoff))
                    .ExecuteDeleteAsync(stoppingToken);

                if (removed > 0) logger.LogInformation("Purged {Count} stale refresh tokens.", removed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Refresh-token cleanup failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
