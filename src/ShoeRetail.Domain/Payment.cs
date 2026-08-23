namespace ShoeRetail.Domain;

// Ters kayıt: satır SİLİNMEZ, Status = 'Reversed' işaretlenir. Negatif tutar YOK.
public sealed class Payment
{
    public long Id { get; set; }
    public long CustomerId { get; set; }

    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }

    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }

    public string Status { get; set; } = "Active";
    public DateTimeOffset? ReversedAt { get; set; }
    public long? ReversedByUserId { get; set; }
    public string? ReversalReason { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
