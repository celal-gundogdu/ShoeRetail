namespace ShoeRetail.Domain;

public sealed class OrderItem
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long ProductVariantId { get; set; }

    // Snapshot alanları (sipariş anındaki hâl) — Değişmez Kural #5
    public string StockCodeSnapshot { get; set; } = string.Empty;
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string SizeSnapshot { get; set; } = string.Empty;
    public string ColorSnapshot { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal UnitSalePrice { get; set; }
    public decimal UnitPurchasePrice { get; set; } // SADECE Owner görür

    // DB'de GENERATED ALWAYS AS (quantity * unit_sale_price) STORED.
    public decimal LineTotal { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
}
