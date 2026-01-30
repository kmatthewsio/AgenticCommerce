using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticCommerce.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripePurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stripe_purchases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    payment_intent_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    customer_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    product_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    amount_cents = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    refunded = table.Column<bool>(type: "boolean", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    api_key_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_event = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_purchases", x => x.id);
                    table.ForeignKey(
                        name: "FK_stripe_purchases_api_keys_api_key_id",
                        column: x => x.api_key_id,
                        principalTable: "api_keys",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_stripe_purchases_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_stripe_purchases_api_key_id",
                table: "stripe_purchases",
                column: "api_key_id");

            migrationBuilder.CreateIndex(
                name: "IX_stripe_purchases_email",
                table: "stripe_purchases",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_stripe_purchases_organization_id",
                table: "stripe_purchases",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_stripe_purchases_payment_intent_id",
                table: "stripe_purchases",
                column: "payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "IX_stripe_purchases_session_id",
                table: "stripe_purchases",
                column: "session_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stripe_purchases");
        }
    }
}
