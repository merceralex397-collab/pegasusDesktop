# Plan — FND-023: land the first one-way upstream sync into the fork's `dev`

**Diff estimate: ~2 files, ~25 lines authored** — plus one merge commit whose content
is entirely upstream's and is not authored here.

`docs/engineering.md` § Plan sizing requires the estimate first. This is a `chore`,
so it owes no `research` and no `files` document and this plan carries the surface
area alone. The two numbers are separated deliberately: the merge's size is
**re-derived at execution time** (step 4) and cannot honestly be estimated now — the
recorded 32-commit set at `7d6a948a` was read on 2026-08-23 and upstream has almost
certainly moved.

### Measured surface-area inventory

Measured in `C:\Users\PC\Documents\GitHub\pegasusDesktop` at `bbd1c549`, 2026-08-24.

| Path | Measured current value | What this ticket authors |
| --- | --- | --- |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | 105 lines; 46 `PAR-` rows (`grep -c '^\| PAR-'` → `46`) at `:46-91`; the inventory baseline SHA `191ddf33` appears **once**, at `:6`; `PAR-17` at `:62` already names upstream `CASE-019` (`upstream efbb2a9`) with test evidence "upstream CASE-019 test (after sync)" | re-stamp the rows the sync touched; complete `PAR-17`'s test evidence — ~15 lines |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | 224 lines; § Code drift and the first sync at `:197-224`, still describing `7d6a948a` and "32 commits ahead" | two dated lines: the head actually synced with its real commit count, and the statement that the range is re-derived at execution time rather than replayed — ~8 lines |
| everything the merge brings | unknown until step 4 | **nothing** — the fork authors no source change in this ticket |

Total authored: **2 files, ~25 lines**. No `.cs`, `.csproj`, `.bicep` or workflow file
is authored by this ticket; every source change in the diff arrives from upstream.

### Measured preconditions — both are currently unmet

- **`dev` does not exist.** `git branch -a` returns `kanmer-board`, `main`,
  `task/desktop-plan-segmentation` and `remotes/origin/main` — no `dev`, local or
  remote. Step 2's stop condition is therefore live today, and `list_items` reports
  this ticket `blocked: true`. [[FND-001]] (plan handle `DSK-00-01`) owns creating
  `dev` from `main` at the baseline SHA.
- **The `upstream` remote does not exist.** `git remote -v` returns `origin` only
  (`https://github.com/merceralex397-collab/pegasusDesktop.git`, fetch and push).
  Nothing is configured that could accidentally push to upstream today, and step 3
  must keep it that way.

### Measured: the follow-up ticket already exists

The body's § *Follow-up ticket to create* specifies a standing later-sync ticket
verbatim. **It has already been created**: [[FND-051]] (plan handle `DSK-01-13`),
title `DSK-01-13 · Standing later upstream syncs up to the D-001 freeze`, area
`desktop-foundation`, profile `chore`, groups `["EPIC-002", "HZN-010"]`, labels
`["desktop-conversion", "plan-01", "phase-0", "tier-1", "needs-operator"]`,
`refs` empty — matching the specification field for field (`list_items --area
desktop-foundation`, 2026-08-24). Step 14's hand-over is therefore a **confirmation**,
not a creation: `get_item FND-051`, confirm the body carries the eleven headings, and
record the confirmation in `proof`. Creating a second standing-cadence ticket is a
stop condition.

## Approach

Run the sync exactly as `docs/engineering.md` § Branches and delivery already
prescribes — task branch from `dev`, merge `upstream/main` into it, reviewed PR into
`dev`, then an exact-SHA atomic fast-forward promotion to `main` after the literal
`MERGE AUTH GRANTED` — and **re-derive the commit range from `upstream/main` HEAD at
execution time** rather than replaying the recorded 32-commit set. The alternative,
syncing to the recorded SHA `7d6a948a` because it is written down, is rejected: it
silently drops everything merged upstream since 2026-08-23, and under D-001 what is
dropped at the freeze is lost rather than deferred. The second rejected alternative —
merging `upstream/main` straight into `main` because the content is a fast-forward —
is rejected because `scripts/Test-MainBranchHistory.ps1` fails a push to `main` whose
history is not contained in `dev`, and because a GitHub merge, squash or rebase is not
an exact-SHA promotion.

## Governing docs

