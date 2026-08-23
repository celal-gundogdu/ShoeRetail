namespace ShoeRetail.Domain;

// Sevkiyat anında oluşur (sipariş alındığında DEĞİL). V1'de sipariş başına tek plan.
public sealed class PaymentPlan
{
    public long Id { get; set; }
    public long OrderId { get; set; }

    public long CreatedByUserId { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
