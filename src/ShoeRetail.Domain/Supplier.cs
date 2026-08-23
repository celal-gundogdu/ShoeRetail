namespace ShoeRetail.Domain;

public sealed class Supplier
{
    public long Id { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }

    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }

    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }

    public string? City { get; set; }
    public string? District { get; set; }
    public string? Address { get; set; }

    public short? DefaultPaymentTermDays { get; set; }
    public short? DefaultLeadTimeDays { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
