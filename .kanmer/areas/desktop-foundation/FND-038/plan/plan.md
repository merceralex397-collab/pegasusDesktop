# Plan — FND-038: Create `tests/Pegasus.Desktop.ViewModelTests` with fakes for the API client, clock and credential store

**Diff estimate: ~12 files, ~540 lines** (~110 of them a generated `packages.lock.json`).
Derived from the files document, file by file, against measured neighbours rather than
asserted: csproj ~32 lines (`tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` measured at
25 lines by `wc -l`, plus `TargetPlatformMinVersion`, `Platforms`, `RuntimeIdentifier` and
two extra `ProjectReference` lines); `packages.lock.json` ~110 (Core.Tests' is 106 lines,
same four packages, plus the Windows targeting-pack entries); `Fakes/FixedTimeProvider.cs`
~30; `Fakes/FakeGatewayClient.cs` ~90 (queued responses, a recorded-request list, and a
switch over the thirteen problem slugs at
`docs/desktop/03-gateway-api-and-data/README.md:167`); `Fakes/InMemoryCredentialStore.cs`
~35; the host fixture ~60; shell-navigation tests ~75; status-bar tests ~60; the DPAPI
round-trip test ~40; `Pegasus.slnx` +1 line (it is 13 lines today);
`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` +1 line (one entry inserted in
the ordinal array at `:141-149`); `Directory.Packages.props` +0 or +4 depending on step 2's
finding. `docs/engineering.md:201` § Plan sizing requires the estimate first.

## Approach

**Copy the repository's existing test-project shape verbatim and change only what the
Windows target framework forces, then put every fake in one `Fakes/` folder as `internal`
types so the "one fake per concept" rule is satisfied from the first commit.** The
alternative rejected is **adding a mocking framework** (Moq, NSubstitute, or
`Microsoft.Extensions.TimeProvider.Testing`): the research confirmed by reading all three
test csproj files that no such package exists anywhere in the tree — the four packages are
the whole set — and the ticket's Guardrails forbid adding one without a recorded reason. The
second alternative rejected is **letting each test file carry its own clock**, which is what
the repository actually does today (twelve `FixedTimeProvider`-shaped nested classes measured
by `grep -rn 'class.*TimeProvider' tests --include=*.cs`, with three different constructor
parameter names). `docs/desktop/08-testing/README.md` § 5 row `DSK-08-04` records the shared
clock as an explicit **Deviation** from that habit, and `docs/engineering.md:194-199`
§ Test support is the authority behind it. The third alternative, **starting a UI thread so
view models can call `DispatcherQueue.GetForCurrentThread()`**, is ruled out by the plan 02
§ 4 target-state row for this project ("View-model behaviour without the dispatcher") and by
C-01: a suite that pumps a dispatcher is slow, and private-repository `windows-latest`
minutes bill at a 2× multiplier.

One consequence is deliberate and must not be smoothed over: **this ticket ships tests that
CI does not run.** `.github/workflows/ci.yml:145-147` names its two projects literally, and
adding a third there is [[FND-040]]'s (plan handle `DSK-02-15`) decision under the ticket's
own Guardrails. The proof states the gap rather than implying coverage.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(confirmed by `get_doc_gates FND-038`, which also reports the `leave-backlog`
`governing-doc` requirement already satisfied). No existing PRD, FRD or ADR is claimed to be
met.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client converted inside this fork, which
> authorises the new top-level projects this test project is built against), authored by
> [[FND-026]] (plan handle `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`) also claims
> ADR-0100 — see [[FND-026]]'s plan for the ownership reconciliation.
> `AGENTS.md` § Product invariants is why this matters here: a new top-level project needs an
> accepted ADR proving the existing boundary cannot carry it, and for this project the proof
> is mechanical — `tests/Pegasus.ArchitectureTests` targets `net10.0`
> (`Pegasus.ArchitectureTests.csproj:4`) and so cannot exercise `ProtectedData`,
> `ApplicationData.Current.LocalFolder` or `Package.Current`.
> This plan is written to the decision as recorded in
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 and § 4; if ADR-0100 lands
> differently this plan is revised before implementation.

