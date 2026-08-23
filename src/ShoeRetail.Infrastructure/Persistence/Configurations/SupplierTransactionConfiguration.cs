using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class SupplierTransactionConfiguration : IEntityTypeConfiguration<SupplierTransaction>
{
    public void Configure(EntityTypeBuilder<SupplierTransaction> builder)
    {
        builder.Property(t => t.TransactionType).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<Supplier>().WithMany().HasForeignKey(t => t.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PurchaseOrder>().WithMany().HasForeignKey(t => t.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SupplierPayment>().WithMany().HasForeignKey(t => t.SupplierPaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.SupplierId, t.CreatedAt }).HasDatabaseName("ix_supplier_transactions_supplier_created");
        builder.HasIndex(t => t.PurchaseOrderId).HasDatabaseName("ix_supplier_transactions_purchase_order_id");
        builder.HasIndex(t => t.SupplierPaymentId).HasDatabaseName("ix_supplier_transactions_payment_id");
        builder.HasIndex(t => t.CreatedByUserId).HasDatabaseName("ix_supplier_transactions_created_by");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_supplier_transactions_amount_nonzero", "amount <> 0");
            tb.HasCheckConstraint("chk_supplier_transactions_type_signature", """
                (transaction_type = 'Purchase' AND amount > 0 AND purchase_order_id IS NOT NULL AND supplier_payment_id IS NULL)
                OR
                (transaction_type = 'Payment' AND amount < 0 AND supplier_payment_id IS NOT NULL)
                OR
                (transaction_type = 'Reversal' AND (purchase_order_id IS NOT NULL OR supplier_payment_id IS NOT NULL))
                OR
                (transaction_type = 'Adjustment' AND description IS NOT NULL AND btrim(description) <> '')
                """);
        });
    }
}
