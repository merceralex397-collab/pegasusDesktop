# Research — FND-008: FRD-13, the PRD scope change and the `DSK` capability family

## Question

Where does the native desktop client currently appear in the repository's
authority chain (PRD → FRD → capabilities → ADR), what boundary must FRD-13 hold
against FRD-12 so no behaviour is specified twice, and what exactly has to be
recomputed in `docs/capabilities.md` when a new family is added?

## Current behaviour

Measured 2026-08-24 from the working tree at `origin/main` `191ddf3342…`. The
desktop client appears in **none** of the four authority documents:

- **PRD.** `docs/prd/pegasus-product.md` is 103 lines with headings
  `## Purpose, users, and outcomes` (`:4`), `## Product invariants` (`:19`),
  `## Quality, capacity, security, and evidence` (`:31`),
  `## Permanent boundaries` (`:51`) and `## Acceptance model` (`:64`). Its
  purpose paragraph describes "one auditable system" and "authorised Collision
  Engineers staff"; there is no native client and no web-retirement statement.
- **FRD.** `docs/frd/README.md` (58 lines) lists FRD-01…FRD-12 at `:16-32`.
  FRD-12 is "Operator experience, dashboard freshness/reconciliation", capability
  family `UI` — and its content is unambiguously the **web** operator
  experience: `docs/frd/frd-12-operator-experience.md:4-20` specifies the
  authenticated dashboard, queues, intake-evidence filters, list/detail journeys
  and administration.
- **Capabilities.** `docs/capabilities.md` holds **231** rows
  (`grep -c '^| [A-Z]*-[0-9][0-9] |'` → 231) across **18** families — ACC, AI,
  API, BND, CASE, DATA, DOC, ENG, EVAL, EXT, INT, MAIL, MCP, MI, OPS, RPT, TRI,
  UI. **There is no desktop family.** The table starts at `## Capabilities`
  (`:69`) with header `| ID | Durable outcome | Horizon | Target release |
  Canonical owner | Activation/boundary |` (`:71`).
- **ADR.** The conversion block ADR-0100…ADR-0110 does not exist yet
  (`ls docs/adr/010*` → nothing); [[FND-005]] (plan handle `DSK-00-05`) authors
  six of them and [[FND-006]] four more.
- **Index.** `docs/index.md` (59 lines) is a question→file table at `:7-30`; the
  desktop plan set already has a row (`:30`), but nothing points at a desktop
  *behaviour* document.

**No parity-matrix row covers this ticket, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds **46** rows
(`grep -c '^| PAR-'` → 46), each keyed to a Razor page model under
`src/Pegasus.Web/Pages/` (`parity-matrix.md:36-38`). FRD-13 generalises *across*
those rows rather than owning any one of them. The closest existing repository
mechanism this ticket must not break is the documentation coherence lane: the
`Canonical owner` column in `docs/capabilities.md` that joins an ID to its owning
document, plus the CI `documentation` job (`.github/workflows/ci.yml:71-87`).

## Findings

- **The authority chain is stated once, in two places that agree.**
  `docs/index.md:32-39` § Authority: operator-notes > PRD > FRD > capabilities >
  ADRs > current-architecture/operations > runbook, engineering, design/README.
  `AGENTS.md` § Documentation model says the same. Today the desktop sits below
  the bottom of that chain — it exists only in `docs/desktop/`, which
  `AGENTS.md` § New Markdown placement calls "programme planning only".
- **The FRD template exists, and no existing FRD follows it.** The template at
  `docs/frd/README.md:35-58` prescribes the owner line plus `## Purpose`,
  `## Behaviour`, `## States and transitions`, `## Edge cases and fail-closed
  behaviour`, `## Acceptance evidence`, `## Links`. Measured `##` heading counts
  across the twelve files: 2, 1, 2, 1, 1, 2, 1, 2, 2, 3, 1, 1. Every one uses a
  single domain heading instead — FRD-11 and FRD-12 both have exactly one
  (`## Reports, correspondence, and reviewed proposals`; `## Operator
  experience`).
  - The house **owner line** also differs from the template: FRD-11 `:2` and
    FRD-12 `:2` both read
    `> Owner capabilities: <FAMILIES> · Source PRD: [Pegasus product requirements](../prd/pegasus-product.md) · UI behaviour: docs/design/README.md`
    — "UI behaviour", not the template's "Design", and the design reference is a
    **bare path, not a Markdown link**.
  - This is a genuine body-versus-tree divergence: the ticket body says use the
    template "verbatim". The plan follows the body and records the divergence for
    the reviewer rather than silently picking the house form.
