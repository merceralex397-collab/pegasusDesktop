using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pegasus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentReportGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssessmentReportVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    AssessmentFamily = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AcceptedPayloadSha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    TemplateVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogicalKey = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AcceptedPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PredecessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LeaseId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentReportVersions", x => x.Id);
                    table.CheckConstraint("CK_AssessmentReportVersions_State", "[State] IN ('Rendering', 'Generated', 'Failed')");
                    table.CheckConstraint("CK_AssessmentReportVersions_Version", "[Version] > 0");
                    table.ForeignKey(
                        name: "FK_AssessmentReportVersions_AssessmentReportVersions_PredecessorId",
                        column: x => x.PredecessorId,
                        principalTable: "AssessmentReportVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentReportVersions_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentReportArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentVersion = table.Column<int>(type: "int", nullable: false),
                    DocumentOrdinal = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ContentLength = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    PageCount = table.Column<int>(type: "int", nullable: false),
                    TemplateVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EngineVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentReportArtifacts", x => x.Id);
                    table.CheckConstraint("CK_AssessmentReportArtifacts_Kind", "[Kind] IN ('Assessment', 'FeeNote')");
                    table.CheckConstraint("CK_AssessmentReportArtifacts_Length", "[ContentLength] >= 0");
                    table.CheckConstraint("CK_AssessmentReportArtifacts_Pages", "[PageCount] >= 0");
                    table.ForeignKey(
                        name: "FK_AssessmentReportArtifacts_AssessmentReportVersions_ReportVersionId",
                        column: x => x.ReportVersionId,
                        principalTable: "AssessmentReportVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentReportArtifacts_DocumentOccurrences_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalTable: "DocumentOccurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentReportArtifacts_DocumentVersions_DocumentVersionId",
                        column: x => x.DocumentVersionId,
                        principalTable: "DocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentReportArtifacts_DocumentVersionId",
                table: "AssessmentReportArtifacts",
                column: "DocumentVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentReportArtifacts_OccurrenceId",
                table: "AssessmentReportArtifacts",
                column: "OccurrenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentReportArtifacts_ReportVersionId_Kind",
                table: "AssessmentReportArtifacts",
                columns: new[] { "ReportVersionId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentReportVersions_CaseId_AssessmentFamily_AcceptedPayloadSha256_TemplateVersion",
                table: "AssessmentReportVersions",
                columns: new[] { "CaseId", "AssessmentFamily", "AcceptedPayloadSha256", "TemplateVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentReportVersions_CaseId_CreatedAtUtc",
                table: "AssessmentReportVersions",
                columns: new[] { "CaseId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentReportVersions_CaseId_Version",
                table: "AssessmentReportVersions",
                columns: new[] { "CaseId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentReportVersions_PredecessorId",
                table: "AssessmentReportVersions",
                column: "PredecessorId");

            if (IsSqlServer())
            {
                RequireWebRole(migrationBuilder);
                migrationBuilder.Sql(
                    "GRANT SELECT, INSERT, UPDATE ON OBJECT::[dbo].[AssessmentReportVersions] TO [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "GRANT SELECT, INSERT ON OBJECT::[dbo].[AssessmentReportArtifacts] TO [pegasus_web_runtime_role];");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (IsSqlServer())
            {
                migrationBuilder.Sql(
                    "REVOKE SELECT, INSERT, UPDATE ON OBJECT::[dbo].[AssessmentReportVersions] FROM [pegasus_web_runtime_role];");
                migrationBuilder.Sql(
                    "REVOKE SELECT, INSERT ON OBJECT::[dbo].[AssessmentReportArtifacts] FROM [pegasus_web_runtime_role];");
            }
            migrationBuilder.DropTable(
                name: "AssessmentReportArtifacts");

            migrationBuilder.DropTable(
                name: "AssessmentReportVersions");
        }

        private bool IsSqlServer() =>
            string.Equals(
                ActiveProvider,
                "Microsoft.EntityFrameworkCore.SqlServer",
                StringComparison.Ordinal);

        private static void RequireWebRole(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM sys.database_principals
                    WHERE name = N'pegasus_web_runtime_role'
                      AND [type] = 'R'
                      AND is_fixed_role = 0
                      AND owning_principal_id = DATABASE_PRINCIPAL_ID(N'dbo'))
                    THROW 51000, 'The fixed Pegasus Web runtime role is missing or invalid.', 1;
                """);
    }
}
