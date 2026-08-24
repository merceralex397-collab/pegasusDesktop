# Plan — FND-013: Record in ADR-0100 that the proposal's three "prior documents" are not in the repository and are not an input

**Diff estimate: ~1 file, ~4 lines** — and on the expected hand-off path (step 3, outcome A
or B) **~0 files and ~0 lines in *this* branch's diff**, because the paragraph lands inside
ADR-0100's authoring PR rather than in a PR of its own. `docs/engineering.md` § plan sizing
requires the estimate first. This profile is `chore` — it owes no `research` or `files`
document, so the measured inventory below carries the surface area alone.

## Measured file-and-line inventory

Every current value was measured at `bbd1c549` on 2026-08-24 with the command shown.

| Path | Current size and the exact anchor | What this ticket does to it | Est. lines |
| --- | --- | --- | --- |
| `docs/adr/0100-native-winui3-desktop-client.md` | **does not exist.** `ls docs/adr/*.md \| wc -l` → `29`; `ls docs/adr/` returns `0001…0029` (0017 never issued: `ls docs/adr/ \| grep -c '^0017'` → `0`) plus `README.md`. There is no `01xx` file. | One paragraph supplied to the ADR's author for `## Context`; **zero diff on this branch** on the hand-off path | 0 here (~4 inside the authoring ticket's PR) |
| `docs/index.md` | 59 lines (`wc -l`). `## Authority` at `:30`; its paragraph runs `:32-39`; `## New Markdown files` at `:41`. The paragraph names no prior documents at all today. | **Conditional only** — step 3 outcome C (ADR-0100 already `accepted`): one sentence appended to the `:32-39` paragraph | 0 or ~3 |
| `docs/desktop/00-governance-and-workflow/README.md` | 431 lines (`wc -l`). The § 3 authority-order paragraph is `:131-139` and **already states the position** verbatim: "The proposal's three 'prior documents' (§2 item 5) are **not** in the repository; they are not an input to any ticket." (`grep -n 'prior document' docs/desktop/`) | **Read, not edited.** Step 6 cross-checks that the plan paragraph and the ADR sentence agree — they already do, so no edit is expected | 0 |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | 2 citation sites: § 2 item 5 at `:46-49`, and Appendix D — Research basis at `:2239-2241` | **Never edited** — the Guardrails forbid rewriting the proposal | 0 |
| ticket `proof` document | Kanmer document, not a repository file | The quoted ADR paragraph plus the step-2 search outputs | 0 repo lines |

Two measured facts that change how step 2 must be run:

- **`git ls-files | grep -i -E 'desktop-conversion-plan|desktop.azure.conversion|recommended.desktop.api'` returns nothing** (exit status `1`, no output). The premise holds: none of the three documents is tracked in this repository.
- **`grep -ril 'Recommended desktop API architecture' docs/ reference/` returns ONE hit, not zero** — `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md`. That hit is the proposal *citing* the title at `:49` and `:2241`; it is not the document. The body's step 2 says "expected: no results", which is right about the premise and wrong about this command's output. The narrowed command that answers the body's actual question is
  `grep -ril 'Recommended desktop API architecture' docs/ reference/ | grep -v 'Pegasus_Native_Desktop_Design_Proposal.md'` → **empty**. `reference/` exists (`ls -d reference`), so the path is not a silent no-op.

Reading the single proposal hit as "the document was found" would trip the body's stop condition
("If any of the three documents *is* found, stop") and kill a correct ticket. That is why the
narrowed command is in Verification below.

## Approach

Deliver the sentence by **hand-off, not by editing**: ADR-0100 does not exist, its body is
written by whichever of [[FND-005]] (plan handle `DSK-00-05`) or [[FND-026]] (plan handle
`DSK-02-01`) reaches `docs/adr/0100-native-winui3-desktop-client.md` first, and `AGENTS.md:77-91`
makes an ADR body immutable once published. So this ticket verifies the premise, writes one
paragraph, and gives it to that author before the `status: proposed → accepted` flip. The
rejected alternative was to create a small ADR of its own for the note — rejected because
`AGENTS.md:91-92` requires one *decision* per ADR and this is a scope note inside ADR-0100's
`## Context`, not a decision; a second rejected alternative was editing ADR-0100 after
acceptance, which the immutability rule forbids outright.

The fallback (`docs/index.md` § Authority) exists only for the case where the flip already
happened. It is deliberately the *weaker* home — `docs/index.md` restates the authority chain
but is a working index, not a decision record — and the plan records why it was used.

## Governing docs

The ticket's `refs` is empty and it carries `docs_todo: true` — confirmed in
`get_doc_gates FND-013` (`"refs": []`, `"docs_todo": true`). No governing document exists to
meet yet, so the New-ADR paragraph and the authority table below both apply.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client in the fork, converted inside this
> fork, no WebView shell), authored by [[FND-005]] (plan handle `DSK-00-05`); ADR-0100 is
> co-claimed with [[FND-026]] (plan handle `DSK-02-01`), so see [[FND-005]]'s plan for the
> ownership reconciliation rather than assuming a single author. Both tickets resolve onto the
> one filename `docs/adr/0100-native-winui3-desktop-client.md`.
> This plan is written to the position as recorded in
> `docs/desktop/00-governance-and-workflow/README.md:131-139` (§ 3, authority order); if
> ADR-0100 lands a different authority order this plan is revised before implementation.

