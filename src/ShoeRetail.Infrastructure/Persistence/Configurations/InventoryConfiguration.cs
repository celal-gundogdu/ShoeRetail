using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.Property(i => i.QuantityOnHand).HasDefaultValue(0);
        builder.Property(i => i.QuantityReserved).HasDefaultValue(0);
        builder.Property(i => i.QuantityAvailable)
            .HasComputedColumnSql("quantity_on_hand - quantity_reserved", stored: true);
        builder.Property(i => i.LowStockThreshold).HasDefaultValue(0);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(i => i.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(i => i.ProductVariantId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.ProductVariantId).IsUnique().HasDatabaseName("ux_inventory_product_variant_id");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_inventory_on_hand_nonneg", "quantity_on_hand >= 0");
            tb.HasCheckConstraint("chk_inventory_reserved_nonneg", "quantity_reserved >= 0");
            tb.HasCheckConstraint("chk_inventory_reserved_le_on_hand", "quantity_reserved <= quantity_on_hand");
            tb.HasCheckConstraint("chk_inventory_low_stock_nonneg", "low_stock_threshold >= 0");
        });
    }
}
