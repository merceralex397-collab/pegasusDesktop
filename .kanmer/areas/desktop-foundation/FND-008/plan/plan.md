# Plan — FND-008: Write FRD-13, update the PRD scope and add the `DSK` capability family

**Diff estimate: ~5 files, ~200 lines.**

`docs/engineering.md:201-207` § Plan sizing requires the estimate first, derived
from the `files` document. FRD-13 at the measured house length (FRD-12 is 131
lines, FRD-11 196) → ~150; one row in `docs/frd/README.md`; one row in
`docs/index.md`; ~8 lines in `docs/prd/pegasus-product.md`; and in
`docs/capabilities.md` the `DSK` rows plus a note row (~10 lines) with three
recomputed totals touching ~6 more. Five files, ~200 lines.

## Approach

Draw the FRD-12 / FRD-13 boundary first and write it down, then author against
it. The boundary is the whole risk in this ticket: FRD-12 already specifies an
operator experience in twenty-odd normative bullets
(`docs/frd/frd-12-operator-experience.md:4-20`), and overlapping normative text
in two FRDs is a defect — two owners for one rule, with no way to tell which
governs. The rule adopted here is **web until cutover stays FRD-12; the desktop
experience is FRD-13**, and anything genuinely shared stays in FRD-12 and is
*cited* by FRD-13 rather than restated. The rejected alternative was extending
FRD-12 to cover both clients: it would put the retiring surface and the
replacing one under one document and make the post-cutover cleanup a rewrite
instead of a deletion.

Everything lands in one PR because `docs/capabilities.md`'s `Canonical owner`
column is the join that makes a new family meaningful. Adding `DSK` rows whose
owner is an unmerged FRD would fail the link gate; splitting the PRD change out
would leave a family with no product scope behind it.

## Governing docs

`refs` is empty and `docs_todo: true` — confirm with `get_doc_gates FND-008`,
which for profile `feature` shows `leave-backlog: [governing-doc]` satisfied by
`docs_todo`, and `leave-preparing: [research, files, plan, checklist,
questions-resolved]`.

> **New FRD — this ticket authors it.** FRD-13 "Desktop operator experience",
> plus the PRD scope change and the `DSK` capability family. It cites, and does
> not author, the conversion ADRs: **ADR-0100** and **ADR-0104**, authored by
> [[FND-005]] (plan handle `DSK-00-05`) and co-claimed by [[FND-026]] (plan
> handle `DSK-02-01`) — see [[FND-005]]'s plan for the ownership reconciliation;
> **ADR-0102**, authored by [[FND-006]] (plan handle `DSK-00-06`) and co-claimed
> by [[FND-042]] (plan handle `DSK-04-01`) — see [[FND-006]]'s plan;
> **ADR-0105**, which has three claimants ([[FND-005]], [[REL-001]],
> [[FND-042]]) — see [[FND-005]]'s plan; and **ADR-0108**, authored `proposed`
> by [[FND-007]] (plan handle `DSK-00-07`) and accepted later by [[FEAT-038]]
> (plan handle `DSK-07-12`).
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 4 Target state and § 8;
> if an ADR lands differently this plan is revised before implementation.

