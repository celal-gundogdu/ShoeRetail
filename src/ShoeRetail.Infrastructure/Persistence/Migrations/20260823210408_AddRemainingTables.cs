using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ShoeRetail.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "order_number_seq");

            migrationBuilder.CreateSequence(
                name: "purchase_order_number_seq");

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contact_person = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tax_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tax_office = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    billing_address = table.Column<string>(type: "text", nullable: true),
                    delivery_address = table.Column<string>(type: "text", nullable: true),
                    default_payment_term_days = table.Column<short>(type: "smallint", nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                    table.CheckConstraint("chk_customers_credit_limit_nonneg", "credit_limit IS NULL OR credit_limit >= 0");
                    table.CheckConstraint("chk_customers_payment_term_nonneg", "default_payment_term_days IS NULL OR default_payment_term_days >= 0");
                    table.CheckConstraint("chk_customers_phone_not_blank", "btrim(phone) <> ''");
                    table.CheckConstraint("chk_customers_type", "customer_type IN ('Individual', 'Corporate')");
                    table.CheckConstraint("chk_customers_type_name_consistency", "(customer_type = 'Individual' AND full_name IS NOT NULL) OR (customer_type = 'Corporate' AND company_name IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_person = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tax_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tax_office = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    default_payment_term_days = table.Column<short>(type: "smallint", nullable: true),
                    default_lead_time_days = table.Column<short>(type: "smallint", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                    table.CheckConstraint("chk_suppliers_company_name_not_blank", "btrim(company_name) <> ''");
                    table.CheckConstraint("chk_suppliers_lead_time_nonneg", "default_lead_time_days IS NULL OR default_lead_time_days >= 0");
                    table.CheckConstraint("chk_suppliers_payment_term_nonneg", "default_payment_term_days IS NULL OR default_payment_term_days >= 0");
                    table.CheckConstraint("chk_suppliers_phone_not_blank", "btrim(phone) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_code = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    material = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    season = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    supplier_product_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("chk_products_gender", "gender IS NULL OR gender IN ('Men', 'Women', 'Kids', 'Unisex')");
                    table.CheckConstraint("chk_products_name_not_blank", "btrim(name) <> ''");
                    table.CheckConstraint("chk_products_season", "season IS NULL OR season IN ('Summer', 'Winter', 'AllSeason')");
                    table.CheckConstraint("chk_products_stock_code_format", "stock_code ~ '^[A-Z]{2,5}[0-9]{4,8}$'");
                    table.ForeignKey(
                        name: "fk_products_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("chk_users_full_name_not_blank", "btrim(full_name) <> ''");
                    table.CheckConstraint("chk_users_normalized_username_format", "normalized_username ~ '^[A-Z0-9._-]{3,50}$'");
                    table.CheckConstraint("chk_users_role", "role IN ('Owner', 'Manufacturer')");
                    table.CheckConstraint("chk_users_role_supplier_consistency", "(role = 'Owner' AND supplier_id IS NULL) OR (role = 'Manufacturer' AND supplier_id IS NOT NULL)");
                    table.CheckConstraint("chk_users_username_format", "username ~ '^[A-Za-z0-9._-]{3,50}$'");
                    table.ForeignKey(
                        name: "fk_users_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    size = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    purchase_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variants", x => x.id);
                    table.CheckConstraint("chk_product_variants_color_trimmed", "color = btrim(color) AND color <> ''");
                    table.CheckConstraint("chk_product_variants_purchase_price_nonneg", "purchase_price >= 0");
                    table.CheckConstraint("chk_product_variants_sale_price_nonneg", "sale_price >= 0");
                    table.CheckConstraint("chk_product_variants_size_trimmed", "size = btrim(size) AND size <> ''");
                    table.ForeignKey(
                        name: "fk_product_variants_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<long>(type: "bigint", nullable: true),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.CheckConstraint("chk_audit_logs_action_not_blank", "btrim(action) <> ''");
                    table.CheckConstraint("chk_audit_logs_entity_type_not_blank", "btrim(entity_type) <> ''");
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    expected_ship_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Received"),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    delivery_address = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    shipped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    shipped_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.CheckConstraint("chk_orders_cancelled_fields", "status <> 'Cancelled' OR (cancelled_at IS NOT NULL AND cancelled_by_user_id IS NOT NULL AND cancellation_reason IS NOT NULL AND btrim(cancellation_reason) <> '')");
                    table.CheckConstraint("chk_orders_delivered_fields", "status <> 'Delivered' OR delivered_at IS NOT NULL");
                    table.CheckConstraint("chk_orders_shipped_fields", "status NOT IN ('Shipped','Delivered') OR (shipped_at IS NOT NULL AND shipped_by_user_id IS NOT NULL)");
                    table.CheckConstraint("chk_orders_status", "status IN ('Received','Preparing','Shipped','Delivered','Cancelled')");
                    table.CheckConstraint("chk_orders_total_nonneg", "total_amount >= 0");
                    table.ForeignKey(
                        name: "fk_orders_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_users_cancelled_by_user_id",
                        column: x => x.cancelled_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_users_shipped_by_user_id",
                        column: x => x.shipped_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    reversal_reason = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("chk_payments_amount_positive", "amount > 0");
                    table.CheckConstraint("chk_payments_method", "payment_method IN ('Cash', 'BankTransfer', 'CreditCard', 'Cheque', 'PromissoryNote')");
                    table.CheckConstraint("chk_payments_reversal_consistency", "(status = 'Active' AND reversed_at IS NULL AND reversed_by_user_id IS NULL AND reversal_reason IS NULL)\nOR\n(status = 'Reversed' AND reversed_at IS NOT NULL AND reversed_by_user_id IS NOT NULL\n    AND reversal_reason IS NOT NULL AND btrim(reversal_reason) <> '')");
                    table.CheckConstraint("chk_payments_status", "status IN ('Active', 'Reversed')");
                    table.ForeignKey(
                        name: "fk_payments_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_users_reversed_by_user_id",
                        column: x => x.reversed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    expected_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    payment_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    supplier_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    internal_notes = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    supplier_shipped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                    table.CheckConstraint("chk_purchase_orders_cancelled_fields", "status <> 'Cancelled' OR (cancelled_at IS NOT NULL AND cancelled_by_user_id IS NOT NULL AND cancellation_reason IS NOT NULL AND btrim(cancellation_reason) <> '')");
                    table.CheckConstraint("chk_purchase_orders_completed_fields", "status <> 'Completed' OR (completed_at IS NOT NULL AND completed_by_user_id IS NOT NULL)");
                    table.CheckConstraint("chk_purchase_orders_sent_fields", "status IN ('Draft','Cancelled') OR sent_at IS NOT NULL");
                    table.CheckConstraint("chk_purchase_orders_status", "status IN ('Draft','Sent','InProduction','Ready','Shipped','Completed','Cancelled')");
                    table.CheckConstraint("chk_purchase_orders_total_nonneg", "total_amount >= 0");
                    table.ForeignKey(
                        name: "fk_purchase_orders_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_users_cancelled_by_user_id",
                        column: x => x.cancelled_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_users_completed_by_user_id",
                        column: x => x.completed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_variant_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity_on_hand = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    quantity_reserved = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    quantity_available = table.Column<int>(type: "integer", nullable: false, computedColumnSql: "quantity_on_hand - quantity_reserved", stored: true),
                    low_stock_threshold = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory", x => x.id);
                    table.CheckConstraint("chk_inventory_low_stock_nonneg", "low_stock_threshold >= 0");
                    table.CheckConstraint("chk_inventory_on_hand_nonneg", "quantity_on_hand >= 0");
                    table.CheckConstraint("chk_inventory_reserved_le_on_hand", "quantity_reserved <= quantity_on_hand");
                    table.CheckConstraint("chk_inventory_reserved_nonneg", "quantity_reserved >= 0");
                    table.ForeignKey(
                        name: "fk_inventory_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    changed_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_history", x => x.id);
                    table.CheckConstraint("chk_order_history_event_type", "event_type IN ('Created','StatusChanged','ItemAdded','ItemChanged','ItemRemoved','NoteChanged','ExpectedShipDateChanged','DeliveryAddressChanged')");
                    table.ForeignKey(
                        name: "fk_order_history_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_history_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    product_variant_id = table.Column<long>(type: "bigint", nullable: false),
                    stock_code_snapshot = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    product_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    size_snapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    color_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_sale_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_purchase_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "quantity * unit_sale_price", stored: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.CheckConstraint("chk_order_items_purchase_price_nonneg", "unit_purchase_price >= 0");
                    table.CheckConstraint("chk_order_items_quantity_positive", "quantity > 0");
                    table.CheckConstraint("chk_order_items_sale_price_nonneg", "unit_sale_price >= 0");
                    table.CheckConstraint("chk_order_items_snapshots_not_blank", "btrim(stock_code_snapshot) <> '' AND btrim(product_name_snapshot) <> '' AND btrim(size_snapshot) <> '' AND btrim(color_snapshot) <> ''");
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_plans",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_plans_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_plans_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    payment_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_transactions", x => x.id);
                    table.CheckConstraint("chk_account_transactions_amount_nonzero", "amount <> 0");
                    table.CheckConstraint("chk_account_transactions_type_signature", "(transaction_type = 'Sale' AND amount > 0 AND order_id IS NOT NULL AND payment_id IS NULL)\nOR\n(transaction_type = 'Payment' AND amount < 0 AND payment_id IS NOT NULL AND order_id IS NULL)\nOR\n(transaction_type = 'Reversal' AND (order_id IS NOT NULL OR payment_id IS NOT NULL))\nOR\n(transaction_type = 'Adjustment' AND description IS NOT NULL AND btrim(description) <> '')");
                    table.ForeignKey(
                        name: "fk_account_transactions_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_account_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_account_transactions_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_account_transactions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_variant_id = table.Column<long>(type: "bigint", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    on_hand_delta = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    reserved_delta = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    purchase_order_id = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.CheckConstraint("chk_inventory_movements_manual_reason", "movement_type NOT IN ('ManualIncrease','ManualDecrease','Damaged') OR (reason IS NOT NULL AND btrim(reason) <> '')");
                    table.CheckConstraint("chk_inventory_movements_order_link", "movement_type NOT IN ('Sale','OrderReservation','ReservationReleased') OR order_id IS NOT NULL");
                    table.CheckConstraint("chk_inventory_movements_type_signature", "(movement_type = 'InitialStock'        AND on_hand_delta >  0 AND reserved_delta =  0) OR\n(movement_type = 'Purchase'            AND on_hand_delta >  0 AND reserved_delta =  0) OR\n(movement_type = 'Return'              AND on_hand_delta >  0 AND reserved_delta =  0) OR\n(movement_type = 'ManualIncrease'      AND on_hand_delta >  0 AND reserved_delta =  0) OR\n(movement_type = 'OrderReservation'    AND on_hand_delta =  0 AND reserved_delta >  0) OR\n(movement_type = 'ReservationReleased' AND on_hand_delta =  0 AND reserved_delta <  0) OR\n(movement_type = 'Sale'                AND on_hand_delta <  0 AND reserved_delta <= 0) OR\n(movement_type = 'ManualDecrease'      AND on_hand_delta <  0 AND reserved_delta =  0) OR\n(movement_type = 'Damaged'             AND on_hand_delta <  0 AND reserved_delta =  0)");
                    table.ForeignKey(
                        name: "fk_inventory_movements_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_movements_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: true),
                    new_value = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    changed_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_history", x => x.id);
                    table.CheckConstraint("chk_po_history_event_type", "event_type IN ('Created','StatusChanged','ItemAdded','ItemChanged','ItemRemoved','GoodsReceived','NoteChanged','ExpectedDeliveryDateChanged','SupplierReferenceChanged')");
                    table.ForeignKey(
                        name: "fk_purchase_order_history_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_order_history_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_id = table.Column<long>(type: "bigint", nullable: false),
                    product_variant_id = table.Column<long>(type: "bigint", nullable: false),
                    stock_code_snapshot = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    product_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    size_snapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    color_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    supplier_product_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ordered_quantity = table.Column<int>(type: "integer", nullable: false),
                    received_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    unit_purchase_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "ordered_quantity * unit_purchase_price", stored: true),
                    received_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "received_quantity * unit_purchase_price", stored: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_items", x => x.id);
                    table.CheckConstraint("chk_purchase_order_items_ordered_positive", "ordered_quantity > 0");
                    table.CheckConstraint("chk_purchase_order_items_price_nonneg", "unit_purchase_price >= 0");
                    table.CheckConstraint("chk_purchase_order_items_received_nonneg", "received_quantity >= 0");
                    table.CheckConstraint("chk_purchase_order_items_snapshots_not_blank", "btrim(stock_code_snapshot) <> '' AND btrim(product_name_snapshot) <> '' AND btrim(size_snapshot) <> '' AND btrim(color_snapshot) <> ''");
                    table.ForeignKey(
                        name: "fk_purchase_order_items_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_order_items_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_id = table.Column<long>(type: "bigint", nullable: false),
                    purchase_order_id = table.Column<long>(type: "bigint", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reversed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    reversal_reason = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_payments", x => x.id);
                    table.CheckConstraint("chk_supplier_payments_amount_positive", "amount > 0");
                    table.CheckConstraint("chk_supplier_payments_method", "payment_method IN ('Cash', 'BankTransfer', 'CreditCard', 'Cheque', 'PromissoryNote')");
                    table.CheckConstraint("chk_supplier_payments_reversal_consistency", "(status = 'Active' AND reversed_at IS NULL AND reversed_by_user_id IS NULL AND reversal_reason IS NULL)\nOR\n(status = 'Reversed' AND reversed_at IS NOT NULL AND reversed_by_user_id IS NOT NULL\n    AND reversal_reason IS NOT NULL AND btrim(reversal_reason) <> '')");
                    table.CheckConstraint("chk_supplier_payments_status", "status IN ('Active', 'Reversed')");
                    table.ForeignKey(
                        name: "fk_supplier_payments_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_payments_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_payments_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_payments_users_reversed_by_user_id",
                        column: x => x.reversed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "installments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    installment_number = table.Column<short>(type: "smallint", nullable: false),
                    installment_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_installments", x => x.id);
                    table.CheckConstraint("chk_installments_amount_positive", "amount > 0");
                    table.CheckConstraint("chk_installments_number_positive", "installment_number > 0");
                    table.CheckConstraint("chk_installments_type", "installment_type IN ('DownPayment', 'Regular')");
                    table.ForeignKey(
                        name: "fk_installments_payment_plans_payment_plan_id",
                        column: x => x.payment_plan_id,
                        principalTable: "payment_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    supplier_id = table.Column<long>(type: "bigint", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    purchase_order_id = table.Column<long>(type: "bigint", nullable: true),
                    supplier_payment_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_transactions", x => x.id);
                    table.CheckConstraint("chk_supplier_transactions_amount_nonzero", "amount <> 0");
                    table.CheckConstraint("chk_supplier_transactions_type_signature", "(transaction_type = 'Purchase' AND amount > 0 AND purchase_order_id IS NOT NULL AND supplier_payment_id IS NULL)\nOR\n(transaction_type = 'Payment' AND amount < 0 AND supplier_payment_id IS NOT NULL)\nOR\n(transaction_type = 'Reversal' AND (purchase_order_id IS NOT NULL OR supplier_payment_id IS NOT NULL))\nOR\n(transaction_type = 'Adjustment' AND description IS NOT NULL AND btrim(description) <> '')");
                    table.ForeignKey(
                        name: "fk_supplier_transactions_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_transactions_supplier_payments_supplier_payment_id",
                        column: x => x.supplier_payment_id,
                        principalTable: "supplier_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_transactions_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplier_transactions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    installment_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocations", x => x.id);
                    table.CheckConstraint("chk_payment_allocations_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_allocations_installments_installment_id",
                        column: x => x.installment_id,
                        principalTable: "installments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_transactions_created_by",
                table: "account_transactions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_transactions_customer_created",
                table: "account_transactions",
                columns: new[] { "customer_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_account_transactions_order_id",
                table: "account_transactions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_transactions_payment_id",
                table: "account_transactions",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_created",
                table: "audit_logs",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_city",
                table: "customers",
                column: "city");

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_name",
                table: "customers",
                column: "company_name");

            migrationBuilder.CreateIndex(
                name: "ix_customers_full_name",
                table: "customers",
                column: "full_name");

            migrationBuilder.CreateIndex(
                name: "ix_customers_phone",
                table: "customers",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_installments_due_date",
                table: "installments",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ux_installments_plan_number",
                table: "installments",
                columns: new[] { "payment_plan_id", "installment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_inventory_product_variant_id",
                table: "inventory",
                column: "product_variant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_created_by",
                table: "inventory_movements",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_order_id",
                table: "inventory_movements",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_purchase_order_id",
                table: "inventory_movements",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_type_created",
                table: "inventory_movements",
                columns: new[] { "movement_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_variant_created",
                table: "inventory_movements",
                columns: new[] { "product_variant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_order_history_changed_by_user_id",
                table: "order_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_history_order_id_changed_at",
                table: "order_history",
                columns: new[] { "order_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_product_variant_id",
                table: "order_items",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "ux_order_items_order_variant",
                table: "order_items",
                columns: new[] { "order_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_cancelled_by_user_id",
                table: "orders",
                column: "cancelled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_created_by_user_id",
                table: "orders",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_id",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_date",
                table: "orders",
                column: "order_date");

            migrationBuilder.CreateIndex(
                name: "ix_orders_shipped_by_user_id",
                table: "orders",
                column: "shipped_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_installment_id",
                table: "payment_allocations",
                column: "installment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_payment_id",
                table: "payment_allocations",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_plans_created_by",
                table: "payment_plans",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_payment_plans_order_id",
                table: "payment_plans",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_created_by",
                table: "payments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_customer_date",
                table: "payments",
                columns: new[] { "customer_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_payment_date",
                table: "payments",
                column: "payment_date");

            migrationBuilder.CreateIndex(
                name: "ix_payments_reversed_by_user_id",
                table: "payments",
                column: "reversed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_id",
                table: "product_variants",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_variants_barcode",
                table: "product_variants",
                column: "barcode",
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_product_variants_product_size_color",
                table: "product_variants",
                columns: new[] { "product_id", "size", "color" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_brand",
                table: "products",
                column: "brand");

            migrationBuilder.CreateIndex(
                name: "ix_products_gender",
                table: "products",
                column: "gender");

            migrationBuilder.CreateIndex(
                name: "ix_products_name",
                table: "products",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_products_season",
                table: "products",
                column: "season");

            migrationBuilder.CreateIndex(
                name: "ix_products_supplier_id",
                table: "products",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_products_stock_code",
                table: "products",
                column: "stock_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_po_history_changed_by_user_id",
                table: "purchase_order_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_po_history_order_id_changed_at",
                table: "purchase_order_history",
                columns: new[] { "purchase_order_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_order_id",
                table: "purchase_order_items",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_items_product_variant_id",
                table: "purchase_order_items",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "ux_purchase_order_items_order_variant",
                table: "purchase_order_items",
                columns: new[] { "purchase_order_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_cancelled_by_user_id",
                table: "purchase_orders",
                column: "cancelled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_completed_by_user_id",
                table: "purchase_orders",
                column: "completed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_created_by",
                table: "purchase_orders",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_expected_delivery",
                table: "purchase_orders",
                column: "expected_delivery_date");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_order_date",
                table: "purchase_orders",
                column: "order_date");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_payment_due",
                table: "purchase_orders",
                column: "payment_due_date");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_status",
                table: "purchase_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_supplier_id",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_purchase_orders_number",
                table: "purchase_orders",
                column: "purchase_order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_created_by",
                table: "supplier_payments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_payment_date",
                table: "supplier_payments",
                column: "payment_date");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_purchase_order_id",
                table: "supplier_payments",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_reversed_by_user_id",
                table: "supplier_payments",
                column: "reversed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_supplier_date",
                table: "supplier_payments",
                columns: new[] { "supplier_id", "payment_date" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_transactions_created_by",
                table: "supplier_transactions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_transactions_payment_id",
                table: "supplier_transactions",
                column: "supplier_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_transactions_purchase_order_id",
                table: "supplier_transactions",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_transactions_supplier_created",
                table: "supplier_transactions",
                columns: new[] { "supplier_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_company_name",
                table: "suppliers",
                column: "company_name");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_phone",
                table: "suppliers",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_users_supplier_id",
                table: "users",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ux_users_normalized_username",
                table: "users",
                column: "normalized_username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_transactions");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "order_history");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "purchase_order_history");

            migrationBuilder.DropTable(
                name: "purchase_order_items");

            migrationBuilder.DropTable(
                name: "supplier_transactions");

            migrationBuilder.DropTable(
                name: "installments");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "supplier_payments");

            migrationBuilder.DropTable(
                name: "payment_plans");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropSequence(
                name: "order_number_seq");

            migrationBuilder.DropSequence(
                name: "purchase_order_number_seq");
        }
    }
}
