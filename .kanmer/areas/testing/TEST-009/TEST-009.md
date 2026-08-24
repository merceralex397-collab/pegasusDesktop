---
id: TEST-009
type: ticket
title: >-
  DSK-08-09 · Accessibility lane: `AxeWindowsCLI` scan script plus the ten
  recorded reviews checklist
status: backlog
area: testing
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-08
  - phase-3
  - tier-7
  - needs-operator
groups:
  - EPIC-009
  - HZN-004
links: []
refs:
  - docs/frd/frd-12-operator-experience.md
docs_todo: true
archived: false
created: '2026-08-24T07:48:40.569Z'
updated: '2026-08-24T07:48:40.569Z'
---

## What

Add the desktop accessibility lane: a script that runs `AxeWindowsCLI` over every screen of the running app and files the results, plus the evidence template for the ten manual reviews `docs/design/README.md` requires of every surface with a real caller.

## Why

Proposal §22.2 ("Accessibility testing") requires an automated scan, keyboard-only walkthrough, Narrator smoke test, high contrast, 200% scaling, focus order, text alternatives and status/error announcements. The repository's own rule is stricter and older: `docs/design/README.md` lists ten reviews that must be recorded when a planned surface has a real caller, and `docs/engineering.md` tier 7 says in terms that automated axe results do not replace manual keyboard or assistive-technology review. Today that evidence exists only for the web app through `Deque.AxeCore.Playwright`; the desktop has no equivalent. Without this lane, Phase 3's "accessibility and keyboard baseline passes" gate has nothing to point at.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-09`
- Plan detail: `docs/desktop/08-testing/README.md` § 2 (`AxeWindowsCLI` from <https://github.com/microsoft/axe-windows>, CLI README under `src/CLI/README.MD`, fetched 2026-08-23) and § 7 ("Automated axe is not acceptance")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "Accessibility testing", § 14.9 keyboard and accessibility
- Repository evidence:
  - `docs/design/README.md:795-805` — the ten reviews, verbatim: keyboard-only traversal; screen-reader and semantic inspection; focus and error behavior; 1280px-and-wider desktop review; 1024–1279px constrained-desktop review; 200% zoom review; forced-colours review; reduced-motion review; contrast review; automated accessibility scanning through the real caller
  - `docs/engineering.md` § Required evidence tiers, tier 7 — automated axe does not replace manual review
  - `tests/Pegasus.IntegrationTests/Browser/AccessibilityTests.cs` — the existing web-side pattern (`Deque.AxeCore.Playwright` 4.12.0) this lane parallels but does not reuse
  - `docs/desktop/06-ui-design/keyboard-and-accessibility.md` — the content owner of what is reviewed
- Binding decisions:
  - L-02 — the scan runs against the locally installed package on the Test/UAT workstation; no Azure resource is involved.
- Depends on: `DSK-08-06` — the UI harness that launches the installed package and knows the screen inventory. Coordinates with `DSK-06-15` (AutomationId audit and axe wiring) and `DSK-06-16` (the ten reviews per release candidate) — those own the content, this owns the lane that executes it.

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`) — its AutomationId audit and accessibility section. `AxeWindowsCLI` is a tool, not a skill.
- **MCP**: Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`) for UI Automation property semantics; Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-09` and § 7, `docs/desktop/06-ui-design/keyboard-and-accessibility.md`, and `docs/design/README.md:770-810` (the ten reviews). Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. **Operator step**: install `AxeWindowsCLI` on the Test/UAT workstation from <https://github.com/microsoft/axe-windows> following its `src/CLI/README.MD`, and confirm it runs against any window. Evidence to hand back: the CLI version and one sample output file. Record the exact installed version in the ticket — the lane must pin it.
3. Load `pegasus-desktop`, then `winui-ui-testing`. Add `tests/Pegasus.Desktop.UITests/Invoke-AccessibilityScan.ps1` taking `-AppPid`, `-ScreenList` and `-OutputPath` (default `artifacts/accessibility/`). For each screen in the list it navigates there through the same `winapp ui` verbs the UI suite uses, waits for the screen's root element, then invokes `AxeWindowsCLI` with the process id and an output directory per screen.
4. Define the screen list in one file, `tests/Pegasus.Desktop.UITests/screens.json`, keyed by the route names from the shell of [[DSK-02-08]], so a new screen is added in one place and the scan, the AutomationId audit and the review checklist all pick it up.
5. Make the script fail on any axe error-severity result, and summarise per screen: screen name, error count, warning count, output file path. Done when introducing a control with no accessible name makes the lane fail naming that screen.
6. Add `tests/Pegasus.Desktop.UITests/accessibility-review-template.md` with the ten reviews from `docs/design/README.md:795-805` as ten headed sections, each with: what was done, the tool or assistive technology used, the result, and the evidence file. The tenth section is the automated scan and links the axe output. State at the top that a completed template is required per release candidate and that automated results alone are not acceptance.
7. Add a short procedure section to the template for the two reviews that need named tools: Narrator for the screen-reader pass, and Windows forced-colours mode for the forced-colours pass. Use `microsoft_docs_search` for the current way to enable forced colours and for the UI Automation properties Narrator announces, and cite what you find.
8. Wire the scan into the UI suite as an optional stage: `Invoke-UiSuite.ps1 -IncludeAccessibility` runs the batch then the scan, so one command produces both artefacts.
9. **Operator step**: complete one full review pass against the current build using the template — all ten sections — and hand back the filled template plus the axe output directory. This is the lane's first proof and the pattern every release candidate repeats.
10. Run the scan twice and confirm the error counts are identical; an unstable count means the scan is racing a screen that has not finished loading, which is fixed with `wait-for`, never a delay.
11. File the axe outputs and the completed template as ticket proof (Kanmer `proof`/`reference`); `artifacts/` is ignored and no evidence is committed.
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] The scan runs per screen from a single screen list and writes one output directory per screen.
- [ ] The lane fails on any axe error-severity result, naming the screen.
- [ ] The review template carries all ten reviews from `docs/design/README.md` verbatim, with an evidence slot each.
- [ ] One completed review pass is filed against the current build.
- [ ] The template states that automated results do not replace manual review.

