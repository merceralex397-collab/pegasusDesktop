---
id: INTK-009
type: ticket
title: Fix concurrent intake completion SQL deadlock
status: preparing
area: intake-processing
assignee: ''
profile: fix
stageEntered:
  preparing: '2026-08-27T20:06:15.606Z'
labels:
  - defect
  - intake
  - sqlserver
  - concurrency
  - ci-blocker
  - in-repo
links: []
blocks:
  - DOCS-001
refs:
  - docs/frd/frd-02-intake-and-source-identity.md
archived: false
created: '2026-08-27T20:06:12.155Z'
updated: '2026-08-27T20:06:15.606Z'
---

## What

Eliminate the SQL Server deadlock that still occurs when two grouped-image intake members complete concurrently through `EfIntakeWorkStore.CompleteProcessingAsync`.

## Evidence

The exact-head repository CI rerun for PR #14 (`bb263b20a49af1375d2823ce5c4a803dd66bdc39`) failed in job `98652140460` (`sql-integration (2)`) on `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns`. The failure was SQL Server error 1205 at `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:338`, after 296/298 assigned tests passed. The test's existing `ProcessWithDeadlockRetryAsync` already retries five times; the rerun therefore does not establish a transient runner failure.

The same failure is recorded in the DOCS-001 post-implementation report and blocks its required exact-head CI. No existing fork ticket owns this exact completion-path deadlock.

## Scope

Make the smallest correctness-preserving change in the existing intake work-store transaction and its focused integration test coverage. Preserve grouped-image atomicity: concurrent members must still never split, exactly one member/group outcome must be registered, and leases/revisions/idempotency must remain correct. Do not weaken assertions, remove the concurrency test, mask error 1205, add blanket retry policy, or alter unrelated intake behavior.

No upstream synchronization, cloud write, deployment, credential change, or external operation is permitted.

## Governing document

`docs/frd/frd-02-intake-and-source-identity.md` — existing intake persistence and failure rules.

## Acceptance criteria

- [ ] `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` passes repeatedly against SQL Server without exhausting its existing deadlock retry.
- [ ] The completion transaction remains atomic: no duplicate evaluation revision, no lost lease completion, no split grouped-image outcome, and no silent partial state.
- [ ] The fix does not use a blanket retry policy or weaken the test.
- [ ] Existing intake recovery, idempotency, and failure-path tests remain green.
- [ ] The exact failure mechanism and chosen transaction/ordering change are recorded in the ticket plan and proof.
- [ ] PR #14's exact-head required CI can rerun green after this fix is merged to `dev`.

## Verification

- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns"`
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`
- `dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release`
- `dotnet build Pegasus.slnx --configuration Release`
- `pwsh ./scripts/Test-MigrationGrants.ps1`

## Delivery

Independent review, green exact-head CI, merge to `dev`, proof on `main`, and Kanmer closeout are required. The ticket blocks `DOCS-001` only because its exact-head required CI currently fails on this shared defect.
