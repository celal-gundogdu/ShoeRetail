using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(s => s.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ContactPerson).HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Email).HasMaxLength(255);
        builder.Property(s => s.TaxNumber).HasMaxLength(50);
        builder.Property(s => s.TaxOffice).HasMaxLength(100);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.District).HasMaxLength(100);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(s => s.CompanyName).HasDatabaseName("ix_suppliers_company_name");
        builder.HasIndex(s => s.Phone).HasDatabaseName("ix_suppliers_phone");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_suppliers_company_name_not_blank", "btrim(company_name) <> ''");
            tb.HasCheckConstraint("chk_suppliers_phone_not_blank", "btrim(phone) <> ''");
            tb.HasCheckConstraint("chk_suppliers_payment_term_nonneg",
                "default_payment_term_days IS NULL OR default_payment_term_days >= 0");
            tb.HasCheckConstraint("chk_suppliers_lead_time_nonneg",
                "default_lead_time_days IS NULL OR default_lead_time_days >= 0");
        });
    }
}
