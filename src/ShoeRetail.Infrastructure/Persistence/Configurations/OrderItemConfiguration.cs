using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(i => i.StockCodeSnapshot).HasMaxLength(15).IsRequired();
        builder.Property(i => i.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(i => i.SizeSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(i => i.ColorSnapshot).HasMaxLength(50).IsRequired();
        builder.Property(i => i.UnitSalePrice).HasPrecision(18, 2);
        builder.Property(i => i.UnitPurchasePrice).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2)
            .HasComputedColumnSql("quantity * unit_sale_price", stored: true);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<Order>().WithMany().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(i => i.ProductVariantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.OrderId, i.ProductVariantId }).IsUnique().HasDatabaseName("ux_order_items_order_variant");
        builder.HasIndex(i => i.OrderId).HasDatabaseName("ix_order_items_order_id");
        builder.HasIndex(i => i.ProductVariantId).HasDatabaseName("ix_order_items_product_variant_id");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_order_items_quantity_positive", "quantity > 0");
            tb.HasCheckConstraint("chk_order_items_sale_price_nonneg", "unit_sale_price >= 0");
            tb.HasCheckConstraint("chk_order_items_purchase_price_nonneg", "unit_purchase_price >= 0");
            tb.HasCheckConstraint("chk_order_items_snapshots_not_blank",
                "btrim(stock_code_snapshot) <> '' AND btrim(product_name_snapshot) <> '' AND btrim(size_snapshot) <> '' AND btrim(color_snapshot) <> ''");
        });
    }
}
