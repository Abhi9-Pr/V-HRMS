using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vespera.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddOffboardingChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OffboardingChecklist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AccessRevokedStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AccessRevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssetsRecoveredStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AssetsRecoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalSettlementStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FinalSettlementAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_OffboardingChecklist", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingChecklist_TenantId",
                table: "OffboardingChecklist",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingChecklist_TenantId_EmployeeId",
                table: "OffboardingChecklist",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OffboardingChecklist");
        }
    }
}
