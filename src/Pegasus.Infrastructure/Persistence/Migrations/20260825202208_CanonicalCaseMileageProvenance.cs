using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pegasus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CanonicalCaseMileageProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CaseDataFields_FieldName",
                table: "CaseDataFields");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CaseDataFields_FieldName",
                table: "CaseDataFields",
                sql: "[FieldName] IN ('work_provider_code', 'claimant_name', 'claim_number', 'vehicle_registration', 'vehicle_make', 'vehicle_model', 'vehicle_mileage', 'vehicle_mileage_unit', 'vehicle_mileage_kilometres', 'accident_circumstances', 'incident_date', 'contact_name', 'contact_email_address', 'contact_phone_number', 'instruction_date', 'vat_status', 'inspection_date', 'inspection_deadline', 'inspection_address', 'inspection_mode')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CaseDataFields_FieldName",
                table: "CaseDataFields");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CaseDataFields_FieldName",
                table: "CaseDataFields",
                sql: "[FieldName] IN ('work_provider_code', 'claimant_name', 'claim_number', 'vehicle_registration', 'vehicle_make', 'vehicle_model', 'vehicle_mileage', 'vehicle_mileage_unit', 'accident_circumstances', 'incident_date', 'contact_name', 'contact_email_address', 'contact_phone_number', 'instruction_date', 'vat_status', 'inspection_date', 'inspection_deadline', 'inspection_address', 'inspection_mode')");
        }
    }
}
