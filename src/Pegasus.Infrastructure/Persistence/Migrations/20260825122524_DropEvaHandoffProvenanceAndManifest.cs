using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pegasus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropEvaHandoffProvenanceAndManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This is schema-reversible but data-destructive: Up removes historical
            // manifest/provenance values, and Down can recreate only empty/default columns.
            // Do not roll an older application back behind this migration.
            migrationBuilder.DropColumn(
                name: "ManifestContent",
                table: "EvaHandoffRevisions");

            migrationBuilder.DropColumn(
                name: "ProvenanceContent",
                table: "EvaHandoffRevisions");

            migrationBuilder.DropColumn(
                name: "ProvenanceSha256",
                table: "EvaHandoffRevisions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ManifestContent",
                table: "EvaHandoffRevisions",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<byte[]>(
                name: "ProvenanceContent",
                table: "EvaHandoffRevisions",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.AddColumn<string>(
                name: "ProvenanceSha256",
                table: "EvaHandoffRevisions",
                type: "nchar(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}
