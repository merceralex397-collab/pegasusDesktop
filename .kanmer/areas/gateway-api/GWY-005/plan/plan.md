# Plan — GWY-005: DSK-03-05 · Kiota client generation script, tool pin, committed output and CI no-op check

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Chosen approach

Generate the desktop's typed API client from `openapi/pegasus-v1.json` with Kiota through `eng/api/Generate-ApiClient.ps1`, pin the Kiota tool in `.config/dotnet-tools.json`, commit the generated code into `src/Pegasus.Desktop.Infrastructure/Api/Generated/`, and make CI fail when regeneration would change the tree.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [Kiota .NET quickstart](https://learn.microsoft.com/openapi/kiota/quickstarts/dotnet) confirms generation from an OpenAPI description and the generated-client dependency requirement. The ticket remains authoritative for the pinned tool/version and repository generation script.


## Ordered implementation steps

1. Orient. Read `docs/desktop/03-gateway-api-and-data/README.md` § 3 row *OpenAPI & client*, § 2 assumption A-3 and § 7, then `get_doc_gates <this ticket id>` and `take_ticket`.
2. Use `microsoft_docs_fetch` on <https://learn.microsoft.com/openapi/kiota/quickstarts/dotnet> and confirm the current tool package id, the `kiota generate` argument names (`--openapi`, `--language`, `--class-name`, `--namespace-name`, `--output`, `--clean-output`) and the runtime packages the generated code needs. Record the fetch date in the ticket research document.
3. Pin the tool in `.config/dotnet-tools.json` with an exact version and `"rollForward": false`, matching the `dotnet-ef` entry already there. Run `dotnet tool restore` and confirm it resolves offline-repeatably.
4. Add `eng/api/Generate-ApiClient.ps1` that runs `dotnet tool run kiota generate --openapi ./openapi/pegasus-v1.json --language CSharp --class-name PegasusApiClient --namespace-name Pegasus.Desktop.Infrastructure.Api.Generated --output ./src/Pegasus.Desktop.Infrastructure/Api/Generated --clean-output`, then normalises line endings, and exits non-zero on any failure. Keep it beside `eng/api/Export-OpenApiDocument.ps1` from [[DSK-03-04]].
5. Run the script and commit the generated tree. Add the Kiota runtime package references (`Microsoft.Kiota.Abstractions` and the serialization packages the quickstart lists) to `src/Pegasus.Desktop.Infrastructure` through central package management.
6. Build the desktop infrastructure project. If analyzer warnings from generated code break `TreatWarningsAsErrors`, add `src/Pegasus.Desktop.Infrastructure/Api/Generated/Directory.Build.props` that disables analysis **for that folder only** (`<EnableNETAnalyzers>false</EnableNETAnalyzers>`, `<NoWarn>` for the specific ids). Never lower the repository-wide policy in the root `Directory.Build.props` — this is the § 7 trap, and A-3 exists precisely to be resolved here. Record which option was taken and why in the ticket plan.
7. Assert the boundary: extend `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` with a fact that the desktop UI project references the generated client only through `Pegasus.Desktop.Infrastructure`, and that no hand-written type in the desktop duplicates a generated DTO name (proposal § 10.3 "prevent handwritten duplicate DTOs").
8. Add the no-op check to the existing `unit` job in `.github/workflows/ci.yml`: a step that runs `dotnet tool restore`, `pwsh ./eng/api/Generate-ApiClient.ps1`, then `git diff --exit-code src/Pegasus.Desktop.Infrastructure/Api/Generated`. Done means the job fails with a readable message naming the regeneration command when the tree differs.
9. Verify determinism: run the script twice in a clean worktree and confirm the second run produces no diff. If Kiota emits a timestamp or machine-specific comment, strip it in the script's normalisation step rather than accepting a dirty tree.
10. Document the regeneration contract inside `eng/api/Generate-ApiClient.ps1` as a comment header (not a new `.md` — the CI `documentation` job rejects Markdown outside the allowed roots).
11. Run `dotnet build Pegasus.slnx -c Release` and the architecture tests. Done means green with zero warnings.
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the ticket plan.

## Acceptance conditions

- [ ] Kiota is pinned in `.config/dotnet-tools.json` with `rollForward: false`.
- [ ] `eng/api/Generate-ApiClient.ps1` regenerates the client deterministically; a second run leaves no diff.
- [ ] The generated client is committed and compiles under `TreatWarningsAsErrors` without changing the repository-wide analysis policy.
- [ ] CI fails when the committed generated tree does not match the current OpenAPI document.
- [ ] The desktop references only the generated client; no hand-written duplicate DTO exists.

## Verification

- [ ] `dotnet tool restore && pwsh ./eng/api/Generate-ApiClient.ps1 && git diff --exit-code src/Pegasus.Desktop.Infrastructure/Api/Generated` — expected: exit code 0, no output.
- [ ] `dotnet build Pegasus.slnx -c Release` — expected: `Build succeeded`, `0 Warning(s)`.

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `.config/dotnet-tools.json`, `eng/api/**`, `src/Pegasus.Desktop.Infrastructure/Api/**`, `tests/Pegasus.ArchitectureTests`, central package management and the `unit` job in `.github/workflows/ci.yml`. Must not touch `src/Pegasus.Web` or `openapi/pegasus-v1.json` (that file is [[DSK-03-04]]'s output).
- **Traps**: never lower `TreatWarningsAsErrors` or `AnalysisLevel` at the repository root to make generated code compile — scope any suppression to the generated folder. Adding a CI job on a private repository bills Windows minutes at 2× (C-01), so extend the existing `unit` job. The desktop projects come from area 02; if `src/Pegasus.Desktop.Infrastructure` does not exist yet, stop rather than creating it here.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
