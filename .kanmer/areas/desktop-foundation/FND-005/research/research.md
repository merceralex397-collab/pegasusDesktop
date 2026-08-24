# Research — FND-005: the reserved ADR block and its six foundation decisions

## Question

What must ADR-0100, ADR-0101, ADR-0103, ADR-0104, ADR-0105 and ADR-0110 contain,
what house form must they take so the CI documentation lane and the ADR index
stay coherent, and — where more than one seeded ticket claims the same ADR
number — what is actually settled about who authors it?

## Current behaviour

The repository records durable technical decisions as ADRs under `docs/adr/`.
Measured on 2026-08-24:

- **The set.** `docs/adr/` holds ADR-0001…ADR-0029 with **ADR-0017 never issued**
  (a numbering collision while filing 0018/0019; the gap is intentional and the
  number is not reused — `docs/adr/README.md:53-54`). **None of 0100, 0101, 0103,
  0104, 0105 or 0110 exists**: `ls docs/adr/010*` returns nothing.
- **The index.** `docs/adr/README.md:16` is
  `## Current architecture decisions (`status: accepted`)` and its table header at
  `:18-19` is **`ADR | Title | Related FRD`** — three cells, no status column.
  Superseded entries live in a separate `## Superseded and relocated` table at
  `:43-52`.
- **The conventions.** `AGENTS.md:77-118` § ADR conventions: stable IDs, one
  decision per ADR, YAML frontmatter, supersede-don't-renumber, immutable
  published bodies, and the operator-confirmed reserved-block exception at
  `:84-90`.
- **The gate.** The CI `documentation` job (`.github/workflows/ci.yml:71-87`,
  `windows-latest`) runs `scripts/Test-TestMarkdownPlacement.ps1` (`:84`) and
  `scripts/Test-DocumentationLinks.ps1` (`:87`) on every change set — it is "the
  one lane every change set runs, including change sets that touch no
  build-relevant path" (`ci.yml:72-73`).

**No parity-matrix row covers this, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds **46** rows
(`grep -c '^| PAR-'` → 46), each keyed to a Razor page model under
`src/Pegasus.Web/Pages/` with its `OnGet`/`OnPost*` handlers
(`parity-matrix.md:36-38`). ADR authoring is a documentation mechanism, not an
operator-visible surface, so the closest existing repository mechanism — and the
thing this ticket must not break — is the ADR index plus that CI lane, named
above with their line references.

## Findings

- **The reserved block is recorded, not proposed.** `AGENTS.md:84-90` states the
  one operator-confirmed exception (2026-08-23): the native-desktop conversion
  uses ADR-0100–ADR-0110 instead of the next free number "so one-way syncs from
  the still-active upstream `collisionengineers/pegasus` ADR sequence cannot
  collide with conversion ADRs; every other decision keeps taking the next free
  number below ADR-0100."
- **`AGENTS.md` describes an index shape the index does not have.**
  `AGENTS.md:114-115` says the index is "a thin table derived from frontmatter:
  `ID | Title | Status | Superseded-by | Owner capability`". `docs/adr/README.md:18-19`
  is `ADR | Title | Related FRD`. `grep -n 'Owner capability' AGENTS.md` → one
  hit, line 115.
  - This ticket owns the one-line correction; [[FND-007]] (plan handle
    `DSK-00-07`), [[FND-026]] (plan handle `DSK-02-01`) and [[FND-042]] (plan
    handle `DSK-04-01`) all carry the same warning and are told to cite this
    ticket rather than make the edit.
- **The house heading set has drifted, and the newest form is the one to copy.**
  `docs/adr/0014-*.md`, `0015-*.md` and `0025-*.md` open at `## Context`;
  `0028-*.md:13` and `0029-*.md:13` open at `## Status` and then run
  `Context · Decision · Consequences · Options considered · Links`. `AGENTS.md`
  § ADR conventions asks for Status first, "so a body-only read is never mistaken
  for current when it is superseded". Follow ADR-0028/ADR-0029.
- **`related_frd` frontmatter uses lowercase file stems, never the display form.**
  Across `docs/adr/*.md` the values are `[frd-08]`, `[frd-10, frd-11]`,
  `[frd-01, frd-02, frd-05, frd-06, frd-12]` — there is no `[FRD-11]` anywhere.
  Writing the display form is a silent house-style break.
- **The repository already has a worked partial-supersession pattern, and it is
  exactly what ADR-0100 needs for ADR-0009.**
  `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:73-74` carries the clause
  "The future desktop workbench remains deferred until the Web capability is
  complete", and at `:76-77` says "This decision supersedes ADR-0002 **only
  where** ADR-0002 implies…". ADR-0009's own frontmatter is `supersedes: []`
  (`:5`) and ADR-0002 keeps `status: accepted` with an empty `superseded_by`.
  - The contrasting full-supersession form is ADR-0029: `supersedes: [ADR-0013]`
    (`:5`), with ADR-0013 moved into the `## Superseded and relocated` table
    (`docs/adr/README.md:50`). That symmetric consequence — `status: superseded`
    on the old ADR and its removal from the accepted table — is **not** the
    decision here.
