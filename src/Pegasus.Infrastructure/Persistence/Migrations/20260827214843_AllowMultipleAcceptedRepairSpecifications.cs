using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pegasus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleAcceptedRepairSpecifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CaseRepairSpecifications_CaseId",
                table: "CaseRepairSpecifications");

            migrationBuilder.CreateIndex(
                name: "IX_CaseRepairSpecifications_CaseId",
                table: "CaseRepairSpecifications",
                column: "CaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CaseRepairSpecifications_CaseId",
                table: "CaseRepairSpecifications");

            migrationBuilder.CreateIndex(
                name: "IX_CaseRepairSpecifications_CaseId",
                table: "CaseRepairSpecifications",
                column: "CaseId",
                unique: true,
                filter: "[State] = 'Accepted'");
        }
    }
}
