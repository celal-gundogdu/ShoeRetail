namespace ShoeRetail.Domain;

// payments <-> installments köprüsü. "NE KADARIYLA bağlı" bilgisini taşır.
public sealed class PaymentAllocation
{
    public long Id { get; set; }

    public long PaymentId { get; set; }
    public long InstallmentId { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
