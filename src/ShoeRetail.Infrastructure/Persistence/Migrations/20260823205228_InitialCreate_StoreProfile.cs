using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoeRetail.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_StoreProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_profile",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    store_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    tax_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tax_office = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    currency_code = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "TRY"),
                    stock_code_prefix = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "GND"),
                    stock_code_digits = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)6),
                    default_low_stock_threshold = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_profile", x => x.id);
                    table.CheckConstraint("chk_store_profile_currency_format", "currency_code ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("chk_store_profile_low_stock_nonneg", "default_low_stock_threshold >= 0");
                    table.CheckConstraint("chk_store_profile_name_not_blank", "btrim(store_name) <> ''");
                    table.CheckConstraint("chk_store_profile_singleton", "id = 1");
                    table.CheckConstraint("chk_store_profile_stock_digits_range", "stock_code_digits BETWEEN 4 AND 8");
                    table.CheckConstraint("chk_store_profile_stock_prefix_format", "stock_code_prefix ~ '^[A-Z]{2,5}$'");
                });

            migrationBuilder.InsertData(
                table: "store_profile",
                columns: new[] { "id", "address", "currency_code", "default_low_stock_threshold", "email", "phone", "stock_code_digits", "stock_code_prefix", "store_name", "tax_number", "tax_office", "updated_at" },
                values: new object[] { (short)1, null, "TRY", 5, null, null, (short)6, "GND", "Mağaza Adı", null, null, new DateTimeOffset(new DateTime(2026, 8, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_profile");
        }
    }
}
