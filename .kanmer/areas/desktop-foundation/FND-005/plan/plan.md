# Plan — FND-005: Author ADR-0100, ADR-0101, ADR-0103, ADR-0104, ADR-0105 and ADR-0110 in the reserved block

**Diff estimate: ~9 files, ~560 lines.**

Derived from the `files` document, not asserted. Six new ADRs at the measured
house length — the six most recent ADRs are 60, 61, 66, 70, 84 and 153 lines
(`wc -l docs/adr/00{15,24,26,27,28,29}-*.md`, 2026-08-24) — plus the ten-line
six-question table each one must carry inside `## Context`. Budget: ADR-0100
~110 (it also carries the reserved block, the ADR-0009 clause sentence, the
D-001 consequence and the prior-documents sentence), ADR-0105 ~100 (two
enforcement layers, D-002, D-003, C-01), ADR-0101/0103 ~85 each, ADR-0110 ~85,
ADR-0104 ~80 — **~545 lines of new ADR**. Then 6 index rows in
`docs/adr/README.md`, and two one-line corrections
(`AGENTS.md:114-116`, `docs/desktop/00-governance-and-workflow/README.md:423`)
that reflow to ~3 lines each at the files' hard wrap. Nine files touched, six of
them new.

## Approach

Write all six in one branch and one PR, in the order ADR-0101 → ADR-0103 →
ADR-0104 → ADR-0105 → ADR-0110 → ADR-0100. That order is deliberate and is the
one substantive choice in this plan: ADR-0100 is the only file that must carry
text three other tickets own, and it is the file whose body becomes immutable
the moment it merges (`docs/adr/README.md:12-14`). Writing it **last**, after
the other five have fixed their own wording, is what makes it possible to check
that ADR-0100's `## Consequences` actually agrees with them before the one-way
door closes.

The rejected alternative was one PR per ADR, taking ADR-0100 first as the
"foundation". It reads more naturally and it is worse: ADR-0100 would merge
before ADR-0105 had settled how the version gate is described, and correcting an
immutable body afterwards costs a whole new superseding ADR. The second rejected
alternative was writing `supersedes: [ADR-0009]` in ADR-0100's frontmatter — see
step 6; the repository's own precedent at `docs/adr/0009-…:77-80` writes a
partial supersession as prose while leaving both ADRs' frontmatter empty and
both `accepted`, and the frontmatter form would import a consequence
(`status: superseded` on ADR-0009, removal from the accepted index) that the
decision does not intend.

## Governing docs

The ticket's `refs` is empty and `docs_todo: true` — confirmed by
`get_doc_gates FND-005`, whose `leave-backlog` requirement `governing-doc` reads
`satisfied: true` on the strength of `docs_todo`, and whose `docs_todo` field
reads `true`. No repository ADR governs this work today, because the documents
this ticket governs itself with are the ones it is writing.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client converted inside this
> fork), **authored by this ticket**; ADR-0100 is co-claimed by [[FND-026]]
> (plan handle `DSK-02-01`), so where another document must name an author it
> writes `authored by [[FND-005]]; see [[FND-005]]'s plan for the ownership
> reconciliation`. ADR-0104 is co-claimed the same way. ADR-0105 has three
> claimants — this ticket, [[REL-001]] (plan handle `DSK-09-01`) and [[FND-042]]
> (plan handle `DSK-04-01`) — and ADR-0110 two — this ticket and [[TOOL-008]]
> (plan handle `DSK-12-08`); see the reconciliation in step 2 below.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR set table and
> the cloud-justification table) and in `docs/desktop/README.md` § Locked
> decisions (L-01, L-02, D-002, D-003, C-01). If a decision lands differently
> this plan is revised before implementation.

