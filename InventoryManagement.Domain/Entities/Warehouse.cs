using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

public sealed class Warehouse : AuditableEntity
{
    private Warehouse() { }

    public string Name { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public string? ManagerName { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Warehouse Create(string name, string location, string? managerName)
    {
        var warehouse = new Warehouse();
        warehouse.Update(name, location, managerName, isActive: true);
        return warehouse;
    }

    public void Update(string name, string location, string? managerName, bool isActive)
    {
        Name = Guard.NotBlank(name, nameof(Name), 150);
        Location = Guard.NotBlank(location, nameof(Location), 300);
        ManagerName = Guard.Optional(managerName, nameof(ManagerName), 150);
        IsActive = isActive;
    }
}