Programme-level authorities that bind today, with the step that satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 2 item 5 (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:46-49`) | The three prior documents sit fourth in the authority order, above generic skill guidance | Steps 2–4 (the reference is closed, not deleted) |
| Proposal § 2 closing paragraph (`:51`) | "The earlier plans are useful research, not constraints" | Step 4's wording — not an input, positions reconciled in § 3 |
| Proposal § 3 (`:91-107`) | Reconciliation of the earlier desktop proposals — the substantive positions are already carried forward in the decision table | Step 4's final clause ("Their substantive positions are reconciled in proposal §3") |
| Proposal Appendix D (`:2233-2247`) | Research basis lists the same three titles | Step 6 (both citation sites checked; neither edited) |
| Plan 00 § 3 (`docs/desktop/00-governance-and-workflow/README.md:131-139`) | The position is stated in the plan set — but a plan is not authority | Steps 4, 6, and 9 (the ADR carries the authority, the plan agrees) |
| `AGENTS.md:77-91` § ADR conventions | Stable IDs; the conversion uses the reserved block ADR-0100–ADR-0110; published bodies are immutable and are superseded, never edited | Steps 3, 5 |
| `AGENTS.md:91-92` § ADR conventions | One decision per ADR | Step 5 (no new ADR for a scope note) |
| `docs/index.md:30-39` § Authority + `docs/index.md:41-55` § New Markdown files | The repository's own authority order contains no such documents; a new repository `.md` is only a PRD, FRD or ADR | Step 3's fallback, and the refusal to create a new file |
| `docs/adr/README.md:9-14` | The index is derived from frontmatter; published bodies are immutable | Step 3's frontmatter check |

## Routing

Copied from the ticket body's `## Routing` block
(`docs/desktop/00-governance-and-workflow/README.md:272` § 3 "Ticket template" row 7 makes this
block mandatory in the plan document specifically).

- **Subagent**: — (parent session)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-tickets` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates FND-013` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's nine implementation steps — same order, same ownership, same
file paths.

1. **Orient.** Read proposal `:38-51` (§ 2 authority order, including item 5 at `:46-49`) and
   `:91-107` (§ 3 reconciliation table), then
   `docs/desktop/00-governance-and-workflow/README.md:131-139`, then `AGENTS.md:77-92`.
   Call `get_doc_gates FND-013` — it currently reports `leave-preparing` needing `plan` and
   `enter-done` needing `proof`, with `questions-resolved` already satisfied — then
   `take_ticket` onto `task/<slug>` in `../pegasus-worktrees/<slug>` from `origin/dev`.
2. **Verify the premise, and read the second command correctly.** Run both:
   - `git ls-files | grep -i -E 'desktop-conversion-plan|desktop.azure.conversion|recommended.desktop.api'` — expected **no output, exit 1**.
   - `grep -ril 'Recommended desktop API architecture' docs/ reference/` — expected **exactly one path**, `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md`, which is the proposal citing the title at `:49` and `:2241`. Confirm that is the only hit by re-running it with `| grep -v 'Pegasus_Native_Desktop_Design_Proposal.md'` and getting nothing.
   The body's stop condition ("if any of the three documents *is* found, stop") fires only on a
   hit that is a *document*, never on the proposal's own citation. Record both outputs verbatim.
3. **Decide the destination before writing anything.** Run `ls docs/adr/ | grep -E '^0100'` on
   this branch and on the branches of [[FND-005]] and [[FND-026]]
   (`git branch -a --list 'task/*'`). Three outcomes; the proof records which one happened:
   - **A — file absent everywhere** → hand the step-4 paragraph to whichever ticket is taken
     first; this branch writes nothing under `docs/adr/` and opens no PR of its own.
   - **B — file exists with frontmatter `status: proposed`** → hand the paragraph to that
     branch's author for `## Context` *before* the acceptance flip. Still no edit from here.
   - **C — file exists with frontmatter `status: accepted`** → the body is immutable
     (`AGENTS.md:81-83`). Append the sentence to `docs/index.md`'s `## Authority` paragraph
     (`:32-39`) instead, and record in this ticket's proof why ADR-0100 could not carry it.
     Do **not** raise a superseding ADR: a scope note is not a changed decision.
4. **Write the paragraph once**, in the exact form the body specifies, for ADR-0100's
   `## Context` (the authority paragraph):

   > The proposal's authority order cites three prior documents — *Pegasus Desktop Conversion
   > Plan*, *Desktop Azure Conversion Plan*, *Recommended desktop API architecture*. They are
   > not present in this repository and are not retrievable; they are therefore not an input to
   > any conversion ticket. Their substantive positions are reconciled in proposal §3.

   Keep it to that one paragraph and keep it free of dated cost tables, prices and runbook
   detail (`AGENTS.md:111-114`).
5. **Create no new ADR.** `AGENTS.md:91-92` requires one decision per ADR and this is a scope
   note inside ADR-0100, not a decision of its own. Do not take a number from the reserved
   block ADR-0100–ADR-0110 for it.
6. **Cross-check that the reference is closed everywhere an agent might look.** Run
   `grep -rn 'prior document' docs/desktop/ docs/adr/`. Today that returns three hits — the
   area 00 § 3 paragraph at `:135`, its own work-breakdown row at `:376`, and the proposal at
   `:46`. After step 4 there must be a fourth in `docs/adr/0100-*.md`, and the plan paragraph
   and the ADR sentence must say the same thing, with the ADR carrying the authority. The
   proposal's two citation sites (`:46-49`, `:2239-2241`) are read and left untouched.
7. **Run the documentation gate.** `pwsh ./scripts/Test-DocumentationLinks.ps1` — it takes no
   parameters (`param()` at `scripts/Test-DocumentationLinks.ps1:9`) and must exit 0.
8. **PR or fold.** On outcome A or B the change belongs in the authoring ticket's PR — record
   in this ticket which one ([[FND-005]] or [[FND-026]]) and link it. On outcome C open a PR
   against `dev` for the one-sentence `docs/index.md` change. Either way take the independent
   `pegasus-desktop-reviewer` review and record `n/a — docs-only` under a dated
   `## Simplification pass` heading in this plan (`AGENTS.md` § Repository task workflow
   step 4).
9. **Write `proof`** as a `command-log`: the two step-2 search outputs (including the single
   proposal hit and its explanation), the quoted ADR paragraph as it landed, the
   `grep -rn 'prior document'` output showing plan and ADR agreeing, the name of the ticket
   that authored ADR-0100, and the step-3 outcome letter.

## Verification

Evidence tier from the ticket body: **Tier 1 — Static/build/architecture**. A recorded scope
note and a negative search result; nothing here is proved by code. `proof` is a `command-log`.

| Command | Expected |
| --- | --- |
| `git ls-files \| grep -i -E 'desktop-conversion-plan\|desktop.azure.conversion\|recommended.desktop.api'` | no output, exit 1 |
| `grep -ril 'Recommended desktop API architecture' docs/ reference/` | exactly one path — `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md`. Record it as the proposal's own citation (`:49`, `:2241`), **not** as a found document |
| `grep -ril 'Recommended desktop API architecture' docs/ reference/ \| grep -v 'Pegasus_Native_Desktop_Design_Proposal.md'` | no output — this is the command that actually answers "does the document exist?" |
| `grep -n 'prior document' docs/adr/0100-*.md` | the recorded paragraph. On outcomes A and B the hit lands on the co-claimant's branch — quote it from there and say so |
| `grep -rn 'prior document' docs/desktop/ docs/adr/` | the area 00 paragraph (`:135`), its work-breakdown row (`:376`), the proposal (`:46`) and the new ADR hit — plan and ADR saying the same thing |
| `git diff --stat docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | empty — the proposal is the recorded design target and is never rewritten by a ticket |
| `git diff --name-only \| grep '^docs/adr/'` | empty on outcome C — this branch must not edit an accepted ADR body |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |

The observable outcome to check by eye: an agent that follows proposal § 2's authority order
down to item 5 now finds, in ADR-0100 itself, a statement that the item is closed — and does
not stall or invent the documents' contents.

## Risks / open questions

- **Risk: the step-2 `grep` hit on the proposal is read as "the document exists" and the
  ticket is stopped.** Mitigation: the measured inventory records the single expected hit with
  its two line numbers, and Verification carries the narrowed command that returns empty. This
  is a body-command imprecision, not a scope change — the body's intent (the three documents
  are not in the repository) is confirmed.
- **Risk: ADR-0100 flips to `accepted` before the paragraph is handed over.** Mitigation:
  step 3 checks the frontmatter on both claimant branches before writing, and outcome C gives
  the ticket a defined way to close without editing an immutable body.
- **Risk: the note is turned into its own ADR** because it "feels like" a decision.
  Mitigation: step 5 forbids it and cites `AGENTS.md:91-92`; the Verification table checks
  `docs/adr/` for exactly the ADR-0100 hit.
- **Scope boundary, not an open question**: which of [[FND-005]] or [[FND-026]] authors
  ADR-0100 is settled by whichever is worked first; [[FND-005]]'s plan owns the reconciliation.
- **Scope boundary, not an open question**: the proposal's own text (both citation sites) is
  not edited by this or any ticket — it is the recorded design target.
- No `open-questions` document is opened. The ticket body does not instruct one; the premise is
  verifiable from the repository (and was verified above); and the only branching decision
  (step 3's three outcomes) is fully specified in the body, so nothing is left for someone else
  to answer.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's
own diff before the PR, recorded here under a dated heading. Record `n/a — docs-only` for this
documentation-only branch — and on step 3's outcome A or B, where this branch has no diff of
its own, record `n/a — docs-only; change folded into <authoring ticket>'s PR`._
