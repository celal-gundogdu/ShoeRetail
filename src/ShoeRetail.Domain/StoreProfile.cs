namespace ShoeRetail.Domain;

// Tekil mağaza ayarları. Tabloda her zaman tam bir satır olmalı (id sabit 1) —
// bkz. docs/database/02-physical-blueprint.md Tablo 1/22, "Singleton tablo problemi".
public sealed class StoreProfile
{
    public short Id { get; set; }

    public string StoreName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }

    public string CurrencyCode { get; set; } = "TRY";

    public string StockCodePrefix { get; set; } = "GND";
    public short StockCodeDigits { get; set; } = 6;

    public int DefaultLowStockThreshold { get; set; } = 5;

    public DateTimeOffset UpdatedAt { get; set; }
}
