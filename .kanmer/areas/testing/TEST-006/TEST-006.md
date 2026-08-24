---
id: TEST-006
type: ticket
title: >-
  DSK-08-06 · `tests/Pegasus.Desktop.UITests`: `ui-tests.ps1` harness around
  `winapp ui` with the AutomationId contract
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
docs_todo: true
archived: false
created: '2026-08-24T07:46:12.621Z'
updated: '2026-08-24T07:46:12.621Z'
---

## What

Create `tests/Pegasus.Desktop.UITests` with a `ui-tests.ps1` batch harness driving the installed MSIX through `winapp ui` (Windows UI Automation): one pass, pass/fail results as JSON, screenshots per state, and an AutomationId coverage audit that fails when an interactive element has no stable identifier.

## Why

Proposal §22.2 ("WinUI UI automation") asks for a small, high-value suite covering launch/update/login, open case, edit/save, concurrency message, document upload, vehicle lookup, report preview/finalize, logout, keyboard navigation and core accessibility properties. §23.2 makes several of those a release gate. None of it is reachable without a harness that can find elements deterministically, which is what the AutomationId contract buys: the plan's mitigation for UI flakiness is *AutomationId contract, `wait-for` instead of sleeps, two fix-and-rerun cycles maximum*. This ticket is the harness only; the scripts are [[DSK-08-07]] and [[DSK-08-08]], the accessibility lane is [[DSK-08-09]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-06`
- Plan detail: `docs/desktop/08-testing/test-uat-stack.md` § "Tickets to build it" (DSK-08-06/07/08/09 depend on the stack) and § "Evidence capture" (`winapp ui screenshot` per state, `winapp ui record` for the critical path, JSON results from `ui-tests.ps1`)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "WinUI UI automation", § 23.2 native verification
- Repository evidence:
  - `.codex/skills/winui-ui-testing/SKILL.md` — the `winapp ui` verbs (`wait-for`, `invoke`, `get-value`, `set-value`, `send-keys`, `screenshot`, `record`), the `ui-tests.ps1` template with `Test-UI`, and the two gotchas: never name the parameter `$Pid`, and select the main window by excluding `PopupHost`
  - `docs/desktop/08-testing/README.md` § 2 — `winapp` CLI ≥ 0.3 via `winget install Microsoft.WinAppCLI`
  - `docs/desktop/08-testing/test-uat-stack.md` § "Machine prerequisites" — dedicated Windows 11 workstation, dev certificate trusted in `Cert:\LocalMachine\TrustedPeople`
  - `.gitignore` — `artifacts/` is ignored; UI evidence is filed there and in the Kanmer ticket, never in the tree
- Binding decisions:
  - L-02 — the suite runs against the local Test/UAT stack; there is no Azure test environment to point it at.
  - D-002 — the package under test is signed with a certificate trusted in `LocalMachine\TrustedPeople`, the same store production uses.
