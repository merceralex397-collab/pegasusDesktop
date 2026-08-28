using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pegasus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentReportRetryPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "AssessmentReportVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextAttemptAtUtc",
                table: "AssessmentReportVersions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentReportVersions_AttemptCount",
                table: "AssessmentReportVersions",
                sql: "[AttemptCount] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentReportVersions_AttemptCount",
                table: "AssessmentReportVersions");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "AssessmentReportVersions");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "AssessmentReportVersions");
        }
    }
}
