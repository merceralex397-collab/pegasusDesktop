# Plan — FND-027: central package management and lock files for every project

**Diff estimate: ~17 files, ~100 hand-authored lines** — plus up to 6 regenerated
`packages.lock.json` files (6,871 lines across the seven today) whose content is
machine-generated and reviewed as output, not as authorship.

`docs/engineering.md` § Plan sizing requires the estimate first. This is a `chore`, so
it owes no `research` and no `files` document and this plan carries the surface area
alone. Every number below is measured, not asserted.

### Measured surface-area inventory

Measured in `C:\Users\PC\Documents\GitHub\pegasusDesktop` at `bbd1c549`, 2026-08-24.

- **`Directory.Packages.props` does not exist** (`ls Directory.Packages.props` → no
  such file). **`nuget.config` does not exist** (`ls nuget.config` → no such file).
- **Seven project files, six with `PackageReference` items** (`ls src/*/*.csproj
  tests/*/*.csproj`): `src/Pegasus.Core`, `src/Pegasus.Infrastructure`,
  `src/Pegasus.Web`, `src/Pegasus.Worker`, `tests/Pegasus.ArchitectureTests`,
  `tests/Pegasus.Core.Tests`, `tests/Pegasus.IntegrationTests`.
- **50 `PackageReference` items carrying a `Version` attribute**, from
  `grep -rn 'PackageReference' src/*/*.csproj tests/*/*.csproj`:

  | Project | `PackageReference` items | Line range |
  | --- | --- | --- |
  | `src/Pegasus.Core/Pegasus.Core.csproj` | **0** | — (14-line file, zero packages) |
  | `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` | 18 | `:8-28` |
  | `src/Pegasus.Web/Pegasus.Web.csproj` | 7 | `:36-45` |
  | `src/Pegasus.Worker/Pegasus.Worker.csproj` | 9 | `:15-23` |
  | `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` | 4 | `:12-15` |
  | `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` | 4 | `:12-15` |
  | `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` | 8 | `:12-19` |

- **36 distinct package ids** across those 50 items. The duplicates are
  `Azure.Identity` (3 projects), `Azure.Storage.Blobs` (2), `DocumentFormat.OpenXml`
  (2), `Microsoft.EntityFrameworkCore.Design` (2), `Microsoft.Playwright` (2),
  `coverlet.collector` (3), `Microsoft.NET.Test.Sdk` (3), `xunit` (3) and
  `xunit.runner.visualstudio` (3).
- **`RestorePackagesWithLockFile` is set in exactly three places**, all test projects
  and all at line 8: `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj:8`,
  `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj:8`,
  `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:8`
  (`grep -rn 'RestorePackagesWithLockFile' src tests Directory.Build.props`). It is
  **not** in `Directory.Build.props`, whose `PropertyGroup` spans `:2-18`.
- **Seven lock files are committed anyway** (`git ls-files | grep packages.lock.json`),
  totalling **6,878 lines**: Core 7, Infrastructure 1,014, Web 1,198, Worker 1,253,
  ArchitectureTests 1,706, Core.Tests 106, IntegrationTests 1,594. Core's 7-line file
  is consistent with its zero packages and is unlikely to change.
- **`.github/actions/dotnet-build/action.yml`** — the `cache-dependency-path` list is
  at `:18-21` and holds `global.json`, `src/**/packages.lock.json`,
  `tests/**/packages.lock.json`; the restore step at `:22-24` runs
  `dotnet restore ./Pegasus.slnx --locked-mode` and the build at `:25-27` runs
  `dotnet build ./Pegasus.slnx --configuration Release --no-restore`.
- **`docs/runbook.md` § Locked restore, build, and test** is at `:298`, with the
  canonical commands at `:302-306`.