Because `refs` is empty, the programme-level authorities that bind today, each with the step
that satisfies it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 22.2 Test pyramid | View-model tests are a named layer of the pyramid | Steps 4–8 |
| Proposal § 24 Phase 1 | Foundation tests pass before the Phase 1 gate | Step 11, consumed by [[FND-041]] (plan handle `DSK-02-16`) |
| Plan 02 § 4 target-state table | `tests/Pegasus.Desktop.ViewModelTests` — new, xunit, references Desktop and Contracts, "View-model behaviour without the dispatcher" | Steps 2–3 and 7 |
| Plan 02 § 4 exit-gate row "Foundation tests pass" | `dotnet test` on ViewModelTests **and** ArchitectureTests | Step 11 and the third verification command |
| Plan 02 § 7 trap "Lock files with Windows-only packages" | `packages.lock.json` is RID/TFM specific; CI must restore with the same RID | Step 10 |
| Plan 02 § 7 trap "`TreatWarningsAsErrors=true` + `AnalysisLevel=latest-recommended`" | The new project compiles warning-free or not at all | Step 11 |
| Plan 08 § 5 row `DSK-08-04` | One shared `FixedTimeProvider` for desktop tests — a recorded **Deviation** from per-file copies | Step 4 |
| `docs/engineering.md:194-199` § Test support | "One fake per concept, in the shared driver, `internal`… A fake or helper copied into a second test file is the third-copy rule applied to tests" | Steps 4–6 and 8 |
| `docs/engineering.md:76` § Required evidence tiers, tier 2 | Positive, contradictory, ambiguous **and** failure cases | Verification |
| `docs/engineering.md:201` § Plan sizing | A plan states its diff estimate first | The first line of this document |
| C-01 (`docs/desktop/README.md` § Constraints) | Private-repository `windows-latest` minutes bill at 2× | Steps 7 and 11 — no process, server or database; run time recorded |
| L-02 (`docs/desktop/README.md` § Locked decisions) | The only real stack is the local Test/UAT one, and it is not a unit-test dependency | Step 5 — the gateway is always a fake |
| `Directory.Build.props:6-7` | `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended` | Step 11 |
| `AGENTS.md` § Product invariants | A new top-level project requires an accepted ADR | The New-ADR paragraph above |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 12 |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing, reviewer `pegasus-desktop-reviewer` |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `scaffold-dotnet-test-project`
  (dotnet/skills `98f84851`, plugin `dotnet-test`) → `code-testing-agent` (same pin) →
  `run-tests` (same pin). The three dotnet skills are not vendored under `.agents/skills/`
  today, so they arrive with [[TOOL-002]] (plan handle `DSK-12-02`); record in the
  post-implementation report which were actually loadable.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for `TimeProvider`
  fakes and xunit on a Windows target framework).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-038` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient, settle ownership, take.** Read `docs/desktop/02-architecture-and-foundation/README.md`
   § 4 and § 7, `docs/desktop/08-testing/README.md` § 4–5, and
   `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` (25 lines) in full. Then settle the
   duplicate-scaffold boundary with **two read-only calls**, not a discussion:
   `get_item TEST-004` (plan handle `DSK-08-04`, the area 08 twin of this scaffold) and
   `ls tests/Pegasus.Desktop.ViewModelTests`. If the directory exists, this ticket **extends**
   what is there and does not recreate it; if it does not, this ticket creates it and the
   fact is recorded here so [[TEST-004]] can be closed as already-delivered by its own owner.
   Confirm the three prerequisite projects exist —
   `ls src/Pegasus.Contracts src/Pegasus.Desktop src/Pegasus.Desktop.Infrastructure`; if any
   is missing, stop: this ticket is blocked behind [[FND-029]] (plan handle `DSK-02-04`),
   [[FND-030]] (plan handle `DSK-02-05`) and [[FND-031]] (plan handle `DSK-02-06`). Then
   `get_doc_gates FND-038`, `take_ticket FND-038`, and branch
   `task/desktop-viewmodel-tests` from `origin/dev`.
2. **Create the csproj by copying the measured shape.** Start from
   `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` and keep its five properties
   (`ImplicitUsings`, `Nullable`, `IsPackable=false`, `RestorePackagesWithLockFile=true`, and
   the framework), its four packages (`coverlet.collector`, `Microsoft.NET.Test.Sdk`,
   `xunit`, `xunit.runner.visualstudio`) and `<Using Include="Xunit" />`. Change only what the
   target forces: `<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>`,
   `<TargetPlatformMinVersion>10.0.22000.0</TargetPlatformMinVersion>`,
   `<Platforms>x64</Platforms>`, `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`.
   **Check for `Directory.Packages.props` first** — it does not exist in the tree as of
   2026-08-24, so the body's "without version literals" instruction is correct only after
   [[FND-027]] (plan handle `DSK-02-02`) lands. If it is absent, write the same four version
   literals the other test projects carry (`6.0.4`, `17.14.1`, `2.9.3`, `3.1.4`) and record
   here that [[FND-027]] centralises them; if it is present, add the versions there and none
   in the csproj. Nothing beyond this list goes in the file: anything extra is a convention
   the other three test projects do not have and needs a reason.
3. **Add exactly three `ProjectReference` entries**: `src/Pegasus.Desktop`,
   `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`. Not
   `Pegasus.Infrastructure`, `Pegasus.Web` or `Pegasus.Worker` — [[FND-037]]'s (plan handle
   `DSK-02-12`) `ForbiddenDesktopDependencyPrefixes` fact assumes the desktop side stays
   clean, and a reference here is the first place that assumption erodes.
4. **Create `Fakes/FixedTimeProvider.cs`** — one `internal sealed class FixedTimeProvider :
   TimeProvider` with a settable instant, an `Advance(TimeSpan)` method and an override of
   `GetUtcNow()`. Settle on **one** constructor parameter name; the twelve existing copies
   use three (`utcNow`, `nowUtc`, `now`) and `utcNow` is the majority form, so use it. Add a
   comment naming `docs/desktop/08-testing/README.md` § 5 `DSK-08-04` as the decision that
   makes this shared rather than per-file, and stating plainly that **consolidating the
   existing `tests/Pegasus.Core.Tests` copies is out of scope** — a different diff in a
   different area. A second clock fake anywhere under this project is a defect.
5. **Create `Fakes/FakeGatewayClient.cs`** — an in-memory implementation of the gateway
   abstraction [[FND-031]] defines, with three capabilities the later tickets need and none
   they do not: a queue of responses to return in order; a recorded list of the requests it
   received, **including headers**, so `X-Pegasus-Client-Version` and `X-Correlation-Id`
   assertions are possible without a real handler; and the ability to return a
   `PegasusProblem` for any of the thirteen `urn:pegasus:problem:<slug>` values listed at
   `docs/desktop/03-gateway-api-and-data/README.md:167` (`validation`, `not-authorized`,
   `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`,
   `client-unsupported`, `password-change-required`, `account-disabled`,
   `provider-unavailable`, `not-found`, `rate-limited`, `maintenance`). It reaches no network
   endpoint (L-02, and the ticket's Guardrails).
6. **Create `Fakes/InMemoryCredentialStore.cs`** — `IDesktopCredentialStore` over a
   dictionary, `internal`. The **real** `DpapiCredentialStore` is used with a temporary
   directory in exactly one test, the DPAPI round-trip, and nowhere else; the round-trip test
   deletes its directory in a `finally` so a failed run leaves nothing behind.
7. **Add the first behaviour tests the plan row names**: shell navigation (each rail route
   resolves to its view model, and the navigation service records the route it was asked
   for) and status-bar state (connected, disconnected, update-available). Drive them purely
   through view models with **no `DispatcherQueue`**. If a view model needs the dispatcher,
   the fix is an `IDispatcher` abstraction injected into it — that is [[FND-032]]'s (plan
   handle `DSK-02-07`) and [[FND-033]]'s (plan handle `DSK-02-08`) work, and this ticket
   reports the need rather than starting a UI thread in a unit test. Cover the tier-2 shape,
   not one happy path: a route that resolves, a route that does not, a disconnected→connected
   transition, and the update-available state that must survive a reconnect.
8. **Add one host fixture file** that builds [[FND-032]]'s generic host with the three fakes
   substituted, so every later ticket resolves real services in a test without duplicating
   registration. **One file** — a second copy of that registration in any later test file is
   the third-copy rule applied to tests (`docs/engineering.md:194-199`).
9. **Register the project.** Add it to `Pegasus.slnx` under the existing `/tests/` folder
   (the file is 13 lines and lists seven projects today), keep it out of the server entry
   point [[FND-028]] (plan handle `DSK-02-03`) creates, and insert the path into the expected
   array of `ApplicationSolutionExcludesSourceWorkspaces`
   (`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:141-149`). The array is
   compared against a list ordered with `StringComparer.Ordinal` (`:137`), so
   `tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj` is
   **inserted between** the `Pegasus.Core.Tests` and `Pegasus.IntegrationTests` entries, not
   appended. Read the array first — five Phase 1 tickets edit it, and a blind append is a
   merge conflict rather than a contribution.
10. **Restore and commit the lock file.**
    `dotnet restore ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj -r win-x64 --force-evaluate`,
    then `dotnet restore ./Pegasus.slnx --locked-mode` on Windows. The RID matters: the
    package graph is Windows-only, so the lock file is RID/TFM specific
    (plan 02 § 7) and CI must restore with the same RID. If `--locked-mode` fails, the answer
    is to regenerate with the RID CI uses — never to drop `--locked-mode`, which is what
    `.github/actions/dotnet-build/action.yml` runs for every building lane.
11. **Run the suite and measure it.**
    `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`.
    Expected: all tests pass, zero skipped, the run completes in seconds, and no process,
    server or database is started. **Record the wall-clock run time in the proof** —
    [[FND-040]] budgets the `desktop-build` lane against it, and C-01 makes that budget real
    money. Confirm the build is warning-free: `Directory.Build.props:6-7` sets
    `TreatWarningsAsErrors=true` with `AnalysisLevel=latest-recommended`, and the two
    prerequisites for that here are nullable-clean fakes and no unused usings in generated
    scaffolding.
12. **Leave CI to [[FND-040]], then simplify and open the PR.** Do not edit
    `.github/workflows/ci.yml`: its `unit` job names its two projects literally at `:145-147`
    and chains them with `&&` because pwsh reports only the last command's exit code
    (the reason is in the workflow's own comment at `:143-144`). Adding a third project there
    is [[FND-040]]'s decision. Note in the post-implementation report that these tests are
    **unenforced by CI at merge**. Run the simplification pass over this branch's own diff,
    record it under a dated `## Simplification pass` heading below, and open the PR into
    `dev`.

