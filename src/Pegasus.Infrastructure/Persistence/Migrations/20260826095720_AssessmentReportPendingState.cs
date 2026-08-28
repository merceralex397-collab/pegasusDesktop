using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pegasus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentReportPendingState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentReportVersions_State",
                table: "AssessmentReportVersions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentReportVersions_State",
                table: "AssessmentReportVersions",
                sql: "[State] IN ('Pending', 'Rendering', 'Generated', 'Failed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentReportVersions_State",
                table: "AssessmentReportVersions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentReportVersions_State",
                table: "AssessmentReportVersions",
                sql: "[State] IN ('Rendering', 'Generated', 'Failed')");
        }
    }
}
