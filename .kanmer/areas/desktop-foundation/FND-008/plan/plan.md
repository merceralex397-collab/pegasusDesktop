# Plan — FND-008: FRD-13, the PRD scope change and the `DSK` capability family

**Diff estimate: ~5 files, ~185 lines.**

Derived from the `files` document, not asserted. One new FRD at the measured
house length — FRD-12 is 131 lines and FRD-13 carries the template's six headings
rather than one domain heading, so ~150; `docs/frd/README.md` +1 row;
`docs/index.md` +1 row; `docs/prd/pegasus-product.md` ~8 lines across two
sections; `docs/capabilities.md` ~20 lines (the `DSK` rows plus the family note
row, plus roughly six changed lines across the three recomputed totals).
Five files, one of them new.

## Approach

Do the three halves in dependency order — **boundary, then FRD, then registry** —
and treat the allocation summary as arithmetic that must reconcile rather than
prose that must read well.

The boundary comes first because it is the only part a reviewer cannot check
without a written rule. FRD-12 (`docs/frd/frd-12-operator-experience.md`, 131
lines) already specifies an operator experience, and it is unambiguously the
**web** one (`:4-20`: authenticated dashboard, queues, intake-evidence filters,
list/detail journeys, administration). Writing FRD-13 first and drawing the line
afterwards produces two FRDs that both say "the dashboard must…", which is a
defect and an expensive one to unpick. So step 2 writes the rule down before any
behaviour is drafted, and every subsequent decision is measured against it.

The rejected alternative was **splitting the ticket** — FRD-13 in one PR, the
`DSK` family in another. It is superficially cleaner and it breaks the join:
`docs/frd/README.md:11-13` states that "each FRD is owned by one or more
capability IDs; the join is the *Canonical owner* column in `capabilities.md`".
An FRD merged without its capability rows is an unowned document, and rows merged
without their FRD point at nothing. They go together.

The second rejected alternative was **inferring the `DSK` rows from the desktop
screen specs** — one row per screen. The existing families do not work that way:
`UI` and `OPS` register durable outcomes, not screens. A per-screen family would
inflate all three allocation totals and make `Canonical owner` meaningless.

## Governing docs

`refs` is empty and `docs_todo: true` — confirmed by `get_doc_gates FND-008`,
whose `leave-backlog` requirement `governing-doc` reads `satisfied: true` on the
strength of `docs_todo`.

> **New FRD** — FRD-13 "Desktop operator experience", **authored by this
> ticket**; it is the governing document [[DUI-013]] and the area 05/06 UI
> tickets are waiting for.
> It is written **to** the conversion ADR block, which this ticket does not
> author: **ADR-0100** (native WinUI 3 client) and **ADR-0104** (online-required,
> bounded local cache), authored by [[FND-005]] (plan handle `DSK-00-05`) —
> ADR-0100 and ADR-0104 are co-claimed by [[FND-026]] (plan handle `DSK-02-01`),
> so write `authored by [[FND-005]]; see [[FND-005]]'s plan for the ownership
> reconciliation`; **ADR-0102** (credentials and token session) and **ADR-0105**
> (MSIX and minimum-version gate) — ADR-0102 authored by [[FND-006]] (plan handle
> `DSK-00-06`), co-claimed by [[FND-042]] (`DSK-04-01`), and ADR-0105 authored by
> [[FND-005]] with three claimants, `see [[FND-005]]'s plan for the ownership
> reconciliation`; and **ADR-0108** (isolated WebView2 report rendering),
> authored `proposed` by [[FND-007]] (`DSK-00-07`) and flipped to `accepted` by
> [[FEAT-038]] (`DSK-07-12`).
> This plan is written to those decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and to L-03 as recorded
> in `docs/desktop/README.md` § Locked decisions; if an ADR lands differently
> this plan is revised before implementation.

