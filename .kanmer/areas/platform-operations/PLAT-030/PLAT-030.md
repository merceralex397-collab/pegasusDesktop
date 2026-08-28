---
id: PLAT-030
type: ticket
title: Grant the two composition-root runtime permissions exposed by PLAT-018
status: preparing
area: platform-operations
assignee: codex-mcp-client
profile: fix
taken_at: '2026-08-28T05:23:21.646Z'
branch: task/plat-030-runtime-permissions
worktree: ../pegasus-worktrees/plat-030-runtime-permissions
labels:
  - desktop-conversion
  - plan-10
  - phase-8
  - tier-1
  - runtime-grants
  - grant-only-migration
groups:
  - EPIC-011
  - HZN-003
links:
  - PLAT-018
blocks:
  - PLAT-018
docs_todo: true
archived: false
created: '2026-08-28T05:22:38.415Z'
updated: '2026-08-28T05:23:21.646Z'
---

## What

Add one additive, grant-only EF migration for the two runtime permissions exposed by [[PLAT-018]]:

- Web UPDATE on dbo.ApprovedSentPollOutcomes, required by EfTriageStore.LinkResponseEvidenceAsync.
- Web INSERT on dbo.EvaHandoffDownloadOperations, required by EvaHandoffStore.DownloadAsync.

## Why

PLAT-018 now derives registered composition-root stores, their EF model tables, and actual writes. Its focused gate recognizes the existing creation-migration grants and reports exactly these two missing Web permissions. PLAT-018 is intentionally test-only and forbids migration edits, so the permissions belong in this separate remediation ticket.

## Scope and guardrails

May add one additive, grant-only migration following the existing runtime-role grant migration convention. No changes to the PLAT-018 analyzer, runtime stores, unrelated migrations, CI workflows, deployment, cloud state, credentials, or upstream repositories.

## Acceptance

- SQL Server Up grants Web UPDATE on dbo.ApprovedSentPollOutcomes.
- SQL Server Up grants Web INSERT on dbo.EvaHandoffDownloadOperations.
- Up checks the managed Web runtime role and is a no-op for non-SQL providers.
- Down exactly revokes the two permissions and is a no-op for non-SQL providers.
- The PLAT-018 focused architecture gate passes after this migration is present.
- The migration-grant script, Release validation, and diff check pass.
- No cloud or upstream operation is performed.

## Verification

- dotnet build --configuration Release
- focused RuntimeGrantCompositionTests
- pwsh ./scripts/Test-MigrationGrants.ps1
- git diff --check

## Evidence

The exact missing permissions and source owners are recorded in PLAT-018's implementation scratch. This ticket remains a dependency of PLAT-018 until its migration is merged to dev and the focused gate passes.
