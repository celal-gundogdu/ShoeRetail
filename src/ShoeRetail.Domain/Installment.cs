namespace ShoeRetail.Domain;

// Durum kolonu yoktur — Ödendi/Kısmi/Bekliyor/Gecikmiş SUM(payment_allocations) +
// due_date karşılaştırmasından türetilir.
public sealed class Installment
{
    public long Id { get; set; }
    public long PaymentPlanId { get; set; }

    public short InstallmentNumber { get; set; }
    public string InstallmentType { get; set; } = string.Empty; // DownPayment | Regular

    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
