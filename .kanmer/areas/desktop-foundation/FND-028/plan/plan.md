# Plan — FND-028: Add the server solution filter for Linux builds and extend the solution architecture test

**Diff estimate: ~3 files, ~95 lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. This ticket is
profile `chore`: it owes no `research` and no `files` document, so this plan carries the
surface area alone. Every figure below was measured on fork `main` at the branch
`task/desktop-plan-segmentation` working tree on 2026-08-24, with the command shown.

| Path | Measured current state | Change | Lines |
| --- | --- | --- | --- |
| `Pegasus.Server.slnf` (new) | absent — `ls Pegasus.Server.slnf` → *No such file or directory* | JSON: `solution.path` plus the seven server project paths | +13 |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | 520 lines (`wc -l`). `ApplicationSolutionExcludesSourceWorkspaces` at `:128`, its expected seven-path array at `:137-149`. Helpers: `ProjectReferences(root, path)` `:493`, `FindRepositoryRoot()` `:509`, `ForbiddenDirectDependencies` `:480`. Usings at `:1-3` are `System.Reflection`, `System.Text.RegularExpressions`, `System.Xml.Linq` — **`System.Text.Json` is not imported** | two new `[Fact]`s, one `using`, one comment block above the `:137-149` array | +72 |
| `docs/runbook.md` | 1254 lines (`wc -l`). § Supported platform `:19-40`; the sentence "record the platform actually exercised" at `:38`; § Locked restore, build, and test `:298-305`, whose command at `:303` is `dotnet restore ./Pegasus.slnx --locked-mode` | one paragraph naming which entry point each platform builds | +10 |

Total: **3 files, ~95 added lines, 0 deleted**. No file in `src/` is touched.

## Approach

Add a solution **filter** (`Pegasus.Server.slnf`) over the existing `Pegasus.slnx` rather
than a second solution file, and prove the filter's contents with two new architecture
facts instead of relaxing the one that already pins the solution. A filter beats a second
solution because there is then exactly one place a project is registered — `Pegasus.slnx`
— and the filter is a subset assertion over it; a second `Pegasus.Server.slnx` would be a
second registration list that can silently drift from the first, which is the "one list per
concept" failure `AGENTS.md` § Simplicity rails exists to prevent. The rejected alternative
is (b) from the ticket body — documenting an explicit project list for Linux developers in
`docs/runbook.md` — which is rejected because prose cannot be executed and nothing would
fail the day a Windows-target project is added to it.

The filter mechanism is only *conditionally* available, and step 2 settles that before
anything is written; §Risks records exactly what is unknown and what proves it.

**Established from official documentation (fetched 2026-08-24):**

- A `.slnf` is a JSON file carrying `solution.path` plus a `projects` array; the solution
  path is relative to the filter file, the project paths are relative to the *solution*
  file, and backslashes are doubled —
  <https://learn.microsoft.com/visualstudio/msbuild/solution-filters>. Building it is
  identical to building a solution: `msbuild [options] solutionFilterFile.slnf`, and MSBuild
  follows project dependencies automatically.
- The `dotnet` CLI gained `.slnf` support in **.NET SDK 9.0.3xx** —
  <https://learn.microsoft.com/dotnet/core/tools/dotnet-sln#commands>. `global.json` pins
  `10.0.302` with `rollForward: latestFeature`, so the CLI side is satisfied.
