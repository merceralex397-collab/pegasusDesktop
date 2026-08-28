---
id: PLAT-031
type: ticket
title: Grant Worker INSERT on CaseReportVersionLedgers exposed by PLAT-018
status: preparing
area: platform-operations
assignee: ''
profile: fix
labels:
  - desktop-conversion
  - plan-10
  - phase-8
  - runtime-grants
  - grant-only-migration
groups:
  - EPIC-011
  - HZN-003
links:
  - PLAT-018
blocks:
  - PLAT-018
refs:
  - docs/desktop/10-security-observability-performance/README.md
  - docs/current-architecture.md
docs_todo: true
archived: false
created: '2026-08-28T07:45:21.624Z'
updated: '2026-08-28T07:45:21.624Z'
---

## What

Add the missing Worker runtime-role `INSERT` grant for `dbo.CaseReportVersionLedgers`, which the PLAT-018 composition-root gate exposed from `EfCaseWorkflowStore`.

## Why

PLAT-018 must compare every composition-root write against the migration grant matrix. Its exact combined validation identifies a genuine missing permission: Worker inserts `CaseReportVersionLedgers`, while migration `20260827231948_IssuedReportVersionEvidenceLedger` grants the Worker role only `SELECT, UPDATE`. Tests run with full privilege and therefore do not expose this deployed least-privilege failure.

## Acceptance criteria

- [ ] A new, narrow, SQL Server guarded migration grants `INSERT` on `dbo.CaseReportVersionLedgers` to `pegasus_worker_runtime_role`.
- [ ] The migration Down path revokes exactly the permission it adds and follows the repository's runtime-role migration conventions.
- [ ] The migration grant census and the PLAT-018 composition-root gate both pass with the new grant present.
- [ ] No production code, existing migration, grant script, CI job, cloud resource, deployment, credential, upstream remote, or corpus content is changed.
- [ ] Independent review confirms the scope, permission, rollback, and evidence.

## Verification

- `dotnet restore Pegasus.slnx --locked-mode`
- `dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false`
- `pwsh ./scripts/Test-MigrationGrants.ps1`
- Relevant exact-head CI is green before merge.
- Combined exact-head validation includes PLAT-018 and PLAT-030.

## Guardrails

This is a grant-only migration ticket. Do not alter the existing migration; add one new migration with the next repository timestamp/name convention. No Azure or deployment write is permitted. Work only in the configured `pegasusDesktop` repository and remote; never sync upstream.
