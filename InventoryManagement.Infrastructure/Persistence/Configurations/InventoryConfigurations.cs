using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Persistence.Configurations;

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b)
    {
        b.ToTable("Inventory", t => t.HasCheckConstraint("CK_Inventory_Quantity", "[Quantity] >= 0"));
        b.ConfigureKey("InventoryId");

        b.Property(i => i.RowVersion).IsRowVersion();   // optimistic concurrency token
        b.Property(i => i.LastUpdated).IsRequired();

        b.HasOne(i => i.Warehouse).WithMany().HasForeignKey(i => i.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(i => new { i.WarehouseId, i.ProductId }).IsUnique();   // one row per product per warehouse
        b.HasIndex(i => i.ProductId);
        b.HasIndex(i => i.Quantity);                                       // low-stock scans
    }
}

internal sealed class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> b)
    {
        b.ToTable("StockTransactions", t =>
        {
            t.HasCheckConstraint("CK_StockTransactions_Quantity", "[Quantity] > 0");
            // The row's shape must match its type: In has only a destination, Out only a source, Transfer both (different).
            t.HasCheckConstraint("CK_StockTransactions_Shape",
                "([TransactionType] = 'In'  AND [DestinationWarehouseId] IS NOT NULL AND [SourceWarehouseId] IS NULL) OR " +
                "([TransactionType] = 'Out' AND [SourceWarehouseId] IS NOT NULL AND [DestinationWarehouseId] IS NULL) OR " +
                "([TransactionType] = 'Transfer' AND [SourceWarehouseId] IS NOT NULL AND [DestinationWarehouseId] IS NOT NULL " +
                "AND [SourceWarehouseId] <> [DestinationWarehouseId])");
        });
        b.ConfigureKey("TransactionId");

        b.Property(t => t.Type).HasColumnName("TransactionType").HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(t => t.ReferenceNumber).HasMaxLength(50).IsRequired();
        b.Property(t => t.Notes).HasMaxLength(500);
        b.Property(t => t.CreatedBy).HasMaxLength(64).IsRequired();

        b.HasOne(t => t.Product).WithMany().HasForeignKey(t => t.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.SourceWarehouse).WithMany().HasForeignKey(t => t.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.DestinationWarehouse).WithMany().HasForeignKey(t => t.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(t => new { t.ProductId, t.CreatedAt });
        b.HasIndex(t => t.ReferenceNumber);
    }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.ConfigureKey("OrderId");
        b.ConfigureAudit();

        b.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false).IsRequired();
        b.Property(o => o.Notes).HasMaxLength(500);
        b.Ignore(o => o.TotalAmount);

        b.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(o => o.Warehouse).WithMany().HasForeignKey(o => o.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasIndex(o => o.OrderNumber).IsUnique();
        b.HasIndex(o => new { o.CustomerId, o.OrderDate });
        b.HasIndex(o => o.Status);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems", t => t.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0"));
        b.ConfigureKey("OrderItemId");

        b.Property(i => i.UnitPrice).HasPrecision(18, 2);
        b.Ignore(i => i.LineTotal);

        b.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(i => new { i.OrderId, i.ProductId }).IsUnique();
        b.HasIndex(i => i.ProductId);
    }
}
