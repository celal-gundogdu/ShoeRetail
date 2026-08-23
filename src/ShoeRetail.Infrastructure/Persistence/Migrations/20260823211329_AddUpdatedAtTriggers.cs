using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoeRetail.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtTriggers : Migration
    {
        // Karar ve gerekçe: docs/database/02-physical-blueprint.md
        // "Tekrar Eden Desenler" #6 (Faz 4). Birebir schema.sql ile aynı DDL.
        private const string TriggerFunctionSql = """
            CREATE OR REPLACE FUNCTION set_updated_at()
            RETURNS trigger AS $$
            BEGIN
                NEW.updated_at = now();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            """;

        private static readonly string[] TablesWithUpdatedAt =
        [
            "store_profile", "customers", "suppliers", "users", "products",
            "product_variants", "inventory", "orders", "purchase_orders",
            "purchase_order_items", "payment_plans", "installments", "payments",
            "supplier_payments"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(TriggerFunctionSql);

            foreach (var table in TablesWithUpdatedAt)
            {
                migrationBuilder.Sql($"""
                    CREATE TRIGGER trg_{table}_set_updated_at
                        BEFORE UPDATE ON {table}
                        FOR EACH ROW EXECUTE FUNCTION set_updated_at();
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in TablesWithUpdatedAt)
            {
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS trg_{table}_set_updated_at ON {table};");
            }

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS set_updated_at();");
        }
    }
}
