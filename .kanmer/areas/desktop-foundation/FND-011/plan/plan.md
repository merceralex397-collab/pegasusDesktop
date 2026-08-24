# Plan — FND-011: Enforce the ticket template — every DSK plan document carries `## Routing` and a dated `## Simplification pass`

**Diff estimate: ~3 files, ~14 lines** in the repository, plus one Kanmer plan document that
is not a repository file. `docs/engineering.md` § plan sizing requires the estimate first.
This profile is `chore` — it owes no `research` or `files` document, so the measured inventory
below carries the surface area alone.

## Measured file-and-line inventory

Measured at `bbd1c549` on 2026-08-24.

| Path | Current size and the exact anchor | Change | Est. lines |
| --- | --- | --- | --- |
| `.codex/agents/pegasus-desktop-reviewer.toml` | 26 lines. Four keys (`name`, `description`, `model_reasoning_effort`, `sandbox_mode`) at `:1-4`; `developer_instructions = """` opens at `:6` and the closing `"""` is `:26`. The `Review lenses` list is `:13-23`; the existing simplification-pass lens is the last bullet, `:23`; the Output line is `:25`. | One new lens bullet inserted **inside** the `"""` block, immediately after `:23` | ~2 |
| `.agents/skills/project/pegasus-desktop/SKILL.md` | 110 lines. `## Invocation protocol (every ticket)` at `:77`; six numbered steps at `:79-90` (`1.`=79, `2.`=81, `3.`=83, `4.`=85, `5.`=87, `6.`=89); `## Evidence format (Appendix C)` at `:92`. | New step inserted after `:80` as the new `2.`, and `:81-90` renumbered `3.`–`7.` | ~3 added, 6 digits renumbered |
| `docs/desktop/00-governance-and-workflow/README.md` | 431 lines. `### Ticket template (proposal §25 → Kanmer documents)` at `:264`; its table row 7 (the `plan/` `## Routing` block) at `:272`; the two-`AGENTS.md`-rules paragraph at `:279-286`. | One sentence recording the enforced rule, appended to the `:279-286` paragraph | ~3 |
| Kanmer plan document of the sample ticket | Not a repository file — lives in `.worktrees/kanmer/.kanmer` | Read with `get_ticket_doc`; repaired with `set_ticket_doc` only if a heading is missing | 0 repo lines |

Two measured facts that shape the plan:

- `git status --porcelain .grok` is **empty** today. Step 6's "no `.grok/` edits" rule is
  therefore verifiable as a clean-tree assertion at PR time, not just an intention.
- `docs/desktop/12-agent-tooling/skill-routing.md` is 69 lines and its
  `## Not applicable to this conversion (do not load)` heading is at `:56`. That is the table
  the new lens must cite by name.

## Approach

Put the rule in the two places an agent actually reads — the reviewer's own lens list and the
project skill's invocation protocol — and prove it once against a real ticket. The rejected
alternative was a CI check: a heading grep in the `documentation` job would have to read
Kanmer plan documents, and those live in `.worktrees/kanmer/.kanmer`, outside the repository
tree the CI job checks out. A second rejected alternative was editing
`.grok/skills/kanmer-plan/SKILL.md` so the plan-writing skill itself carried the rule — that
tree is installed and re-written by `kanmer-setup`, so a project rule added there is lost at
the next Kanmer update.

The lens is written so it can fail a plan, not merely describe one: it names the two headings,
names `skill-routing.md` as the source of legal skill names, and makes a skill drawn from that
document's `:56` "do not load" table a finding.

## Governing docs

The ticket's `refs` is empty and it carries `docs_todo: true` — confirmed in
`get_doc_gates FND-011` (`"refs": []`, `"docs_todo": true`). Nothing this ticket touches is a
governing document: the reviewer TOML and the project skill are agent instructions, and the
one plan sentence is programme planning. No new ADR is created here either.

> **New ADR** — none is authored by this ticket. The nearest conversion ADR is ADR-0110
> (agent-skill pinning and the invocation/review protocol), authored by [[TOOL-008]] (plan
> handle `DSK-12-08`); ADR-0110 is co-claimed with [[FND-005]] (plan handle `DSK-00-05`), so
> see [[TOOL-008]]'s plan for the ownership reconciliation. This plan is written to the rule
> as recorded in `docs/desktop/00-governance-and-workflow/README.md:264-286` § 3 "Ticket
> template"; if ADR-0110 lands a different invocation protocol this plan is revised before
> implementation.

