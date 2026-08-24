# Research — FND-005: the six reserved-block foundation ADRs

## Question

What must ADR-0100, ADR-0101, ADR-0103, ADR-0104, ADR-0105 and ADR-0110 each
contain; what house style, frontmatter and index shape must they match; which
repository gate checks them; and which other board tickets claim the same ADR
numbers?

## Current behaviour

This is governance and documentation work inside the repository's decision log.
**No parity-matrix row covers it, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` →
**46**, run 2026-08-24), every row keyed to a Razor page model under
`src/Pegasus.Web/Pages/**` (`parity-matrix.md:1-7`). An ADR is not an observable
operator capability, so there is no row to name; inventing one would be a defect.

The closest existing repository mechanism — the thing that does this job today —
is the ADR set and its gate:

- **`docs/adr/`** holds **28** ADR files, `0001`…`0029` with `0017` never issued
  (`docs/adr/README.md:57-58` records the gap as intentional and unreusable).
- **`docs/adr/README.md`** (59 lines) is the index.
  `## Current architecture decisions (`status: accepted`)` at `:16`; a
  **three-column** header `| ADR | Title | Related FRD |` at `:18-19`; 22 accepted
  rows at `:20-41`, last row ADR-0029 at `:41`.
  `## Superseded and relocated` at `:43`; its own three-column header
  `| ADR | Title | Now owned by |` at `:45-46`; 6 rows at `:47-52`.
- **`AGENTS.md:77-116` § ADR conventions** defines stable IDs and the reserved-block
  exception (`:81-89`), one decision per ADR (`:90-91`), the YAML frontmatter block
  (`:95-105`), the template `Status · Context · Decision · Consequences ·
  Options considered (optional) · Links` (`:107-110`), and the index shape
  (`:114-116`).
- **CI** — the `documentation` job (`.github/workflows/ci.yml:70-87`,
  `windows-latest`, described in the file as "the one lane every change set
  runs") executes `./scripts/Test-TestMarkdownPlacement.ps1` (`:82-84`) then
  `./scripts/Test-DocumentationLinks.ps1` (`:85-87`).

## Findings

### Facts

Each verified by reading the repository on **2026-08-24** at `origin/main`
`191ddf334208b8966dc5e32f4f597e434a086233`, with the command that produced it.

- **F1 — none of the six ADRs exists yet.** `ls docs/adr/010*` →
  `No such file or directory`; `grep -l '^id: ADR-01' docs/adr/*.md` → no match.
  Step 1's existence check therefore currently finds nothing, and the co-claim
  reconciliation of step 2 is still live for all four contested numbers.
- **F2 — the index has three columns, not five.** `docs/adr/README.md:18` is
  `| ADR | Title | Related FRD |`, while `AGENTS.md:115` describes
  `ID | Title | Status | Superseded-by | Owner capability`. The file and the
  convention disagree; the ticket body resolves it in favour of the file and
  assigns this ticket the one-line `AGENTS.md` correction.
  `grep -n 'Owner capability' AGENTS.md` → exactly one match, at `:115`.
- **F3 — partial supersession in this repository is written in prose, never in
  frontmatter.** `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:74-75`
  carries the deferral clause ADR-0100 supersedes ("The future desktop workbench
  remains deferred until the Web capability is complete"), and `:77-80` carries
  ADR-0009's own *partial* supersession of ADR-0002 as a prose sentence — while
  `docs/adr/0009-…:5` keeps `supersedes: []`, `docs/adr/0002-…:5-6` keeps
  `supersedes: []` / `superseded_by: []` with `status: accepted`, and ADR-0002
  stays in the accepted table at `docs/adr/README.md:21`. This is exactly the
  precedent step 6 names, and it is load-bearing: writing
  `supersedes: [ADR-0009]` would import the symmetric full-supersession
  consequence (`status: superseded` on ADR-0009, removal from the accepted
  table) that the decision does not intend.
- **F4 — `grep -c '^| \[01' docs/adr/README.md` is a sound reserved-block probe,
  but not a "six new rows" probe.** Every existing ADR id is zero-padded and
  begins `00` (`0001`…`0029`), so the pattern cannot match an existing row;
  measured today it returns **0**. It will not, however, return 6 after this
  ticket: [[FND-006]] (plan handle `DSK-00-06`) adds ADR-0102, 0106, 0107 and
  0109, [[FND-007]] (`DSK-00-07`) adds ADR-0108, and [[TOOL-008]] (`DSK-12-08`)
  may already have added ADR-0110 — a full reserved block is **11** rows. The
  body is right to demand one row *per ID* rather than a total.
- **F5 — `scripts/Test-DocumentationLinks.ps1` takes no parameters** (`:8-9`,
  `[CmdletBinding()] param()`). It fails when a tracked Markdown file holds a
  relative link to a path that does not exist; external URLs and same-file
  anchors are not checked (`:1-3`); fenced and inline code are stripped before
  scanning (`:4-7`, `:15-20`); and `^(node_modules|corpus|artifacts|\.git|
  \.claude|\.agents|\.codex|\.kanmer)/` is excluded (`:14`). Consequence for this
  ticket: a cross-ADR link written **inside** a fenced block is not verified at
  all, so the `## Links` sections must use ordinary relative links outside fences
  if the gate is to prove anything.
- **F6 — `scripts/Test-MarkdownPlacement.ps1` takes mandatory `-Base` and
  `-Head`** (`:2-5`); a bare call fails on the missing argument, not on a
  placement violation. Its allowed-roots regex at `:31` is
  `^((docs/(prd|frd|adr|design|desktop))|workspaces/document-extraction|\.agents/skills|\.design-sync|\.grok|\.stitch|design/planning-and-old-designs)/.+\.md$`,
  so `docs/adr/*.md` passes. CI calls the regression wrapper
  `scripts/Test-TestMarkdownPlacement.ps1`, which takes none.
- **F7 — the evidence each ADR needs is already in the tree.** `src/` holds
  exactly four production projects (`Pegasus.Core`, `Pegasus.Infrastructure`,
  `Pegasus.Web`, `Pegasus.Worker`) and `tests/` three
  (`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`).
  The SQL Server connection is composed only at `src/Pegasus.Web/Program.cs:549`
  and `src/Pegasus.Worker/WorkerDependencyInjection.cs:150`, both through
  `src/Pegasus.Infrastructure/DependencyInjection.cs:53`
  (`AddDbContextFactory<PegasusDbContext>`) — that single-owner fact is what
  ADR-0103 turns into a rule for workstations. The `Features:*` composition-gate
  pattern ADR-0104 and ADR-0105 rely on for expand/contract is at
  `src/Pegasus.Web/Program.cs:112-116`, `:202` and `:640-660`.
- **F8 — ADR-0110's subject matter already exists.** `skills-lock.json` at the
  repository root (29 lines; `version: 1` and a `skills` map of `source`,
  `sourceType`, `skillPath`, `computedHash`), the fuller
  `docs/desktop/12-agent-tooling/skills.lock.draft.json` (382 lines), eight
  vendored subagents at `.codex/agents/*.toml` (`pegasus-azure-auditor`,
  `pegasus-desktop-reviewer`, `pegasus-gateway-dev`, `pegasus-parity-researcher`,
  `pegasus-release-packager`, `pegasus-test-engineer`, `pegasus-ui-verifier`,
  `winui-dev`), twelve Kanmer skills under `.grok/skills/`, and the project
  skills under `.agents/skills/` (`pegasus-release`, `project/pegasus-desktop`).
- **F9 — ADR-0105's filename is already agreed by the plan set itself.**
  `docs/desktop/04-auth-session-update-and-startup/README.md:296-297` names
  `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`. It is the only
  ADR-0105 path the plan set states, which is why all three claimant tickets
  quote it identically.
- **F10 — the co-claimants resolve to these board ids** (`search_items`,
  2026-08-24 — read, never computed): ADR-0100 and ADR-0104 → [[FND-026]]
  (plan handle `DSK-02-01`); ADR-0105 → [[REL-001]] (`DSK-09-01`) **and**
  [[FND-042]] (`DSK-04-01`); ADR-0110 → [[TOOL-008]] (`DSK-12-08`, whose entire
  subject is that ADR). All four are in `backlog` today.
- **F11 — the plan sentence this ticket must correct is real.**
  `docs/desktop/00-governance-and-workflow/README.md:423` still reads
  "New ADRs and index rows; ADR-0009 `superseded_by` note limited to its deferral
  clause (body immutable — record in ADR-0100)" — which instructs the frontmatter
  edit step 6 forbids.
- **F12 — the repository has removed governance from the ADR set before.**
  `docs/adr/README.md:48` and `:52` relocate ADR-0010 and ADR-0023 to
  `AGENTS.md` / `docs/index.md` with the stated reason "governance is not an
  ADR". The six decisions here are architectural and belong; the two one-line
  corrections this ticket also makes are governance and correctly land in
  `AGENTS.md` and in the plan, not in an ADR.
- **F13 — `docs/index.md` needs no change.** It links the ADR *index*
  (`docs/index.md:21`, `:46`) and only one individual ADR (`:56`, ADR-0029). Six
  new ADRs create no dangling reference there.

### Assumptions

- **A-00-5-1 — no co-claimant lands its ADR before this ticket runs.** Measured:
  F1 (no ADR-01xx file) and F10 (all four claimants in `backlog`).
  *Confirmed by:* step 1's `ls docs/adr/010*` at execution time.
  *Breaks if:* one merged first — this ticket then verifies and extends that file
  in place and creates no second file, which is the rule every claimant body
  already states. Nothing is lost; only the work shape changes.
- **A-00-5-2 — `accepted` is the right status for all six on the day they merge.**
  Plan 00 § 3 marks only ADR-0108 `proposed`, and that one belongs to [[FND-007]].
  *Confirmed by:* the operator accepting the PR.
  *Breaks if:* the operator wants one held at `proposed` — then that ADR's
  `status` changes and its index row must not go in the accepted table.
- **A-00-5-3 — `docs/adr/README.md`'s three-column shape is correct and
  `AGENTS.md:115` is the sentence in error**, not the reverse.
  *Confirmed by:* the file, maintained in that shape across 28 ADRs (F2).
  *Breaks if:* the operator would rather the index gained the two extra columns —
  that is a 28-row index rewrite and a different ticket, and this ticket's
  one-line `AGENTS.md` correction would then be wrong.
- **A-00-5-4 — the two texts other tickets owe ADR-0100 can be written into it in
  this PR** (step 7): the decided D-001 consequence from [[FND-010]]
  (`DSK-00-10`) and the "prior documents are not in the repository and are not an
  input" sentence from [[FND-013]] (`DSK-00-13`).
  *Confirmed by:* agreeing it with those two tickets before the PR opens.
  *Breaks if:* they are not coordinated — published bodies are immutable
  (`docs/adr/README.md:12-14`), so recording them afterwards needs a **new
  superseding ADR**. That is a real, one-way cost and the body requires the
  choice to be recorded rather than drifted into.
- **A-00-5-5 — six ADRs are one PR.** The plan row and the ticket treat them as a
  unit. *Breaks if:* review asks for one PR per ADR — the file set is identical,
  only the branch topology differs.

## Execution placement

**This ticket places no runtime responsibility anywhere**, so the six-question
test is not answered for the ticket itself: it writes decision records, and the
placements those records describe were decided in
`docs/desktop/00-governance-and-workflow/README.md` § 3 and in areas 02, 04, 09
and 12 — not here. It creates, moves and deletes no Azure resource.

**The one placement this ticket assumes** is the split it records: the client
runs on the operator's Windows 11 workstation, the gateway (`Pegasus.Web`
evolved in place, L-01) keeps authority, and — under D-002 and D-003 — the
signing certificate and the update feed both live on **in-house Windows hosts**,
not in Azure.

### The six tables the ADRs themselves must carry

Step 5 requires the Appendix A table answered inside each ADR's `## Context`.
These are the answers the repository evidence supports; **re-verify each cited
`path:line` when writing**, and note that a "yes" names *where* the
responsibility lands, which on this programme is frequently an in-house host and
never automatically Azure.

**ADR-0100 — where the operator's client application runs.**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **no** | A UI is per-operator; the shared, mutable case state it edits lives behind the gateway (ADR-0103) |
| Unattended execution | **no** | The client runs only while an operator is at it; the unattended half is the Worker (`src/Pegasus.Worker`), placed centrally by ADR-0106 — a different responsibility |
| Protected credentials | **no** | The client holds a short-lived access token and a rotated refresh handle (ADR-0102); no long-lived provider secret ships in the package (ADR-0107) |
| Public callback | **no** | Nothing external calls a workstation |
| Central enforcement | **no** | Permissions, revocation and audit are enforced by the gateway; the client is not the enforcement point |
| Measured operational advantage | **no** | Proposal §15 argues the native client is materially better, not the server |

→ All six "no": the client belongs on the desktop.

**ADR-0101 — where domain calculation runs (the local-execution half of the
split).**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **no** | A calculation is a function of inputs the client already holds; the *result* is persisted through the gateway |
| Unattended execution | **no** | Calculations run in response to an operator action |
| Protected credentials | **no** | No secret is needed to compute |
| Public callback | **no** | — |
| Central enforcement | **yes — on the gateway** | The invariant that a persisted assessment satisfies domain policy must not depend on the client, so the same `src/Pegasus.Core` policy runs server-side on write (one Core owner, ADR-0002 / ADR-0009). Local execution buys responsiveness; it is not the enforcement point |
| Measured operational advantage | **no** | Nothing measured says central computation is better; §15 says the opposite for latency |

→ One "yes", naming the gateway. This is the split ADR-0101 records, in one
table.

**ADR-0103 — where the database and its authority live.**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **yes — gateway + its SQL database** | Several operators see and update the same case state; one database and one migration stream (ADR-0002), composed only at `src/Pegasus.Infrastructure/DependencyInjection.cs:53` |
| Unattended execution | **yes — the existing central Worker** | `src/Pegasus.Worker` processes intake with every desktop closed (ADR-0106) |
| Protected credentials | **yes — the gateway host** | The SQL connection string is long-lived and must not sit on a workstation (`src/Pegasus.Web/Program.cs:549`, `src/Pegasus.Worker/WorkerDependencyInjection.cs:150`) |
| Public callback | **no** | Nothing external calls the database |
| Central enforcement | **yes — the gateway** | Permissions, revocation, audit and the concurrency invariant are enforced server-side |
| Measured operational advantage | **no** | Not claimed; the four "yes" already decide it |

→ Stays central; workstations never hold a connection string.

**ADR-0104 — where operator-visible case data rests on the workstation.**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **yes — the gateway** | Case state is shared and mutable, which is precisely why a replicated offline store would fork it (proposal §11) |
| Unattended execution | **no** | Nothing on the workstation must run with the desktop closed; that is the Worker's job |
| Protected credentials | **no** | The cache holds no secret; the DPAPI-protected refresh handle is a separate store under ADR-0102 |
| Public callback | **no** | — |
| Central enforcement | **yes — the gateway** | Revocation and permission changes must take effect without waiting for a client to reconcile, which an offline replica defeats |
| Measured operational advantage | **no** | No measured evidence supports a replicated store; §11 rejects it on correctness grounds |

→ Two "yes", both on the gateway: online-required, bounded cache only, no
replication.

**ADR-0105 — where distribution, signing and version enforcement live.**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **no** | A package is not shared mutable state |
| Unattended execution | **yes — the always-on in-house Windows host** | The feed must answer App Installer's update check with every developer machine closed; **D-003** places it on a UNC share served over SMB, *not* in Azure |
| Protected credentials | **yes — the in-house signing host** | The production signing key is long-lived and must not sit on workstations or in a public CI runner; **D-002** keeps a self-managed certificate in-house, trusted per workstation in `LocalMachine\TrustedPeople` |
| Public callback | **no** | SMB carries Windows authentication and nothing external calls in — which is the point of D-003 under **C-01**, the constraint that makes the repositories private and rules out anonymous HTTPS from GitHub permanently |
| Central enforcement | **yes — the gateway** | The minimum-client-version gate must be enforced server-side and fail closed; a client cannot be trusted to block itself. App Installer's `UpdateBlocksActivation` is the second, client-side layer |
| Measured operational advantage | **no** | Not claimed; the recorded advantage of D-002 + D-003 is that the whole distribution path touches no Azure resource and carries no recurring cost |

→ Three "yes", **none of which lands in Azure**. Do not let this table be read
as an Azure justification.

**ADR-0110 — where skill pinning and the invocation protocol live.**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **yes — the git repository** | Every agent session must resolve the same skill revision; `skills-lock.json` in git is that shared authority. Not a service |
| Unattended execution | **no** | Pinning resolves when a session starts |
| Protected credentials | **no** | The lockfile holds sources and hashes, no secret |
| Public callback | **no** | — |
| Central enforcement | **yes — the repository and its review gate** | A session must not silently take an unpinned skill; the vendored tree under `.codex/skills/` and the lockfile hashes are the enforcement, checked at review |
| Measured operational advantage | **no** | Not claimed |

→ Two "yes", both landing on the repository itself — neither desktop nor Azure,
and saying so plainly is the honest answer.

## Implications

- **Write the existence check before the prose.** F1 says nothing is there today,
  but four numbers have other claimants (F10). `ls docs/adr/010*` is one command
  covering all six and is the executable form of the collision rule; run it and
  record its output before creating a file.
- **ADR-0100 is the one-way door.** Published bodies are immutable
  (`docs/adr/README.md:12-14`), so the D-001 consequence and the "prior
  documents" sentence must be inside it at merge or they cost a new superseding
  ADR (A-00-5-4). Coordinate [[FND-010]] and [[FND-013]] into this PR, or record
  the decision not to.
- **`supersedes: []` stays empty in ADR-0100**, and ADR-0009 is not edited at all
  — F3 gives the repository's own precedent, and the alternative imports a
  consequence the decision does not intend.
- **Follow the file, not the convention sentence, for the index shape** (F2), and
  correct the sentence in the same PR — otherwise the next ADR author writes
  five-cell rows into a three-column table.
- **Verify per ADR ID, never by row total** (F4): the reserved block reaches 11
  rows once [[FND-006]], [[FND-007]] and [[TOOL-008]] have landed theirs.
- **Both gates are cheap and local**; run them before the PR
  (F5, F6), remembering that `Test-MarkdownPlacement.ps1` needs `-Base`/`-Head`
  and that a link inside a fence is not checked.
- **Nothing ripples into code, contracts or the board's gate rules except by
  `link_doc`.** There is no `openapi/` directory in the repository today
  (`ls openapi` → *No such file or directory*), so the usual contract ripple does
  not apply; the only behavioural ripple is step 12, where clearing `docs_todo`
  on a ticket swaps which document satisfies its `leave-backlog` gate.

## Open questions

- **Which of the three ADR-0105 claimants authors the file** — this ticket,
  [[REL-001]] (`DSK-09-01`) or [[FND-042]] (`DSK-04-01`). The body makes this an
  ownership question for the operator to settle before Phase 2 and directs the
  answer to **the plan document**, which is where it is recorded; it is a scope
  boundary between named tickets, not an unsettled design question, and it
  blocks nothing — the path and the first-author-wins rule are already agreed
  identically by all three. No `open-questions` document is created for it.
- **Whether [[FND-010]] and [[FND-013]] fold into this PR** (A-00-5-4). Answered
  in the plan by taking the default — fold them in — with the cost of the
  alternative stated, rather than left open.
