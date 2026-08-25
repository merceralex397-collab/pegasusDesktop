# Post-implementation report — ENG-001

## Delivered

On `task/eng-001-drop-manifest-indent-json`:

- removes manifest/provenance file creation, archive entries, record members, persistence columns, configuration, persistence mapping, and reconstruction;
- writes the ordered EVA JSON with two-space indentation and explicit `\r\n` newlines;
- adds scaffolded migration `20260825122524_DropEvaHandoffProvenanceAndManifest` (three drops in `Up`, three restores in `Down`) and regenerated model snapshot;
- corrects FRD-07 before code so the package is JSON plus eligible images only;
- strengthens Core and LocalDB tests to prove the exact archive list, ZIP JSON bytes, CRLF/two-space layout, hashes, source provenance validation, and current production writer path.

No enum, flag, compatibility path, Azure operation, mailbox/Box operation, release, or deployment was added. `docs/current-architecture.md` was inspected but already contained no stale manifest/provenance claim.

## Validation

| Command / check | Result |
| --- | --- |
| `dotnet build ./Pegasus.slnx --configuration Release` | Passed, 0 warnings/errors after scaffold analyzer correction. |
| `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` | Passed after final test changes: 917/917. |
| `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --filter "FullyQualifiedName~EvaBundleContractTests"` | Passed after final test changes: 8/8. |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~EvaHandoffPersistenceTests"` | Passed after final test changes: 8/8 LocalDB facts. |
| `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` | Passed: 99/99. |
| `dotnet ef migrations has-pending-model-changes --project src/Pegasus.Infrastructure --startup-project src/Pegasus.Web --configuration Release --no-build` | Passed: no pending model changes. |
| Dedicated `PegasusEng001Migration` LocalDB `Up → Down → Up` | Passed; three columns absent after final `Up`, 13 columns remain. Temporary database removed afterward. |
| `pwsh ./scripts/Test-MigrationGrants.ps1` | Passed: 65 migration files checked. |
| Active-model search excluding frozen migrations | Passed: no manifest/provenance file or active members in `src`/tests. |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | Passed: 232 files checked. |
| `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` | Passed. |
| `git diff --check` | Passed; no whitespace errors. |

## Test analysis and simplification

An independent `pegasus-test-engineer` analysis identified gaps in the first test revision. The ZIP JSON byte assertion, full production archive list, direct hash checks, and contradictory provenance case were applied and rerun. A duplicate migration test was not added because this ticket requires and performed the real scaffolded LocalDB round trip; no existing migration-harness test needs extending. The copied plan's absent `CaseOperatorExportTests.cs` and unrelated Custody Outbox manifest were not created or changed.

## Review handoff

Independent review found no substantive code defect. The migration is **schema-reversible but data-destructive** for historical `ManifestContent`, `ProvenanceContent`, and `ProvenanceSha256` values: `Up()` deletes those values, while `Down()` recreates empty/default columns and cannot recover them. This is accepted within the pre-release scope. Retained `BundleContent`, `BundleSha256`, `JsonContent`, and `JsonSha256` remain intact, and downloads still use `BundleContent`. Old application binaries cannot generate EVA hand-offs after the columns are dropped until rolled forward. The archive bytes intentionally change `InputFingerprint`, so regenerated historical input forms Revision 2 rather than reusing Revision 1.

PR, CI, merge, proof, and closeout are not yet claimed.

## Hosted CI correction — 2026-08-25

Run `32850235619` completed with one real test failure in SQL shard 3: `IntakePersistenceIntegrationTests.CommittedMigrationCreatesTheSqlServerSchema` expected the prior 64-entry migration list but the branch correctly included the new EVA migration as entry 65. The shard ran 291/291 assigned tests with 290 passed and 1 failed; browser, unit, the other SQL shards, coverage, documentation, changes, reference-data, and local-development lanes passed. Commit `e6bd1949` adds `20260825122524_DropEvaHandoffProvenanceAndManifest` to that existing assertion. Focused Release LocalDB validation passed 1/1. A new hosted run on `e6bd1949` is required before merge.

## Merge result — 2026-08-25

Corrected PR head `e6bd1949` passed hosted run `32852051438` completely: changes, documentation, local-development-scripts, reference-data, unit, browser, SQL integration shards 1–3, and SQL integration coverage passed; infrastructure was correctly skipped. PR #6 was independently reviewed and merged into `dev` at `e0322ee1b7523a76451bf1c65416b4a55c4f8173`. No `main` promotion, deployment, Azure write, or proof claim has been made.
