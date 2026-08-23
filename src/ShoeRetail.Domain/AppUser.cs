namespace ShoeRetail.Domain;

// "User" değil "AppUser": ASP.NET Core'da ControllerBase.User (ClaimsPrincipal) ile
// isim çakışmasını baştan önlemek için. Tablo adı yine de "users" (bkz. AppDbContext).
public sealed class AppUser
{
    public long Id { get; set; }

    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; set; } = string.Empty; // ToUpperInvariant()
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty; // Owner | Manufacturer
    public long? SupplierId { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
