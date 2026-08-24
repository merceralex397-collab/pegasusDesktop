# Plan — FND-040: CI lane `desktop-build` on `windows-latest`

**Diff estimate: ~5 files, ~50 lines.**

## Measured file-and-line inventory

Profile `chore` owes no `research` and no `files` document, so this plan carries the
surface-area burden alone (`.grok/skills/kanmer-plan/assets/plan-template.md`'s
"written FROM research and files" precondition does not apply). Every row was measured
against the fork working tree on 2026-08-24 with `wc -l`, `cat -n` and `grep -n`.

| Path | Measured now | Change | Lines |
| --- | --- | --- | --- |
| `.github/workflows/ci.yml` | 234 lines, one workflow `repository-check` (`:1`), nine job definitions: `changes` `:12`, `documentation` `:71`, `local-development-scripts` `:89`, `reference-data` `:100`, `infrastructure` `:115`, `unit` `:131`, `sql-integration` `:149`, `sql-integration-coverage` `:185`, `browser` `:207`. `grep -n 'runs-on'` returns nine hits: seven `windows-latest`, two `ubuntu-latest` (`:15` `changes`, `:194` `sql-integration-coverage`) | Add one `desktop-build` job after `unit` ends at `:147`, before `sql-integration` at `:149`, in the `unit` job's style (a comment above the job saying what it proves) | +~28 |
| `.github/actions/dotnet-build/action.yml` | 27 lines. `cache-dependency-path` `:17-20` (`global.json`, `src/**/packages.lock.json`, `tests/**/packages.lock.json`); `dotnet restore ./Pegasus.slnx --locked-mode` `:24`; `dotnet build ./Pegasus.slnx --configuration Release --no-restore` `:27` | Step 2's split: point the composite at the server entry point and add `Directory.Packages.props` to the cache key, with a comment naming the reason | ~+8 / 2 changed |
| `scripts/Get-CiChangeFlags.ps1` | 26 lines; `$buildPattern` at `:11` already matches `^(src\|tests)/`, `^Pegasus\.slnx$`, `\.csproj$`, `\.props$`, `\.targets$`, `packages\.lock\.json$`, `^global\.json$`, `^nuget\.config$` and `^\.github/(workflows/ci\.yml\|actions/)` | **Probably unchanged.** Only if step 2 introduces a `.slnf` file does `:11` gain that pattern | +0 or 1 changed |
| `scripts/Test-CiChangeFlags.ps1` | 37 lines | Only if `Get-CiChangeFlags.ps1:11` changes: one regression case | +0 or ~3 |
| `docs/engineering.md` § Branches and delivery (`:10`) | The section runs `:10-…`; `grep -n 'desktop-build' docs/engineering.md` returns nothing | One line naming the `desktop-build` lane and what it produces | +2 |
| `docs/runbook.md` § Locked restore, build, and test (`:298`) | `:300` explains `--locked-mode`; `:303` is the literal `dotnet restore ./Pegasus.slnx --locked-mode` | One note that the desktop projects restore with `-r win-x64` | +3 |

Not touched, and each is a deliberate exclusion recorded under *Risks* below: every test
project's contents, every `csproj`, `scripts/Build-ReleaseArtifacts.ps1`, and **any
packaging, signing or artifact-upload step**.

## Approach

**Add one narrow, path-gated Windows job that proves the desktop restores, builds x64 Release
and passes its tests — and move the whole-solution build off the shared composite action so
the seven existing Windows lanes do not silently start paying for the desktop projects.**
The alternative rejected is **adding the two desktop test projects to the existing `unit`
job** (`ci.yml:145-147`): it looks like the smaller diff, but `unit` already builds through
the composite action, so the desktop cost would land on `unit`, `sql-integration` ×3 and
`browser` alike, and a desktop failure would redden lanes that have nothing to do with it.
The second alternative rejected is **leaving the composite on `Pegasus.slnx` and accepting
the added minutes** — step 2 keeps it as an explicit option because it is the honest fallback
if the server entry point from [[FND-028]] (plan handle `DSK-02-03`) has not landed, but
under C-01 it is the expensive answer: private-repository `windows-latest` minutes bill at a
**2× multiplier** (`docs/desktop/08-testing/README.md` § 7, first bullet), and this workflow
already runs seven Windows jobs.

