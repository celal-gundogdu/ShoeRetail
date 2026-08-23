using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.Property(t => t.TransactionType).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<Customer>().WithMany().HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Order>().WithMany().HasForeignKey(t => t.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Payment>().WithMany().HasForeignKey(t => t.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.CustomerId, t.CreatedAt }).HasDatabaseName("ix_account_transactions_customer_created");
        builder.HasIndex(t => t.OrderId).HasDatabaseName("ix_account_transactions_order_id");
        builder.HasIndex(t => t.PaymentId).HasDatabaseName("ix_account_transactions_payment_id");
        builder.HasIndex(t => t.CreatedByUserId).HasDatabaseName("ix_account_transactions_created_by");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_account_transactions_amount_nonzero", "amount <> 0");
            tb.HasCheckConstraint("chk_account_transactions_type_signature", """
                (transaction_type = 'Sale' AND amount > 0 AND order_id IS NOT NULL AND payment_id IS NULL)
                OR
                (transaction_type = 'Payment' AND amount < 0 AND payment_id IS NOT NULL AND order_id IS NULL)
                OR
                (transaction_type = 'Reversal' AND (order_id IS NOT NULL OR payment_id IS NOT NULL))
                OR
                (transaction_type = 'Adjustment' AND description IS NOT NULL AND btrim(description) <> '')
                """);
        });
    }
}