- **The link checker is narrower than it looks, and that matters for the owner
  line.** `scripts/Test-DocumentationLinks.ps1` is 53 lines and its header
  (`:1-7`) says it fails on a relative link to a path that does not exist;
  external URLs and same-file anchors are **not** checked; fenced code blocks and
  inline code spans are stripped first. At `:39-40` it skips `https?:`,
  `mailto:` and `#` targets and strips the fragment before testing the path — so
  **an anchor is never validated**. A bare path such as
  `docs/design/README.md` is not a link at all and is never checked; a relative
  link `../design/README.md#operator-experience-requirements` is checked for the
  file only.
- **`docs/capabilities.md` has three derived totals, not one.** `## Allocation
  summary` at `:29` contains: a horizon table at `:31-36` (Now 132, Next 29,
  Later 41, Not planned 29); the line `Total: **231 capabilities; 231 unique
  IDs**.` at `:38`; and a target-release table at `:40-54` (`0.1.0-alpha.1` 132
  through `1.4.0` 3, plus `unallocated` 29). Adding `DSK` rows changes **all
  three** — 132+29+41+29 = 231 must still reconcile, and the release table's
  column must still sum to the same number. An unreconciled total is the defect
  the reviewer must reject.
- **Capability IDs are two-digit and collide visually with plan handles.** The
  registry form is `FAMILY-NN` (`OPS-10`, `EVAL-01`, `INT-17`). So `DSK-01` is a
  **capability**, and `DSK-00-01` is a **plan handle** whose board id is
  [[FND-001]]. Neither is a board id. `docs/desktop/README.md` § "Ticket IDs"
  and `docs/desktop/00-governance-and-workflow/README.md` § 7 both flag the
  collision; the ticket body requires a note row in the family saying so.
- **`OPS-10` already has a note this programme changes, but not here.**
  `docs/capabilities.md:73` reads "Executed for releases 1–3 … operator
  acceptance outstanding". The operator decided on 2026-08-24 (D-004) that this
  acceptance **folds into the desktop pilot approval**, and that the `OPS-10`
  note change is owned by [[REL-016]] (plan handle `DSK-09-18`). This ticket
  must not touch that row — recomputing the allocation summary is not licence to
  edit a row another ticket owns.
- **`docs/design/README.md` is binding UI authority and is cited, never
  restated.** Its opening paragraph says product scope stays with the PRD and
  capabilities, and `AGENTS.md` § Simplicity rails makes it binding on every UI
  change. FRD-12's owner line points at it; FRD-13's must too.
- **The dependency on [[FND-005]] is real, not ceremonial.** FRD-13 cites
  ADR-0100, ADR-0102, ADR-0104, ADR-0105 and ADR-0108. Four of those come from
  [[FND-005]] and [[FND-006]]; ADR-0108 comes from [[FND-007]] and will still be
  `status: proposed` (its acceptance flip belongs to [[FEAT-038]], plan handle
  `DSK-07-12`). A relative link to an ADR file that does not exist fails
  `Test-DocumentationLinks.ps1` and the CI `documentation` job.
- **`docs/index.md` has two places a new document can land**, and only one is
  right: the question→file table at `:7-30`, and the § New Markdown files
  paragraph at `:41-53` which already states the PRD/FRD/ADR rule generically.
  FRD-13 gets a question row; the paragraph needs no edit.

### Facts

| Fact | Source |
| --- | --- |
| PRD is 103 lines with those five headings; no native client mentioned | `docs/prd/pegasus-product.md` |
| FRD index lists FRD-01…FRD-12; FRD-12 owns family `UI` | `docs/frd/README.md:16-32` |
| FRD-12 specifies the web operator experience | `docs/frd/frd-12-operator-experience.md:4-20` |
| FRD template prescribes six headings plus an owner line | `docs/frd/README.md:35-58` |
| No existing FRD follows it — heading counts 2,1,2,1,1,2,1,2,2,3,1,1 | `grep -c '^## ' docs/frd/frd-*.md` |
| House owner line says "UI behaviour", with a bare design path | `docs/frd/frd-11-*.md:2`, `frd-12-*.md:2` |
| 231 capability rows across 18 families; no desktop family | `grep -c` and family extraction over `docs/capabilities.md` |
| Capabilities table header and start | `docs/capabilities.md:69,71` |
| Three derived totals in the allocation summary | `docs/capabilities.md:29-54` |
| `OPS-10` row and its outstanding-acceptance note | `docs/capabilities.md:73` |
| Link checker skips anchors and external URLs; strips fences | `scripts/Test-DocumentationLinks.ps1:1-7,39-40` |
| Authority chain | `docs/index.md:32-39` |
| Index question→file table and § New Markdown files | `docs/index.md:7-30`, `:41-53` |
| No conversion ADR exists yet | `ls docs/adr/010*` |

