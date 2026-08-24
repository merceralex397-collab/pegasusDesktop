# Files — FND-005

Surveyed 2026-08-24 against the working tree at `origin/main`
`191ddf334208b8966dc5e32f4f597e434a086233`. Every path below was confirmed with
`ls` or `grep`; the six ADR files are the only ones that do not exist yet, and
they are created by this ticket.

## Where the change lands

| Path | Why |
| --- | --- |
| `docs/adr/0100-native-winui3-desktop-client.md` | **New.** Native WinUI 3 client converted inside this fork, no WebView shell. Also carries: the reserved block ADR-0100…ADR-0110 and its 2026-08-23 operator confirmation; the ADR-0009 **deferral-clause** supersession as a `## Context` sentence with `supersedes: []` left empty; the decided D-001 consequence (owed by [[FND-010]], plan handle `DSK-00-10`); and the "the proposal's three prior documents are not in the repository and are not an input" sentence (owed by [[FND-013]], `DSK-00-13`). Co-claimed by [[FND-026]] (`DSK-02-01`) at this exact path — one ADR ID, one file |
| `docs/adr/0101-local-execution-cloud-authority-split.md` | **New.** The local-execution / cloud-authority split, and adoption of the six-question cloud-justification test as the repository's placement rule. Must state that **ADR-0014 is not superseded** (step 9) |
| `docs/adr/0103-gateway-not-direct-database-access.md` | **New.** Workstations never connect to the database; the gateway is `Pegasus.Web` evolved in place under L-01. Must also state that ADR-0014 is not superseded (step 9) |
| `docs/adr/0104-online-required-no-offline-replication.md` | **New.** Online-required, bounded local cache, no replication. Co-claimed by [[FND-026]] (`DSK-02-01`) |
| `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` | **New.** Two-layer version enforcement (App Installer `UpdateBlocksActivation` **plus** the gateway minimum-client-version gate that fails closed), the D-002 self-managed certificate trusted in `LocalMachine\TrustedPeople`, the D-003 in-house UNC feed over SMB. Relates ADR-0007, which is unchanged. **This exact filename is the single agreed path**, named by the plan set at `docs/desktop/04-auth-session-update-and-startup/README.md:297`; co-claimed by [[REL-001]] (`DSK-09-01`) and [[FND-042]] (`DSK-04-01`) |
| `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md` | **New.** Skill pinning by revision, the vendored tree, and the invocation/review protocol; relates `skills-lock.json`. Co-claimed by [[TOOL-008]] (`DSK-12-08`), whose whole subject is this ADR — whichever ticket runs first authors it, the other extends in place |
| `docs/adr/README.md` | **Edit.** Six rows appended in ID order after the ADR-0029 row at `:41`, inside the accepted table (`:16-41`). **Three cells per row** — `| [0100](0100-native-winui3-desktop-client.md) | … | — |` — matching the header at `:18`, not the five-column shape `AGENTS.md:115` describes |
| `AGENTS.md` | **Edit, one line.** `:114-116` § ADR conventions describes the index as `ID \| Title \| Status \| Superseded-by \| Owner capability`; `docs/adr/README.md:18` actually has `ADR \| Title \| Related FRD`. This ticket owns the correction — [[FND-007]], [[FND-026]] and [[FND-042]] carry the same warning and cite this ticket instead of making the edit |
| `docs/desktop/00-governance-and-workflow/README.md` | **Edit, one line.** § 8 row at `:423` still instructs an ADR-0009 `superseded_by` frontmatter note; step 6 resolves that into a `## Context` sentence in ADR-0100 with ADR-0009 untouched. The row must say the same or the plan and the tree disagree |

## Context files

Read these before writing a line. Each says what it tells the implementer, not
merely that it is relevant.

