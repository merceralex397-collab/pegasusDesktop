# Plan — TOOL-007 (plan handle `DSK-12-07`): Put the `## Routing` block and the Appendix C evidence shape into the Kanmer ticket documents

**Diff estimate: ~3 repository files, ~45 lines** — plus two Kanmer ticket documents written
with `set_ticket_doc`, which are not repository files and not part of the diff.
`.agents/skills/project/pegasus-desktop/SKILL.md` +~30 (the five-bullet `## Routing` shape
and the seven `## Agent evidence` headings, added beside its existing § Invocation protocol
and § Evidence format sections), `.grok/skills/kanmer-tickets/assets/ticket-template.md` and
`.grok/skills/kanmer-execute/assets/post-implementation-report-template.md` +~6 each (a
pointer, not a copy — see step 4), `docs/desktop/12-agent-tooling/README.md` § 6 +1 line
recording where the shape lives.

## Approach

**Define the shape once in the project skill and make the `.grok/` templates point at it
rather than copy it.** That is a refinement of the body's step-4 option (c), and it is the
only option that satisfies the acceptance criterion "defined in exactly one place" while
still reaching an agent that opens a stock Kanmer template. The reasoning:

- `.grok/skills/` is **tracked but machine-managed** — installed and reconciled by
  `kanmer-setup`, with `get_status` reporting its artefacts as `behind` by content hash. A
  definition placed there is reported stale and can be reinstalled over. So it cannot be the
  authority (rules out option (a) alone).
- `.agents/skills/project/pegasus-desktop/SKILL.md` is tracked, hand-maintained, and — 
  verified 2026-08-24 — **already loaded first by all eight `.codex/agents/*.toml` files** as
  their step `0.`. It is the one file a conversion agent is guaranteed to read, and it
  already carries § Invocation protocol and § Evidence format (Appendix C) sections, so the
  shape belongs beside them, not in a new home.
- A one-line **pointer** in each `.grok/` template costs nothing if `kanmer-setup` wipes it,
  because the definition survives elsewhere. A *copy* there would be a second definition and
  a stop condition (one list per concept).

