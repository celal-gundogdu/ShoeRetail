using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.Username).HasMaxLength(100).IsRequired();
        builder.Property(u => u.NormalizedUsername).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(20).IsRequired();
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne<Supplier>().WithMany().HasForeignKey(u => u.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.NormalizedUsername).IsUnique().HasDatabaseName("ux_users_normalized_username");
        builder.HasIndex(u => u.SupplierId).HasDatabaseName("ix_users_supplier_id");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_users_role", "role IN ('Owner', 'Manufacturer')");
            tb.HasCheckConstraint("chk_users_role_supplier_consistency",
                "(role = 'Owner' AND supplier_id IS NULL) OR (role = 'Manufacturer' AND supplier_id IS NOT NULL)");
            tb.HasCheckConstraint("chk_users_username_format", "username ~ '^[A-Za-z0-9._-]{3,50}$'");
            tb.HasCheckConstraint("chk_users_normalized_username_format", "normalized_username ~ '^[A-Z0-9._-]{3,50}$'");
            tb.HasCheckConstraint("chk_users_full_name_not_blank", "btrim(full_name) <> ''");
        });
    }
}
