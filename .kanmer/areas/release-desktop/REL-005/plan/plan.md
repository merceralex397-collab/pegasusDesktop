# Plan — REL-005: DSK-09-05 · CI desktop lanes: build, dev-cert package, packaging tests, artifact upload

**Diff estimate: ~1 file, ~45 lines** in the preferred branch (one `desktop-package` job
added to `.github/workflows/ci.yml`, sized against the `unit` job at `:131-148` which is
18 lines, plus a six-line leading comment and three extra steps). **~3 files, ~60 lines**
in the fallback branch step 3 may select, which adds one alternation to
`scripts/Get-CiChangeFlags.ps1:11` and two cases to `scripts/Test-CiChangeFlags.ps1`.
`docs/engineering.md:201-207` § Plan sizing requires the estimate first; the range is
honest because the branch is decided by reading the tree, not by guessing.

## Approach

**Add one job to the existing workflow, gate it on a flag someone else owns, and check
before adding.** Three tickets name an overlapping desktop lane, and the body settles the
split on evidence rather than convention: `DSK-02-15` (board `FND-040`) owns
`desktop-build` and packages nothing; this ticket owns `desktop-package`; `DSK-08-13`
(board `TEST-013`) owns the `desktop` change flag, the `changes` job output that carries
it, and the later `desktop-ui-smoke` and `packaging-tests` extensions of this same job.
The alternative rejected was **a separate `desktop.yml` workflow**: it would need its own
`changes`-equivalent path detection, would duplicate the composite build action's cache
key, and — under constraint C-01's 2× private-repository Windows multiplier — would bill a
second Windows job for work the existing `changes` job already classifies. A second
alternative, **adding a `desktop` flag here**, is rejected because
`scripts/Test-CiChangeFlags.ps1:9-21`'s `Assert-Flags` helper takes exactly `-Build` and
`-Infrastructure`, so a third output is a multi-file change to a file another ticket owns.

The lane packages with a **generated development certificate** and never signs for
production. That is not a convenience: D-002 confines the production `.pfx` to the in-house
signing host and forbids it as a GitHub secret, so a hosted runner cannot sign at all.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-005`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). This lane is the
> pre-merge guard on the packaging half of that decision: it proves the MSIX still builds
> and that `eng/packaging/Test-TestAppInstaller.ps1`'s eight validator checks still fire.
> This plan is written to the decisions as recorded in
> `docs/desktop/09-release-update-and-distribution/README.md` § 2 and § 7; if ADR-0105
> lands differently, this plan is revised before implementation.

Binding operator decisions and constraints, written to as settled:

- **D-002** (2026-08-23) — the production certificate is self-managed and confined to the
  signing host; **it never reaches a CI runner**, and PR builds use a generated dev
  certificate. `grep -n "secrets\." .github/workflows/ci.yml` must show no match
  introduced by this diff.
- **C-01** — private-repository Windows runners bill at a 2× multiplier and this
  repository already runs seven of nine jobs on `windows-latest`, so the lane is
  change-gated and time-boxed, and a duplicate packaging lane bills twice. The cost plan
  is `DSK-08-19` (board `TEST-019`), which needs this lane's measured duration.
- **D-003** — the feed is an in-house UNC share, and nothing in CI publishes to it. The
  artifact is uploaded to the run and goes no further.

Ownership contracts this plan **honours** rather than defines: the `desktop` change flag
and the `changes` job output (`DSK-08-13`, board `TEST-013`); the `desktop-build` lane
(`DSK-02-15`, board `FND-040`); the artifact name `desktop-msix-unsigned`, which
`DSK-08-13`'s `desktop-ui-smoke` downloads by that exact literal.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `authoring-github-workflows` (`dotnet/skills` `98f84851`,
  `.agents/skills/authoring-github-workflows/SKILL.md` once `DSK-12-02`, board `TOOL-002`,
  vendors it — it is **not** in `.agents/skills/` today, which holds only
  `pegasus-release` and `project/`) → `winui-packaging`
  (`.codex/skills/winui-packaging/SKILL.md`, verified present) → `binlog-failure-analysis`
  (`dotnet/skills` `98f84851`, plugin `dotnet-msbuild`) when the MSIX or XAML build fails
  in CI.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates REL-005` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's thirteen implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient and take.** Read the area plan § 5 row `DSK-09-05` and § 2 and § 7, then
   `.github/workflows/ci.yml` in full (234 lines) and
   `.github/actions/dotnet-build/action.yml`. `get_doc_gates REL-005`, then
   `take_ticket REL-005`.