**This lane packages nothing.** The body's step 10 is a scope boundary, not a preference:
[[REL-005]] (plan handle `DSK-09-05`) owns the `desktop-package` lane and its dev-certificate
package and artifact upload, and [[TEST-013]] (plan handle `DSK-08-13`) extends the lane set
later. Three plans name an overlapping desktop CI lane; exactly one `desktop-package` job may
ever exist, and this ticket does not create it.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(confirmed by `get_doc_gates FND-040`). No existing PRD, FRD or ADR is claimed to be met.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client inside this fork, which authorises
> the desktop projects this lane restores, builds and tests), authored by [[FND-026]]
> (plan handle `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`) also claims ADR-0100 —
> see [[FND-026]]'s plan for the ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 5 (server projects stay
> Linux-publishable behind a solution filter) and `docs/desktop/README.md` § Constraints
> (C-01); if ADR-0100 lands differently this plan is revised before implementation.
> No ADR governs a CI job on its own — `AGENTS.md` § Product invariants reserves that for a
> new project, runtime, deployment unit or migration stream, and this lane is none of those.

Because `refs` is empty, the programme-level authorities that bind today, each with the step
that satisfies it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| **C-01** (`docs/desktop/README.md` § Constraints) | Private-repository Windows runners bill at a 2× multiplier against a monthly allowance | Steps 2, 3 and 9 — the lane is path-gated, the composite split is measured, and the minutes are recorded for [[TEST-019]] (plan handle `DSK-08-19`) |
| **D-002** | The production `.pfx` never leaves the signing host and is never a GitHub secret | Step 10 — CI signs nothing here; a PR build would use a job-generated development certificate, and that lane is [[REL-005]]'s |
| **D-003** | The feed is an in-house UNC share | The Guardrails' "no artifact is published to any feed from CI" |
| Proposal § 21.2 CI stages | Locked restore, Release build, unit and view-model tests are distinct stages | Steps 5 and 6 |
| Proposal § 24 Phase 1 | A CI Windows build exists before the Phase 1 gate | The lane itself; consumed by [[FND-041]] (plan handle `DSK-02-16`) |
| Plan 02 § 2 assumption **A3** | `windows-latest` restoring the Windows SDK build-tools packages and running `winapp` is "to be proven by the CI lane ticket, not assumed green" | Step 7, which answers A3 with a real run link either way |
| Plan 02 § 3 decision 5 | Desktop projects join `Pegasus.slnx` **and** a server solution filter keeps Linux builds green | Step 2 |
| Plan 02 § 5 row `DSK-02-15` acceptance | "Job green on PR; Linux jobs unaffected (slnf)" | Steps 2 and 8 |
| Plan 02 § 7 trap "Lock files with Windows-only packages" | RID/TFM-specific lock files; CI must restore with the same RID | Step 5's `-r win-x64` |
| Plan 08 § 5 row `DSK-08-13` | The four-lane target `desktop-build`, `desktop-package`, `desktop-ui-smoke`, `packaging-tests` | Step 10 — this ticket owns **only** `desktop-build` |
| Plan 08 § 7 first bullet | The 2× Windows multiplier, and self-hosted runners as the first mitigation | Steps 2 and 9; the decision itself belongs to [[TEST-019]] |
| `docs/engineering.md:76` § Required evidence tiers, tier 1 | "compile the four approved projects, enforce dependency direction… This proves consistency only" | Verification, which states the limit |
| `docs/engineering.md:201` § Plan sizing | A plan states its diff estimate first, from a real number | The first line and the inventory above |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 10 |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing, reviewer `pegasus-desktop-reviewer` |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `authoring-github-workflows`
  (dotnet/skills `98f84851`, expected at `.agents/skills/authoring-github-workflows/SKILL.md`;
  not vendored there today, so it arrives with [[TOOL-002]], plan handle `DSK-12-02`) →
  `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`, win-dev-skills v0.5.0
  `f1028dd5`, verified present — read for the `microsoft/setup-WinAppCli@v0.1` step only) →
  `binlog-failure-analysis` (dotnet/skills `98f84851`) when the lane fails.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for
  `actions/setup-dotnet` caching and `dotnet restore --locked-mode` with a runtime
  identifier).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates FND-040` before
  every move; a move crosses at most one gated boundary. `chore` owes `plan` at
  `leave-preparing` and `proof` at `enter-done`, and no `research`, `files` or `checklist`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's ten implementation steps in the same order, with the same ownership
and the same file paths. Each names a measured current value.

1. **Orient and take.** Read `.github/workflows/ci.yml` in full (234 lines — it is short
   enough that skimming it is a false economy) and `.github/actions/dotnet-build/action.yml`
   (27 lines). Read plan 02 § 2 assumption A3, § 3 decision 5 and § 7; plan 08 § 5 row
   `DSK-08-13` and § 7; `docs/desktop/README.md` § Constraints. Confirm the two prerequisites
   exist — `ls tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle `DSK-02-13`)
   and `ls src/Pegasus.Desktop` ([[FND-030]], plan handle `DSK-02-05`); if either is missing,
   stop rather than writing a lane that references nothing. Then `get_doc_gates FND-040`,
   `take_ticket FND-040`, and branch `task/ci-desktop-build` from `origin/dev`.
