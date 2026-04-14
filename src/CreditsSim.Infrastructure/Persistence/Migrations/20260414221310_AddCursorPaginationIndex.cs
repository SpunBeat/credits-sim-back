using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditsSim.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCursorPaginationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_simulation_history_created_at_id",
                table: "simulation_history",
                columns: new[] { "created_at", "id" },
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_simulation_history_created_at_id",
                table: "simulation_history");
        }
    }
}
