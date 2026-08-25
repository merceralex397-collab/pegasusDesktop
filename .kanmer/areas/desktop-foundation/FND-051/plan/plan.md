# Plan — FND-051: standing later upstream syncs up to the D-001 freeze

**Diff estimate: ~4 files, ~30 fork-authored lines**, spread across every
repetition and the freeze — plus the upstream commits each merge carries,
which are **not** this ticket's diff and are explicitly out of scope for the
simplification pass (body step 10).

## Measured file-and-line inventory

Profile `chore` owes no `research` and no `files` document, so this plan
carries the surface-area burden alone —
`.grok/skills/kanmer-plan/assets/plan-template.md`'s "written FROM the ticket's
`research` and `files` documents" precondition does not apply to `chore`. Every
row was measured against the fork working tree on 2026-08-24 with `wc -l`,
`sed -n` and `grep -n`.

| Path | Measured now | Change | Lines |
| --- | --- | --- | --- |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | **224 lines.** § Code drift and the first sync runs `:197-224`; the "Notable upstream changes" bullets are `:203-217`; the first-sync paragraph `:219-224` ends with the repeat rule — "**Repeat after each upstream release until cutover; each sync re-runs this triage for tickets that changed status upstream.**" That sentence is this ticket's whole mandate. | **Edit, once per repetition.** Append one dated line per sync recording the head SHA, the commit count and the upstream ticket ids brought; at the freeze, the frozen SHA and date plus the still-unarrived list. | +1 per sync, +6 at the freeze (~16) |
| `docs/desktop/01-inventory-and-parity/README.md` | **249 lines.** § 5 Work breakdown at `:178`; the table header `:184`; rows `:185-197`, last row `DSK-01-12` at `:197`; § 6 Routing table at `:199`. **There is no `DSK-01-13` row** — `grep -n "DSK-01-13"` returns nothing. | **Edit, once (body step 11).** Insert one `DSK-01-13` row after `:197`, matching the table's eight columns. | +1 |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | **46 rows, `PAR-01`…`PAR-46`** (`grep -c '^\| PAR-'` → `46`, run 2026-08-24). | **Edit, per repetition.** Re-stamp the status cell of any row citing a file the sync touched. Cell edits, so usually no new lines. | ~0–6 per sync |
| `docs/desktop/README.md` | **142 lines.** § Locked decisions and open decisions holds the `D-001` row with its "Decided 2026-08-23: Option A" text. | **Edit, at the final sync only.** Record the D-001 freeze SHA and date. | +2 |

**Sum of fork-authored lines: ~4 files, ~30 lines.**

### Measured and deliberately not touched — but watched every time

| Path | Measured now | Why it matters |
| --- | --- | --- |
| `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` | **756 lines.** `CommittedMigrationCreatesTheSqlServerSchema` at `:21` pins **64** migration ids in an `Assert.Equal` list at `:28-95`, followed by `Assert.Empty(await context.Database.GetPendingMigrationsAsync())` at `:96`. `ls src/Pegasus.Infrastructure/Persistence/Migrations/*.cs` counts **104** files. | An upstream sync that brings a new migration turns this test **red** until its id is added to the census. The body's Traps name it; this row gives the implementer the line numbers so the failure is recognised in seconds rather than debugged. The census line comes from upstream's own commit if upstream added it, and is a conflict resolution if both sides did. |
| `docs/adr/README.md` | Lists ADR-0001…ADR-0029 (0017 never issued) | Upstream **keeps issuing ADRs below 0100**, and the conversion reserved ADR-0100…ADR-0110 (`docs/desktop/00-governance-and-workflow/README.md:140-165`). Check for a collision **every** repetition, not once. |
| `scripts/Test-MainBranchHistory.ps1` | Declares **three** mandatory parameters — `-Before`, `-Head`, `-ReleaseBranch` (`:2-12`) | The CI `repository-check` job invokes it; a manual invocation without all three prompts or fails. Not in this ticket's verification list, and should not be added to it bare. |
| `scripts/Test-MigrationGrants.ps1` | `param()` at `:2` — **no** parameters | The body's verification calls it bare, which is correct. |
| `src/**` | — | This ticket writes **no** source file of its own. Every source change in every diff comes from upstream (Guardrails). |

## Approach