The ticket's `refs` is **empty** and `docs_todo: true` — confirmed by
`get_doc_gates FND-023`, which reports `refs: []` and `docs_todo: true`. Profile
`chore` has no `leave-backlog` boundary on this board, so `docs_todo` satisfies no
gate here; it states honestly that no existing `docs/(prd|frd|adr)` document is
implemented by this work.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client in the fork), whose
> **Consequences** record D-001: the fork becomes the single release source at the
> first production gateway change. ADR-0100 is **authored by [[FND-026]] (plan handle
> `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`) is its co-claimant — see
> [[FND-026]]'s plan for the ownership reconciliation.** The D-001 wording itself, in
> ADR-0100's consequences and in `docs/operations.md`, is owned by [[FND-010]] (plan
> handle `DSK-00-10`), which also agrees the freeze with the upstream owners.
> This plan is written to D-001 as recorded in `docs/desktop/README.md` § Locked
> decisions and open decisions; if ADR-0100 lands differently this plan is revised
> before implementation. **No ADR is authored, edited or claimed by this ticket.**

Because `refs` is empty, the programme-level authorities that bind today are listed
with the step that satisfies each. `kanmer-review` checks this table against the diff.

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/engineering.md:10-52` § Branches and delivery | Task branches from `dev`; PR into `dev`; promotion is an exact-SHA atomic fast-forward with an explicit lease on `dev`, requiring the literal `MERGE AUTH GRANTED`; a GitHub merge/squash/rebase is not a promotion; both remote heads read back equal to the recorded SHA | Steps 7, 10, 11 |
| D-001 (decided 2026-08-23) | The fork becomes the single release source; upstream work unmerged at the freeze is lost, not deferred | Steps 4, 5, 6, 14 |
| L-01 (`docs/desktop/README.md`) | The gateway is `Pegasus.Web` evolved in place, which is why upstream web fixes still matter until cutover | Step 7's "merge them anyway" rule |
| C-01 (2026-08-23) | The repositories become private and Windows runners bill at 2×, so CI runs cost real money | Steps 9, 10 — no speculative re-runs |
| `docs/desktop/00-governance-and-workflow/README.md` § Recommended branching flow items 1–2 | `upstream` is a read-only remote; one-way sync only; never push fork → upstream | Steps 3, 14 |
| `AGENTS.md` § ADR conventions (reserved block) | The conversion uses ADR-0100…ADR-0110; upstream keeps issuing numbers below 0100 and a collision is a blocking finding | Step 8 |
| Proposal § 6.3 | Fork, not greenfield; no permanent second Pegasus repository | The whole approach |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's **own** diff, recorded under a dated heading in this plan | § Simplification pass below |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing → reviewer |
| `AGENTS.md` § New Markdown placement | No new `.md` outside `docs/(prd\|frd\|adr\|design\|desktop)` | Guardrails; both edited files are already under `docs/desktop/` |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory
in the plan document.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `run-tests` (`dotnet/skills`
  `98f84851`, plugin `dotnet-test`).
- **MCP**: Kanmer — `get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`. **No Azure MCP, no Microsoft Learn.**
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-verify` → `kanmer-closeout`. Gated boundaries confirmed by
  `get_doc_gates FND-023`: `leave-preparing` (`plan`, `questions-resolved`) and
  `enter-done` (`proof`, `questions-resolved`). Call `get_doc_gates FND-023` before
  every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement reviews
  the sync diff (`AGENTS.md` § Repository task workflow step 5; area 00 § 6 routing
  table).

## Steps

These refine the ticket body's fourteen implementation steps: same order, same
ownership, same commands, with the measured current values a step must be checked
against.

1. **Orient, then take.** Read `upstream-kanmer-carryover.md` § Code drift and the
   first sync (`:197-224`), `docs/desktop/00-governance-and-workflow/README.md`
   § Recommended branching flow, and `docs/engineering.md:10-52` § Branches and
   delivery in full. Check with `search_items` whether [[FND-002]] (plan handle
   `DSK-00-02`) has already landed the sync; if it has, this ticket does steps 9–13
   only. Call `get_doc_gates FND-023`, then `take_ticket`.
2. **Confirm `dev` exists.** `git branch --list dev` and `git rev-parse dev main`.
   **Measured today: `dev` does not exist** (`git branch -a` → `kanmer-board`, `main`,
   `task/desktop-plan-segmentation`, `remotes/origin/main`). If that is still true,
   **stop** — [[FND-001]] (plan handle `DSK-00-01`) owns creating it and this ticket is
   blocked.
