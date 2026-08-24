---
id: PLAT-009
type: ticket
title: >-
  DSK-10-09 · Desktop diagnostics: bounded redacted rolling logs, session and
  correlation ids, exportable bundle
status: backlog
area: platform-operations
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-10
  - phase-1
  - tier-9
groups:
  - EPIC-011
  - HZN-002
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:10:26.581Z'
updated: '2026-08-24T08:10:26.581Z'
---

## What

Complete the desktop diagnostics surface: structured rolling local logs with a bounded size and retention, redaction by default, a per-launch session identifier, the API correlation identifier on every request-related entry, and an "Export diagnostic bundle" action that packages the logs with app, Windows, package and dependency versions plus the last compatibility response.

## Why

ADR-0109 makes the desktop diagnostics bundle plus the **existing** Application Insights the whole observability answer — no new telemetry fleet. That choice only works if the bundle is genuinely sufficient, and it has to be, because the production Log Analytics workspace runs a 0.1 GB/day cap resetting at 03:00Z that the estate exhausts within hours (`docs/operations.md:363-369`, `docs/current-architecture.md:160-177`, PLAT-034): for most of each UK working day there is no central signal at all. Proposal §18.1 `:1204-1213` lists exactly what the bundle must contain. Operator-visible consequence: a support call in a capped window has nothing to diagnose from unless the operator can export a bundle. Siblings: [[DSK-10-07]] (file hygiene for the same folders), [[DSK-10-14]] (the gateway side), [[DSK-10-17]] (crash path writes a bundle), [[DSK-10-01]] (pattern list).

