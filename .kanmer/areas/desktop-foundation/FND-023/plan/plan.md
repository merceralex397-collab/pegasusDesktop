# Plan — FND-023: land the first one-way upstream sync from `upstream/main` into the fork's `dev`

**Diff estimate: fork-authored ~2 files, ~20 lines (about +16 / −4); plus the merged upstream content, whose size is re-derived at execution time and was ~32 commits at the 2026-08-23 baseline.**

`docs/engineering.md` § Plan sizing requires the estimate first. Profile `chore` owes
neither `research` nor `files`, so this plan carries the surface-area burden alone and
the estimate is derived from the measured inventory below.

The two numbers are deliberately separate. The **fork-authored** diff is what a
reviewer reads line by line; the **merged upstream** diff is content this ticket does
not write and does not review line by line (it was proved by upstream's own release
evidence), which is also why the `## Simplification pass` covers the branch's own diff
only.

### Measured file-and-line inventory (read 2026-08-24 at `bbd1c549`, branch `task/desktop-plan-segmentation`)

Fork-authored edits — the only repository files this ticket may deliberately edit
(Guardrails § Scope boundary):

| Path | Measured now | What this ticket changes | Added | Removed |
| --- | --- | --- | --- | --- |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | **105 lines**; `grep -c '^\| PAR-'` → **46 rows** at `:46`–`:91`; § Legend `:9`, § Matrix `:42`, § Notes `:93` | Re-stamp the inventoried-at baseline and re-check the rows the sync touched. **There is no per-row inventoried-at column** — `grep -n '191ddf33'` returns exactly one hit, `:6` ("Pre-populated on 2026-08-23 from the fork at `main` `191ddf33`"), so the re-stamp is that sentence plus a per-row note where a row's evidence changed. `PAR-17` at `:62` already names upstream `CASE-019` and its commit `efbb2a9`, so it is the row that gains real test evidence. | ~9 | ~3 |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | **224 lines**; § Code drift and the first sync at `:197`–`:224`; the 32-commit claim at `:199`–`:201` | Two dated lines in § Code drift: (a) the first sync landed, with the **actual** upstream HEAD SHA, date and commit count; (b) the range is re-derived at execution time, so a later reader does not treat `7d6a948a` / "32 commits" as the instruction | ~7 | ~1 |
| **Total (fork-authored)** | | | **~16** | **~4** |

Merged upstream content, re-derived at execution time (step 4) and **never replayed
from a recorded list**: at the 2026-08-23 baseline `git log --oneline main..upstream/main`
was 32 commits from `191ddf33` to `7d6a948a`. Its size at execution is the number step
4 records, and it is expected to be larger.

Repository mechanisms this plan depends on, each verified present on 2026-08-24:

| Path | Measured | Why it matters here |
| --- | --- | --- |
| `docs/engineering.md:10-52` | § Branches and delivery; the exact-SHA promotion block is at `:17-24`, the literal `MERGE AUTH GRANTED` sentence at `:29-30`, and `:33-35` states a GitHub merge/rebase/squash is **not** a promotion | Step 11 is executed verbatim from here |
| `scripts/Test-MainBranchHistory.ps1` | **58 lines**, present | Fails a push to `main` whose history is not contained in `dev` |
| `scripts/Test-MigrationGrants.ps1` | **99 lines**, present | Local gate in step 9 |
| `scripts/Test-DocumentationLinks.ps1`, `scripts/Test-MarkdownPlacement.ps1` | present | Local gates in step 9 |
| `scripts/Invoke-TestShard.ps1` | **216 lines**, present | The integration shards in step 9 |
| `.github/workflows/ci.yml` | **234 lines**; jobs at `:12` `changes`, `:71` `documentation`, `:89` `local-development-scripts`, `:100` `reference-data`, `:115` `infrastructure`, `:131` `unit`, `:149` `sql-integration`, `:185` `sql-integration-coverage`, `:207` `browser` — **nine jobs** | "Green" in step 10 means every one succeeded or was path-skipped |
| `docs/operations.md` | **920 lines**; release table header at `:311`, newest row **release 20** (2026-08-22) at `:313`; the drift line "the estate currently serves **release 14**" at `:295` | Step 12 checks releases 21–24 arrive and does not silently rewrite `:295` |
| `docs/frd/frd-07-eva-and-external-engineering-handoff.md` | **91 lines**; `grep -n -i manifest` → **exactly three hits**, `:12`, `:42`, `:45` — the mandate is at `:12` ("SHA-256 manifest over the JSON and image identities and bytes. Stable manifest…"), the download clause at `:42`, the no-removal clause at `:45` | Step 6's finding, and the file this ticket must **not** edit |
| Git state | `git remote -v` → **`origin` only** (no `upstream`); `git branch -a` → `kanmer-board`, `main`, `task/desktop-plan-segmentation`, `remotes/origin/main` — **there is no `dev` branch yet** | Step 2 stops the ticket if `dev` is absent; it is absent today |