3. **Add `upstream` read-only and fetch.**
   `git remote add upstream https://github.com/collisionengineers/pegasus.git`,
   `git remote set-url --push upstream DISABLED_NO_PUSH_TO_UPSTREAM`,
   `git fetch upstream main --no-tags`. Use the literal
   `DISABLED_NO_PUSH_TO_UPSTREAM` verbatim — it is the same string [[FND-002]] (plan
   handle `DSK-00-02`) step 2 sets, so if that ticket added the remote first this is
   already what is configured; do not re-set it to anything shorter. Measured today:
   `git remote -v` returns `origin` only, so this is a first configuration.
4. **Re-derive the range; never replay the recorded set.** Record
   `git rev-parse upstream/main` as the actual HEAD being synced. Then
   `git merge-base --is-ancestor $(git rev-parse main) upstream/main` (**expect exit
   0**) and `git log --oneline main..upstream/main | wc -l`. The planning baseline said
   32 commits at `7d6a948a` on 2026-08-23; state the **actual** number and, if it
   differs, say so plainly rather than reconciling it to 32. Write both figures, the
   HEAD SHA and the date into this plan. A non-zero ancestor check means the histories
   have diverged — stop and raise it; force nothing.
5. **Name every upstream ticket the sync brings that postdates the 2026-08-23
   triage.** `git log --oneline --no-merges main..upstream/main`, then name the upstream
   ticket id each subject carries. The known post-triage cases at planning time are
   upstream `DOCS-013`, `ENG-014` and `ENG-015` — the last two are already imported to
   this board as [[ENG-001]] and [[ENG-002]] — so any of them may or may not have merged
   by execution. Tick each arrival off the carry-over register; record each
   non-arrival, because under D-001 it is lost at the freeze unless a fork ticket holds
   it. Write the list into this plan.
6. **If upstream `DOCS-013` has not merged, record the FRD-07 consequence
   explicitly.** Measured today, the fork's
   `docs/frd/frd-07-eva-and-external-engineering-handoff.md` still mandates the
   invented manifest at **`:12`** ("SHA-256 manifest over the JSON and image identities
   and bytes. Stable manifest…"), **`:42`** ("…all-eligible-image, and manifest bundle
   available for immediate staff download") and **`:45`** ("…the exact package
   contents, manifest, or manual-handoff boundary"). If upstream `DOCS-013` has not
   arrived by the D-001 freeze it must be recreated as a fork docs chore against the
   fork's own FRD-07, and it must land **together with** upstream `ENG-014` (board
   [[ENG-001]]) — the doc change and the code change are two halves of one correction,
   and either alone leaves the governing document and the produced package
   contradicting each other. Raise it as a named finding here. **Do not edit FRD-07 in
   this ticket**; upstream `DOCS-013` owns those lines until the freeze.
7. **Cut the task branch from `dev` and merge upstream into it — never into `main`.**
   `git switch dev && git pull --ff-only && git switch -c task/dsk-01-10-first-upstream-sync && git merge upstream/main`.
   A merge commit is acceptable. Resolve conflicts **in favour of upstream** for files
   upstream owns; do not "fix forward" in the fork what upstream owns — raise those
   upstream instead (area 00 § 7 trap 4).
8. **Check for an ADR-number collision immediately after the merge.** `ls docs/adr/`
   and `sed -n '1,80p' docs/adr/README.md`. Measured today: `docs/adr/` holds
   ADR-0001…ADR-0029 with 0017 never issued, plus `README.md`; the index's accepted
   table starts at `docs/adr/README.md:18` with the three columns
   `ADR | Title | Related FRD`; **no `0100-*.md` … `0110-*.md` file exists yet**, so the
   reserved block is intact. Upstream keeps issuing ADRs from the low numbers. If the
   merge brings an ADR whose number the fork also used, that is a **blocking finding**.
9. **Run the full local gate before opening the PR.**
   `pwsh ./scripts/Test-DocumentationLinks.ps1`,
   `pwsh ./scripts/Test-MarkdownPlacement.ps1`,
   `pwsh ./scripts/Test-MigrationGrants.ps1`, then the test lanes through the
   `run-tests` skill (`dotnet test` over `tests/Pegasus.Core.Tests` and
   `tests/Pegasus.ArchitectureTests`, and `scripts/Invoke-TestShard.ps1` for the
   integration shards). Every command must exit 0, and the migration census must still
   match the folder: `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:93`
   currently pins `"20260822044425_GrantWorkerCaseDocuments"` as the head, and the
   folder currently holds **64** migrations
   (`git ls-files src/Pegasus.Infrastructure/Persistence/Migrations | grep "\.cs$" | grep -v "\.Designer\.cs$" | grep -v "ModelSnapshot" | wc -l` → `64`).
   Every migration id the sync adds must be present in that census.
