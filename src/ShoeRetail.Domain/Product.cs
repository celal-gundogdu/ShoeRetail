namespace ShoeRetail.Domain;

public sealed class Product
{
    public long Id { get; set; }

    public string StockCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Category { get; set; }
    public string? Material { get; set; }
    public string? Gender { get; set; }
    public string? Season { get; set; }
    public string? Description { get; set; }

    // Varsayılan üretici: kısıt değil, ipuçu. Gerçek üretici purchase_orders.supplier_id'dir.
    public long? SupplierId { get; set; }
    public string? SupplierProductCode { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
