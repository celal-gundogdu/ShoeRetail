namespace ShoeRetail.Domain;

// Perakendeci cari defteri — bakiyenin TEK doğruluk kaynağı (bakiye = SUM(Amount)).
// İşaret: Amount > 0 borç artar (sevkiyat), Amount < 0 borç azalır (tahsilat).
// SAF APPEND-ONLY: güncellenmez, silinmez.
public sealed class AccountTransaction
{
    public long Id { get; set; }
    public long CustomerId { get; set; }

    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; } // işaretli

    public long? OrderId { get; set; }
    public long? PaymentId { get; set; }

    public string? Description { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
