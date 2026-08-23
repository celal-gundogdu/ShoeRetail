using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.Property(a => a.Amount).HasPrecision(18, 2);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<Payment>().WithMany().HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Installment>().WithMany().HasForeignKey(a => a.InstallmentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.PaymentId).HasDatabaseName("ix_payment_allocations_payment_id");
        builder.HasIndex(a => a.InstallmentId).HasDatabaseName("ix_payment_allocations_installment_id");

        builder.ToTable(tb => tb.HasCheckConstraint("chk_payment_allocations_amount_positive", "amount > 0"));
    }
}
