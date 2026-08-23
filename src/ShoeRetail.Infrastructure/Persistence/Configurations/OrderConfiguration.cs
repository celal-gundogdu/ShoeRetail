using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.OrderDate).HasDefaultValueSql("CURRENT_DATE");
        builder.Property(o => o.Status).HasMaxLength(20).HasDefaultValue("Received");
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2).HasDefaultValue(0m);
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(o => o.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(o => o.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(o => o.ShippedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(o => o.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.OrderNumber).IsUnique().HasDatabaseName("ux_orders_order_number");
        builder.HasIndex(o => o.CustomerId).HasDatabaseName("ix_orders_customer_id");
        builder.HasIndex(o => o.Status).HasDatabaseName("ix_orders_status");
        builder.HasIndex(o => o.OrderDate).HasDatabaseName("ix_orders_order_date");
        builder.HasIndex(o => o.CreatedByUserId).HasDatabaseName("ix_orders_created_by_user_id");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_orders_status",
                "status IN ('Received','Preparing','Shipped','Delivered','Cancelled')");
            tb.HasCheckConstraint("chk_orders_total_nonneg", "total_amount >= 0");
            tb.HasCheckConstraint("chk_orders_shipped_fields",
                "status NOT IN ('Shipped','Delivered') OR (shipped_at IS NOT NULL AND shipped_by_user_id IS NOT NULL)");
            tb.HasCheckConstraint("chk_orders_delivered_fields", "status <> 'Delivered' OR delivered_at IS NOT NULL");
            tb.HasCheckConstraint("chk_orders_cancelled_fields",
                "status <> 'Cancelled' OR (cancelled_at IS NOT NULL AND cancelled_by_user_id IS NOT NULL AND cancellation_reason IS NOT NULL AND btrim(cancellation_reason) <> '')");
        });
    }
}
