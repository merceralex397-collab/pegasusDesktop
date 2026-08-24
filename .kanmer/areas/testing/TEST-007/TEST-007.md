---
id: TEST-007
type: ticket
title: >-
  DSK-08-07 · UI critical-path scripts: launch/update/login, open case,
  edit/save, concurrency message, logout, keyboard navigation
status: preparing
area: testing
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:34:14.090Z'
labels:
  - desktop-conversion
  - plan-08
  - phase-4
  - tier-7
groups:
  - EPIC-009
  - HZN-005
links: []
docs_todo: true
archived: false
created: '2026-08-24T07:48:40.534Z'
updated: '2026-08-24T21:34:14.090Z'
---

## What

Write the six critical-path `winapp ui` scripts on the harness from [[DSK-08-06]]: launch with an update check, login, open a case, edit and save, the two-user concurrency message, logout, and a keyboard-only navigation pass — each asserting through `wait-for` and `get-value`, with the conflict driven by a second writer against the same gateway.

## Why

Proposal §22.2 names exactly these as the high-value UI suite, and §24 makes two of them phase exit gates: Phase 3 requires the accessibility and keyboard baseline, Phase 4 requires a passing two-user conflict test with no silent overwrite. A conflict path asserted only at the API level does not prove the operator sees the conflict; that is what these scripts add. Runs on the local Test/UAT stack ([[DSK-08-17]]); the document, vehicle and report paths are [[DSK-08-08]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-07`
- Plan detail: `docs/desktop/08-testing/test-uat-stack.md` § "UAT scripts — end-to-end scenarios 1–14" rows 1, 3, 5, 8, 11 (scenario 11 is "two desktops or one desktop + API client")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "WinUI UI automation", § 24 Phase 3 and Phase 4 exit gates
- Repository evidence:
  - `tests/Pegasus.Desktop.UITests/ui-tests.ps1` — the harness, `Test-UI` helper and results contract from [[DSK-08-06]]
  - `.codex/skills/winui-ui-testing/SKILL.md` — `wait-for`, `invoke`, `set-value`, `get-value`, `send-keys` verbs; the x:Bind `LostFocus` commit gotcha for text edits
  - `docs/desktop/06-ui-design/keyboard-and-accessibility.md` — the keyboard map the navigation script walks
  - `docs/desktop/08-testing/test-uat-stack.md` § Lifecycle — `Smoke -Mode TestStack` logs in through `/connect/token` with a seeded staff account
- Binding decisions:
  - L-02 — the scripts run against the local stack only; the same scripts are repeated on the pilot ring for scenarios that need real Azure, which is [[DSK-08-16]]'s mapping, not this ticket's.
- Depends on: `DSK-08-06` — the harness. `DSK-05-01`, `DSK-05-02`, `DSK-05-03`, `DSK-05-04`, `DSK-05-05` — slices S1–S5 supply the dashboard, case list, case detail, case create and case edit screens the scripts drive.

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-07`, `docs/desktop/08-testing/test-uat-stack.md` § "UAT scripts", and `docs/desktop/06-ui-design/keyboard-and-accessibility.md`. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Bring up the stack: `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start -Mode TestStack`, then `-Action Status -Mode TestStack` and confirm the gateway, Worker, Azurite, database and feed are all reported healthy before writing a single assertion.
3. Load `pegasus-desktop`, then `winui-ui-testing`. Add `tests/Pegasus.Desktop.UITests/scripts/01-launch-update-login.ps1`: assert the update check ran (status-bar version element matches the installed package version from `Get-AppxPackage`), the login screen appears, credentials for the seeded staff account are entered with `set-value`, sign-in is invoked, and the shell rail appears within an explicit `wait-for` timeout.
4. Add `scripts/02-open-case.ps1`: from the case list, `wait-for` the list to be populated, select the seeded case by its AutomationId, invoke open, and assert the case reference shown equals the seeded reference with `winapp ui get-value`.
5. Add `scripts/03-edit-save.ps1`: change one field with `set-value`, move focus off the field before invoking save (x:Bind commits on `LostFocus` — the skill's WinUI gotcha; a save invoked while the editor still has focus silently saves the old value), invoke save, then assert the saved value and the incremented version indicator.
6. Add `scripts/04-concurrency-message.ps1`: with the case open and edited but unsaved in the desktop, have a second writer change the same case through the gateway (a `curl`/`Invoke-RestMethod` call to the `/api/v1` command with a staff token from `/connect/token`), then invoke save in the desktop and assert the conflict message element appears, that it names the reload/compare path, and that the desktop's unsaved value is still present — nothing was silently overwritten.
7. Add `scripts/05-logout.ps1`: invoke logout, assert the login screen returns, and assert that reopening the app does not restore the session.
8. Add `scripts/06-keyboard-navigation.ps1`: drive the keyboard map of [[DSK-06-14]] with `send-keys` — rail access keys, Ctrl+K search, Ctrl+N new, Ctrl+S save, F5 refresh, Esc dismiss — asserting the focused element after each with `winapp ui get-focused`. No pointer input in this script.
9. Register all six scripts in `ui-tests.ps1` as one batch so a full run is a single pass, and keep each script independently runnable by PID for debugging.
10. Replace any wait you were tempted to write as a delay with `winapp ui wait-for <AutomationId> -t <ms>`. Run the batch twice; a differing pass count between runs is a defect to fix now, and the skill's limit of two fix-and-rerun cycles applies before the flake is escalated as a finding.
11. Capture a screenshot per asserted state and a `winapp ui record` video of the conflict script; file `artifacts/ui-tests/results.json`, the screenshots and the recording in the ticket proof, not in the repository tree.
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] Six scripts exist and run as one batch, each asserting through `wait-for`/`get-value`/`get-focused`.
- [ ] No sleeps or fixed delays anywhere.
- [ ] The two-user conflict is driven by a real second writer against the gateway, and the desktop's unsaved edit survives the conflict.
- [ ] The keyboard script uses no pointer input and asserts the focused element after every key.
- [ ] Two consecutive full runs produce identical results.

## Verification

- [ ] `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Status -Mode TestStack` — expected: every component healthy before the run.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/Invoke-UiSuite.ps1` — expected: exit 0, `results.json` with all six scripts `PASS`.
- [ ] Rerun immediately — expected: identical pass count and identical test names.

## Evidence tier

Tier 7 — Browser/accessibility, desktop reading. It obliges authenticated workflows through the real UI, two-session editing, keyboard operation and focus behaviour observed; the automated result does not replace the manual reviews of [[DSK-08-09]].

## Documentation changes

- `tests/Pegasus.Desktop.UITests/README.md` — list the six scripts and what each proves.
- `docs/desktop/08-testing/README.md` § 4 — mark the UI automation row's critical path as scripted.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create and edit `tests/Pegasus.Desktop.UITests/**`. Must not change `src/Pegasus.Desktop` or gateway code — a missing AutomationId or an unobservable state is a finding for `winui-dev`.
- **Traps**: UI automation flakiness — AutomationId contract, `wait-for` never sleeps, two fix-and-rerun cycles maximum. UI tests mutate the installed package; run only on the dedicated workstation or runner. Operator copy rules apply to the conflict message asserted here. Never fabricate domain data — the seeded case comes from the stack's generic fixtures, never `corpus/`.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