- The same MSBuild page carries the note: *"In the case where you're using the `.slnx`
  solution file format, supported in MSBuild 17.12 and later, the `.slnx` file takes
  priority over the `.slnf` file."* This is about implicit selection when both are present
  in a directory; it does **not** state that `solution.path` may point at a `.slnx`. That
  remains the one unproven point, and step 5 is the empirical settle.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-028` reports `docs_todo: true`, so
there is no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client inside the fork; the new top-level
> projects it authorises are what forces a Linux build split), authored by
> [[FND-026]] (plan handle `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`) also claims
> ADR-0100 in the reserved block — see [[FND-026]]'s plan for the ownership reconciliation.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 5; if the ADR lands
> differently this plan is revised before implementation.

Because `refs` is empty, the authorities that actually bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Plan 02 § 3 decision 5 | "add the desktop projects to `Pegasus.slnx` **and** add a solution filter `Pegasus.Server.slnf` … used by Linux builds … The architecture test that pins the solution contents is extended rather than bypassed" | Steps 3–4, 6–8 |
| Plan 02 § 7 (Linux build break) | "the Linux release script must switch to the slnf in the same ticket" | Step 9 — the measured finding is that `scripts/Build-ReleaseArtifacts.ps1` never names a solution, so no change is owed; the finding is recorded, not the edit |
| Proposal § 5.4 (recommended solution structure), § 21.2 (CI stages) | The server projects remain one buildable, Linux-publishable set as the desktop projects arrive | Steps 3, 5 |
| `docs/runbook.md` § Supported platform (`:19-40`) | Linux with PowerShell 7 is a first-class development workstation; `:38` requires recording the platform actually exercised | Steps 6, 10, and the Verification section's honesty clause |
| `AGENTS.md` § Product invariants (`:235`) | A new top-level project needs an accepted ADR proving the boundary cannot carry it | Cited only — this ticket adds **no** project; it registers a view over projects that already exist |
| `docs/engineering.md` § Plan sizing (`:201`) | A plan states its diff estimate first, derived from a measured inventory | The inventory table above |
| L-02 (locked, `docs/desktop/README.md`) | Test/UAT is a local production-mimicking stack; no Azure test environment | Step 10 — the Linux evidence is a local build or an honest "not exercised" note; it never becomes a request for a hosted Linux runner |
| C-01 (constraint, `docs/desktop/README.md`) | The repositories become private; GitHub Actions minutes stop being free | Step 11 — this ticket adds **no** CI job; [[FND-040]] (plan handle `DSK-02-15`) owns the lane split |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan
document specifically.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `directory-build-organization`
  (dotnet/skills `98f84851`, plugin `dotnet-msbuild`) → `binlog-failure-analysis` (same pin)
  if the filtered build fails.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before
  every move; a move crosses at most one gated boundary. `get_doc_gates FND-028` reports the
  owed set as `plan` + `questions-resolved` at `leave-preparing`, and `proof` +
  `questions-resolved` at `enter-done`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's eleven implementation steps: same order, same ownership,
same paths, with the *how* the body leaves out.

1. **Orient.** Read `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 5
   and § 7, then `Pegasus.slnx` (14 lines, four `/src/` and three `/tests/` projects) and
   `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:126-172` in full. Call
   `get_doc_gates FND-028`, then `take_ticket` on branch `task/server-solution-filter`
   created from `origin/dev`.
2. **Settle the mechanism before writing the file.** Run `microsoft_docs_search` for
   `solution filter slnf dotnet CLI` and for `slnx solution file format dotnet 10`. The
   three facts already established are in § Approach above with their URLs and the
   2026-08-24 fetch date — re-fetch to confirm they have not moved, and record the fetch
   date in the proof. The single open point is whether `solution.path` may name a `.slnx`;
   documentation does not say, so step 5 settles it empirically rather than by argument.
3. **Write the filter.** Create `Pegasus.Server.slnf` at the repository root:
   `{"solution":{"path":"Pegasus.slnx","projects":[ … ]}}` with the seven paths in the
   ordinal order the existing test uses, backslash-escaped as the MSBuild page requires
   (`src\\Pegasus.Core\\Pegasus.Core.csproj`, `src\\Pegasus.Infrastructure\\…`,
   `src\\Pegasus.Web\\…`, `src\\Pegasus.Worker\\…`,
   `tests\\Pegasus.ArchitectureTests\\…`, `tests\\Pegasus.Core.Tests\\…`,
   `tests\\Pegasus.IntegrationTests\\…`). These are exactly the seven paths asserted at
   `DependencyDirectionTests.cs:137-149`, so the two lists are provably the same set today.
4. **If step 5 proves `.slnf` over `.slnx` unsupported**, take alternative (a) from the
   ticket body — a second solution file `Pegasus.Server.slnx` holding the same seven
   `<Project Path="…" />` elements in a `/src/` and a `/tests/` folder, mirroring
   `Pegasus.slnx`'s shape — and record the decision, the failing command and its exact
   error in this plan under a dated heading before continuing. Do not invent a third
   mechanism. Alternative (b) (documentation only) stays rejected for the reason in
   § Approach. Whatever file results is "the server entry point" below, and every command
   in § Verification is re-run against its real name.
