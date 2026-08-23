namespace ShoeRetail.Domain;

// Polimorfik referans: EntityId bilinçli olarak FK DEĞİL (21 tablonun hepsine
// referans verebilmeli). SAF APPEND-ONLY.
public sealed class AuditLog
{
    public long Id { get; set; }

    public long? UserId { get; set; } // NULL = başarısız giriş / sistem işlemi

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;
    public long? EntityId { get; set; }

    public string? OldValues { get; set; } // jsonb
    public string? NewValues { get; set; } // jsonb

    public string? Description { get; set; }
    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