- Depends on: `DSK-08-17` — the Test/UAT stack the app under test talks to. `DSK-02-14` — the dev-certificate MSIX build that produces the package. `DSK-06-15` — the AutomationId convention and coverage audit this harness executes.

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`, same pin) for build-and-launch
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-06`, § 7 and `docs/desktop/08-testing/test-uat-stack.md` in full, then `docs/desktop/06-ui-design/keyboard-and-accessibility.md`. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. **Operator step**: on the dedicated Windows 11 Test/UAT workstation (never a developer machine holding a pilot install), install the CLI with `winget install Microsoft.WinAppCLI` and confirm `winapp ui status` responds; trust the development certificate into `Cert:\LocalMachine\TrustedPeople` per [[DSK-09-06]]. Evidence to hand back: the `winapp --version` output and `Get-ChildItem Cert:\LocalMachine\TrustedPeople` showing the subject.
3. Load `pegasus-desktop`, then `winui-ui-testing`, and follow its "Write the Test Script" section. Create `tests/Pegasus.Desktop.UITests/ui-tests.ps1` from the skill's template: `param([Parameter(Mandatory)][int]$AppPid)` — do **not** name it `$Pid`, it is read-only in PowerShell — the `Test-UI` helper that counts pass/fail and records a `$results` array, and the main-window HWND lookup that filters out the window titled `PopupHost`.
4. Extend the template with: a `-ResultsPath` parameter defaulting to `artifacts/ui-tests/results.json`, a `-ScreenshotPath` defaulting to `artifacts/ui-tests/screenshots`, a non-zero exit code when any test fails, and a JSON result object per test carrying name, status, duration and the failure detail.
5. Add `tests/Pegasus.Desktop.UITests/Invoke-UiSuite.ps1`: resolve the installed package with `Get-AppxPackage CollisionEngineers.Pegasus`, launch it through its application user model id, capture the PID, wait for the shell root element with `winapp ui wait-for`, then invoke `ui-tests.ps1` with that PID. Never relaunch a running app, and never launch the packaged exe directly (`winui-dev-workflow` rule).
6. Add `tests/Pegasus.Desktop.UITests/AutomationIdAudit.ps1`: walk the UI tree with `winapp ui inspect -a <PID>` and fail listing any element with an interactive control type (Button, ListItem, Edit, ComboBox, CheckBox, Tab) that has no AutomationId, or whose AutomationId is not in the convention agreed by [[DSK-06-15]]. Done when removing an AutomationId in XAML makes this audit fail with the control's name.
7. Add a smoke batch of no more than eight assertions in `ui-tests.ps1` — shell root present, rail items present, status bar present, title bar search slot present, keyboard focus reaches the rail — using `wait-for` with an explicit timeout. **No `Start-Sleep` anywhere**: a sleep is the flakiness the plan names.
8. Capture a screenshot per asserted state with `winapp ui screenshot` into the screenshot path, and add an optional `-Record` switch that wraps the run in `winapp ui record` for the critical path.
9. Write `tests/Pegasus.Desktop.UITests/README.md`: prerequisites, how to bring up the stack (`Invoke-LocalDevelopment.ps1 -Action Start -Mode TestStack`, [[DSK-08-17]]), how to run the suite, where results land, and the two-fix-and-rerun-cycles rule from the skill.
10. Run the suite twice in a row against the same installed package on the workstation. Done when both runs are green and produce identical pass counts — a differing count means a timing dependency that must be fixed before the ticket closes.
11. File `artifacts/ui-tests/results.json` and the screenshots as the ticket's proof (Kanmer `proof`/`reference`), never in the repository tree — `artifacts/` is ignored and `AGENTS.md` § New Markdown placement forbids evidence in the tree.
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] `ui-tests.ps1` launches against an already-running installed package by PID, runs the batch in one pass, writes `results.json` and screenshots, and exits non-zero on any failure.
- [ ] The AutomationId coverage audit fails, by control name, when an interactive element has no conforming AutomationId.
- [ ] No `Start-Sleep` and no fixed delay anywhere in the harness; every wait is `winapp ui wait-for` with a timeout.
- [ ] Two consecutive runs produce identical results.
- [ ] Evidence is written under `artifacts/` and filed in the ticket, not committed.

## Verification

- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/Invoke-UiSuite.ps1` on the Test/UAT workstation — expected: exit 0, `artifacts/ui-tests/results.json` with every test `PASS`, one screenshot per asserted state.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/AutomationIdAudit.ps1 -AppPid <pid>` — expected: exit 0 and a printed count of audited elements.
- [ ] Remove one AutomationId in XAML, rebuild, reinstall, rerun the audit — expected: exit 1 naming the control; restore and confirm exit 0.

## Evidence tier

Tier 7 — Browser/accessibility, in its desktop reading (`winapp ui` + `AxeWindowsCLI` + manual reviews). It obliges authenticated workflows driven through the real UI with keyboard, focus and semantic properties observed; automated results do not replace the manual reviews owned by [[DSK-08-09]].

## Documentation changes

- `tests/Pegasus.Desktop.UITests/README.md` — new; prerequisites, run instructions, evidence locations, the rerun-cycle rule.
- `docs/runbook.md` § Locked restore, build, and test — add the UI suite command with its Windows-and-workstation-only note.
- `docs/operations.md` § Evidence profiles — register the `DesktopUI` trait.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create `tests/Pegasus.Desktop.UITests/**` and the documentation lines above. Must not change `src/Pegasus.Desktop` — a missing AutomationId is a finding for `winui-dev` under [[DSK-06-15]].
- **Traps**: UI tests mutate the installed package (`Add-AppxPackage`, `Remove-AppxPackage`) — run them only on a dedicated runner or workstation, never on a machine holding a pilot install. UI automation is flaky on hosted runners; the hosted-versus-self-hosted decision belongs to [[DSK-08-12]] and this harness must not assume either. `winapp ui` is the only driver: no WinAppDriver, Appium or FlaUI, and no driver dependency in the application. Never fabricate domain data in fixtures.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
