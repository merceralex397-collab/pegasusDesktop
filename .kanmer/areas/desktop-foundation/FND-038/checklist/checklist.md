# Checklist — FND-038: Create `tests/Pegasus.Desktop.ViewModelTests`

One box per plan step, in plan order. Tick a box only when the thing it names is true in the
worktree.

- [ ] **Orient and settle the duplicate-scaffold boundary.** Read plan 02 § 4 and § 7, plan 08 § 4–5, and `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` in full; run `get_item TEST-004` and `ls tests/Pegasus.Desktop.ViewModelTests`, and record in the plan which of "this ticket creates it" or "this ticket extends it" applied.
- [ ] **Confirm the prerequisites exist.** `ls src/Pegasus.Contracts src/Pegasus.Desktop src/Pegasus.Desktop.Infrastructure` succeeds for all three; if not, stop and report the ticket blocked behind [[FND-029]], [[FND-030]], [[FND-031]].
- [ ] **Take the ticket.** `get_doc_gates FND-038`, `take_ticket FND-038`, branch `task/desktop-viewmodel-tests` created from `origin/dev`.
- [ ] **Check for `Directory.Packages.props`** and record the finding in the plan: absent → four version literals (`6.0.4`, `17.14.1`, `2.9.3`, `3.1.4`); present → versions there and none in the csproj.
- [ ] **Create `tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj`** with the Core.Tests property set plus `net10.0-windows10.0.26100.0`, `TargetPlatformMinVersion 10.0.22000.0`, `Platforms x64`, `RuntimeIdentifier win-x64`, the four packages and `<Using Include="Xunit" />` — and nothing else.
- [ ] **Add exactly three `ProjectReference` entries** — `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts` — and no reference to `Pegasus.Infrastructure`, `Pegasus.Web` or `Pegasus.Worker`.
- [ ] **Create `Fakes/FixedTimeProvider.cs`**: one `internal sealed` `TimeProvider` with a settable instant, `Advance(TimeSpan)`, `GetUtcNow()`, the constructor parameter named `utcNow`, and a comment naming `DSK-08-04` and stating that consolidating the existing Core-side copies is out of scope.
- [ ] **Create `Fakes/FakeGatewayClient.cs`** with queued responses, recorded requests including headers, and the ability to return a `PegasusProblem` for each of the thirteen `urn:pegasus:problem:<slug>` values at `docs/desktop/03-gateway-api-and-data/README.md:167`.
- [ ] **Create `Fakes/InMemoryCredentialStore.cs`** implementing `IDesktopCredentialStore` over a dictionary, `internal`.
- [ ] **Write the DPAPI round-trip test** using the real `DpapiCredentialStore` against a temporary directory that is deleted in a `finally`, and use the real store in no other test.
- [ ] **Write the shell-navigation tests**: every rail route resolves to its view model, the navigation service records the route requested, and an unknown route fails as designed — with no `DispatcherQueue` created.
- [ ] **Write the status-bar state tests**: connected, disconnected, update-available, a disconnected→connected transition, and update-available surviving a reconnect.
- [ ] **Drive one failure case through `FakeGatewayClient`** so the tier-2 obligation (positive, contradictory, ambiguous, failure) is met rather than a single happy path.
- [ ] **Add the single host fixture file** that builds the [[FND-032]] generic host with the three fakes substituted; confirm no registration code is duplicated in any test file.
- [ ] **Add the project to `Pegasus.slnx`** under the existing `/tests/` folder, and keep it out of the server entry point owned by [[FND-028]].
- [ ] **Insert the project path into `ApplicationSolutionExcludesSourceWorkspaces`** (`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:141-149`) in ordinal position, between the `Pegasus.Core.Tests` and `Pegasus.IntegrationTests` entries — after reading the array as it currently stands.
- [ ] **Generate and commit the lock file**: `dotnet restore ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj -r win-x64 --force-evaluate`, then `dotnet restore ./Pegasus.slnx --locked-mode` exits `0` on Windows.
- [ ] **Confirm `.github/workflows/ci.yml` is untouched** in `git diff --name-only`, and note in the post-implementation report that these tests are not enforced by CI until [[FND-040]] adds `desktop-build`.
- [ ] **Run the simplification pass** over this branch's own diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] **Verification / proof.** Run `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` (all pass, zero skipped, wall-clock time recorded for [[FND-040]] to budget against), `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`, `dotnet restore ./Pegasus.slnx --locked-mode`, and the two `grep` guards from the plan's Verification table; capture every output as `test-output` and `command-log` proof, and state in it that `Directory.Packages.props` was present or absent, that the project did or did not already exist, and that CI does not yet run this suite. Open the PR into `dev`.

## Progress notes

## Progress notes

- 2026-08-29 — Stopped at the duplicate-scaffold guard. TEST-004 already owns and delivered tests/Pegasus.Desktop.ViewModelTests on origin/dev; no FND-038 checklist item was ticked and no repository source was changed. Exact audit, skipped-command disposition, and remaining ownership blocker are recorded in the plan, post-implementation report, and scratch.

## Amended-scope progress — 2026-08-29

The ownership amendment makes TEST-004 the owner of the existing scaffold and FND-038 the owner of this extension. The original project-creation, baseline-fake, shared-clock, no-UI-guard, solution-registration, and architecture-list boxes are superseded and remain unticked rather than being falsely re-marked as FND-038 work.

- [x] Reused the existing TEST-004 ViewModel test project and all of its baseline support.
- [x] Added the FND-031 credential, header, retry, redaction, rotation, retention, and current options-validation coverage.
- [x] Added only the test-only current-infrastructure support required by those tests.
- [x] Confirmed the current target has no FND-032 host/options/log-provider API; documented host-fixture dependency without pulling another task branch or changing production.
- [x] Ran the Windows RID restore, locked solution restore, Release build, focused ViewModel tests, architecture tests, scope guards, and simplification pass.
- [ ] Independent `pegasus-desktop-reviewer` review and PR remain pending; no PR has been opened.
