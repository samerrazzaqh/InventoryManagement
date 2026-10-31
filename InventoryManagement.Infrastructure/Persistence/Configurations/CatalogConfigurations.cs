using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.ConfigureKey("CategoryId");
        b.ConfigureAudit();

        b.Property(c => c.Name).HasMaxLength(100).IsRequired();
        b.Property(c => c.Description).HasMaxLength(500);
        b.Property(c => c.IsActive).HasDefaultValue(true);

        b.HasIndex(c => c.Name).IsUnique();
    }
}

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("Suppliers");
        b.ConfigureKey("SupplierId");
        b.ConfigureAudit();

        b.Property(s => s.Name).HasMaxLength(200).IsRequired();
        b.Property(s => s.ContactName).HasMaxLength(150);
        b.Property(s => s.Email).HasMaxLength(256);
        b.Property(s => s.Phone).HasMaxLength(32);
        b.Property(s => s.IsActive).HasDefaultValue(true);

        b.OwnsOne(s => s.Address, a =>
        {
            a.Property(x => x.Street).HasColumnName("AddressStreet").HasMaxLength(200);
            a.Property(x => x.City).HasColumnName("AddressCity").HasMaxLength(100);
            a.Property(x => x.State).HasColumnName("AddressState").HasMaxLength(100);
            a.Property(x => x.PostalCode).HasColumnName("AddressPostalCode").HasMaxLength(20);
            a.Property(x => x.Country).HasColumnName("AddressCountry").HasMaxLength(100);
        });

        b.HasIndex(s => s.Name);
    }
}

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.ToTable("Warehouses");
        b.ConfigureKey("WarehouseId");
        b.ConfigureAudit();

        b.Property(w => w.Name).HasMaxLength(150).IsRequired();
        b.Property(w => w.Location).HasMaxLength(300).IsRequired();
        b.Property(w => w.ManagerName).HasMaxLength(150);
        b.Property(w => w.IsActive).HasDefaultValue(true);

        b.HasIndex(w => w.Name).IsUnique();
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products", t =>
        {
            t.HasCheckConstraint("CK_Products_UnitPrice", "[UnitPrice] >= 0");
            t.HasCheckConstraint("CK_Products_ReorderLevel", "[ReorderLevel] >= 0");
        });
        b.ConfigureKey("ProductId");
        b.ConfigureAudit();

        b.Property(p => p.Sku).HasMaxLength(50).IsUnicode(false).IsRequired();
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.Property(p => p.Description).HasMaxLength(2000);
        b.Property(p => p.UnitPrice).HasPrecision(18, 2);
        b.Property(p => p.IsActive).HasDefaultValue(true);

        b.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Supplier).WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(p => p.Sku).IsUnique();
        b.HasIndex(p => p.CategoryId);
        b.HasIndex(p => p.SupplierId);
        b.HasIndex(p => new { p.IsActive, p.Name });
    }
}

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.ConfigureKey("CustomerId");
        b.ConfigureAudit();

        b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        b.Property(c => c.Email).HasMaxLength(256);
        b.Property(c => c.Phone).HasMaxLength(32);

        b.OwnsOne(c => c.Address, a =>
        {
            a.Property(x => x.Street).HasColumnName("AddressStreet").HasMaxLength(200);
            a.Property(x => x.City).HasColumnName("AddressCity").HasMaxLength(100);
            a.Property(x => x.State).HasColumnName("AddressState").HasMaxLength(100);
            a.Property(x => x.PostalCode).HasColumnName("AddressPostalCode").HasMaxLength(20);
            a.Property(x => x.Country).HasColumnName("AddressCountry").HasMaxLength(100);
        });

        b.HasIndex(c => c.Email);
    }
}