Because `refs` is empty, the authorities that actually bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `AGENTS.md:81-89` § ADR conventions | Stable IDs; never renumber, reuse or delete; and the one operator-confirmed exception — the conversion uses the reserved block ADR-0100–ADR-0110 instead of the next free number | Steps 1, 3, 6 |
| `AGENTS.md:90-91` | One decision per ADR, not a bundle | Step 8 (each ADR carries exactly one) |
| `AGENTS.md:95-105` | The eight-key YAML frontmatter block, verbatim in shape | Step 4 |
| `AGENTS.md:107-110` | Template `Status · Context · Decision · Consequences · Options considered (optional) · Links`, Status stated first | Step 5 |
| `AGENTS.md:111-113` | Keep ADRs durable — no dated cost tables or runbooks; feature behaviour goes in an FRD | Step 8, and the Out-of-scope line barring FRD-13 content |
| `AGENTS.md:114-116` | The index shape sentence — **wrong**, and this ticket owns the correction | Step 10a |
| `docs/adr/README.md:10-14` | Every ADR carries the eight frontmatter keys; **published bodies are immutable**; a changed decision needs a new superseding ADR | Steps 4, 7 (and the whole ordering rationale in Approach) |
| `docs/adr/README.md:16-41` | The accepted table's real three-column shape and ID ordering | Step 10 |
| `docs/adr/0009-…:74-75` | The exact deferral clause ADR-0100 supersedes | Step 6 |
| `docs/adr/0009-…:77-80` with `:5` and `docs/adr/0002-…:5-6` | The repository's precedent for a **partial** supersession: prose in the body, `supersedes: []` in frontmatter, the superseded ADR left `accepted` and in the index | Step 6 |
| Proposal § 26 Documentation set (Decisions), § 27 item 18 | The conversion needs a recorded decision set, and every deviation needs an ADR | The whole ticket |
| Proposal § 4 / Appendix A | The six-question cloud-justification test, answered — never prose | Step 5 |
| Proposal § 1, § 6.3, § 9, § 10.1, § 11, § 20 | The content of ADR-0100, 0105, 0103, 0104 and 0110 respectively | Step 8 |
| L-01 (`docs/desktop/README.md`) | The gateway is `Pegasus.Web` evolved in place; no new deployment unit | ADR-0103, step 8 |
| L-02 | Test/UAT is a local production-mimicking stack; **ADR-0014 stands** | Step 9 |
| D-002 (decided 2026-08-23) | Self-managed certificate, kept in-house, trusted per workstation in `LocalMachine\TrustedPeople` | ADR-0105, step 8 |
| D-003 (decided 2026-08-23) | Update feed is an in-house UNC share served to App Installer over SMB; no Azure write | ADR-0105, step 8 |
| C-01 (2026-08-23) | The repositories become private; GitHub Releases and Pages are ruled out permanently | ADR-0105, step 8 |
| `scripts/Test-MarkdownPlacement.ps1:31` and `.github/workflows/ci.yml:70-87` | New Markdown only under the allowed roots; the `documentation` job is the lane every change set runs | Step 11 |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § "Ticket template" requires
of the plan document specifically.

- **Subagent**: `pegasus-parity-researcher` —
  `.codex/agents/pegasus-parity-researcher.toml` (verified present). Read-only
  evidence gathering for each ADR's `## Context`; it cannot write files, so its
  answer is transcribed into the ADR by the ticket owner.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`) → `microsoft-docs` (Microsoft Learn
  plugin) for any API claim.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `link_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-005` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's thirteen implementation steps; the order, the ownership
and the file paths are the body's. Measured values below were read on
2026-08-24 and are what to *compare against*, not to copy into the proof.

1. **Orient, then run the existence check before writing anything.** Read
   `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR set table and
   the cloud-justification table), `AGENTS.md:77-116`, and two existing ADRs for
   house style — `docs/adr/0015-host-web-on-container-apps-consumption.md` (66
   lines; `## Context` `:16`, `## Decision` `:28`, `## Consequences` `:53`) and
   `docs/adr/0029-image-initiated-case-projection.md` (60 lines; `## Status`
   `:13`, `## Context` `:19`, `## Decision` `:27`, `## Consequences` `:45`,
   `## Links` `:54`). Call `get_doc_gates FND-005` — expect `leave-backlog:
   [governing-doc]` satisfied by `docs_todo`, and `leave-preparing: [research,
   files, plan, checklist, questions-resolved]`. Then `take_ticket`.
   Now run the one command that covers all six numbers and **record its output
   verbatim**:
   ```
   ls docs/adr/010*
   ```
   Measured 2026-08-24: `No such file or directory` — nothing exists, so this
   ticket authors all six. If an ADR-0110 file is there, [[TOOL-008]] authored it:
   verify it covers step 8's skill-pinning content and **extend it in place**. If
   ADR-0100 or ADR-0104 is there, [[FND-026]] authored it — same rule. If
   ADR-0105 is there, [[REL-001]] or [[FND-042]] authored it — same rule. Never a
   second file for one number.
