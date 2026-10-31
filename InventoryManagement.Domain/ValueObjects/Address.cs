namespace InventoryManagement.Domain.ValueObjects;

/// <summary>Immutable value object, persisted as an owned type (columns on the owning table).</summary>
public sealed record Address(string Street, string City, string? State, string PostalCode, string Country);
