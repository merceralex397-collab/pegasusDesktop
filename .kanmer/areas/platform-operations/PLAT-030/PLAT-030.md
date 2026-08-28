---
id: PLAT-030
type: ticket
title: Grant Web UPDATE on ApprovedSentPollOutcomes exposed by PLAT-018
status: verifying
area: platform-operations
assignee: codex-mcp-client
profile: fix
stageEntered:
  implementing: '2026-08-28T05:25:55.425Z'
  review: '2026-08-28T17:29:45.306Z'
  verifying: '2026-08-28T17:30:13.765Z'
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
prs:
  - '37'
archived: false
created: '2026-08-28T05:22:38.415Z'
updated: '2026-08-28T17:30:13.765Z'
---

## What

Add one additive, grant-only EF migration for the one genuine missing runtime permission exposed by [[PLAT-018]]:

- Web UPDATE on dbo.ApprovedSentPollOutcomes, required by EfTriageStore.LinkResponseEvidenceAsync.

## Why

PLAT-018 derives registered composition-root stores, their EF model tables, and actual writes. A focused run reports Web UPDATE on ApprovedSentPollOutcomes as missing. The apparent Web INSERT gap for EvaHandoffDownloadOperations was a parser false negative: the existing `20260819180000_GrantEvaHandoffDownloadOperations.cs` already grants Web SELECT, INSERT. PLAT-018 will correct that comment-sensitive parser separately. Its own scope is test-only and forbids migration edits, so this ticket owns only the genuine missing grant.

## Scope and guardrails

May add one additive, grant-only migration following the existing runtime-role migration convention and the one corresponding entry in the existing `Invoke-AzureDatabaseBootstrap.ps1` permission matrix required by the repository's release-plan validator. No changes to the PLAT-018 analyzer, runtime stores, unrelated migrations, CI workflows, deployment execution, cloud state, credentials, or upstream repositories. Do not duplicate the existing EvaHandoffDownloadOperations grant.

## Acceptance

- SQL Server Up grants Web UPDATE on dbo.ApprovedSentPollOutcomes.
- Up checks the managed Web runtime role and is a no-op for non-SQL providers.
- Down exactly revokes that permission and is a no-op for non-SQL providers.
- The existing bootstrap permission matrix accounts for the new grant-carrying migration.
- The PLAT-018 focused architecture gate passes after this migration and its parser correction are present.
- The migration-grant script, Local deployment-plan validation, Release validation, and diff check pass.
- No cloud or upstream operation is performed.

## Verification

- dotnet build --configuration Release
- focused RuntimeGrantCompositionTests
- pwsh ./scripts/Test-MigrationGrants.ps1
- pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local
- git diff --check

## Evidence

Read-only inspection on 2026-08-28 confirmed `20260819180000_GrantEvaHandoffDownloadOperations.cs` contains `GRANT SELECT, INSERT` for `pegasus_web_runtime_role`. The remaining real gap is Web UPDATE on `ApprovedSentPollOutcomes`, written by `EfTriageStore.LinkResponseEvidenceAsync`. This ticket remains a dependency of PLAT-018 until its migration is merged to dev and the focused gate passes.