| Path | What it tells the implementer |
| --- | --- |
| `AGENTS.md:77-116` § ADR conventions | The whole contract: stable IDs and the reserved-block exception at `:84-89` (**never take "the next free number"** — that is what would collide with upstream); one decision per ADR at `:90-91`; the exact YAML frontmatter keys at `:95-105`; the template `Status · Context · Decision · Consequences · Options considered (optional) · Links` at `:107-110`. Note `:107-108`: **Status is stated first** so a body-only read cannot be mistaken for current. And `:114-116` is the sentence this ticket corrects — read it knowing it is wrong |
| `docs/adr/README.md:10-14` | The two facts that make ADR-0100 a one-way door: every ADR carries the eight frontmatter keys, and **published bodies are immutable — a changed decision needs a new superseding ADR**. Anything ADR-0100 must ever say has to be in it before it merges |
| `docs/adr/README.md:16-41` | The accepted table's real shape: header `\| ADR \| Title \| Related FRD \|` at `:18`, separator at `:19`, 22 rows `:20-41` in ID order, each linking by bare relative filename. Copy this shape; do not copy `AGENTS.md:115` |
| `docs/adr/README.md:43-52` | The *separate* superseded/relocated table. This ticket touches none of it — and `:48`/`:52` record why governance decisions were moved **out** of the ADR set ("governance is not an ADR"), which is why the two one-line corrections above go to `AGENTS.md` and the plan rather than into an ADR |
| `docs/adr/README.md:57-58` | ADR-0017 was never issued and the number is not reused. The precedent that a gap in the sequence is deliberate — relevant because this ticket deliberately skips 0102 and 0106…0109 ([[FND-006]]) and 0108 ([[FND-007]]) |
| `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:1-10` | The frontmatter of the ADR being partially superseded: `supersedes: []`, `superseded_by: []`, `status: accepted`. **Leave every one of these untouched** — the whole point of step 6 |
| `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:74-75` | The exact deferral clause ADR-0100 supersedes: "The future desktop workbench remains deferred until the Web capability is complete." Quote it; do not paraphrase |
| `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:77-80` | **The precedent for how to write a partial supersession here**: prose in the body ("This decision supersedes ADR-0002 only where…"), with the superseded ADR left `accepted` and still in the index. Copy this pattern exactly |
| `docs/adr/0015-host-web-on-container-apps-consumption.md` | House style for a short, modern ADR: `## Context` `:16`, `## Decision` `:28`, `## Consequences` `:53`. The target length and tone to match |
| `docs/adr/0029-image-initiated-case-projection.md` | The most recent ADR and the fullest heading set — `## Status` `:13`, `## Context` `:19`, `## Decision` `:27`, `## Consequences` `:45`, `## Links` `:54`. The closest model for the six written here |
| `docs/adr/0002-dotnet-modular-monolith-on-azure.md:1-16` | What a *long* ADR looks like and why not to imitate it (17 headings, 554 lines). Its `- Status:` line at `:12` also shows how a partial supersession is signalled in a body without frontmatter churn. It is the ADR that ADR-0101 and ADR-0103 relate to |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table and cloud-justification table) | The per-ADR content brief: which decision each number carries, its one-line context, and what it supersedes or relates. Also the Appendix A six-question table verbatim, which goes inside each ADR's `## Context` |
| `docs/desktop/README.md` § Locked decisions | D-002 (self-managed certificate, `LocalMachine\TrustedPeople`), D-003 (in-house UNC share over SMB), C-01 (repositories become private — GitHub Releases and Pages permanently ruled out), L-01, L-02. ADR-0105 is unwritable without these three; ADR-0101/0103 without L-01 and L-02 |
| `src/Pegasus.Infrastructure/DependencyInjection.cs:53` | `AddDbContextFactory<PegasusDbContext>` — the single place the database context is composed. Together with `src/Pegasus.Web/Program.cs:549` and `src/Pegasus.Worker/WorkerDependencyInjection.cs:150` it is the measured fact behind ADR-0103's "workstations never connect directly" |
| `src/Pegasus.Web/Program.cs:112-116`, `:640-660` | The `Features:*` composition-gate pattern (a flag that *refuses to compose* outside its runtime profile rather than merely hiding a screen). The expand/contract mechanism ADR-0104 and ADR-0105 assume for shipping behind a gate |
| `skills-lock.json` and `docs/desktop/12-agent-tooling/skills.lock.draft.json` | What ADR-0110 is deciding about: the live 29-line lockfile shape (`source`, `sourceType`, `skillPath`, `computedHash`) and the 382-line draft that [[TOOL-002]] (`DSK-12-02`) promotes. ADR-0110 must describe the mechanism these files implement, not invent a second one |
| `scripts/Test-DocumentationLinks.ps1:1-14` | The link gate: **no parameters**; relative links only, external URLs and anchors unchecked; fenced and inline code stripped before scanning; `.agents`, `.codex`, `.kanmer`, `.git` excluded. Consequence: a cross-ADR link inside a fenced block is **not** verified, so put real links outside fences |
| `scripts/Test-MarkdownPlacement.ps1:2-5`, `:31` | `-Base` and `-Head` are **mandatory** — a bare call fails on the argument, not on a violation. `:31` is the allowed-roots regex; `docs/adr/*.md` passes |
| `.github/workflows/ci.yml:70-87` | The `documentation` job — "the one lane every change set runs" — on `windows-latest`, calling `Test-TestMarkdownPlacement.ps1` then `Test-DocumentationLinks.ps1`. What must be green before merge |
| Kanmer group doc `HZN-001/board-conventions.md` (`get_group_doc HZN-001 board-conventions.md`) | The id rule: a bare `<PREFIX>-<nnn>` is a **fork board id**; an upstream id is written `upstream <ID>`; and the 19-row join table. Read before writing any id into an ADR |