## Approach

Treat the sync as a **range re-derivation followed by the standard branch-and-promote
procedure**, not as a replay of the recorded 32-commit set: fetch `upstream/main`,
record its actual HEAD and the real commit count, prove the fast-forward, merge into a
task branch cut from `dev`, run every local gate, take the independent review and green
CI, then promote by the exact-SHA atomic fast-forward after an explicit
`MERGE AUTH GRANTED`. The rejected alternative is syncing to the recorded SHA
`7d6a948a` because it is written down and reproducible: under D-001 anything merged
upstream after that point and before the freeze is **lost, not deferred**, so a
reproducible sync to a stale SHA silently drops production fixes — and the ticket body
names four already-known items (upstream `DOCS-013`, `ENG-014`, `ENG-015`,
`INTK-033`) that sit outside that range today. The second rejected alternative,
merging `upstream/main` straight into `main` to save a hop, is barred outright by
`scripts/Test-MainBranchHistory.ps1` and by `docs/engineering.md:33-35`.

## Governing docs

`refs` on this ticket is **empty** and `docs_todo: true` is set — confirmed by
`get_doc_gates FND-023`, which reports `"refs": []` and `"docs_todo": true`, and
resolves profile `chore` to `leave-preparing: [plan, questions-resolved]` and
`enter-done: [proof, questions-resolved]`. `chore` has no `leave-backlog`
`governing-doc` requirement; only `feature` does.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client converted inside this fork;
> its **consequences** section is where D-001 — the fork becoming the single release
> source, upstream merged one final time and then frozen — is recorded), authored by
> [[FND-005]] (plan handle `DSK-00-05`). **ADR-0100 has more than one claimant** —
> [[FND-026]] (plan handle `DSK-02-01`) also names it — so see [[FND-005]]'s plan for
> the ownership reconciliation rather than asserting a single author. The recording of
> D-001 into ADR-0100's consequences and into `docs/operations.md` is owned by
> [[FND-010]] (plan handle `DSK-00-10`), which also agrees the freeze with the
> upstream owners. This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (**D-001, decided
> 2026-08-23**) and `docs/desktop/README.md` § Locked decisions; if the ADR lands
> differently this plan is revised before implementation.

Because `refs` is empty, the New-ADR paragraph alone is not sufficient. The
programme-level authorities that bind **today**, each with the step that satisfies it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/engineering.md:10-52` § Branches and delivery | Task branches cut from `dev`, PR into `dev`; `main` promoted only by exact-SHA atomic fast-forward with an explicit `MERGE AUTH GRANTED`; a GitHub merge/squash/rebase is not a promotion; `dev` and `main` are never rebased, reset or force-pushed | Steps 7, 10, 11 |
| `docs/engineering.md` § Green | Every `repository-check` job for the PR head succeeded or was path-skipped — nine jobs, enumerated above from `.github/workflows/ci.yml` | Step 10; Verification row 9 |
| `docs/desktop/00-governance-and-workflow/README.md` § Recommended branching flow item 2 | Add `upstream` as a **read-only** remote and sync one-way after each upstream release until cutover; never push fork → upstream; verify the fast-forward with `git merge-base --is-ancestor` | Steps 3, 4; Verification rows 1, 3, 5 |
| `docs/desktop/README.md` § Locked decisions — **D-001** (2026-08-23) | The fork becomes the single release source at the first production gateway change; upstream is merged once more and then frozen, so anything unmerged at the freeze is lost | Steps 4, 5, 6; Risks |
| `docs/desktop/README.md` § Locked decisions — **L-01** | The gateway is `Pegasus.Web` evolved in place, which is why upstream web fixes still matter to the conversion | Approach; step 5 |
| `docs/desktop/README.md` § Constraints — **C-01** (2026-08-23) | The repositories become private; private-repository Windows runners bill at a 2× multiplier, so CI minutes have a real cost | Step 9/10 — no speculative re-runs; Risks |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § Code drift (`:197`–`:224`) | The sync procedure, and its own rule "Repeat after each upstream release until cutover" | Steps 4–13; step 14 hand-over |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 Deviation: reserved ADR block | The conversion uses ADR-0100…ADR-0110 and never "next free number"; check `docs/adr/README.md` after every sync for a collision | Step 8; Verification row 10 |
| Proposal § 6.3 (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md`) | Fork, not greenfield; no permanent second Pegasus repository | Approach; step 11 |
| `docs/desktop/00-governance-and-workflow/README.md` § Ticket template | The plan document carries a `## Routing` block and a dated `## Simplification pass` | This document's `## Routing` and `## Simplification pass` |
| `AGENTS.md` § Repository task workflow steps 4–5 | Simplification pass over the branch's **own** diff before the PR; independent review by an agent that did not implement | `## Simplification pass`; Routing → reviewer |
| `docs/engineering.md` § Required evidence tiers | Tier 1 — static/build/architecture | Verification |
| `docs/runbook.md` § Live-operation approval matrix | No Azure write; a gateway release is a separate runbook-controlled action | Guardrails; Risks |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory
in the plan document specifically.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
  (verified present).
