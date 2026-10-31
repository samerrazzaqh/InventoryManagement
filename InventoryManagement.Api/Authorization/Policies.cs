using InventoryManagement.Application.Common;

namespace InventoryManagement.Api.Authorization;

public static class Policies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string ManageCatalog = nameof(ManageCatalog);
    public const string MoveStock = nameof(MoveStock);

    public static void Register(Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, p => p.RequireRole(Roles.Admin));
        options.AddPolicy(ManageCatalog, p => p.RequireRole(Roles.Admin, Roles.WarehouseManager));
        options.AddPolicy(MoveStock, p => p.RequireRole(Roles.Admin, Roles.WarehouseManager, Roles.Staff));

        // Secure by default: any endpoint without an explicit [Authorize]/[AllowAnonymous] requires a signed-in user.
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build();
    }
}
