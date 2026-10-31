using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.ValueObjects;

namespace InventoryManagement.Domain.Entities;

public sealed class Supplier : AuditableEntity
{
    private Supplier() { }

    public string Name { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public Address? Address { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Supplier Create(string name, string? contactName, string? email, string? phone, Address? address)
    {
        var supplier = new Supplier();
        supplier.Update(name, contactName, email, phone, address, isActive: true);
        return supplier;
    }

    public void Update(string name, string? contactName, string? email, string? phone, Address? address, bool isActive)
    {
        Name = Guard.NotBlank(name, nameof(Name), 200);
        ContactName = Guard.Optional(contactName, nameof(ContactName), 150);
        Email = Guard.Optional(email, nameof(Email), 256);
        Phone = Guard.Optional(phone, nameof(Phone), 32);
        Address = address;
        IsActive = isActive;
    }
}