**Re-derive the range from `upstream/main` HEAD every single time, and let the
freeze be the ticket's only closing condition.** Each repetition is: fetch,
record the head, prove ancestry, list the commits, name the upstream ticket ids
they carry, merge into a task branch off `dev`, run the gates, take the
independent review, promote by exact-SHA fast-forward, then append one dated
line to the register. The ticket stays open across the whole conversion by
design (body step 9) and closes only after the D-001 freeze sync.

The alternative rejected is **replaying the recorded 32-commit list from
`upstream-kanmer-carryover.md:203-217`**. It is right there, it is dated
2026-08-23, and it is the trap: that list describes the range as of one day,
and by the second repetition it is a description of history rather than of
what is pending. The body says "**Re-derive the range every time; never replay
a recorded list**" in as many words. The recorded list stays useful as a
*watch* list — the register rows to tick off — and never as a range.

The second alternative rejected is **one sync per upstream commit** rather than
per release. It would keep the range tiny and it collides with **C-01**:
private-repository Windows runners bill at a 2× multiplier, and this repository
runs most `repository-check` jobs on `windows-latest`. Batching per upstream
release is the body's own instruction and the cheaper one; a fortnightly floor
(step 2) stops the batch growing unboundedly between releases.

## Governing docs

The ticket's `refs` list is **empty** and `get_doc_gates FND-051` reports
`docs_todo: true`.

> **New ADR** — ADR-0100 (native WinUI 3 client converted inside this fork),
> whose **consequences** carry D-001: the fork becomes the single release
> source and upstream is merged once more and then frozen
> (`docs/desktop/00-governance-and-workflow/README.md` § D-001). Authored by
> [[FND-026]] (plan handle `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`)
> also claims ADR-0100 — see [[FND-026]]'s plan for the ownership
> reconciliation, and note that [[FND-010]] (plan handle `DSK-00-10`) records
> D-001 into that ADR's consequences and into `docs/operations.md`.
> This plan is written to D-001 as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § D-001 and
> `docs/desktop/README.md` § Locked decisions; if ADR-0100 lands differently
> this plan is revised before the next repetition.