2. **Decide and record the solution split before touching the workflow.** The composite
   action runs `dotnet restore ./Pegasus.slnx --locked-mode` (`action.yml:24`) and
   `dotnet build ./Pegasus.slnx --configuration Release --no-restore` (`:27`), and **six**
   jobs use it — `documentation` has no build, but `unit` (`:131`), the three
   `sql-integration` shards (`:149`) and `browser` (`:207`) all do — so once the desktop
   projects join `Pegasus.slnx` every one of them starts building them. Choose one and record
   the choice with its measured minute impact in this plan:
   **(a)** point the composite at the server entry point [[FND-028]] creates and let only
   `desktop-build` build the full `Pegasus.slnx` — the plan's stated intent
   ("Linux jobs unaffected (slnf)", plan 02 § 5) and the cheaper answer under C-01; or
   **(b)** keep the composite on `Pegasus.slnx` and accept the added minutes on every lane —
   honest only if [[FND-028]] has not landed, and then the follow-up is named here.
   Whichever is chosen, add `Directory.Packages.props` to the composite's
   `cache-dependency-path` (`:17-20`) if [[FND-027]] (plan handle `DSK-02-02`) has landed:
   central package management changes the graph and must therefore change the cache key.
3. **Add the `desktop-build` job** to `.github/workflows/ci.yml` immediately after `unit`
   ends at `:147` and before `sql-integration` at `:149`, so the two build-and-test lanes read
   together. Give it `needs: changes`, `if: needs.changes.outputs.build == 'true'`,
   `runs-on: windows-latest`, `timeout-minutes: 30`, and a comment above the job in the house
   style — every job in this file carries one (see `:132-133` on `unit`, `:150-158` on
   `sql-integration`) saying what it proves and why. Say plainly: this lane proves the
   desktop restores, builds x64 Release and passes its tests; it is Windows-only because the
   projects target `net10.0-windows10.0.26100.0`; and it packages nothing.
4. **Confirm the path gate needs no change and say so rather than editing it.**
   `scripts/Get-CiChangeFlags.ps1:11` `$buildPattern` already matches `^(src|tests)/`,
   `\.csproj$`, `\.props$`, `\.targets$` and `packages\.lock\.json$`, so every desktop path
   already sets `build=true`; the `changes` job feeds that through `:52-53` into the
   `build` output this job's `if` reads. Record the finding in this plan. **Only** if step 2
   introduces a new file type — a `.slnf`, for instance — does `:11` gain that pattern, and
   then `scripts/Test-CiChangeFlags.ps1` (37 lines) gains the matching regression case in the
   same commit: the script and its test move together or neither moves.