Programme-level authorities that bind today, with the step that satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| **L-04** (`docs/desktop/README.md` § Locked decisions) | Every ticket names its subagent, skills and MCP tools | Steps 2–3, 5 |
| **L-05** (same) | The board is the executable form of the plan set, so the enforcement point is the ticket document, not a repository file | Step 3's rejection of a CI check; step 7 |
| Proposal § 25 item 7 (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:1932…`) | "Agent skills — exact pinned skills to invoke" is a required ticket section | Steps 2–3, 5 |
| Plan 00 § 3 Ticket template row 7 (`docs/desktop/00-governance-and-workflow/README.md:272`) | The `plan/` `## Routing` block is **required**, in the form `subagent · skills (pinned path) · MCP tools` | Steps 2, 3, 9 |
| Plan 00 § 3 (`docs/desktop/00-governance-and-workflow/README.md:279-286`) | The simplification pass is recorded under a dated heading in the plan; docs-only records `n/a — docs-only`; review is by an agent that did not implement | Steps 2, 3, 9 |
| `AGENTS.md:289-297` § Repository task workflow step 4 | Findings and dispositions under a dated "Simplification pass" heading **in the ticket's plan** | Steps 2, 3 |
| `AGENTS.md:298-305` § step 5 | Independent review by an agent that did not implement | Step 8, and the Routing block's reviewer |
| Plan 12 skill routing (`docs/desktop/12-agent-tooling/skill-routing.md:56`) | Skills in the "Not applicable to this conversion (do not load)" table must not be loaded | Step 3 (the lens's failure condition) |

## Routing

Copied from the ticket body's `## Routing` block — the block this ticket exists to make
mandatory.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan`
  (`.grok/skills/kanmer-plan/SKILL.md`) → `kanmer-review`
  (`.grok/skills/kanmer-review/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `get_ticket_doc`, `set_ticket_doc`,
  `take_ticket`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-tickets` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates FND-011` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md:298-305`)

## Steps

These refine the ticket body's twelve implementation steps — same order, same ownership, same
file paths.

1. **Orient.** Read `docs/desktop/00-governance-and-workflow/README.md:264-286` (§ 3 Ticket
   template, the table and the two-rules paragraph), `AGENTS.md:289-305` (workflow steps 4–5),
   `.codex/agents/pegasus-desktop-reviewer.toml` in full (26 lines), and
   `.agents/skills/project/pegasus-desktop/SKILL.md:77-90` (§ Invocation protocol). Call
   `get_doc_gates FND-011`, then `take_ticket` onto `task/<slug>` in
   `../pegasus-worktrees/<slug>` from `origin/dev`.
2. **Write the required shape down once, here in this plan**, so both edits say the same
   thing. The canonical block, exactly as the body specifies:

   ```
   ## Routing
   - Subagent: <name> (.codex/agents/<name>.toml)
   - Skills, in load order: pegasus-desktop (.agents/skills/project/pegasus-desktop/SKILL.md) -> <skill> (<pinned source path>) -> ...
   - MCP: <server> (<exact tool names>)

   ## Simplification pass (YYYY-MM-DD)
   <findings and dispositions, or "n/a - docs-only">
   ```

3. **Add the review lens** to `.codex/agents/pegasus-desktop-reviewer.toml`, as a new bullet
   immediately after the existing simplification-pass lens at `:23`, worded:
   "Plan document shape: the ticket's `plan/` document carries a `## Routing` block naming
   subagent, skills with their pinned source paths, and MCP tools from
   `docs/desktop/12-agent-tooling/skill-routing.md`, and a dated `## Simplification pass`
   heading; a skill from the routing document's 'Not applicable — do not load' table is a
   finding."
4. **Keep the TOML valid.** The bullet goes *inside* the `developer_instructions = """…"""`
   block that opens at `:6` and closes at `:26`; do not open a second block and do not put a
   bare `"""` in the lens text. Verify with
   `python -c "import tomllib,sys;tomllib.load(open(sys.argv[1],'rb'))" .codex/agents/pegasus-desktop-reviewer.toml`
   — expected: exit 0, no output. If `python` is unavailable on the machine, any TOML parser
   that exits non-zero on a parse error is an acceptable substitute; record which one ran.
5. **Add the same requirement to the project skill.** Insert a new numbered step after
   `.agents/skills/project/pegasus-desktop/SKILL.md:80`, becoming the new `2.`: "Before
   implementing, write the ticket's `plan/` document with a `## Routing` block (subagent ·
   skills with pinned source paths · MCP tools) and a dated `## Simplification pass` heading;
   `pegasus-desktop-reviewer` checks both." Then renumber the existing `2.`–`6.` at
   `:81`, `:83`, `:85`, `:87`, `:89` to `3.`–`7.`. Six digits change; nothing else in the file
   moves semantically.
6. **Touch nothing under `.grok/skills/`.** That tree is installed and re-written by
   `kanmer-setup`, so a project rule added there is lost at the next Kanmer update; `get_status`
   reports the installed skills trees and their version stamps. `git status --porcelain .grok`
   is empty today and must still be empty at PR time.
7. **Pick the sample and read it.** Choose one already-planned `DSK` ticket — every ticket in
   the `agent-tooling` area already carries a `plan` document, so [[TOOL-002]] (plan handle
   `DSK-12-02`) or [[TOOL-007]] (plan handle `DSK-12-07`) is a live candidate; confirm with
   `list_items` that `docs.plan` is `true` before choosing. Read it with
   `get_ticket_doc <id> plan`. If either heading is missing, repair that ticket's plan with
   `set_ticket_doc` and record it as the first enforcement instance. Do **not** take, move or
   edit the sample ticket itself — only its plan document.
8. **Run the reviewer against the sample.** Spawn `pegasus-desktop-reviewer` and confirm its
   findings table names the plan-shape lens explicitly: a pass on the compliant plan, and a
   finding on a deliberately stripped copy (strip the copy in the ticket scratch with
   `append_scratch`, never in the ticket's real plan document).
9. **Record the enforced rule in the plan set.** One sentence appended to the paragraph at
   `docs/desktop/00-governance-and-workflow/README.md:279-286`, saying that the two headings
   are enforced by the `pegasus-desktop-reviewer` plan-shape lens and by the `pegasus-desktop`
   invocation protocol — so plan, reviewer instructions and skill agree.
10. **Coordinate before merging.** [[TOOL-007]] (plan handle `DSK-12-07`, the `## Routing`
    block and the Appendix C evidence shape) and [[TOOL-010]] (plan handle `DSK-12-10`, the
    skill-update procedure and the §20.6 review checklist) both edit the same reviewer
    instructions. Read their plan documents with `get_ticket_doc` before opening the PR and
    agree one lens list between the three; three uncoordinated edits produce a contradictory
    list.
11. **Gate and PR.** Run `pwsh ./scripts/Test-DocumentationLinks.ps1` — exits 0. Run the
    simplification pass over this branch's own diff and record `n/a — docs-only` under a dated
    `## Simplification pass` heading in this plan (`AGENTS.md:289-297`). Open the PR against
    `dev`; merge after the independent review.
12. **Write `proof`** as a `command-log`: the TOML parse result, the sample ticket's
    `get_ticket_doc` output showing both headings, the reviewer's verdict on the compliant
    plan and on the stripped copy, and the empty `git status --porcelain .grok`.

## Verification

Evidence tier from the ticket body: **Tier 1 — Static/build/architecture**. The claim is
document shape and agent instructions; the reviewer's verdict on the sample is the operable
evidence. `proof` is a `command-log`.

| Command | Expected |
| --- | --- |
| `python -c "import tomllib,sys;tomllib.load(open(sys.argv[1],'rb'))" .codex/agents/pegasus-desktop-reviewer.toml` | exit 0, no output |
| `grep -n 'Routing' .codex/agents/pegasus-desktop-reviewer.toml .agents/skills/project/pegasus-desktop/SKILL.md` | a hit in both files |
| `get_ticket_doc <sample ticket id> plan` | the plan contains `## Routing` **and** `## Simplification pass (` followed by a date |
| `git status --porcelain .grok` | no output |
| `grep -c '^- ' .codex/agents/pegasus-desktop-reviewer.toml` | one more than before the change — the lens list grew by exactly one bullet |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |

The behaviour to observe: the reviewer, run twice, passes the compliant plan and raises a
finding on the stripped copy. A lens that cannot fail is not enforcement.

## Risks / open questions

- **Risk: an unescaped `"""` inside the lens text breaks the TOML.** Mitigation: step 4's
  parse check runs before the PR, and the lens is written as a single bullet with no triple
  quotes.
- **Risk: three tickets edit one lens list.** Mitigation: step 10 reads [[TOOL-007]]'s and
  [[TOOL-010]]'s plans and agrees one list before merge. This is a **scope boundary, not an
  open question** — both siblings are named and own their own edits.
- **Risk: the renumber in step 5 is applied to only part of the list.** Mitigation: the
  measured digit positions (`:81`, `:83`, `:85`, `:87`, `:89`) are in the inventory table, and
  the diff is five one-character changes plus three added lines.
- **Risk: `python` is not on the machine.** Mitigation: step 4 names the substitute condition
  (any parser that exits non-zero on a parse error) and requires recording which ran.
- **Scope boundary, not an open question**: the sample ticket's plan may be repaired here, but
  the sample ticket is not taken, moved or edited — [[FND-004]] (plan handle `DSK-00-04`) owns
  the seeded ticket bodies.
- No `open-questions` document is opened. The ticket body does not instruct one, and nothing
  here is unsettled: the enforcement point, the two headings, the forbidden tree and the
  coordination partners are all named in the body.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 (`AGENTS.md:289-297`) requires a
pass over this branch's own diff before the PR, recorded here under a dated heading. Record
`n/a — docs-only` for this documentation-only branch._
