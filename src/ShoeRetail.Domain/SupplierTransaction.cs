namespace ShoeRetail.Domain;

// Üretici cari defteri. İŞARET TERS PERSPEKTİF (account_transactions'ın aynası değil,
// karşıtı): Amount > 0 bizim borcumuz artar (mal kabul), Amount < 0 azalır (ödeme).
// SAF APPEND-ONLY.
public sealed class SupplierTransaction
{
    public long Id { get; set; }
    public long SupplierId { get; set; }

    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; } // işaretli

    public long? PurchaseOrderId { get; set; }
    public long? SupplierPaymentId { get; set; }

    public string? Description { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
