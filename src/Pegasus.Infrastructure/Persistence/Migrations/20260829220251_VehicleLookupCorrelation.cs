using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pegasus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VehicleLookupCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "VehicleLookupRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [VehicleLookupRequests] " +
                "SET [CorrelationId] = CONCAT(N'vehicle-lookup:migrated:', CONVERT(nvarchar(36), [WorkItemId])) " +
                "WHERE [CorrelationId] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "VehicleLookupRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "VehicleLookupRequests");
        }
    }
}
