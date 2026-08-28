# PLAT-031 plan

## Scope and evidence

The PLAT-018 exact combined validation exposed a real gap: Worker code in `EfCaseWorkflowStore` inserts `CaseReportVersionLedgerEntity`, mapped to `dbo.CaseReportVersionLedgers`, but migration `20260827231948_IssuedReportVersionEvidenceLedger` grants `SELECT, UPDATE` only to `pegasus_worker_runtime_role`. The existing Web grant includes `INSERT`; the Worker grant does not.

This ticket is limited to the missing grant. PLAT-018 remains the owner of the composition-root gate and its tests. PLAT-030 remains the owner of the separate Web `ApprovedSentPollOutcomes` grant.

## Plan

1. Create a task branch from `origin/dev` using the repository's recorded worktree convention.
2. Add one new SQL Server-guarded EF migration, using the next repository timestamp/name convention. Its `Up` grants only `INSERT` on `dbo.CaseReportVersionLedgers` to `pegasus_worker_runtime_role`; its `Down` revokes only that permission. Include the same managed-role validation/idempotence pattern used by the existing grant-only migrations.
3. Do not edit production code, the existing table-creation migration, the grant census script, CI, cloud resources, deployment state, credentials, upstream remotes, or `corpus/`.
4. Run locked restore, the architecture grant-composition suite, the migration grant census, and the repository's relevant local checks. Confirm the new migration is discovered and the combined PLAT-018 + PLAT-030 validation passes at exact heads.
5. Inspect the branch diff for the required simplification pass. Record dated findings and dispositions here before review.
6. Open/update the PR to `dev`, record exact head and CI, and request an independent review from an agent that did not implement this ticket.
7. After review PASS and green exact-head CI, merge the PR to `dev`. Verify the merge SHA. PLAT-018 and PLAT-030 may then complete their own independent review/merge gates; this ticket does not authorize cloud or deployment writes.
8. After the reviewed merge reaches the authorized target branch, verify the exact merged result and write Kanmer proof. Move one Kanmer stage at a time after fresh `get_doc_gates`.

## Acceptance checklist mapping

- New migration grants exactly Worker `INSERT` on `CaseReportVersionLedgers`: implementation and migration census.
- Down revokes exactly `INSERT`: migration inspection and focused test/build.
- PLAT-018 composition-root gate passes: exact combined validation with PLAT-018 and PLAT-030.
- No forbidden scope: branch diff and repository status checks.
- Independent review: reviewer result recorded in scratch/report and PR review state.

## Commands

- `git status --short`
- `dotnet restore Pegasus.slnx --locked-mode`
- `dotnet test tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false`
- `pwsh ./scripts/Test-MigrationGrants.ps1`
- Relevant repository build/test commands required by the current CI and ticket profile.

## Simplification pass

_To be recorded after the implementation diff exists, before PR review._

## Scope correction from independent test analysis — 2026-08-28

The initial implementation scope omitted two expected-state consumers. Independent analysis found that the new migration is not locally complete until:
- `scripts/Invoke-AzureDatabaseBootstrap.ps1` includes `pegasus_worker_runtime_role|G|INSERT|CaseReportVersionLedgers` in its existing matrix; this is a local/effective matrix expectation, not an Azure write and not the unchanged `Test-MigrationGrants.ps1`.
- `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs` includes `CaseReportVersionLedgers:INSERT` in the existing Worker expectation.

These are the only scope additions. The existing table-creation migration, production code, CI, cloud/deployment state, credentials, upstream remotes, and corpus remain out of scope. The combined PLAT-018 + PLAT-030 + PLAT-031 validation must run the bootstrap local-plan check and the focused permission assertion after these consumers are updated.

## Simplification pass — 2026-08-28

Independent read-only simplification review (Aristotle) passed with no code changes recommended:
- Reused the existing SQL Server guard, managed-role validation, and direct grant/revoke migration shape.
- The repeated managed-role validation in both `Up` and `Down` is retained because rollback is also fail-closed.
- No unnecessary loop, dependency, abstraction, or compatibility path was introduced.
- Scope altitude is correct: one Worker `INSERT` permission, plus the two required expected-state consumers.

Independent test analysis (Popper) initially found the first local implementation incomplete because the bootstrap expected matrix and focused Worker permission expectation did not include the new grant. The files map and plan were corrected before review. After that correction:
- `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local` passed.
- The focused `LatestMigrationGrantsIssuedReportVersionLedgerToItsRuntimeCallers` test passed 1/1.
- `Test-MigrationGrants.ps1` remains unchanged and continues to pass.

## Independent review and combined validation — 2026-08-28

- Boyle (`pegasus-desktop-reviewer`), who did not implement PLAT-031, reviewed PR #38 at exact head `59af1b21fa9a09cffc370d299f5c10363e7a4edb`. Plan coverage, implementation coverage, local evidence, simplification, and scope all passed; the review verdict was **not merge-ready at review time** only because exact-head CI was still running and the required combined validation had not yet been recorded. No implementation defect was found.
- Clean detached validation combined exact PLAT-030 `c599a42b1f964c4e5a1dc13894f28f8300152984`, PLAT-018 `fca04a917f2f2e3c3abc7eeb93e46c9ca9a00ea5`, and PLAT-031 `59af1b21fa9a09cffc370d299f5c10363e7a4edb` without changing or pushing any task branch. Locked restore passed; focused `RuntimeGrantCompositionTests` passed 10/10; full ArchitectureTests passed 121/121; `Test-MigrationGrants.ps1` passed 73/73 migration files; `Test-AzureDeploymentPlan.ps1 -Mode Local` passed; and `git diff --check` passed.
- Exact-head GitHub CI run `33153323761` remains the only open merge gate; its unit, browser, infrastructure, documentation, changes, local-development-scripts, and reference-data jobs have passed, while SQL shards 2 and 3 are still running. No merge is claimed until those jobs complete successfully.

## Exact-head CI remediation — 2026-08-28

- Exact-head GitHub Actions run `33153323761` failed in SQL shard 3 (job `98790328153`), while the other jobs and SQL shards passed. The failed test was `IntakePersistenceIntegrationTests.CommittedMigrationCreatesTheSqlServerSchema`: the database applied the new `20260828074800_GrantWorkerCaseReportVersionLedgerInsert` migration but the test's expected migration list stopped at `20260827231948_IssuedReportVersionEvidenceLedger`. This was a ticket defect, not a transient CI failure.
- Added the missing expected migration entry in `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`. Commit `0ab518e3` is pushed to PR #38's configured branch.
- The focused census/schema test passed 1/1 after the fix. Exact-head CI and independent review must be refreshed for `0ab518e3` before merge.
