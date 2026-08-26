---
id: INTK-008
type: ticket
title: Make grouped-image SQL deadlock retry deterministic in the integration test
status: preparing
area: intake-processing
assignee: codex-mcp-client
profile: fix
taken_at: '2026-08-26T11:30:01.716Z'
branch: task/intk-008-deadlock-retry
worktree: ../pegasus-worktrees/intk-008-deadlock-retry
labels:
  - ci-blocker
  - sql-server
  - concurrency
  - test-harness
  - found-during-qa
links: []
blocks:
  - DOCS-001
refs:
  - docs/frd/frd-02-intake-and-source-identity.md
archived: false
created: '2026-08-26T11:29:43.488Z'
updated: '2026-08-26T11:30:01.716Z'
---

## What

Make the grouped-image SQL concurrency test faithfully retry the transient SQL Server deadlock it intentionally provokes.

The exact-head CI failure is in `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns`. The test already models queue redelivery with `ProcessWithDeadlockRetryAsync`, but the current helper catches only a bare `SqlException`. EF Core surfaces the observed deadlock as an outer `InvalidOperationException` with `DbUpdateException` and inner `SqlException.Number == 1205`, so the intended retry path is bypassed.

## Why

PR #14's exact-head required CI cannot become green while this test fails. The failure is outside DOCS-001's changed files and is a repository test-harness defect, not evidence against report generation.

## Scope

- Update only the owned integration-test retry handling and its focused test evidence.
- Retry only SQL Server deadlock 1205 through the existing bounded queue-delivery simulation.
- Do not catch unrelated failures, weaken assertions, change production intake code, add global EF retry policy, change schema, or add compatibility behavior.
- No upstream operation, cloud write, deployment, credential change, or external environment change.

## Acceptance criteria

- The test catches the actual EF-wrapped SQL deadlock shape and retries it within a bounded attempt count.
- Non-deadlock exceptions still fail the test immediately.
- `ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` passes its full 12-iteration concurrency assertion locally.
- The focused integration test command passes and exact-head CI for PR #14 is rerun after the fix.
- No production source file or runtime behavior changes.

## Verification

- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~GroupedImageIntakeConcurrencyTests"`
- Relevant repository validation and the exact-head PR #14 CI rerun.
- Independent review of the test-only diff before merge.

## Evidence

Observed run `32959758190`, rerun job `98152798225`, failed `sql-integration (2)` with SQL deadlock 1205 at `EfIntakeWorkStore.CompleteProcessingAsync`. The failing stack shows `InvalidOperationException` → `DbUpdateException` → `SqlException`, while the helper at `GroupedImageIntakeConcurrencyTests.cs:329` catches only `SqlException`.

## Outcome

_Filled at closeout._
