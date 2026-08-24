# EPIC-004 · Area 03 — gateway API and data

Read once before working any ticket in this epic. Ticket-specific detail lives in the ticket; this file is what every ticket here shares.

## What the area delivers

The versioned `/api/v1` JSON gateway the native desktop calls, **evolved inside the existing `src/Pegasus.Web`** — route groups behind a `Features:DesktopGateway` composition gate, the shared `src/Pegasus.Contracts` DTO/problem vocabulary, an OpenAPI document that is the contract, a Kiota-generated client committed into `src/Pegasus.Desktop.Infrastructure/Api/Generated/`, and the concurrency, idempotency, audit, paging and problem-details rules every endpoint follows. Board area `gateway-api` (GWY-001…GWY-018 = DSK-03-01…DSK-03-18).

## Proposal coverage

`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` §4 and §4.1 (placement reason per endpoint group), §5.2–5.3 (one deployable, desktop → generated client → `/api/v1`), §10.2–10.6 (API style, generated client, concurrency, transactions and audit, query strategy), §13 (data needs per capability), §16.1–16.2 gateway half, §21.2 stage 6, §24 Phase 2 plus the API half of Phases 3–8. Out of scope here: the token endpoint and session lifecycle (area 04), the desktop HTTP pipeline (area 02), UI (area 06), provider adapters (area 07), packaging (area 09).

## Decisions, assumptions and deviations that bind every ticket

- **L-01** — the gateway is `Pegasus.Web` evolved in place: versioned `/api/v1` route groups beside the Razor Pages, same Container App, same `pegasus-release` route. No new deployment unit, so no ADR for one and no Azure change for hosting.
- **L-02** — Test/UAT is the local production-mimicking stack (local gateway and Worker, Azurite, LocalDB, replay adapters). ADR-0014 stands: asking for an Azure dev/test/staging resource is out of bounds.
- **L-04** — every ticket names its subagent, skills and MCP tools; **L-05** — the board is seeded from these plans.
- **L-03** binds DSK-03-14 only: the gateway report renderer is retained until golden-file parity passes (ADR-0108).
- **C-01** — the repositories become private at completion; private Windows runner minutes bill at 2×, so extend an existing CI job rather than adding one (bites DSK-03-04, DSK-03-05).
- **D-001** — the fork becomes the single release source at the first production gateway change, which is this surface reaching production.
- **Deviation (recorded)** — `expectedVersion` + `editLeaseToken` as explicit body fields, not `If-Match`: proposal §10.4 offers either, and Core's semantics are per aggregate and lease-aware (`CaseWorkflowContracts.cs:182`).
- **Deviation (recorded)** — `OperatorLabels` moves to `Pegasus.Contracts` (DSK-03-16); the proposal is silent, the one-list-per-concept rail requires it.
- **Assumptions to re-check, not assume**: A-1 the existing Container App absorbs the JSON surface; A-3 Kiota output compiles under `TreatWarningsAsErrors` (resolved in DSK-03-05); A-4 Azure SQL S0 tolerates ten desktop clients (measured in DSK-03-17).
- **ADR block**: this conversion uses the reserved ADR-0100…ADR-0110, never "next free number". ADR-0101 and ADR-0103 govern this area and do not exist yet — every ticket carries `docs_todo: true`.
- **Azure**: reads are free; the only write this area implies is `Features__DesktopGateway=true` as a production Container App app setting, done once at the Phase 2 release under exact-target approval (`docs/runbook.md` § Live-operation approval matrix) and mirrored in `docs/desktop/11-azure-disposition/README.md`. No ticket here performs it.

## Exit gate and what proves it

`docs/desktop/03-gateway-api-and-data/README.md` § 4: the `openapi/pegasus-v1.json` snapshot test passes and regeneration is a no-op; the generated client compiles under the repository warning policy; every command endpoint has tests for authorized success, unauthorized, version conflict, lease conflict, operation-key replay, validation failure and the problem-details shape; every list endpoint has paging/filter/sort and newest-first tests; `Features:DesktopGateway=false` leaves no `/api/v1` route (404 test); the previous snapshot still validates against the current server for the supported client range; and the existing Razor page tests in `tests/Pegasus.IntegrationTests` stay green. DSK-03-18 is the independent audit that says the set is complete.

## Routing for this area

