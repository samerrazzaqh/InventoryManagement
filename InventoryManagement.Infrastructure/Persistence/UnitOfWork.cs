using System.Data;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    private const int MaxConcurrencyAttempts = 3;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("The data was modified by another request. Please retry.", ex);
        }
        // 2601 = duplicate key in unique index, 2627 = unique/PK constraint violation
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("A record with the same unique value already exists.", ex);
        }
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        // EnableRetryOnFailure forbids user-initiated transactions outside an execution strategy,
        // so the whole unit (transaction + delegate) is handed to the strategy, which may re-run it on transient faults.
        var strategy = db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async token =>
        {
            for (var attempt = 1; ; attempt++)
            {
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
                try
                {
                    var result = await operation(token);
                    await tx.CommitAsync(token);
                    return result;
                }
                catch (ConcurrencyException) when (attempt < MaxConcurrencyAttempts)
                {
                    await tx.RollbackAsync(token);
                    db.ChangeTracker.Clear();   // drop stale entities so the next attempt re-reads fresh rows
                }
            }
        }, ct);
    }
}
