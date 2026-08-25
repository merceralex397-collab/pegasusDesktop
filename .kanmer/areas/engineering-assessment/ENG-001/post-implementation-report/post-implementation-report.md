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
| `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` | Passed: 916/916. |
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

Before a PR, independent review must assess the accepted non-additive migration consequence: old application binaries cannot generate EVA hand-offs after the three columns are dropped until they are rolled forward. Existing revisions retain their bundle/JSON/hashes and downloads still use `BundleContent`; no data is deleted from those retained fields. The archive bytes intentionally change `InputFingerprint`, so a regenerated historical input forms Revision 2 rather than reusing Revision 1.

PR, CI, merge, proof, and closeout are not yet claimed.
