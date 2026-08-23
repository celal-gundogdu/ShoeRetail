namespace ShoeRetail.Domain;

public sealed class PurchaseOrder
{
    public long Id { get; set; }

    public string PurchaseOrderNumber { get; set; } = string.Empty; // ALS-2026-000042
    public long SupplierId { get; set; }
    public long CreatedByUserId { get; set; }

    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public DateOnly? PaymentDueDate { get; set; } // mal kabulde hesaplanır

    public string Status { get; set; } = "Draft";

    public decimal TotalAmount { get; set; }

    public string? SupplierReference { get; set; }
    public string? Notes { get; set; } // üretici de görür
    public string? InternalNotes { get; set; } // sadece biz, asla DTO'ya girmez

    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? SupplierShippedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long? CompletedByUserId { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public long? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
