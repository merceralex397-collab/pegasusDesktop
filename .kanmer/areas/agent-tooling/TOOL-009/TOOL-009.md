---
id: TOOL-009
type: ticket
title: >-
  DSK-12-09 · Dry run: take one foundation ticket end to end through the
  subagent protocol
status: preparing
area: agent-tooling
assignee: ''
profile: spike
stageEntered:
  preparing: '2026-08-24T21:21:30.551Z'
labels:
  - desktop-conversion
  - plan-12
  - phase-1
  - tier-1
groups:
  - EPIC-013
  - HZN-002
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:10:38.266Z'
updated: '2026-08-24T21:21:30.551Z'
---

## What

Run one real area-02 foundation ticket through the full invocation protocol with `winui-dev`, `pegasus-test-engineer` and `pegasus-desktop-reviewer`, observe where the instructions fail a working agent, and record the concrete fixes as follow-up tickets. This spike changes no repository file itself.

## Why

`docs/desktop/12-agent-tooling/README.md` § 4 makes "first ticket executed end to end with the protocol and reviewed by `pegasus-desktop-reviewer`" part of this area's exit gate, and proposal §20.5–20.6 defines a protocol that has never been executed. The eight agent TOMLs, the project skill and the routing tables were all written from documentation rather than from use. The cheapest place to discover that an agent cannot find the endpoint map, or that a read-only agent's output was never transcribed, is one ticket — not the twenty-two vertical slices of area 05 that inherit the same instructions.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-09`
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 6 — the seven-step invocation protocol and the review protocol; § 4 Exit gate
- Plan detail: `docs/desktop/12-agent-tooling/subagents.md` — § Roster, § Usage examples (the second example is exactly this shape: spawn an implementer and a test engineer in parallel, then the reviewer on the combined diff), and the three agent sections for `winui-dev`, `pegasus-test-engineer`, `pegasus-desktop-reviewer`
- Plan detail: `docs/desktop/02-architecture-and-foundation/README.md` § 5 — the candidate target rows: `DSK-02-01` (author ADR-0100/ADR-0104, `kanmer-docs` only), `DSK-02-02` (CPM), `DSK-02-05` (scaffold `src/Pegasus.Desktop` with `dotnet new winui-mvvm`, routed to `winui-dev` with `winui-setup`, `winui-dev-workflow`, `winui-design`)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 20.5 Invocation protocol, § 20.6 Review protocol, Appendix C Agent implementation evidence
- Repository evidence:
  - `.codex/agents/winui-dev.toml` — loads `pegasus-desktop`, then `winui-dev-workflow`, `winui-design`; requires a unique `AutomationProperties.AutomationId` on every interactive control; never self-delegates
  - `.codex/agents/pegasus-test-engineer.toml` — `sandbox_mode = "workspace-write"`, `model_reasoning_effort = "high"`; xunit 2.9.3 and hand-rolled fakes only; runbook profiles
  - `.codex/agents/pegasus-desktop-reviewer.toml` — `sandbox_mode = "read-only"`; ten review lenses; "if you implemented the change, say so and stop"
  - `.agents/skills/project/pegasus-desktop/SKILL.md` § Invocation protocol and § Evidence format (Appendix C)
  - `docs/runbook.md` § Locked restore, build, and test — the command profiles the agents must actually use
- Binding decisions:
  - **L-04** (locked) — subagents exist and every ticket names them; this spike is what proves the naming works in practice.
  - **L-02** — Test/UAT is local; no Azure test resource may be requested during the dry run.
- Depends on:
  - `DSK-12-05` — the `[agents]` table must be present and the roster loading, or there is nothing to delegate to.
  - `DSK-02-05` — the target. The plan says "area 02 first ticket"; `DSK-02-01` is ADR authoring routed to `kanmer-docs` and exercises neither the implementer nor the test engineer, so the first row that genuinely exercises the trio is `DSK-02-05` (scaffold `src/Pegasus.Desktop`). Record the choice in step 2.

## Routing

- **Subagents**: `winui-dev` (`.codex/agents/winui-dev.toml`), `pegasus-test-engineer` (`.codex/agents/pegasus-test-engineer.toml`), `pegasus-desktop-reviewer` (`.codex/agents/pegasus-desktop-reviewer.toml`) — in that order
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → whatever the chosen target ticket routes to, read from `.agents/skills/vendor/…` at the pinned revisions — for `DSK-02-05` that is `winui-setup`, `winui-dev-workflow`, `winui-design` (`.agents/skills/vendor/windows/<name>/`) and `run-tests`, `scaffold-dotnet-test-project` (`.agents/skills/vendor/dotnet/<name>/`) → `kanmer-research` (`.grok/skills/kanmer-research/SKILL.md`) for this spike's own document
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `get_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`) for any WinUI or Windows App SDK API the implementer is unsure of
- **Kanmer pipeline** for profile `spike`: `kanmer-research` → `kanmer-closeout`. The only gate is `enter-done`: `research` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then `get_doc_gates <this ticket's board id>` and `take_ticket`. Confirm [[DSK-12-05]] landed by checking `.codex/config.toml` contains an `[agents]` table.
2. Choose and record the target ticket. Default is `DSK-02-05` (scaffold `src/Pegasus.Desktop`) because it exercises the implementer, the test engineer and the reviewer. Write one sentence saying why the chosen row was picked over `DSK-02-01`; if area 02 has already progressed, pick the earliest unstarted row that still routes to `winui-dev` and say so.
3. Run the invocation protocol literally, in the seven steps of `docs/desktop/12-agent-tooling/README.md` § 6, and note the wall-clock time and any stall at each hand-off: (1) read project skill, area plan and ticket folder; (2) read the exact upstream `SKILL.md` files from the lockfile; (3) summarise only the applicable guidance in the plan and name overridden guidance; (4) implement the smallest vertical slice; (5) run the skill-prescribed verification plus the repository profiles; (6) record Appendix C evidence; (7) hand to the independent reviewer.
4. Delegate the implementation to `winui-dev`. It must load `pegasus-desktop` first, then `winui-dev-workflow` and `winui-design`, from the vendored paths. Its evidence must include the `BuildAndRun.ps1` output and the launched process id — a build log alone does not prove the app ran.
5. In parallel, delegate the test scaffold to `pegasus-test-engineer` per `subagents.md` § Usage examples. Its evidence must include the verbatim `dotnet test` command from `docs/runbook.md` § Locked restore, build, and test, and the pass/fail counts — not a summary sentence.
6. Delegate the review of the combined diff to `pegasus-desktop-reviewer`. Confirm it loads `winui-code-review` and `winui-design` **itself** rather than trusting the implementer's summary, and that it produces the findings table (severity, `file:line`, finding, cost, alternative, blocks merge yes/no) plus a one-line verdict.
7. Collect all three Appendix C reports and grade them against the seven headings (Skills consulted / Applicable guidance / Project decisions taking precedence / Repository evidence / Implementation / Verification / Deviations). List every heading that came back empty, vague or invented, with the agent that produced it.
8. Record every question an agent had to ask that its instructions should have answered — a missing path, an ambiguous command, a skill that did not resolve, a decision it could not find. Each one becomes a concrete proposed edit to a named `.codex/agents/*.toml` line or to `.agents/skills/project/pegasus-desktop/SKILL.md`.
9. Check the read-only mechanics explicitly: `pegasus-desktop-reviewer` cannot write files, so its findings must have been transcribed into the ticket by the caller. Record whether that actually happened or whether the findings only existed in a transcript — that failure mode silently loses review evidence.
10. Check the self-hop rule: confirm no agent delegated to another agent of its own kind, and record how that was observed rather than assumed.
11. Write the `research/` document: what worked, the graded evidence table from step 7, the instruction gaps from step 8, the read-only transcription finding, and a numbered list of proposed edits with the exact file and line each would touch.
12. **Do not make those edits here.** File them as follow-up tickets in `agent-tooling` and name the ids in the research document; a spike that quietly rewrites eight agent definitions has stopped being a spike.
13. Tick every item in `open-questions/` (or park it below the literal `## Parked (explicitly deferred)` heading with a reason) and move to `done` after `get_doc_gates` confirms `research` and `questions-resolved`.

