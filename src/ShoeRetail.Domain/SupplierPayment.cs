namespace ShoeRetail.Domain;

// payments'ın üretici aynası. Taksit/dağıtım tablosu YOK (asimetri bilinçli, spec §8.3).
public sealed class SupplierPayment
{
    public long Id { get; set; }
    public long SupplierId { get; set; }

    public long? PurchaseOrderId { get; set; } // toplu ödeme/avansta NULL

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
