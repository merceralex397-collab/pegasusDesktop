using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Pegasus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PegasusDbContext))]
    [Migration("20260828052825_GrantWebApprovedSentPollOutcomeUpdate")]
    public sealed class GrantWebApprovedSentPollOutcomeUpdate : Migration
    {
        private const string WebRole = "pegasus_web_runtime_role";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!string.Equals(
                    ActiveProvider,
                    "Microsoft.EntityFrameworkCore.SqlServer",
                    StringComparison.Ordinal))
            {
                return;
            }

            RequireWebRole(migrationBuilder);
            migrationBuilder.Sql(
                $"GRANT UPDATE ON OBJECT::[dbo].[ApprovedSentPollOutcomes] TO [{WebRole}];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!string.Equals(
                    ActiveProvider,
                    "Microsoft.EntityFrameworkCore.SqlServer",
                    StringComparison.Ordinal))
            {
                return;
            }

            RequireWebRole(migrationBuilder);
            migrationBuilder.Sql(
                $"REVOKE UPDATE ON OBJECT::[dbo].[ApprovedSentPollOutcomes] FROM [{WebRole}];");
        }

        private static void RequireWebRole(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                $"""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.database_principals
                    WHERE name = N'{WebRole}'
                      AND [type] = 'R'
                      AND is_fixed_role = 0
                      AND owning_principal_id = DATABASE_PRINCIPAL_ID(N'dbo'))
                    THROW 51000, 'The Pegasus Web runtime role is missing or invalid.', 1;
                """);
    }
}