Because `refs` is empty, the authorities that bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/index.md:32-39` § Authority | operator-notes > PRD > FRD > capabilities > ADRs > current state > working rules | The whole ticket: it puts the desktop into that chain for the first time |
| `docs/frd/README.md:4-8` | An FRD specifies behaviour; it "never invents product scope or records a technical decision" | Steps 4–5 (behaviour and citations only); step 9 puts scope in the PRD |
| `docs/frd/README.md:35-58` | The FRD template | Step 3 |
| `docs/frd/README.md:11-12` | Each FRD is owned by one or more capability IDs; the join is the `Canonical owner` column | Steps 6–7 |
| `docs/design/README.md` + `AGENTS.md` § Simplicity rails | Binding UI authority; a field is a label and a control; operator-facing explanation is a defect | Step 4 (cited, never restated) |
| `docs/capabilities.md:69-71` | The `FAMILY-NN` registry and its six-column table | Step 7 |
| `docs/capabilities.md:29-54` | Three derived totals that must reconcile | Step 8 |
| Proposal § 26 Documentation set; § 14; § 27 | The product/UI documentation set and the acceptance criteria FRD-13 makes checkable | Steps 3–5 |
| Plan 00 § 4 Target state | "FRD-13 and the PRD update are merged; `docs/capabilities.md` carries the `DSK` family" | The whole ticket |
| L-03 | Report-rendering behaviour is owned by ADR-0108 | Step 5 (referenced, marked `proposed`) |
| D-001 | The fork becomes the single release source — which makes a PRD scope change in **this** repository the right place to record web retirement | Step 9 |
| D-004 (operator, 2026-08-24) | `OPS-10` acceptance folds into the desktop pilot approval; the row's note change belongs to [[REL-016]] | Step 8's guardrail: the `OPS-10` row is not edited here |
| `scripts/Test-MarkdownPlacement.ps1:31` + `.github/workflows/ci.yml:71-87` | Placement and link gates on every change set | Step 11 |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § "Ticket template" requires
of the plan document specifically.

- **Subagent**: — (parent session).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `link_doc`, `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-008` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's thirteen implementation steps; order, ownership and file
paths are the body's.

1. **Orient.** Read `docs/frd/README.md` in full (58 lines: definition at
   `:1-12`, documents table at `:16-32`, template at `:35-58`),
   `docs/frd/frd-12-operator-experience.md`, `docs/design/README.md`, and
   `docs/desktop/06-ui-design/README.md`. Call `get_doc_gates FND-008`, then
   `take_ticket`. Confirm [[FND-005]] has merged — FRD-13 cites ADR files by
   relative path and `Test-DocumentationLinks.ps1` fails on a path that does not
   exist.
2. **Draw the FRD-12 / FRD-13 boundary and write it into this plan** before
   writing any behaviour. FRD-12 keeps the **web** operator experience until
   cutover — its dashboard, queues, the intake-evidence filters `All` /
   `Instructions` / `Images`, list/detail journeys, supporting-detail navigation
   and administration (`frd-12:4-20`). FRD-13 owns the **desktop** operator
   experience: shell and navigation, session and first run, keyboard completion,
   accessibility baseline, error and empty states, update-required behaviour.
   Anything genuinely shared and normative stays in FRD-12 and FRD-13 **cites**
   it. Overlapping normative text in two FRDs is a defect.
3. **Create `docs/frd/frd-13-desktop-operator-experience.md` using the template
   at `docs/frd/README.md:35-58` verbatim** — the
   `> Owner capabilities: … · Source PRD: … · Design: docs/design/README.md#…`
   line, then `## Purpose`, `## Behaviour`, `## States and transitions`,
   `## Edge cases and fail-closed behaviour`, `## Acceptance evidence`,
   `## Links`.
   **Recorded divergence, flagged for the reviewer:** no existing FRD follows
   this template. Measured `##` heading counts across the twelve files are 2, 1,
   2, 1, 1, 2, 1, 2, 2, 3, 1, 1 — each uses a single domain heading — and the
   house owner line at `frd-11:2` and `frd-12:2` reads
   `· UI behaviour: docs/design/README.md` (a bare path, not a link) rather than
   the template's `· Design: …#anchor`. The body instructs the template, so the
   template is used, and FRD-13 becomes the first conformant FRD. Say so in the
   PR description so the reviewer sees a decision rather than an inconsistency.
   Note that a bare path is not a link and a `#anchor` is never validated
   (`scripts/Test-DocumentationLinks.ps1:39-40` strips the fragment), so either
   form passes the gate — this is a house-style call, not a gate risk.
4. **Write `## Behaviour` as normative rules** with "must" / "never" / "fails
   closed". For example: an unsupported client version must not proceed
   (ADR-0105); every critical workflow must be completable from the keyboard; a
   field is a label and a control, and operator-facing explanation is a defect
   (`docs/design/README.md`, `AGENTS.md` § Simplicity rails); no colour-only
   state. Cite `docs/desktop/06-ui-design/screen-specs.md` for per-screen detail
   instead of copying it — copying makes two owners for one rule.
5. **Cite accepted ADRs, not the plan set**: ADR-0100 (native client), ADR-0102
   (session), ADR-0104 (online-required), ADR-0105 (minimum version), ADR-0108
   (report rendering) — and **mark ADR-0108 as still `proposed`**, because a
   `proposed` ADR is not settled authority and its acceptance flip belongs to
   [[FEAT-038]]. Every citation is a relative path to a file that has merged.
6. **Add the FRD-13 row** to the `## Documents` table in `docs/frd/README.md`
   (`:16-32`), after the FRD-12 row, with capability families `DSK` — and `UI`
   where it extends FRD-12's domain.
7. **Add the `DSK` family rows** to `docs/capabilities.md` under
   `## Capabilities` (`:69`) using the existing column order
   `| ID | Durable outcome | Horizon | Target release | Canonical owner |
   Activation/boundary |` (`:71`), one row per **durable desktop outcome** — not
   one per screen; the existing `UI` and `OPS` families set that granularity.
   `Canonical owner` links to FRD-13 or to the owning ADR. Use two-digit IDs
   (`DSK-01`, `DSK-02`, …) and include a note row in the family stating that a
   capability ID is `FAMILY-NN` and is **not** the plan handle
   `DSK-<area>-<nn>` — `DSK-01` is a capability, `DSK-00-01` is a plan handle
   whose board id is [[FND-001]], and neither is a board id.
8. **Recompute all three derived totals** in `## Allocation summary` (`:29`):
   the horizon table at `:31-36` (today Now 132, Next 29, Later 41, Not planned
   29), the line `Total: **231 capabilities; 231 unique IDs**.` at `:38`, and the
   target-release table at `:40-54`. The horizon column must still sum to the
   stated total, and so must the release column. An unreconciled total is a
   defect the reviewer must reject. **Do not edit the `OPS-10` row at `:73`** —
   under D-004 its note change is [[REL-016]]'s (plan handle `DSK-09-18`);
   recomputing totals is not licence to edit a row another ticket owns.
