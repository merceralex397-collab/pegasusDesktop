# Research — FND-037: executable desktop boundary facts and the no-WebView rule

## Question

What must be added to `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` so that a
forbidden desktop dependency, a wrong desktop project-reference direction, a dependency on
`Pegasus.Contracts`, or a `WebView2` element in desktop XAML fails the build — and by what
mechanism, given that the test project cannot reference the desktop projects at all?

## Current behaviour

No parity-matrix row covers this, **and none should**. The matrix
(`docs/desktop/01-inventory-and-parity/parity-matrix.md`) holds `PAR-01`…`PAR-46` —
`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` returns `46` —
and every row is keyed to a Razor page model under `src/Pegasus.Web/Pages/**`. This ticket
delivers no operator-visible surface, so it has no row to reach parity with.

The closest existing repository mechanism is the file this ticket extends,
`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` (520 lines, measured
2026-08-24). Today it enforces the modular-monolith direction with custom reflection and
csproj XML parsing:

| Fact | Line | What it enforces |
| --- | --- | --- |
| `CoreHasNoInfrastructureOrHostDependencies` | `:42-48` | `Pegasus.Core`'s *loaded assembly* references contain no forbidden prefix |
| `CoreDependencyGuardDetectsForbiddenAndAllowedExamples` | `:50-78` | 24 `[InlineData]` rows proving the prefix matcher accepts `Box.V2` and rejects `Boxed` |
| `CoreProjectHasNoForbiddenDirectDependencies` | `:80-87` | `src/Pegasus.Core/Pegasus.Core.csproj` declares no forbidden package |
| `CoreDirectDependencyGuardDetectsForbiddenAndAllowedFixtures` | `:89-108` | the **inline `XDocument.Parse` fixture pattern** this ticket copies |
| `ProjectReferencesFollowTheModularMonolithDirection` | `:110-125` | Core → none, Infrastructure → `[Core]`, Web/Worker → `[Core, Infrastructure]` |
| `ApplicationSolutionExcludesSourceWorkspaces` | `:127-153` | `Pegasus.slnx` lists exactly the seven expected projects (array `:141-149`) |
| `ApplicationProjectsDoNotReferenceSourceWorkspaces` | `:155-173` | no solution project references `workspaces/` |
| `ReportRenderingHasOneCorePortAndOneInfrastructureAdapter` | `:175-188` | one port, one adapter, no `CollisionRenderer` |

Helpers: `IsForbiddenCoreDependency` `:475-478`, `ForbiddenDirectDependencies(XDocument)`
`:480-491`, `ProjectReferences(root, path)` `:493-507`, `FindRepositoryRoot()` `:509-519`.

CI runs the suite in the `unit` job of `.github/workflows/ci.yml` (`:131-147`), chained with
`&&` after `Pegasus.Core.Tests`, on `windows-latest`.

## Findings

- **The test project is `net10.0` and must stay Linux-buildable, so no desktop fact may use
  reflection.** `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj:4` sets
  `<TargetFramework>net10.0</TargetFramework>` and its four `ProjectReference` entries are
  Core, Infrastructure, Web and Worker. A `net10.0-windows10.0.26100.0` project reference
  would break `dotnet build` on Linux, which `docs/runbook.md:21-23` § Supported platform
  keeps supported. Every desktop fact must therefore be **csproj/XAML text and XML analysis**.
  - The ticket body states this as step 2 and it is confirmed by reading both files.
- **The fixture pattern already exists and is the model to copy.**
  `CoreDirectDependencyGuardDetectsForbiddenAndAllowedFixtures` (`:89-108`) builds an
  in-memory `XDocument.Parse` project carrying `PackageReference Include`,
  `PackageReference Update`, `FrameworkReference` and `Reference` items and asserts the
  exact ordered forbidden set. This proves the guard red **without touching a real project
  file** — which is what the ticket's acceptance criterion asks for.
- **`ForbiddenDirectDependencies` is currently hard-wired to the Core list and must be
  parameterised.** `:480-491` filters with `.Where(IsForbiddenCoreDependency)`, and
  `IsForbiddenCoreDependency` (`:475-478`) closes over the static
  `ForbiddenCoreDependencyPrefixes` array (`:23-40`, 15 entries). Reusing the helper for a
  second prefix list requires it to take the list as a parameter — body step 4 says exactly
  this, and it is the minimal change: three call sites (`:86`, `:107`, plus the new desktop
  fact).
