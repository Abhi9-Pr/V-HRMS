using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vespera.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddPayrollEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DryRunExecutedBy",
                table: "PayrollRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FreezeOverriddenBy",
                table: "PayrollRun",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FreezeOverrideReason",
                table: "PayrollRun",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvestmentDeclaration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxRegimeVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentDeclaration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentDeclarationWindow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OpenFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    LockAt = table.Column<DateOnly>(type: "date", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentDeclarationWindow", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceFreezeDay = table.Column<int>(type: "integer", nullable: false),
                    VarianceThresholdPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payslip",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetPay = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DocumentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payslip", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryComponent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComponentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsTaxable = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_SalaryComponent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryStructure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryStructure", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxRegimeVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegimeType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRegimeVersion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentDeclarationLines",
                columns: table => new
                {
                    InvestmentDeclarationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Section = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProofFileReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReviewStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReviewComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentDeclarationLines", x => new { x.InvestmentDeclarationId, x.Id });
                    table.ForeignKey(
                        name: "FK_InvestmentDeclarationLines_InvestmentDeclaration_Investment~",
                        column: x => x.InvestmentDeclarationId,
                        principalTable: "InvestmentDeclaration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayslipComponentLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ComponentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Amount = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayslipId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayslipComponentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipComponentLines_Payslip_PayslipId",
                        column: x => x.PayslipId,
                        principalTable: "Payslip",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryStructureLines",
                columns: table => new
                {
                    SalaryStructureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComponentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Formula = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryStructureLines", x => new { x.SalaryStructureId, x.Id });
                    table.ForeignKey(
                        name: "FK_SalaryStructureLines_SalaryStructure_SalaryStructureId",
                        column: x => x.SalaryStructureId,
                        principalTable: "SalaryStructure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxSlabs",
                columns: table => new
                {
                    TaxRegimeVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UpTo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RatePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxSlabs", x => new { x.TaxRegimeVersionId, x.Id });
                    table.ForeignKey(
                        name: "FK_TaxSlabs_TaxRegimeVersion_TaxRegimeVersionId",
                        column: x => x.TaxRegimeVersionId,
                        principalTable: "TaxRegimeVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentDeclaration_TenantId_EmployeeId_FinancialYear",
                table: "InvestmentDeclaration",
                columns: new[] { "TenantId", "EmployeeId", "FinancialYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentDeclarationWindow_TenantId_FinancialYear",
                table: "InvestmentDeclarationWindow",
                columns: new[] { "TenantId", "FinancialYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettings_TenantId",
                table: "PayrollSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payslip_TenantId_PayrollRunId_EmployeeId",
                table: "Payslip",
                columns: new[] { "TenantId", "PayrollRunId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayslipComponentLines_PayslipId",
                table: "PayslipComponentLines",
                column: "PayslipId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryComponent_TenantId",
                table: "SalaryComponent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryComponent_TenantId_Name",
                table: "SalaryComponent",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructure_TenantId",
                table: "SalaryStructure",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructure_TenantId_EmployeeId",
                table: "SalaryStructure",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRegimeVersion_TenantId_RegimeType_FinancialYear",
                table: "TaxRegimeVersion",
                columns: new[] { "TenantId", "RegimeType", "FinancialYear" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestmentDeclarationLines");

            migrationBuilder.DropTable(
                name: "InvestmentDeclarationWindow");

            migrationBuilder.DropTable(
                name: "PayrollSettings");

            migrationBuilder.DropTable(
                name: "PayslipComponentLines");

            migrationBuilder.DropTable(
                name: "SalaryComponent");

            migrationBuilder.DropTable(
                name: "SalaryStructureLines");

            migrationBuilder.DropTable(
                name: "TaxSlabs");

            migrationBuilder.DropTable(
                name: "InvestmentDeclaration");

            migrationBuilder.DropTable(
                name: "Payslip");

            migrationBuilder.DropTable(
                name: "SalaryStructure");

            migrationBuilder.DropTable(
                name: "TaxRegimeVersion");

            migrationBuilder.DropColumn(
                name: "DryRunExecutedBy",
                table: "PayrollRun");

            migrationBuilder.DropColumn(
                name: "FreezeOverriddenBy",
                table: "PayrollRun");

            migrationBuilder.DropColumn(
                name: "FreezeOverrideReason",
                table: "PayrollRun");
        }
    }
}