Rejected alternatives: option (a) (edit `.grok/` only) for the staleness reason above;
option (b) (project skill plus each EPIC group's `context.md`) because thirteen epic
contexts is thirteen copies, which is the same defect at larger scale — the epic contexts
should *reference* the shape, and `EPIC-013/context.md` already does that by pointing at the
project skill in its "Read before starting" list.

## Governing docs

The ticket carries `refs: []` and **`docs_todo: true`**.

> **New ADR** — ADR-0110 (agent-skill pinning and the invocation/review protocol), authored
> by [[TOOL-008]] (plan handle `DSK-12-08`) — and see the collision note there: ADR-0110 is
> also claimed by board [[FND-005]] (plan handle `DSK-00-05`). This plan is written to the
> decision as recorded in `docs/desktop/12-agent-tooling/README.md` § 6 (invocation and
> review protocols, ticket-metadata block) and to the mapping table at
> `docs/desktop/00-governance-and-workflow/README.md:264-278` § Ticket template. If the ADR
> lands differently this plan is revised before implementation.

Programme-level authorities this plan meets:

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-04 (locked) | "Every ticket names its subagent, skills, and MCP tools" | Step 5 (the five-bullet block) |
| Proposal §25 § 7 → `docs/desktop/00-governance-and-workflow/README.md:272` | "7 Agent skills → `plan/` **`## Routing` block — required**", shape `subagent · skills (pinned path) · MCP tools` | Step 5 |
| Proposal §20.5 step 6 / Appendix C → `docs/desktop/00-governance-and-workflow/README.md:275` | "10 Verification → `post-implementation-report` (Appendix C shape)" | Step 6 |
| `AGENTS.md` § Repository task workflow step 4 → `docs/desktop/00-governance-and-workflow/README.md:280-283` | The simplification pass is recorded under a dated heading in the plan | Step 8's sample check |
| L-05 (locked) | The board is seeded from these plans; the shape must fit tickets already on the board | Step 8 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (`sandbox_mode = "read-only"`, `model_reasoning_effort = "high"`). It audits the two
  sample documents against the required shape; it cannot write, so the owner transcribes.
- **Skills**, in load order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md`
  2. `kanmer-tickets` — `.grok/skills/kanmer-tickets/SKILL.md`
  3. `kanmer-docs` — `.grok/skills/kanmer-docs/SKILL.md`
  4. `kanmer-plan` — `.grok/skills/kanmer-plan/SKILL.md`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `get_ticket_doc`, `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates confirmed by
  `get_doc_gates TOOL-007`: `leave-preparing` needs `plan` + `questions-resolved`;
  `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates TOOL-007` before
  every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

Refines the body's 12 steps in the same order.

1. **Orientation.** Read `EPIC-013/context.md`, then the plan sections in the body's
   **Source of truth** — especially `docs/desktop/00-governance-and-workflow/README.md:264-286`
   § Ticket template, which is the authority for the mapping.
   `get_doc_gates TOOL-007`, then `take_ticket`. Confirm the two dependencies landed:
   board [[FND-003]] (plan handle `DSK-00-03`, board shape) and board [[FND-004]] (plan
   handle `DSK-00-04`, the seeded tickets) — step 8 needs real tickets to prove the shape on.
2. **Read both stock templates end to end** so the change is additive, not a rewrite:
   - `.grok/skills/kanmer-tickets/assets/ticket-template.md` — sections today: What, Why,
     Approach, Verification, Outcome. **No routing section.**
   - `.grok/skills/kanmer-execute/assets/post-implementation-report-template.md` — sections
     today: Summary, Changes (a File/Change/Why table), Governing docs, Risks / follow-ups,
     Verification hand-off. **No skills, no SHAs.**
   (Both verified 2026-08-24.)
3. **Copy the two verbatim artefacts into this plan first**, so they cannot be paraphrased
   later: (a) the `agent_skills:` / `routing:` YAML block from
   `docs/desktop/12-agent-tooling/README.md` § 6; (b) the seven Appendix C headings from
   `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` Appendix C — **Skills
   consulted**, **Applicable guidance**, **Project decisions taking precedence**,
   **Repository evidence**, **Implementation**, **Verification**, **Deviations**.
4. **Decide and record the durable home. Recommended: the project skill is the definition;
   the `.grok/` templates carry a pointer.** Write the choice under a dated heading here with
   the reasoning from **Approach** above. Then record the reconcile step that the pointer
   needs: after a Kanmer update, run `kanmer-setup` and re-apply the two pointer lines;
   `get_status` → `repo.stale` is what reports the drift. Because the pointer is one line and
   the definition lives elsewhere, losing it to a reinstall is an inconvenience, not a data
   loss — which is precisely why this option does **not** need an `open-questions` item,
   where option (a) would have.
5. **Define the `## Routing` block once**, in the project skill, as exactly five bullets:
   1. **Subagent** — name plus `.codex/agents/<name>.toml`
   2. **Skills** in load order, each with the pinned source path
      (`.agents/skills/vendor/<family>/<name>/` or `.grok/skills/<name>/SKILL.md`)
   3. **MCP** — server and exact tool names
   4. **Kanmer pipeline** for the ticket's profile, with the gates `get_doc_gates` reports
   5. **Reviewer** — `pegasus-desktop-reviewer` (an agent that did not implement)

   Fill it from `docs/desktop/12-agent-tooling/skill-routing.md`, **never from memory**. Say
   in the definition that a routing name which does not resolve to a real file is a defect,
   not a typo.
6. **Define the report addition** as one new section, `## Agent evidence`, with the seven
   Appendix C headings as sub-headings. It is **additive**: Summary, Changes, Governing docs,
   Risks / follow-ups and Verification hand-off all stay. Put the definition in the project
   skill beside § Evidence format (Appendix C), which already lists the same seven items in
   prose — reconcile the two so there is one wording, not two.
7. **Record the reconcile procedure** for the `.grok/` pointers (already drafted in step 4).
   Keep it beside the pointers themselves so whoever runs `kanmer-setup` finds it.
8. **Prove it on two real tickets.** Pick two existing `DSK` tickets, write or read their
   `plan/` document, and confirm each carries a filled `## Routing` block and a
   `## Simplification pass` heading. **Timing constraint worth knowing before you start:**
   the acceptance says a *dated* `## Simplification pass` heading, and the date only exists
   once the pass has actually been run over a branch diff — a plan written before
   implementation carries the heading with a "not yet run" placeholder. So either pick two
   tickets whose implementation has completed, or record explicitly that the samples show
   the heading present and un-dated and name the ticket that will date it. As of 2026-08-24
   the `agent-tooling` area already carries plan documents in the required shape (for
   example [[TOOL-002]] and [[TOOL-003]]), and board [[REL-001]] (plan handle `DSK-09-01`)
   carries one too — read them back with `get_ticket_doc` rather than writing new ones if
   they already satisfy the shape.
9. **Do not duplicate board [[FND-011]] (plan handle `DSK-00-11`).** That ticket owns the
   *enforcement* half — the reviewer checklist that asserts the two headings are present
   (its title is literally "Enforce the ticket template: every DSK plan document carries
   `## Routing` and a dated `## Simplification pass`"). This ticket owns the *template*
   half. Write that boundary sentence into this plan so neither ticket writes the other's
   change, and make **no** reviewer-checklist edit here.
10. **Have `pegasus-desktop-reviewer` read both sample documents** and report whether the
    routing names resolve to real files: every skill path must exist on disk and every
    subagent name must match a `.codex/agents/*.toml`. Transcribe its findings — it cannot
    write.
11. **Run the documentation gates.** `pwsh ./scripts/Test-DocumentationLinks.ps1` → expect
    `All relative Markdown links resolve (<n> files checked).` If any `.md` was added,
    `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD` →
    expect `Markdown placement passed`. `.grok` **and** `.agents/skills` are both allowed
    roots (`scripts/Test-MarkdownPlacement.ps1:31`), so edits in either pass. Note that
    `scripts/Test-DocumentationLinks.ps1:14` excludes `.agents` and `.grok` is **not**
    excluded — so a broken relative link added to a `.grok/` template *will* be caught,
    while one added to the project skill will not. Check the project-skill links by hand.
12. **Record the Appendix C evidence for this ticket itself**, using the new
    `## Agent evidence` section. It is the first report to use the shape, which is the
    cheapest possible proof that the shape works.

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states. Two sample ticket
documents read back through `get_ticket_doc`, and a grep showing the shape is defined once.
`proof` is a `command-log`.

1. `get_ticket_doc <first sample ticket id> plan` → a `plan` document containing `## Routing`
   with five filled bullets and a `## Simplification pass` heading.
2. `get_ticket_doc <second sample ticket id> plan` → the same two headings present.
3. `grep -rn '## Routing' .agents/skills/project/pegasus-desktop/SKILL.md .grok/skills/` →
   **one definition** (in the project skill) and at most pointer lines elsewhere.
4. `grep -c 'Skills consulted' <the chosen home>` → `1`.
5. `pwsh ./scripts/Test-DocumentationLinks.ps1` →
   `All relative Markdown links resolve (<n> files checked).`

## Risks / open questions

| Risk | Mitigation |
| --- | --- |
| `.grok/skills/` is machine-managed; a hand edit is reported stale by `get_status` and can be reinstalled over. | Step 4's recommendation puts the *definition* outside `.grok/` and only a one-line pointer inside it, so a reinstall costs a pointer, not the shape. This is also why no `open-questions` item is opened — the body's "Open question to carry" is conditional on choosing option (a), which this plan does not. |
| Two definitions of the Routing shape (a stop condition). | Verification item 3 is the check; the pointer wording must say "see" and not restate the five bullets. |
| Overlap with board [[FND-011]] — two agents writing the same reviewer-checklist change. | Step 9 states the boundary explicitly; this ticket makes no checklist edit. |
| The sample tickets cannot show a *dated* simplification pass before implementation. | Step 8 names the constraint and gives both acceptable outcomes; record which was used. |
| Editing inside `AGENTS.md`'s managed block (`:1-22`). | Not needed by this ticket; if a sentence is added there it goes at or after `AGENTS.md:24`. |
| Ticket-transient documents drifting into the repository tree. | Sample plans are written with `set_ticket_doc` into the Kanmer folder, never as `.md` files under `docs/`. |

Open questions: **none opened.** The body's conditional open question ("whether a Kanmer
upgrade reinstalls `.grok/skills/` in place or only reports drift") is made moot by the
recommended option in step 4 — with the definition outside `.grok/`, the answer no longer
changes any decision. If the implementer overrides the recommendation and chooses option
(a), they must open it as a blocking `open-questions` item at that point, as the body
requires.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch edits only
Markdown playbooks and one plan document; if that stays true the honest record is
`n/a — docs-only`, but state it explicitly with the date rather than omitting the heading —
this ticket is the one teaching every other ticket to carry it._
