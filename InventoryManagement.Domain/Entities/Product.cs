using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

public sealed class Product : AuditableEntity
{
    private Product() { }

    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid SupplierId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int ReorderLevel { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Category Category { get; private set; } = null!;
    public Supplier Supplier { get; private set; } = null!;

    public static Product Create(string sku, string name, string? description,
        Guid categoryId, Guid supplierId, decimal unitPrice, int reorderLevel)
    {
        var product = new Product { Sku = Guard.NotBlank(sku, nameof(Sku), 50).ToUpperInvariant() };
        product.Apply(name, description, categoryId, supplierId, unitPrice, reorderLevel);
        return product;
    }

    /// <summary>SKU is immutable after creation (it is referenced by external systems).</summary>
    public void Update(string name, string? description, Guid categoryId, Guid supplierId,
        decimal unitPrice, int reorderLevel, bool isActive)
    {
        Apply(name, description, categoryId, supplierId, unitPrice, reorderLevel);
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;

    private void Apply(string name, string? description, Guid categoryId, Guid supplierId,
        decimal unitPrice, int reorderLevel)
    {
        Name = Guard.NotBlank(name, nameof(Name), 200);
        Description = Guard.Optional(description, nameof(Description), 2000);
        CategoryId = Guard.NotEmpty(categoryId, nameof(CategoryId));
        SupplierId = Guard.NotEmpty(supplierId, nameof(SupplierId));
        UnitPrice = Math.Round(Guard.NotNegative(unitPrice, nameof(UnitPrice)), 2);
        ReorderLevel = Guard.NotNegative(reorderLevel, nameof(ReorderLevel));
    }
}
