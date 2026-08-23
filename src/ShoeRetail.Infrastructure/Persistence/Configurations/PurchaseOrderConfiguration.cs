using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.Property(o => o.PurchaseOrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.OrderDate).HasDefaultValueSql("CURRENT_DATE");
        builder.Property(o => o.Status).HasMaxLength(20).HasDefaultValue("Draft");
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(o => o.SupplierReference).HasMaxLength(100);
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(o => o.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne<Supplier>().WithMany().HasForeignKey(o => o.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(o => o.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(o => o.CompletedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(o => o.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.PurchaseOrderNumber).IsUnique().HasDatabaseName("ux_purchase_orders_number");
        builder.HasIndex(o => o.SupplierId).HasDatabaseName("ix_purchase_orders_supplier_id");
        builder.HasIndex(o => o.Status).HasDatabaseName("ix_purchase_orders_status");
        builder.HasIndex(o => o.OrderDate).HasDatabaseName("ix_purchase_orders_order_date");
        builder.HasIndex(o => o.PaymentDueDate).HasDatabaseName("ix_purchase_orders_payment_due");
        builder.HasIndex(o => o.ExpectedDeliveryDate).HasDatabaseName("ix_purchase_orders_expected_delivery");
        builder.HasIndex(o => o.CreatedByUserId).HasDatabaseName("ix_purchase_orders_created_by");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_purchase_orders_status",
                "status IN ('Draft','Sent','InProduction','Ready','Shipped','Completed','Cancelled')");
            tb.HasCheckConstraint("chk_purchase_orders_total_nonneg", "total_amount >= 0");
            tb.HasCheckConstraint("chk_purchase_orders_sent_fields", "status IN ('Draft','Cancelled') OR sent_at IS NOT NULL");
            tb.HasCheckConstraint("chk_purchase_orders_completed_fields",
                "status <> 'Completed' OR (completed_at IS NOT NULL AND completed_by_user_id IS NOT NULL)");
            tb.HasCheckConstraint("chk_purchase_orders_cancelled_fields",
                "status <> 'Cancelled' OR (cancelled_at IS NOT NULL AND cancelled_by_user_id IS NOT NULL AND cancellation_reason IS NOT NULL AND btrim(cancellation_reason) <> '')");
        });
    }
}