### Assumptions

- **A-00-13 — the FRD-12 / FRD-13 boundary can be drawn as "web until cutover,
  desktop from the start" without leaving a behaviour unowned.** *Confirmed by:*
  reading FRD-12's twenty-odd bullets at `:4-20` against the desktop screen
  specs in `docs/desktop/06-ui-design/` and finding each either clearly web-only
  or clearly restated for the desktop. *Breaks if:* a behaviour is genuinely
  shared and normative in both — for example dashboard freshness rules — in
  which case it stays in FRD-12 and FRD-13 cites it rather than duplicating it.
  Overlapping normative text in two FRDs is a defect, and the mitigation is
  always "cite, do not copy".
- **A-00-14 — the `DSK` family is a modest set of durable outcomes, not one row
  per screen.** *Confirmed by:* checking the drafted rows against the existing
  families' granularity — `UI` and `OPS` describe durable outcomes, not screens.
  *Breaks if:* the family is drafted per screen, which would inflate the
  allocation summary and make `Canonical owner` meaningless.
- **A-00-15 — the horizon and target-release allocations for `DSK` rows can be
  taken from the conversion phase map rather than needing a fresh operator
  decision.** *Confirmed by:* the operator accepting the drafted allocations at
  review. *Breaks if:* they want the desktop rows `unallocated` until the pilot,
  which changes only the numbers, not the structure.

## Execution placement

**This ticket places no responsibility anywhere: it authors and edits
documents.** The one placement it assumes is that FRD-13, the PRD update and the
`DSK` family rows live in this repository under `docs/frd/`, `docs/prd/` and
`docs/capabilities.md`. No runtime work moves, no credential is handled, no
artefact is published, and no Azure resource is touched — so the six-question
cloud-justification test has nothing to answer here. The placements FRD-13
*describes* are already recorded in the ADRs it cites (ADR-0100, ADR-0102,
ADR-0104, ADR-0105, ADR-0108), each of which carries its own answered table; this
FRD references them and does not re-derive them, because an FRD never records a
technical decision (`docs/frd/README.md:4-8`).

## Implications

1. **Draw the boundary before writing a line of behaviour.** FRD-12 keeps the
   web operator experience until cutover; FRD-13 owns the desktop one — shell
   and navigation, session and first run, keyboard completion, accessibility
   baseline, error and empty states, and update-required behaviour. Write the
   boundary into the plan, because a reviewer cannot check for duplication
   without a stated rule.
2. **Follow the template, and say that you are the first to.** The body
   instructs the `docs/frd/README.md` template verbatim, and the body outranks
   the author. But the twelve existing FRDs use a single domain heading and an
   owner line reading "UI behaviour" with a bare design path. Record the
   divergence in the plan and flag it in the PR so the reviewer sees a decision
   rather than an inconsistency — and so a later ticket can normalise the other
   twelve if the operator wants that.
3. **Recompute all three allocation totals, not the one that is easy to spot.**
   The horizon table, the `Total: **231 …**` line and the target-release table
   are three derived views of the same set.
4. **Cite ADRs, and only ones that exist.** [[FND-005]] must land first. Mark
   ADR-0108 as `proposed` where FRD-13 cites it — a `proposed` ADR is not
   settled authority.
5. **Do not touch the `OPS-10` row.** Its note change is [[REL-016]]'s under
   D-004. Recomputing totals is not licence to edit rows other tickets own.
6. **Anchors are free but paths are not.** The link checker validates the path
   and drops the fragment, so a relative link to a file that does not exist is
   the only way this ticket can fail the CI lane on links.

## Open questions

- **Whether FRD-13 adopts the template's heading set or the house single-heading
  form.** The body settles it — use the template — so this is recorded, not
  open; the plan states the divergence and the PR flags it.
- **Whether the twelve existing FRDs should be normalised to the template.** Not
  this ticket's, and not opened here: it is a separate documentation-hygiene
  decision for the operator, and [[FND-052]] is the board's grooming ticket if
  it becomes one.
- **The exact `DSK` row set, horizons and target releases** (A-00-14, A-00-15).
  Drafted from the conversion phase map and confirmed at review; the structure
  does not depend on the answer.
- Not open, and not to be reopened: **D-004** — the `OPS-10` operator acceptance
  folds into the desktop pilot approval, and the row's note change belongs to
  [[REL-016]]; the reserved ADR block (operator, 2026-08-23); L-03, which puts
  report-rendering behaviour under ADR-0108 so FRD-13 references it rather than
  re-specifying it.