2. **Load the skills** and read `winui-packaging` § CI/CD with GitHub Actions in full:
   `microsoft/setup-WinAppCli@v0.1`, `winapp cert generate --if-exists skip --quiet`,
   `winapp package … --quiet`. Note that `authoring-github-workflows` may not be vendored
   yet and record that if so.
3. **Decide the change-flag branch by looking, not asking.** Run
   `grep -n "desktop" scripts/Get-CiChangeFlags.ps1` and
   `grep -n "outputs.desktop\|desktop:" .github/workflows/ci.yml`.
   - **Preferred branch — the `desktop` flag has landed**: gate the job on
     `needs.changes.outputs.desktop == 'true'` and change **no** pattern and **no**
     classifier file.
   - **Fallback branch — it has not landed**: extend `$buildPattern` at
     `scripts/Get-CiChangeFlags.ps1:11` with `|^eng/packaging/`, add one positive case
     (`eng/packaging/Test-AppInstaller.ps1` → `Build $true`, `Infrastructure $false`) and
     one negative case to `scripts/Test-CiChangeFlags.ps1` using the existing
     `Assert-Flags` helper (no signature change needed — only a third *output* would
     require one), and run `pwsh ./scripts/Test-CiChangeFlags.ps1`, expected exit `0`.
   Record which branch applied in this document under a dated note. Either way, add **no**
   second pattern for paths `DSK-08-13`'s `desktop` flag classifies.
4. **Check whether the job already exists.** `grep -n '^  desktop-package:' .github/workflows/ci.yml`.
   If `DSK-08-13` has already added it, **extend it in place** and add no second job. If
   not, add it after `unit` (which ends at `:148`), with `needs: changes`, the `if:` clause
   step 3 selected, `runs-on: windows-latest`, `timeout-minutes: 30`, and a leading comment
   saying four things: why the lane exists, that it never signs for production, that it is
   the only packaging lane in the workflow, and which change flag gates it. If
   `DSK-02-15`'s `desktop-build` lane has landed, add it to `needs:` and take the build
   from it — never re-add a second build-and-test lane. Record which case applied.
5. **Steps 1–2 of the job**: `actions/checkout@v7`, then `uses: ./.github/actions/dotnet-build`.
   Do **not** re-pin the SDK inline: the composite action exists to keep the NuGet cache
   key from drifting between lanes, and a second pin is exactly that drift.
6. **Step 3 of the job — desktop tests before packaging**, chained with `&&` on one
   `run: >` line because pwsh reports only the last command's exit code
   (`ci.yml:145-147` records the reason):
   `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build && dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`.
   If `DSK-02-15`'s `desktop-build` lane already proved those two projects for the same
   commit, take the result through `needs:` and **drop this step** rather than paying for
   the same Windows minutes twice (C-01). Note that
   `tests/Pegasus.Desktop.ViewModelTests` does not exist yet — `ls tests/` shows only
   `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.
7. **Step 4 of the job — package with a development certificate.**
   `uses: microsoft/setup-WinAppCli@v0.1`, then
   `winapp cert generate --manifest ./src/Pegasus.Desktop --if-exists skip --quiet`
   followed by
   `winapp package ./src/Pegasus.Desktop/bin/x64/Release/<tfm>/ --cert ./devcert.pfx --self-contained --quiet`.
   `--manifest` makes the generated subject match `Identity.Publisher`, which is the fix
   for the `0x8007000B` Publisher-mismatch failure; `--if-exists skip` keeps a re-run from
   failing on an existing certificate. **Never** run `winapp cert install` here: the skill
   records that it writes to the machine Trusted Root store and persists across reboots,
   which is a grant a shared runner must not receive — and packaging does not need it,
   only installing does. Resolve `<tfm>` with a glob and fail with a named message if it
   does not match exactly one directory, rather than hard-coding a framework moniker that
   the next Windows App SDK bump invalidates.
8. **Step 5 of the job — run the two suites**:
   `pwsh ./eng/packaging/Test-TestAppInstaller.ps1` (validator regression, `DSK-09-03`,
   board `REL-003`) and `pwsh ./eng/packaging/Test-Package.ps1` (packaging scenarios,
   `DSK-08-10`, board `TEST-010`). If `Test-Package.ps1` needs an installed package and
   the hosted runner cannot install one, run only the scenarios that do not require
   installation and **write the skipped list into the job summary**
   (`>> $env:GITHUB_STEP_SUMMARY`) — do not silently drop them.
9. **Step 6 of the job — upload**: `uses: actions/upload-artifact@v6` (matching the
   version already at `ci.yml:179`) with `name: desktop-msix-unsigned`,
   `path: '**/*.msix'` and `if-no-files-found: error`. That artifact name is a
   cross-ticket literal — `DSK-08-13`'s `desktop-ui-smoke` downloads it by exactly this
   name — so never rename it and never publish a second MSIX artifact under another name.
   Leave the `path` glob broad enough that `DSK-09-16` (board `REL-014`) can add the SBOM
   to the same artifact.
10. **Name and comment the artifact step** so a reader cannot mistake it: the MSIX is
    **dev-signed, not production-signed**, and must never be published to a feed.
    Production signing happens only in the tag-triggered lane, `DSK-09-17` (board
    `REL-015`).
11. **Prove nothing else changed.** `git diff .github/workflows/ci.yml` must show only
    additions plus the `changes`-output reuse; `documentation`, `unit`, `sql-integration*`
    and `browser` must be untouched. Confirm the trigger block at `:3-6` is unchanged — no
    tag trigger was added.
12. **Open the PR and read the run.** `desktop-package` green, all nine pre-existing jobs
    green, artifact attached. Record the run URL in the ticket proof, and record the
    observed job duration in minutes for `DSK-08-19` (board `TEST-019`)'s C-01 cost
    picture.
13. **Simplification pass.** Record it under a dated `## Simplification pass` heading in
    this document (`AGENTS.md` § Repository task workflow step 4). This branch changes a
    workflow, so `n/a — docs-only` does not apply.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture.** The obligation is a