| Work | Subagent | Skills (pinned source) | MCP |
| --- | --- | --- | --- |
| Endpoint design and implementation | `pegasus-gateway-dev` (`.codex/agents/pegasus-gateway-dev.toml`) | `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) first, then `dotnet-webapi`, `minimal-api-file-upload` (dotnet/skills `98f84851`, plugin `dotnet-aspnetcore`), `microsoft-code-reference` (Microsoft Learn plugin) | Microsoft Learn `microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search`; Kanmer `get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item` |
| EF query shape for list/section endpoints | `pegasus-gateway-dev` | `optimizing-ef-core-queries` (dotnet/skills `98f84851`, plugin `dotnet-data`) | Microsoft Learn |
| Tests and gap analysis | `pegasus-test-engineer`, `pegasus-desktop-reviewer` | `code-testing-agent`, `run-tests`, `test-gap-analysis`, `assertion-quality`, `scaffold-dotnet-test-project` (dotnet/skills `98f84851`, plugin `dotnet-test`) | Kanmer |
| Client generation and CI check | `pegasus-gateway-dev`, `pegasus-release-packager` | `authoring-github-workflows` (dotnet/skills `98f84851`, `.agents/skills/`), `directory-build-organization` (plugin `dotnet-msbuild`) | Microsoft Learn (Kiota) |
| Independent review | `pegasus-desktop-reviewer` | `pegasus-desktop`, `microsoft-code-reference` — `winui-code-review` is **not** needed here | Microsoft Learn |

Do not load, per `docs/desktop/12-agent-tooling/skill-routing.md` § Not applicable: `azure-deploy`, `azure-prepare`, `entra-app-registration`, `dotnet-maui`/`dotnet-blazor` plugins, `winui-wpf-migration`, `configuring-opentelemetry-dotnet`, `dotnet-aot-compat`.

## Traps (README § 7) — every ticket inherits these

1. **Runtime-role grants.** Any new table or write path needs a `Grant*` migration mirrored in `scripts/Invoke-AzureDatabaseBootstrap.ps1` and the census in `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`, enforced by `scripts/Test-MigrationGrants.ps1` in CI. "Works locally, fails only in production" has shipped three times (PLAT-035).
2. **Composition gate off = 404.** Test the gate both ways; production enablement is an approval-gated app-setting change.
3. **Two policy engines.** API endpoints and MCP tools both call Core use cases. A rule that appears in an endpoint filter is a defect.
4. **TempData semantics.** Never port `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs`'s proposed-values/lease chaining; the desktop keeps that state in memory and sends explicit fields.
5. **`TreatWarningsAsErrors` + generated code.** Scope any Kiota analyzer suppression to the generated folder; never lower the repository policy.
6. **Linux publish.** `Pegasus.Web` still publishes `linux-x64` into the Playwright base image; no Windows-only package may enter it, and `Pegasus.Contracts` stays `net10.0` with no dependency beyond `System.Text.Json`.
7. **Coexistence.** Razor Pages and the API share Identity, OpenIddict, the rate limiters and the `Features:*` gates; adding bearer authentication must not change the cookie scheme defaults (`__Host-Pegasus`, `SameSite=Strict`).
8. **Rate limiting.** Extend the existing limiter configuration at `src/Pegasus.Web/Program.cs:275-327`; a second limiter mechanism is a defect.
9. **Observability blind spot.** App Insights ingestion is capped at 0.1 GB/day (PLAT-034); problem details with correlation ids and the desktop diagnostics bundle are the compensating evidence.
10. **Upstream drift.** Upstream `main` was 32 commits ahead at planning time; start Mail and Box endpoint work only after the first upstream sync (DSK-00-02).
11. **Pilot-ring compatibility.** Contract changes stay additive until the minimum client version advances; removing a field is a contract-test failure by design.
12. **Markdown placement.** Any new `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job; ticket-transient documents live in Kanmer.

## Read these before starting any ticket in this epic

- `docs/desktop/03-gateway-api-and-data/README.md` — this area's plan (§ 3 decisions, § 4 exit gate, § 7 traps)
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — the authoritative route, verb, right, concurrency-token and phase table
- `docs/desktop/README.md` — locked decisions, constraint C-01, routing legend
- `docs/desktop/00-governance-and-workflow/README.md` — branching, ADR block, board shape, ticket template
- `docs/desktop/12-agent-tooling/skill-routing.md` — exact skill names, pinned revisions, do-not-load table
- `AGENTS.md` — product invariants, simplicity rails, safety rails, Repository task workflow steps 1–6
- `docs/engineering.md` § Required evidence tiers and § One Core owner
- `src/Pegasus.Web/Program.cs` (composition root; `:275-327` limiters, `:525-530` form limits, `:939-964` endpoint mapping)
- `src/Pegasus.Web/Mcp/AutomationMcp.cs`, `AutomationMcpExtensions.cs`, `AutomationMcpErrors.cs`, `AutomationActorResolver.cs` — the existing gated machine ingress this surface is modelled on
- `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs` and `CaseCommandContracts.cs` — versions, leases, replay and the four conflict exceptions
- `src/Pegasus.Core/Identity/StaffAuthorization.cs` and `src/Pegasus.Core/Actors/StaffActorFactory.cs` — the right set and the claims → actor seam