## Acceptance criteria

- [ ] The target ticket is named with the reason it was chosen.
- [ ] `winui-dev`, `pegasus-test-engineer` and `pegasus-desktop-reviewer` each produced an Appendix C report, and all three are attached to this spike's research document.
- [ ] Each report is graded against the seven Appendix C headings, with empty or vague headings named.
- [ ] Every instruction gap is written as a proposed edit naming the exact file it would touch.
- [ ] The read-only transcription question is answered with evidence, not assumed.
- [ ] Follow-up ticket ids are recorded; no agent TOML or project skill was edited by this ticket.

## Verification

- [ ] `get_ticket_doc <this ticket's board id>` — expected: a `research` document containing three Appendix C reports, the grading table and the numbered proposed edits.
- [ ] `git status --porcelain` on this spike's own branch — expected: empty; the target ticket's code lands on the target ticket's branch, not this one.
- [ ] `search_items agent-tooling` — expected: the follow-up tickets named in the research document exist on the board.
- [ ] The recorded `BuildAndRun.ps1` output — expected: a launched process id, not only a successful build.
- [ ] The recorded `dotnet test` output — expected: the verbatim command from `docs/runbook.md` and its pass/fail counts.

## Evidence tier

Tier 1 — Static/build/architecture, for this spike's own claim. The target ticket carries its own tier; this spike proves only that the protocol ran and what it cost, and must not claim the target ticket's evidence as its own.

## Documentation changes

- `docs/desktop/12-agent-tooling/README.md` § 4 Exit gate — record the dry run as executed with its date. Everything else the spike finds becomes a follow-up ticket, not an edit here.

## Guardrails

- **Azure**: no write. L-02 stands: if any agent asks for an Azure test resource during the dry run, that is a finding to record, not a request to fulfil.
- **Scope boundary**: this spike writes Kanmer documents and files follow-up tickets. The target ticket's code changes belong to the target ticket's own branch, worktree and PR. Must not edit `.codex/agents/*.toml`, `.agents/skills/**` or `eng/skills/**`.
- **Traps**: Codex self-hop — an agent must never delegate to an agent of its own kind; read-only agents cannot write, so their output is lost unless the caller transcribes it; `winui-session-report` is user-invoked only and carries a privacy warning before anything is shared — do not use it to reconstruct the dry run; Kanmer gates are resolved at runtime by `get_doc_gates`, and a move crosses at most one gated boundary.
- **Sizing concern**: this row observes three agents across a real ticket and could sprawl. Timebox it to the single target ticket and file everything else; if the dry run cannot finish because the target ticket is blocked, record that as the finding rather than switching targets mid-run.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only` (this spike produces no branch diff).

## Outcome

_Filled at closeout._