Because `refs` is empty, the authorities that bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/index.md:32-39` § Authority | operator-notes > PRD > FRD > capabilities > ADRs > current state > working rules; on conflict, fix the losing document in the same commit | The whole ticket — it is the ticket that puts the desktop into that chain |
| `docs/frd/README.md:3-9` | An FRD specifies behaviour, cites the design document for UI, and **never invents product scope or records a technical decision** | Steps 4–5 and the PRD split in step 9 |
| `docs/frd/README.md:11-13` | Each FRD is owned by capability IDs; the join is the `Canonical owner` column | Steps 3, 7 (and the Approach's reason for not splitting) |
| `docs/frd/README.md:35-58` | The FRD template — owner line plus six headings | Step 3 |
| `docs/frd/README.md:16-32` | The documents table shape | Step 6 |
| `docs/capabilities.md:69-71` | The six-column registry order and the `Canonical owner` join | Step 7 |
| `docs/capabilities.md:29-54` | Three derived totals that must reconcile | Step 8 |
| `docs/design/README.md` (binding via `AGENTS.md` § Simplicity rails) | UI behaviour is cited, never restated; operator-facing explanation is a defect | Steps 3–4 |
| `AGENTS.md` § New Markdown placement | `docs/desktop/` is programme planning only — decisions land as ADRs, behaviour as FRDs | The whole ticket |
| Proposal § 26 Documentation set (Product and UI) | The conversion needs the product/UI documentation set | Steps 3, 7, 9 |
| Proposal § 14 Native WinUI 3 experience | The behaviour FRD-13 generalises | Step 4 |
| Proposal § 27 item 4 and item 12 | Unsupported versions cannot proceed; critical workflows keyboard-accessible | Step 4's normative rules |
| Plan 00 § 4 Target state and exit gate | FRD-13 and the PRD update are merged; `docs/capabilities.md` carries the `DSK` family | Steps 3, 7, 9 |
| L-03 (`docs/desktop/README.md`) | Report rendering behaviour is owned by ADR-0108 | Step 5 (referenced, marked `proposed`, never re-specified) |
| D-001 | The fork becomes the single release source — which is what makes a PRD scope change **in this repository** the right place to record web retirement | Step 9 |
| D-004 (operator, 2026-08-24) | `OPS-10`'s outstanding acceptance folds into the desktop pilot approval, and the row's note change belongs to [[REL-016]] (`DSK-09-18`) | Step 8's guardrail: recompute totals, touch no row |
| `scripts/Test-MarkdownPlacement.ps1:31` + `.github/workflows/ci.yml:70-87` | New Markdown only under the allowed roots; the `documentation` job runs on every change set | Step 11 |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § "Ticket template" requires
of the plan document specifically.

- **Subagent**: — (parent session; the plan routes this work to no subagent).
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

These refine the body's thirteen implementation steps; the order, the ownership
and the file paths are the body's. Measured values were read on 2026-08-24.

1. **Orient.** Read `docs/frd/README.md` in full (58 lines: definition `:3-13`,
   documents table `:16-32`, template `:35-58`), then
   `docs/frd/frd-12-operator-experience.md` in full (131 lines),
   `docs/design/README.md`, and `docs/desktop/06-ui-design/README.md`. Call
   `get_doc_gates FND-008` — expect `leave-backlog: [governing-doc]` satisfied by
   `docs_todo` and `leave-preparing: [research, files, plan, checklist,
   questions-resolved]` — then `take_ticket`.
   **Gate before anything else:** run `ls docs/adr/010*`. It returned
   *No such file or directory* on 2026-08-24. FRD-13 cites ADR-0100, ADR-0102,
   ADR-0104, ADR-0105 and ADR-0108 by relative path, and
   `scripts/Test-DocumentationLinks.ps1` fails on a relative link to a
   non-existent path — so this ticket **cannot merge** until [[FND-005]],
   [[FND-006]] and [[FND-007]] have landed theirs. If they have not, stop here
   rather than writing links you will have to remove.
2. **Draw the FRD-12 / FRD-13 boundary first, and write it into this plan.**
   Read every bullet of `docs/frd/frd-12-operator-experience.md:4-20` and classify
   each as *web only until cutover* or *restated for the desktop*. The rule to
   record and then hold:
   > **FRD-12 keeps the web operator experience until cutover. FRD-13 owns the
   > desktop operator experience: shell and navigation, session and first run,
   > keyboard completion, accessibility baseline, error and empty states, and
   > update-required behaviour.**
   Where a behaviour is genuinely shared and normative — dashboard freshness and
   reconciliation is the likely case — **it stays in FRD-12 and FRD-13 cites it**.
   Overlapping normative text in two FRDs is a defect; the resolution is always
   *cite, do not copy*. Write the classification into this section as you make it,
   so the reviewer can check the rule against the diff rather than re-deriving it.
3. **Create `docs/frd/frd-13-desktop-operator-experience.md`** using the
   `docs/frd/README.md:35-58` template **verbatim**, as the body instructs: the
   owner line
   `> Owner capabilities: <IDs> · Source PRD: [Pegasus product requirements](../prd/pegasus-product.md) · Design: docs/design/README.md#<...>`
   then `## Purpose`, `## Behaviour`, `## States and transitions`,
   `## Edge cases and fail-closed behaviour`, `## Acceptance evidence`,
   `## Links`.
   > **Divergence to record and flag, not to resolve silently.** No existing FRD
   > follows that template. Measured `grep -c '^## ' docs/frd/frd-*.md`: 2, 1, 2,
   > 1, 1, 2, 1, 2, 2, 3, 1, 1 — every file uses a single domain heading instead
   > (FRD-11 `## Reports, correspondence, and reviewed proposals`; FRD-12
   > `## Operator experience` at `:4`). The house **owner line** also differs:
   > `docs/frd/frd-11-…:2` and `docs/frd/frd-12-…:2` both read
   > `· UI behaviour: docs/design/README.md` — *"UI behaviour"*, not the
   > template's *"Design"*, and a **bare path, not a Markdown link**. The ticket
   > body says use the template, the body is settled, and this plan follows it —
   > but raise the divergence with `pegasus-desktop-reviewer` in the PR so it
   > reads as a decision rather than an inconsistency, and so a later ticket can
   > normalise the other twelve if the operator wants that. It blocks nothing.
