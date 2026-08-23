namespace ShoeRetail.Domain;

public sealed class PurchaseOrderItem
{
    public long Id { get; set; }
    public long PurchaseOrderId { get; set; }
    public long ProductVariantId { get; set; }

    public string StockCodeSnapshot { get; set; } = string.Empty;
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string SizeSnapshot { get; set; } = string.Empty;
    public string ColorSnapshot { get; set; } = string.Empty;
    public string? SupplierProductCode { get; set; }

    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; } // birikimli; fazla teslimat bilinçli olarak engellenmez

    public decimal UnitPurchasePrice { get; set; }

    // DB'de GENERATED ALWAYS AS STORED — taahhüt edilen tutar
    public decimal LineTotal { get; private set; }
    // DB'de GENERATED ALWAYS AS STORED — fiilen borçlanılan tutar
    public decimal ReceivedTotal { get; private set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