2. **Settle ownership, and record it here rather than deciding it silently.**
   Co-claims, resolved with `search_items` on 2026-08-24 (read, never computed):
   ADR-0100 and ADR-0104 → [[FND-026]] (`DSK-02-01`); ADR-0105 → [[REL-001]]
   (`DSK-09-01`) **and** [[FND-042]] (`DSK-04-01`); ADR-0110 → [[TOOL-008]]
   (`DSK-12-08`). Two authors on one ADR ID is a stop condition — IDs are stable
   and are never renumbered (`AGENTS.md:81-83`).
   All claimant bodies already agree on the same two points, so nothing here is
   in dispute: **one filename per number** — for ADR-0105 that is
   `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`, the only
   ADR-0105 path the plan set names
   (`docs/desktop/04-auth-session-update-and-startup/README.md:297`); for
   ADR-0110 that is `docs/adr/0110-agent-skill-pinning-and-invocation-protocol.md`;
   for ADR-0100 that is `docs/adr/0100-native-winui-3-client-in-the-fork.md` — and
   **one rule**: whichever ticket is worked first authors the file, and the
   others verify that it covers their content and extend it in place.
   **ADR-0105 ownership is settled.** On 2026-08-24 the operator assigned authorship to FND-005. This ticket owns `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`; the other claimants link to or verify that canonical file and do not create another ADR-0105.
3. **Create the six files** under `docs/adr/` using the existing
   `NNNN-kebab-title.md` pattern, in the order the Approach gives:
   `0101-local-execution-cloud-authority-split.md`,
   `0103-gateway-not-direct-database-access.md`,
   `0104-online-required-bounded-local-cache.md`,
   `0105-msix-app-installer-and-minimum-version-gate.md`,
   `0110-agent-skill-pinning-and-invocation-protocol.md`, and last
   `0100-native-winui-3-client-in-the-fork.md`.
4. **Frontmatter, verbatim in shape** from `AGENTS.md:95-105` — eight keys, in
   this order, no tabs and no smart quotes:
   ```yaml
   ---
   id: ADR-0100
   status: accepted
   date: <the date it is accepted>
   supersedes: []
   superseded_by: []
   related_capabilities: []
   related_frd: []
   tags: []
   ---
   ```
   `status: accepted` for all six — ADR-0108 is the only `proposed` one in the
   block and it belongs to [[FND-007]] (`DSK-00-07`). Use real `tags` in the
   house idiom (`docs/adr/0002-…:9` is `tags: [architecture, stack, hosting]`;
   `docs/adr/0009-…:9` is `tags: [architecture, workspaces]`).
5. **Headings and the six-question table.** Use
   `Status · Context · Decision · Consequences · Options considered · Links`,
   with **Status first** (`AGENTS.md:107-110` — so a body-only read cannot mistake
   a superseded ADR for current). Put the Appendix A table verbatim inside
   `## Context`:
   | Question | Answer (yes/no) | Evidence |
   | --- | --- | --- |
   | Shared authority — must several users see and update the same state? | | |
   | Unattended execution — must it run with every desktop closed? | | |
   | Protected credentials — long-lived secret that must not sit on workstations? | | |
   | Public callback — must an external service call a stable public endpoint? | | |
   | Central enforcement — revocation, permissions, audit, invariant independent of the client? | | |
   | Measured operational advantage — measured evidence central is materially better? | | |
   **Fill every cell.** All six "no" means the responsibility belongs in the
   desktop. A "yes" **names the host it lands on** — and on this programme that
   host is frequently in-house: D-003's always-on Windows host satisfies
   "unattended execution" and D-002's in-house signing host satisfies "protected
   credentials" exactly as a cloud service would. "It is already in Azure", "the
   web app does it" and "it may scale later" are not answers, and neither is
   treating a "yes" as a reason to reach for Azure. The six worked answer sets,
   with their evidence, are in this ticket's `research` document under
   *Execution placement* — transcribe them and re-verify each cited `path:line`
   as you go.