9. **Update `docs/prd/pegasus-product.md`**: add the native Windows desktop
   client to `## Purpose, users, and outcomes` (`:4-17`), matching the file's
   register, and record the web front end's retirement after cutover under
   `## Permanent boundaries` (`:51`) or the scope section it belongs in —
   **recorded as scope, not as a schedule**. Read `## Permanent boundaries`
   first: a permanent boundary and a planned retirement are different claims.
10. **Add the FRD-13 link to `docs/index.md`**'s question→file table (`:7-30`),
    phrased as a question in the file's voice. The § New Markdown files
    paragraph at `:41-53` is already generic and needs no edit.
11. **Run the gates**, the same two the CI `documentation` job runs at
    `.github/workflows/ci.yml:84,87`:
    ```
    pwsh ./scripts/Test-DocumentationLinks.ps1
    pwsh ./scripts/Test-TestMarkdownPlacement.ps1
    ```
    Both exit 0. Confirm the FRD owner line matches whichever house pattern step
    3 recorded.
12. **Link, then clear.** `link_doc`
    `docs/frd/frd-13-desktop-operator-experience.md` to the area 06 and area 05
    tickets whose governing document it now is — [[DUI-013]] is recorded on the
    board as blocked by this ticket — and clear `docs_todo` **only** where it was
    set for FRD-13 alone, and only after the link exists. `docs_todo: true` is
    what satisfies `leave-backlog` for every `feature` ticket.
13. **Open the PR against `dev`**, take the independent review, and record
    `n/a — docs-only` under a dated `## Simplification pass` heading below.

## Verification

Evidence tier 1 — Static/build/architecture (`docs/engineering.md:72-90`), as
the body states: documentation coherence and link integrity. Behaviour itself is
proved by the area 05/06/08 tickets that implement against FRD-13.

| Command | Expected |
| --- | --- |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0, no broken relative link |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |
| `grep -c '^\| DSK-' docs/capabilities.md` | equals the number of `DSK` rows the allocation summary accounts for |
| `sed -n '29,54p' docs/capabilities.md` | the horizon column sums to the stated `Total:` line, and the target-release column sums to the same number |
| `grep -n 'FRD-13' docs/frd/README.md docs/index.md` | one link in each |
| `grep -n 'DSK-' docs/capabilities.md` | every hit is a two-digit `DSK-NN` capability; no `DSK-<area>-<nn>` plan handle appears as an ID |
| `git diff -- docs/capabilities.md \| grep 'OPS-10'` | empty — the `OPS-10` row is untouched |
| `grep -n '0108' docs/frd/frd-13-*.md` | ADR-0108 cited and explicitly marked `proposed` |
| `grep -c '^## ' docs/frd/frd-13-*.md` | matches the template's heading count, and the PR description flags that this is the first conformant FRD |

Proof is written on merged `main`, after review and the merge.

## Risks / open questions

- **Specifying the same behaviour in FRD-12 and FRD-13.** The defect this ticket
  is most likely to introduce, and the reason step 2 comes before step 3.
  Mitigation: the boundary is written into this plan, and the rule for shared
  rules is "cite, do not copy".
- **An unreconciled allocation summary.** Three totals, and only one is
  conspicuous. Mitigation: step 8 names all three with their line ranges, and the
  verification table checks both column sums.
- **Editing the `OPS-10` row while recomputing totals.** It is owned by
  [[REL-016]] under D-004. Mitigation: named in step 8 and checked by a `git
  diff` grep.
- **Citing an ADR that has not merged.** `Test-DocumentationLinks.ps1` fails on a
  relative link to a missing path. Mitigation: step 1 confirms [[FND-005]] has
  landed; step 5 restricts citations to merged files.
- **Template versus house form.** A recorded divergence, not an open question:
  the body instructs the template and the body outranks the author. Flagged in
  the PR so the reviewer sees a decision. Whether the twelve existing FRDs should
  be normalised is a separate operator call and is not opened here — [[FND-052]]
  is the board's grooming ticket if it becomes one.
- **A `DSK` family drafted per screen** would inflate the summary and make
  `Canonical owner` meaningless. Mitigation: step 7 fixes the granularity against
  the existing `UI` and `OPS` families.
- **Capability IDs collide visually with plan handles.** `DSK-01` (capability) is
  not `DSK-00-01` (plan handle, board id [[FND-001]]). Mitigation: the note row
  in step 7 and the grep in the verification table.
- **Scope boundaries owned by named tickets, not questions:** ADR authoring
  ([[FND-005]], [[FND-006]], [[FND-007]]); screen specifications (area 06); the
  `OPS-10` note ([[REL-016]]); ADR-0108's acceptance ([[FEAT-038]]).
- **Not open, and not to be reopened:** D-004; D-001; L-03; the reserved ADR
  block (operator, 2026-08-23).

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome: `n/a — docs-only`._
