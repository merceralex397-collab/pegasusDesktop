using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pegasus.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PegasusDbContext))]
[Migration("20260828074800_GrantWorkerCaseReportVersionLedgerInsert")]
public sealed class GrantWorkerCaseReportVersionLedgerInsert : Migration
{
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";
    private const string WorkerRole = "pegasus_worker_runtime_role";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (!IsSqlServer())
        {
            return;
        }

        RequireManagedRole(migrationBuilder);
        migrationBuilder.Sql(
            $"GRANT INSERT ON OBJECT::[dbo].[CaseReportVersionLedgers] TO [{WorkerRole}];");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (!IsSqlServer())
        {
            return;
        }

        RequireManagedRole(migrationBuilder);
        migrationBuilder.Sql(
            $"REVOKE INSERT ON OBJECT::[dbo].[CaseReportVersionLedgers] FROM [{WorkerRole}];");
    }

    private bool IsSqlServer() =>
        string.Equals(ActiveProvider, SqlServerProvider, StringComparison.Ordinal);

    private static void RequireManagedRole(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql($"""
            IF NOT EXISTS (
                SELECT 1 FROM sys.database_principals
                WHERE name = N'{WorkerRole}'
                  AND [type] = 'R'
                  AND is_fixed_role = 0
                  AND owning_principal_id = DATABASE_PRINCIPAL_ID(N'dbo'))
                THROW 51000, 'The fixed Pegasus Worker runtime role is missing or invalid.', 1;
            """);
}
