---
id: TEST-012
type: ticket
title: >-
  DSK-08-12 · CI spike: can `windows-latest` install a dev-signed MSIX, run
  `winapp ui` and run `AxeWindowsCLI`?
status: backlog
area: testing
assignee: ''
profile: spike
labels:
  - desktop-conversion
  - plan-08
  - phase-3
  - tier-1
  - needs-operator
groups:
  - EPIC-009
  - HZN-004
links: []
blocks:
  - TEST-013
docs_todo: true
archived: false
created: '2026-08-24T07:51:10.314Z'
updated: '2026-08-24T08:51:14.500Z'
---

## What

A timeboxed spike that answers one question with a run log: on a GitHub-hosted `windows-latest` runner, can the workflow trust a development certificate, install the MSIX with `Add-AppxPackage`, launch the app, drive it with `winapp ui`, and scan it with `AxeWindowsCLI`? The output is a written answer and a recorded decision — hosted runner or self-hosted — not production code.

## Why

The testing plan records two assumptions it cannot verify by reading: that hosted `windows-latest` runners provide an interactive desktop session sufficient for UI Automation against an installed MSIX, and that `Add-AppxPackage` works there with a certificate in `LocalMachine\TrustedPeople` without Developer Mode. Everything in [[DSK-08-13]] depends on the answer, and so does the cost analysis in [[DSK-08-19]] — a self-hosted runner changes both the workflow and the bill. Guessing here means building a lane that either flakes permanently or cannot run at all.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-12`
- Plan detail: `docs/desktop/08-testing/README.md` § 2 Assumptions (the two to verify) and § 7 (UI automation flakiness on hosted runners; CI checkout timeouts on the 700 MB repository)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 21.2 stages 8–11 (build the MSIX, install on a clean Windows 11 test image, run desktop smoke/UI automation, run packaging/update tests)
- Repository evidence:
  - `.github/workflows/ci.yml` — the single `repository-check` workflow, its `windows-latest` jobs and their `timeout-minutes`; the composite action `.github/actions/dotnet-build/action.yml` (`actions/setup-dotnet@v6` 10.0.x, locked restore, Release build)
  - `.codex/skills/winui-packaging/SKILL.md` — the CI sample: `winapp cert generate --if-exists skip --quiet` then `winapp package ./bin/x64/Release/ --cert ./devcert.pfx --quiet`
  - `.codex/skills/winui-ui-testing/SKILL.md` — the `ui-tests.ps1` batch pattern the spike executes
  - `tests/Pegasus.Desktop.UITests/**` — the harness from [[DSK-08-06]] the spike runs unchanged
- Binding decisions:
  - C-01 — the repositories go private on completion and private Windows runner minutes bill at 2×, so the answer feeds the runner decision in [[DSK-08-19]].
  - D-002/D-003 — if the answer is self-hosted, the natural host is the always-on machine that serves the UNC feed and custodies the signing certificate; the spike must say whether that is required, not assume it.
- Depends on: `DSK-08-06` — the UI harness and the AutomationId audit the spike runs.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `authoring-github-workflows` (`dotnet/skills`, `.agents/skills/authoring-github-workflows/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`)
- **MCP**: Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`) for sideloading and `Add-AppxPackage` requirements; Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `spike`: `kanmer-research` → (no implementation gates) → `kanmer-verify` → `kanmer-closeout`. `enter-done` requires the `research` document and `questions-resolved`; call `get_doc_gates <id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 2 Assumptions, § 5 row `DSK-08-12` and § 7. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch. Timebox the spike to one working session and say so in the research document.
2. Load `pegasus-desktop`, then `authoring-github-workflows`. Before running anything, use `microsoft_docs_search` for the current requirements of `Add-AppxPackage` for a sideloaded signed MSIX on Windows 11 (is Developer Mode required, is sideloading on by default, which certificate store is consulted) and record the answers with their URLs.
3. Create a throwaway workflow `.github/workflows/spike-desktop-ui.yml` triggered by `workflow_dispatch` only — it must not run on `pull_request` or `push`, so it cannot slow or block the existing `repository-check`.
4. In that workflow on `runs-on: windows-latest`: check out with `fetch-depth: 1` (the repository is about 700 MB and `actions/checkout` has been observed dying at around five minutes — upstream DELIV-010), use the existing `./.github/actions/dotnet-build` composite action, then build the desktop project and package it with `winapp cert generate --if-exists skip --quiet` and `winapp package ./bin/x64/Release/ --cert ./devcert.pfx --quiet` per the packaging skill's CI sample.
5. Add a step that trusts the certificate into `Cert:\LocalMachine\TrustedPeople` and installs the package with `Add-AppxPackage`. Record whether it succeeds, and if it fails record the exact HRESULT and message verbatim — the failure text is the finding.
6. Add a step that launches the installed package, captures the PID and runs `tests/Pegasus.Desktop.UITests/ui-tests.ps1` unchanged. Record the pass/fail counts and the total wall-clock time.
7. Add a step that installs and runs `AxeWindowsCLI` against the same PID. Record whether it produces results and how long it takes.
8. Run the workflow three times from `workflow_dispatch` and record each run's URL, duration per step, and result. Three runs, because the question is not only "does it work once" but "is it stable enough to gate a PR".
9. Record the measured Windows minutes for the spike workflow, per run, so [[DSK-08-19]] can price the real lanes from a measurement rather than an estimate.
10. Write the answer in the ticket research document: yes/no per capability (install, launch, UI Automation, axe), the evidence for each, the observed flakiness, the measured minutes, and the recommended runner strategy — hosted, hosted-with-retries, or self-hosted. If self-hosted, state which host, what isolation and permissions it needs, and that it is the same machine as the D-003 feed host and the D-002 certificate custodian.
11. **Operator step**: if the recommendation is a self-hosted runner, the operator must confirm the host and register the runner; the spike does not provision anything. Evidence to hand back: confirmation of the host and its intended isolation, or a decision to stay hosted.
12. Delete the throwaway workflow in the same PR, or keep it as `workflow_dispatch`-only and say why in the research document. Answer the ticket's open questions and record the decision so [[DSK-08-13]] can be planned against it. No simplification pass is needed if the branch carries no production change; record `n/a — spike` under the dated `## Simplification pass` heading.

## Acceptance criteria

- [ ] A written answer exists for each of the four capabilities with the run evidence behind it.
- [ ] Three workflow runs are recorded with their URLs, durations and results.
- [ ] The measured Windows minutes per run are recorded.
- [ ] A runner decision is recorded (hosted or self-hosted), with the host and isolation named if self-hosted.
- [ ] The throwaway workflow does not run on `pull_request` or `push`.

## Verification

- [ ] Three `workflow_dispatch` run URLs in the ticket research document — expected: each with per-step timings and the install/launch/UI/axe outcome.
- [ ] `gh run list --workflow spike-desktop-ui.yml` — expected: exactly the spike runs, none triggered by a pull request.
- [ ] The `repository-check` workflow's own duration is unchanged — expected: no new required check appears on open PRs.

## Evidence tier

Tier 1 — Static/build/architecture. It obliges a compiled, packaged and installed artefact plus recorded run logs; it proves consistency of the toolchain on the runner, and nothing about the application's behaviour.

## Documentation changes

- The answer and decision live in the ticket's `research` document (Kanmer), not in the repository — `AGENTS.md` § New Markdown placement.
- `docs/desktop/08-testing/README.md` § 2 — replace the two assumptions with the verified answer once decided.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may add and remove one `workflow_dispatch` workflow file. Must not change the existing `repository-check` jobs — that is [[DSK-08-13]] — and must not register or configure a runner; that is an operator action.
- **Traps**: CI checkout timeouts on the 700 MB repository — use a shallow checkout where the history guard does not need depth. UI automation flakiness is the point of the measurement, not a reason to retry until green: record the flake rate. A GitHub secret must never hold a signing key — the spike uses a generated development certificate created inside the run.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — spike` if the branch carries no production change; otherwise required over the branch diff and recorded under a dated `## Simplification pass` heading.

## Outcome

_Filled at closeout._