- **Skills**, loaded in this order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md` (verified present)
  2. `run-tests` — `dotnet/skills` `98f84851`, plugin `dotnet-test` (pinned source per
     `docs/desktop/README.md` § Routing legend and
     `docs/desktop/12-agent-tooling/skill-routing.md`)
- **MCP**: Kanmer only — `get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`. **No Azure MCP tool and no
  Microsoft Learn tool is called on this ticket.**
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-verify` → `kanmer-closeout`. Gated boundaries from `get_doc_gates FND-023`:
  `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` +
  `questions-resolved`. Call `get_doc_gates` before every move and cross at most one
  gated boundary per move.
- **Reviewer**: `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml` (verified present); an agent that did
  not implement reviews the sync diff (`AGENTS.md` § Repository task workflow step 5;
  area 00 § 6 routing table).

## Steps

These refine the ticket body's fourteen implementation steps — same order, same
ownership, same file paths — adding the *how* the body leaves out.

1. **Orientation and take.** Read
   `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § Code drift
   and the first sync (`:197`–`:224`),
   `docs/desktop/00-governance-and-workflow/README.md` § Recommended branching flow,
   and `docs/engineering.md:10-52` in full. Then `search_items` for `DSK-00-02` and
   `get_item` it: **if [[FND-002]] (plan handle `DSK-00-02`) has already landed the
   sync, this ticket does steps 9–13 only.** `get_doc_gates FND-023`, then
   `take_ticket`.
2. **Confirm `dev`.** `git branch --list dev` and `git rev-parse dev main`.
   **Measured today: `dev` does not exist** — `git branch -a` returns `kanmer-board`,
   `main`, `task/desktop-plan-segmentation`, `remotes/origin/main`. If it is still
   absent, **stop**: [[FND-001]] (plan handle `DSK-00-01`) owns creating it and this
   ticket is blocked. This is the `blocked: true` flag the board already carries.
3. **Add `upstream` read-only and fetch.** Measured today: `git remote -v` shows
   **`origin` only**, so the remote is not yet present.
   ```
   git remote add upstream https://github.com/collisionengineers/pegasus.git
   git remote set-url --push upstream DISABLED_NO_PUSH_TO_UPSTREAM
   git fetch upstream main --no-tags
   ```
   `DISABLED_NO_PUSH_TO_UPSTREAM` is the **literal** string [[FND-002]] already uses —
   use it verbatim, and if that ticket added the remote first, leave the value alone
   rather than shortening it.
4. **Re-derive the range; never replay the recorded set.** Record
   `git rev-parse upstream/main` as the actual HEAD being synced (`7d6a948a` was the
   head on 2026-08-23 and upstream has almost certainly moved). Then
   `git merge-base --is-ancestor $(git rev-parse main) upstream/main` — **expect exit
   code 0**; a non-zero result means the histories diverged, so **stop and raise it**
   and force nothing. Then `git log --oneline main..upstream/main | wc -l` — the
   planning baseline said 32; state the **actual** number and, if it differs, say so
   plainly rather than reconciling it to 32. Write the HEAD SHA, the date and both
   figures into this plan.
5. **Name every upstream ticket the sync brings that postdates the 2026-08-23
   triage.** `git log --oneline --no-merges main..upstream/main` and read the ticket id
   out of each subject. Known cases at planning time: upstream `DOCS-013` (at `review`
   on `task/docs-013-strike-eva-manifest`), upstream `ENG-014` (on
   `task/eng-014-drop-manifest-indent-json`, PR #527 against `dev`), upstream `ENG-015`
   (no branch at all). For each that arrives, tick it off the carry-over register; for
   each that has **not**, record that fact here, because under D-001 it is lost at the
   freeze unless a fork ticket holds it. Note that board [[ENG-001]] and [[ENG-002]]
   are the fork imports of upstream `ENG-014` and upstream `ENG-015` — write the
   upstream ids in full, never bare, per `HZN-001`/`board-conventions.md`.
6. **If upstream `DOCS-013` has not merged, record the FRD-07 consequence — do not fix
   it here.** Measured today: `grep -n -i manifest docs/frd/frd-07-eva-and-external-engineering-handoff.md`
   returns **exactly three hits — `:12`, `:42`, `:45`** — so the fork still mandates
   the invented SHA-256 manifest, including the clause forbidding its removal. If
   upstream `DOCS-013` has not arrived by the D-001 freeze it must be recreated as a
   fork docs chore against the fork's own FRD-07, and it must land **together with**
   upstream `ENG-014` (board [[ENG-001]]): the doc change and the code change are two
   halves of one correction, and either alone leaves the governing document and the
   produced package contradicting each other. Raise it as a named finding in this plan;
   `docs/frd/frd-07-…` is **not** in this ticket's editable set.
7. **Branch and merge — into `dev`, never into `main`.**
   ```
   git switch dev && git pull --ff-only
   git switch -c task/dsk-01-10-first-upstream-sync
   git merge upstream/main
   ```
   A merge commit is acceptable (the carry-over document says so at `:220`). Resolve
   conflicts **in favour of upstream** for files upstream owns; do not "fix forward" in
   the fork what upstream owns — raise those upstream instead (area 00 § 7 trap 4).
8. **ADR-collision check, immediately after the merge.** `ls docs/adr/` and
   `sed -n '1,80p' docs/adr/README.md`. Upstream keeps issuing ADRs from the low
   numbers (ADR-0001…ADR-0029, 0017 never issued); the conversion uses the reserved
   block **ADR-0100…ADR-0110** and must never take "next free number". A number the
   fork also used is a **blocking finding**.
9. **Full local gate before the PR** — every command exits 0:
   `pwsh ./scripts/Test-DocumentationLinks.ps1`, `pwsh ./scripts/Test-MarkdownPlacement.ps1`,
   `pwsh ./scripts/Test-MigrationGrants.ps1` (99 lines, verified present), then the
   test lanes through the `run-tests` skill: `dotnet test` over
   `tests/Pegasus.Core.Tests` and `tests/Pegasus.ArchitectureTests`, and
   `pwsh ./scripts/Invoke-TestShard.ps1` (216 lines, verified present) for the
   integration shards. Done also means the pinned migration census in
   `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` still matches
   the migration folder — a new upstream migration that is not in the census is a
   release trap.
10. **PR into `dev`, independent review, green CI.** Request
    `pegasus-desktop-reviewer`. "Green" is the nine `repository-check` jobs enumerated
    above from `.github/workflows/ci.yml` — `changes` `:12`, `documentation` `:71`,
    `local-development-scripts` `:89`, `reference-data` `:100`, `infrastructure`
    `:115`, `unit` `:131`, `sql-integration` `:149`, `sql-integration-coverage`
    `:185`, `browser` `:207` — each **succeeded or path-skipped**. Merge into `dev`
    only after both. C-01: private Windows runners bill at 2×, so do not re-run lanes
    speculatively.
11. **Operator step — promotion to `main`.** The release actor needs the literal
    `MERGE AUTH GRANTED` from the operator before the push. Execute
    `docs/engineering.md:17-24` verbatim: fetch both remote refs, confirm
    `git merge-base --is-ancestor origin/main origin/dev`, record the reviewed
    `origin/dev` SHA, then
    ```
    git push --atomic --force-with-lease=refs/heads/dev:<reviewed-dev-sha> origin <reviewed-dev-sha>:refs/heads/main <reviewed-dev-sha>:refs/heads/dev
    ```
    then fetch again and require **both** remote heads to equal the recorded SHA. The
    lease is an expected-value assertion, not permission to rewrite. A failed
    preflight, a rejected transaction or an unequal read-back **stops the release** and
    is never repaired by a rebase, reset or force push. A GitHub merge, squash or
    rebase is **not** a promotion. Evidence handed back: the granted authorisation text
    and the reviewed SHA.
12. **Release table.** Confirm `docs/operations.md` now carries releases 21–24. Today
    the table header is at `:311` and the newest row is **release 20** (2026-08-22) at
    `:313`. The drift line at `:295` ("the estate currently serves **release 14**") is
    a *separate* one-line follow-up documentation ticket if upstream has not already
    fixed it — do **not** silently rewrite unrelated upstream text in this PR.
13. **Re-stamp the parity rows the sync touched.**
    `git diff --name-only <recorded fork main>..<recorded upstream HEAD>`, then for
    every changed path under `src/Pegasus.Web/Pages`, `src/Pegasus.Core`,
    `src/Pegasus.Infrastructure/Custody` and `src/Pegasus.Infrastructure/Email`,
    re-check the matching `PAR` rows in
    `docs/desktop/01-inventory-and-parity/parity-matrix.md` (46 rows at `:46`–`:91`).
    **The matrix has no per-row inventoried-at column** —
    `grep -n '191ddf33' parity-matrix.md` returns exactly one hit, `:6` — so the
    re-stamp is: update the `:6` sentence to the post-sync fork `main` SHA and its
    date, and add the commit reference inline in the row's evidence cell where a row's
    evidence actually changed. `PAR-17` at `:62` is the row that gains real test
    evidence: it already names upstream `CASE-019` and its commit `efbb2a9`, with test
    evidence currently written "upstream CASE-019 test (after sync)". Record the
    absence of a per-row column as a finding for the matrix owner rather than inventing
    a column here.
14. **Proof, and hand over the standing cadence.** Write the ticket `proof`
    (`set_ticket_doc`): the actual upstream HEAD SHA and date, the real commit count,
    the commit range, the upstream tickets it brought and those it did not, the
    reviewed SHA, the CI run link, the parity rows re-stamped, and the confirmation
    that no push to upstream was made. **The follow-up ticket the body's § Follow-up
    ticket to create specifies already exists on this board as [[FND-051]] (plan
    handle `DSK-01-13`, "Standing later upstream syncs up to the D-001 freeze",
    `desktop-foundation`, `chore`, groups `EPIC-002` + `HZN-010`)** — so step 14's
    hand-over is discharged by confirming [[FND-051]] with `get_item` and recording its
    id in the proof, **not** by creating a second standing-cadence ticket, which the
    body's Guardrails make a stop condition. Then tick every `open-questions/` item if
    any exist, call `get_doc_gates FND-023`, and move.

## Verification

Evidence tier **1 — static/build/architecture** (`docs/engineering.md` § Required
evidence tiers), as the ticket body states: each sync is proved by history
containment, a green `repository-check` run and the read-back promotion, not by
application behaviour. The merged upstream behaviour was proved by upstream's own
release evidence and is not re-derived here. The `proof` document is a `command-log`
built from the following, each output pasted raw:

| # | Command / call | Expected |
| --- | --- | --- |
| 1 | `git rev-parse upstream/main` (before the merge) | a SHA, recorded verbatim in this plan as the head actually synced — **not** assumed to be `7d6a948a` |
| 2 | `git log --oneline main..upstream/main \| wc -l` (before the merge) | the real commit count, recorded; 32 was the 2026-08-23 baseline only |
| 3 | `git merge-base --is-ancestor $(git rev-parse main) upstream/main; echo $?` | `0` — a fast-forward; non-zero stops the ticket |
| 4 | `git log --oneline dev..upstream/main` (after the merge) | empty |
| 5 | `git remote get-url --push upstream` | `DISABLED_NO_PUSH_TO_UPSTREAM` — the same literal [[FND-002]] sets |
| 6 | `git merge-base --is-ancestor origin/main origin/dev; echo $?` (before the promotion push) | `0` |
| 7 | `git rev-parse origin/main origin/dev` (after the push) | two identical SHAs, both equal to the recorded reviewed SHA |
| 8 | `grep -rn -i manifest docs/frd/frd-07-eva-and-external-engineering-handoff.md` | either **no** mandate lines (upstream `DOCS-013` arrived) or `:12`, `:42`, `:45` still present **and** recorded as an unarrived-item finding — never silently left unexamined. **Measured 2026-08-24: three hits, `:12`, `:42`, `:45`** |
| 9 | GitHub Actions `repository-check` for the PR head | all nine jobs succeeded or path-skipped |
| 10 | `ls docs/adr/` and `sed -n '1,80p' docs/adr/README.md` (after the merge) | no number in ADR-0100…ADR-0110 taken by an upstream ADR |
| 11 | `pwsh ./scripts/Test-MigrationGrants.ps1` | exit code 0 |
| 12 | `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit code 0 |
| 13 | `pwsh ./scripts/Test-MarkdownPlacement.ps1` | exit code 0 |
| 14 | `sed -n '311,320p' docs/operations.md` | the release table now carries rows 21–24 above the release 20 row currently at `:313` |
| 15 | Kanmer `get_item FND-051` | the standing later-sync ticket exists; its id is recorded in the proof and **no second one was created** |

