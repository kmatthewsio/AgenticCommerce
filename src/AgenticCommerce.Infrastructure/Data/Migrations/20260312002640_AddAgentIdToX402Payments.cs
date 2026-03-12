using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgenticCommerce.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentIdToX402Payments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agent_id",
                table: "x402_payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nonce",
                table: "x402_payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_x402_payments_agent_id",
                table: "x402_payments",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_x402_payments_nonce_network",
                table: "x402_payments",
                columns: new[] { "nonce", "network" },
                unique: true,
                filter: "nonce IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_x402_payments_agent_id",
                table: "x402_payments");

            migrationBuilder.DropIndex(
                name: "IX_x402_payments_nonce_network",
                table: "x402_payments");

            migrationBuilder.DropColumn(
                name: "agent_id",
                table: "x402_payments");

            migrationBuilder.DropColumn(
                name: "nonce",
                table: "x402_payments");
        }
    }
}