## Source of truth

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

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`; win-dev-skills v0.5.0 `f1028dd5`) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`) for the logging/performance checklist. Do **not** load `configuring-opentelemetry-dotnet`.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `Microsoft.Extensions.Logging` scopes, source-generated `LoggerMessage`, and `Package.Current.Id.Version` / `Windows.System.Profile.AnalyticsInfo` for the version block
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, proposal `:1204-1227`, `docs/current-architecture.md:160-177` (so the quota gap is understood as the reason for this ticket) and what `DSK-02-07`/`DSK-02-11` already built. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-09-desktop-diagnostics` from `dev`.
3. Define the log entry shape once, as a structured record written through source-generated `LoggerMessage` methods: timestamp (UTC, round-trip format), level, category, session id, correlation id, operation state (`not-started`/`running`/`succeeded`/`failed`/`cancelled`/`uncertain` — proposal §16.1, shared with [[DSK-10-17]]), event name, and a small typed property bag. No interpolated message strings carrying data.
4. Generate the per-launch session id once in `App.xaml.cs` (a GUID), put it in a logging scope that covers the whole process, and write it into the first log line together with app version, Windows build and package identity.
5. Propagate the API correlation id: the HTTP pipeline built by `DSK-02-06` already sets `X-Correlation-Id`; add a logging scope so every entry produced during a request carries the same value, and record the value the gateway echoes back so a desktop log line can be joined to an App Insights request.
6. Implement redaction by default: a single `IDiagnosticRedactor` applied to every property before it is written, using the pattern list in `docs/desktop/10-security-observability-performance/threat-register.md` § Secret and PII pattern list ([[DSK-10-01]]). Redact tokens, passwords, connection strings, key-vault URI values, attachment bytes and file contents; replace personal data (names, addresses, registrations) with a stable hash so entries can still be correlated. Unit-test each pattern with a positive and a negative case.
7. Bound the log: rolling files with a maximum single-file size, a maximum total folder size and a maximum age, oldest-first eviction, and asynchronous writing that never blocks the UI thread (proposal §15.2 `:1095`). Reuse the storage locations and retention machinery from [[DSK-10-07]] rather than adding a second policy.
8. Complete the bundle: extend the export from `DSK-02-11` so the zip contains — the rolling logs; a `manifest.json` with app version, package identity and version, Windows version and build, .NET and Windows App SDK versions, WebView2 runtime version, machine name only if the operator consents, and the session ids covered; the last compatibility response from `GET /api/v1/client-compatibility` ([[DSK-10-14]], `DSK-04-06`); and the last N API failures with their correlation ids. No attachment content, no tokens, no credentials.
9. Surface the action: an "Export diagnostic bundle" command reachable from the app's Help/Settings surface with an `AutomationProperties.AutomationId` so `DSK-08-06`'s `winapp ui` harness can drive it; it writes to a user-chosen folder and shows the resulting path. Follow `docs/design/README.md` operator-copy rules — the button says what it does, not how it works.
10. Add the bundle schema test: build a bundle in a test, assert `manifest.json` has every required field, assert the zip contains no file matching the secret patterns (reuse the scanner from [[DSK-10-03]] against the extracted bundle), and assert the total size is bounded.
11. Prove sufficiency: reproduce one deliberately injected failure (for example a gateway 500 on a save) end to end, export the bundle, and show in the post-implementation report that the bundle alone identifies the failing operation, its correlation id and its state — without any central telemetry. This is the evidence ADR-0109 rests on.
12. Write the support procedure into `docs/runbook.md`: where the logs live, how an operator exports a bundle, what to check before sending it, and how to join a correlation id to an App Insights request during an uncapped window.
13. Update the threat register row "sensitive information in logs/temp files" with the redaction tests and the bundle scan ([[DSK-10-01]]).
14. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] Every log entry carries session id, correlation id (where a request is in flight), level, category and an operation state from the §16.1 set.
- [ ] Redaction is applied by default to every property, driven by the shared pattern list, with a positive and negative unit test per pattern.
- [ ] Log files roll and are bounded by single-file size, total folder size and age; writing is asynchronous and never blocks the UI thread.
- [ ] The exported bundle contains logs, a complete `manifest.json` version block, the last compatibility response and the recent API failures — and no token, credential or attachment content.
- [ ] A scan of the extracted bundle with [[DSK-10-03]]'s scanner reports no match.
- [ ] One injected failure is diagnosed from the bundle alone, recorded in the post-implementation report.

## Verification

- [ ] `dotnet test` on the desktop test project filtered to the redaction and bundle-schema tests — expected: all pass.
- [ ] `pwsh ./eng/packaging/Test-PackageSecrets.ps1 -PackagePath <msix> -AdditionalPath <extracted bundle folder>` — expected: exit 0.
- [ ] `winapp ui` script exercising the Export command against the Test/UAT stack — expected: a bundle file is produced at the reported path and the command completes without blocking the UI.

## Evidence tier

Tier 9 — Security/observability. Here that obliges correlation, redaction, health-adjacent version reporting and bounded failure recording — proved by the injected-failure walkthrough and by a clean secret scan of the bundle, not by the presence of a log file.

## Documentation changes

- `docs/runbook.md` — desktop diagnostics collection procedure for support.
- `docs/operations.md` — where desktop diagnostics bundles are collected and retained.
- `docs/desktop/10-security-observability-performance/threat-register.md` — redaction and bundle-scan evidence.
- `docs/capabilities.md` — a `DSK` row for the diagnostics bundle, once the `DSK` family exists (`DSK-00-08`).

## Guardrails

- **Azure**: no write. The desktop adds **no** Application Insights SDK (plan § 2 assumption; proposal §18.2 makes central desktop telemetry optional after stabilisation) — revisit only with pilot evidence and a recorded decision.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, the desktop test projects, `docs/runbook.md`, `docs/operations.md`. Must not touch `src/Pegasus.Web` telemetry — that is [[DSK-10-14]]. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: the App Insights blind window means the bundle is the primary support tool — an incomplete bundle has no fallback; a redactor applied at the sink instead of before formatting misses interpolated strings; synchronous file logging on the UI thread breaks the §15.1 navigation budget; `configuring-opentelemetry-dotnet` is on the do-not-load table and adding a collector fleet contradicts ADR-0109.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
