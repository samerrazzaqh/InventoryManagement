using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

public sealed class Category : AuditableEntity
{
    private Category() { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Category Create(string name, string? description)
    {
        var category = new Category();
        category.Apply(name, description);
        return category;
    }

    public void Update(string name, string? description, bool isActive)
    {
        Apply(name, description);
        IsActive = isActive;
    }

    private void Apply(string name, string? description)
    {
        Name = Guard.NotBlank(name, nameof(Name), 100);
        Description = Guard.Optional(description, nameof(Description), 500);
    }
}
