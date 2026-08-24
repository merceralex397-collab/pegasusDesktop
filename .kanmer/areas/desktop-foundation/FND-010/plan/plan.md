# Plan — FND-010: Record decided D-001 in ADR-0100 and `docs/operations.md`, and agree the upstream freeze

**Diff estimate: ~3 files, ~30 lines** (a 4th file and ~2 more lines only if step 8's
fallback fires). `docs/engineering.md` § plan sizing requires the estimate first, and this
profile is `chore` — it owes no `research` or `files` document, so the measured inventory
below carries the surface area alone.

## Measured file-and-line inventory

Every current value below was measured at `bbd1c549` on 2026-08-24 with the command shown.

| Path | Current size | What this ticket does to it | Est. lines |
| --- | --- | --- | --- |
| `docs/adr/0100-native-winui-3-client-in-the-fork.md` | **does not exist** (`ls docs/adr/` returns `0001…0029` and `README.md`; no `01xx` file) | Text handed to the ADR's author for `## Consequences`; **zero diff on this branch** on the cheap path | 0 (or ~12 if this branch authors it) |
| `docs/operations.md` | 920 lines (`wc -l`); `## Production environment` at `:280`; the `- **Deployed evidence:**` bullet runs `:295-299`; the release-history prose is an indented continuation of that bullet from `:301`; the release table header is `:311` | New `### Release source of truth` subsection inserted after `:299` and **before** the `:301` continuation | ~14 |
| `docs/desktop/README.md` | 142 lines (`wc -l`); the D-001 row is `:47` | That one row rewritten to point at the recorded location instead of carrying the decision | 1 replaced |
| `docs/open-decisions.md` | 35,922 bytes; `## Azure ownership and retirement targets` at `:333` | **Conditional only** — one line if the freeze date cannot be agreed (step 8) | 0 or ~2 |

The `:301` boundary matters: the release-history prose is *inside* the `- **Deployed
evidence:**` bullet, not a sibling of it. A new `###` heading dropped at `:301` would break
that bullet in two. Insert after `:299` (the blank line at `:300` stays).

## Approach

Write the D-001 consequence text **once**, then place the same words in two destinations with
different authority: ADR-0100 `## Consequences` (the durable decision record) and
`docs/operations.md` (the current-state record of what is deployed and from where). The
ADR half is delivered by **hand-off, not by editing**: ADR-0100 does not exist yet, its
`## Consequences` section is written by whichever of [[FND-005]] (plan handle `DSK-00-05`) or
[[FND-026]] (plan handle `DSK-02-01`) reaches `docs/adr/0100-native-winui-3-client-in-the-fork.md`
first, and `AGENTS.md:81-90` makes an ADR body immutable once published. The rejected
alternative was to let this ticket author or edit ADR-0100 itself — rejected because it either
races the two claimants on one filename or, if ADR-0100 has already flipped to `accepted`,
forces a superseding ADR for what is a consequence note, not a new decision.

The freeze itself is **not** performed here. This ticket records a decision and captures an
agreement; the archive/read-only action lives in the upstream repository and belongs to its
owners.

## Governing docs

The ticket's `refs` is empty and it carries `docs_todo: true` — confirmed in
`get_doc_gates FND-010` (`"refs": []`, `"docs_todo": true`). No governing document exists to
meet yet, so the New-ADR paragraph and the authority table below both apply.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client in the fork), authored by
> [[FND-005]] (plan handle `DSK-00-05`); see [[FND-005]]'s plan for the ownership
> reconciliation with its co-claimant [[FND-026]] (plan handle `DSK-02-01`). Both tickets
> resolve onto the single filename `docs/adr/0100-native-winui-3-client-in-the-fork.md`.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md:212-224` (§ 3, "D-001 (decided
> 2026-08-23) — release source of truth after Phase 2"); if ADR-0100 lands differently this
> plan is revised before implementation.

Programme-level authorities that bind today, with the step that satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| **D-001** (`docs/desktop/README.md:47`; detail at `docs/desktop/00-governance-and-workflow/README.md:212-224`) | Option A: at the first production gateway change the fork becomes the single release source for gateway, worker and desktop; upstream is merged once more, then frozen | Steps 3–5 |
| Proposal § 6.3 (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:360-370`) | "no permanent second Pegasus repository is created" | Steps 3, 7 |
| **L-01** (`docs/desktop/README.md` § Locked decisions) | The gateway is `Pegasus.Web` evolved in place — which is *why* the first `/api/v1` change is a production gateway change and therefore the D-001 trigger | Step 3 (trigger wording) |
| **C-01** (`docs/desktop/README.md` § Constraints) | The repositories become private on completion — a second public release line is not kept alive | Step 3 (rationale sentence) |
| `AGENTS.md:81-90` § ADR conventions | ADR bodies are immutable once published; supersession is by a new ADR; the conversion uses the reserved block ADR-0100–ADR-0110 | Step 2 (branch check), step 4 |
| `docs/index.md:17` § question→file table | `docs/operations.md` owns "What is deployed, released, monitored, or recovery-proved now?" | Step 5 |
| `docs/index.md:30-39` § Authority | Current-state documents outrank working rules and plans, so the plan set must stop being the carrier once the ADR and operations record exist | Step 9 |
| Plan 00 § 8 (`docs/desktop/00-governance-and-workflow/README.md:430`) | `docs/operations.md` changes only when a deployment changes — "gateway releases from the fork after D-001" is exactly that case | Step 5 |