- **`Directory.Build.props:8`** sets `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  and `:7` `<AnalysisLevel>latest-recommended</AnalysisLevel>`; `:17` is
  `<PlaywrightVersion>1.61.0</PlaywrightVersion>`.

**Hand-authored diff:** `Directory.Packages.props` new (~40 lines: `Project`,
`PropertyGroup` with `ManagePackageVersionsCentrally`, and 36 `PackageVersion` items);
`Directory.Build.props` +1 line; six `.csproj` files with 50 `Version="…"` attributes
stripped and 3 `RestorePackagesWithLockFile` lines deleted; `action.yml` +1 line;
`docs/runbook.md` +2 lines. ≈100 lines across 10 files, plus up to 6 regenerated lock
files.

### Measured: the one thing the ticket body does not anticipate

**`Azure.Storage.Blobs` is pinned at two different versions today.**

- `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:9` →
  `<PackageReference Include="Azure.Storage.Blobs" Version="12.26.0" />`
- `src/Pegasus.Worker/Pegasus.Worker.csproj:21` →
  `<PackageReference Include="Azure.Storage.Blobs" Version="12.29.1" />`

Step 3 as written — "one `<PackageVersion Include="…" Version="…" />` per distinct
package, taking each version from the csproj it is in today" — cannot be satisfied for
this id, because central package management permits exactly one `PackageVersion` per
package. **Default taken, and it is forced by the ticket's own acceptance criteria:**
unify on **12.29.1**, the higher of the two. The alternative — leaving
`VersionOverride="12.26.0"` on the Infrastructure reference — is rejected because the
acceptance criterion `grep -rn 'PackageReference[^>]*Version=' src tests` must return
**no matches**, and `VersionOverride=` matches that pattern. The unification is proved,
not assumed, by the ticket's own verification: `--locked-mode` restore, a Release build
with `TreatWarningsAsErrors=true`, and both unit lanes green. See Risks.

Every other duplicated id already agrees across projects: `Azure.Identity` 1.21.0 in
all three, `DocumentFormat.OpenXml` 3.5.1 in both,
`Microsoft.EntityFrameworkCore.Design` 10.0.10 in both, `coverlet.collector` 6.0.4,
`Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3 and `xunit.runner.visualstudio` 3.1.4
in all three test projects.

## Approach

Convert in place with the `convert-to-cpm` skill's procedure: inventory the version
literals first, create `Directory.Packages.props` with one `PackageVersion` per distinct
id, strip only the `Version` attribute from each `PackageReference` (keeping
`PrivateAssets`/`IncludeAssets` metadata), lift `RestorePackagesWithLockFile` into
`Directory.Build.props` so it applies to every project including the desktop projects
added later, regenerate the locks with `--force-evaluate`, then prove with the canonical
`--locked-mode` restore and Release build. The rejected alternative is to introduce
`Directory.Packages.props` only for the *new* desktop packages and leave the seven
existing projects carrying literals: it is rejected because `NU1008` makes a mixed
state a build failure under `TreatWarningsAsErrors=true`, and because it leaves the
`Microsoft.Playwright` literal at
`tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:17` free to
desynchronise from `Directory.Build.props:17` and from
`src/Pegasus.Web/Pegasus.Web.csproj:28`'s `ContainerBaseImage` — the ADR-0028 /
DELIV-012 trap this repository already wrote a comment about.

## Governing docs