4. **Write `## Behaviour` as normative rules** — "must", "never", "fails closed"
   — not description. The rules this FRD owes, each traceable to an authority:
   an unsupported client version **must not proceed** (ADR-0105, proposal § 27
   item 4); every critical workflow **must be completable from the keyboard**
   (§ 27 item 12); a field is a label and a control, and operator-facing
   explanation **is a defect** (`docs/design/README.md`, `AGENTS.md` § Simplicity
   rails); **no colour-only state**; a disconnected client **must not silently
   queue** work (ADR-0104). Cite
   `docs/desktop/06-ui-design/screen-specs.md` for per-screen detail instead of
   copying it — the FRD states the rule, the screen spec states the screen.
5. **Cite accepted ADRs, not the plan set** — the plan set is programme planning
   only (`AGENTS.md` § New Markdown placement). Cite ADR-0100 (native client),
   ADR-0102 (session), ADR-0104 (online-required), ADR-0105 (minimum version) and
   ADR-0108 (report rendering). **Mark ADR-0108 as `status: proposed` where it is
   cited**: a `proposed` ADR is not settled authority, its acceptance flip belongs
   to [[FEAT-038]] (plan handle `DSK-07-12`), and FRD-13 must not read as though
   L-03 were already proven.
6. **Add the FRD-13 row** to the documents table in `docs/frd/README.md`
   (`:16-32`, FRD-12 last at `:32`) — three cells, matching the header at `:16`:
   a bracketed relative link, the domain phrase, and the capability families
   (`DSK`, plus `UI` where FRD-13 extends FRD-12's domain).
7. **Add the `DSK` family rows** to `docs/capabilities.md` under `## Capabilities`
   (`:69`), using the six-column order at `:71` —
   `| ID | Durable outcome | Horizon | Target release | Canonical owner | Activation/boundary |`
   — one row per **durable desktop outcome**, not per screen (`UI` and `OPS` are
   the granularity to match), with `Canonical owner` a bracketed link to FRD-13 or
   the owning ADR. Use two-digit IDs `DSK-01`, `DSK-02`, …
   **Include the family note row, and make it do real work:** a capability ID is
   `FAMILY-NN`, so `DSK-01` is a capability; `DSK-00-01` is a **plan handle**;
   and the **board id** is [[FND-001]]. Three namespaces, one prefix — the Kanmer
   group document `HZN-001/board-conventions.md` holds the id rule, and
   `docs/desktop/README.md` § "Ticket IDs" and plan 00 § 7 both flag the
   collision.
8. **Recompute all three derived totals** in `## Allocation summary` (`:29`) —
   not just the obvious one:
   (a) the horizon table `:31-36`, today Now 132 / Next 29 / Later 41 / Not
   planned 29;
   (b) the line `Total: **231 capabilities; 231 unique IDs**.` at `:38`;
   (c) the target-release table `:40-54`, today `0.1.0-alpha.1` 132 through
   `1.4.0` 3 plus `unallocated` 29.
   The invariant to hold: the four horizon numbers sum to the stated total, the
   release column sums to the same number, and the total equals
   `grep -c '^| [A-Z]*-[0-9][0-9] |' docs/capabilities.md` — which returns **231**
   today, matching `:38` exactly. **An unreconciled total is a defect the
   reviewer must reject.**
   **Guardrail while doing it: touch no existing row.** In particular
   `docs/capabilities.md:73` (`OPS-10`) still reads "operator acceptance
   outstanding"; under D-004 that note change is [[REL-016]]'s (plan handle
   `DSK-09-18`). Recomputing totals is not licence to edit a row another ticket
   owns.
