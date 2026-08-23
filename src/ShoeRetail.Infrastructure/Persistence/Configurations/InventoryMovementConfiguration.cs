using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.Property(m => m.MovementType).HasMaxLength(30).IsRequired();
        builder.Property(m => m.OnHandDelta).HasDefaultValue(0);
        builder.Property(m => m.ReservedDelta).HasDefaultValue(0);
        builder.Property(m => m.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(m => m.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(m => m.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PurchaseOrder>().WithMany().HasForeignKey(m => m.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(m => m.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.ProductVariantId, m.CreatedAt }).HasDatabaseName("ix_inventory_movements_variant_created");
        builder.HasIndex(m => m.OrderId).HasDatabaseName("ix_inventory_movements_order_id");
        builder.HasIndex(m => m.PurchaseOrderId).HasDatabaseName("ix_inventory_movements_purchase_order_id");
        builder.HasIndex(m => new { m.MovementType, m.CreatedAt }).HasDatabaseName("ix_inventory_movements_type_created");
        builder.HasIndex(m => m.CreatedByUserId).HasDatabaseName("ix_inventory_movements_created_by");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_inventory_movements_type_signature", """
                (movement_type = 'InitialStock'        AND on_hand_delta >  0 AND reserved_delta =  0) OR
                (movement_type = 'Purchase'            AND on_hand_delta >  0 AND reserved_delta =  0) OR
                (movement_type = 'Return'              AND on_hand_delta >  0 AND reserved_delta =  0) OR
                (movement_type = 'ManualIncrease'      AND on_hand_delta >  0 AND reserved_delta =  0) OR
                (movement_type = 'OrderReservation'    AND on_hand_delta =  0 AND reserved_delta >  0) OR
                (movement_type = 'ReservationReleased' AND on_hand_delta =  0 AND reserved_delta <  0) OR
                (movement_type = 'Sale'                AND on_hand_delta <  0 AND reserved_delta <= 0) OR
                (movement_type = 'ManualDecrease'      AND on_hand_delta <  0 AND reserved_delta =  0) OR
                (movement_type = 'Damaged'             AND on_hand_delta <  0 AND reserved_delta =  0)
                """);
            tb.HasCheckConstraint("chk_inventory_movements_manual_reason",
                "movement_type NOT IN ('ManualIncrease','ManualDecrease','Damaged') OR (reason IS NOT NULL AND btrim(reason) <> '')");
            tb.HasCheckConstraint("chk_inventory_movements_order_link",
                "movement_type NOT IN ('Sale','OrderReservation','ReservationReleased') OR order_id IS NOT NULL");
        });
    }
}
