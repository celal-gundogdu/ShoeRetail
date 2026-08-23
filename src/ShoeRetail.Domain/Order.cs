namespace ShoeRetail.Domain;

public sealed class Order
{
    public long Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty; // SIP-2026-000142
    public long CustomerId { get; set; }
    public long CreatedByUserId { get; set; }

    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedShipDate { get; set; }

    public string Status { get; set; } = "Received";

    public decimal TotalAmount { get; set; }

    public string? DeliveryAddress { get; set; } // snapshot
    public string? Notes { get; set; }

    public DateTimeOffset? ShippedAt { get; set; }
    public long? ShippedByUserId { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public long? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
