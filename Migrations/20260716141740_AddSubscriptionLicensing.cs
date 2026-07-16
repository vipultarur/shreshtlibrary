using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApplication1.Migrations
{
    public partial class AddSubscriptionLicensing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_payment_settings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    merchant_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    upi_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    qr_code_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bank_account = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    account_holder = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ifsc = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_instructions = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_payment_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "platform_subscription_plans",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    monthly_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    quarterly_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    half_yearly_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    yearly_price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    max_students = table.Column<int>(type: "integer", nullable: false),
                    max_staff = table.Column<int>(type: "integer", nullable: false),
                    features = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_recommended = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_subscription_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "library_subscriptions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan_id = table.Column<long>(type: "bigint", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_library_subscriptions_platform_subscription_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "platform_subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "library_payments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plan_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    utr_number = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    screenshot_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    approved_by_id = table.Column<long>(type: "bigint", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_library_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_library_payments_accounts_adminuser_approved_by_id",
                        column: x => x.approved_by_id,
                        principalTable: "accounts_adminuser",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_library_payments_platform_subscription_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "platform_subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_library_payments_approved_by_id",
                table: "library_payments",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_library_payments_plan_id",
                table: "library_payments",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_library_subscriptions_plan_id",
                table: "library_subscriptions",
                column: "plan_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "library_payments");

            migrationBuilder.DropTable(
                name: "library_subscriptions");

            migrationBuilder.DropTable(
                name: "platform_payment_settings");

            migrationBuilder.DropTable(
                name: "platform_subscription_plans");
        }
    }
}
