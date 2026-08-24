---
id: TEST-005
type: ticket
title: >-
  DSK-08-05 · View-model test catalogue: states, commands, cancellation, dirty
  state, validation, navigation, stale session, mandatory update
status: preparing
area: testing
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:34:13.681Z'
labels:
  - desktop-conversion
  - plan-08
  - phase-2
  - tier-2
groups:
  - EPIC-009
  - HZN-003
links: []
docs_todo: true
archived: false
created: '2026-08-24T07:46:12.608Z'
updated: '2026-08-24T21:34:13.681Z'
---

## What

Define and implement the standing catalogue of view-model tests every desktop screen must satisfy — command availability, loading/empty/error/success states, cancellation, dirty state, validation, navigation decisions, stale session and mandatory update — and cover the startup, session and update view models with it.

## Why

Proposal §22.2 lists these nine behaviours as the view-model layer's obligations. Without a written catalogue each slice invents its own subset, and the branches that matter operationally — a save attempted after the session went stale, a command still enabled while a request is in flight, a mandatory update screen that can be dismissed — are exactly the ones nobody writes by hand. Making the catalogue explicit turns "tests exist" into "the same nine questions are answered for every screen". Builds on [[DSK-08-04]]; every area 05 slice ticket consumes it.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-05`
- Plan detail: `docs/desktop/08-testing/README.md` § 4 (target state row "View-model tests")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "View-model tests", § 8.4 session failure handling, § 9.2 startup sequence
- Repository evidence:
  - `tests/Pegasus.Desktop.ViewModelTests/**` — the project, fakes and shared clock from [[DSK-08-04]]
  - `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 rows `DSK-04-08` (session-failure matrix) and `DSK-04-09` (startup orchestrator) — the behaviours this catalogue asserts
  - `docs/desktop/06-ui-design/README.md` § 5 row `DSK-06-10` — problem presentation and the banned-words lint that lives in view-model tests
- Binding decisions:
  - L-02 — no Azure test resource is involved; these run headless on any Windows machine or runner.
- Depends on: `DSK-08-04` — the project, the fakes and the shared clock. `DSK-04-09` — the startup orchestrator whose view models the catalogue covers first.

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (`dotnet/skills` `98f84851`, plugin `dotnet-test`) → `assertion-quality` (same pin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-05` and § 4, then `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 rows `DSK-04-08`, `DSK-04-09`, `DSK-04-11`. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Load `pegasus-desktop`, then `code-testing-agent`. Write `tests/Pegasus.Desktop.ViewModelTests/CATALOGUE.md` listing the nine required questions, one line each, with the expected assertion for each: (a) command availability per state and per `StaffAccessRight`; (b) loading, empty, error and success states are distinct and observable; (c) an in-flight request is cancelled on navigation away and leaves no state change; (d) dirty state blocks navigation and is cleared on save; (e) validation errors are per field and survive a failed save; (f) navigation decisions are made in the view model, not the page; (g) a stale session surfaces the re-authentication path and never silently discards edits; (h) a mandatory update state cannot be dismissed; (i) problem text uses the operator vocabulary and no banned word.
3. Add `Catalogue/CatalogueCoverageTests.cs`: reflect over the view-model types in `src/Pegasus.Desktop`, and fail listing any view model that has no corresponding test class following the naming convention `\<ViewModelName\>CatalogueTests`. Done when adding a new view model turns exactly this test red with the type name in the message.
4. Implement `StartupViewModelCatalogueTests` against the orchestrator of [[DSK-04-09]]: update check available and unavailable, compatibility gate pass and block, the 24-hour fail-closed cached decision (drive it with the shared `TestClock`), missing WebView2 runtime, and session restore success and failure.
5. Implement `LoginViewModelCatalogueTests` covering the session-failure matrix of [[DSK-04-08]]: disabled account, password-change-required, rate limited, disconnected. Assert the state and the operator sentence, and that credentials are not retained after a failure.
6. Implement `MandatoryUpdateViewModelCatalogueTests`: the state is entered from a blocking compatibility response, no command dismisses it, and the retry path re-checks rather than assuming.
7. Implement `ConnectivityViewModelCatalogueTests` for [[DSK-04-11]]: the disconnected indicator, automatic recheck, and saves disabled while offline with **no** silent queueing — assert that a save attempted while offline neither enqueues nor mutates local state.
8. Add the banned-words lint from [[DSK-06-10]] as a shared assertion helper used by every catalogue class, reading its word list from one place; a problem message containing a banned word fails.
9. Load `assertion-quality` and grade the new classes; replace any assertion that would pass on a wrong value (`NotNull` on a computed string, `True` on a compound condition) with a literal comparison.
10. Run `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build --filter "Category=ViewModel"`. Done when green and the coverage test reports no uncovered view model.
11. Add a line to `docs/desktop/08-testing/README.md` § 4 pointing slice tickets at `tests/Pegasus.Desktop.ViewModelTests/CATALOGUE.md`, and run the simplification pass over the branch diff before opening the PR.

## Acceptance criteria

- [ ] `CATALOGUE.md` states the nine questions and the assertion each requires.
- [ ] A view model with no catalogue test class fails the coverage test by name.
- [ ] Startup, login, mandatory-update and connectivity view models are fully covered.
- [ ] Offline saves are proved to neither queue nor mutate.
- [ ] Banned-word lint runs against every operator-visible message the tests produce.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build --filter "Category=ViewModel"` — expected: `Passed!`, non-zero total.
- [ ] Add a throwaway view model to `src/Pegasus.Desktop` and rerun — expected: `CatalogueCoverageTests` fails naming it; remove and confirm green.

## Evidence tier

Tier 2 — Core/domain, desktop-side (view model). It obliges positive, contradictory, ambiguous and failure cases for each screen's state machine, with fakes for the gateway, clock and credential store and no UI thread.

## Documentation changes

- `tests/Pegasus.Desktop.ViewModelTests/CATALOGUE.md` — new; the nine questions and their assertions.
- `docs/desktop/08-testing/README.md` § 4 — point slice tickets at the catalogue.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create and edit `tests/Pegasus.Desktop.ViewModelTests/**` and the two documentation lines. Must not change `src/Pegasus.Desktop`; a missing observable state is a finding for `winui-dev`.
- **Traps**: use the shared `TestClock` from [[DSK-08-04]], never a private `FixedTimeProvider`. Operator copy rules apply to every message asserted here — a message that explains rather than states is a defect under `docs/design/README.md`. `TreatWarningsAsErrors=true` applies.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