5. **Job steps, in order**: `actions/checkout@v7` (the version every job in this file uses);
   `actions/setup-dotnet@v6` with `dotnet-version: 10.0.x` and `cache: true` keyed on
   `global.json`, `Directory.Packages.props`, `src/**/packages.lock.json` and
   `tests/**/packages.lock.json`; then
   `dotnet restore ./Pegasus.slnx --locked-mode -r win-x64`; then
   `dotnet build ./Pegasus.slnx --configuration Release --no-restore -p:Platform=x64`.
   The `-r win-x64` is not decoration: plan 02 § 7 records that Windows-only package graphs
   produce RID/TFM-specific lock files, and [[FND-038]] generates its lock file with exactly
   that RID. A mismatch fails `--locked-mode`, and the fix is always to match the RID, never
   to drop the flag.
6. **Test step, chained with `&&`.** `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build && dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`.
   The chaining is copied from `unit` at `:145-147` for the reason its own comment gives at
   `:143-144`: **a `pwsh` step reports only the last command's exit code**, so two separate
   lines would let a failing first project pass the step. This also closes the coverage gap
   [[FND-038]] shipped with — its tests are unenforced by CI until this lane exists.
7. **Install the packaging toolchain and answer assumption A3 with evidence.** Add
   `- uses: microsoft/setup-WinAppCli@v0.1` (the action named by
   `.codex/skills/winui-packaging/SKILL.md` § CI/CD with GitHub Actions) and a step running
   `winapp --version`. If the action is unavailable, or the runner cannot restore
   `Microsoft.Windows.SDK.BuildTools`, **stop**: record the failure with the run link as the
   answer to A3 and hand the runner decision to [[TEST-012]] (plan handle `DSK-08-12`, the
   hosted-versus-self-hosted spike) rather than working around it. Plan 02 § 2 is explicit
   that A3 is "to be proven by the CI lane ticket, not assumed green", and a green-looking
   lane that quietly skipped the toolchain would answer nothing.
8. **Prove the existing lanes are unaffected.** After the change, `unit`,
   `sql-integration` ×3, `browser`, `documentation`, `local-development-scripts`,
   `reference-data` and `infrastructure` must all still pass, and the two `ubuntu-latest`
   jobs — `changes` (`:15`) and `sql-integration-coverage` (`:194`) — must be untouched in
   the diff as well as green in the run. Attach the workflow run link to the proof.
9. **Record the measured minutes.** Note the `desktop-build` job duration and the change in
   total workflow minutes per PR run — before and after, from two real runs, not an estimate.
   Add both numbers to this plan: they are the input [[TEST-019]] (plan handle `DSK-08-19`)
   needs to price the private-repository era, and plan 08 § 7 asks for the decision to be
   made "before the repositories flip, not after".
