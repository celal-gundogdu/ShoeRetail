using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.Property(i => i.InstallmentType).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(i => i.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne<PaymentPlan>().WithMany().HasForeignKey(i => i.PaymentPlanId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.PaymentPlanId, i.InstallmentNumber }).IsUnique().HasDatabaseName("ux_installments_plan_number");
        builder.HasIndex(i => i.DueDate).HasDatabaseName("ix_installments_due_date");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_installments_type", "installment_type IN ('DownPayment', 'Regular')");
            tb.HasCheckConstraint("chk_installments_amount_positive", "amount > 0");
            tb.HasCheckConstraint("chk_installments_number_positive", "installment_number > 0");
        });
    }
}
