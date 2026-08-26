# Proof — ENG-001

## Merged-main identity

- PR #6: https://github.com/merceralex397-collab/pegasusDesktop/pull/6
- PR branch CI: hosted run `32852051438` passed at corrected head `e6bd1949`.
- PR #6 merged into `dev` at `e0322ee1b7523a76451bf1c65416b4a55c4f8173`.
- `dev` was promoted to `main` by an exact non-force fast-forward.
- Verification checkout: `80d9f96d64b1dfbeea4658adfc99351f71b303d7` (`main`, 2026-08-26).

## Independent review

The independent review recorded no substantive defect. It explicitly accepted the three-column schema-reversible/data-destructive migration consequence, the fixed CRLF JSON layout, and the absence of a compatibility path.

## Verification commands

All commands ran in a clean detached worktree at the merged `main` SHA:

| Command | Result |
| --- | --- |
| `dotnet restore` | Passed |
| `dotnet build ./Pegasus.slnx --configuration Release --no-restore` | Passed, 0 warnings, 0 errors |
| `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~Eva"` | Passed, 41/41 |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~EvaHandoffPersistence"` | Passed, 8/8 |
| `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` | Passed, 101/101 |
| `dotnet ef migrations has-pending-model-changes --project src/Pegasus.Infrastructure --startup-project src/Pegasus.Web --configuration Release --no-build` | Passed; no pending model changes |
| `pwsh -NoProfile -File ./scripts/Test-MigrationGrants.ps1` | Passed; 66 migration files checked |
| `pwsh -NoProfile -File ./scripts/Test-DocumentationLinks.ps1` | Passed; 236 files checked |
| `git diff --check` | Passed |

The implementation evidence and the migration `Up → Down → Up` LocalDB round trip are recorded in the post-implementation report and plan. No Azure, deployment, mailbox, Box, or upstream operation was performed.

## Acceptance result

The EVA export no longer produces or persists manifest/provenance companions; it emits the ordered thirteen-key JSON with two-space CRLF layout and eligible images only. The scaffolded migration drops the three obsolete revision columns, and retained JSON/bundle hashes and in-memory provenance validation remain covered. FRD-07 no longer mandates the removed companions.