5. **Prove the file is well-formed.** `dotnet restore ./Pegasus.Server.slnf --locked-mode`
   then `dotnet build ./Pegasus.Server.slnf --configuration Release --no-restore`. Expected:
   exit 0, seven projects. Until [[FND-030]] (plan handle `DSK-02-05`) lands, the filtered
   set is identical to the full solution, so a green result proves the *file* is valid and
   nothing about filtering yet — say exactly that in the proof rather than overclaiming.
   `--locked-mode` works because `packages.lock.json` exists for all seven projects today.
6. **First new fact — the exact contents.** Add
   `ServerSolutionFilterContainsExactlyTheServerProjects` to
   `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`, placed immediately after
   `ApplicationSolutionExcludesSourceWorkspaces` (which ends at `:154`). Load the entry
   point through `FindRepositoryRoot()` (`:509`), parse with `System.Text.Json`
   (`JsonDocument.Parse`, reading `solution.projects`) if the file is `.slnf` or with
   `XDocument` if step 4 forced a `.slnx`, normalise `\\` to `/` exactly as
   `ProjectReferences` does at `:502` so the assertion holds on Linux, `.Order(StringComparer.Ordinal)`,
   and `Assert.Equal` against the same seven-path array. Add `using System.Text.Json;` to
   the file's using block at `:1-3` — it is not there today.
7. **Second new fact — the exclusion.** Add
   `ServerSolutionFilterExcludesWindowsTargetedProjects` asserting
   `Assert.DoesNotContain(projectPaths, p => p.StartsWith("src/Pegasus.Desktop", StringComparison.OrdinalIgnoreCase) || p.StartsWith("tests/Pegasus.Desktop", StringComparison.OrdinalIgnoreCase))`.
   This is the fact that fires the day someone adds a Windows-target project to the Linux
   build, so it must be written against the *normalised forward-slash* paths from step 6,
   not the raw escaped JSON.
8. **Leave the existing fact alone.** `ApplicationSolutionExcludesSourceWorkspaces` keeps
   asserting today's seven-project `Pegasus.slnx` list. Add a comment directly above the
   array at `:137` naming the four tickets that will each extend it as they add their
   project — [[FND-029]] (plan handle `DSK-02-04`, `src/Pegasus.Contracts`), [[FND-030]]
   (`src/Pegasus.Desktop`), [[FND-031]] (plan handle `DSK-02-06`,
   `src/Pegasus.Desktop.Infrastructure`) and [[FND-038]] (plan handle `DSK-02-13`,
   `tests/Pegasus.Desktop.ViewModelTests`) — so the next agent knows the list is
   intentionally exact rather than stale.
9. **Confirm the release route needs no change.** `scripts/Build-ReleaseArtifacts.ps1`
   restores and publishes `src/Pegasus.Web/Pegasus.Web.csproj` (`:45`, `:49`) and
   `src/Pegasus.Worker/Pegasus.Worker.csproj` (`:47`, `:68`) **by path**, and builds the EF
   migration bundle from `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` (`:70`).
   It never names a solution. Record that finding in the proof; do not edit the script.
10. **Runbook.** Add one paragraph to `docs/runbook.md` § Supported platform (`:19-40`),
    after the existing "Linux development is supported by these procedures; record the
    platform actually exercised" sentence at `:38`: Linux builds and tests the server entry
    point, Windows builds the full `Pegasus.slnx`. Do **not** restate the `winapp` CLI or
    Developer Mode prerequisites — [[FND-039]] (plan handle `DSK-02-14`) owns that sentence;
    cite it. Record in the proof which case applied (whether [[FND-039]] had already added
    them). Leave § Locked restore, build, and test (`:298-305`) unchanged: its
    `dotnet restore ./Pegasus.slnx --locked-mode` at `:303` stays correct for Windows and is
    a second list-of-commands concept this ticket does not own.