## Risks / open questions

- **Blocked today, and correctly so.** `dev` does not exist (`git branch -a`,
  2026-08-24) and `upstream` is not a remote (`git remote -v`). *Mitigation:* step 2
  stops the ticket; [[FND-001]] (plan handle `DSK-00-01`) owns creating `dev`. This is
  a dependency, not an open question.
- **The recorded range is stale by construction.** `7d6a948a` and "32 commits" were
  read on 2026-08-23. *Mitigation:* step 4 re-derives both from the live head and
  records them; under D-001 what a stale range drops is lost, not deferred.
- **Ownership overlap with [[FND-002]] (plan handle `DSK-00-02`).** Both own the first
  sync, from the governance side and the inventory side. *Mitigation:* step 1's
  `search_items` check, and the explicit reduction to steps 9–13 if the sync has
  already landed. Coordinate; do not duplicate.
- **Upstream keeps issuing ADRs below 0100.** *Mitigation:* step 8's collision check
  after every merge; a collision is blocking, not cosmetic.
- **The migration census is a release trap.** A new upstream migration missing from
  `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs` fails late.
  *Mitigation:* step 9 checks it before the PR opens.
- **CI cost is real (C-01).** Private-repository Windows runners bill at 2×.
  *Mitigation:* one full run per PR head; no speculative re-runs.
