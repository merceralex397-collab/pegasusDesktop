# Post-implementation report — GWY-004

## Delivered

- Added the centrally pinned `Microsoft.AspNetCore.OpenApi` 10.0.11 package and the contract-test friend assembly entry.
- Registered the named `v1` OpenAPI document and `/openapi/{documentName}.json` route through the existing DesktopGateway composition. The existing feature gate controls both registrations.
- Added an OpenAPI transformer with the gateway title, assembly informational version, description, explicit `PegasusProblem` fields, and generated `PagedResult` schema.
- Extended the existing `Pegasus.Api.ContractTests` project from [[TEST-001]]; no second project or solution entry was created.
- Added normalized byte-for-byte snapshot testing, an additive previous-snapshot test, the loopback exporter, both committed snapshots, and the `docs/index.md` index row.
- No new endpoint was retained: the temporary `/api/v1/__probe` used for red-then-green evidence was removed.

## Verification evidence

Branch: `task/openapi-snapshot`
Commit: `8d3827c68530ae7db8468d3bf6a488511c10b4a0`

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed; all projects up to date.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false` — passed; `Build succeeded. 0 Warning(s). 0 Error(s).`
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --filter "Category=Contract"` — passed; 5/5.
- `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category=Contract"` — passed; contract project 5/5, other solution projects had no matching tests.
- `pwsh ./eng/api/Export-OpenApiDocument.ps1` twice — passed; byte-identical SHA-256 `DF3761703FB4122C4E173D091BB6D654D49DA4EEF2895952F5180E8E998395E4`.
- Current and previous snapshot hashes match at the same SHA-256.
- `diagnostics/version` is absent from the committed snapshot.
- Feature-gate-off `GET /openapi/v1.json` returned 404.
- The temporary probe caused `OpenApiSnapshotTests` to fail with the exact snapshot path and regeneration command; removing it restored the passing suite.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` passed.

## Scope and deviations

- The existing CI unit job already contained the TEST-001 contract command and corrected comment, so no duplicate CI edit was made.
- The first OpenAPI package version exposed a high-severity transitive vulnerability; 10.0.11 was selected because it resolves `Microsoft.OpenApi` 2.7.5 and passes locked restore.
- No Azure, deployment, upstream, credential, or external write was performed.
- Simplification pass completed with no unapplied findings.

## CI follow-up — 2026-08-27

The first exact-head CI run `33043859460` exposed parallel `WebApplicationFactory` startup contention in the contract class: build passed, but the four concurrent OpenAPI requests returned HTTP 500 after the runner timeout. The class was changed to an xUnit collection with parallelization disabled. The exact unit command sequence was reproduced locally afterward with Core 935/935, Architecture 110/110, and Contract 5/5 passing. A fresh commit and exact-head CI run are required before merge.

## CI follow-up correction — 2026-08-27

The initial class-level collection marker did not serialize the separate host fixture as observed in the second CI run. The final fix is assembly-level xUnit parallelization disabled for this small host-backed contract-test project, with no product or workflow change. The exact repository unit sequence passed locally afterward with Core 935/935, Architecture 110/110, and Contract 5/5.

## CI diagnostics follow-up — 2026-08-27

Run `33045186858` confirmed the host failure remains after serialization; the first request returned HTTP 500 in 14 seconds and the subsequent requests failed immediately. Added test-only response-body diagnostics so the next CI run identifies the server exception. Local targeted build and contract tests remain green. Fresh exact-head CI run is pending.

## CI root-cause follow-up — 2026-08-27

Run `33045552935` provided the evidence previously hidden by the assertion: SQL error 4060, `Cannot open database "PegasusDevelopment"`, from `DevelopmentOfflineAuthenticationHandler.HandleAuthenticateAsync` during the anonymous OpenAPI request. The existing contract factory now replaces `IAuthenticationService` with a test-only no-op implementation, leaving product authentication unchanged and allowing the real host's anonymous OpenAPI route to be tested without SQL. Targeted Release build and contract tests pass 5/5 locally. Fresh exact-head CI is required.

## Final CI and review reconciliation — 2026-08-27

- Final implementation head: `ff2fb8bf4e14bc3e58c22d4864ca83e33ec32448` on `task/openapi-snapshot`.
- Exact-head repository-check run `33048018900` is terminal green: changes, documentation, local-development-scripts, reference-data, unit, browser, SQL integration shards 1/2/3, and sql-integration-coverage. Infrastructure was skipped by the workflow as expected.
- The final contract test compares canonical normalized host output with the raw committed snapshot bytes. `.gitattributes` now explicitly pins LF for both tracked snapshots; `git check-attr` verified the rule and the exporter remains LF/no-BOM.
- The disabled-gateway factory also uses the test-only authentication override, so the suite does not require SQL Server for anonymous route composition checks. Local rebuilt Contract tests pass 5/5.
- Hilbert independently re-reviewed final head `ff2fb8bf` after the canonical-byte correction and returned `PASS` (merge-ready). The prior review blocker and stale predecessor-head reference are superseded by this final evidence.
