# Post-implementation report — PLAT-029

## Result

The local document adapter now resolves intake-retained source, attachment, and folded image content from the existing LocalCaseCustody layouts while preserving the existing managed version-id layout. The implementation is committed at `a505175c` and pushed to `origin/plat-029-local-document-content`.

The ticket is not complete. The required local Start/Smoke evidence could not run because this checkout's doctor requires SDK `10.0.302`, while the workstation exposes `10.0.204` and `10.0.303`. PR creation is also blocked by the GitHub account's collaborator permission.

## Scope delivered

- `src/Pegasus.Infrastructure/Custody/LocalDocumentContentStore.cs`
  - Added `OpenReadVersionAsync`.
  - Checks the existing `managed/{versionId:N}/content` path first.
  - Resolves existing `documents/{receipt}/{hash}`, `documents/{receipt}/attachments/{ordinal}-{hash}`, and folded `images/{ordinal}-{receipt}` directories under `cases/{caseId:N}).
  - Binds candidates to existing `metadata.json` values for hash, filename, media type, and image ordinal.
  - Preserves root containment, SHA-256 and length verification, FileStream options, cancellation, missing-file message, and fail-closed ambiguity/metadata errors.
- `tests/Pegasus.IntegrationTests/DocumentCustodyDurabilityTests.cs`
  - Covers source, attachment, folded image, managed fallback, missing-file, and integrity behavior.
- `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs`
  - Covers a real accepted/processed intake source through document download and ZIP export.
- `docs/desktop/08-testing/test-uat-stack.md`
  - Updates the named Components and Known gaps sections.
- No Core contract, Box adapter, LocalCaseCustody writer, Worker, API, Azure, or FRD files changed.

## Validation evidence

- `dotnet restore Pegasus.slnx` — passed.
- Focused `OpenReadVersionAsync` integration tests — 3 passed, 0 failed.
- `IntakeRetainedDocumentIsReadableThroughDownloadAndExportReaders` — 1 passed, 0 failed.
- Relevant integration suites (DocumentCustodyDurabilityTests, EvaHandoffPersistenceTests, CaseCustodyWebTests, BoxDocumentContentStoreTests, CustodyOutboxIntegrationTests) — 41 passed, 0 failed, 1 pre-existing corpus-dependent skip.
- `dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-restore` — 916 passed, 0 failed.
- `dotnet build Pegasus.slnx --configuration Release --no-restore --nologo` — passed, 0 warnings, 0 errors.
- `git diff --check` — passed.
- Pre-fix reproduction — 1 failed as expected with `FileNotFoundException: The document content is unavailable.` at `LocalDocumentContentStore.OpenReadAsync` line 97.
- `pwsh ./scripts/Initialize-LocalDevelopment.ps1` — restore and Debug build passed; doctor stopped before launch on the SDK mismatch above. Therefore `Invoke-LocalDevelopment.ps1 -Action Start` and `-Action Smoke` have no valid success evidence.

## Simplification pass

Completed and recorded in the plan. Existing validation, root guard, managed layout, and stream behavior are reused. No new naming convention, marker file, Core contract, API, Worker, Azure, desktop, or FRD scope was introduced.

## Independent review

The independent `pegasus-desktop-reviewer` review on 2026-08-25 returned FAIL because this report and checklist were initially absent, and because Start/Smoke remains blocked by the SDK requirement. No code findings were reported. The report and checklist are now being added; the SDK blocker remains.

## Remaining blockers and next actions

1. Provide a workstation/environment with the repository-required SDK `10.0.302`, then rerun `Initialize-LocalDevelopment.ps1`, `Invoke-LocalDevelopment.ps1 -Action Start`, and `-Action Smoke`; capture the retained-content read result.
2. Restore GitHub collaborator permission or have an authorized collaborator create the PR from the pushed branch.
3. After those blockers, obtain a fresh independent review, satisfy CI/PR requirements, merge to `dev`, then verify on merged `main` and write proof before moving Kanmer stages.

## Validation refresh — 2026-08-26

The required SDK is now available at the task-local path `C:\\Users\\PC\\AppData\\Local\\Temp\\pegasus-intk002-sdk-10.0.302`. Running `Initialize-LocalDevelopment.ps1` with it passed restore, Debug build, and Offline Doctor.

The exact prescribed `Invoke-LocalDevelopment.ps1 -Action Start` command then failed before readiness at line 1482 while recording the launched process: `GetFullPath` received an empty process path. The failed run was `6b86d27dffba4f9a9fa8cffb35da877e`; its manifest and logs remain under the run-owned artifact directory, and `-Action Stop -RunId 6b86d27dffba4f9a9fa8cffb35da877e` completed successfully. Consequently no Start/Smoke success or retained-content operator journey is claimed.

PR #25 is open against `dev`. Fresh independent review is in progress. The code/test evidence remains as recorded above; the launcher failure is outside this ticket's permitted source scope and requires resolution in the local-development stack owner before PLAT-029 can close.

## Reader-consumer coverage — 2026-08-26

Added and passed `IntakeRetainedImageIsReadByEvaAndAssessmentReportProjection` (1/1). The test writes the image through `LocalCaseCustody`'s existing intake-retained attachment layout, then reads it through both `EvaHandoffStore` generation and `EfAssessmentReportProjectionSource`; no managed-layout seeding is used. The combined affected integration set passed 42 tests with 1 pre-existing corpus-dependent skip and 0 failures. Release solution build passed with 0 warnings and 0 errors. This closes the independent review's missing report/EVA consumer-coverage finding.

The exact prescribed local Start command still fails before readiness in the existing launcher at line 1482 because the launched process path is empty; the failed run was stopped cleanly. Start/Smoke and the operator-visible journey remain unproven.

## CI contract refresh — 2026-08-26

PR run \`33012366372\` was a truthful failure, not a transient runner outage: the stale task branch's new test referenced \`EvaBundle.ProvenanceContent\`, which current \`dev\` no longer exposes. The task branch was synchronized with \`origin/dev\` in merge commit \`013fba28\`; the test now reads the generated archive's retained image entry under the current EVA contract. Correction commit \`7d761ed6\` is pushed to PR #25.

Current local evidence on \`7d761ed6\`:

- focused retained-reader test: 1 passed, 0 failed;
- affected integration set: 42 passed, 1 pre-existing corpus-dependent skip, 0 failed;
- Release solution build with shared compilation disabled: 0 warnings, 0 errors.

A fresh independent review is pending for \`7d761ed6\`. PR merge is not claimed. The existing local Start command still fails before readiness at line 1482 while recording an empty launched-process path; Start/Smoke and the operator-visible journey remain unproven and require the local-development stack owner to resolve the launcher defect. No out-of-scope script, Worker, API, cloud, mailbox, Box, or upstream change was made.

## Validation refresh — 2026-08-26 (synchronized branch)

The synchronized branch's Core suite passed 935 tests with 0 failures and 0 skips using the repository-required Release configuration and shared-compilation-disabled retry. The task worktree is clean at \`7d761ed6dbe66fd274bac3701618980499bf0a47\`. The main checkout's pre-existing user change was not touched.

PR #25 replacement CI run \`33013301879\` is still in progress: browser, unit, changes, documentation, local-development-scripts, reference-data, and SQL shard 2 are green; SQL shards 1 and 3 remain pending.