10. **Hold the lane boundary, simplify, open the PR.** Do **not** add `desktop-package`,
    `desktop-ui-smoke` or `packaging-tests` — [[TEST-013]] (plan handle `DSK-08-13`) and
    [[REL-005]] (plan handle `DSK-09-05`) own those. Do **not** package, sign or upload an
    MSIX: the dev-certificate package and its artifact belong to the `desktop-package` lane
    owned by [[REL-005]], and D-002 keeps the production `.pfx` out of GitHub entirely.
    This lane proves build and tests only. Add the `docs/engineering.md` § Branches and
    delivery line (`:10-…`; `grep -n 'desktop-build' docs/engineering.md` returns nothing
    today) and the `docs/runbook.md` § Locked restore, build, and test note (`:298-303`) that
    the desktop projects restore with `-r win-x64` — coordinating with [[TEST-013]] so the
    lane list lives once. Run the simplification pass over this branch's own diff, record it
    under a dated `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**
(`docs/engineering.md:76`). The obligation is a green build of the approved projects and the
enforced dependency direction on a hosted runner. The tier's own words limit the claim —
"This proves consistency only" — so the proof must **not** say the desktop installs, launches
or is packageable: install and launch are [[FND-039]]'s (plan handle `DSK-02-14`) and
[[FND-041]]'s evidence, and packaging is [[REL-005]]'s lane. Proof type: `command-log`, with
the workflow run link.

| Command / observation | Expected evidence |
| --- | --- |
| A pull-request run of `repository-check` | `desktop-build` green, and every previously existing job green — nine jobs before, ten after |
| The same run's `changes` job output | `build=true` for a desktop-only path change, with **no** edit to `scripts/Get-CiChangeFlags.ps1` unless step 2 forced one |
| `pwsh ./scripts/Test-CiChangeFlags.ps1` | exit `0` — run whether or not the pattern changed |
| `pwsh ./scripts/Test-TestShard.ps1` | exit `0`, proving the shard classifier is unaffected |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit `0` — the `documentation` job runs it over the two `docs/` changes |
| The `winapp --version` step | a version string, or a recorded failure with the run link as the A3 answer |
| `git diff --name-only` at PR time | `.github/workflows/ci.yml`, `.github/actions/dotnet-build/action.yml`, `docs/engineering.md`, `docs/runbook.md`, and the two `scripts/*CiChangeFlags*` files only if step 4 forced it; **no** `src/**`, **no** `tests/**`, **no** `*.csproj` |
| Observations stated rather than inferred | which option step 2 chose and why; the `desktop-build` duration; the before/after total workflow minutes; whether A3 was proven or recorded as failing |

## Risks / open questions

- **Risk — the desktop cost lands on six unrelated lanes.** The composite action builds the
  whole `Pegasus.slnx` (`action.yml:24,27`) and `unit`, the three `sql-integration` shards
  and `browser` all use it. Mitigation: step 2 makes the split an explicit, recorded decision
  with measured minutes rather than a side effect discovered in a bill.
- **Risk — `--locked-mode` fails on a RID mismatch.** The desktop lock files are RID/TFM
  specific (plan 02 § 7) and [[FND-038]] generates them with `-r win-x64`. Mitigation: step 5
  restores with the same RID. Dropping `--locked-mode` to make the lane green would defeat
  the property the flag exists for and is never the fix.
- **Risk — a `pwsh` step hides a failing first command.** Mitigation: step 6 chains with
  `&&`, exactly as `unit` does at `:145-147` for the reason recorded at `:143-144`.
- **Risk — assumption A3 is quietly assumed rather than proven.** A lane that installs no
  toolchain still goes green. Mitigation: step 7 adds `winapp --version` as an explicit step,
  and a failure is recorded with its run link and handed to [[TEST-012]] rather than worked
  around.
- **Risk — three plans name an overlapping desktop CI lane.** [[FND-040]] (this ticket),
  [[REL-005]] and [[TEST-013]]. Mitigation and the settled division: this ticket owns
  `desktop-build` only; [[REL-005]] owns `desktop-package` — including the dev-certificate
  package and artifact upload — and [[TEST-013]] extends the lane set. Exactly one
  `desktop-package` job may ever exist and this ticket does not create it. This is a scope
  boundary owned by named sibling tickets, not an open question.
- **Risk — CI checkout timeouts on the ~700 MB repository** (plan 08 § 7). Mitigation: the
  new job uses the same `actions/checkout@v7` shape as its neighbours and adds no depth
  requirement; if it times out, that is the known DELIV-010 trap and not a defect in this
  lane.
- **Scope boundary, not an open question — self-hosted runners.** Plan 08 § 7 names a
  self-hosted Windows runner on the D-003 UNC-share host as the first C-01 mitigation.
  [[TEST-019]] (plan handle `DSK-08-19`) owns that decision; this ticket supplies the
  measured minutes in step 9 and takes no view.
- **Open questions**: none. No `open-questions` document is created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch changes
workflow YAML and documentation, so `n/a — docs-only` does not apply._
