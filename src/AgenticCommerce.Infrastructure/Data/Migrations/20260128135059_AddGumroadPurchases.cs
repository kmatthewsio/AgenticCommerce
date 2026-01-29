using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticCommerce.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGumroadPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gumroad_purchases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    license_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    price_cents = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    refunded = table.Column<bool>(type: "boolean", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    api_key_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_payload = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gumroad_purchases", x => x.id);
                    table.ForeignKey(
                        name: "FK_gumroad_purchases_api_keys_api_key_id",
                        column: x => x.api_key_id,
                        principalTable: "api_keys",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_gumroad_purchases_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_gumroad_purchases_api_key_id",
                table: "gumroad_purchases",
                column: "api_key_id");

            migrationBuilder.CreateIndex(
                name: "IX_gumroad_purchases_email",
                table: "gumroad_purchases",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_gumroad_purchases_license_key",
                table: "gumroad_purchases",
                column: "license_key");

            migrationBuilder.CreateIndex(
                name: "IX_gumroad_purchases_organization_id",
                table: "gumroad_purchases",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_gumroad_purchases_sale_id",
                table: "gumroad_purchases",
                column: "sale_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gumroad_purchases");
        }
    }
}
