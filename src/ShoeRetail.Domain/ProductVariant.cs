namespace ShoeRetail.Domain;

public sealed class ProductVariant
{
    public long Id { get; set; }
    public long ProductId { get; set; }

    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty; // ToUpperInvariant() ile normalize edilmiş saklanır

    public string? Barcode { get; set; }

    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