9. **Update `docs/prd/pegasus-product.md`** (103 lines): add the native Windows
   desktop client to `## Purpose, users, and outcomes` (`:4-18`) as an **outcome,
   not a mechanism** — the PRD states no mechanics
   (`AGENTS.md` § Documentation model) — and record the web front end's
   retirement after cutover as **scope, not schedule**. Read
   `## Permanent boundaries` (`:51-63`) before putting it there: a *permanent*
   boundary is not the same as a planned retirement, and if that section will not
   carry it honestly, the scope statement in `:4-18` is the right home. Say which
   you chose and why, here in this plan.
10. **Add the FRD-13 link to `docs/index.md`'s question→file table** (`:7-30`) —
    one desktop-experience question row, placed where it reads naturally beside
    the FRD-index row at `:11` or the design row at `:24`. The § New Markdown
    files paragraph at `:41-53` already states the PRD/FRD/ADR rule generically
    and needs **no** edit.
11. **Run the gates**, the same two the CI `documentation` job runs at
    `.github/workflows/ci.yml:82-87`:
    ```
    pwsh ./scripts/Test-DocumentationLinks.ps1
    pwsh ./scripts/Test-TestMarkdownPlacement.ps1
    ```
    Both exit 0, and confirm the FRD owner line matches the shape step 3 chose.
    Know what the link gate does and does not do
    (`scripts/Test-DocumentationLinks.ps1:1-7`, `:39-40`): it fails on a relative
    link to a path that does not exist, **skips external URLs and same-file
    anchors entirely**, strips the fragment before testing a path, and strips
    fenced and inline code before scanning. So a `#anchor` is never validated and
    a **bare path is not a link at all** — the only way this ticket fails on links
    is a relative link to a missing file, which is exactly the ADR-citation risk
    step 1 gates on.
12. **`link_doc` `docs/frd/frd-13-desktop-operator-experience.md`** to the area 06
    and area 05 tickets whose governing document it now is — [[DUI-013]] is the
    ticket this one blocks — and clear `docs_todo` **only** where it was set for
    FRD-13 alone. `docs_todo: true` is what satisfies `leave-backlog` for a
    `feature` ticket today, so link first and clear second; clearing without a
    real linked path removes a satisfied gate. Re-probe `get_doc_gates` on at
    least one affected ticket and record the output.
13. **Open the PR against `dev`** (`gh pr create --base dev`), take the
    independent review from `pegasus-desktop-reviewer`, and record
    `n/a — docs-only` under a dated `## Simplification pass` heading below.

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states.
Documentation coherence and link integrity are the whole of the claim; the
behaviour itself is proved by the area 05/06/08 tickets that implement **against**
FRD-13.

| Check | Expected |
| --- | --- |
| `ls docs/adr/010*` — run **before** writing (step 1) | the ADR files FRD-13 cites already exist; *No such file or directory* is a stop condition, not a warning |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0, no broken relative link |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |
| `grep -c '^| DSK-' docs/capabilities.md` | the number of `DSK` rows, and it must equal the increase recorded in all three allocation totals |
| `grep -c '^| [A-Z]*-[0-9][0-9] |' docs/capabilities.md` | equals the new `Total: **N capabilities; N unique IDs**.` line at `:38`; measured **231** before this ticket |
| Horizon table `:31-36` summed | equals that same total |
| Target-release table `:40-54` summed | equals that same total |
| `git diff -- docs/capabilities.md \| grep '^-'` | **no removed row** other than the three total lines being rewritten — in particular no change to the `OPS-10` row at `:73` |
| `grep -n 'FRD-13' docs/frd/README.md docs/index.md` | exactly one link in each |
| `grep -n '^## ' docs/frd/frd-13-*.md` | `## Purpose`, `## Behaviour`, `## States and transitions`, `## Edge cases and fail-closed behaviour`, `## Acceptance evidence`, `## Links` |
| `grep -n 'proposed' docs/frd/frd-13-*.md` | ADR-0108 cited **and marked `proposed`** |
| `grep -n 'desktop' docs/prd/pegasus-product.md` | the native client recorded as an outcome, and web retirement recorded as scope |
| `get_doc_gates <a ticket whose docs_todo step 12 cleared>` | `leave-backlog` still `passable: true`, now on the linked FRD rather than on `docs_todo` |

