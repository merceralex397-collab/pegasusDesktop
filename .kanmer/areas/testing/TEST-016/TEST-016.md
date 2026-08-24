---
id: TEST-016
type: ticket
title: >-
  DSK-08-16 · End-to-end business scenarios 1–14 as UAT scripts, each mapped to
  the Test/UAT stack or the pilot ring
status: backlog
area: testing
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-08
  - phase-9
  - tier-12
  - needs-operator
groups:
  - EPIC-009
  - HZN-010
links: []
docs_todo: true
archived: false
created: '2026-08-24T07:55:26.128Z'
updated: '2026-08-24T07:55:26.128Z'
---

## What

Turn the fourteen end-to-end business scenarios into written UAT scripts — steps, expected results, evidence to capture, the tier each proves, and where it runs — and dry-run the set once on the Test/UAT stack. This is the release critical path.

## Why

Proposal §22.3 makes it a merge rule that *every release passes the end-to-end critical path*, and §24 Phase 9 requires pilot users to complete all normal workflows with no unexplained data divergence. Under L-02 there is no Azure test environment, so three of the fourteen scenarios cannot be fully proved locally and must be repeated on the production pilot ring; writing that down per scenario is what stops a local pass being mistaken for release evidence. Consumes the stack from [[DSK-08-17]] and the automated suites from [[DSK-08-07]] and [[DSK-08-08]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-16`
- Plan detail: `docs/desktop/08-testing/test-uat-stack.md` § "UAT scripts — end-to-end scenarios 1–14", which gives the mapping table verbatim: 1 login (stack, then pilot); 2 Graph intake while no desktop is open (stack replay inbox, **pilot** real mailbox); 3 user sees and opens the new intake (stack); 4 duplicate detection and provider matching (stack); 5 case created or resolved (stack); 6 vehicle data looked up (stack replay, pilot live); 7 documents loaded from and uploaded to Box (stack local custody, **pilot** Box); 8 assessment/case data completed (stack); 9 report generated, previewed, finalized, stored (stack); 10 assignment/status/history correct (stack); 11 another user sees the update and a conflicting edit is handled (stack, two desktops or one desktop plus API client); 12 obsolete version blocked and updates successfully (stack local feed, **pilot** real feed); 13 integration failure visible and recoverable (stack failure injection); 14 audit identifies who performed each sensitive action (stack)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "End-to-end business scenarios", § 22.3 coverage policy, § 23.1 required conversion evidence
- Repository evidence:
  - `docs/desktop/08-testing/test-uat-stack.md` § "Evidence capture" — what each scenario must file: `winapp ui` screenshots and JSON, TRX under `artifacts/test-results/`, axe output, `Get-AppxPackage` transcripts, the performance table, gateway and Worker logs, the desktop diagnostics bundle
  - `docs/desktop/08-testing/test-uat-stack.md` § Data — seed set built from `reference/` and existing integration-test builders (`tests/Pegasus.IntegrationTests/DocumentExtraction/`); never `corpus/`
  - `docs/engineering.md` § Required evidence tiers, tier 12 — "Registration or mock-only paths do not satisfy this tier"
- Binding decisions:
  - L-02 — the stack is the UAT surface and the pilot ring is the only real-Azure validation; scenarios 2, 7 and 12 are explicitly repeated there.
- Depends on: `DSK-08-17` — the stack the scripts run on. The slices supply the workflows: `DSK-05-03`, `DSK-05-04`, `DSK-05-08`, `DSK-05-09`, `DSK-05-14`, `DSK-05-15`, `DSK-05-17`, `DSK-05-18`, `DSK-05-20`. Scenario 12 also needs `DSK-04-12` (local feed) and `DSK-04-06` (compatibility endpoint).

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-verify` (`.grok/skills/kanmer-verify/SKILL.md`, Kanmer 0.1.0) for the proof shape each scenario files
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/test-uat-stack.md` in full — particularly the fourteen-row mapping table, the evidence-capture list and the data rules — then `docs/desktop/01-inventory-and-parity/README.md` for the parity-matrix rows each scenario feeds. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Load `pegasus-desktop`, then `kanmer-verify`. Create `docs/desktop/08-testing/uat-scenarios/` with one file per scenario, `scenario-01.md` … `scenario-14.md`. This folder is inside `docs/desktop/`, which is an allowed Markdown root — anything outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job.
3. Give every scenario file the same five headings: **Preconditions** (stack state, seeded data, package version), **Steps** (numbered operator actions), **Expected results** (observable, in operator vocabulary), **Evidence to capture** (from the evidence-capture list), **Where it runs and what it proves** (stack, pilot, or both, with the tier).
4. Copy the where-it-runs value for each scenario from the mapping table verbatim; do not re-derive it. Scenarios 2, 7 and 12 carry an explicit second row: "repeat on the pilot ring — the stack cannot prove real Graph mailbox polling / Box tenant custody / the production feed and signing chain".
5. For each scenario, name the automated script that covers part of it ([[DSK-08-07]], [[DSK-08-08]]) and state precisely what the human still checks. A scenario that is entirely automated says so; a scenario with no automation says that too. Do not claim automation that does not exist.
6. Write the scenario 11 procedure explicitly: two desktops, or one desktop plus an API client, editing the same case; the expected result is the `409` problem surfaced as an operator sentence with a reload or compare path, and nothing silently overwritten.
7. Write the scenario 13 procedure against the stack's existing failure injection (`scripts/Invoke-LocalDevelopment.ps1 -FailureMode AfterWeb`): the failure must be visible in the Operations view and recoverable by retry, with the retry outcome recorded.
8. Write the scenario 14 procedure: perform one sensitive action per role and assert the action history and security events name the acting operator — the actor, not the service.
9. Add `docs/desktop/08-testing/uat-scenarios/README.md`: the mapping table, the pass/fail recording convention, where evidence is filed (the Kanmer release ticket's `proof` and `reference`, never the tree), and the rule that a release candidate must have all fourteen recorded.
10. **Operator step**: dry-run all fourteen once on the Test/UAT stack, recording pass/fail and the evidence for each. Hand back the completed set. A scenario that cannot be run because its slice is not finished is recorded as `blocked: <slice handle>`, never as passed.
11. Cross-check each scenario against the parity matrix rows it proves ([[DSK-05-25]]) so a scenario pass can move a matrix row to `UAT passed`, and record the mapping in the scenario file.
12. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — both are CI jobs and the new folder must satisfy them. Then run the simplification pass (`n/a — docs-only` if the branch touches no code) and record it under a dated `## Simplification pass` heading.

## Acceptance criteria

- [ ] Fourteen scenario files exist, each with preconditions, steps, expected results, evidence to capture, and where it runs with the tier it proves.
- [ ] Scenarios 2, 7 and 12 carry an explicit pilot-ring repeat and say what the stack cannot prove.
- [ ] Each scenario names the automated coverage that exists and what the human still checks.
- [ ] One full dry run is recorded, with blocked scenarios named by the slice that blocks them.
- [ ] Every scenario maps to the parity-matrix rows it can advance.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exit 0 (the new folder is under an allowed root).
- [ ] The filed dry-run record — expected: fourteen rows, each `pass`, `fail` or `blocked: <handle>`, each with its evidence reference.

## Evidence tier

Tier 12 — Integrated workflow. It obliges an authenticated source receipt through Core, SQL and outbox, the actual Worker trigger, the adapter outcome, the persisted operator view, telemetry and safe replay; registration or mock-only paths do not satisfy it, which is why three scenarios are repeated on the pilot ring.

## Documentation changes

- `docs/desktop/08-testing/uat-scenarios/*.md` — new; fourteen scenarios and their README.
- `docs/desktop/08-testing/README.md` § 4 — point the exit gate at the scenario folder.
- `docs/operations.md` — note the release critical path and where its evidence is filed.

## Guardrails

- **Azure**: no write. The pilot-ring repeats are performed under the release runbooks of area 09, not by this ticket.
- **Scope boundary**: may create `docs/desktop/08-testing/uat-scenarios/**` and edit the two documentation lines. Must not create Markdown outside `docs/(prd|frd|adr|design|desktop)` — the CI `documentation` job fails on it — and must not file evidence in the repository tree.
- **Traps**: never fabricate domain data; the seed set comes from `reference/` and the existing integration builders, and `corpus/` is never copied. A local pass on scenarios 2, 7 and 12 is not release evidence. Automated axe and `winapp ui` results do not replace the human checks these scripts define.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only` if the branch touches no code; otherwise required over the branch diff, recorded under a dated `## Simplification pass` heading.

## Outcome

_Filled at closeout._