The ticket's `refs` is **empty** and `docs_todo: true` — confirmed by
`get_doc_gates FND-027`. Profile `chore` has no `leave-backlog` boundary on this board,
so `docs_todo` satisfies no gate here; it states honestly that no existing
`docs/(prd|frd|adr)` document is implemented by this work.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client in the fork), which authorises
> the new top-level projects whose Windows App SDK version this ticket makes it possible
> to pin centrally. ADR-0100 is **authored by [[FND-026]] (plan handle `DSK-02-01`);
> [[FND-005]] (plan handle `DSK-00-05`) is its co-claimant — see [[FND-026]]'s plan for
> the ownership reconciliation.** This plan is written to plan-02 decision 4 as recorded
> in `docs/desktop/02-architecture-and-foundation/README.md` § 3 ("introduce
> `Directory.Packages.props` and set `RestorePackagesWithLockFile=true` for every
> project (today only tests). Major Windows App SDK / toolkit upgrades are reviewed PRs,
> never automatic"); if ADR-0100 lands differently this plan is revised before
> implementation. **No ADR is authored, edited or claimed by this ticket.**

Because `refs` is empty, the programme-level authorities that bind today are listed
with the step that satisfies each. `kanmer-review` checks this table against the diff.

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 21.1 Build properties | Target framework, Windows SDK version, Windows App SDK version, package versions, RID and signing references centralised, with lock files and reviewed dependency updates | Steps 3, 5, 6 |
| Plan 02 § 3 decision 4 | `Directory.Packages.props` plus `RestorePackagesWithLockFile=true` for **every** project; major Windows App SDK / toolkit upgrades are reviewed PRs, never automatic | Steps 3, 5, 10 |
| Plan 02 § 7 (Risks and traps) | Lock files with Windows-only packages are RID/TFM specific, so CI must restore with the same RID | Step 10's note for [[FND-040]] (plan handle `DSK-02-15`) |
| ADR-0028 / DELIV-012 | The `playwright/dotnet` base image tag must match the pinned `Microsoft.Playwright` version exactly, so `$(PlaywrightVersion)` stays the single source | Step 3's `$(PlaywrightVersion)` rule; step 4 removes the duplicate literal |
| C-01 (2026-08-23) | The repositories become private and Windows runners bill at 2×; a restore that resolves floating versions costs real money | The whole ticket, and step 9's cache key |
| `Directory.Build.props:7-8` (repository policy) | `AnalysisLevel=latest-recommended` and `TreatWarningsAsErrors=true` — fix the cause of a NuGet warning, never relax the policy | Step 7 |
| `docs/runbook.md:298-306` § Locked restore, build, and test | The canonical solution commands, `--locked-mode` enforcing the committed locks | Steps 7, and the documentation change |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | `CoreProjectHasNoForbiddenDirectDependencies` parses `src/Pegasus.Core/Pegasus.Core.csproj`; `ApplicationSolutionExcludesSourceWorkspaces` pins the solution contents | Step 8; the `workspaces/` guardrail |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff, recorded under a dated heading in this plan | Step 11 and § Simplification pass |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing → reviewer |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory
in the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `convert-to-cpm` (`dotnet/skills`
  `98f84851`, plugin `dotnet-nuget`) → `directory-build-organization` (`dotnet/skills`
  `98f84851`, plugin `dotnet-msbuild`) → `binlog-failure-analysis` (same pin) **when a
  restore fails**.
- **MCP**: Kanmer — `get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`; Microsoft Learn — `microsoft_docs_search` for
  `ManagePackageVersionsCentrally`, `NU1008`, `NU1507`, `--force-evaluate`.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gated boundaries confirmed by
  `get_doc_gates FND-027`: `leave-preparing` (`plan`, `questions-resolved`) and
  `enter-done` (`proof`, `questions-resolved`). Call `get_doc_gates FND-027` before
  every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's eleven implementation steps: same order, same ownership,
same file paths, with the measured current values a step must be checked against.

1. **Orient, then take.** Read the plan row and
   `docs/desktop/02-architecture-and-foundation/README.md` § 2 (Facts), § 3 decision 4
   and § 7. Then `get_doc_gates FND-027` and `take_ticket` on branch
   `task/central-package-management` in worktree
   `../pegasus-worktrees/central-package-management`, cut from `origin/dev`.
2. **Load `convert-to-cpm` and inventory first.**
   `grep -rn 'PackageReference' src/*/*.csproj tests/*/*.csproj` — **expect 50 items
   across six projects**, with `src/Pegasus.Core/Pegasus.Core.csproj` contributing zero.
   Confirm the shape with `ls src tests` (seven project folders) and
   `ls nuget.config 2>/dev/null` (**no such file** today). Compare the result against
   the inventory table above; a different count means the tree moved and the plan is
   re-derived, not adjusted.
3. **Create `Directory.Packages.props` at the repository root** with
   `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` and one
   `<PackageVersion Include="…" Version="…" />` per distinct id — **36 items** — each
   version taken from the csproj it is in today. Two items need care:
   - `Microsoft.Playwright` **must** be written
     `<PackageVersion Include="Microsoft.Playwright" Version="$(PlaywrightVersion)" />`
     so `Directory.Build.props:17` stays the single source shared with
     `src/Pegasus.Web/Pegasus.Web.csproj:28`'s `ContainerBaseImage`
     (`mcr.microsoft.com/playwright/dotnet:v$(PlaywrightVersion)-noble`).
   - `Azure.Storage.Blobs` has **two** current versions — `12.26.0`
     (`src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:9`) and `12.29.1`
     (`src/Pegasus.Worker/Pegasus.Worker.csproj:21`) — and CPM permits one. Write
     `Version="12.29.1"`, the higher of the two, and record the unification in the PR
     description and in the simplification-pass note. Do **not** use `VersionOverride`:
     it would still match the acceptance grep in step 4.
4. **Strip only the `Version="…"` attribute** from every `PackageReference` in the six
   projects. Keep `PrivateAssets`/`IncludeAssets` metadata exactly as it is — it exists
   on `Microsoft.EntityFrameworkCore.Design` in two places,
   `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:14-17` and
   `src/Pegasus.Web/Pegasus.Web.csproj:42-45`, both with `<PrivateAssets>all</PrivateAssets>`
   and `<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>`.
   Done looks like: `grep -rn 'PackageReference[^>]*Version=' src tests` returns
   **nothing**.
5. **Lift `RestorePackagesWithLockFile` into `Directory.Build.props`.** Add
   `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` to the
   `PropertyGroup` at `Directory.Build.props:2-18`, then delete the three per-project
   copies — `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj:8`,
   `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj:8`,
   `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:8`. All seven
   projects — and every desktop project added later by [[FND-030]] (plan handle
   `DSK-02-05`) — then restore locked. Done looks like:
   `grep -rn 'RestorePackagesWithLockFile' src tests Directory.Build.props` returns
   exactly one hit, in `Directory.Build.props`.
6. **Regenerate the locks.** `dotnet restore ./Pegasus.slnx --force-evaluate`.
   Expected: the `packages.lock.json` files are rewritten (six of the seven; Core's
   7-line file has no packages and may be unchanged) and `git status` shows **only**
   lock files, the six csproj files, `Directory.Build.props` and the new
   `Directory.Packages.props` as modified or added.
7. **Run the canonical locked commands** from `docs/runbook.md:302-306`:
   `dotnet restore ./Pegasus.slnx --locked-mode`, then
   `dotnet build ./Pegasus.slnx --configuration Release --no-restore`. Expected: both
   exit 0 with **no `NU1008`** ("Projects that use central package version management
   should not define the version on the PackageReference items") and **no `NU1507`**
   (multiple package sources) — `TreatWarningsAsErrors=true` at `Directory.Build.props:8`
   turns either into a build failure. If `NU1507` appears, add a `nuget.config` with a
   single `nuget.org` source and record that addition here rather than suppressing the
   warning. Use `binlog-failure-analysis` if a restore fails.
8. **Run the two unit lanes locally.**
   `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build`
   and
   `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`.
   Expected: both green. In particular
   `DependencyDirectionTests.CoreProjectHasNoForbiddenDirectDependencies` still passes —
   it parses `src/Pegasus.Core/Pegasus.Core.csproj`, which has **zero**
   `PackageReference` items (14-line file) and is indifferent to where versions live.
   `ApplicationSolutionExcludesSourceWorkspaces` must also stay green, which is why
   `workspaces/` is not brought into central package management.
9. **Extend the CI cache key.** `.github/actions/dotnet-build/action.yml:18-21` keys the
   NuGet cache on `global.json`, `src/**/packages.lock.json` and
   `tests/**/packages.lock.json`. Add `Directory.Packages.props` to that
   `cache-dependency-path` list so a version bump invalidates the cache, and confirm the
   restore step at `:22-24` (`dotnet restore ./Pegasus.slnx --locked-mode`) is otherwise
   unchanged. Do not touch any job definition in `.github/workflows/ci.yml`.
10. **Add no desktop package here.** `Microsoft.WindowsAppSDK` and every other desktop
    package is added to `Directory.Packages.props` by [[FND-030]] (plan handle
    `DSK-02-05`) when it scaffolds the project. Record two notes here: that major
    Windows App SDK and UI-toolkit upgrades are reviewed PRs only, never automatic
    (plan 02 § 3 decision 4); and that the desktop lock files will be RID/TFM specific,
    so CI must restore with the same RID — a note for [[FND-040]] (plan handle
    `DSK-02-15`).
11. **Simplification pass, then PR.** Run the pass over this branch's own diff (four
    lenses), record the findings and dispositions under a dated `## Simplification pass`
    heading below — including the `Azure.Storage.Blobs` unification and the removal of
    the duplicated `Microsoft.Playwright` literal, both of which are *reductions* the
    pass should name — and open the PR into `dev`.

## Verification

`proof` is produced from the outputs below. Evidence tier: **Tier 1 —
Static/build/architecture** (`docs/engineering.md` § Required evidence tiers). This
obliges a green locked restore and Release build of the four production projects plus
the three test projects, and nothing more; it changes no runtime behaviour.

- `dotnet restore ./Pegasus.slnx --locked-mode` — expected: exit 0, no `NU1004`,
  `NU1008` or `NU1507`.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — expected: exit 0,
  **zero warnings** (warnings are errors, `Directory.Build.props:8`).
- `grep -rn 'PackageReference[^>]*Version=' src tests` — expected: **no matches**
  (baseline today: 50 matches).
- `grep -rn 'RestorePackagesWithLockFile' src tests Directory.Build.props` — expected:
  exactly **one** match, in `Directory.Build.props` (baseline today: three matches, all
  in test csproj files at line 8).
- `grep -n 'Microsoft.Playwright' Directory.Packages.props tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj`
  — expected: the `PackageVersion` resolves from `$(PlaywrightVersion)` and the
  `1.61.0` literal at `Pegasus.IntegrationTests.csproj:17` is gone.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`
  — expected: all facts pass.
- `git status --porcelain` after step 6 — expected: only `Directory.Packages.props`
  (added), `Directory.Build.props`, the six csproj files and the regenerated
  `packages.lock.json` files.
- `git diff --stat .github/actions/dotnet-build/action.yml` — expected: one line added
  to `cache-dependency-path`; no job definition changed.

## Risks / open questions

- **Risk, and the one the ticket body does not anticipate — `Azure.Storage.Blobs` has
  two versions.** Measured: `12.26.0` at
  `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:9` and `12.29.1` at
  `src/Pegasus.Worker/Pegasus.Worker.csproj:21`. Central package management permits one
  `PackageVersion` per id, so step 3 as literally written cannot be executed for this
  id. **Default taken: unify on 12.29.1**, because `VersionOverride="12.26.0"` would
  still match the acceptance grep `PackageReference[^>]*Version=` and fail the ticket's
  own criterion. *Mitigation and proof:* the unification changes
  `Pegasus.Infrastructure`'s package graph, so it is proved by the ticket's own
  verification — `--locked-mode` restore, a Release build under
  `TreatWarningsAsErrors=true` (which turns any new obsolete/analyzer warning into a
  failure), and both unit lanes green. If the Release build fails on the bump, that is
  a real finding: record it, do **not** relax `TreatWarningsAsErrors`, and raise the
  bump as its own ticket rather than smuggling a `.cs` change into this one — the
  guardrail forbids touching any `.cs` file.
- **Risk — `NU1507` from having no `nuget.config`.** Measured: none exists.
  *Mitigation:* step 7 adds one with a single `nuget.org` source **only if** the warning
  appears, and records the addition here. Never suppress the warning.
- **Risk — a mixed CPM state fails the build.** `NU1008` under
  `TreatWarningsAsErrors=true` makes a half-converted tree a build failure.
  *Mitigation:* steps 3 and 4 are one commit; the step 4 grep is the completeness check.
- **Risk — the regenerated lock files hide a real graph change.** They are 6,878 lines
  today and dominate the diff. *Mitigation:* the reviewer reads the lock diff for
  package-id and version changes only, and every intentional version change is named in
  the PR description — today that is exactly one, `Azure.Storage.Blobs`.
- **Risk — the desktop lock files will be RID/TFM specific.** Plan 02 § 7 records it.
  *Mitigation:* step 10 leaves the note for [[FND-040]] (plan handle `DSK-02-15`), whose
  `desktop-build` lane must restore with the same RID.
- **Risk — `workspaces/` gets pulled in.** It is deliberately outside `Pegasus.slnx`
  and `DependencyDirectionTests.ApplicationSolutionExcludesSourceWorkspaces` asserts it.
  *Mitigation:* the guardrail, and step 8's green architecture run.
- **Open question — does the `Azure.Storage.Blobs` unification need a separate
  decision?** **Answered by:** the implementer at step 3, using the evidence the ticket
  already demands (a green locked restore, a zero-warning Release build and green unit
  lanes). Recorded here rather than as an unticked `open-questions` box because the
  ticket's own acceptance criteria force the answer and its own verification proves it —
  a trivial default taken, not a question asked. If the build fails on 12.29.1, the
  finding is raised then, with evidence, rather than guessed at now.
- **Scope boundary, not a question — desktop packages.** `Microsoft.WindowsAppSDK` and
  every UI-toolkit package belong to [[FND-030]] (plan handle `DSK-02-05`). This ticket
  adds none.
- **Scope boundary, not a question — the pinned SDK.** `global.json` pins
  `10.0.302` with `rollForward: latestFeature`; the guardrail forbids changing it here.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. Unlike the
docs-only tickets in this area, this branch carries a real code-adjacent diff, so
`n/a — docs-only` is **not** available: run the four lenses and record the findings.
Two reductions are already known and should be named in the record — the
`Azure.Storage.Blobs` version unification, and the removal of the duplicated
`Microsoft.Playwright` literal at
`tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:17`._