- **The prefix matcher's semantics come for free.** `:475-478` matches exact equality or
  `prefix + "."`, so `Box` matches `Box.V2` and not `Boxed`; the 24-row theory at `:50-78`
  is the proof. A desktop prefix list inherits that behaviour with no new matcher.
- **`ProjectReferences` already normalises MSBuild backslashes and orders ordinally.**
  `:497-506`, with the reason in the comment at `:500-501`. Desktop expectation arrays must
  therefore be written in ordinal order — `["Pegasus.Contracts", "Pegasus.Core"]` and
  `["Pegasus.Contracts", "Pegasus.Core", "Pegasus.Desktop.Infrastructure"]` are already
  ordinal, so no re-ordering is needed.
- **`Pegasus.slnx` lists exactly seven projects today** (4 under `/src/`, 3 under `/tests/`),
  and the expected array at `:141-149` matches them one for one. Every new project added by
  a Phase 1 ticket must be added to that array or the fact fails — which is the intended
  behaviour and the reason body step 9 exists.
- **Nothing WebView-shaped exists yet.** `grep -rn 'WebView2' src tests` returns nothing;
  `find src tests -name '*.xaml'` returns nothing; `grep -rn 'WindowsAppSDK' src tests`
  returns nothing (all measured 2026-08-24). The no-WebView fact is therefore green from the
  moment it is written, and only the fixture proves it can go red.
- **`TreatWarningsAsErrors=true` and `AnalysisLevel=latest-recommended` are repository-wide**
  (`Directory.Build.props:6-7`), so the new test code must compile warning-free; there is no
  per-project relaxation to lean on.
- **The desktop projects do not exist yet.** `ls src` returns `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker` only. `src/Pegasus.Contracts`,
  `src/Pegasus.Desktop` and `src/Pegasus.Desktop.Infrastructure` are created by
  [[FND-029]] (plan handle `DSK-02-04`), [[FND-030]] (plan handle `DSK-02-05`) and
  [[FND-031]] (plan handle `DSK-02-06`) respectively — the three tickets this one is blocked
  behind.

### Facts

Each verified by reading the file or running the command named:

