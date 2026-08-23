using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShoeRetail.Domain;

namespace ShoeRetail.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.OldValues).HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnType("jsonb");
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        // entity_id KASITLI OLARAK FK DEĞİL (polimorfik referans, 21 tabloya bakabilir).
        builder.HasOne<AppUser>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("ix_audit_logs_entity");
        builder.HasIndex(a => new { a.UserId, a.CreatedAt }).HasDatabaseName("ix_audit_logs_user_created");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("ix_audit_logs_created_at");

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("chk_audit_logs_action_not_blank", "btrim(action) <> ''");
            tb.HasCheckConstraint("chk_audit_logs_entity_type_not_blank", "btrim(entity_type) <> ''");
        });
    }
}
