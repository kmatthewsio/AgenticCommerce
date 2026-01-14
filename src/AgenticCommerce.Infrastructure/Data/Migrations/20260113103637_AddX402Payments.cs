using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgenticCommerce.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddX402Payments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "x402_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    network = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount_usdc = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    amount_smallest_unit = table.Column<long>(type: "bigint", nullable: false),
                    payer_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recipient_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    transaction_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    client_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    settled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_x402_payments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_x402_payments_created_at",
                table: "x402_payments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_x402_payments_network",
                table: "x402_payments",
                column: "network");

            migrationBuilder.CreateIndex(
                name: "IX_x402_payments_payer_address",
                table: "x402_payments",
                column: "payer_address");

            migrationBuilder.CreateIndex(
                name: "IX_x402_payments_payment_id",
                table: "x402_payments",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_x402_payments_status",
                table: "x402_payments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_x402_payments_transaction_hash",
                table: "x402_payments",
                column: "transaction_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "x402_payments");
        }
    }
}
