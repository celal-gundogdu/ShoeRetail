using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class OrderHistoryConfiguration : IEntityTypeConfiguration<OrderHistory>
{
    public void Configure(EntityTypeBuilder<OrderHistory> builder)
    {
        builder.Property(h => h.EventType).HasMaxLength(30).IsRequired();
        builder.Property(h => h.ChangedAt).HasDefaultValueSql("now()");

        builder.HasOne<Order>().WithMany().HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(h => h.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.OrderId, h.ChangedAt }).HasDatabaseName("ix_order_history_order_id_changed_at");
        builder.HasIndex(h => h.ChangedByUserId).HasDatabaseName("ix_order_history_changed_by_user_id");

        builder.ToTable(tb => tb.HasCheckConstraint("chk_order_history_event_type",
            "event_type IN ('Created','StatusChanged','ItemAdded','ItemChanged','ItemRemoved','NoteChanged','ExpectedShipDateChanged','DeliveryAddressChanged')"));
    }
}
