# Plan — PLAT-014

## Objective

Add the desktop-era dimensions to the gateway's existing Application Insights telemetry — client version, channel, blocked-client count, update-required responses, provider dependency timings and correlation-id propagation — verify them against the daily-quota window, and measure desktop-era ingestion volume before any quota change is proposed.

## Chosen approach

Proposal §18.2 `:1215-1227` keeps the existing Application Insights and lists exactly these uses: gateway requests and failures, worker checkpoints, third-party dependency timing, client-version distribution, blocked obsolete clients, update success/failure and pilot diagnostics. ADR-0109 forbids a new telemetry fleet, so these dimensions are the only central signal the conversion gets. The signal is also constrained: the Log Analytics workspace runs a **0.1 GB/day cap resetting at 03:00Z** which the estate exhausts within hours, so most of each UK working day is blind and the two alert rules cannot fire (`docs/operations.md:363-369`, `docs/current-architecture.md:160-177`, PLAT-034). That is why this ticket measures volume first and leaves the quota decision to [[DSK-10-16]]. Operator-visible consequence: nobody can answer "which version is each workstation on" or "how many clients were blocked today". Siblings: [[DSK-10-09]] (the desktop half), [[DSK-10-15]] (health), [[DSK-10-16]] (quota and alerts).

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. Keep `docs_todo: true`; planned desktop governing documents must not be linked until they exist on `origin/dev`.
- Use the ticket's Source of truth and its area plan until a real governing doc can be linked.

## Routing

- **Subagents**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (implementation); `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only verification and the volume measurement)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `appinsights-instrumentation` (azure-skills `1a03acfb`) → `azure-diagnostics` (same pin) → `dotnet-webapi` (dotnet/skills `98f84851`, plugin `dotnet-aspnetcore`). Do **not** load `configuring-opentelemetry-dotnet`.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** `monitor` and `applicationinsights` for the KQL checks and the volume query; Microsoft Learn (`microsoft_docs_search`) for `ITelemetryInitializer` and custom-dimension limits
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

1. Orientation. Read the plan row, proposal `:1215-1227`, `docs/current-architecture.md:160-177` and `src/Pegasus.Web/Program.cs:193-199`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-14-gateway-telemetry-dimensions` from `dev`.
3. Implement one `ITelemetryInitializer` in `src/Pegasus.Web` that adds, to every request telemetry item on `/api/v1`: `ClientVersion` (from `X-Pegasus-Client-Version`), `ClientChannel` (pilot/production, from the same header set or the compatibility response), `CorrelationId` (the `X-Correlation-Id` the desktop sends, so a desktop log line joins an App Insights request), and `StaffSubjectId` **hashed**, never raw. Register it only inside the existing `if (!string.IsNullOrWhiteSpace(...APPLICATIONINSIGHTS_CONNECTION_STRING))` block so local and offline runs stay uninstrumented.
4. Use `microsoft_docs_search` for `ITelemetryInitializer custom dimensions cardinality` and record in the plan document the cardinality limit and why `ClientVersion`/`ClientChannel` are safe (a handful of values) while a per-user or per-case dimension is not.
5. Emit the compatibility outcomes as named custom events, not as log text: `DesktopClientBlocked` (with `ClientVersion` and the configured minimum) and `DesktopUpdateRequired`, raised by the middleware from `DSK-04-06`. A count that has to be derived by parsing message strings is not a metric.
6. Emit provider dependency timings through the standard dependency telemetry for Box, DVLA, DVSA and Graph calls made by the gateway, tagging each with the provider name and the outcome class from the taxonomy in `DSK-07-19` (`terminal`/`transient`/`unknown`). Do not add a second timing mechanism where dependency tracking already exists.
7. Add contract tests in `tests/Pegasus.Api.ContractTests` (or `tests/Pegasus.IntegrationTests`) using a fake telemetry channel: assert that a request carrying `X-Pegasus-Client-Version` produces a telemetry item with the dimension set; that a blocked client raises `DesktopClientBlocked` exactly once; that no raw subject id, token or personal data appears in any dimension.
8. Write the KQL checks into `docs/desktop/10-security-observability-performance/telemetry-queries.md`: client-version distribution over 7 days; blocked-client count per day; update-required count per day; p95 dependency duration by provider; a join from `CorrelationId` to `requests` and `exceptions`. Each query states the table, the time range and what "healthy" looks like.
9. **Operator step** — release the gateway change to production by the existing route (`pegasus-release`), then run the KQL checks **inside an uncapped window** (the cap resets at 03:00Z, so the window is the early morning UTC period before ingestion stops). Hand back the query results showing the dimensions present. Running them during a UK working hour will return empty and prove nothing — that is the PLAT-034 blind window, not a defect in this ticket.
10. Measure desktop-era volume: with `pegasus-azure-auditor` and Azure MCP read-only `monitor`, query `Usage` / `_BilledSize` by table over a representative period, split Worker versus Web versus the new custom events, and estimate the daily total the desktop era will produce. Write the result into `docs/desktop/10-security-observability-performance/telemetry-volume.md` with the query, the period and the numbers.
11. Record the conclusion without acting on it: whether the current 0.1 GB/day cap can hold a working day of desktop-era ingestion, and what raising it would cost (use `azure-cost` read-only for the per-GB price). The decision and any change belong to [[DSK-10-16]]; this ticket produces the evidence only.
12. Update `docs/current-architecture.md` with the retained telemetry facts — the dimensions now emitted and the correlation join — and cross-reference the volume note.
13. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --filter "FullyQualifiedName~Telemetry"` — expected: dimension and custom-event facts pass; the no-personal-data fact passes.
- [ ] Azure MCP `monitor` KQL run in an uncapped window — expected: `requests | where isnotempty(customDimensions.ClientVersion)` returns rows for the new release.
- [ ] Azure MCP `monitor` usage query — expected: a per-table `_BilledSize` breakdown covering the measurement period, matching the numbers written into `telemetry-volume.md`.

## Risks and constraints

- **Azure**: no write from this ticket. Every Azure MCP call is read-only (`monitor`, `applicationinsights`, `group_resource_list`, `pricing`). Raising the daily cap or adding an alert rule is [[DSK-10-16]] and needs exact-target approval (`docs/runbook.md` § Live-operation approval matrix, "Change or use an Azure service"), mirrored in `docs/desktop/11-azure-disposition/README.md`. The gateway **deployment** itself is an operator-run release under the same matrix.
- **Scope boundary**: may touch `src/Pegasus.Web` telemetry composition, the API/integration test projects, and documentation. Must not touch `infra/modules/platform.bicep` — that is [[DSK-10-16]]. Must not add an Application Insights SDK to the desktop (plan § 2 assumption). Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: the 0.1 GB/day cap resetting at 03:00Z means a working-hour verification returns empty and looks like a failure — verify inside an uncapped window; adaptive sampling is on, so a count derived from sampled requests must use `itemCount`, not a raw row count; a high-cardinality dimension (subject id, case id) both costs quota and leaks; `configuring-opentelemetry-dotnet` is on the do-not-load table.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Simplification pass

Before the PR, independently assess the branch diff for unnecessary abstractions, duplicated policy, or scope expansion and record the disposition here.
