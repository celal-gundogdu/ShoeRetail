using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.CustomerType).HasMaxLength(20).IsRequired();
        builder.Property(c => c.FullName).HasMaxLength(200);
        builder.Property(c => c.CompanyName).HasMaxLength(200);
        builder.Property(c => c.ContactPerson).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(255);
        builder.Property(c => c.TaxNumber).HasMaxLength(50);
        builder.Property(c => c.TaxOffice).HasMaxLength(100);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.District).HasMaxLength(100);
        builder.Property(c => c.CreditLimit).HasPrecision(18, 2);
        builder.Property(c => c.IsActive).HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(c => c.Phone).HasDatabaseName("ix_customers_phone");
        builder.HasIndex(c => c.FullName).HasDatabaseName("ix_customers_full_name");
        builder.HasIndex(c => c.CompanyName).HasDatabaseName("ix_customers_company_name");
        builder.HasIndex(c => c.City).HasDatabaseName("ix_customers_city");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_customers_type", "customer_type IN ('Individual', 'Corporate')");
            tb.HasCheckConstraint("chk_customers_type_name_consistency",
                "(customer_type = 'Individual' AND full_name IS NOT NULL) OR (customer_type = 'Corporate' AND company_name IS NOT NULL)");
            tb.HasCheckConstraint("chk_customers_phone_not_blank", "btrim(phone) <> ''");
            tb.HasCheckConstraint("chk_customers_payment_term_nonneg",
                "default_payment_term_days IS NULL OR default_payment_term_days >= 0");
            tb.HasCheckConstraint("chk_customers_credit_limit_nonneg",
                "credit_limit IS NULL OR credit_limit >= 0");
        });
    }
}
