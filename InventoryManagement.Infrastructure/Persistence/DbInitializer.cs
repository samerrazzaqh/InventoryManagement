using InventoryManagement.Application.Common;
using InventoryManagement.Infrastructure.Identity;
using InventoryManagement.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Persistence;

public static class DbInitializer
{
    /// <summary>
    /// Always ensures roles exist. Optionally applies migrations (use only in development; in production run
    /// `dotnet ef migrations bundle` as a deployment step) and seeds the first admin from configuration.
    /// </summary>
    public static async Task InitialiseAsync(IServiceProvider services, bool applyMigrations)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        if (applyMigrations)
            await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));

        var seed = sp.GetRequiredService<IConfiguration>().GetSection(SeedSettings.SectionName).Get<SeedSettings>();
        if (string.IsNullOrWhiteSpace(seed?.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword)) return;

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(seed.AdminEmail) is not null) return;

        var admin = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = seed.AdminEmail,
            Email = seed.AdminEmail,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(admin, seed.AdminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Admin seeding failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }
        await userManager.AddToRoleAsync(admin, Roles.Admin);
        logger.LogInformation("Seeded initial administrator {Email}.", seed.AdminEmail);
    }
}
