# Files — FND-008

Surveyed 2026-08-24 against the working tree at `origin/main`
`191ddf334208b8966dc5e32f4f597e434a086233`. Every path and line reference below
was confirmed with `ls`, `grep -n`, `sed -n` or `wc -l`. One file is created;
four existing files are edited.

## Where the change lands

| Path | Why |
| --- | --- |
| `docs/frd/frd-13-desktop-operator-experience.md` | **New.** The desktop operator experience: shell and navigation, session and first run, keyboard completion, accessibility baseline, error and empty states, update-required behaviour. Uses the `docs/frd/README.md:35-58` template (owner line + `## Purpose`, `## Behaviour`, `## States and transitions`, `## Edge cases and fail-closed behaviour`, `## Acceptance evidence`, `## Links`), which — see Context files — **no existing FRD follows**. Cites `docs/design/README.md` for UI behaviour rather than restating it, and cites ADR-0100/0102/0104/0105/0108 rather than re-deciding them |
| `docs/frd/README.md` | **Edit, +1 row.** The documents table runs `:16-32` with header `\| FRD \| Domain \| Capability families \|` at `:16-17` and FRD-12 as the last row at `:32`. Append an FRD-13 row naming families `DSK` (and `UI` where it extends FRD-12's domain). Three cells, matching the header |
| `docs/index.md` | **Edit, +1 row.** The question→file table is `:7-30`, header at `:7-8`. Add one desktop-experience question row where it reads naturally — beside the FRD-index row at `:11` or the design row at `:24`. The § New Markdown files paragraph at `:41-53` already states the PRD/FRD/ADR rule generically and needs **no** edit |
| `docs/prd/pegasus-product.md` | **Edit, ~8 lines.** 103 lines; headings `## Purpose, users, and outcomes` `:4`, `## Product invariants` `:19`, `## Quality, capacity, security, and evidence` `:31`, `## Permanent boundaries` `:51`, `## Acceptance model` `:64`. Add the native Windows desktop client to `:4`'s outcomes, and record the web front end's retirement after cutover as **scope, not schedule** — under `## Permanent boundaries` or the scope statement it belongs in |
| `docs/capabilities.md` | **Edit, ~20 lines.** 392 lines. Append `DSK` family rows under `## Capabilities` (`:69`, header `\| ID \| Durable outcome \| Horizon \| Target release \| Canonical owner \| Activation/boundary \|` at `:71`), each with a `Canonical owner` link to FRD-13 or the owning ADR, plus the family note row recording that `DSK-01` is a capability and `DSK-00-01` is a plan handle. **Then recompute all three derived totals** in `## Allocation summary` (`:29`) — the horizon table `:31-36`, the `Total: **231 capabilities; 231 unique IDs**.` line at `:38`, and the target-release table `:40-54` |

## Context files

Read these before writing a line. Each says what it tells the implementer.

| Path | What it tells the implementer |
| --- | --- |
| `docs/frd/README.md:3-13` | **What an FRD is allowed to be**: it specifies how a capability must behave, "cites `design.md` for UI behaviour, and **never invents product scope or records a technical decision** (those belong to the PRD and the ADRs)". This is the sentence that decides what goes in FRD-13 versus what goes in the PRD edit versus what stays in an ADR — the three halves of this ticket |
| `docs/frd/README.md:11-13` | "Each FRD is owned by one or more capability IDs; the join is the *Canonical owner* column in `capabilities.md`." The reason the `DSK` rows and FRD-13 are one ticket and not two: an FRD with no capability rows is unowned, and rows with no FRD have nothing to point at |
| `docs/frd/README.md:16-32` | The documents table to append to, and the exact shape of a row — a bracketed relative link, a domain phrase, and comma-separated family codes |
| `docs/frd/README.md:35-58` | **The template the body says to use verbatim**: the owner line `> Owner capabilities: <IDs> · Source PRD: <link> · Design: docs/design/README.md#<...>` and six `##` headings. Read it knowing the tree diverges (next row) |
| `docs/frd/frd-12-operator-experience.md:1-4` | **The boundary this FRD must not cross, and the house form the template does not match.** `:1` is `# FRD-12: Operator experience`; `:2` is the owner line, reading `· UI behaviour: docs/design/README.md` — *"UI behaviour"*, not the template's *"Design"*, and a **bare path, not a Markdown link**; `:4` is `## Operator experience`, its single domain heading. 131 lines in total |
| `docs/frd/frd-12-operator-experience.md:4-20` | The web operator experience in detail — authenticated dashboard, queues, intake-evidence filters, list/detail journeys, administration. Read every bullet and decide, for each, "web only until cutover" or "restated for the desktop". A behaviour normative in both FRDs is a defect; the resolution is always **cite, do not copy** |
| `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md:1-4` | A second sample of the same house form, confirming `:2`'s owner line is a pattern rather than one file's accident |
| `docs/design/README.md` | Binding UI authority (`AGENTS.md` § Simplicity rails). FRD-13's owner line points here and its `## Behaviour` cites it; operator-facing explanation in UI copy is a **defect** under this document, not a style preference |
| `docs/capabilities.md:29-54` | **The three derived totals, and the trap.** Horizon table `:31-36` (Now 132, Next 29, Later 41, Not planned 29); `Total: **231 capabilities; 231 unique IDs**.` at `:38`; target-release table `:40-54` (`0.1.0-alpha.1` 132 … `1.4.0` 3, `unallocated` 29). Adding rows changes **all three**, and 132+29+41+29 must still reconcile to the stated total. Measured today: `grep -c '^| [A-Z]*-[0-9][0-9] |' docs/capabilities.md` → **231**, matching `:38` exactly |
| `docs/capabilities.md:69-71` | Where the rows go and the six-column order. `Canonical owner` is the join key to a PRD, FRD or ADR — a row whose owner column names nothing is the defect the column exists to prevent |
| `docs/capabilities.md:73` | **The `OPS-10` row — do not touch it.** Its note still reads "operator acceptance outstanding"; operator decision D-004 (2026-08-24) folds that acceptance into the desktop pilot approval, and the note change is owned by [[REL-016]] (plan handle `DSK-09-18`). Recomputing the allocation summary is not licence to edit a row another ticket owns |
| `docs/capabilities.md:73-78` | The house row idiom to imitate — a durable outcome phrased as an outcome, not a screen; a bracketed link in `Canonical owner`; and an `Activation/boundary` cell that names the gate rather than restating the outcome |
| `docs/prd/pegasus-product.md:4-18` | `## Purpose, users, and outcomes` — one auditable system, authorised Collision Engineers staff. Where the native client is added, in the PRD's register: outcomes, not mechanics |
| `docs/prd/pegasus-product.md:51-63` | `## Permanent boundaries` — the candidate home for the post-cutover web retirement, recorded as scope. Read it first: a *permanent* boundary is not the same as a planned retirement, and if the section will not carry it honestly, the scope statement in `:4-18` is the alternative |
| `docs/index.md:7-30` | The question→file table, one row per question. The desktop plan set already has a row at `:30`, and it is labelled "programme planning only" — which is exactly why a *behaviour* row is needed |
| `docs/index.md:32-39` § Authority | The chain FRD-13 is joining: operator-notes > PRD > FRD > capabilities > ADRs > current state > working rules. Also the conflict rule — "fix the losing document in the same commit you notice it" |
| `docs/index.md:41-53` § New Markdown files | Already states the PRD/FRD/ADR placement rule generically. **No edit needed** — a second statement would be the duplication this ticket is trying to avoid elsewhere |
| `scripts/Test-DocumentationLinks.ps1:1-7`, `:39-40` | **What the link gate actually checks.** It fails on a relative link to a path that does not exist; external URLs and same-file anchors are **not** checked; fenced and inline code are stripped first; at `:39-40` it skips `https?:`, `mailto:` and `#` targets and strips the fragment before testing. Consequences: an anchor is never validated, a **bare path is not a link at all**, and the only way this ticket fails on links is a relative link to a file that does not exist — which is precisely the ADR-citation risk |
| `docs/desktop/06-ui-design/README.md` and `screen-specs.md` | The per-screen behaviour FRD-13 **generalises and cites, never copies**. Screen specs are area 06's; FRD-13 states the rule, the spec states the screen |
| `docs/desktop/00-governance-and-workflow/README.md` § 4 | The Phase 0 target state that makes FRD-13 and the PRD update part of the governance exit gate, and `docs/capabilities.md` carrying the `DSK` family part of it too |
| Kanmer group doc `HZN-001/board-conventions.md` (`get_group_doc HZN-001 board-conventions.md`) | The id rule: a bare `<PREFIX>-<nnn>` is a **fork board id**. Read it before writing the `DSK` family note row, because that row exists to separate three namespaces at once — capability `DSK-01`, plan handle `DSK-00-01`, board id [[FND-001]] |

## Ripple effects

- **The allocation summary is the ripple**, and it is arithmetic, not prose.
  Three views of one set (`docs/capabilities.md:31-36`, `:38`, `:40-54`) all
  change when `DSK` rows are added, and they must still reconcile to each other
  and to `grep -c '^| [A-Z]*-[0-9][0-9] |'`. An unreconciled total is the defect
  the reviewer must reject.
- **The FRD count changes in two indexes**: `docs/frd/README.md`'s documents
  table and `docs/index.md`'s question table. Both are hand-maintained; neither
  is generated.
- **Board ripple.** Step 12 `link_doc`s FRD-13 to the area 05 and area 06 tickets
  it now governs and clears `docs_todo` **only** where it was set for FRD-13
  alone. `docs_todo: true` is what satisfies `leave-backlog` for a `feature`
  ticket today, so clearing it without a real linked path would leave that ticket
  unable to leave `backlog`. [[DUI-013]] is the ticket this one blocks. Re-probe
  with `get_doc_gates` afterwards.
- **A hard upstream dependency, not a soft one.** FRD-13 cites ADR-0100,
  ADR-0102, ADR-0104, ADR-0105 and ADR-0108 by relative path. Those files come
  from [[FND-005]] (plan handle `DSK-00-05`), [[FND-006]] (`DSK-00-06`) and
  [[FND-007]] (`DSK-00-07`); `ls docs/adr/010*` returned *No such file or
  directory* on 2026-08-24. A relative link to a file that does not exist fails
  `scripts/Test-DocumentationLinks.ps1` and the CI `documentation` job
  (`.github/workflows/ci.yml:70-87`) — so this ticket cannot merge before them.
- **No code, test or build ripple.** `src/`, `tests/`, `scripts/` and `.github/`
  are untouched, and there is no `openapi/` directory in the repository today
  (`ls openapi` → *No such file or directory*), so the usual contract ripple does
  not apply. The only checks that follow are the two CI documentation scripts.
- **Downstream authors stop citing the plan set.** Once FRD-13 exists, area 05
  and 06 tickets cite a real FRD instead of writing the "New FRD" paragraph, and
  `docs/capabilities.md`'s `Canonical owner` column can finally answer "which
  document owns this desktop outcome".

## Out of scope

Recorded so the reviewer sees each was a decision. The ticket's Guardrails
already forbid them.

- **Authoring any ADR.** ADR-0100/0101/0103/0104/0105/0110 are [[FND-005]]'s,
  ADR-0102/0106/0107/0109 are [[FND-006]]'s, and ADR-0108 is [[FND-007]]'s.
  FRD-13 **cites** them; it records no technical decision
  (`docs/frd/README.md:5-8`).
- **The `OPS-10` row at `docs/capabilities.md:73`.** Its note change under D-004
  belongs to [[REL-016]] (plan handle `DSK-09-18`). Not edited here, not even in
  passing while recomputing totals.
- **Per-screen behaviour.** `docs/desktop/06-ui-design/screen-specs.md` is area
  06's; FRD-13 cites it rather than copying it. No screen spec, no wireframe, no
  UI copy is written here.
- **Re-specifying report rendering.** L-03 puts that behaviour under ADR-0108,
  which is still `status: proposed` — FRD-13 references it **and marks it as
  proposed**, because a `proposed` ADR is not settled authority.
- **Normalising the twelve existing FRDs to the template.** They all use a single
  domain heading and the "UI behaviour" owner line. That is a separate
  documentation-hygiene decision for the operator; [[FND-052]] is the board's
  grooming ticket if it becomes one.
- **Editing `docs/index.md` § New Markdown files (`:41-53`)** — it already states
  the rule generically.
- **`docs/boundaries.md`, `docs/operations.md`, `docs/current-architecture.md`,
  `docs/engineering.md`, `AGENTS.md`.** None is touched. The `AGENTS.md`
  ADR-index-shape correction is [[FND-005]]'s.
- **All of `src/`, `tests/`, `scripts/`, `.github/`, `.codex/`, `.agents/`,
  `.grok/`.** This is a documentation-only branch; its simplification pass
  records `n/a — docs-only`.
- **Any Azure read or write.**
