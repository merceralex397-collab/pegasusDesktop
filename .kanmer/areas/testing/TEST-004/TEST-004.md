---
id: TEST-004
type: ticket
title: >-
  DSK-08-04 · Scaffold `tests/Pegasus.Desktop.ViewModelTests`
  (`net10.0-windows10.0.26100.0`, no UI thread)
status: done
area: testing
assignee: codex-mcp-client
profile: feature
stageEntered:
  preparing: '2026-08-24T21:34:13.457Z'
  review: '2026-08-28T21:53:25.700Z'
  verifying: '2026-08-28T22:52:11.306Z'
  done: '2026-08-29T14:15:03.522Z'
labels:
  - desktop-conversion
  - plan-08
  - phase-1
  - tier-2
groups:
  - EPIC-009
  - HZN-002
links: []
blocks:
  - TEST-005
docs_todo: true
commits:
  - c7f6f689
  - 5602d7f1
  - 66aa3eba08f7717b590812053695cc26f3170e7a
prs:
  - '40'
archived: false
created: '2026-08-24T07:46:12.595Z'
updated: '2026-08-29T14:17:05.523Z'
---

## What

Create `tests/Pegasus.Desktop.ViewModelTests` — a xunit project targeting `net10.0-windows10.0.26100.0` that exercises desktop view models with no `DispatcherQueue`, no packaged identity and no WinRT UI type, using a fake gateway client and one shared fake clock.

## Why

Proposal §22.2 ("View-model tests") makes commands, states, cancellation, dirty state, validation, navigation decisions, stale-session and mandatory-update handling a test layer of their own. Without a project that runs them headless on `windows-latest`, every one of those behaviours is only reachable through UI automation, which is slower, flakier and cannot cover the failure branches. The plan's assumption is explicit: view models stay free of `DispatcherQueue` and WinRT UI types, so this project is also the enforcement of that design rule. Consumed by [[DSK-08-05]] and by every slice ticket in area 05.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-04`
- Plan detail: `docs/desktop/08-testing/README.md` § 2 (assumption: the ViewModel test project can target `net10.0-windows10.0.26100.0` and run on `windows-latest` without packaged identity) and § 7 (test clock inconsistency)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "View-model tests", § 5.3 native desktop layers
- Repository evidence:
  - `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` — the package set and properties to copy
  - `Pegasus.slnx:10-15` — the `/tests/` folder the project joins
  - `Directory.Build.props:1-12` — `TreatWarningsAsErrors=true`, `Nullable`, `AnalysisLevel latest-recommended`
  - `tests/Pegasus.Core.Tests/Intake/ProcessIntakeTests.cs:1208` and `tests/Pegasus.Core.Tests/.../RetainedMailTests.cs:820` — the eight-or-more private `FixedTimeProvider` copies this project must **not** add a ninth of
  - `global.json` — SDK 10.0.302
- Binding decisions:
  - L-04 — this ticket's routing names its subagent, skills and MCP tools, and every test added later must do the same.
