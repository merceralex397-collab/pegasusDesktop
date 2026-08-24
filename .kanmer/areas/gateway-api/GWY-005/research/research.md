# Research — GWY-005: DSK-03-05 · Kiota client generation script, tool pin, committed output and CI no-op check

## Question

Generate the desktop's typed API client from `openapi/pegasus-v1.json` with Kiota through `eng/api/Generate-ApiClient.ps1`, pin the Kiota tool in `.config/dotnet-tools.json`, commit the generated code into `src/Pegasus.Desktop.Infrastructure/Api/Generated/`, and make CI fail when regeneration would change the tree.

## Evidence examined

- Plan row: `docs/desktop/03-gateway-api-and-data/README.md` § 5 — `DSK-03-05`
- Plan detail: same file § 3 — row *OpenAPI & client*; § 2 assumption A-3; § 7 — *`TreatWarningsAsErrors` + generated code*
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 10.3 Generated client, § 21.2 CI stages
- Official documentation: Kiota for .NET — <https://learn.microsoft.com/openapi/kiota/quickstarts/dotnet>
- Repository evidence:
  - `.config/dotnet-tools.json` — today pins only `dotnet-ef 10.0.10` with `"rollForward": false`; the same discipline applies to `Microsoft.OpenApi.Kiota`
  - `Directory.Build.props:7-8` — `AnalysisLevel=latest-recommended`, `TreatWarningsAsErrors=true`; generated code must satisfy this or be excluded by a folder-level `Directory.Build.props`
  - `.github/workflows/ci.yml:130-146` — the `unit` job on `windows-latest` where the no-op check belongs
  - `global.json` — SDK pinned to `10.0.302`
- Binding decisions:
  - C-01 — private Windows runner minutes bill at 2×; add the check to an existing job rather than a new one.
  - L-01 — the client targets the one gateway; there is no second base address to configure.
- Depends on: `DSK-03-04` produces `openapi/pegasus-v1.json`; `DSK-02-06` creates `src/Pegasus.Desktop.Infrastructure` and its HTTP pipeline.

## Scope and constraints

Proposal § 10.3 requires the client to be generated in a controlled build step, committed according to repository practice, and never regenerated unpredictably on developer machines — and forbids hand-written duplicate DTOs. Operator-visible consequence: a desktop build can never drift from the contract the gateway actually serves, so an endpoint rename cannot ship as a runtime 404 in the field. This is the last Phase 2 foundation ticket in the epic; [[DSK-03-06]] onwards assume the pipeline exists.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [Kiota .NET quickstart](https://learn.microsoft.com/openapi/kiota/quickstarts/dotnet) confirms generation from an OpenAPI description and the generated-client dependency requirement. The ticket remains authoritative for the pinned tool/version and repository generation script.

- **Azure**: no write.
- **Scope boundary**: may touch `.config/dotnet-tools.json`, `eng/api/**`, `src/Pegasus.Desktop.Infrastructure/Api/**`, `tests/Pegasus.ArchitectureTests`, central package management and the `unit` job in `.github/workflows/ci.yml`. Must not touch `src/Pegasus.Web` or `openapi/pegasus-v1.json` (that file is [[DSK-03-04]]'s output).
- **Traps**: never lower `TreatWarningsAsErrors` or `AnalysisLevel` at the repository root to make generated code compile — scope any suppression to the generated folder. Adding a CI job on a private repository bills Windows minutes at 2× (C-01), so extend the existing `unit` job. The desktop projects come from area 02; if `src/Pegasus.Desktop.Infrastructure` does not exist yet, stop rather than creating it here.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
