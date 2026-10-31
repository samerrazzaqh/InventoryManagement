using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserService currentUser,
    TimeProvider clock,
    IDomainEventDispatcher dispatcher)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Friendlier Identity table names.
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            b.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        });

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Money is always decimal(18,2) unless a configuration says otherwise.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var actor = currentUser.UserId?.ToString() ?? "system";

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.MarkCreated(now, actor);
            else if (entry.State == EntityState.Modified) entry.Entity.MarkUpdated(now, actor);
        }

        // Harvest events before saving, publish only after the data is durable.
        var events = ChangeTracker.Entries<BaseEntity>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()) entry.Entity.ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (events.Count > 0)
            await dispatcher.DispatchAsync(events, cancellationToken);

        return result;
    }
}
