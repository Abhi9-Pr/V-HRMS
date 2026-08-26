using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vespera.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddSalaryStructureMonthlyCtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MonthlyCtc",
                table: "SalaryStructure",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyCtc",
                table: "SalaryStructure");
        }
    }
}
