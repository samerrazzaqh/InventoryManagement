using InventoryManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    /// <summary>Maps the shared Id to a domain-specific column name (e.g. ProductId). Ids are generated in the domain (GUID v7).</summary>
    public static void ConfigureKey<T>(this EntityTypeBuilder<T> b, string columnName) where T : BaseEntity
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName(columnName).ValueGeneratedNever();
        b.Ignore(e => e.DomainEvents);
    }

    public static void ConfigureAudit<T>(this EntityTypeBuilder<T> b) where T : AuditableEntity
    {
        b.Property(e => e.CreatedAt).IsRequired();
        b.Property(e => e.CreatedBy).HasMaxLength(64).IsRequired();
        b.Property(e => e.UpdatedBy).HasMaxLength(64);
    }
}
