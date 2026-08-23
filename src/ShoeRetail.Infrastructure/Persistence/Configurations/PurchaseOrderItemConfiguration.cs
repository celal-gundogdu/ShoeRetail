using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.Property(i => i.StockCodeSnapshot).HasMaxLength(15).IsRequired();
        builder.Property(i => i.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(i => i.SizeSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(i => i.ColorSnapshot).HasMaxLength(50).IsRequired();
        builder.Property(i => i.SupplierProductCode).HasMaxLength(100);
        builder.Property(i => i.ReceivedQuantity).HasDefaultValue(0);
        builder.Property(i => i.UnitPurchasePrice).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2)
            .HasComputedColumnSql("ordered_quantity * unit_purchase_price", stored: true);
        builder.Property(i => i.ReceivedTotal).HasPrecision(18, 2)
            .HasComputedColumnSql("received_quantity * unit_purchase_price", stored: true);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(i => i.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne<PurchaseOrder>().WithMany().HasForeignKey(i => i.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(i => i.ProductVariantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.PurchaseOrderId, i.ProductVariantId }).IsUnique()
            .HasDatabaseName("ux_purchase_order_items_order_variant");
        builder.HasIndex(i => i.PurchaseOrderId).HasDatabaseName("ix_purchase_order_items_order_id");
        builder.HasIndex(i => i.ProductVariantId).HasDatabaseName("ix_purchase_order_items_product_variant_id");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_purchase_order_items_ordered_positive", "ordered_quantity > 0");
            tb.HasCheckConstraint("chk_purchase_order_items_received_nonneg", "received_quantity >= 0");
            tb.HasCheckConstraint("chk_purchase_order_items_price_nonneg", "unit_purchase_price >= 0");
            tb.HasCheckConstraint("chk_purchase_order_items_snapshots_not_blank",
                "btrim(stock_code_snapshot) <> '' AND btrim(product_name_snapshot) <> '' AND btrim(size_snapshot) <> '' AND btrim(color_snapshot) <> ''");
        });
    }
}