- Depends on: `DSK-02-05` — `src/Pegasus.Desktop` must exist to reference (the plan row cites `DSK-02-03`, which in area 02 is the solution filter; the desktop scaffold is `DSK-02-05`). Coordinate with `DSK-02-13`, which lists the same project in area 02's breakdown: whichever lands first creates the project, the other verifies and extends it rather than creating a second.

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `scaffold-dotnet-test-project` (`dotnet/skills` `98f84851`, plugin `dotnet-test`) → `code-testing-agent` (same pin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-04`, § 2 assumptions and § 7, and `docs/desktop/02-architecture-and-foundation/README.md` § 5 rows `DSK-02-05` and `DSK-02-13`. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Check whether `tests/Pegasus.Desktop.ViewModelTests` already exists from [[DSK-02-13]]. If it does, skip to step 5 and treat the remaining steps as an extension; record that in the post-implementation report.
3. Load `pegasus-desktop`, then `scaffold-dotnet-test-project`. Create `tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj` with `<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>`, `<Platforms>x64</Platforms>`, `IsPackable=false`, `RestorePackagesWithLockFile=true`, the four pinned test packages from `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj`, and `ProjectReference` entries to `src/Pegasus.Desktop` and `src/Pegasus.Contracts`. Do **not** set `UseWinUI` or `WindowsPackageType` — the project must run unpackaged.
4. Register the project in `Pegasus.slnx` inside `<Folder Name="/tests/">`, restore once unlocked to generate `packages.lock.json`, and commit it.
5. Add `Support/FakeGatewayClient.cs` — a hand-rolled fake of the generated-client abstraction with per-call queued responses and recorded requests. One fake per concept; no Moq, no FluentAssertions (`docs/desktop/08-testing/README.md` § 2).
6. Add `Support/TestClock.cs` — **the one** shared `FixedTimeProvider : TimeProvider` for all desktop test projects, with a single documented base date. Record the chosen date in `tests/Pegasus.Desktop.ViewModelTests/README.md` and state that desktop tests use this type and never a private copy. This is a deliberate deviation from the repository's per-file copies, recorded in the plan row.
7. Add `Support/FakeCredentialStore.cs` and `Support/FakeNavigationService.cs` covering the DPAPI credential store and navigation abstractions introduced by [[DSK-02-06]] and [[DSK-02-08]].
8. Add `NoUiThreadDependencyTests.cs`: reflect over the public view-model types in `src/Pegasus.Desktop` and fail on any reference to `Microsoft.UI.Dispatching.DispatcherQueue` or a `Microsoft.UI.Xaml` type in a constructor parameter, field or property. Done when introducing such a dependency turns exactly this test red. Use `microsoft_docs_search` for the exact namespace names before writing the check.
9. Put `[Trait("Category", "ViewModel")]` on every test class so the CI lane of [[DSK-08-13]] and the runbook filters can select them.
10. Run `dotnet build ./Pegasus.slnx --configuration Release --no-restore`, then `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`. Done when the suite runs green in a plain console session with no packaged identity and no interactive desktop requirement.
11. Add the focused command to `docs/runbook.md` § Locked restore, build, and test with the Windows-only note, and register the `ViewModel` trait in `docs/operations.md` § Evidence profiles.
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] The project exists, is listed in `Pegasus.slnx`, has a committed lock file and builds with `TreatWarningsAsErrors`.
- [ ] View models are testable with no `DispatcherQueue` and no packaged identity; the guard test proves it.
- [ ] One shared fake clock exists for desktop tests; no ninth private `FixedTimeProvider` copy is added.
- [ ] Fake gateway client, credential store and navigation service are hand-rolled, one per concept.
- [ ] Every test class carries `[Trait("Category", "ViewModel")]`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: `Passed!`, non-zero total, run from a non-interactive console.
- [ ] `grep -rn "FixedTimeProvider" tests/Pegasus.Desktop.ViewModelTests` — expected: only `Support/TestClock.cs` defines it.
- [ ] Add a `DispatcherQueue` field to a view model temporarily and rerun — expected: `NoUiThreadDependencyTests` fails naming the type; revert and confirm green.

## Evidence tier

Tier 2 — Core/domain, in its desktop-side reading (view model). It obliges positive, contradictory, ambiguous and failure cases for the view models' own logic, with no UI thread, no HTTP and no database in the loop.

## Documentation changes

- `docs/runbook.md` § Locked restore, build, and test — the focused desktop test command and its Windows-only note.
- `docs/operations.md` § Evidence profiles — the `ViewModel` trait and what it proves.
- `tests/Pegasus.Desktop.ViewModelTests/README.md` — the shared clock type and the one date convention.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create `tests/Pegasus.Desktop.ViewModelTests/**` and edit `Pegasus.slnx`, `docs/runbook.md`, `docs/operations.md`. Must not change `src/Pegasus.Desktop` production code — a testability problem is a finding for `winui-dev`, with the smallest possible proposed change.
- **Traps**: do not add a ninth `FixedTimeProvider` copy; this project establishes the shared one. `TreatWarningsAsErrors=true` applies. The project must stay unpackaged, or it cannot run on a hosted runner. Overlap with [[DSK-02-13]] — check before creating.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

- Implemented and merged as PR [#40](https://github.com/merceralex397-collab/pegasusDesktop/pull/40); merge commit `66aa3eba08f7717b590812053695cc26f3170e7a` is on `main`.
- Added the shared `Pegasus.Desktop.ViewModelTests` project, headless fakes, the no-UI-thread guard, solution registration, and focused runbook/operations evidence.
- The source ticket requested a new `tests/Pegasus.Desktop.ViewModelTests/README.md`, but the repository markdown-placement rule forbids that location; the clock convention is recorded in the canonical `docs/runbook.md` instead.
- FND-031 may now add its infrastructure behavior tests to this shared project; no duplicate test project was created.
