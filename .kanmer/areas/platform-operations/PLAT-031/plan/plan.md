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
