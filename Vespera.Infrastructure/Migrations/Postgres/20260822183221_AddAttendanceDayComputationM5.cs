using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vespera.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddAttendanceDayComputationM5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EarlyLeaveByMinutes",
                table: "AttendanceDay",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstIn",
                table: "AttendanceDay",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLopCandidate",
                table: "AttendanceDay",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastComputedAt",
                table: "AttendanceDay",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastComputedBy",
                table: "AttendanceDay",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastOut",
                table: "AttendanceDay",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LateByMinutes",
                table: "AttendanceDay",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OvertimeMinutes",
                table: "AttendanceDay",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkedMinutes",
                table: "AttendanceDay",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EarlyLeaveByMinutes",
                table: "AttendanceDay");

            migrationBuilder.DropColumn(
                name: "FirstIn",
                table: "AttendanceDay");

            migrationBuilder.DropColumn(
                name: "IsLopCandidate",
                table: "AttendanceDay");

            migrationBuilder.DropColumn(
                name: "LastComputedAt",
                table: "AttendanceDay");

            migrationBuilder.DropColumn(
                name: "LastComputedBy",
                table: "AttendanceDay");

            migrationBuilder.DropColumn(
                name: "LastOut",
                table: "AttendanceDay");

            migrationBuilder.DropColumn(
                name: "LateByMinutes",
                table: "AttendanceDay");

            migrationBuilder.DropColumn(
                name: "OvertimeMinutes",
                table: "AttendanceDay");

            migrationBuilder.DropColumn(
                name: "WorkedMinutes",
                table: "AttendanceDay");
        }
    }
}
