---
id: TEST-003
type: ticket
title: >-
  DSK-08-03 · Extend the `Pegasus.IntegrationTests` shards with `/api/v1`
  persistence paths; keep `-VerifyPartition` green
status: review
area: testing
assignee: codex-mcp-client
profile: fix
stageEntered:
  preparing: '2026-08-24T21:34:13.245Z'
  review: '2026-08-30T07:38:27.148Z'
taken_at: '2026-08-30T06:45:01.262Z'
branch: task/dsk-08-03-integration-shard-persistence
worktree: >-
  C:\Users\PC\Documents\GitHub\pegasus-worktrees\dsk-08-03-integration-shard-persistence
labels:
  - desktop-conversion
  - plan-08
  - phase-2
  - tier-4
  - tier-5
groups:
  - EPIC-009
  - HZN-003
links: []
docs_todo: true
commits:
  - f85e5236a27bfbda91278569ef46743776fc3160
prs:
  - '56'
archived: false
created: '2026-08-24T07:46:12.580Z'
updated: '2026-08-30T08:43:08.698Z'
---

## What

Add the `/api/v1` persistence coverage to `tests/Pegasus.IntegrationTests` — transactions, audit rows, outbox/work items, leases and concurrency on the real LocalDB schema — so that the new tests land in exactly one of the three CI shards and `scripts/Invoke-TestShard.ps1 -VerifyPartition` stays green.

## Why

Proposal §22.2 ("Server integration tests") wants migrations, transactions, audit, outbox/work items and concurrent users proved against an isolated test database. The contract tests of [[DSK-08-01]] and [[DSK-08-02]] boot a host but do not prove what was written. The existing integration project already owns LocalDB persistence evidence (tier 4) and is sharded three ways in CI; adding `/api/v1` tests carelessly is the failure the plan calls *shard partition drift* — a test that runs in two shards, or in none, while the lane still reports success because `dotnet test` exits 0 when a filter matches nothing.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-03`
- Plan detail: `docs/desktop/08-testing/README.md` § 4 (target state row "Server integration") and § 7 (shard partition drift; `Category` traits; LocalDB is Windows-only)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "Server integration tests"
- Repository evidence:
  - `scripts/Invoke-TestShard.ps1:1-49` — shards are assigned from the project's own enumerated test list, whole classes together; `-VerifyPartition` rejects a run whose shards do not reassemble into the enumerated set
  - `.github/workflows/ci.yml` job `sql-integration` — matrix `shard: [1, 2, 3]`, filter `Category!=Corpus&Category!=Browser`, artifact `test-shard-<n>`; job `sql-integration-coverage` runs `-VerifyPartition -ShardCount 3` on ubuntu
  - `tests/Pegasus.IntegrationTests/xunit.runner.json` — `maxParallelThreads: 4`
  - `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` — `IntakeWebApplicationFactory` and the disposable LocalDB database it owns
  - `docs/runbook.md` § Locked restore, build, and test — the template-database mechanism and the `Pegasus_Test_*` sweep
- Binding decisions:
  - L-02 — persistence evidence is LocalDB only; Azure SQL locking, throttling and restore are pilot-ring checks, not this ticket's.
- Depends on: `DSK-08-02` — the command coverage table names the commands whose persisted effects this ticket asserts.

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `run-tests` (`dotnet/skills` `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `fix`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-03` and § 7, then `scripts/Invoke-TestShard.ps1` in full. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Load `pegasus-desktop`, then `run-tests`. Record the current baseline: `pwsh ./scripts/Invoke-TestShard.ps1 -Project ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -Filter "Category!=Corpus&Category!=Browser" -Shard 1 -ShardCount 3 -ListOnly` and note the enumerated count for each shard, so drift caused by this ticket is measurable.
3. Create `tests/Pegasus.IntegrationTests/ApiV1/` and add one test class per `/api/v1` route group that persists (cases, received, uploads, mail, vehicle, assessment, administration). Each class carries `[Trait("Category", "SqlServer")]`, matching `tests/Pegasus.IntegrationTests/AdministrationPolicyPersistenceTests.cs:10`.
4. Reuse `IntakeWebApplicationFactory` (`tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26`) rather than adding a second factory; add the `Features:DesktopGateway=true` switch to it if it is not already configurable, keeping the existing constructors' behaviour unchanged.
5. For each command endpoint that writes, assert the persisted result, not the response alone: the row and its columns, the action-history actor, the outbox/work item where one is enqueued, and the version increment. This is what makes the evidence tier 4 rather than tier 5.
6. Add concurrency tests for the lease and version paths: two callers, second gets `409`, database shows exactly one write. Use the existing disposable-database collection so the class stays pinned to one shard.
7. Keep every new class self-contained — the shard assigner groups whole classes (`scripts/Invoke-TestShard.ps1` `Get-TestClass`), so a test that depends on another class's state will break when the two land in different shards.
8. Run each shard locally: `pwsh ./scripts/Invoke-TestShard.ps1 -Project ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -Filter "Category!=Corpus&Category!=Browser" -Shard <n> -ShardCount 3` for n = 1, 2, 3. Done when each shard's executed count equals its assigned count (the script throws otherwise).
9. Run `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3`. Done when it reports the shards reassemble into exactly the enumerated set.
10. Confirm shard wall-clock time is still inside the `sql-integration` job's `timeout-minutes: 20`; if a shard exceeds about 15 minutes, say so in the post-implementation report rather than raising the timeout silently.
11. Verify the template-database path still engages (`LocalDbTemplateDatabaseTests` green) and that no stray `Pegasus_Test_*` database or `.bak` is left behind after the run.
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [x] Every new integration test appears in exactly one shard.
- [x] `-VerifyPartition` passes with `-ShardCount 3`.
- [x] The LocalDB template-database backup path is still used (no per-test migration fallback).
- [x] Each `/api/v1` write command asserts its persisted row, actor and version increment.
- [x] No new test class depends on another class's database state.

## Verification

- [x] `pwsh ./scripts/Invoke-TestShard.ps1 -Project ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -Filter "Category!=Corpus&Category!=Browser" -Shard 1 -ShardCount 3` (then 2, then 3) — expected: exit 0, executed count equals assigned count for each shard.
- [x] `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3` — expected: exit 0, every enumerated test attributed to exactly one shard.
- [x] `sqlcmd` census from `docs/runbook.md` § Locked restore, build, and test — expected: no `Pegasus_Test_*` database left attached from this run.

## Evidence tier

Tiers 4 and 5 — LocalDB persistence, and Web/API caller. It obliges committed-migration schema evidence with transactions, action-history atomicity, allocation, leases, stale versions and concurrency, observed through the real `/api/v1` route.

## Documentation changes

- `docs/desktop/08-testing/README.md` § 4 — mark the "Server integration" row as extended to `/api/v1`.
- Updated the DSK-08-03 work-breakdown row with retained-mail persistence, lease, audit, and concurrent-conflict coverage.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create and edit `tests/Pegasus.IntegrationTests/**`. Must not edit `scripts/Invoke-TestShard.ps1` or the `sql-integration` matrix — if the partition cannot hold, that is a finding for [[DSK-08-13]], not a script change here.
- **Traps**: shard partition drift — `dotnet test` exits 0 when a filter matches nothing, so `-VerifyPartition` is the only real gate. LocalDB is Windows-only; these tests cannot run on the Linux lanes. `maxParallelThreads: 4` bounds concurrent restores against one LocalDB instance — do not raise it. Never fabricate domain data.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
