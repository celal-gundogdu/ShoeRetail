using Microsoft.EntityFrameworkCore;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence;

// Entity'ler tabloyla aynı sırada, tek tek eklenecek (bkz. CLAUDE.md §12).
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<StoreProfile> StoreProfile => Set<StoreProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
