# Checklist — PLAT-014

## Implementation

- [ ] 1. Orientation. Read the plan row, proposal `:1215-1227`, `docs/current-architecture.md:160-177` and `src/Pegasus.Web/Program.cs:193-199`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.

- [ ] 2. Branch `task/dsk-10-14-gateway-telemetry-dimensions` from `dev`.

- [ ] 3. Implement one `ITelemetryInitializer` in `src/Pegasus.Web` that adds, to every request telemetry item on `/api/v1`: `ClientVersion` (from `X-Pegasus-Client-Version`), `ClientChannel` (pilot/production, from the same header set or the compatibility response), `CorrelationId` (the `X-Correlation-Id` the desktop sends, so a desktop log line joins an App Insights request), and `StaffSubjectId` **hashed**, never raw. Register it only inside the existing `if (!string.IsNullOrWhiteSpace(...APPLICATIONINSIGHTS_CONNECTION_STRING))` block so local and offline runs stay uninstrumented.

- [ ] 4. Use `microsoft_docs_search` for `ITelemetryInitializer custom dimensions cardinality` and record in the plan document the cardinality limit and why `ClientVersion`/`ClientChannel` are safe (a handful of values) while a per-user or per-case dimension is not.

- [ ] 5. Emit the compatibility outcomes as named custom events, not as log text: `DesktopClientBlocked` (with `ClientVersion` and the configured minimum) and `DesktopUpdateRequired`, raised by the middleware from `DSK-04-06`. A count that has to be derived by parsing message strings is not a metric.

- [ ] 6. Emit provider dependency timings through the standard dependency telemetry for Box, DVLA, DVSA and Graph calls made by the gateway, tagging each with the provider name and the outcome class from the taxonomy in `DSK-07-19` (`terminal`/`transient`/`unknown`). Do not add a second timing mechanism where dependency tracking already exists.

- [ ] 7. Add contract tests in `tests/Pegasus.Api.ContractTests` (or `tests/Pegasus.IntegrationTests`) using a fake telemetry channel: assert that a request carrying `X-Pegasus-Client-Version` produces a telemetry item with the dimension set; that a blocked client raises `DesktopClientBlocked` exactly once; that no raw subject id, token or personal data appears in any dimension.

- [ ] 8. Write the KQL checks into `docs/desktop/10-security-observability-performance/telemetry-queries.md`: client-version distribution over 7 days; blocked-client count per day; update-required count per day; p95 dependency duration by provider; a join from `CorrelationId` to `requests` and `exceptions`. Each query states the table, the time range and what "healthy" looks like.

- [ ] 9. **Operator step** — release the gateway change to production by the existing route (`pegasus-release`), then run the KQL checks **inside an uncapped window** (the cap resets at 03:00Z, so the window is the early morning UTC period before ingestion stops). Hand back the query results showing the dimensions present. Running them during a UK working hour will return empty and prove nothing — that is the PLAT-034 blind window, not a defect in this ticket.

- [ ] 10. Measure desktop-era volume: with `pegasus-azure-auditor` and Azure MCP read-only `monitor`, query `Usage` / `_BilledSize` by table over a representative period, split Worker versus Web versus the new custom events, and estimate the daily total the desktop era will produce. Write the result into `docs/desktop/10-security-observability-performance/telemetry-volume.md` with the query, the period and the numbers.

- [ ] 11. Record the conclusion without acting on it: whether the current 0.1 GB/day cap can hold a working day of desktop-era ingestion, and what raising it would cost (use `azure-cost` read-only for the per-GB price). The decision and any change belong to [[DSK-10-16]]; this ticket produces the evidence only.

- [ ] 12. Update `docs/current-architecture.md` with the retained telemetry facts — the dimensions now emitted and the correlation join — and cross-reference the volume note.

- [ ] 13. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --filter "FullyQualifiedName~Telemetry"` — expected: dimension and custom-event facts pass; the no-personal-data fact passes.
- [ ] Azure MCP `monitor` KQL run in an uncapped window — expected: `requests | where isnotempty(customDimensions.ClientVersion)` returns rows for the new release.
- [ ] Azure MCP `monitor` usage query — expected: a per-table `_BilledSize` breakdown covering the measurement period, matching the numbers written into `telemetry-volume.md`.

## Progress notes

Record factual progress here.