## Verification

Evidence tier from the body: **Tier 2 — Core/domain** (`docs/engineering.md:76`). The tier
obliges "positive, contradictory, ambiguous, and failure cases", so a project that merely
compiles and asserts one happy path has not met it: step 7 owes a resolving route, a route
that does not resolve, a state transition, and a failure response driven through
`FakeGatewayClient`'s problem slugs. Proof types: `test-output` and `command-log`.

| Command / observation | Expected evidence |
| --- | --- |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` | `Passed!`, zero skipped, with the wall-clock duration recorded |
| `dotnet restore ./Pegasus.slnx --locked-mode` on Windows | exit `0`, with `tests/Pegasus.Desktop.ViewModelTests/packages.lock.json` committed |
| `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` | `ApplicationSolutionExcludesSourceWorkspaces` passes with the extended array |
| `grep -rn 'class.*TimeProvider' tests/Pegasus.Desktop.ViewModelTests` | exactly one match — the shared `FixedTimeProvider` |
| `grep -n 'Moq\|NSubstitute\|FakeItEasy\|TimeProvider.Testing' tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj` | no match — the four packages stay four |
| `git diff --name-only` at PR time | no `.github/workflows/ci.yml`; no `src/**` production file |
| Observation, stated in the proof rather than inferred | whether `Directory.Packages.props` existed at step 2; whether `tests/Pegasus.Desktop.ViewModelTests` already existed at step 1; the fact that CI does **not** run this project yet |

## Risks / open questions

- **Scope boundary, not an open question — who creates this project.** [[TEST-004]] (plan
  handle `DSK-08-04`) is the area 08 row for the identical scaffold, and its recorded
  dependency `DSK-02-03` is a **stale handle**: `DSK-02-03` is now [[FND-028]] (the solution
  filter) and the desktop scaffold is [[FND-030]] (plan handle `DSK-02-05`). Both tickets are
  in `backlog` and untaken (2026-08-24). Step 1 settles it with `get_item TEST-004` and one
  `ls`, and records the outcome here — it is a named sibling ticket, so it belongs in this
  section and not in an `open-questions` document.
- **Risk — the tests merge unenforced.** `.github/workflows/ci.yml:145-147` runs two projects
  by name; this one is invisible to CI until [[FND-040]] adds `desktop-build`. Mitigation:
  step 12 states the gap in the post-implementation report, and [[FND-041]] (plan handle
  `DSK-02-16`) runs the suite by hand for its "Foundation tests pass" gate row in the
  meantime. Not a defect — the ordering the board chose — but it must not be reported as
  green coverage.
- **Risk — `--locked-mode` fails on a lane whose RID differs** (research assumption
  A-02-13-3). Mitigation: step 10 restores with `-r win-x64 --force-evaluate` before
  `--locked-mode` is expected to pass, and the seven building lanes are all `windows-latest`
  today (`grep -n 'runs-on' .github/workflows/ci.yml`, 2026-08-24). The failure mode is a
  lock/RID mismatch and the fix is regeneration, never dropping the flag.
- **Risk — a view model that reaches for `DispatcherQueue.GetForCurrentThread()`**
  (assumption A-02-13-1). Mitigation: step 7 reports the need to [[FND-032]] / [[FND-033]]
  and injects an `IDispatcher` instead. Starting a UI thread in a unit test would satisfy the
  test and destroy the property the plan 02 § 4 row exists to protect.
- **Risk — xunit 2.9.3 on a Windows target framework** (assumption A-02-13-2). Mitigation:
  step 11's first run settles it. If it fails, the fallback is the same package versions on a
  plain `net10.0-windows` moniker with the difference recorded — not a different test
  framework.
- **Risk — the shared clock quietly becomes the thirteenth copy.** Twelve
  `TimeProvider`-shaped nested classes already exist under `tests/`. Mitigation: step 4's
  in-file comment and the fourth verification command, which is a `grep` a reviewer can run.
- **Risk — merge conflict on the expected solution array** at
  `DependencyDirectionTests.cs:141-149`, which five Phase 1 tickets touch. Mitigation: step 9
  reads it first and inserts in ordinal position.
- **Open questions**: none. No `open-questions` document is created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds C#
and a project file, so `n/a — docs-only` does not apply._

## Dependency-cycle disposition — 2026-08-29

The current board block from FND-031 is inconsistent with the ticket plans: FND-038 requires the FND-031 implementation to exist, while FND-031's remaining acceptance tests are explicitly assigned to FND-038. The implementation prerequisite is already merged through PR #42; PR #43 is the follow-up shared-redaction correction and must merge first. Once that correction is green and merged, the coordinator should remove only the implementation-prerequisite block from the board, then take FND-038. FND-031 remains incomplete until these tests and its proof are satisfied. This is a documented dependency correction, not a bypass or a Done claim.
