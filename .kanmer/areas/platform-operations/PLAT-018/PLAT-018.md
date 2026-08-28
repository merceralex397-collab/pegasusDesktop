---
id: PLAT-018
type: ticket
title: >-
  DSK-10-18 · PLAT-035 carry-over: gate that every table a composition root
  writes has its runtime-role grant
status: implementing
area: platform-operations
assignee: codex-mcp-client
profile: fix
stageEntered:
  preparing: '2026-08-24T21:21:15.725Z'
taken_at: '2026-08-28T02:13:56.478Z'
branch: task/dsk-10-18-runtime-grant-composition-gate
worktree: ../pegasus-worktrees/dsk-10-18-runtime-grant-composition-gate
labels:
  - desktop-conversion
  - plan-10
  - phase-2
  - tier-11
groups:
  - EPIC-011
  - HZN-003
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:16:25.644Z'
updated: '2026-08-28T02:13:56.478Z'
---

## What

Add an automated check that every table a composition root actually writes is granted to that host's runtime role, so a gateway table added for the desktop cannot ship ungranted. The check must fail on an ungranted write and must cover the three regressions that already shipped.

## Why

`docs/current-architecture.md:176-183` records the defect precisely: the least-privilege grant matrix is the one list of what Web and Worker may touch, and nothing verifies it against the stores each composition root registers. Tests and LocalDB runs are full-privilege, so **the suite is green while the deployed estate refuses the write**. This has shipped three times — `20260814092852`, `20260821095500` and `20260822044425`, the last of which broke case custody for every case created after release 17 (PLAT-035, open). The desktop conversion adds gateway tables from Phase 2 onward (the minimum client version setting in `DSK-04-06` is the first), so the gate must exist before those tables ship. Operator-visible consequence: a case save fails in production with a SQL permission error while every test is green. Siblings: [[DSK-10-01]] (register row), [[DSK-10-05]] (authorization at the API layer).

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-18`
- Plan detail: same file § 2 (Facts — "Database least privilege … the untested gap is PLAT-035"), § 7 ("Runtime-role grants missing on new tables (shipped three times)")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 17.1 Required controls `:1153-1172` (least-privilege service identities); § 10.1 Why retain an API `:528-545`
- Repository evidence:
  - `scripts/Test-MigrationGrants.ps1:1-60` — the existing static check: a table is satisfied when **any** migration file contains a `GRANT` naming it, or the creating file carries `// no-runtime-grant: <Table>` with a reason. It checks migrations against migrations — it cannot see what the code writes.
  - `src/Pegasus.Infrastructure/Persistence/Migrations/20260729176000_AzureSqlRuntimeLeastPrivilege.cs` and `20260729199000_RuntimeRoleReconciliation.cs:1-20` — `pegasus_web_runtime_role`, `pegasus_worker_runtime_role`, and the `RuntimeTables` list
  - `20260814092852_AddWorkerCaseCreationGrants.cs`, `20260821095500_GrantWorkerVehicleLookupRequests.cs`, `20260822044425_GrantWorkerCaseDocuments.cs` — the three shipped regressions the new check must catch retrospectively
  - `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs:1-40` — the existing `ExpectedSchemaTableSpec` list and the SqlServer-trait test style to extend
  - `tests/Pegasus.ArchitectureTests/WorkerCompositionTests.cs`, `WorkerAzureClientCompositionTests.cs` — the existing pattern for asserting what a composition root registers
  - `.github/workflows/ci.yml:58-60` — `Migration runtime-grant check` runs `./scripts/Test-MigrationGrants.ps1` in the `changes` job (ubuntu, runs on every change set)
- Binding decisions:
  - **L-01** — the gateway is `Pegasus.Web` evolved in place, so the new `/api/v1` write paths run under `pegasus_web_runtime_role`; a desktop feature that adds a table adds a grant.
  - **L-02** — there is no Azure test environment; the check must work against LocalDB or a SQL container, which is exactly why a full-privilege run cannot be the evidence.
- Depends on: `DSK-03-02` — the `/api/v1` route-group skeleton, the point from which desktop-driven gateway writes begin.

## Routing