**This ticket has no seeded plan row.** It is the one conversion ticket with no
upstream id and no row in the plan set — specified verbatim inside [[FND-023]]
(plan handle `DSK-01-10`) during board seeding and created from that
specification on 2026-08-24. Step 11 adds the row, and until it does, the
authorities below are the only things this ticket can be checked against.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:219-224` | "Repeat after each upstream release until cutover; each sync re-runs this triage for tickets that changed status upstream" | Steps 2–4, every repetition |
| `docs/desktop/00-governance-and-workflow/README.md` § Recommended branching flow item 2 | Add `upstream` as a **read-only** remote and sync one-way after each upstream release **until cutover**; never push fork → upstream | Steps 3, 5; the `git remote get-url --push` verification |
| **D-001** (decided 2026-08-23) | The fork becomes the single release source at the first production gateway change; upstream is merged in one final time and then frozen | Step 8 — this ticket's **last** execution *is* that final merge |
| **L-01** (`docs/desktop/README.md` § Locked decisions) | The gateway is `Pegasus.Web` evolved in place | Why upstream web fixes still matter until the freeze — step 5 merges Razor changes the conversion intends to retire |
| **C-01** (`docs/desktop/README.md` § Constraints) | Private-repository Windows runners bill at 2× against a monthly allowance | Step 2's per-release batching and the read-only watch mechanism, which costs no CI minutes |
| `docs/engineering.md:11-15` § Branches and delivery | Task branches cut from `dev`, merged into `dev` through a PR; `main` is the active deployment; `dev` and `main` are **never** rebased, reset or force-pushed | Step 5 |
| `docs/engineering.md:16-33` | Promotion is an exact-SHA atomic fast-forward with `--force-with-lease` on `dev`, after explicit `MERGE AUTH GRANTED`, with both remote heads read back equal; "A GitHub PR merge, rebase merge, or squash merge is **not** an exact-SHA promotion" | Step 6, and the `git rev-parse origin/main origin/dev` verification |
| `docs/engineering.md:46-52` | "Green means every `repository-check` job for the PR's head revision succeeded or was path-skipped" | Step 5's merge condition |
| `AGENTS.md` § ADR conventions + `00/README.md:140-165` | ADR-0100…ADR-0110 is the reserved conversion block; upstream keeps issuing below it | Step 5's per-repetition collision check |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | A bare `<PREFIX>-<nnn>` is a **fork board id**; an upstream id is always `upstream <ID>` | Step 1 reads it first; every id in step 4 is written `upstream <ID>` |
| `docs/engineering.md:72-74` tier 1 | Static/build/architecture — "This proves consistency only" | Verification: each sync is proved by history containment, a green `repository-check` and the read-back promotion, **not** by application behaviour |
| `docs/engineering.md:201-203` § Plan sizing | A plan states its diff estimate first, from a real number | The first line and the inventory above |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass per branch, over that branch's own diff | Step 10 — `n/a — docs-only` where the branch carries only the merge and the register lines; **upstream commits are never in scope** |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Step 5 and Routing |

## Routing

Copied from the ticket body's `## Routing` block; required in the plan document
by `docs/desktop/00-governance-and-workflow/README.md` § Ticket template.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
  (confirmed present 2026-08-24)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `run-tests`
  (`dotnet/skills` `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`, `get_group_doc`). **No Azure
  MCP, no Microsoft Learn.**
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates FND-051` before every move; a move crosses at most one gated
  boundary). `chore` owes `plan` at `leave-preparing` and `proof` at
  `enter-done`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's eleven implementation steps in the same order,
with the same ownership and the same file paths.

1. **Orient and take.** Read
   `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:197-224`
   § Code drift and the first sync, `docs/engineering.md:10-52` § Branches and
   delivery, and [[FND-023]]'s (plan handle `DSK-01-10`) `proof` document for
   the head it actually synced to — that head, not `7d6a948a`, is the baseline
   for the first repetition. **Read the group document `HZN-001` /
   `board-conventions.md` § "Upstream ids versus board ids" before writing any
   id**: a bare `<PREFIX>-<nnn>` is a fork board id, and the two namespaces
   collide on 45 ids. Call `get_doc_gates FND-051`, then `take_ticket`.
2. **Fix the trigger and name the watcher.** *Recorded here as the body's step
   2 requires, taken as a default rather than asked:* a sync runs **when
   upstream cuts a release, and in any case no less often than every two
   weeks** while the conversion is live. **The watcher is the operator who
   holds the `MERGE AUTH GRANTED` authority** — the release actor named in
   `docs/engineering.md:29-30` — because that person already gates every
   promotion this ticket performs, so no new role is created and no new person
   has to be told. The watch mechanism is read-only and free:
   `git ls-remote --heads upstream main` compared against the last head
   recorded in `upstream-kanmer-carryover.md` § Code drift. It runs no CI job,
   which matters under **C-01** (private Windows runners bill at 2×). "A
   cadence nobody watches is not a cadence" — so the watcher, the interval and
   the mechanism are all three recorded above, and a repetition that finds the
   heads equal is logged as a no-op rather than skipped silently.
3. **Per repetition — derive the range, never replay one.**
   `git fetch upstream main --no-tags`; record `git rev-parse upstream/main`
   and the date; prove ancestry with
   `git merge-base --is-ancestor $(git rev-parse main) upstream/main`; list the
   range with `git log --oneline --no-merges main..upstream/main`. **Re-derive
   every time.** The 32-commit list at
   `upstream-kanmer-carryover.md:203-217` is a *watch* list, not a range: it
   describes 2026-08-23 and nothing after it.
4. **Per repetition — reconcile against the carry-over register.** Name every
   upstream ticket id the commit subjects carry, tick each off the register,
   and record every register row that has **not** arrived. Write each as
   `upstream <ID>`, never bare. The watch list at creation time is upstream
   `DOCS-013`, upstream `PLAT-041`, upstream `ENG-014`, upstream `ENG-015`,
   upstream `INTK-033` and upstream `CASE-021`. Two of these already have fork
   board tickets under different ids — the join table in `HZN-001` /
   `board-conventions.md` maps upstream `ENG-014` to board [[ENG-001]] and
   upstream `INTK-033` to board [[INTK-007]] — so do **not** file duplicates
   for them; the register row is what tracks the *code* arriving. **A row still
   unarrived as the freeze approaches must be raised as a fork ticket before
   the freeze, not after**: under D-001 upstream work not merged before the
   freeze does not arrive late, it vanishes.
5. **Per repetition — merge, gate, review.** Task branch from `dev`;
   `git merge upstream/main`; resolve conflicts **in favour of upstream for
   files upstream owns**. Then, before opening the PR, three checks that are
   easy to skip and expensive to miss:
   - **ADR collision** — `docs/adr/README.md` lists ADR-0001…ADR-0029 today and
     upstream keeps issuing below 0100; a new upstream ADR must not land on
     ADR-0100…ADR-0110 (`00/README.md:140-165`). Check **every** repetition.
   - **Migration census** — if the sync brings a migration,
     `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:28-95`
     pins 64 ids in an `Assert.Equal` list and `:96` asserts no pending
     migrations. A new migration turns that test red until its id is in the
     list, in the right position.
   - **Local gates** — `pwsh ./scripts/Test-DocumentationLinks.ps1` (declares
     `param()`, takes no arguments), `pwsh ./scripts/Test-MarkdownPlacement.ps1
     -Base origin/dev -Head HEAD` (**both parameters are
     `[Parameter(Mandatory)]` at `:3-4`; a bare invocation prompts or fails and
     checks nothing**), `pwsh ./scripts/Test-MigrationGrants.ps1` (also
     `param()`, bare is correct), and the test lanes via `run-tests`.

   Open the PR into `dev`, take the independent review
   (`AGENTS.md` step 5), and merge only with `repository-check` green or
   path-skipped for the PR's head revision (`docs/engineering.md:46-49`).
6. **Per repetition — Operator step: promote.** The exact-SHA atomic
   fast-forward from `docs/engineering.md:16-33`, after the literal
   `MERGE AUTH GRANTED`: fetch both remote refs, confirm
   `git merge-base --is-ancestor origin/main origin/dev`, record the reviewed
   `origin/dev` SHA, then the single atomic push with
   `--force-with-lease=refs/heads/dev:<reviewed-dev-sha>` pushing that SHA to
   both `refs/heads/main` and `refs/heads/dev`. **Read both remote heads back
   equal to the recorded SHA.** A GitHub merge, squash or rebase is **not** a
   promotion. A failed preflight, a rejected transaction or an unequal
   read-back **stops** the release and is never repaired by a rebase, reset or
   force push.
7. **Per repetition — re-stamp and record.** Re-stamp the status cell of every
   `parity-matrix.md` row citing a file the sync touched (46 rows,
   `PAR-01`…`PAR-46`), and append one dated line to
   `upstream-kanmer-carryover.md` § Code drift recording the head SHA, the
   commit count and the upstream ticket ids brought.
8. **The final sync — the D-001 freeze.** When [[FND-010]] (plan handle
   `DSK-00-10`) confirms the freeze date with the upstream owners, run one last
   sync, then record in `upstream-kanmer-carryover.md` and in
   `docs/desktop/README.md` § Locked decisions that upstream is frozen **from
   that SHA**: after it the fork is the single release source and nothing
   further arrives from upstream. List, by upstream id, every register row
   still unarrived at that moment — each is now either a fork ticket or a
   deliberate loss, **and this ticket says which**.
9. **Close only after the freeze sync.** Every earlier repetition is a dated
   entry in this ticket's `proof` document; the ticket stays open across the
   whole conversion **by design**, and its checklist is one item per
   repetition. Because `chore` owes `proof` at `enter-done`, the proof document
   is appended to as the ticket runs and finalised at the freeze — not written
   once at the end from memory.
10. **Simplification pass per branch, over that branch's own diff.**
    `n/a — docs-only` where the branch carries only the merge and the register
    lines. **Upstream commits are never in scope for the pass** — they were
    reviewed upstream, and re-simplifying them would create a fork/upstream
    divergence the merge exists to avoid.
11. **Add the plan row, and record why the horizon is HZN-010.** Insert a
    `DSK-01-13` row into `docs/desktop/01-inventory-and-parity/README.md` § 5
    after `:197` (the `DSK-01-12` row), matching the table's eight columns —
    ID · Title · Profile · Depends on · Acceptance · Verification · Tier ·
    Routing — so this stops being the one board ticket with no plan row.
    **And record this, because the horizon reads as a deferral and is not
    one:** the ticket sits in **HZN-010 (Phase 9 — pilot and parallel
    operation)** because that is where it *closes*, at the D-001 freeze. Its
    **first** execution is immediately after [[FND-023]] in Phase 0, and every
    intermediate repetition runs on the cadence in step 2. A reader who takes
    HZN-010 to mean "start this in Phase 9" would let the whole conversion run
    with no upstream syncs — which is the exact failure this ticket exists to
    prevent.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**
(`docs/engineering.md:72-74`). Each sync is proved by history containment, a
green `repository-check` run and the read-back promotion — **not** by
application behaviour, which tier 1 explicitly does not cover ("This proves
consistency only").

| Command / observation | Expected | Becomes evidence as |
| --- | --- | --- |
| `git remote get-url --push upstream` | `DISABLED_NO_PUSH_TO_UPSTREAM` — the value [[FND-002]] (plan handle `DSK-00-02`) sets. Run it **every repetition**, not once | `proof` (command-log) |
| `git rev-parse upstream/main` before each merge | the head SHA, recorded with the date; it is the next repetition's baseline | `proof` (command-log) |
| `git log --oneline dev..upstream/main` after each sync | **empty** | `proof` (command-log) |
| `git rev-parse origin/main origin/dev` after each promotion | two identical SHAs, both equal to the reviewed SHA | `proof` (command-log) |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit `0` per repetition (no parameters — `param()` at `:8-9`) | `proof` (command-log) |
| `pwsh ./scripts/Test-MigrationGrants.ps1` | exit `0` per repetition (no parameters — `param()` at `:2`) | `proof` (command-log) |
| `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` | exit `0` and `Markdown placement passed for <base>..<head>.` — **both parameters are mandatory**, so a bare call is not a pass, it is no check at all | `proof` (command-log) |
| GitHub Actions `repository-check` for each PR head | every job succeeded or path-skipped | the run URL into `proof` |
| `dotnet test tests/Pegasus.IntegrationTests --filter FullyQualifiedName~CommittedMigrationCreatesTheSqlServerSchema` when the sync brings a migration | `Passed!` after the new id is added to the census at `:28-95` | TRX summary into `proof` (test-output) |
| `grep -n "frozen" docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` after the freeze | the dated freeze SHA line | `proof` (command-log) |

## Risks / open questions

- **Risk — a recorded commit list is replayed as a range.** The 32-commit list
  at `upstream-kanmer-carryover.md:203-217` is right there and dated. Replaying
  it silently skips everything upstream did afterwards. Mitigation: step 3
  derives the range from `upstream/main` HEAD every time; the register list is
  used only for ticking off arrivals.
- **Risk — nobody watches, so no sync ever runs.** Mitigation: step 2 names the
  watcher (the operator holding `MERGE AUTH GRANTED`), the interval (per
  upstream release, floor of two weeks) and a read-only mechanism that costs no
  CI minutes; a no-op repetition is logged, so silence is distinguishable from
  a missed cadence.
- **Risk — HZN-010 is read as "start in Phase 9".** Then no sync runs for the
  whole conversion and D-001's freeze silently discards everything upstream
  fixed after 2026-08-23. Mitigation: step 11 records the reason in this
  document and in the plan row.
- **Risk — an upstream ADR collides with ADR-0100…ADR-0110.** Upstream keeps
  issuing below 0100 and does not know about the reserved block. Mitigation:
  step 5 checks `docs/adr/README.md` every repetition; a collision is resolved
  by renumbering the **fork's** ADR, never upstream's, because upstream is the
  side that keeps releasing until the freeze.
- **Risk — a sync brings a migration and the census test goes red at an
  inconvenient moment.**
  `IntakePersistenceIntegrationTests.cs:28-95` pins 64 ids and `:96` asserts no
  pending migrations. Mitigation: step 5 makes it a pre-PR check rather than a
  CI surprise; the runtime-role `GRANT` for any new table is caught separately
  by `Test-MigrationGrants.ps1`.
- **Risk — `main` is updated by a GitHub merge.** `docs/engineering.md:34-38`
  says plainly that a PR merge, rebase merge or squash merge is not a
  promotion, and notes that "GitHub protection and rulesets are intentionally
  out of scope on subscription grounds, so the main-push CI check is
  **detective** rather than a server-side prevention" — nothing stops it
  happening. Mitigation: step 6's read-back, and the
  `git rev-parse origin/main origin/dev` verification row.
- **Risk — CI cost.** C-01 makes private Windows runners bill at 2×, and every
  repetition runs the full `repository-check`. Mitigation: batch per upstream
  release (step 2); never run a sync speculatively; the watch check is
  `git ls-remote`, not a workflow.
- **Risk — an unarrived register row is discovered after the freeze.** Under
  D-001 that is not a delay, it is a loss. Mitigation: step 4 requires
  unarrived rows to be raised as fork tickets **before** the freeze, and step 8
  requires the freeze record to say, for each remaining row, whether it is a
  fork ticket or a deliberate loss.
- **Scope boundary, not an open question — the first sync.** [[FND-023]] (plan
  handle `DSK-01-10`) and [[FND-002]] (plan handle `DSK-00-02`) own the *first*
  sync, the `upstream` remote and its disabled push URL. This ticket owns every
  later one, and the body makes **a second standing-cadence ticket a stop
  condition**.
- **Scope boundary, not an open question — the freeze agreement itself.**
  [[FND-010]] (plan handle `DSK-00-10`) agrees the freeze with the upstream
  repository's owners and records D-001 in ADR-0100's consequences and
  `docs/operations.md`. This ticket performs the final merge once that date
  exists; it does not negotiate it.
- **Scope boundary, not an open question — the upstream board triage.**
  [[FND-022]] (plan handle `DSK-01-09`) owns the carry-over batch and the
  recorded exclusions in it. This ticket ticks register rows off as their code
  arrives; it does not re-triage tickets.
- **Operator dependency, not an open question.** Step 6 needs the literal
  `MERGE AUTH GRANTED` from the release actor on every repetition; the ticket
  carries `needs-operator` for exactly that. D-001 was decided on 2026-08-23
  and `docs/desktop/README.md` records that **no open decisions remain**.
- **Open questions**: none. No `open-questions` document is created — the
  ticket body does not instruct one, the cadence and watcher are recorded as
  defaults taken rather than asked (step 2), and every remaining unknown is a
  scope boundary owned by a named sibling ticket, which
  `docs/desktop/00-governance-and-workflow/README.md` § 3 makes a boundary
  rather than a question.

## Simplification pass

_Not yet run, and it is run **per branch** rather than once: `AGENTS.md`
§ Repository task workflow step 4 requires a pass over each branch's own diff
before its PR, recorded here under a dated heading per repetition.
`n/a — docs-only` is the expected record where a branch carries only the merge
and the register lines. Upstream commits are never in scope for the pass._

## Operator scope amendment — 2026-08-25

The operator has prohibited all upstream synchronization. This supersedes the standing later-sync cadence, watch, fetch, merge, and freeze-dependent acceptance criteria.

This ticket records the boundary through the refactor:

- No upstream remote, fetch, merge, comparison, cadence, or push is permitted.
- All implementation and history work stays in this repository and uses the configured `pegasusDesktop` remote only.
- Cloud writes and deployments remain deferred until the full refactor is complete.
- Any remaining external-release or upstream-dependent language is a deferred boundary, not an in-repository implementation task.

The amended acceptance is: the repository governance record and Kanmer proof state the boundary, and no upstream operation is performed.

## In-repository boundary implementation — 2026-08-25

The amended ticket is implemented without any upstream operation. The canonical plan surfaces now state that the historical upstream comparison and sync instructions are provenance only and are superseded for the current refactor:

- docs/desktop/README.md records the current operator boundary and the current dev baseline.
- docs/desktop/01-inventory-and-parity/README.md records the boundary, removes the upstream sync from the Phase 0 exit gate, and adds the DSK-01-13 plan row with the amended acceptance and verification.
- docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md labels the historical drift/sync section non-executable and directs needed work to in-repository fork tickets.

The configured remote check shows only origin pointing to pegasusDesktop; no upstream remote was added or read. No cloud, deployment, credential, mailbox, Box, or external environment operation was performed.

Validation:
- pwsh ./scripts/Test-DocumentationLinks.ps1 — passed; 233 files checked.
- pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD — passed.
- git diff --check — passed; only line-ending normalization warnings.
- git remote -v — only configured pegasusDesktop origin fetch/push URLs.

## Simplification pass — 2026-08-25

n/a — docs-only. Reused the three existing canonical plan documents and added one bounded boundary statement plus the missing DSK-01-13 row. No new document family, abstraction, remote, compatibility path, or external operation was introduced. Historical evidence was retained rather than deleted, but all executable upstream instructions were explicitly superseded.
