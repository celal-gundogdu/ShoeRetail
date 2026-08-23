namespace ShoeRetail.Domain;

public sealed class Inventory
{
    public long Id { get; set; }
    public long ProductVariantId { get; set; }

    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }

    // DB'de GENERATED ALWAYS AS (quantity_on_hand - quantity_reserved) STORED.
    // Uygulama asla yazmaz; private set bunu ifade eder.
    public int QuantityAvailable { get; private set; }

    public int LowStockThreshold { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
