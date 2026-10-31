namespace InventoryManagement.Application.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string WarehouseManager = "WarehouseManager";
    public const string Staff = "Staff";

    public static readonly string[] All = [Admin, WarehouseManager, Staff];
}

/// <summary>Cache "groups" are invalidated as a unit (see ICacheService.InvalidateGroupAsync).</summary>
public static class CacheGroups
{
    public const string Products = "products";
    public const string Categories = "categories";
}
