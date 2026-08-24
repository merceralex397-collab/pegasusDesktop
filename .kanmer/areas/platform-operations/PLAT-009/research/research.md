# Research — PLAT-009

## Question

Complete the desktop diagnostics surface: structured rolling local logs with a bounded size and retention, redaction by default, a per-launch session identifier, the API correlation identifier on every request-related entry, and an "Export diagnostic bundle" action that packages the logs with app, Windows, package and dependency versions plus the last compatibility response.

## Findings

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

## Implications for this ticket

ADR-0109 makes the desktop diagnostics bundle plus the **existing** Application Insights the whole observability answer — no new telemetry fleet. That choice only works if the bundle is genuinely sufficient, and it has to be, because the production Log Analytics workspace runs a 0.1 GB/day cap resetting at 03:00Z that the estate exhausts within hours (`docs/operations.md:363-369`, `docs/current-architecture.md:160-177`, PLAT-034): for most of each UK working day there is no central signal at all. Proposal §18.1 `:1204-1213` lists exactly what the bundle must contain. Operator-visible consequence: a support call in a capped window has nothing to diagnose from unless the operator can export a bundle. Siblings: [[DSK-10-07]] (file hygiene for the same folders), [[DSK-10-14]] (the gateway side), [[DSK-10-17]] (crash path writes a bundle), [[DSK-10-01]] (pattern list).

## Boundaries and assumptions

- **Azure**: no write. The desktop adds **no** Application Insights SDK (plan § 2 assumption; proposal §18.2 makes central desktop telemetry optional after stabilisation) — revisit only with pilot evidence and a recorded decision.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, the desktop test projects, `docs/runbook.md`, `docs/operations.md`. Must not touch `src/Pegasus.Web` telemetry — that is [[DSK-10-14]]. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: the App Insights blind window means the bundle is the primary support tool — an incomplete bundle has no fallback; a redactor applied at the sink instead of before formatting misses interpolated strings; synchronous file logging on the UI thread breaks the §15.1 navigation budget; `configuring-opentelemetry-dotnet` is on the do-not-load table and adding a collector fleet contradicts ADR-0109.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Research conclusion

The ticket evidence identifies the target, routing and verification. It does not create or link a planned canonical governing document; `docs_todo` remains accurate until one exists.
