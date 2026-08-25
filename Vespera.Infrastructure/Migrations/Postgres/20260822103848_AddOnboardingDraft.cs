using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vespera.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddOnboardingDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                table: "EmployeeDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedBy",
                table: "EmployeeDocuments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedFieldsJson",
                table: "EmployeeDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOcrConfirmed",
                table: "EmployeeDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "OcrConfidence",
                table: "EmployeeDocuments",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrSuggestedFieldsJson",
                table: "EmployeeDocuments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanStatus",
                table: "EmployeeDocuments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentAnnualCtc",
                table: "Employee",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OnboardingDraft",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    WorkEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DesignationId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateOfJoining = table.Column<DateOnly>(type: "date", nullable: true),
                    ConvertedEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_OnboardingDraft", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingDraftConsentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Granted = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WithdrawnAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OnboardingDraftId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingDraftConsentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingDraftConsentRecords_OnboardingDraft_OnboardingDra~",
                        column: x => x.OnboardingDraftId,
                        principalTable: "OnboardingDraft",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingDraftDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FileReference = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ScanStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OcrSuggestedFieldsJson = table.Column<string>(type: "text", nullable: true),
                    OcrConfidence = table.Column<double>(type: "double precision", nullable: true),
                    IsOcrConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmedFieldsJson = table.Column<string>(type: "text", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OnboardingDraftId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingDraftDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingDraftDocuments_OnboardingDraft_OnboardingDraftId",
                        column: x => x.OnboardingDraftId,
                        principalTable: "OnboardingDraft",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingDraft_TenantId",
                table: "OnboardingDraft",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingDraftConsentRecords_OnboardingDraftId",
                table: "OnboardingDraftConsentRecords",
                column: "OnboardingDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingDraftDocuments_OnboardingDraftId",
                table: "OnboardingDraftDocuments",
                column: "OnboardingDraftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnboardingDraftConsentRecords");

            migrationBuilder.DropTable(
                name: "OnboardingDraftDocuments");

            migrationBuilder.DropTable(
                name: "OnboardingDraft");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "ConfirmedBy",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "ConfirmedFieldsJson",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "IsOcrConfirmed",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "OcrConfidence",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "OcrSuggestedFieldsJson",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "ScanStatus",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "CurrentAnnualCtc",
                table: "Employee");
        }
    }
}
