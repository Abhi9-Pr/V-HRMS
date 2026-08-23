using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vespera.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddBiometricDeviceIngestionM7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BiometricDeviceUserId",
                table: "Employee",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BiometricDevice",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Host = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    ApiKeyConfigurationKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Cursor = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricDevice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BiometricIngestionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BiometricDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalRecordId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricIngestionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuarantinedBiometricPunch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BiometricDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PunchedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExternalRecordId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResolvedEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuarantinedBiometricPunch", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevice_LocationId",
                table: "BiometricDevice",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevice_TenantId",
                table: "BiometricDevice",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricIngestionRecords_BiometricDeviceId_ExternalRecordId",
                table: "BiometricIngestionRecords",
                columns: new[] { "BiometricDeviceId", "ExternalRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuarantinedBiometricPunch_BiometricDeviceId_ExternalRecordId",
                table: "QuarantinedBiometricPunch",
                columns: new[] { "BiometricDeviceId", "ExternalRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuarantinedBiometricPunch_TenantId",
                table: "QuarantinedBiometricPunch",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantinedBiometricPunch_TenantId_Status",
                table: "QuarantinedBiometricPunch",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BiometricDevice");

            migrationBuilder.DropTable(
                name: "BiometricIngestionRecords");

            migrationBuilder.DropTable(
                name: "QuarantinedBiometricPunch");

            migrationBuilder.DropColumn(
                name: "BiometricDeviceUserId",
                table: "Employee");
        }
    }
}
