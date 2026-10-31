using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence.Repositories;

internal class Repository<T>(AppDbContext db) : IRepository<T> where T : BaseEntity
{
    protected AppDbContext Db => db;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<T>().FindAsync([id], ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await db.Set<T>().AddAsync(entity, ct);

    public void Remove(T entity) => db.Set<T>().Remove(entity);
}
