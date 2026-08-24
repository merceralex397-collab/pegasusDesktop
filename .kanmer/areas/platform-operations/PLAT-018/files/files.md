# File map — PLAT-018

## Direct change surface

- `docs/current-architecture.md:176-183` — replace the PLAT-035 gap paragraph with the verification that now exists.
- `docs/desktop/10-security-observability-performance/threat-register.md` — add or update the row covering ungranted writes.
- `docs/engineering.md` — only if the reviewer finds the tier-11 examples need this case; otherwise `None.`

## Context files

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

## Ripple effects

- [ ] The check derives the tables each composition root writes from the registered stores and the EF model, not from a hand-maintained list.
- [ ] It compares per role and per verb against the grants parsed from the migrations, accepting the same two grant shapes the existing script accepts.
- [ ] It fails, naming the table and the missing verb, on each of the three historical regressions when run against the tree as it stood before their fixes.
- [ ] It fails on a newly added ungranted table fixture and passes once the grant is added.
- [ ] The `// no-runtime-grant: <Table>` opt-out with a reason is honoured.
- [ ] `scripts/Test-MigrationGrants.ps1` remains in CI unchanged; the new check adds a lane-free step inside an existing job.
- [ ] `docs/current-architecture.md` records PLAT-035 as covered, naming the check.

## Out of scope

- **Azure**: no write. The check runs against LocalDB or a SQL container (L-02); it must not query the production database.
- **Scope boundary**: may add tests in `tests/Pegasus.ArchitectureTests` and, if the runtime variant is chosen, `tests/Pegasus.IntegrationTests`; may edit `docs/`. Must not change grant migrations to make the check pass — a genuinely missing grant is a new grant-only migration, exactly as `20260801220500_GrantWebMigrationHistoryRead.cs` did it. Must not weaken `scripts/Test-MigrationGrants.ps1`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: tests and LocalDB runs are full-privilege, so a passing integration test is **not** evidence that the deployed estate will accept the write — that is the whole defect; collapsing verbs hides the exact regressions that shipped; parsing grants with a second, subtly different rule from the existing script produces two checks that disagree; adding a new Windows CI job costs 2× under C-01, so the check goes inside an existing job.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.
