# File map — PLAT-009

## Change surface

- `docs/runbook.md` — desktop diagnostics collection procedure for support.
- `docs/operations.md` — where desktop diagnostics bundles are collected and retained.
- `docs/desktop/10-security-observability-performance/threat-register.md` — redaction and bundle-scan evidence.
- `docs/capabilities.md` — a `DSK` row for the diagnostics bundle, once the `DSK` family exists (`DSK-00-08`).

## Context files and evidence

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-09`
- Plan detail: same file § 1 (§18 coverage), § 2 (Facts — the quota gap), § 3 (ADR-0109 row and the OpenTelemetry deviation), § 4 (target state), § 7 ("Secrets leaking through desktop logs or the diagnostics bundle")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 18.1 Desktop diagnostics `:1204-1213`; § 18.2 Central telemetry `:1215-1227`; § 11.1 `:641-651`; § 16.1 Operation model `:1117-1126`
- Repository evidence:
  - `docs/operations.md:363-369` — the 0.1 GB/day cap resetting at 03:00Z and why every working-hour check comes back empty
  - `docs/current-architecture.md:160-177` — PLAT-034 in full, including that sampling is on and the Worker produces most volume
  - `src/Pegasus.Web/Program.cs:193-199` — how the gateway is instrumented (Entra ingestion, credential supplied explicitly), for the correlation story
  - `src/Pegasus.Web/Program.cs:954` — `/diagnostics/version` returning `version` and `sourceSha`, the shape the bundle's server-identity section mirrors
  - New: the host logging configuration from `DSK-02-07`, the diagnostics writer from `DSK-02-06`, and the first bundle export and unhandled-exception handler from `DSK-02-11` — this ticket completes them rather than starting again
- Binding decisions:
  - **ADR-0109** (to be authored) — desktop diagnostics bundle + existing App Insights; **no** OpenTelemetry collector fleet, and the `configuring-opentelemetry-dotnet` skill is explicitly not loaded (plan § 3 deviation, and `docs/desktop/12-agent-tooling/skill-routing.md` § "Not applicable — do not load").
  - **ADR-0104** — bounded local state only.
  - **L-02** — verification runs on the local Test/UAT stack.
- Depends on: `DSK-02-07` (Generic Host, logging), `DSK-02-11` (unhandled-exception handler and first bundle export).

## Ripple effects and acceptance

- [ ] Every log entry carries session id, correlation id (where a request is in flight), level, category and an operation state from the §16.1 set.
- [ ] Redaction is applied by default to every property, driven by the shared pattern list, with a positive and negative unit test per pattern.
- [ ] Log files roll and are bounded by single-file size, total folder size and age; writing is asynchronous and never blocks the UI thread.
- [ ] The exported bundle contains logs, a complete `manifest.json` version block, the last compatibility response and the recent API failures — and no token, credential or attachment content.
- [ ] A scan of the extracted bundle with [[DSK-10-03]]'s scanner reports no match.
- [ ] One injected failure is diagnosed from the bundle alone, recorded in the post-implementation report.

## Deliberately out of scope

- **Azure**: no write. The desktop adds **no** Application Insights SDK (plan § 2 assumption; proposal §18.2 makes central desktop telemetry optional after stabilisation) — revisit only with pilot evidence and a recorded decision.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, the desktop test projects, `docs/runbook.md`, `docs/operations.md`. Must not touch `src/Pegasus.Web` telemetry — that is [[DSK-10-14]]. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: the App Insights blind window means the bundle is the primary support tool — an incomplete bundle has no fallback; a redactor applied at the sink instead of before formatting misses interpolated strings; synchronous file logging on the UI thread breaks the §15.1 navigation budget; `configuring-opentelemetry-dotnet` is on the do-not-load table and adding a collector fleet contradicts ADR-0109.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.
