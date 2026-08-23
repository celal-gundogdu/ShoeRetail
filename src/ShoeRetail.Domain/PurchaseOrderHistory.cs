namespace ShoeRetail.Domain;

// APPEND-ONLY. changed_by_user_id hem bizden hem üreticiden olabilir.
public sealed class PurchaseOrderHistory
{
    public long Id { get; set; }
    public long PurchaseOrderId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Note { get; set; }

    public long ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
