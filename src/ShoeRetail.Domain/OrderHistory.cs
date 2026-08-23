namespace ShoeRetail.Domain;

// APPEND-ONLY: güncellenmez, silinmez (bu yüzden UpdatedAt yok).
public sealed class OrderHistory
{
    public long Id { get; set; }
    public long OrderId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Note { get; set; }

    public long ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
