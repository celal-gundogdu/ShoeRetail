using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.PaymentMethod).HasMaxLength(20).IsRequired();
        builder.Property(p => p.ReferenceNo).HasMaxLength(100);
        builder.Property(p => p.Status).HasMaxLength(20).HasDefaultValue("Active");
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne<Customer>().WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(p => p.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(p => p.ReversedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.CustomerId, p.PaymentDate }).HasDatabaseName("ix_payments_customer_date");
        builder.HasIndex(p => p.PaymentDate).HasDatabaseName("ix_payments_payment_date");
        builder.HasIndex(p => p.CreatedByUserId).HasDatabaseName("ix_payments_created_by");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_payments_amount_positive", "amount > 0");
            tb.HasCheckConstraint("chk_payments_method",
                "payment_method IN ('Cash', 'BankTransfer', 'CreditCard', 'Cheque', 'PromissoryNote')");
            tb.HasCheckConstraint("chk_payments_status", "status IN ('Active', 'Reversed')");
            tb.HasCheckConstraint("chk_payments_reversal_consistency", """
                (status = 'Active' AND reversed_at IS NULL AND reversed_by_user_id IS NULL AND reversal_reason IS NULL)
                OR
                (status = 'Reversed' AND reversed_at IS NOT NULL AND reversed_by_user_id IS NOT NULL
                    AND reversal_reason IS NOT NULL AND btrim(reversal_reason) <> '')
                """);
        });
    }
}