- **Subagents**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `optimizing-ef-core-queries` (dotnet/skills `98f84851`, plugin `dotnet-data`) for reading the model and entity-to-table mapping → `code-testing-agent` (same pin) → `run-tests` (same pin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `IModel` / `IEntityType.GetTableName()` and for SQL Server `HAS_PERMS_BY_NAME` / `sys.database_permissions` if the runtime variant is chosen
- **Kanmer pipeline** for profile `fix`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (leave-preparing needs `files`, `plan`, `questions-resolved`)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, `docs/current-architecture.md:176-183` (the PLAT-035 statement), `scripts/Test-MigrationGrants.ps1` in full, and `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-18-runtime-grant-composition-gate` from `dev`.
3. Decide the mechanism and record the choice with its trade-offs in the ticket's `plan` document, choosing between: (a) a **build/architecture test** that derives, for each composition root, the set of entity types whose stores that root registers, maps them to table names through the EF `IModel`, and compares against the grant matrix parsed from the migrations; or (b) a **runtime test** that connects as each runtime role against LocalDB or a SQL container and asserts `HAS_PERMS_BY_NAME` for the required verbs on each table. Prefer (a) as the always-on CI gate and add (b) only if the mapping cannot be derived reliably.
4. Build the two lists. **What the code writes**: extend the composition-test style already in `tests/Pegasus.ArchitectureTests/WorkerCompositionTests.cs` to enumerate the store/repository registrations of `src/Pegasus.Web/Program.cs` and `src/Pegasus.Worker/Program.cs`, resolve each to its entity types, and map to table names via `IEntityType.GetTableName()`. **What is granted**: parse the grant statements out of `src/Pegasus.Infrastructure/Persistence/Migrations/*.cs` with the same two shapes `Test-MigrationGrants.ps1:30-52` already accepts (a literal `GRANT … [Table]` and the interpolated `("Table", "PERMISSIONS")` tuple), so the two checks cannot disagree about what counts as a grant.
5. Compare per role, per verb: a table the Web root writes must carry `INSERT`/`UPDATE`/`DELETE` as applicable for `pegasus_web_runtime_role`; a table it only reads needs `SELECT`. Do not collapse verbs — the shipped regressions were missing write verbs on tables that already had read access.
6. Provide the same explicit opt-out the existing script honours: `// no-runtime-grant: <Table>` with a reason, for a table a runtime role legitimately never touches. Reuse the marker rather than inventing a second convention.
7. Prove it catches the historical regressions: check out (or reconstruct in a fixture) the migration set as it stood immediately **before** each of `20260814092852_AddWorkerCaseCreationGrants`, `20260821095500_GrantWorkerVehicleLookupRequests` and `20260822044425_GrantWorkerCaseDocuments`, run the new check, and assert it fails naming the missing table and verb in each case. This is the acceptance evidence the plan row asks for.
8. Add a forward-looking fixture: a test that adds a fake entity/store registration with no grant and asserts the check fails, then removes it — so the gate is proved to work on a new table, which is the desktop case.
9. Wire it into CI. If the mechanism is (a), it runs inside the existing `unit` job's `Pegasus.ArchitectureTests` invocation (`.github/workflows/ci.yml:141-148`) — no new job. If (b) is added, attach it to the existing `sql-integration` lane with the `SqlServer` trait, and register the new tests with `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` so shard assignment stays valid.
10. Keep `scripts/Test-MigrationGrants.ps1` in CI unchanged — the plan's mitigation is explicit that it stays. The new check is additive: it catches the case the static check cannot see (code writes a table no migration grants), while the static check keeps catching a created-but-ungranted table.
11. Update `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs`'s `ExpectedSchemaTableSpec` if the desktop work has added tables since it was written, so the two lists stay in step.
12. Record the closure of PLAT-035 in `docs/current-architecture.md:176-183` — replace the "nothing verifies it" paragraph with what now verifies it and how — and note the carry-over disposition in `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` if that file tracks PLAT-035.
13. Run `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` and `pwsh ./scripts/Test-MigrationGrants.ps1`. Both green on the current tree.
14. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] The check derives the tables each composition root writes from the registered stores and the EF model, not from a hand-maintained list.
- [ ] It compares per role and per verb against the grants parsed from the migrations, accepting the same two grant shapes the existing script accepts.
- [ ] It fails, naming the table and the missing verb, on each of the three historical regressions when run against the tree as it stood before their fixes.
- [ ] It fails on a newly added ungranted table fixture and passes once the grant is added.
- [ ] The `// no-runtime-grant: <Table>` opt-out with a reason is honoured.
- [ ] `scripts/Test-MigrationGrants.ps1` remains in CI unchanged; the new check adds a lane-free step inside an existing job.
- [ ] `docs/current-architecture.md` records PLAT-035 as covered, naming the check.

## Verification

- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` — expected: the new grant-coverage test passes on the current tree.
- [ ] The same test against the three pre-fix fixtures — expected: three failures, each naming the table and verb (`CaseDocuments`, `VehicleLookupRequests` and the release-17 case-creation tables respectively).
- [ ] `pwsh ./scripts/Test-MigrationGrants.ps1` — expected: exit 0, unchanged behaviour.

## Evidence tier

Tier 11 — Migration/recovery. Here that obliges the check to be proved against every supported prior schema state that carried the defect — the three shipped regressions — rather than only against today's tree, and to keep the migration scripts idempotent.

## Documentation changes

- `docs/current-architecture.md:176-183` — replace the PLAT-035 gap paragraph with the verification that now exists.
- `docs/desktop/10-security-observability-performance/threat-register.md` — add or update the row covering ungranted writes.
- `docs/engineering.md` — only if the reviewer finds the tier-11 examples need this case; otherwise `None.`

## Guardrails

- **Azure**: no write. The check runs against LocalDB or a SQL container (L-02); it must not query the production database.
- **Scope boundary**: may add tests in `tests/Pegasus.ArchitectureTests` and, if the runtime variant is chosen, `tests/Pegasus.IntegrationTests`; may edit `docs/`. Must not change grant migrations to make the check pass — a genuinely missing grant is a new grant-only migration, exactly as `20260801220500_GrantWebMigrationHistoryRead.cs` did it. Must not weaken `scripts/Test-MigrationGrants.ps1`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: tests and LocalDB runs are full-privilege, so a passing integration test is **not** evidence that the deployed estate will accept the write — that is the whole defect; collapsing verbs hides the exact regressions that shipped; parsing grants with a second, subtly different rule from the existing script produces two checks that disagree; adding a new Windows CI job costs 2× under C-01, so the check goes inside an existing job.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