## Routing

Copied from the ticket body's `## Routing` block
(`docs/desktop/00-governance-and-workflow/README.md` § 3 "Ticket template" row 7 makes this
block mandatory in the plan document specifically).

- **Subagent**: — (parent session; the operator performs the upstream conversation)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `link_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-tickets` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates FND-010` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5, `AGENTS.md:298-305`)

## Steps

These refine the ticket body's implementation steps — same order, same ownership, same file
paths.

1. **Orient.** Read `docs/desktop/00-governance-and-workflow/README.md:212-224` (the D-001
   paragraph, 13 lines) in full, `docs/desktop/README.md:47` (the D-001 row), and
   `AGENTS.md:77-118` § ADR conventions. Call `get_doc_gates FND-010` — it currently reports
   `leave-preparing` needing `plan` and `enter-done` needing `proof`, with
   `questions-resolved` already satisfied — then `take_ticket` onto `task/<slug>` in
   `../pegasus-worktrees/<slug>` from `origin/dev`.
2. **Decide the ADR path before writing anything.** Run
   `ls docs/adr/ | grep -E '^0100'` on this branch and on the branches of [[FND-005]] and
   [[FND-026]] (`git branch -a --list 'task/*'`). Three outcomes, and the plan records which
   one happened:
   - **File absent everywhere** → hand the step-3 text to whichever ticket is taken first;
     this branch writes nothing under `docs/adr/`.
   - **File exists, frontmatter `status: proposed`** → hand the text to that branch's author
     for `## Consequences` *before* the acceptance flip. Still no edit from here.
   - **File exists, frontmatter `status: accepted`** → the body is immutable
     (`AGENTS.md:81-83`). Record D-001 in `docs/operations.md` only, and raise a superseding
     ADR in the reserved block **only if** the decision genuinely changes an accepted
     decision — a consequence note does not.
3. **Draft the text once.** One block, reused verbatim in both destinations, carrying all six
   elements the body requires: the decision (Option A); its date (2026-08-23); the trigger
   (the first production gateway change — the compatibility endpoint and staff token flow of
   area 04, per `docs/desktop/00-governance-and-workflow/README.md:213-215`); what becomes
   true (the fork is the single release source for gateway, worker and desktop); what happens
   upstream (`collisionengineers/pegasus` merged in one final time, then frozen — read-only or
   archived); and the rejected alternative (merging fork gateway changes back upstream per
   release, rejected for double CI/review cost and two current-state documents). Add one
   clause naming C-01 as a supporting reason. Keep it free of dated cost tables and prices —
   `AGENTS.md:111-114` bars those from an ADR.
4. **Place it in ADR-0100 `## Consequences`** by the path chosen in step 2. On the hand-off
   paths this is a message to the other author plus a note in this ticket's proof, not a diff.
5. **Place it in `docs/operations.md`** as a new `### Release source of truth` subsection
   inserted after `:299` and before the `:301` bullet continuation. Phrase it as current state
   plus the trigger: *today* the gateway releases from `collisionengineers/pegasus` by the
   authorised-terminal route described at `:301-309`; *at the trigger* the fork becomes the
   single release source. Do not touch the release table at `:311` and do not touch the known
   drift at `:295` ("the estate currently serves **release 14**" while the table's newest row
   is release 20) — that line is owned by [[FND-023]] (plan handle `DSK-01-10`) or a separate
   one-line doc ticket (`docs/desktop/01-inventory-and-parity/README.md` § 8).
6. **Record the sync stop condition in the same subsection**: the one-way `upstream` sync of
   [[FND-002]] (plan handle `DSK-00-02`) repeats after each upstream release **until** the
   freeze and stops on the freeze date. Name [[FND-051]] (plan handle `DSK-01-13`, standing
   later upstream syncs up to the D-001 freeze) as the ticket that runs it in the meantime.
