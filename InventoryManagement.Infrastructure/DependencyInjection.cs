using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Infrastructure.Caching;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Infrastructure.Persistence.Repositories;
using InventoryManagement.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace InventoryManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ---- Options: validated at startup so misconfiguration fails fast instead of at the first request
        services.AddOptions<JwtSettings>().Bind(config.GetSection(JwtSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<CacheSettings>().Bind(config.GetSection(CacheSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddSingleton(TimeProvider.System);

        // ---- SQL Server / EF Core
        var sqlConnection = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(sqlConnection, sql =>
        {
            sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
            sql.CommandTimeout(30);
            sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        }));

        // ---- Identity (core only: no cookies, no UI - this is a token-based API)
        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 12;
                o.Password.RequireDigit = o.Password.RequireLowercase = o.Password.RequireUppercase = o.Password.RequireNonAlphanumeric = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // ---- Redis
        var redisConnection = config.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisOptions = ConfigurationOptions.Parse(redisConnection);
            redisOptions.AbortOnConnectFail = false;     // start even if Redis is down; the multiplexer reconnects in the background
            redisOptions.ConnectRetry = 3;
            redisOptions.ConnectTimeout = 3000;
            redisOptions.AsyncTimeout = 1500;            // a slow cache must never be slower than the database
            return ConnectionMultiplexer.Connect(redisOptions);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<ITokenDenylist, RedisTokenDenylist>();

        // ---- Persistence abstractions
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        // ---- Auth
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddHostedService<RefreshTokenCleanupService>();

        return services;
    }
}
