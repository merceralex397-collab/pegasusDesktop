---
id: INTK-008
type: ticket
title: Make grouped-image SQL deadlock retry deterministic in the integration test
status: implementing
area: intake-processing
order: 120
assignee: codex-mcp-client
profile: fix
stageEntered:
  implementing: '2026-08-26T11:34:01.194Z'
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
archived: true
created: '2026-08-26T11:29:43.488Z'
updated: '2026-08-26T11:39:07.636Z'
---

## Archived — non-actionable scope correction — 2026-08-26

This diagnostic ticket is archived, not done. Its proposed test-only workaround was disproven by its own focused validation and would overlap active production-intake ownership.

Evidence:
- Exact-head CI run `32959758190` and rerun job `98152798225` fail `GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns` with SQL deadlock 1205 wrapped as `InvalidOperationException` → `DbUpdateException` → `SqlException`.
- The attempted test-only exception-chain catch was reverted after local focused validation failed at iteration 0 with `State=Failed` and `FailureCode=unexpected_intake_processing_failure`.
- `src/Pegasus.Core/Intake/DurableIntake.cs:573-596` catches the provider failure before a test helper can retry; `IntakeExceptionPolicy.IsTransientFailure` is the relevant classification boundary.
- Active ticket `INTK-001` owns the intake fault taxonomy and active ticket `INTK-002` owns `EfIntakeWorkStore.cs`; this ticket must not overlap either claim.
- The branch is clean at `origin/dev`; no production or test workaround was retained, and no external operation was performed.

Disposition: non-actionable duplicate/scope correction. The correct next action is for the active intake owner to classify the provider deadlock within the existing transient-failure boundary, then rerun exact-head CI. `DOCS-001` remains blocked until its own required checks are green.

## Outcome

Archived with documented evidence; no completion claim.