7. **Operator step — agree the freeze.** Hand the operator three questions for the owners of
   `collisionengineers/pegasus`: the date; whether the repository is archived or made
   read-only; and who performs it. Evidence to hand back is the agreed date and mechanism *in
   writing*, pasted into the ticket proof and into the step-5 subsection.
8. **If no date is agreed, do not invent one.** Add one line to `docs/open-decisions.md` —
   under `## Azure ownership and retirement targets` (`:333`) if the operator agrees it fits,
   otherwise a new `## Upstream repository freeze` heading — stating that the D-001 freeze
   date is pending with the upstream owners, and that the sync of [[FND-002]] keeps running
   until it is set. Leave the `docs/operations.md` subsection saying the date is pending with
   an explicit pointer to that line; a current-state document must never carry a speculative
   date.
9. **Retire the plan set as the carrier.** Rewrite `docs/desktop/README.md:47` so the D-001
   row's Status cell reads that the decision is recorded in ADR-0100 § Consequences and
   `docs/operations.md` § Release source of truth, rather than restating the decision. Keep
   the row; only the Status cell changes.
10. **Gate and PR.** Run `pwsh ./scripts/Test-DocumentationLinks.ps1` (takes no parameters) —
    exits 0. Run the simplification pass over this branch's own diff and record
    `n/a — docs-only` under a dated `## Simplification pass` heading in this plan
    (`AGENTS.md:289-297`). Open the PR against `dev`; merge after the independent
    `pegasus-desktop-reviewer` review.
11. **Write `proof`** as a `command-log`: the two file excerpts showing the identical decision
    text, the `grep -n 'D-001' docs/adr/0100-*.md docs/operations.md` output, the agreed
    freeze date and mechanism (or the `docs/open-decisions.md` line standing in for it), and
    the name of the ticket that authored ADR-0100 — [[FND-005]] or [[FND-026]].

## Verification

Evidence tier from the ticket body: **Tier 1 — Static/build/architecture**. Documentation
consistency plus a recorded external agreement; nothing here is proved by code. `proof` is a
`command-log`.

| Command | Expected |
| --- | --- |
| `grep -n 'D-001' docs/adr/0100-*.md docs/operations.md` | The decision text present in both. On the hand-off path the ADR hit lands on the co-claimant's branch — quote it from there and say so. |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |
| `grep -n 'freeze' docs/operations.md` | The agreed date and mechanism, **or** an explicit pointer to the `docs/open-decisions.md` line |
| `git diff --stat docs/adr/` | empty on the hand-off paths — this branch must not edit an accepted ADR body |
| `sed -n '295,302p' docs/operations.md` | the `- **Deployed evidence:**` bullet still reads as one bullet; the new `###` heading sits after it, not inside it |

The observable behaviour to check by eye: a reader who opens only `docs/operations.md` learns
where the gateway is released from today, what changes at the trigger, and when the upstream
sync stops — without opening the plan set.

## Risks / open questions

- **Risk: ADR-0100 flips to `accepted` before the text is handed over.** Mitigation: step 2
  checks the frontmatter on both claimant branches before writing, and step 10 keeps the PR
  small enough to land inside either claimant's window. If it has flipped, step 2's third
  outcome applies — `docs/operations.md` only, and no superseding ADR for a consequence note.
- **Risk: two claimants author one filename.** Mitigation: the multi-claimant form above —
  ownership is reconciled in [[FND-005]]'s plan, not asserted here.
- **Risk: a `###` heading inserted at `:301` splits the `- **Deployed evidence:**` bullet.**
  Mitigation: the measured boundary in the inventory table, and the `sed -n '295,302p'` check
  in Verification.
- **Open question — the freeze date — is answered by the operator**, in conversation with the
  owners of `collisionengineers/pegasus` (step 7). It is deliberately **not** an
  `open-questions/` item: the body's `## Guardrails` § Unresolved routes it to
  `docs/open-decisions.md`, an unticked box here would block `leave-preparing` and stop the
  ticket ever reaching the implementation step that asks the question, and step 8 already
  gives the ticket a defined way to close without an answer.
- **Scope boundary, not an open question**: the drift at `docs/operations.md:295` belongs to
  [[FND-023]] (plan handle `DSK-01-10`) / the area 01 § 8 doc ticket, not here.
- **Scope boundary, not an open question**: which of [[FND-005]] or [[FND-026]] authors
  ADR-0100 is settled by whichever is worked first; [[FND-005]]'s plan owns the
  reconciliation.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 (`AGENTS.md:289-297`) requires a
pass over this branch's own diff before the PR, recorded here under a dated heading. Record
`n/a — docs-only` for this documentation-only branch._
