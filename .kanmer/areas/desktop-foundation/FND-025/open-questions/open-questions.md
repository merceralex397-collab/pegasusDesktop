# Open questions — FND-025

These boxes are the gate. For profile `spike` an unticked `- [ ]` line **above** the
`## Parked` heading blocks `enter-done` — and only `enter-done`; it never gates
`leave-backlog`. Verified with `get_doc_gates` (no id): `spike` resolves to
`enter-done: [research, questions-resolved]` and nothing else.

Every box corresponds to a `NOT YET CAPTURED` block in the `research` document. The
denominators are already measured there; what is missing is the per-policy verdict.
Tick a box only when the entry is written into the `research` document and the command
that produced it is recorded beside it.

- [ ] **U-1 · the `gap:` cells from [[FND-016]] (plan handle `DSK-01-03`) and
      [[FND-017]] (plan handle `DSK-01-04`).** **Currently unavailable**: both report
      `docs: {}`, so `get_ticket_doc <id> research` returns `content: null` and both are
      themselves blocked on [[FND-014]] (plan handle `DSK-01-01`). Matters because the
      acceptance criterion is "every `gap:` cell … appears in the list,
      cross-referenced to its `PAR` row". Recommended answer if they are still empty:
      record the shortfall explicitly against that criterion — **never invent cells**.
- [ ] **U-2 · the public surface of all 19 Core folders, each item marked covered,
      partially covered or uncovered.** Use
      `git grep -n "public sealed class\|public interface I\|public static class"` per
      folder and the `test-gap-analysis` skill to structure the pass; do not eyeball it.
      The denominators are measured in research F-1 (107 `.cs` files: 105 across 19
      folders plus `CoreAssembly.cs` and `LondonCalendar.cs` at the root).
- [ ] **U-3 · the lowest reliable boundary named per gap**, in the tier vocabulary of
      `docs/engineering.md:72-89` — tier 2 for a pure policy, tier 3 for an extraction
      or format rule, tier 4 for a concurrency/lease/reference-allocation rule, tier 5
      only where the behaviour is observable solely through the route. Lower is better;
      pushing a tier-2 rule to tier 5 is the waste this box exists to prevent.
- [ ] **U-4 · the four §22.1 items per gap**: entry point (`path:line`), fixture source
      (a real path under `reference/`, or a **named** `corpus/` cohort — never invented
      data), the existing result and side effects to capture, and whether the behaviour
      looks intentional or accidental. Where accidental, mark "needs approval before
      change" and propose no fix.
- [ ] **U-5 · assertion quality on the two highest-risk lifecycles.** Apply the
      `assertion-quality` skill to the existing tests of
      `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` (629 lines; 4 test files in
      `tests/Pegasus.Core.Tests/Lifecycle`) and `src/Pegasus.Core/Triage/TriageLifecycle.cs`
      (561 lines; 2 test files in `.../Triage`). A test that asserts only "no exception"
      is a gap even though a test file exists — record each with the reason.
- [ ] **U-6 · the `Actors` finding written up.** Research F-3 and F-4 measure it:
      `Actors` is a **third** Core folder with no matching test folder (the ticket body
      names only `Documents` and `Eva`), and `StaffActorFactory`
      (`src/Pegasus.Core/Actors/StaffActorFactory.cs`, 40 lines) and `StaffSessionPolicy`
      (14 lines) are named by **zero** files in either `tests/Pegasus.Core.Tests` or
      `tests/Pegasus.IntegrationTests`. Matters because `StaffActorFactory.TryCreate` is
      the transport-neutral seam every `/api/v1` call must pass and area 04's token flow
      is built directly on it — see [[FND-019]]'s (plan handle `DSK-01-06`) `Q1.2`.
      Recommended: record it as a tier-2 gap with the highest priority in the list.
- [ ] **U-7 · the numbered dependency-rule target list**, each item phrased as
      "assembly X must not (transitively) reference Y" so the 12-line helper
      `tests/Pegasus.ArchitectureTests/TypeInspection.cs` can evaluate it, and each
      stating whether `DependencyDirectionTests.cs` (520 lines) already asserts it,
      needs extending, or is new. Minimum six rules, listed in research U-7. **Write no
      test** — area 02 owns that.
- [ ] **U-8 · the two constraints recorded.** xunit 2.9.3 is the only framework
      (measured: all three test projects, no alternative), and every proposed test must
      run on the local production-mimicking stack (L-02, ADR-0014). Note any gap
      provable only with a real provider — it belongs to the pilot ring, not to a test.
- [ ] **U-9 · the counts re-run at the head this ticket runs on, and a green
      architecture-test run before and after.** Measured at `bbd1c549`: **107** Core
      `.cs` files and **69** `Core.Tests` `.cs` files. Note that the glob form in the
      ticket's Verification block (`git ls-files 'src/Pegasus.Core/**/*.cs' | wc -l`)
      returns **105** and **68**, because it misses the two Core root files and the one
      test root file — state which form produced the number you report.
      `dotnet test tests/Pegasus.ArchitectureTests` must be green both before and after,
      since this ticket changes no test.

## Parked (explicitly deferred)

Everything below this heading is **not** counted by the gate.

- [ ] Whether the `tests/Pegasus.Core.Tests/Qdos` folder — the one test folder with no
      Core counterpart (research F-3) — should be recorded in the gap list at all.
      Default taken: **no**. It is capability-shaped rather than folder-shaped, so it is
      not a Core coverage gap; a one-line note that it exists and why is enough.
      Reopened if the reviewer reads its absence from the list as an oversight.
- [ ] Whether `src/Pegasus.Core/CoreAssembly.cs` and `src/Pegasus.Core/LondonCalendar.cs`
      — the two project-root files — need their own gap-list row.
      `LondonCalendarTests.cs` exists at the test-project root, so `LondonCalendar` is
      covered; `CoreAssembly.cs` is an assembly marker, not a policy. Default taken:
      one line noting both, no gap row. Reopened only if `CoreAssembly.cs` turns out to
      carry behaviour.
- [ ] Whether the ticket's own Verification glob should be corrected to the form that
      returns 107/69 rather than 105/68. Safe to defer: research U-9 records both forms
      and their outputs, so no number is lost. Correcting the ticket text belongs to
      [[FND-052]] (board grooming — unrunnable verification commands).

**Not open questions.** The test framework (xunit 2.9.3 only) and the absence of an
Azure test environment (L-02, ADR-0014) are settled. Do not raise either.
