using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pegasus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IssuedReportVersionEvidenceLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssociationStatus",
                table: "CaseReportSentEvidence",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssociationStatusReason",
                table: "CaseReportSentEvidence",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceArtifactIdentity",
                table: "CaseReportSentEvidence",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceArtifactSha256",
                table: "CaseReportSentEvidence",
                type: "nchar(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceReportVersionId",
                table: "CaseReportSentEvidence",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssociationStatus",
                table: "CaseReportApprovals",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssociationStatusReason",
                table: "CaseReportApprovals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseReportVersionLedgers",
                columns: table => new
                {
                    ReportVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    ConcurrencyToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReportVersionLedgers", x => x.ReportVersionId);
                    table.CheckConstraint("CK_CaseReportVersionLedgers_Case", "[CaseId] IS NOT NULL");
                    table.CheckConstraint("CK_CaseReportVersionLedgers_Version", "[Version] >= 0");
                    table.ForeignKey(
                        name: "FK_CaseReportVersionLedgers_AssessmentReportVersions_ReportVersionId",
                        column: x => x.ReportVersionId,
                        principalTable: "AssessmentReportVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportVersionLedgers_CaseReportApprovals_ApprovalId",
                        column: x => x.ApprovalId,
                        principalTable: "CaseReportApprovals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportVersionLedgers_CaseReportSentEvidence_CurrentEvidenceId",
                        column: x => x.CurrentEvidenceId,
                        principalTable: "CaseReportSentEvidence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseReportVersionLedgers_CaseWorkflows_CaseId",
                        column: x => x.CaseId,
                        principalTable: "CaseWorkflows",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseReportAssociationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerReportVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeforeReportVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AfterReportVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorSubjectId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActorRolesJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OperationKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LedgerVersion = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FormerCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FormerLinkedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FormerLinkedByKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    FormerLinkedBySubjectId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FormerLinkedByRolesJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReportAssociationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseReportAssociationHistory_CaseReportVersionLedgers_LedgerReportVersionId",
                        column: x => x.LedgerReportVersionId,
                        principalTable: "CaseReportVersionLedgers",
                        principalColumn: "ReportVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportSentEvidence_SourceReportVersionId",
                table: "CaseReportSentEvidence",
                column: "SourceReportVersionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CaseReportSentEvidence_SourceReportVersion",
                table: "CaseReportSentEvidence",
                sql: "([SourceReportVersionId] IS NULL AND [SourceArtifactIdentity] IS NULL AND [SourceArtifactSha256] IS NULL) OR ([SourceReportVersionId] IS NOT NULL AND [SourceArtifactIdentity] IS NOT NULL AND [SourceArtifactSha256] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportAssociationHistory_LedgerReportVersionId_OccurredAtUtc_Id",
                table: "CaseReportAssociationHistory",
                columns: new[] { "LedgerReportVersionId", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportAssociationHistory_LedgerReportVersionId_OperationKey",
                table: "CaseReportAssociationHistory",
                columns: new[] { "LedgerReportVersionId", "OperationKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportVersionLedgers_ApprovalId",
                table: "CaseReportVersionLedgers",
                column: "ApprovalId",
                unique: true,
                filter: "[ApprovalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportVersionLedgers_CaseId",
                table: "CaseReportVersionLedgers",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReportVersionLedgers_CurrentEvidenceId",
                table: "CaseReportVersionLedgers",
                column: "CurrentEvidenceId",
                unique: true,
                filter: "[CurrentEvidenceId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseReportSentEvidence_AssessmentReportVersions_SourceReportVersionId",
                table: "CaseReportSentEvidence",
                column: "SourceReportVersionId",
                principalTable: "AssessmentReportVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Existing approval/evidence rows predate immutable report-version identity. Keep
            // their case associations and label them unresolved; no filename, timing or current
            // pointer is authoritative enough to fabricate a version match.
            migrationBuilder.Sql(
                "UPDATE [CaseReportApprovals] SET [AssociationStatus] = N'Unresolved', [AssociationStatusReason] = N'Approval predates the immutable report-version ledger.' WHERE [AssociationStatus] IS NULL;");
            migrationBuilder.Sql(
                "UPDATE [CaseReportSentEvidence] SET [AssociationStatus] = N'Unresolved', [AssociationStatusReason] = N'Sent evidence predates the immutable report-version ledger.' WHERE [AssociationStatus] IS NULL;");
            migrationBuilder.Sql(
                "INSERT INTO [CaseReportVersionLedgers] ([ReportVersionId], [CaseId], [Version], [ConcurrencyToken]) SELECT [Id], [CaseId], 0, NEWID() FROM [AssessmentReportVersions];");

            if (IsSqlServer())
            {
                RequireRuntimeRole(migrationBuilder, "pegasus_web_runtime_role");
                RequireRuntimeRole(migrationBuilder, "pegasus_worker_runtime_role");
                migrationBuilder.Sql(
                    "GRANT SELECT, INSERT, UPDATE ON OBJECT::[dbo].[CaseReportVersionLedgers] TO [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "GRANT SELECT, INSERT ON OBJECT::[dbo].[CaseReportAssociationHistory] TO [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "GRANT SELECT, UPDATE ON OBJECT::[dbo].[CaseReportVersionLedgers] TO [pegasus_worker_runtime_role];");
                migrationBuilder.Sql(
                    "GRANT SELECT, INSERT ON OBJECT::[dbo].[CaseReportAssociationHistory] TO [pegasus_worker_runtime_role];");
                migrationBuilder.Sql(
                    "DENY DELETE ON OBJECT::[dbo].[CaseReportVersionLedgers] TO [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "DENY DELETE ON OBJECT::[dbo].[CaseReportAssociationHistory] TO [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "DENY DELETE ON OBJECT::[dbo].[CaseReportVersionLedgers] TO [pegasus_worker_runtime_role];");
                migrationBuilder.Sql(
                    "DENY DELETE ON OBJECT::[dbo].[CaseReportAssociationHistory] TO [pegasus_worker_runtime_role];");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (IsSqlServer())
            {
                migrationBuilder.Sql(
                    "REVOKE DELETE ON OBJECT::[dbo].[CaseReportVersionLedgers] FROM [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "REVOKE DELETE ON OBJECT::[dbo].[CaseReportAssociationHistory] FROM [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "REVOKE DELETE ON OBJECT::[dbo].[CaseReportVersionLedgers] FROM [pegasus_worker_runtime_role];");
                migrationBuilder.Sql(
                    "REVOKE DELETE ON OBJECT::[dbo].[CaseReportAssociationHistory] FROM [pegasus_worker_runtime_role];");
                migrationBuilder.Sql(
                    "REVOKE SELECT, INSERT, UPDATE ON OBJECT::[dbo].[CaseReportVersionLedgers] FROM [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "REVOKE SELECT, INSERT ON OBJECT::[dbo].[CaseReportAssociationHistory] FROM [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "REVOKE SELECT, UPDATE ON OBJECT::[dbo].[CaseReportVersionLedgers] FROM [pegasus_worker_runtime_role];");
                migrationBuilder.Sql(
                    "REVOKE SELECT, INSERT ON OBJECT::[dbo].[CaseReportAssociationHistory] FROM [pegasus_worker_runtime_role];");
            }

            migrationBuilder.DropForeignKey(
                name: "FK_CaseReportSentEvidence_AssessmentReportVersions_SourceReportVersionId",
                table: "CaseReportSentEvidence");

            migrationBuilder.DropTable(
                name: "CaseReportAssociationHistory");

            migrationBuilder.DropTable(
                name: "CaseReportVersionLedgers");

            migrationBuilder.DropIndex(
                name: "IX_CaseReportSentEvidence_SourceReportVersionId",
                table: "CaseReportSentEvidence");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CaseReportSentEvidence_SourceReportVersion",
                table: "CaseReportSentEvidence");

            migrationBuilder.DropColumn(
                name: "AssociationStatus",
                table: "CaseReportSentEvidence");

            migrationBuilder.DropColumn(
                name: "AssociationStatusReason",
                table: "CaseReportSentEvidence");

            migrationBuilder.DropColumn(
                name: "SourceArtifactIdentity",
                table: "CaseReportSentEvidence");

            migrationBuilder.DropColumn(
                name: "SourceArtifactSha256",
                table: "CaseReportSentEvidence");

            migrationBuilder.DropColumn(
                name: "SourceReportVersionId",
                table: "CaseReportSentEvidence");

            migrationBuilder.DropColumn(
                name: "AssociationStatus",
                table: "CaseReportApprovals");

            migrationBuilder.DropColumn(
                name: "AssociationStatusReason",
                table: "CaseReportApprovals");
        }

        private bool IsSqlServer() =>
            string.Equals(
                ActiveProvider,
                "Microsoft.EntityFrameworkCore.SqlServer",
                StringComparison.Ordinal);

        private static void RequireRuntimeRole(
            MigrationBuilder migrationBuilder,
            string roleName) =>
            migrationBuilder.Sql($"""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.database_principals
                    WHERE name = N'{roleName}'
                      AND [type] = 'R'
                      AND is_fixed_role = 0
                      AND owning_principal_id = DATABASE_PRINCIPAL_ID(N'dbo'))
                    THROW 51000, 'The fixed Pegasus runtime role is missing or invalid.', 1;
                """);
    }
}
