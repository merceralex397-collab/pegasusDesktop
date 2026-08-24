# Checklist — FND-037 (plan handle `DSK-02-12`)

One box per plan step, in plan order. Every box is independently tickable.

- [ ] Confirm the three prerequisite projects exist (`ls src/Pegasus.Contracts src/Pegasus.Desktop src/Pegasus.Desktop.Infrastructure`); if any is missing, stop — blocked behind [[FND-029]], [[FND-030]], [[FND-031]]
- [ ] `get_doc_gates FND-037`, `take_ticket FND-037`, branch `task/desktop-architecture-tests` from `origin/dev`
- [ ] Read `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` in full, with `:89-108` (fixture pattern) and `:475-519` (helpers) read closely
- [ ] Add the in-code comment recording that this project is `net10.0` and desktop facts must stay text/XML based, naming `tests/Pegasus.Desktop.ViewModelTests` as the home for reflection-based desktop facts
- [ ] Add `ForbiddenDesktopDependencyPrefixes` beside `ForbiddenCoreDependencyPrefixes` (`:23-40`) with the thirteen names, kept separate from the Core list because `System.Net.Http` differs
- [ ] Change `ForbiddenDirectDependencies` (`:480-491`) to take the prefix list as a parameter and update its two call sites at `:86` and `:107`
- [ ] Re-run the suite and confirm `CoreProjectHasNoForbiddenDirectDependencies` and `CoreDirectDependencyGuardDetectsForbiddenAndAllowedFixtures` still pass unchanged
- [ ] Add `DesktopProjectsHaveNoForbiddenDirectDependencies` over both desktop csproj files, resolving paths through `FindRepositoryRoot()`
- [ ] Add `DesktopProjectReferencesFollowTheDesktopDirection` asserting Desktop.Infrastructure → `["Pegasus.Contracts", "Pegasus.Core"]` and Desktop → `["Pegasus.Contracts", "Pegasus.Core", "Pegasus.Desktop.Infrastructure"]`
- [ ] `grep -n 'Contracts' tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`; add `ContractsProjectDependsOnNothing` only if absent, and record which case applied in the plan document
- [ ] Add `DesktopXamlContainsNoWebView` scanning `*.xaml` under both desktop project directories, failing with the offending file path
- [ ] Add the ADR-0108 comment above that fact stating the future exemption is a named-file allow-list, never removal of the fact
- [ ] Add `DesktopDependencyGuardDetectsForbiddenAndAllowedFixtures` with an `XDocument.Parse` fixture covering `PackageReference Include`, `PackageReference Update`, `FrameworkReference` and bare `Reference`, plus one permitted package
- [ ] Add the companion XAML-string fixture proving the WebView scan detects `<WebView2 />` and passes a clean fragment
- [ ] Read `Pegasus.slnx`, then extend the expected array in `ApplicationSolutionExcludesSourceWorkspaces` (`:141-149`) only if it is not already current; record what was found
- [ ] Extend `docs/current-architecture.md` § Architecture invariants (`:69-91`) with the desktop boundary and the no-WebView rule, in that section's reporting tone
- [ ] Confirm `git diff --name-only` lists only `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` and `docs/current-architecture.md` — no production csproj
- [ ] Run the four-lens simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] **Verification run (this box produces `proof`)**: capture (a) `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` green with zero skipped; (b) the same command red with `Microsoft.EntityFrameworkCore.SqlServer` planted in `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj`, message naming the package; (c) the same command red with `<WebView2 />` planted in a desktop XAML file, message naming the file; (d) `pwsh ./scripts/Test-DocumentationLinks.ps1` exit `0` — then revert both plants and open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
