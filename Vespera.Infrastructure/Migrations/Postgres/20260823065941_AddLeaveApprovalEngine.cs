using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vespera.Infrastructure.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddLeaveApprovalEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicableGender",
                table: "LeaveType",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEncashable",
                table: "LeaveType",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxEncashableDays",
                table: "LeaveType",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinimumTenureMonths",
                table: "LeaveType",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AccrualFrequency",
                table: "LeavePolicy",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxNegativeBalanceDays",
                table: "LeavePolicy",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinimumTenureMonthsForAccrual",
                table: "LeavePolicy",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NegativeBalancePolicy",
                table: "LeavePolicy",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "AllowWithLop");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresHrApproval",
                table: "LeavePolicy",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresSkipLevelApproval",
                table: "LeavePolicy",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SandwichLeaveEnabled",
                table: "LeavePolicy",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SkipLevelThresholdDays",
                table: "LeavePolicy",
                type: "numeric(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Employee",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApprovalChain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CurrentStepIndex = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalChain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlackoutPeriod",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_BlackoutPeriod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeavePolicyTenureAccrualTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MinimumTenureMonths = table.Column<int>(type: "integer", nullable: false),
                    MonthlyRate = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    LeavePolicyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeavePolicyTenureAccrualTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeavePolicyTenureAccrualTiers_LeavePolicy_LeavePolicyId",
                        column: x => x.LeavePolicyId,
                        principalTable: "LeavePolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Period = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestedDays = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LossOfPayDays = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovalChainId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalChain_ApprovalChainId",
                        column: x => x.ApprovalChainId,
                        principalTable: "ApprovalChain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Direction = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodKey = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    OccurredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LeaveBalanceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveLedgerEntries_LeaveBalance_LeaveBalanceId",
                        column: x => x.LeaveBalanceId,
                        principalTable: "LeaveBalance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalChain_TenantId_SubjectType_SubjectId",
                table: "ApprovalChain",
                columns: new[] { "TenantId", "SubjectType", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalChainId_SequenceNumber",
                table: "ApprovalSteps",
                columns: new[] { "ApprovalChainId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlackoutPeriod_TenantId",
                table: "BlackoutPeriod",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BlackoutPeriod_TenantId_LeaveTypeId",
                table: "BlackoutPeriod",
                columns: new[] { "TenantId", "LeaveTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalance_TenantId_EmployeeId_LeaveTypeId",
                table: "LeaveBalance",
                columns: new[] { "TenantId", "EmployeeId", "LeaveTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveLedgerEntries_LeaveBalanceId_PeriodKey",
                table: "LeaveLedgerEntries",
                columns: new[] { "LeaveBalanceId", "PeriodKey" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveLedgerEntries_LeaveBalanceId_SourceType_SourceId",
                table: "LeaveLedgerEntries",
                columns: new[] { "LeaveBalanceId", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeavePolicyTenureAccrualTiers_LeavePolicyId",
                table: "LeavePolicyTenureAccrualTiers",
                column: "LeavePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequest_TenantId",
                table: "LeaveRequest",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequest_TenantId_EmployeeId",
                table: "LeaveRequest",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequest_TenantId_Status",
                table: "LeaveRequest",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalSteps");

            migrationBuilder.DropTable(
                name: "BlackoutPeriod");

            migrationBuilder.DropTable(
                name: "LeaveLedgerEntries");

            migrationBuilder.DropTable(
                name: "LeavePolicyTenureAccrualTiers");

            migrationBuilder.DropTable(
                name: "LeaveRequest");

            migrationBuilder.DropTable(
                name: "ApprovalChain");

            migrationBuilder.DropTable(
                name: "LeaveBalance");

            migrationBuilder.DropColumn(
                name: "ApplicableGender",
                table: "LeaveType");

            migrationBuilder.DropColumn(
                name: "IsEncashable",
                table: "LeaveType");

            migrationBuilder.DropColumn(
                name: "MaxEncashableDays",
                table: "LeaveType");

            migrationBuilder.DropColumn(
                name: "MinimumTenureMonths",
                table: "LeaveType");

            migrationBuilder.DropColumn(
                name: "AccrualFrequency",
                table: "LeavePolicy");

            migrationBuilder.DropColumn(
                name: "MaxNegativeBalanceDays",
                table: "LeavePolicy");

            migrationBuilder.DropColumn(
                name: "MinimumTenureMonthsForAccrual",
                table: "LeavePolicy");

            migrationBuilder.DropColumn(
                name: "NegativeBalancePolicy",
                table: "LeavePolicy");

            migrationBuilder.DropColumn(
                name: "RequiresHrApproval",
                table: "LeavePolicy");

            migrationBuilder.DropColumn(
                name: "RequiresSkipLevelApproval",
                table: "LeavePolicy");

            migrationBuilder.DropColumn(
                name: "SandwichLeaveEnabled",
                table: "LeavePolicy");

            migrationBuilder.DropColumn(
                name: "SkipLevelThresholdDays",
                table: "LeavePolicy");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Employee");
        }
    }
}
