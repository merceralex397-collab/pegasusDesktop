---
id: PLAT-031
type: ticket
title: Grant Worker INSERT on CaseReportVersionLedgers exposed by PLAT-018
status: done
area: platform-operations
assignee: codex-mcp-client
profile: fix
stageEntered:
  review: '2026-08-28T07:56:28.928Z'
  verifying: '2026-08-28T17:56:37.062Z'
  done: '2026-08-28T21:04:57.885Z'
taken_at: '2026-08-28T07:46:10.698Z'
branch: task/plat-031-worker-case-report-ledger-grant
worktree: ../pegasus-worktrees/plat-031-worker-case-report-ledger-grant
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
commits:
  - 0ab518e3
  - c97e8e1db774b8b7d6c38ac2fcc24520d27a1150
  - 6d5ee8e4fb14b711fe8f00f2936bf1ce4fc2dc52
prs:
  - '38'
archived: false
created: '2026-08-28T07:45:21.624Z'
updated: '2026-08-28T21:05:08.823Z'
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


## Outcome

PR #38 (`https://github.com/merceralex397-collab/pegasusDesktop/pull/38`) merged the Worker `INSERT` grant into `dev` at `6d5ee8e4fb14b711fe8f00f2936bf1ce4fc2dc52` on 2026-08-28 and it is included in `main` at `28ba13a4fcdb51270b24a48725d53b1de5bcae87`. Merged-main proof is recorded in `proof.md`; no cloud or deployment operation was performed. PLAT-018's runtime-grant gate now has the Worker ledger grant covered.
