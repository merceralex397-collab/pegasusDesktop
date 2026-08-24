---
id: TOOL-007
type: ticket
title: >-
  DSK-12-07 · Put the `## Routing` block and the Appendix C evidence shape into
  the Kanmer ticket documents
status: backlog
area: agent-tooling
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-12
  - phase-0
  - tier-1
groups:
  - EPIC-013
  - HZN-001
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:10:38.208Z'
updated: '2026-08-24T08:10:38.208Z'
---

## What

Make every desktop-conversion plan document carry a `## Routing` block (subagent · pinned skills · MCP tools) and every post-implementation report carry the proposal's Appendix C evidence shape, by putting the requirement somewhere a Kanmer upgrade will not silently erase, and prove it on two real tickets.

## Why

L-04 is locked: "every ticket names its subagent, skills, and MCP tools". Proposal §25 makes the agent-skills block a required ticket section and §20.5 step 7 requires the implementation evidence to be recorded in the Appendix C shape. Today neither template asks for either: `.grok/skills/kanmer-tickets/assets/ticket-template.md` has What / Why / Approach / Verification / Outcome, and `.grok/skills/kanmer-execute/assets/post-implementation-report-template.md` has Summary / Changes / Governing docs / Risks / Verification hand-off. Without this ticket, an agent following the stock templates produces a plan with no routing and a report with no skill SHAs, and the reviewer has nothing to check the invocation protocol against.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-07`
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 6 — the invocation protocol (7 steps), the review protocol (§20.6) and the ticket-metadata YAML block (`agent_skills:` with `project`, `required_capabilities`, `lockfile`; `routing:` with `subagent`, `mcp`). Copy that block verbatim; do not paraphrase it.
- Plan detail: `docs/desktop/12-agent-tooling/skill-routing.md` — the table a `## Routing` block is filled from
- Plan detail: `docs/desktop/00-governance-and-workflow/README.md` § Ticket template (proposal §25 → Kanmer documents) — the mapping table, including "7 Agent skills → `plan/` **`## Routing` block — required**" and "10 Verification → `post-implementation-report` (Appendix C shape)"
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 25 Ticket structure, § 20.5 Invocation protocol, Appendix C Agent implementation evidence (the seven headings: Skills consulted / Applicable guidance / Project decisions taking precedence / Repository evidence / Implementation / Verification / Deviations)
- Repository evidence:
  - `.grok/skills/kanmer-tickets/assets/ticket-template.md` — the stock ticket body template; no routing section
  - `.grok/skills/kanmer-execute/assets/post-implementation-report-template.md` — Summary, Changes, Governing docs, Risks / follow-ups, Verification hand-off; no skills or SHAs
  - `.grok/skills/kanmer-plan/`, `.grok/skills/kanmer-review/assets/pr-review.md` — the plan and review halves
  - `.grok/skills/.kanmer-skills-version` — `0.1.0`; the `.grok/skills/` tree is tracked (34 files) but is **installed and reconciled by `kanmer-setup`**, and `get_status` reports its artefacts as `behind` by content hash
  - `AGENTS.md:1-22` — the `<!-- kanmer:instructions:start … managed by kanmer-setup; edits inside will be overwritten -->` block
  - `.agents/skills/project/pegasus-desktop/SKILL.md` — already carries an "Invocation protocol" and an "Evidence format (Appendix C)" section; it is a candidate durable home
  - `scripts/Test-MarkdownPlacement.ps1:31` — `.grok` is an allowed Markdown root, so editing files there does not fail the placement gate
- Binding decisions:
  - **L-04** (locked) — every ticket names its subagent, skills and MCP tools.
  - **L-05** (locked) — the board is seeded from these plans, so the template must fit the tickets already on the board.
- Depends on:
  - `DSK-00-03` — the board shape (areas, horizons, epics and their `context.md`) must exist before a template can be proven on it.
  - `DSK-00-04` — the DSK tickets must exist before two of them can be used as the proof in step 8.

