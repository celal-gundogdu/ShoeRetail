using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property(v => v.Size).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Color).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Barcode).HasMaxLength(50);
        builder.Property(v => v.PurchasePrice).HasPrecision(18, 2);
        builder.Property(v => v.SalePrice).HasPrecision(18, 2);
        builder.Property(v => v.IsActive).HasDefaultValue(true);
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(v => v.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.HasOne<Product>().WithMany().HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.ProductId, v.Size, v.Color }).IsUnique()
            .HasDatabaseName("ux_product_variants_product_size_color");
        builder.HasIndex(v => v.Barcode).IsUnique()
            .HasDatabaseName("ux_product_variants_barcode").HasFilter("barcode IS NOT NULL");
        builder.HasIndex(v => v.ProductId).HasDatabaseName("ix_product_variants_product_id");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_product_variants_size_trimmed", "size = btrim(size) AND size <> ''");
            tb.HasCheckConstraint("chk_product_variants_color_trimmed", "color = btrim(color) AND color <> ''");
            tb.HasCheckConstraint("chk_product_variants_purchase_price_nonneg", "purchase_price >= 0");
            tb.HasCheckConstraint("chk_product_variants_sale_price_nonneg", "sale_price >= 0");
        });
    }
}
