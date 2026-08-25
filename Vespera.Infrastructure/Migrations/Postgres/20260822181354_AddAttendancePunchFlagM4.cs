using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vespera.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddAttendancePunchFlagM4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "AttendancePunches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "AttendancePunches",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "AttendancePunches");

            migrationBuilder.DropColumn(
                name: "RequiresApproval",
                table: "AttendancePunches");
        }
    }
}
