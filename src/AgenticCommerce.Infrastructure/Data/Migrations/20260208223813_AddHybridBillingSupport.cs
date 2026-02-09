using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticCommerce.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridBillingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stripe_customer_id",
                table: "organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_subscription_id",
                table: "organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_subscription_item_id",
                table: "organizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tier",
                table: "organizations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "environment",
                table: "api_keys",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "usage_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    api_key_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    transaction_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    fee_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    billed = table.Column<bool>(type: "boolean", nullable: false),
                    billed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_usage_events_api_keys_api_key_id",
                        column: x => x.api_key_id,
                        principalTable: "api_keys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_usage_events_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organizations_stripe_customer_id",
                table: "organizations",
                column: "stripe_customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_environment",
                table: "api_keys",
                column: "environment");

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_api_key_id",
                table: "usage_events",
                column: "api_key_id");

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_billed",
                table: "usage_events",
                column: "billed");

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_organization_id",
                table: "usage_events",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_organization_id_billed",
                table: "usage_events",
                columns: new[] { "organization_id", "billed" });

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_recorded_at",
                table: "usage_events",
                column: "recorded_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usage_events");

            migrationBuilder.DropIndex(
                name: "IX_organizations_stripe_customer_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "IX_api_keys_environment",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "stripe_customer_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "stripe_subscription_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "stripe_subscription_item_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "tier",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "environment",
                table: "api_keys");
        }
    }
}
