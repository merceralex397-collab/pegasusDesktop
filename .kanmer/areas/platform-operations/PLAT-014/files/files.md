# File map — PLAT-014

## Direct change surface

- `docs/desktop/10-security-observability-performance/telemetry-queries.md` and `telemetry-volume.md` — new files.
- `docs/current-architecture.md` — the telemetry dimensions and the correlation join as retained facts.
- `docs/operations.md` — note the uncapped-window constraint on any telemetry check.

## Context files

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-14`
- Plan detail: same file § 2 (Facts — Telemetry, the cap, adaptive sampling, the two alert rules), § 3 (ADR-0109 row and the OpenTelemetry deviation), § 7 (the blind-window risk)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 18.2 Central telemetry `:1215-1227`; § 9 Forced updates and compatibility `:467-525`; § 16.2 External provider resilience `:1128-1136`
- Repository evidence:
  - `src/Pegasus.Web/Program.cs:193-199` — App Insights registered only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is present, with `SetAzureTokenCredential` because ingestion is Entra-authenticated; the comment records that the Web host was uninstrumented for thirty days (PLAT-034)
  - `src/Pegasus.Worker/Program.cs:8-43` — `AddApplicationInsightsTelemetryWorkerService()` plus `ConfigureFunctionsApplicationInsights()` and the same explicit credential
  - `infra/modules/platform.bicep:46-55` — the Log Analytics workspace (`PerGB2018`, 31-day retention); `:56-67` the App Insights component with `DisableLocalAuth: true`; `:576-616` `pegasus-prod-web-http5xx`; `:617-689` `pegasus-prod-application-exceptions`
  - `docs/current-architecture.md:160-177` — the cap, adaptive sampling, and that the Worker produces most of the volume
  - New: the `/api/v1` correlation-id and client-version hook from `DSK-03-02`; the minimum-version middleware and `GET /api/v1/client-compatibility` from `DSK-04-06`
- Binding decisions:
  - **ADR-0109** (to be authored) — desktop diagnostics bundle + existing App Insights; **no** OpenTelemetry collector fleet. `configuring-opentelemetry-dotnet` is on the do-not-load table (`docs/desktop/12-agent-tooling/skill-routing.md`).
  - **L-01** — the gateway is `Pegasus.Web` evolved in place; telemetry changes ship with the existing Container App.
  - **Azure rule** — reads are free; any cap or alert change is a separate approved write ([[DSK-10-16]]).
- Depends on: `DSK-03-02` (`/api/v1` route group with correlation id and client-version header hook), `DSK-04-06` (minimum client version and the compatibility endpoint).

## Ripple effects

- [ ] Every `/api/v1` request telemetry item carries `ClientVersion`, `ClientChannel` and `CorrelationId`; no raw subject id, token or personal data appears in any dimension.
- [ ] `DesktopClientBlocked` and `DesktopUpdateRequired` are named custom events with counts, not parsed log text.
- [ ] Provider dependency timings are tagged with provider and outcome class through the existing dependency telemetry.
- [ ] The initializer is registered only when the connection string is present, so local and offline runs stay uninstrumented.
- [ ] The KQL query file exists and each query states its table, range and healthy shape.
- [ ] The dimensions are observed in App Insights during an uncapped window, with results attached.
- [ ] A volume measurement with its query, period and numbers is written down, and the quota conclusion is recorded without any Azure change being made.

## Out of scope

- **Azure**: no write from this ticket. Every Azure MCP call is read-only (`monitor`, `applicationinsights`, `group_resource_list`, `pricing`). Raising the daily cap or adding an alert rule is [[DSK-10-16]] and needs exact-target approval (`docs/runbook.md` § Live-operation approval matrix, "Change or use an Azure service"), mirrored in `docs/desktop/11-azure-disposition/README.md`. The gateway **deployment** itself is an operator-run release under the same matrix.
- **Scope boundary**: may touch `src/Pegasus.Web` telemetry composition, the API/integration test projects, and documentation. Must not touch `infra/modules/platform.bicep` — that is [[DSK-10-16]]. Must not add an Application Insights SDK to the desktop (plan § 2 assumption). Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: the 0.1 GB/day cap resetting at 03:00Z means a working-hour verification returns empty and looks like a failure — verify inside an uncapped window; adaptive sampling is on, so a count derived from sampled requests must use `itemCount`, not a raw row count; a high-cardinality dimension (subject id, case id) both costs quota and leaks; `configuring-opentelemetry-dotnet` is on the do-not-load table.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.