## Verification

- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/Invoke-AccessibilityScan.ps1 -AppPid <pid>` — expected: exit 0, one output directory per screen under `artifacts/accessibility/`, a printed per-screen summary.
- [ ] Remove an accessible name from one control, rebuild, reinstall, rerun — expected: exit 1 naming that screen; restore and confirm exit 0.
- [ ] The completed `accessibility-review-template.md` is attached to the ticket with all ten sections filled.

## Evidence tier

Tier 7 — Browser/accessibility. It obliges keyboard, focus and error behaviour, semantic labels, forced colours, reduced motion, 200% zoom and contrast to be reviewed by a person and recorded; the automated scan is one of the ten items, never the whole of it.

## Documentation changes

- `tests/Pegasus.Desktop.UITests/accessibility-review-template.md` — new; the ten recorded reviews.
- `docs/desktop/08-testing/README.md` § 4 — mark the accessibility row as having a lane.
- `docs/operations.md` § Evidence profiles — note the desktop accessibility evidence alongside the existing `Browser` profile.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create and edit `tests/Pegasus.Desktop.UITests/**` and the documentation lines above. Must not change desktop production code — accessibility defects are findings for `winui-dev` under area 06, and must not weaken or reword the ten reviews in `docs/design/README.md`, which is binding design authority.
- **Traps**: automated axe is not acceptance (tier 7) — the ten manual reviews are still required per release candidate. The existing Playwright/axe-core browser lane stays until web retirement; do not repurpose or remove it. Pin the `AxeWindowsCLI` version; an unpinned tool changes the result set silently.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