6. **ADR-0100's two extra obligations.**
   (a) Restate the reserved block ADR-0100…ADR-0110 and the operator confirmation
   of 2026-08-23, citing `AGENTS.md:84-89` where it is already recorded.
   (b) Record that ADR-0100 supersedes **only** the deferral clause of ADR-0009 —
   quote it from `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:74-75`
   ("The future desktop workbench remains deferred until the Web capability is
   complete"), leaving ADR-0009's workspaces decision and ADR-0016 unchanged.
   **The agreed mechanism, stated identically by [[FND-026]] step 3: keep
   `supersedes: []` in ADR-0100's frontmatter and write the clause-level
   supersession as a sentence in `## Context`.** This is the repository's own
   pattern — `docs/adr/0009-…:77-80` supersedes ADR-0002 only in part, in prose,
   while `docs/adr/0009-…:5` keeps `supersedes: []`, `docs/adr/0002-…:5-6` keeps
   `superseded_by: []` and `status: accepted`, and ADR-0002 stays in the accepted
   index at `docs/adr/README.md:21`. Do **not** write
   `supersedes: [ADR-0009]`: in frontmatter that is the *full*-supersession
   relation, whose symmetric consequence is `status: superseded` on ADR-0009 and
   its removal from the accepted table — not the decision. **ADR-0009 is left
   untouched, body and frontmatter.**
7. **Write ADR-0100's `## Consequences` so it already carries the two texts other
   tickets owe it** — the decided D-001 ([[FND-010]], plan handle `DSK-00-10`)
   and the "the proposal's three prior documents are not in the repository and
   are not an input" sentence ([[FND-013]], `DSK-00-13`). A published ADR body is
   immutable, so a later edit would need a whole new superseding ADR.
   **Default taken here, and recorded rather than drifted into: fold both texts
   into this PR**, agreeing the exact wording with those two tickets before it
   opens — D-001's text from `docs/desktop/README.md` § Locked decisions
   (Option A: the fork becomes the single release source at the first production
   gateway change; upstream merged one final time, then frozen), and the
   prior-documents sentence from
   `docs/desktop/00-governance-and-workflow/README.md` § 3, which already records
   that the proposal's three prior documents (§ 2 item 5) are not in the
   repository and are not an input to any ticket. If the operator would rather
   those two tickets landed separately, record that choice **and its cost** here
   before ADR-0100 merges.
8. **Content per ADR**, from plan 00 § 3's ADR set table — one decision each,
   never a bundle:
   - **ADR-0101** — the local-execution / cloud-authority split, and adoption of
     the six-question test as the repository's placement rule. Relates ADR-0002.
   - **ADR-0103** — workstations never connect to the database; the gateway is
     `Pegasus.Web` evolved in place under L-01. Cite the measured single owner:
     the context is composed only at
     `src/Pegasus.Infrastructure/DependencyInjection.cs:53`
     (`AddDbContextFactory<PegasusDbContext>`), reached from
     `src/Pegasus.Web/Program.cs:549` and
     `src/Pegasus.Worker/WorkerDependencyInjection.cs:150`. Relates ADR-0002,
     ADR-0015.
   - **ADR-0104** — online-required, a bounded local cache, no replication.
   - **ADR-0105** — the **two-layer** enforcement (App Installer
     `UpdateBlocksActivation` **plus** a gateway minimum-client-version gate that
     fails closed), the D-002 self-managed certificate trusted per workstation in
     `LocalMachine\TrustedPeople`, and the D-003 in-house UNC feed over SMB.
     Relates ADR-0007, whose gateway release route is unchanged.
   - **ADR-0110** — skill pinning by revision, the vendored tree, and the
     invocation/review protocol. Describe the mechanism the repository already
     has — `skills-lock.json` at the root (29 lines; `source`, `sourceType`,
     `skillPath`, `computedHash`) and the 382-line
     `docs/desktop/12-agent-tooling/skills.lock.draft.json` that [[TOOL-002]]
     (`DSK-12-02`) promotes — rather than inventing a second one.
   - **ADR-0100** — as steps 6 and 7.
9. **State explicitly in ADR-0101 and ADR-0103 that ADR-0014 is not superseded.**
   Test/UAT stays local under L-02 and no Azure dev/test/staging is created. Do
   not leave this inferable; a reader who infers the opposite creates an Azure
   environment.
10. **Add one row per ADR to `docs/adr/README.md`**, in ID order, appended after
    the ADR-0029 row at `:41` inside the accepted table. **Three cells**, matching
    the header at `:18` — `| [0100](0100-native-winui-3-client-in-the-fork.md) | Native WinUI 3 desktop client in the fork | — |` —
    linking by bare relative filename as every existing row does. Do not touch the
    `## Superseded and relocated` table at `:43-52`.
    **10a.** In the same PR, correct the two governance sentences this ticket
    owns: `AGENTS.md:114-116`, so the index-shape sentence describes
    `ADR | Title | Related FRD` (what `docs/adr/README.md:18` actually has)
    instead of `ID | Title | Status | Superseded-by | Owner capability`; and
    `docs/desktop/00-governance-and-workflow/README.md:423`, so the § 8 row no
    longer instructs an ADR-0009 `superseded_by` frontmatter note and instead
    says the clause-level supersession is recorded in ADR-0100's `## Context`
    with ADR-0009 untouched. [[FND-007]], [[FND-026]] and [[FND-042]] carry the
    same warning and cite this ticket rather than making either edit.
11. **Run the gates**, the same two the CI `documentation` job runs at
    `.github/workflows/ci.yml:82-87`:
    ```
    pwsh ./scripts/Test-DocumentationLinks.ps1
    pwsh ./scripts/Test-TestMarkdownPlacement.ps1
    ```
    Both exit 0. Two mechanics worth knowing:
    `Test-DocumentationLinks.ps1` takes **no** parameters
    (`scripts/Test-DocumentationLinks.ps1:8-9`) and **strips fenced and inline
    code before scanning** (`:4-7`) — a cross-ADR link written inside a fence is
    not checked at all, so put the `## Links` entries outside fences if the gate
    is to prove anything. `Test-MarkdownPlacement.ps1` takes **mandatory**
    `-Base` and `-Head` (`:2-5`); CI calls the wrapper
    `Test-TestMarkdownPlacement.ps1`, which takes none. Then confirm every
    frontmatter block parses — no tabs, no smart quotes, eight keys.
12. **Link the ADRs to their tickets.** `link_doc` from this ticket to each new
    path so the `governing-doc` gate is satisfied by a real document, then clear
    `docs_todo` on the conversion tickets whose governing ADR now exists — and
    **only** those. Clearing `docs_todo` on a ticket whose ADR is not among these
    six would leave it unable to leave `backlog`. Re-probe with `get_doc_gates`
    on at least one affected ticket afterwards and record the output.
13. **Open the PR against `dev`** (`gh pr create --base dev`), take the
    independent review from `pegasus-desktop-reviewer`, and record
    `n/a — docs-only` under a dated `## Simplification pass` heading in this plan
    document.

## Verification

Evidence tier **1 — Static/build/architecture** (`docs/engineering.md`
§ Required evidence tiers), as the ticket body states. Documentation consistency
and link integrity are the whole of the claim; no runtime behaviour is proved.

The `post-implementation-report` and then the `proof` (a `command-log`) carry:

| Command | Expected |
| --- | --- |
| `ls docs/adr/010*` — **run before writing**, output recorded | `No such file or directory` (2026-08-24), or the co-claimant file that already exists, with the extend-in-place decision recorded |
| `ls docs/adr/010*` — run after | exactly one file per ADR number; no duplicate for 0100, 0104, 0105 or 0110 |
| `grep -l '^id: ADR-01' docs/adr/*.md` | the six new files, each with `status:` and `date:` present |
| `grep -n '^| \[01' docs/adr/README.md` | **one row per ADR ID** — 0100, 0101, 0103, 0104, 0105, 0110 — and no duplicate ADR-0110 row. Read the rows, not the count: the pattern is a sound reserved-block probe (every existing ADR id begins `00`, so it returns 0 today) but the block reaches **11** rows once [[FND-006]] adds 0102/0106/0107/0109 and [[FND-007]] adds 0108 |
| `grep -n 'Owner capability' AGENTS.md` | no match after step 10a (exactly one match, at `:115`, before it) |
| `grep -n 'superseded_by' docs/desktop/00-governance-and-workflow/README.md` | no match on the § 8 ADR row at `:423` after step 10a |
| `git diff --stat -- docs/adr/0009-adopt-pegasus-monorepo-workspaces.md` | **empty** — ADR-0009 untouched, body and frontmatter |
| `grep -n 'supersedes:' docs/adr/0100-native-winui-3-client-in-the-fork.md` | `supersedes: []` |
| `grep -c 'ADR-0014' docs/adr/0101-*.md docs/adr/0103-*.md` | at least one match in each — ADR-0014 explicitly not superseded |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0, no broken relative link |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |
| `get_doc_gates <a ticket whose docs_todo step 12 cleared>` | `leave-backlog` still `passable: true`, now on the linked ADR rather than on `docs_todo` |

Proof is written on merged `main`, after review and merge — never before
(`AGENTS.md` § Kanmer operating instructions).

## Risks / open questions

- **ADR-0100's body is a one-way door.** `docs/adr/README.md:12-14` — once
  merged, a changed decision needs a new superseding ADR. Mitigation: the
  Approach writes ADR-0100 last, and step 7 takes and records the default of
  folding [[FND-010]]'s and [[FND-013]]'s texts into this PR rather than
  discovering the cost afterwards.
- **A co-claimant lands first.** Assumption A-00-5-1 in the `research` document.
  Mitigation: step 1's `ls docs/adr/010*` is the executable detector, and the
  extend-in-place rule is already identical in every claimant's body. This is a
  scope boundary between named tickets — [[FND-026]], [[REL-001]], [[FND-042]],
  [[TOOL-008]] — not an unsettled question.
- **ADR-0105 ownership is settled.** The 2026-08-24 operator decision assigns authorship to FND-005; other claimants link to or verify its canonical file and do not create another ADR-0105.
- **Writing `supersedes: [ADR-0009]` by reflex.** The likeliest single defect in
  this ticket, because the plan's own § 8 row at `:423` still asks for it.
  Mitigation: step 6 states the mechanism and its precedent, step 10a corrects
  the plan row in the same PR, and the verification table asserts an empty diff
  on ADR-0009.
- **Copying `AGENTS.md:115`'s five-column index shape** into a three-column
  table. Mitigation: step 10 gives the row form literally, and step 10a removes
  the misleading sentence so the next ADR author does not repeat it.
- **A link inside a fenced block looks checked and is not**
  (`scripts/Test-DocumentationLinks.ps1:4-7`). Mitigation: step 11 says to put
  `## Links` entries outside fences.
- **Six ADRs in one review is a lot to read.** Mitigation: they are ordered so
  the reviewer meets the mechanism ADRs before the one that summarises them, and
  each is at the measured house length rather than the 554-line outlier
  `docs/adr/0002-…`.
- **The reserved block must survive every upstream sync.** Upstream keeps issuing
  numbers below 0100; the standing re-check lives on [[FND-002]] (`DSK-00-02`)
  step 8 and [[FND-051]] (`DSK-01-13`), not here.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome for this ticket: `n/a — docs-only`._

## 2026-08-24 ownership decision

The operator assigned **ADR-0105 authorship to FND-005**. This ticket therefore owns the single canonical file `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`; FND-009, FND-040, FND-041, REL work, and TOOL-008 link to or verify the resulting authority and must not create a competing ADR-0105. This resolves the ownership uncertainty recorded in the ticket body without changing the already-settled distribution mechanism.

## 2026-08-24 implementation reconciliation

The canonical paths for this ticket are:

- `docs/adr/0100-native-winui-3-client-in-the-fork.md`
- `docs/adr/0101-local-execution-cloud-authority-split.md`
- `docs/adr/0103-gateway-not-direct-database-access.md`
- `docs/adr/0104-online-required-bounded-local-cache.md`
- `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`
- `docs/adr/0110-agent-skill-pinning-and-invocation-protocol.md`

They resolve the stale alternative filenames in the ticket body and checklist. FND-005 owns this one canonical file for each listed ADR; tickets that cite a listed decision link to or review it and do not create another file with the same ADR ID.

The repository currently has no `origin/dev`; `origin/main` also lacks the already-tracked `docs/desktop/` conversion plan that these ADRs cite. The branch therefore has the clean documented base `task/desktop-plan-segmentation` (commit `ecb9b7b4`), with no changes to that branch. A PR must be retargeted to the project integration branch once FND-001 establishes it.

## Simplification pass — 2026-08-24

n/a — documentation-only. The diff contains the six ticket-owned ADRs, their index rows, and the two explicitly named consistency corrections; it adds no runtime abstraction, compatibility path, deployment unit, or unrelated cleanup.