- **ADR sizes give the diff estimate a real basis.** `wc -l`: ADR-0014 28,
  ADR-0015 66, ADR-0028 84, ADR-0025 114, ADR-0002 571. Six ADRs each carrying
  the eight-row cloud-justification table land near the 90–110 range.
- **Four of the six numbers are co-claimed by other seeded tickets**, verified on
  the board 2026-08-24:
  - ADR-0100 and ADR-0104 — [[FND-026]] (plan handle `DSK-02-01`, "Author
    ADR-0100 … and ADR-0104 (online-required)").
  - ADR-0105 — [[REL-001]] (plan handle `DSK-09-01`) **and** [[FND-042]] (plan
    handle `DSK-04-01`). Three claimants including this ticket.
  - ADR-0110 — [[TOOL-008]] (plan handle `DSK-12-08`, "Author ADR-0110 —
    agent-skill pinning and the invocation protocol"), whose whole subject is
    that ADR.
  - All are in `backlog`, none taken. [[TOOL-008]] and [[TOOL-002]] already carry
    a `plan` document; [[REL-001]] carries `plan` **and** `open-questions`.
- **[[REL-001]] has already opened the ADR-0105 ownership question as a blocking
  item**, because *its* body instructs an `open-questions` record. Its unticked
  box names the same three claimants and the same tie-break. This ticket's body
  instructs the opposite destination — "record the answer in the ticket plan
  document" — so the record here is in the plan, and [[REL-001]]'s document is
  where the operator's answer is tracked as a gate.
- **The decisions ADR-0105 must state are settled, not open.**
  `docs/desktop/README.md` § Locked decisions: **D-002** (2026-08-23) production
  signing is a self-managed certificate kept in-house and trusted per workstation
  in `LocalMachine\TrustedPeople`; **D-003** (2026-08-23) the update feed is a
  UNC file share on an always-on in-house Windows host served over SMB; **C-01**
  (2026-08-23) the repositories become private, which permanently rules out
  GitHub Releases and GitHub Pages as a feed.
- **ADR-0014 is not superseded.** L-02 keeps Test/UAT local; plan 00 § 3 states
  "**ADR-0014 is not superseded** — Test/UAT is local (L-02)".

### Facts

Everything under **Findings** above is verified by reading the repository or the
board at the `path:line` given, on 2026-08-24. In summary form:

| Fact | Source |
| --- | --- |
| ADR-0001…0029 exist; 0017 never issued; no 01xx file | `ls docs/adr/`; `docs/adr/README.md:53-54` |
| Accepted index columns are `ADR | Title | Related FRD` | `docs/adr/README.md:18-19` |
| `AGENTS.md` describes a different five-column shape | `AGENTS.md:115` |
| Reserved block ADR-0100–0110 is operator-confirmed | `AGENTS.md:84-90` |
| Newest ADRs open at `## Status` | `docs/adr/0028-*.md:13`, `0029-*.md:13` |
| `related_frd` uses lowercase stems | every `related_frd:` line in `docs/adr/*.md` |
| Partial supersession keeps `supersedes: []` | `docs/adr/0009-*.md:5,73-77` vs `0029-*.md:5` |
| ADR-0009's deferral clause is the target sentence | `docs/adr/0009-*.md:73-74` |
| CI documentation lane runs the two scripts | `.github/workflows/ci.yml:84,87` |
| Placement regex allows `docs/(prd|frd|adr|design|desktop)` | `scripts/Test-MarkdownPlacement.ps1:31` |
| D-002, D-003, C-01, L-01, L-02 as stated | `docs/desktop/README.md` § Locked decisions / § Constraints |

### Assumptions

- **A-00-4 — the operator's ADR-0105 ownership answer will not change the ADR's
  content, only its author.** All three claimants name the same single path
  `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` and the same
  two-layer decision. *Confirmed by:* the operator's recorded answer, tracked on
  [[REL-001]]'s `open-questions` document. *Breaks if:* the operator wants
  ADR-0105 split (for example distribution separated from the version gate),
  which would make it two ADRs and change every claimant's scope.
- **A-00-5 — `status: accepted` is correct for all six at first merge.** The
  plan set marks ADR-0108 as the only `proposed` one ([[FND-007]]). *Confirmed
  by:* a reviewer agreeing each decision is settled — D-001, D-002, D-003, L-01,
  L-02 all are. *Breaks if:* one of the six turns out to depend on evidence that
  does not exist yet, in which case it merges `proposed` and gets no index row
  until acceptance (the shape [[FND-007]] uses).
- **A-00-6 — coordinating [[FND-010]]'s D-001 text and [[FND-013]]'s
  "prior documents" sentence into this PR is cheaper than a superseding ADR.**
  ADR bodies are immutable once published (`docs/adr/README.md:12-14`), so a
  later addition to ADR-0100 would need a whole new ADR. *Confirmed by:* the
  owners of [[FND-010]] and [[FND-013]] agreeing to supply their text for this
  PR. *Breaks if:* [[FND-010]]'s upstream-freeze agreement is not yet settled
  enough to write, in which case ADR-0100 must say what is decided and leave the
  execution record to `docs/operations.md`.

## Execution placement

**This ticket places no responsibility anywhere: it authors documents.** The one
placement it assumes is that the ADR files live in this repository under
`docs/adr/`, governed by the CI documentation lane — no runtime work moves, and
no Azure resource is touched. The six-question cloud-justification test is
therefore answered *inside each ADR*, per decision, and not for the ticket.

For the implementer, the placement each ADR must record — with the decision that
already fixed it, so no answer is re-argued:

| ADR | Responsibility it places | Where it lands, and why |
| --- | --- | --- |
| ADR-0101 | The placement rule itself | Adopts the six-question test as the repository's rule; places nothing of its own (relates ADR-0002) |
| ADR-0103 | Data access | The gateway (`Pegasus.Web` evolved in place, L-01). "Shared authority" and "central enforcement" are **yes**; workstations never reach the database (relates ADR-0002, ADR-0015) |
| ADR-0104 | Application state | The desktop, with a bounded local cache and no replication. All six answers are honestly **no** for the cache itself |
| ADR-0105 | A production signing credential, and an update feed | **Not Azure.** "Protected credentials" is **yes** and is satisfied by an in-house signing host under **D-002**; the feed is an always-on in-house Windows host serving a UNC share over SMB under **D-003**. A "yes" names *where* the responsibility lands, and here it lands in-house — C-01 rules out anonymous public hosting permanently |
| ADR-0110 | Agent-skill provenance | The repository (lockfile plus vendored revisions); places nothing at runtime |
| ADR-0100 | The client itself | The desktop. Records the reserved block, the ADR-0009 deferral-clause supersession, and the D-001 consequence |

"It is already in Azure", "the web app does it" and "it may scale later" are not
answers, and an all-no conclusion reached by answering dishonestly is the failure
this section exists to catch.

## Implications

1. **Write the two governance corrections into the same PR as the ADRs.** The
   `AGENTS.md:115` index-shape sentence and the plan 00 § 8 ADR-0009 row both
   instruct something this ticket then contradicts in the tree. Correcting them
   here is what keeps the plan, `AGENTS.md` and the ADRs telling one story; three
   sibling tickets are told to cite this ticket instead of editing.
2. **The collision check must be executable, not prose.** `ls docs/adr/010*`
   covers all six numbers in one command and is the difference between "we
   agreed who authors it" and "we know whether it is already there". Run it
   before writing anything, and record the result.
3. **Everything ADR-0100 will ever say must be in it before it merges.** The
   immutable-body rule turns a forgotten sentence into a new ADR. That makes
   step 7 (coordinating [[FND-010]] and [[FND-013]]) a merge-blocking decision,
   not a nicety.
4. **Follow the file, not the sentence, on the index shape** — three cells, in ID
   order — or the six new rows will not parse against the existing table.
5. **The partial-supersession form is already settled by precedent**: keep
   `supersedes: []` in ADR-0100 and write the clause-level supersession as a
   sentence in `## Context`, leaving ADR-0009 untouched in body and frontmatter.
   The alternative (`supersedes: [ADR-0009]`) carries a symmetric consequence —
   `status: superseded` on ADR-0009 and its removal from the accepted table —
   that is not the decision.

## Open questions

- **Which of the three claimants authors ADR-0105.** An ownership question for
  the operator to settle before Phase 2. It is tracked as a blocking unticked box
  on [[REL-001]]'s `open-questions` document, because *that* ticket's body
  instructs the record there; **this** ticket's body instructs the answer be
  recorded in its plan document, so the plan's *Risks / open questions* section
  holds it here. The body's tie-break (first ticket worked authors the file; the
  others verify and extend in place, never a second file) makes this ticket
  executable while the answer is outstanding — so it is not written as a blocking
  item on this ticket.
- **Whether [[FND-010]] and [[FND-013]] can supply their ADR-0100 text in time
  for this PR** (see A-00-6). The plan takes the default of coordinating them in,
  and records the choice.
- Not open, and not to be reopened: the reserved block itself (operator, 2026-08-23,
  `AGENTS.md:84-90`); D-002 and D-003; whether ADR-0014 is superseded (it is not,
  L-02).