11. **Verify and close.** `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`
    — expected: every existing fact still green plus the two new ones. Run
    `pwsh ./scripts/Test-DocumentationLinks.ps1` after the runbook edit. Then run the
    simplification pass over this branch's own diff, record it under a dated
    `## Simplification pass` heading in this document, and open the PR into `dev`.

## Verification

Evidence tier **1 — Static/build/architecture** (`docs/engineering.md` § Required evidence
tiers, `:72`), as the ticket body states: this obliges a compiled solution set and an
executable architecture fact, and proves consistency only. No operator capability is
claimed.

The `proof` document is produced from these three command logs:

1. `dotnet build ./Pegasus.Server.slnf --configuration Release` — expected exit 0 with
   seven projects named in the output. Paste the project list, not just the exit code.
2. `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --filter "FullyQualifiedName~ServerSolutionFilter"`
   — expected `2 passed, 0 failed`. Then re-run without the filter to show the whole project
   still green (the existing `ApplicationSolutionExcludesSourceWorkspaces` must be visible
   as passing, unchanged).
3. `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected exit 0 after the runbook edit.

**Platform honesty clause.** CI today runs seven of its nine jobs on `windows-latest`; the
two `ubuntu-latest` jobs are `changes` (path detection, `.github/workflows/ci.yml:15`) and
`sql-integration-coverage` (shard-partition check, `:194`), and **neither builds** — the
composite `.github/actions/dotnet-build/action.yml` that runs the restore and build is not
referenced by either. A green `repository-check` run is therefore **not** evidence that the
Linux path works. The Linux evidence is a local `dotnet build ./Pegasus.Server.slnf` on a
Linux workstation, or an explicit "Linux not exercised; Windows only" line in the proof, as
`docs/runbook.md:38` requires.

## Risks / open questions

- **Risk — `solution.path` may not accept a `.slnx`.** The MSBuild solution-filters page
  documents `.slnf` against `.sln` and notes only that `.slnx` "takes priority over the
  `.slnf` file" where both are present; it does not state that a filter may target a `.slnx`.
  *Mitigation*: step 5 settles it by running the build, and step 4 has the pre-agreed
  fallback (a second `Pegasus.Server.slnx`) with the requirement to record the exact failing
  command. This is answered inside this ticket by the implementing agent at steps 2 and 5 —
  it is not an unresolved question that must be settled before the ticket may start, so no
  `open-questions` document is opened for it.
- **Risk — the two lists drift.** `Pegasus.Server.slnf`'s seven paths and the array at
  `DependencyDirectionTests.cs:137-149` are the same seven strings today, but they are two
  literals. *Mitigation*: the step 6 fact asserts the filter against the same array, so the
  moment [[FND-029]] extends the `Pegasus.slnx` array without adding Contracts to the filter
  (which it is instructed to do at its own step 10), the new fact fails loudly. Do not
  "simplify" step 6 into asserting the filter equals the solution — the whole point is that
  after [[FND-030]] they must differ.
- **Risk — `--locked-mode` on a filtered restore.** Every one of the seven projects has a
  committed `packages.lock.json` today, but only the three test projects set
  `RestorePackagesWithLockFile=true` in their csproj. [[FND-027]] (plan handle `DSK-02-02`)
  is the named dependency and sets it everywhere; if it has not landed, run step 5 without
  `--locked-mode` and record that substitution in the proof rather than adding the property
  here — the property is [[FND-027]]'s to own.
- **Scope boundary, not an open question — the CI lane.** Whether `ci.yml` gains an
  ubuntu job that builds the filter is owned by [[FND-040]] and by [[TEST-013]] (plan handle
  `DSK-08-13`); this ticket's Guardrails forbid touching `.github/workflows/ci.yml`.
- **Scope boundary, not an open question — the runbook prerequisites sentence.** The
  `winapp` CLI and Developer Mode prerequisites in `docs/runbook.md` § Supported platform are
  owned by [[FND-039]].
- **No open question is opened on this ticket.** Nothing here is unsettled in a way that
  must be answered before implementation begins; the one genuinely unknown fact (step 2) is
  assigned to the implementing agent inside the ticket's own first steps, with a recorded
  fallback.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading._