10. **Open the PR into `dev`**, request the independent review by
    `pegasus-desktop-reviewer`, and wait for every `repository-check` job in
    `.github/workflows/ci.yml` to be green or path-skipped. Merge into `dev` only after
    both. C-01: private Windows runners bill at 2× — do not re-run lanes speculatively.
11. **Operator step — promotion to `main`.** The release actor needs the literal
    `MERGE AUTH GRANTED` from the operator before the push. Follow
    `docs/engineering.md:10-52` exactly: fetch both remote refs, confirm
    `git merge-base --is-ancestor origin/main origin/dev`, record the reviewed
    `origin/dev` SHA, then
    `git push --atomic --force-with-lease=refs/heads/dev:<reviewed-dev-sha> origin <reviewed-dev-sha>:refs/heads/main <reviewed-dev-sha>:refs/heads/dev`,
    then fetch again and require **both** remote heads to equal the recorded SHA. The
    lease is an expected-value assertion, not permission to rewrite. A GitHub merge,
    squash or rebase is **not** a promotion. Evidence the operator hands back: the
    granted authorisation text and the reviewed SHA. `scripts/Test-MainBranchHistory.ps1`
    and `tests/Pegasus.ArchitectureTests/MainBranchHistoryGuardTests.cs` are the
    detective guard on this; a failed preflight, rejected transaction or unequal
    read-back stops the release and is never repaired by a rebase, reset or force push.
12. **Confirm the release table.** `docs/operations.md:311-332` must now carry releases
    21–24; its newest row today is `| 20 | 2026-08-22 | 05fe7a7f… | … |
    20260822044425_GrantWorkerCaseDocuments |`. If the drift at `docs/operations.md:295`
    ("**Deployed evidence:** the estate currently serves **release 14**") is still wrong
    after the sync, record it as a one-line follow-up documentation ticket — do **not**
    silently rewrite unrelated upstream text in this PR.