- **The sync brings Razor/web changes the conversion intends to retire.**
  *Mitigation:* merge them anyway until cutover; the web app is live. Do not fix
  forward in the fork what upstream owns.
- **Operator step, already scheduled:** the literal `MERGE AUTH GRANTED` before the
  promotion push (step 11), and the label `needs-operator` on this ticket records it.
  Answered by the operator at execution time.
- **Scope boundary, not an open question:** `docs/frd/frd-07-eva-and-external-engineering-handoff.md`
  is owned by upstream `DOCS-013` until the freeze. This ticket records the finding at
  step 6 and edits nothing there.
- **Scope boundary, not an open question:** the standing sync cadence after this first
  sync is owned by [[FND-051]] (plan handle `DSK-01-13`), which already exists. A
  second standing-cadence ticket is a stop condition, so step 14 confirms rather than
  creates.
- **Scope boundary, not an open question:** the `docs/operations.md:295` "release 14"
  drift is a separate one-line documentation ticket if upstream has not fixed it; it is
  not repaired inside this PR.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's **own** diff before the PR, recorded here under a dated heading. **The merged
upstream commits are explicitly out of scope for the pass** and the record must say
so; the pass covers only the fork-authored edits to
`docs/desktop/01-inventory-and-parity/parity-matrix.md` and
`docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`, for which the
expected result is `n/a — docs-only`._

## Operator scope amendment — 2026-08-25

The operator has prohibited all synchronization with the upstream Pegasus repository. This supersedes the original first-sync procedure, acceptance criteria, verification commands, and follow-up cadence.

This ticket is now an in-repository record only:

- Do not add, fetch, merge, validate, or push an `upstream` remote.
- Use the configured `pegasusDesktop` remote and the repository's existing `origin/dev` and `origin/main` history only.
- Do not import upstream commits or wait for upstream changes.
- Record the in-repository baseline and this boundary in the owned repository documentation and Kanmer proof.

The amended acceptance is: the repository-only source boundary is documented, no upstream operation is performed, and downstream tickets proceed from the current in-repository baseline.