## Ripple effects

- **`docs/adr/README.md` is the only index that must follow.** `docs/index.md`
  links the ADR *index* (`:21`, `:46`) and just one individual ADR (`:56`,
  ADR-0029), so six new files create no dangling reference there and it needs no
  edit. Verified with `grep -n "adr" docs/index.md`.
- **No contract, generated client or OpenAPI snapshot ripples.** There is no
  `openapi/` directory in the repository today (`ls openapi` → *No such file or
  directory*); the gateway contract work that creates one belongs to area 03.
  This ticket changes no `src/` file, so nothing regenerates.
- **No test ripples.** `tests/Pegasus.ArchitectureTests`,
  `tests/Pegasus.Core.Tests` and `tests/Pegasus.IntegrationTests` assert on code,
  not on documentation. The only checks that follow are the two CI documentation
  scripts.
- **Board ripple, and it is the one to watch.** Step 12 `link_doc`s the new ADRs
  to the tickets they govern and clears `docs_todo` where the governing document
  now exists. That swaps *which* document satisfies a `feature` ticket's
  `leave-backlog` gate. Clearing `docs_todo` on a ticket whose ADR is **not**
  among these six would make that ticket un-leaveable from `backlog` — clear it
  only where a real, merged path now exists, and re-probe with `get_doc_gates`
  afterwards.
- **Downstream tickets that cite these ADRs as settled** can stop writing the
  "New ADR" paragraph once they merge: [[FND-008]] (`DSK-00-08`, FRD-13 on top of
  these), [[FND-010]], [[FND-013]], [[FND-042]], [[FEAT-038]] (`DSK-07-12`) and
  [[TOOL-008]] are the tickets this one blocks.
- **Every later upstream sync must re-check the ADR namespace.** Upstream keeps
  issuing numbers below 0100; [[FND-002]] (`DSK-00-02`) step 8 and [[FND-051]]
  (`DSK-01-13`) carry that standing check. Adding rows here does not change it,
  but it is why the block exists.

## Out of scope

Recorded so the reviewer sees each was a decision, not an oversight. The
ticket's Guardrails already forbid them.

- **ADR-0102, ADR-0106, ADR-0107, ADR-0109** — the flow-derived ADRs, authored by
  [[FND-006]] (plan handle `DSK-00-06`) from the area 01 flow records. Not
  written here even though they sit inside the same reserved block.
- **ADR-0108** (isolated WebView2 report rendering) — authored `proposed` by
  [[FND-007]] (`DSK-00-07`) and accepted only after the Phase 7 spike. It is the
  one ADR in the block that is not `accepted` on creation.
- **`docs/adr/0009-adopt-pegasus-monorepo-workspaces.md`** — untouched, **body
  and frontmatter**. The deferral-clause supersession is recorded in ADR-0100's
  `## Context` instead. ADR-0016 (standalone WinForms email evaluator) is
  likewise unchanged.
- **`docs/adr/0014-local-to-production-deployment.md`** — not superseded. L-02
  keeps Test/UAT local and no Azure dev/test/staging is created; ADR-0101 and
  ADR-0103 say so explicitly rather than leaving it inferable.
- **FRD-13, the PRD scope update and the `DSK` capability family** — all belong
  to [[FND-008]]. No `docs/frd/`, `docs/prd/` or `docs/capabilities.md` edit here.
- **`docs/index.md`, `docs/operations.md`, `docs/current-architecture.md`,
  `docs/engineering.md`, `docs/boundaries.md`** — no edit. The D-001 text in
  `docs/operations.md` belongs to [[FND-010]]; the release-tag and upstream-sync
  sentences in `docs/engineering.md` belong to [[FND-009]] and [[FND-002]].
- **All of `src/`, `tests/`, `scripts/`, `.github/`, `.codex/`, `.agents/`,
  `.grok/`** — no code, no test, no workflow, no skill file. This is a
  documentation-only branch and its simplification pass records `n/a — docs-only`.
- **Any Azure resource** — no read is needed and no write is permitted.
