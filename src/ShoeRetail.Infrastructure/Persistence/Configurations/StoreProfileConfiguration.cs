using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

// docs/database/02-physical-blueprint.md Tablo 1/22 — store_profile ile birebir eşleşir.
public sealed class StoreProfileConfiguration : IEntityTypeConfiguration<StoreProfile>
{
    public void Configure(EntityTypeBuilder<StoreProfile> builder)
    {
        builder.HasKey(p => p.Id);
        // id her zaman 1'dir; DB'nin ürettiği bir identity değil (22 tablodaki tek istisna).
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.StoreName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Phone).HasMaxLength(30);
        builder.Property(p => p.Email).HasMaxLength(255);
        builder.Property(p => p.TaxNumber).HasMaxLength(50);
        builder.Property(p => p.TaxOffice).HasMaxLength(100);

        builder.Property(p => p.CurrencyCode)
            .HasColumnType("char(3)")
            .HasDefaultValue("TRY")
            .IsRequired();

        builder.Property(p => p.StockCodePrefix).HasMaxLength(5).HasDefaultValue("GND").IsRequired();
        builder.Property(p => p.StockCodeDigits).HasDefaultValue((short)6);
        builder.Property(p => p.DefaultLowStockThreshold).HasDefaultValue(5);

        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_store_profile_singleton", "id = 1");
            tb.HasCheckConstraint("chk_store_profile_name_not_blank", "btrim(store_name) <> ''");
            tb.HasCheckConstraint("chk_store_profile_currency_format", "currency_code ~ '^[A-Z]{3}$'");
            tb.HasCheckConstraint("chk_store_profile_stock_prefix_format", "stock_code_prefix ~ '^[A-Z]{2,5}$'");
            tb.HasCheckConstraint("chk_store_profile_stock_digits_range", "stock_code_digits BETWEEN 4 AND 8");
            tb.HasCheckConstraint("chk_store_profile_low_stock_nonneg", "default_low_stock_threshold >= 0");
        });

        // schema.sql'deki tohum satırıyla aynı: INSERT INTO store_profile (store_name) VALUES ('Mağaza Adı');
        builder.HasData(new StoreProfile
        {
            Id = 1,
            StoreName = "Mağaza Adı",
            CurrencyCode = "TRY",
            StockCodePrefix = "GND",
            StockCodeDigits = 6,
            DefaultLowStockThreshold = 5,
            UpdatedAt = new DateTimeOffset(2026, 8, 23, 0, 0, 0, TimeSpan.Zero)
        });
    }
}
