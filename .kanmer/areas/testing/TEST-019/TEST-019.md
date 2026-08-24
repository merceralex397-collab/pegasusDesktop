---
id: TEST-019
type: ticket
title: >-
  DSK-08-19 · CI cost and runner plan for the private-repository era (C-01):
  measure, price the desktop lanes, decide, and record it
status: preparing
area: testing
assignee: ''
profile: spike
stageEntered:
  preparing: '2026-08-24T21:34:16.644Z'
labels:
  - desktop-conversion
  - plan-08
  - phase-8
  - tier-1
  - needs-operator
groups:
  - EPIC-009
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T07:57:18.362Z'
updated: '2026-08-24T21:34:16.644Z'
---

## What

A costed recommendation, in writing: measure the Windows-runner minutes this repository consumes per PR and per month today, price the four desktop lanes on top, choose between a self-hosted Windows runner, a paid plan and lane trimming, and record the decision and its migration steps in `docs/engineering.md`.

## Why

Constraint C-01 (2026-08-23): the repositories become **private** once the conversion completes — they are public today only because GitHub gives free Actions minutes to public repositories. On a private repository the minutes stop being free, Windows runners bill at a **2× multiplier** against the plan's monthly included allowance, and this repository already runs most of `ci.yml` on `windows-latest` with the desktop build, MSIX packaging, `winapp ui` and packaging lanes still to be added on top. The plan is explicit: decide before the repositories flip, not after. A surprise bill after the flip is the failure mode; a lane the team quietly disables to save money is the worse one.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-19`
- Plan detail: `docs/desktop/08-testing/README.md` § 7, first bullet — the 2× Windows multiplier, the instruction to *verify the current allowance and per-minute rates for the account's plan at decision time*, and the mitigations in order of fit: a **self-hosted Windows runner** on the same always-on host that serves the D-003 UNC share (self-hosted minutes are not billed, the machine is already required, and it is the natural custodian of the signing certificate — D-002 chose exactly that, so the host custodies the `.pfx`); a paid plan; or trimming Windows lanes, for example running contract and view-model tests on the cheapest lane that can host them
- Index: `docs/desktop/README.md` § "Constraints recorded after planning began" — C-01 and its two consequences (a) GitHub Releases and Pages ruled out permanently, (b) Actions minutes stop being free
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 21.2 ("Use the repository's current CI provider unless it cannot build/sign WinUI packages. A new CI platform is not justified merely by the desktop conversion.")
- Repository evidence — the Windows-minute ceiling per run, from the `timeout-minutes` in `.github/workflows/ci.yml`:
  - `documentation` 10, `local-development-scripts` 5, `reference-data` 5, `infrastructure` 10 (conditional), `unit` 20, `sql-integration` 20 × 3 shards = 60, `browser` 25 — **135 Windows minutes ceiling per run**, which bills as **270 minutes** against a private allowance at the 2× multiplier
  - `changes` 5 and `sql-integration-coverage` 5 are the only `ubuntu-latest` jobs — 10 minutes at 1×
  - measured, from the comment on the `sql-integration` job: the whole integration lane run in parallel on one runner is 11m55s of tests, against about 4 minutes per shard — so the ceiling is not the actual, and the actual must be measured
  - four more Windows lanes are added by [[DSK-08-13]]: `desktop-build`, `desktop-package`, `desktop-ui-smoke`, `packaging-tests`
  - `.github/workflows/ci.yml` triggers on every `pull_request` and every push to `main`
- Binding decisions:
  - C-01 — the repositories go private; this ticket exists because of it.
  - D-002 and D-003 — an always-on in-house Windows host already exists to serve the UNC feed and custody the signing certificate; a self-hosted runner on that host adds no new machine, but it does put CI and the signing key on one box, which the recommendation must address rather than skip.
