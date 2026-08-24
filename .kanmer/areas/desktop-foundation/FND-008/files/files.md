# Files — FND-008

Surveyed 2026-08-24 against the working tree at `origin/main` `191ddf3342…`.
Every path was confirmed with `ls`, `wc -l`, `grep -n` or `sed -n`.

## Where the change lands

| Path | Why |
| --- | --- |
| `docs/frd/frd-13-desktop-operator-experience.md` | **New.** The desktop operator experience: shell and navigation, session and first run, keyboard completion, accessibility baseline, error and empty states, update-required behaviour. Uses the template at `docs/frd/README.md:35-58` — which makes it the **first** FRD in the tree to do so; the twelve existing files use a single domain heading. Cites `docs/design/README.md` rather than restating it |
| `docs/frd/README.md` | 58 lines. One new row in the `## Documents` table (`:16-32`), after the FRD-12 row at `:32`, with capability families `DSK` (and `UI` where it extends FRD-12's domain) |
| `docs/index.md` | 59 lines. One new question row in the question→file table (`:7-30`) pointing at FRD-13. The § New Markdown files paragraph at `:41-53` already states the PRD/FRD/ADR rule generically and needs **no** edit |
| `docs/prd/pegasus-product.md` | 103 lines. The native Windows desktop client added to `## Purpose, users, and outcomes` (`:4-17`), and the web front end's retirement after cutover recorded under `## Permanent boundaries` (`:51`) or the scope section it belongs in — **recorded as scope, not as a schedule** |
| `docs/capabilities.md` | 392 lines. New `DSK` family rows appended in the `## Capabilities` table (starts `:69`, header `:71`), each with a `Canonical owner` linking to FRD-13 or the owning ADR, plus a note row stating that a capability ID is `FAMILY-NN` and is **not** the plan handle `DSK-<area>-<nn>`. Then **all three** derived totals in `## Allocation summary` (`:29`) recomputed: the horizon table (`:31-36`), the `Total: **231 capabilities; 231 unique IDs**.` line (`:38`) and the target-release table (`:40-54`) |

## Context files

What the implementer must read to avoid a trap, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `docs/frd/README.md:1-12` | What an FRD is and — decisively for this ticket — what it is **not**: it "never invents product scope or records a technical decision (those belong to the PRD and the ADRs)". FRD-13 describes behaviour; the desktop *decisions* stay in ADR-0100/0102/0104/0105/0108, and the scope change goes in the PRD |
| `docs/frd/README.md:35-58` | The template the body instructs be used verbatim: the `> Owner capabilities: … · Source PRD: … · Design: docs/design/README.md#<...>` line, then `## Purpose`, `## Behaviour`, `## States and transitions`, `## Edge cases and fail-closed behaviour`, `## Acceptance evidence`, `## Links` |
| `docs/frd/frd-12-operator-experience.md:2` and `docs/frd/frd-11-*.md:2` | The **house** owner line, which differs from the template: `> Owner capabilities: <FAMILIES> · Source PRD: [Pegasus product requirements](../prd/pegasus-product.md) · UI behaviour: docs/design/README.md` — "UI behaviour", not "Design", and the design reference is a bare path rather than a Markdown link. Read both before writing line 2 of FRD-13 |
| `docs/frd/frd-12-operator-experience.md:4-20` | The boundary problem in concrete form: the dashboard, queues, intake-evidence filters `All`/`Instructions`/`Images`, list/detail journeys, supporting-detail navigation and administration that FRD-12 already specifies for the **web**. Every one of these is a candidate for accidental duplication — cite, never copy |
| `docs/design/README.md:1-25` | The binding UI authority. Its opening paragraph draws the same line this ticket must respect: design owns visual and interaction contracts; product scope stays with the PRD and capabilities. Its § Evidence discipline distinguishes intended / planned / implemented / caller-proved / deployed / accepted — the vocabulary FRD-13's `## Acceptance evidence` should use |
| `docs/capabilities.md:29-54` | **Three** derived totals, not one: the horizon table at `:31-36`, the `Total: **231 capabilities; 231 unique IDs**.` line at `:38`, and the target-release table at `:40-54`. Adding rows changes all three, and 132+29+41+29 must still reconcile to the stated total |
| `docs/capabilities.md:69-71` | `## Capabilities` and the exact column order `\| ID \| Durable outcome \| Horizon \| Target release \| Canonical owner \| Activation/boundary \|` |
| `docs/capabilities.md:73` | The `OPS-10` row — "Executed for releases 1–3 … operator acceptance outstanding". **Do not edit it.** Under operator decision D-004 (2026-08-24) that acceptance folds into the desktop pilot approval, and the note change is owned by [[REL-016]] (plan handle `DSK-09-18`). Recomputing totals is not licence to edit a row another ticket owns |
| `docs/prd/pegasus-product.md:4-17` | The purpose paragraph and required-outcomes list the desktop client joins, and the register it is written in — "one auditable system", "authorised Collision Engineers staff". Match the voice |
| `docs/prd/pegasus-product.md:51-63` | `## Permanent boundaries` — where the post-cutover web retirement is recorded as scope. Read it first: a *permanent* boundary and a *planned* retirement are different claims, and putting the retirement in the wrong section changes its meaning |
| `docs/index.md:7-30` | The question→file table and its voice: each row is a question a reader has, not a document name. FRD-13's row is phrased as a question |
| `docs/index.md:32-39` | The authority chain — operator-notes > PRD > FRD > capabilities > ADRs > current state > working rules. The reason this ticket exists: the desktop currently sits below all of it |
| `scripts/Test-DocumentationLinks.ps1:1-7,39-40` | 53 lines. Fails on a **relative link to a path that does not exist**; external URLs and same-file anchors are not checked; fenced code blocks and inline code spans are stripped first; `:40` strips the `#fragment` before testing the path. So an anchor is never validated, a bare path is not a link at all, and the only way this ticket fails on links is by citing an ADR file that has not merged yet |
| `.github/workflows/ci.yml:71-87` | The `documentation` job — the one lane every change set runs — invoking `Test-TestMarkdownPlacement.ps1` (`:84`) and `Test-DocumentationLinks.ps1` (`:87`) |
| `scripts/Test-MarkdownPlacement.ps1:31` | The allowed-roots regex. `docs/frd/**.md` is allowed, so FRD-13 passes. `-Base` and `-Head` are **mandatory** parameters (`:2-5`); the CI wrapper takes none |
| `docs/desktop/06-ui-design/README.md` and `screen-specs.md` | The per-screen behaviour FRD-13 **generalises**. Cite these for detail; copying a screen spec into an FRD makes two owners for one rule |
| `docs/desktop/00-governance-and-workflow/README.md` § 4 and § 8 | The Phase 0 target state ("FRD-13 and the PRD update are merged; `docs/capabilities.md` carries the `DSK` family") and the documentation-changes table this ticket satisfies |

## Ripple effects

- **`docs/capabilities.md` is the join between an ID and its owning document.**
  Every new `DSK` row's `Canonical owner` must resolve — to FRD-13 or to an ADR
  that has merged. A row pointing at an unmerged ADR fails the link gate.
- **Three totals move together.** A reviewer who checks only the `Total:` line
  will miss the horizon and target-release tables; the plan's verification checks
  all three.
- **[[DUI-013]] is blocked by this ticket** (recorded on the board), and the
  area 05 and 06 tickets take FRD-13 as their governing document. Step 12
  `link_doc`s FRD-13 to them and clears `docs_todo` where it was set only for
  FRD-13 — link first, clear second, or a satisfied `leave-backlog` gate is
  removed.
- **[[FND-005]] and [[FND-006]] must land first.** FRD-13 cites ADR-0100,
  ADR-0102, ADR-0104 and ADR-0105 by relative path; ADR-0108 comes from
  [[FND-007]] and is still `proposed` when cited.
- **`docs/frd/README.md` and `docs/index.md`** both gain one line; both are read
  by `Test-DocumentationLinks.ps1`.
- **No code, no test, no contract.** `openapi/pegasus-v1.json`, the generated
  client and every `src/` project are unaffected. Say so in the
  post-implementation report so a reviewer does not go looking.

## Out of scope

- **ADR authoring** — [[FND-005]] (ADR-0100, 0101, 0103, 0104, 0105, 0110),
  [[FND-006]] (ADR-0102, 0106, 0107, 0109), [[FND-007]] (ADR-0108). FRD-13 cites
  them; it records no technical decision of its own.
- **Screen specifications** — area 06 (`docs/desktop/06-ui-design/`). FRD-13
  cites `screen-specs.md` for per-screen detail rather than copying it.
- **The `OPS-10` capability row** — [[REL-016]] under D-004.
- **FRD-12's body** — untouched. It keeps the web operator experience until
  cutover; the boundary is written down rather than enforced by deletion.
- **Normalising the twelve existing FRDs to the template** — a separate
  documentation-hygiene decision; [[FND-052]] is the board's grooming ticket if
  it becomes one.
- **`docs/index.md` § New Markdown files** (`:41-53`) — already generic; no edit.
- **`docs/boundaries.md`, `docs/operations.md`, `docs/current-architecture.md`** —
  plan 00 § 8 assigns those to other tickets and to deployment-time changes.
- **`src/`, `tests/`, `.github/workflows/`** — no code, no CI change.
- **Azure** — no write, no read.
