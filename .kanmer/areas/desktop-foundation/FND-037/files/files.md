# Files — FND-037 (plan handle `DSK-02-12`)

Surveyed 2026-08-24 against fork `main`. Paths that do not exist yet name the ticket that
creates them.

## Where the change lands

| Path | Why |
| --- | --- |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | The only file with production edits. Adds `ForbiddenDesktopDependencyPrefixes` beside the Core array at `:23-40`; adds `DesktopProjectsHaveNoForbiddenDirectDependencies`, `DesktopProjectReferencesFollowTheDesktopDirection`, `DesktopXamlContainsNoWebView`, `DesktopDependencyGuardDetectsForbiddenAndAllowedFixtures`, and `ContractsProjectDependsOnNothing` if [[FND-029]] (plan handle `DSK-02-04`) has not already added it; changes `ForbiddenDirectDependencies` (`:480-491`) to take the prefix list as a parameter, which touches its two existing call sites at `:86` and `:107`; extends the expected solution array at `:141-149`. **What could break:** the two existing Core facts if the parameterisation changes their behaviour — they must keep passing unchanged. |
| `docs/current-architecture.md` § Architecture invariants (`:69-91`) | One sentence extending the dependency-direction statement with the desktop boundary and the no-WebView rule. The section explicitly "reports how the running system is wired" to `AGENTS.md` § Product invariants rather than restating it (`:77-79`), so the addition must be a report, not a new rule. |

Nothing else is edited. In particular **no production csproj is touched**: the ticket's
Guardrails forbid changing a project to make a fact pass.

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:89-108` | The inline `XDocument.Parse` fixture pattern to copy verbatim — it asserts an **exact ordered array**, not a "contains", and it deliberately covers `PackageReference Include`, `PackageReference Update`, `FrameworkReference` and bare `Reference` because `ForbiddenDirectDependencies` reads all four element names. Copy the coverage, not just the shape. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:475-478` | The matcher is exact-equality-or-`prefix + "."`. This is why `"Box"` in a prefix list catches `Box.V2` and not `Boxed`, and why the desktop list needs no new matching code. The 24-row theory at `:50-78` is its executable specification — mirror a handful of those rows for the desktop list rather than inventing a second matcher. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:493-507` | `ProjectReferences` strips the path, normalises MSBuild `\` (comment `:500-501` explains that Linux does not treat `\` as a separator) and sorts `StringComparer.Ordinal`. Desktop expectation arrays must therefore be written in **ordinal order**, and a test that fails only on Linux is almost always this. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:509-519` | `FindRepositoryRoot()` walks parents until it finds `Pegasus.slnx`. Every new fact that opens a file must go through it; a relative path from `AppContext.BaseDirectory` will not resolve. |
| `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj:4` | `net10.0`. This single line is the constraint that makes every desktop fact text-based: this project can never reference a `net10.0-windows10.0.26100.0` project without breaking the Linux build that `docs/runbook.md:21-23` supports. |
| `Pegasus.slnx` | The seven projects the expected array at `:141-149` mirrors, and the file that determines whether body step 9 has work to do. Read it, do not assume — [[FND-028]], [[FND-029]], [[FND-030]], [[FND-031]] and [[FND-038]] each add a row and may already have extended the array. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 4 | The target-state project table and the exact dependency-direction sentence the facts encode: "Desktop and Desktop.Infrastructure must not reference `Pegasus.Infrastructure`, Entity Framework, Azure SDKs, Box/Graph SDKs, or `Microsoft.AspNetCore.*`; Contracts references nothing but the BCL/System.Text.Json". The exit-gate table two rows below names the two gate rows this ticket owns. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 7 | Two traps that bite here: "do not introduce NetArchTest or Mono.Cecil" (the repository uses hand-rolled reflection and csproj parsing on purpose), and "`TreatWarningsAsErrors=true` + `AnalysisLevel=latest-recommended` apply to the new projects". |
| `Directory.Build.props:6-7` | `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, repository-wide. New test code compiles warning-free or not at all; there is no per-project escape. |
| `docs/desktop/README.md` § Locked decisions, row L-03 | Why the WebView fact is absolute *today*: report rendering moves to an isolated non-UI WebView2 path **only when ADR-0108 lands**. Until then any WebView2 reference is a gate failure, and the future exemption is a named-file allow-list — never the deletion of the fact. |
| `.github/workflows/ci.yml:131-147` | The `unit` job that runs this suite, and the comment explaining why its two `dotnet test` calls are chained with `&&`: pwsh reports only the last command's exit code. Relevant because a new fact is only enforcement once CI runs it. |
| `docs/current-architecture.md:69-91` | The tone the documentation change must match: the section reports wiring and defers rule ownership to `AGENTS.md` § Product invariants. A new invariant *statement* here would compete with that owner. |

## Ripple effects

- **Tests**: the whole `tests/Pegasus.ArchitectureTests` suite is re-run. The two Core facts
  at `:80-87` and `:89-108` change call shape when `ForbiddenDirectDependencies` gains a
  parameter and must be verified unchanged in behaviour.
- **CI**: the `unit` job (`.github/workflows/ci.yml:131-147`) picks the new facts up with no
  workflow change; `scripts/Get-CiChangeFlags.ps1:11` `$buildPattern` already matches
  `^(src|tests)/`, so a change here sets `build=true` without editing the classifier.
  [[FND-040]] (plan handle `DSK-02-15`) additionally runs the suite in the `desktop-build`
  lane.
- **Blocked tickets**: [[FND-041]] (plan handle `DSK-02-16`) draws its "Architecture
  boundaries enforced" gate-row evidence from this ticket's planted-violation run, and
  [[REL-005]] (plan handle `DSK-09-05`) runs the suite in the `desktop-package` lane. Both
  are recorded as `blocks` on this ticket.
- **Documentation**: `docs/current-architecture.md` § Architecture invariants gains the
  desktop boundary sentence. `scripts/Test-DocumentationLinks.ps1` and
  `scripts/Test-MarkdownPlacement.ps1` run in the CI `documentation` job
  (`.github/workflows/ci.yml:71-87`) over that change.
- **No contract ripple.** This board's usual contract ripple — `openapi/pegasus-v1.json` and
  the generated client — does **not** apply: this ticket touches no endpoint, no DTO and no
  serialized shape. Recorded so the reviewer sees it was checked, not forgotten.

## Out of scope

Recording what the ticket's Guardrails already forbid, so the reviewer sees each as a
decision:

- **Any production `.csproj`.** If a real project violates a boundary, the fix belongs to the
  owning ticket ([[FND-030]] or [[FND-031]]), never to a loosened assertion here.
- **A WebView2 exemption of any kind.** ADR-0108 does not exist (`ls docs/adr/010*` returns
  nothing); [[FEAT-038]] (plan handle `DSK-07-12`) authors it.
- **Reflection over desktop assemblies**, and with it any reference from this `net10.0`
  project to a Windows-target project. Reflection-based desktop facts belong in
  [[FND-038]]'s Windows-target project.
- **NetArchTest, Mono.Cecil or any other architecture-test package.** Area 02 § 7 rules them
  out; the four packages in the csproj stay four.
- **`.github/workflows/ci.yml`.** [[FND-040]] owns the `desktop-build` lane and [[REL-005]]
  owns `desktop-package`.
- **Creating the desktop projects.** [[FND-029]], [[FND-030]] and [[FND-031]] own them; this
  ticket only asserts against them and is blocked until they exist.
