namespace ShoeRetail.Domain;

// Stok hareket defteri. APPEND-ONLY: yanlış hareket düzeltme hareketiyle telafi edilir.
public sealed class InventoryMovement
{
    public long Id { get; set; }
    public long ProductVariantId { get; set; }

    public string MovementType { get; set; } = string.Empty;
    public int OnHandDelta { get; set; }
    public int ReservedDelta { get; set; }

    public long? OrderId { get; set; }
    public long? PurchaseOrderId { get; set; }

    public string? Reason { get; set; }

    public long CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