- Depends on: `DSK-08-13` — the four desktop lanes whose measured durations are the input. `DSK-08-14` — the dependency-scan and SBOM steps, the last additions before the total is priced.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `authoring-github-workflows` (`dotnet/skills`, `.agents/skills/authoring-github-workflows/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`). No Azure MCP tool is used: this is a GitHub billing question, not an Azure one.
- **Kanmer pipeline** for profile `spike`: `kanmer-research` → (no implementation gates) → `kanmer-verify` → `kanmer-closeout`. `enter-done` requires the `research` document and `questions-resolved`; call `get_doc_gates <id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 7 first bullet in full, `docs/desktop/README.md` § "Constraints recorded after planning began", and `.github/workflows/ci.yml` end to end. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch. Timebox the spike and say so in the research document.
2. **Operator step**: export the GitHub Actions usage report for the last 30 days from the account's billing settings — only the account owner can read it. Hand back the CSV or the per-runner-type totals. Also confirm the account's plan (Free, Team or Enterprise), because the included monthly minutes differ by plan.
3. Measure the actual, not the ceiling: from the last 30 days of runs, compute median and 95th-percentile wall-clock minutes per job, split by runner type, and the number of runs per month. Use `gh run list --limit 200 --json databaseId,conclusion,createdAt,updatedAt` and `gh api` for per-job timings rather than reading the web UI by hand.
4. Record the arithmetic that is already knowable and compare it with the measurement: the `timeout-minutes` ceiling is 135 Windows minutes per run (10 + 5 + 5 + 10 + 20 + 60 + 25), which bills as 270 at the 2× multiplier; the Linux jobs add 10 minutes at 1×. State the measured figure beside the ceiling so the gap is explicit.
5. Take the measured durations of the four new lanes from [[DSK-08-13]]'s post-implementation report and the dependency and SBOM steps from [[DSK-08-14]], and produce the projected per-run and per-month Windows minutes after the desktop lanes land.
6. Verify — do not assume — the current multipliers and the included monthly minutes for the account's plan from GitHub's own billing documentation at the time of the decision, and cite the page and the date. The plan says in terms to verify these at decision time; a 2026 recollection is not evidence.
7. Price the three options over twelve months: (a) **self-hosted Windows runner** on the D-003 share host — self-hosted minutes are not billed, the machine already exists, and it already custodies the signing certificate; (b) a **paid plan** or purchased minutes at the verified rate; (c) **lane trimming** — name exactly which lanes move or shrink, for instance running contract and view-model tests on the cheapest lane that can host them, and state what evidence is lost. Note that LocalDB is Windows-only, so the SQL integration shards cannot move to Linux.
8. For option (a), specify the runner concretely: the host, the runner labels, whether it is repository- or organisation-scoped, the service account and its permissions, workspace isolation between jobs, how the signing certificate is kept out of reach of PR-triggered workflows (a fork PR must never run on a host holding a private key), the update and monitoring responsibility, and what happens when the host is down.
9. Write the recommendation: the chosen option, the measured numbers behind it, the migration steps in order, the point in the programme at which it must be done (before the repositories flip, per C-01), and the rollback if the chosen option fails.
10. **Operator step**: the operator confirms the chosen option and, for a self-hosted runner, the host and its isolation. The spike registers nothing and changes no repository setting.
11. Record the decision in `docs/engineering.md` § Branches and delivery — the runner strategy, the constraint it answers (C-01), the date, and the measured basis. If the decision is durable and architectural, raise it as an ADR in the reserved conversion block rather than inventing a new number.
12. Answer the ticket's open questions, file the usage export and the costing table as ticket evidence (never in the repository tree), and record `n/a — spike` under a dated `## Simplification pass` heading if the branch carries no production change.

## Acceptance criteria

- [ ] Measured Windows and Linux minutes per PR run and per month, from the last 30 days, are recorded beside the 135-minute ceiling arithmetic.
- [ ] The four desktop lanes and the dependency/SBOM steps are priced from their measured durations.
- [ ] The current multipliers and included minutes are verified from GitHub's documentation with the page and date cited.
- [ ] All three options are priced over twelve months, with what each costs and what each loses.
- [ ] A decision is recorded with migration steps and the deadline (before the repositories flip).
- [ ] If self-hosted, the host, labels, service account, isolation and signing-key separation are specified.

## Verification

- [ ] The Actions usage report for the last 30 days, filed with the ticket — expected: per-runner-type minutes, matching the measured table.
- [ ] `gh run list --limit 200 --json databaseId,conclusion,createdAt,updatedAt` — expected: the run count per month used in the projection.
- [ ] A dry-run costing of the new lanes — expected: projected per-run and per-month Windows minutes with the 2× multiplier applied, and the twelve-month figure for each option.
- [ ] `grep -n "runner" docs/engineering.md` — expected: the recorded decision with its date and basis.

## Evidence tier

Tier 1 — Static/build/architecture. It obliges recorded measurements and a documented decision; it proves cost and consistency, and nothing about application behaviour.

## Documentation changes

- `docs/engineering.md` § Branches and delivery — the runner strategy, the constraint it answers, the date and the measured basis.
- `docs/desktop/08-testing/README.md` § 7 — replace the open mitigation list with the decision.
- `docs/operations.md` — if self-hosted, the host, its ownership and its monitoring.

## Guardrails

- **Azure**: no write, and no Azure involvement at all — this is a GitHub billing question.
- **Scope boundary**: may edit `docs/engineering.md`, `docs/desktop/08-testing/README.md` and `docs/operations.md`. Must not change `.github/workflows/ci.yml` (lane changes are [[DSK-08-13]]), must not register or configure a runner, and must not change any repository or billing setting — all of those are operator actions.
- **Traps**: a new CI platform is not justified merely by the desktop conversion (proposal §21.2) — the options are hosted, self-hosted or trimmed, not "move to another provider". LocalDB is Windows-only, so the integration shards cannot be moved to Linux to save minutes. Putting a self-hosted runner on the host that custodies the signing certificate concentrates risk: address fork-PR isolation explicitly or reject the option. The plan requires verifying the allowance and rates at decision time rather than reusing the figures quoted when the plan was written.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — spike` if the branch carries no production change; otherwise required over the branch diff and recorded under a dated `## Simplification pass` heading.

## Outcome

_Filled at closeout._