## Routing

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (read-only; it audits the two sample documents against the required shape)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-tickets` (`.grok/skills/kanmer-tickets/SKILL.md`) → `kanmer-docs` (`.grok/skills/kanmer-docs/SKILL.md`) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `get_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates: `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then `get_doc_gates <this ticket's board id>` and `take_ticket`.
2. Read both stock templates end to end so the change is additive, not a rewrite: `.grok/skills/kanmer-tickets/assets/ticket-template.md` and `.grok/skills/kanmer-execute/assets/post-implementation-report-template.md`.
3. Copy the two artefacts that must appear verbatim, into the ticket plan first so they are not paraphrased later: (a) the ticket-metadata YAML block from `docs/desktop/12-agent-tooling/README.md` § 6 (`agent_skills:` / `routing:`), and (b) the seven Appendix C headings from `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` Appendix C.
4. **Decide and record the durable home.** The plan says the `kanmer-tickets` template carries Routing, but `.grok/skills/` is installed by `kanmer-setup` and compared by content hash, so a hand edit will be reported stale and may be reinstalled over. Choose between: (a) edit the `.grok/` templates and record the reconcile step; (b) put the requirement in `.agents/skills/project/pegasus-desktop/SKILL.md` plus each EPIC group's `context.md`, which no upgrade touches; (c) both, with `.grok/` as convenience and the project skill as authority. Write the choice and its reason under a dated heading in the plan document — do not leave it implicit.
5. Define the `## Routing` block shape exactly once, wherever step 4 put it, as five bullets: **Subagent** (name + `.codex/agents/<name>.toml`), **Skills** in load order with the pinned source path of each, **MCP** server and exact tool names, **Kanmer pipeline** for the ticket's profile, **Reviewer** (`pegasus-desktop-reviewer`). Fill it from `docs/desktop/12-agent-tooling/skill-routing.md`, never from memory.
6. Define the report addition as one new section, `## Agent evidence`, with the seven Appendix C headings as sub-headings. It is additive: the existing Summary / Changes / Governing docs / Risks / Verification hand-off sections stay.
7. If step 4 chose to edit `.grok/` files, record the reconcile procedure in the same place: after a Kanmer update, run `kanmer-setup` and re-apply the two additions; `get_status` → `repo.stale` is what reports the drift.
8. Prove it on two real tickets. Pick two existing DSK tickets on the board, write their `plan/` document with `set_ticket_doc` using the shape, and confirm each carries a filled `## Routing` block and a dated `## Simplification pass` heading (`AGENTS.md` § Repository task workflow step 4; docs-only tickets record `n/a — docs-only`). Read them back with `get_ticket_doc`.
9. Do not duplicate [[DSK-00-11]]. That ticket owns the *enforcement* half — the reviewer checklist that asserts the two headings are present. This ticket owns the *template* half. Name the boundary in the plan document so neither ticket writes the other's change.
10. Have `pegasus-desktop-reviewer` read both sample documents and report whether the routing names resolve to real files: every skill path must exist and every subagent name must match a `.codex/agents/*.toml`. Transcribe its findings — it cannot write.
11. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and, if any `.md` was added, `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD`. `.grok` is an allowed root; a new template file there passes.
12. Record the Appendix C evidence for this ticket itself — it is the first report to use the new section, which is the cheapest way to prove the shape works.

## Acceptance criteria

- [ ] The `## Routing` block shape is defined in exactly one place, and that place is recorded with the reason it was chosen over the alternatives.
- [ ] The Appendix C evidence shape appears as an `## Agent evidence` section in the post-implementation report template, with all seven headings.
- [ ] The ticket-metadata YAML block from plan § 6 is reproduced verbatim, not paraphrased.
- [ ] Two existing DSK tickets have `plan/` documents carrying a filled `## Routing` block and a dated `## Simplification pass` heading.
- [ ] If `.grok/` templates were edited, the `kanmer-setup` reconcile step is written down.
- [ ] The boundary with [[DSK-00-11]] is stated; no reviewer-checklist change is made here.

## Verification

- [ ] `get_ticket_doc <first sample ticket id>` — expected: a `plan` document containing `## Routing` with five filled bullets and a `## Simplification pass` heading carrying a date.
- [ ] `get_ticket_doc <second sample ticket id>` — expected: the same two headings present.
- [ ] `grep -n 'Routing' .grok/skills/kanmer-tickets/assets/ticket-template.md` or the equivalent grep on the chosen durable home — expected: the block definition found exactly once across the repository.
- [ ] `grep -c 'Skills consulted' <the report template>` — expected: `1`.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: `All relative Markdown links resolve (<n> files checked).`

## Evidence tier

Tier 1 — Static/build/architecture. It obliges the two sample ticket documents read back through `get_ticket_doc` and a grep showing the shape is defined once; nothing is compiled.

## Documentation changes

- `.agents/skills/project/pegasus-desktop/SKILL.md` — the `## Routing` block shape and the Appendix C section list, if step 4 chose the project skill as the durable home. (Agent skill playbooks are agent tooling, not documentation — `AGENTS.md` § New Markdown placement.)
- `.grok/skills/kanmer-tickets/assets/ticket-template.md` and `.grok/skills/kanmer-execute/assets/post-implementation-report-template.md` — only if step 4 chose to edit them, with the reconcile note.
- `docs/desktop/12-agent-tooling/README.md` § 6 — a line recording where the shape now lives.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may edit `.grok/skills/**` templates, `.agents/skills/project/pegasus-desktop/SKILL.md`, `docs/desktop/12-agent-tooling/README.md`, and Kanmer ticket documents. Must not edit the reviewer checklist ([[DSK-00-11]]), any `.codex/agents/*.toml`, `eng/skills/**`, `src/` or `tests/`.
- **Traps**: `.grok/skills/` is machine-managed by `kanmer-setup` — hand edits there are reported stale by `get_status` and can be overwritten, which is why step 4 is a recorded decision rather than a default. Never edit inside the `<!-- kanmer:instructions -->` managed block of `AGENTS.md`. Kanmer doc gates are resolved at runtime by `get_doc_gates`, never from `board.yml`. Ticket-transient documents live in Kanmer, not in the repository tree — do not add a `.md` outside the allowed roots.
- **Open question to carry**: whether a Kanmer upgrade reinstalls `.grok/skills/` in place or only reports drift is not recorded anywhere; if step 4 chooses option (a), file that as an open question rather than assuming an answer.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
