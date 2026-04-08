using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditsSim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "simulation_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    term_months = table.Column<int>(type: "integer", nullable: false),
                    annual_rate = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    installment_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    schedule_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_simulation_history", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "simulation_history");
        }
    }
}
