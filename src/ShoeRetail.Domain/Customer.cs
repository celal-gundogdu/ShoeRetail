namespace ShoeRetail.Domain;

public sealed class Customer
{
    public long Id { get; set; }

    public string CustomerType { get; set; } = string.Empty; // Individual | Corporate

    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? ContactPerson { get; set; }

    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }

    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }

    public string? City { get; set; }
    public string? District { get; set; }
    public string? BillingAddress { get; set; }
    public string? DeliveryAddress { get; set; }

    public short? DefaultPaymentTermDays { get; set; }
    public decimal? CreditLimit { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