| Fact | Source |
| --- | --- |
| Architecture test project targets `net10.0` | `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj:4` |
| Forbidden Core prefix list holds 15 entries | `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:23-40` |
| Fixture pattern uses `XDocument.Parse` and asserts an ordered array | same file `:89-108` |
| `ForbiddenDirectDependencies` filters by `IsForbiddenCoreDependency` | same file `:480-491` |
| `ProjectReferences` normalises `\` and orders ordinally | same file `:493-507` |
| `FindRepositoryRoot` walks up to `Pegasus.slnx` | same file `:509-519` |
| `Pegasus.slnx` lists 7 projects | `Pegasus.slnx` (whole file, 13 lines) |
| No `WebView2`, no `.xaml`, no `WindowsAppSDK` under `src/` or `tests/` | `grep -rn 'WebView2' src tests`; `find src tests -name '*.xaml'`; `grep -rn 'WindowsAppSDK' src tests` — all empty, 2026-08-24 |
| Repository-wide `TreatWarningsAsErrors` | `Directory.Build.props:6-7` |
| The suite runs in CI job `unit` on `windows-latest` | `.github/workflows/ci.yml:131-147` |
| No NetArchTest / Mono.Cecil package | `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj:12-16` (four packages: coverlet, Test.Sdk, xunit, xunit.runner.visualstudio) |
| ADR-0108 does not exist | `docs/adr/README.md` lists ADR-0001…ADR-0029; `ls docs/adr/010*` returns nothing |

### Assumptions

- **A-02-12-1 — the desktop csproj files will declare their dependencies as literal
  `PackageReference`/`ProjectReference` items rather than importing them from a shared
  `.props`.** Confirmed by: reading `src/Pegasus.Desktop/Pegasus.Desktop.csproj` once
  [[FND-030]] has landed. *If wrong*: an XML-only fact reads the csproj and sees nothing, so
  the guard passes on a project that does violate the boundary — a guard that has never
  fired. Mitigation is stated in the plan: the fixture-based companion is not enough on its
  own, so the plan's step 10 plants a **real** forbidden reference and requires the suite to
  go red.
- **A-02-12-2 — central package management ([[FND-027]], plan handle `DSK-02-02`) will leave
  `PackageReference Include="X"` items in the csproj with the version moved to
  `Directory.Packages.props`.** Confirmed by: reading a desktop csproj after
  [[FND-027]] lands. *If wrong* (for example if a transitive pinning mode removes the item
  entirely), the direct-dependency fact reads an empty item group and cannot fire; the same
  planted-violation check in step 10 is what catches it.
- **A-02-12-3 — `src/Pegasus.Desktop/**/*.xaml` is the complete set of desktop XAML.**
  Confirmed by: `find src/Pegasus.Desktop -name '*.xaml'` after [[FND-030]] lands. *If
  wrong* (XAML also under `src/Pegasus.Desktop.Infrastructure`), the WebView scan misses a
  file; the plan's step 7 therefore globs both desktop project directories rather than one.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered. The responsibility being
placed is **enforcement of the desktop dependency boundary**.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The facts are source under version control; the shared artefact is the git repository, and there is no runtime state for anyone to update. |
| Unattended execution — must it run with every desktop closed? | **Yes — on the existing GitHub-hosted `windows-latest` runner, not in Azure.** | The suite must fail a pull request whether or not any developer runs it: `.github/workflows/ci.yml:131-147` job `unit` already runs it there. No new host and no Azure resource is placed. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | The lane needs no secret. `grep -n "secrets\." .github/workflows/ci.yml` returns no match today and this ticket introduces none. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing calls in; the facts read files from the checkout. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes — enforced by the repository's own test suite, which is the client-independent authority.** | The boundary must hold regardless of which agent or workstation builds. That is precisely why proposal § 27 criterion 9 is expressed as an executable fact rather than a review convention; no service outside the repository is involved. |
| Measured operational advantage — measured evidence central is materially better? | **No** | No measurement exists or is needed; the suite runs in seconds inside an existing lane. |

Two "yes" answers, both landing on the CI workflow already in this repository. Nothing lands
in Azure; the ticket's Guardrails record "Azure: no write" and this analysis agrees.

## Implications

1. **Text and XML only.** Reflection over desktop assemblies is out, permanently, for this
   project. If a reflection-based desktop fact is ever needed it belongs in
   `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle `DSK-02-13`), which is a
   Windows-target project. The plan records this as a comment in the code, not just here.
2. **One helper, parameterised — not two helpers.** `ForbiddenDirectDependencies` gains a
   prefix-list parameter. Copying it into a `ForbiddenDesktopDirectDependencies` twin would
   be the third-copy failure `docs/engineering.md` § One Core owner names.
3. **`ContractsProjectDependsOnNothing` may already exist.** [[FND-029]] (plan handle
   `DSK-02-04`) creates `src/Pegasus.Contracts` with "zero non-BCL references" as its
   acceptance criterion and may have added the fact. Body step 6 says to check first and not
   duplicate — one list per concept.
4. **A fixture alone is not proof of enforcement.** The fixture proves the *matcher* works;
   only planting a real forbidden `PackageReference` in a real desktop csproj proves the
   fact is *wired to the real project files*. The ticket's Verification section asks for
   both, and the plan keeps both.
5. **The WebView fact must be written so the ADR-0108 exemption cannot be taken by deleting
   it.** L-03 moves report rendering to an isolated non-UI WebView2 path only when ADR-0108
   lands ([[FEAT-038]], plan handle `DSK-07-12`). Until then the fact is absolute, and the
   code comment must say the future exemption is a named-file allow-list.
6. **The solution list will be extended more than once during Phase 1.** [[FND-028]]
   (`DSK-02-03`), [[FND-029]], [[FND-030]], [[FND-031]] and [[FND-038]] each add a project.
   Body step 9 says to extend the array to the final Phase 1 set *if the earlier tickets have
   not already done so* — read `Pegasus.slnx` at implementation time rather than assuming.

## Open questions

None. Every branch this ticket takes is decided by reading the tree at implementation time
(does `ContractsProjectDependsOnNothing` already exist; is `ApplicationSolutionExcludesSourceWorkspaces`
already current), and the three assumptions above each name the check that settles them and
the planted-violation step that catches them if they are wrong. No `open-questions` document
is created.
