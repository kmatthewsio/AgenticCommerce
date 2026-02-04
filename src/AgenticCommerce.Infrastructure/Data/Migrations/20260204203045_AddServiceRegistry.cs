using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgenticCommerce.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_registry",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    owner_wallet = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price_usdc = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_registry", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_created_at",
                table: "service_registry",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_owner_wallet",
                table: "service_registry",
                column: "owner_wallet");

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_service_url",
                table: "service_registry",
                column: "service_url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_registry_verified",
                table: "service_registry",
                column: "verified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_registry");
        }
    }
}
