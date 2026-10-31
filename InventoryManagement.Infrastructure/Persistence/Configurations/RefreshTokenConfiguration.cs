using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryManagement.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.ConfigureKey("RefreshTokenId");

        b.Property(t => t.TokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();   // SHA-256 hex
        b.Property(t => t.CreatedByIp).HasMaxLength(45);      // IPv6 max length
        b.Property(t => t.RevokedByIp).HasMaxLength(45);
        b.Property(t => t.RevokedReason).HasMaxLength(100);
        b.Property(t => t.ReplacedByTokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        b.Property(t => t.RowVersion).IsRowVersion();         // two concurrent refreshes of one token cannot both win
        b.Ignore(t => t.IsRevoked);

        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(t => t.TokenHash).IsUnique();
        b.HasIndex(t => t.FamilyId);
        b.HasIndex(t => new { t.UserId, t.ExpiresOn });
    }
}
