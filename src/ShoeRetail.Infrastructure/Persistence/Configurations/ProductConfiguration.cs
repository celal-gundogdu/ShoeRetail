using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.StockCode).HasMaxLength(15).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Brand).HasMaxLength(100);
        builder.Property(p => p.Model).HasMaxLength(100);
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.Material).HasMaxLength(100);
        builder.Property(p => p.Gender).HasMaxLength(20);
        builder.Property(p => p.Season).HasMaxLength(20);
        builder.Property(p => p.SupplierProductCode).HasMaxLength(100);
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.HasOne<Supplier>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.StockCode).IsUnique().HasDatabaseName("ux_products_stock_code");
        builder.HasIndex(p => p.Name).HasDatabaseName("ix_products_name");
        builder.HasIndex(p => p.Brand).HasDatabaseName("ix_products_brand");
        builder.HasIndex(p => p.SupplierId).HasDatabaseName("ix_products_supplier_id");
        builder.HasIndex(p => p.Gender).HasDatabaseName("ix_products_gender");
        builder.HasIndex(p => p.Season).HasDatabaseName("ix_products_season");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_products_stock_code_format", "stock_code ~ '^[A-Z]{2,5}[0-9]{4,8}$'");
            tb.HasCheckConstraint("chk_products_name_not_blank", "btrim(name) <> ''");
            tb.HasCheckConstraint("chk_products_gender", "gender IS NULL OR gender IN ('Men', 'Women', 'Kids', 'Unisex')");
            tb.HasCheckConstraint("chk_products_season", "season IS NULL OR season IN ('Summer', 'Winter', 'AllSeason')");
        });
    }
}
