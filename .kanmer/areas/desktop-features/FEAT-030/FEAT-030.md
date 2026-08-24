---
id: FEAT-030
type: ticket
title: >-
  DSK-07-04 · Desktop Operations screen: intake status, integration health and
  retry bound to the gateway
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-07
  - phase-5
  - tier-7
groups:
  - EPIC-008
  - HZN-006
links: []
refs:
  - docs/frd/frd-12-operator-experience.md
docs_todo: true
archived: false
created: '2026-08-24T08:18:48.657Z'
updated: '2026-08-24T08:18:48.657Z'
---

## What

Build the native Operations screen in `src/Pegasus.Desktop`: retryable external work, active public upload links, and integration-health rows (Graph last successful cycle per mailbox, Box, DVLA/DVSA, update feed, minimum client version) — every value showing when it was obtained, retry commands enabled only when the gateway says they are eligible, and a disconnected state that is honest rather than blank.

## Why

Proposal § 13.10 makes integration health and failed-work review parity capabilities, and § 16.2 requires the client to show when data is cached and when it was obtained. Today this is `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` (236 lines) whose `LoadedAtUtc` is deliberately set only after a successful query (`:43-46`) so a failed load never claims freshness — the desktop must keep that property. This is the operator-visible half of [[DSK-07-01]] and [[DSK-07-02]]; without it, Phase 5's exit gate has no surface to demonstrate.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-04`
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.10 Administration and operations` → `Operations — replaces Pages/Operations/Index.cshtml.cs` (tables, AutomationIds `Operations.External.Table`, `Operations.External.Retry`, `Operations.Links.Revoke`, `Operations.Health.<Dependency>`)
- Cross-cutting UI contract: `docs/desktop/06-ui-design/screen-specs.md` § `Cross-cutting state contract`; `docs/desktop/06-ui-design/keyboard-and-accessibility.md`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.10 Administration and operations, § 16.2 External provider resilience, § 14.8 Notifications and errors
- Repository evidence: `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:41-70` (freshness honesty and the projection it renders), `:72-236` (retry and revoke handlers and their operator sentences); `src/Pegasus.Core/Operations/RequestOperations.cs:32-70`
- Binding decisions: L-01 — all data arrives through `/api/v1`, never from a direct database or provider call. ADR-0106 — the screen reports on the central Graph worker, it does not drive it. ADR-0107 — the screen shows last-good times and states, never a secret or a raw provider payload. L-04 — routing named on this ticket.
- Depends on: `DSK-07-01` the intake-status and external-work reads; `DSK-07-02` the retry commands; `DSK-06-13` the adopted screen specs; `DSK-06-07` the data-table pattern; `DSK-06-10` the problem-presentation `InfoBar`

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; verification by `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`) at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for WinUI `ListView` virtualization and `InfoBar` semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the Operations screen spec, the cross-cutting state contract and `docs/design/README.md` (the operator-copy authority — a screen that explains instead of stating is a defect). Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-04-operations-screen`.
2. Confirm the contracts published by [[DSK-07-01]] and [[DSK-07-02]] in `src/Pegasus.Contracts` and regenerate the API client with `pwsh ./eng/api/Generate-ApiClient.ps1` (the script established by [[DSK-03-05]]). Expected: `git diff --exit-code` is clean after a second regeneration.
3. Add `OperationsViewModel` to `src/Pegasus.Desktop` (the project scaffolded by [[DSK-02-05]]) using `ObservableObject` and `[RelayCommand]` per `winui-code-review`'s MVVM checklist — no `SolidColorBrush`, `Visibility` or other UI type in the view model.
4. Model the load state explicitly per proposal § 16.1: `not started`, `running`, `succeeded`, `failed`, `cancelled`. Set the "obtained at" timestamp **only** on success, reproducing `Index.cshtml.cs:43-46`; on failure the screen keeps the previous data, labels it as previously obtained, and shows the failure sentence.
5. Build `OperationsPage.xaml` from the screen spec: an external-work table (kind, case, last failure, attempts, next action) with `Operations.External.Table`, a retry command with `Operations.External.Retry`, an upload-links table with `Operations.Links.Revoke`, and one health row per dependency with `Operations.Health.<Dependency>`. Use the data-table pattern from [[DSK-06-07]] rather than a bespoke grid.
6. Bind retry enablement to the gateway's `canRetry` field alone. Never infer eligibility client-side from an attempt count — the server owns that decision, and a client that guesses will produce a refused command the operator cannot explain.
7. Show poison and failure counts as named figures, and show the mailbox freshness state (`current` / `stale` / `unavailable`) with its last successful cycle time. Never collapse `unavailable` into "no failures". State that is meaning-bearing must not be colour-only (`docs/desktop/06-ui-design/keyboard-and-accessibility.md`).
8. Render refusals through the shared problem presentation from [[DSK-06-10]]: one operator sentence plus a copyable Reference carrying the correlation id. Reasoned commands (revoke) use the `ReasonDialog` from [[DSK-06-09]].
9. Handle the disconnected case: when the gateway is unreachable the screen says so, keeps the last obtained values labelled with their time, and offers manual refresh. A blank table that implies "nothing failed" is a defect.
10. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` (scaffolded by [[DSK-08-04]]) with a fake API client: success sets the timestamp; failure preserves prior data and does not; retry is disabled when `canRetry` is false; a refused retry surfaces the problem sentence; cancellation leaves the state `cancelled`.
11. Build and launch with `.\BuildAndRun.ps1` from the `winui-dev-workflow` skill (invoke it in async mode and capture the PID), then write and run a `winapp ui` batch script per the `winui-ui-testing` skill covering: table renders, retry disabled/enabled, reason dialog, keyboard-only traversal of both tables, and a screenshot of the disconnected state.
12. Run the accessibility scan from [[DSK-06-15]] over the screen and attach the report. Then run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, and open the PR into `dev`.

## Acceptance criteria

- [ ] Every value on the screen shows when it was obtained; a failed load never claims freshness.
- [ ] Retry and revoke commands are enabled only when the gateway marks the row eligible.
- [ ] Mailbox freshness (`current` / `stale` / `unavailable`), failure codes and poison counts are all visible and never collapsed into success.
- [ ] The disconnected state is honest: previous values are labelled with their time and manual refresh is offered.
- [ ] Every meaning-bearing state has text as well as colour, and both tables are fully keyboard operable.
- [ ] Every interactive element carries the AutomationId named in the screen spec.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` — expected: freshness, enablement, refusal and cancellation facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid> -Script operations` — expected: every assertion passes; screenshots of the loaded, refused and disconnected states are attached.
- [ ] `AxeWindowsCLI` scan of the Operations screen — expected: no critical issue; report attached to the ticket proof.

## Evidence tier

Tier 7 — Browser/accessibility (read as the desktop equivalent: real authenticated workflow, keyboard, focus and error behaviour, semantic labels, text-plus-colour states).
Tier 7 obliges a real run against the gateway, not a mocked screenshot; an automated scan does not replace the keyboard walk.

## Documentation changes

- `docs/frd/frd-13-desktop-operator-experience.md` — Operations screen section
- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — the Operations row moves to `implemented`

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `tests/Pegasus.Desktop.ViewModelTests`, `tests/Pegasus.Desktop.UITests`. Must not add endpoints (that is [[DSK-07-01]] / [[DSK-07-02]]), must not reference `src/Pegasus.Infrastructure`, and must not host any Pegasus UI in a WebView — the architecture test from [[DSK-02-12]] enforces both.
- **Traps**: poison-queue visibility must survive the friendly redesign; a blank screen after a failed load is a lie; secrets and raw provider payloads never reach the client (ADR-0107) — a health row shows a state and a last-good time, nothing more; operator copy rules apply (`docs/design/README.md`), so a row that explains rather than states is a defect.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
