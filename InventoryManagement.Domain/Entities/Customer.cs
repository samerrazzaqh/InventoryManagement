using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.ValueObjects;

namespace InventoryManagement.Domain.Entities;

public sealed class Customer : AuditableEntity
{
    private Customer() { }

    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public Address? Address { get; private set; }

    public static Customer Create(string name, string? email, string? phone, Address? address)
    {
        var customer = new Customer();
        customer.Update(name, email, phone, address);
        return customer;
    }

    public void Update(string name, string? email, string? phone, Address? address)
    {
        Name = Guard.NotBlank(name, nameof(Name), 200);
        Email = Guard.Optional(email, nameof(Email), 256);
        Phone = Guard.Optional(phone, nameof(Phone), 32);
        Address = address;
    }
}