green CI lane and an attached artifact; it does **not** prove installation, update or
signature-chain behaviour on a workstation, and the proof must say so. `proof` is the run
URL plus the captured output of the local commands, as proof types `command-log` and
`test-output`.

| Command / observation | Expected evidence |
| --- | --- |
| `pwsh ./scripts/Test-CiChangeFlags.ps1` | exit `0` (run in both branches — in the preferred branch it proves nothing regressed) |
| `pwsh ./scripts/Get-CiChangeFlags.ps1 -ChangedPath eng/packaging/Test-AppInstaller.ps1` | the flag step 3 selected is `True` — `Desktop` when `DSK-08-13` has landed, otherwise `Build` |
| `grep -c '^  desktop-package:' .github/workflows/ci.yml` | `1` — one packaging lane in the whole workflow |
| `grep -n "secrets\." .github/workflows/ci.yml` | no match introduced by this diff |
| GitHub Actions run on the PR | `desktop-package` succeeds; artifact `desktop-msix-unsigned` present; the nine pre-existing jobs report the same status as on `dev` |

Behaviours to observe rather than infer, and to state in the proof: which branch step 3
selected and why; whether step 4 found an existing `desktop-package` job; whether step 6's
test step was dropped in favour of a `needs: desktop-build` result; which
`Test-Package.ps1` scenarios were skipped and why; and the lane's measured duration.

## Risks / open questions

- **Risk — two `desktop-package` jobs, or one lane gated on two different flags.** Both are
  stop conditions. Mitigation: steps 3 and 4 are mandatory state checks whose outcome is
  recorded in this document, and the third verification command is the guard.
- **Risk — a lane gated on a flag the classifier never emits.** It would never run, and
  nobody would notice because a skipped job looks like a passing one. Mitigation: the
  second verification command asserts the classifier actually emits the flag the `if:`
  reads.
- **Risk — a second SDK pin drifts the NuGet cache key.** Mitigation: step 5 uses the
  composite action and forbids an inline pin.
- **Risk — `winapp cert install` on a shared runner.** It writes to machine Trusted Root
  and persists across reboots. Mitigation: step 7 forbids it and explains that packaging
  does not need it.
- **Risk — `microsoft/setup-WinAppCli@v0.1` does not work on the hosted runner**
  (assumption A-09-9). Mitigation: the recorded fallback is
  `winget install Microsoft.WinAppCLI`, the same route
  `docs/desktop/08-testing/test-uat-stack.md` § Machine prerequisites names for the
  Test/UAT machine. Record which was used.
- **Risk — `Test-Package.ps1` needs an installed package** (assumption A-09-10).
  Mitigation: step 8's rule — run the non-installing scenarios and write the skipped list
  into the job summary.
- **Risk — a hard-coded target framework moniker.** Mitigation: step 7 resolves `<tfm>`
  with a glob and fails loudly on ambiguity.
- **Risk — C-01 cost.** Mitigation: the lane is change-gated, `timeout-minutes: 30`, and
  drops its duplicate test step when `desktop-build` has landed; step 12 hands the measured
  minutes to `DSK-08-19` (board `TEST-019`).
- **Open questions**: none that block. Every branch is decided by `grep` on the branch at
  implementation time, and each assumption has a recorded fallback. No `open-questions`
  document is created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch changes
a workflow, so `n/a — docs-only` does not apply._