Proof is written on merged `main`, after review and merge — never before
(`AGENTS.md` § Kanmer operating instructions).

## Risks / open questions

- **Specifying a behaviour in both FRD-12 and FRD-13.** The defect this ticket is
  most likely to commit, because both documents are called "operator
  experience". Mitigation: step 2 draws and records the rule **before** any
  behaviour is drafted, and the shared-behaviour resolution is fixed in advance —
  it stays in FRD-12 and FRD-13 cites it.
- **An unreconciled allocation total.** There are three derived views
  (`docs/capabilities.md:31-36`, `:38`, `:40-54`) and only one is conspicuous.
  Mitigation: step 8 names all three and the verification table asserts each sum
  against `grep -c`.
- **Editing the `OPS-10` row while recomputing.** Its note change is
  [[REL-016]]'s under D-004 — settled, and not to be reopened here. Mitigation:
  step 8's guardrail and the `git diff | grep '^-'` check.
- **Merging before the ADRs exist.** A relative link to a missing ADR fails the
  CI `documentation` job. Mitigation: step 1 is a hard gate on
  `ls docs/adr/010*`; the dependency on [[FND-005]], [[FND-006]] and [[FND-007]]
  is real, not ceremonial.
- **Template versus house form** (step 3). A recorded divergence flagged to the
  reviewer, not an open question: the body settles it, it blocks nothing, and
  normalising the other twelve FRDs is a separate operator decision that
  [[FND-052]] would carry if it became one.
- **`DSK` row set, horizons and target releases.** Drafted from the conversion
  phase map and confirmed at review (research assumptions A-00-14, A-00-15). The
  structure does not depend on the answer; if the operator wants the desktop rows
  `unallocated` until the pilot, only the numbers change.
- **Scope boundaries owned by named tickets, not questions:** the ADRs
  ([[FND-005]], [[FND-006]], [[FND-007]]); the ADR-0108 acceptance flip
  ([[FEAT-038]]); the `OPS-10` note ([[REL-016]]); per-screen behaviour (area 06);
  FRD normalisation ([[FND-052]] if it becomes a ticket at all). None gates this
  ticket's `leave-preparing`.
- **Not open, and not to be reopened:** D-004 (the `OPS-10` acceptance folds into
  the desktop pilot approval); the reserved ADR block (operator, 2026-08-23);
  L-03, which puts report-rendering behaviour under ADR-0108 so FRD-13 references
  it rather than re-specifying it.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome for this ticket: `n/a — docs-only`._

## Dependency stop — 2026-08-25

Before taking FND-008, the required ADR gate was rerun against `origin/dev`. Present: `docs/adr/0100-native-winui-3-client-in-the-fork.md`, `0101-local-execution-cloud-authority-split.md`, `0103-gateway-not-direct-database-access.md`, `0104-online-required-bounded-local-cache.md`, and `0105-msix-app-installer-and-minimum-version-gate.md`. Missing: ADR-0102 and ADR-0108. Live Kanmer confirms FND-006 remains `preparing` and FND-007 remains `review`/claimed. This is the plan's explicit stop condition, so no take, worktree, branch, document implementation, or speculative link was made. Recheck after those two ADR deliveries land in `dev`; the smallest unblock is their reviewed delivery, not a ticket-local workaround.

## Dependency recheck — 2026-08-26

The earlier 2026-08-25 dependency stop is superseded by a live configured-remote check. On `origin/dev` `fff7e14178f1be6e3d4f2fbc5a5401799ba69409`, ADR-0102 exists with `status: accepted` (FND-042 merge `61227d6b`) and ADR-0108 exists with `status: proposed` (FND-007 PR #13 merge `d4c17fdd`); ADR-0100, ADR-0104 and ADR-0105 are present. FND-005 is `done`. The ticket may proceed from its own `origin/dev`-based worktree. This recheck used only the configured `pegasusDesktop` remote; no upstream sync, cloud write or deployment is permitted.