13. **Re-stamp the parity rows the sync touched.**
    `git diff --name-only <recorded fork main>..<recorded upstream HEAD>`, then for every
    changed path under `src/Pegasus.Web/Pages`, `src/Pegasus.Core`,
    `src/Pegasus.Infrastructure/Custody` and `src/Pegasus.Infrastructure/Email`,
    re-check the matching `PAR-` rows in `parity-matrix.md:46-91`. **Note the shape
    the matrix actually has:** there is **no per-row inventoried-at column** — the
    baseline SHA appears once, at `parity-matrix.md:6` ("Pre-populated on 2026-08-23
    from the fork at `main` `191ddf33`"). Take the trivial default rather than
    redesigning the table: update that line to name the synced head and its date, and
    note per re-checked row in its evidence cell that it was re-verified at that head.
    Adding a per-row column is [[FND-014]]'s (plan handle `DSK-01-01`) skeleton work,
    not this ticket's. `PAR-17` at `:62` gains the upstream `CASE-019` export proof: its
    Test evidence cell currently reads "upstream CASE-019 test (after sync)" and becomes
    the real test path once the sync lands.
14. **Record the sync in `proof`.** The actual upstream HEAD SHA and date, the real
    commit count, the commit range, the upstream tickets it brought and those it did
    not, the reviewed SHA, the CI run link, the parity rows re-stamped, and the
    confirmation that no push to upstream was made. Then **confirm — do not create —**
    the standing-cadence ticket: `get_item FND-051` (plan handle `DSK-01-13`), which
    already exists and matches the § *Follow-up ticket to create* specification field
    for field. Record the confirmation. Tick every `open-questions/` item, call
    `get_doc_gates FND-023`, and move the ticket on.

## Verification

`proof` is produced from the outputs below. Evidence tier: **Tier 1 —
Static/build/architecture** (`docs/engineering.md` § Required evidence tiers) — this
proves the compile, dependency-direction, Bicep and documentation gates pass over the
merged tree; the merged upstream behaviour itself was proved by upstream's own release
evidence and is not re-derived here.

- `git rev-parse upstream/main` before the merge — expected: a SHA recorded verbatim in
  this plan as the head actually synced.
- `git log --oneline dev..upstream/main` — expected: empty after the merge.
- `git merge-base --is-ancestor origin/main origin/dev` — expected: exit 0 before the
  promotion push.
- `git rev-parse origin/main origin/dev` — expected: both equal the recorded reviewed
  SHA after the push.
- `git remote get-url --push upstream` — expected: `DISABLED_NO_PUSH_TO_UPSTREAM`.
- `grep -rn -i manifest docs/frd/frd-07-eva-and-external-engineering-handoff.md` —
  expected: either no mandate lines (upstream `DOCS-013` arrived) **or** lines 12, 42
  and 45 still present **and** recorded as an unarrived-item finding. Never left
  unexamined.
- `pwsh ./scripts/Test-MigrationGrants.ps1` — expected: exit code 0.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit code 0.
- GitHub Actions `repository-check` for the PR head — expected: every job succeeded or
  was path-skipped.
- `get_item FND-051` — expected: the standing later-sync ticket exists and matches the
  § *Follow-up ticket to create* specification; **no second one was created**.

## Risks / open questions

- **Risk — the recorded range is stale by construction.** `7d6a948a` and "32 commits"
  were read on 2026-08-23 and this ticket runs later. Syncing to a recorded SHA
  silently drops everything merged since, and under D-001 what is dropped is lost.
  *Mitigation:* step 4 re-derives the range and records both figures.
- **Risk — merging upstream straight into `main`.** `scripts/Test-MainBranchHistory.ps1`
  fails a push to `main` whose history is not contained in `dev`, and the guard is
  detective rather than a server-side prevention (GitHub rulesets are out of scope on
  subscription grounds, `docs/engineering.md:10-52`). *Mitigation:* step 7 cuts from
  `dev`; step 11 is the exact-SHA promotion.
- **Risk — an ADR-number collision.** Upstream keeps issuing ADRs below 0100 and the
  reserved block ADR-0100…ADR-0110 is currently empty on the fork. *Mitigation:*
  step 8's check after every merge; a collision is a blocking finding, not something to
  renumber around.
- **Risk — the migration census.** `IntakePersistenceIntegrationTests.cs:93` pins the
  head migration; a sync that adds migrations without census entries is a release trap.
  *Mitigation:* step 9 runs the integration shards before the PR.
- **Risk — CI cost.** C-01 makes private-repository Windows runners bill at 2×.
  *Mitigation:* steps 9 and 10 — run the local gate first, then one CI pass; no
  speculative re-runs.
- **Risk — the parity matrix has no field for what step 13 asks for.** Measured: the
  inventoried-at SHA is a single document-level line at `parity-matrix.md:6`, not a
  per-row column. *Mitigation:* step 13 takes the trivial default (update that line and
  annotate the re-checked rows' evidence cells) and leaves any column change to
  [[FND-014]] (plan handle `DSK-01-01`).
- **Finding to hand on, not scope here — bare upstream ids in the matrix.**
  `parity-matrix.md:96-99` lists `PLAT-023/025/026/027/028/029`, `AUTO-006/007`,
  `CASE-011/012/022`, `DOCS-011/012`, `INTK-019`, `UICASE-001` as bare ids. Several
  collide with live fork board ids — board [[PLAT-028]] and [[PLAT-029]] are the imports
  of upstream `PLAT-032` and `PLAT-038`, and board [[CASE-002]] is upstream `CASE-022`
  — so a reader can follow one of those bare ids to the wrong ticket. Owned by
  [[FND-052]] (board grooming — ambiguous carry-over ids); recorded here rather than
  fixed, because the matrix rows this ticket re-stamps are `:46-91`, not the notes.
- **Open question — has [[FND-002]] (plan handle `DSK-00-02`) already landed this
  sync?** Both tickets own the *first* sync from different sides. **Answered by:** the
  implementer at step 1, with `search_items`, before any git command. Recorded here
  rather than as an unticked `open-questions` box because it is answered inside the
  ticket in seconds; gating `leave-preparing` on it would stop the work that answers it.
- **Open question — the operator's `MERGE AUTH GRANTED`.** **Answered by:** the
  operator, at step 11, during implementation. Same reasoning: it is obtained inside
  the ticket, so it is a step, not a planning blocker.
- **Scope boundary, not a question — the standing sync cadence.** This ticket owns the
  **first** sync only. Every later sync up to and including the D-001 freeze belongs to
  [[FND-051]] (plan handle `DSK-01-13`), which already exists. Do not close this ticket
  as though it discharged the repeat obligation, and do not create a second
  standing-cadence ticket.
- **Scope boundary, not a question — FRD-07's manifest lines.** Upstream `DOCS-013`
  owns `docs/frd/frd-07-eva-and-external-engineering-handoff.md:12,42,45` until the
  freeze. Step 6 records the consequence; it does not edit the file.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's **own** diff before the PR, recorded here under a dated heading. The upstream
commits the merge brings are **not** in scope for the pass — say so explicitly in the
record. Expected result for the authored half: `n/a — docs-only`, since the only files
this ticket authors are `parity-matrix.md` and `upstream-kanmer-carryover.md`._
