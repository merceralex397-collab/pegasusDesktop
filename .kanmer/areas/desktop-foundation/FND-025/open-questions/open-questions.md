# Open questions — FND-025

These boxes are the gate. For profile `spike` an unticked `- [ ]` line **above** the
`## Parked` heading blocks `enter-done` — and only `enter-done`; it never gates
`leave-backlog`. Verified with `get_doc_gates` (no id): `spike` resolves to
`enter-done: [research, questions-resolved]` and to nothing else.

This document exists because the `research` document is a **pre-work scaffold**: it is
the spike's output, so its mere existence would otherwise make an unanalysed spike
closable. The banner in `research` is prose; these boxes are the gate.

Every box corresponds to a `NOT YET CAPTURED` block in `research`. Every one of them
**can** be answered from this repository — no Azure call, no external service, no
network — so the only reason a box stays unticked is that the analysis has not been
done. Tick a box only when the output is attached to the ticket and the corresponding
table or list is written into the `research` document.

- [ ] **U-1 · the enumeration re-run at this ticket's own head.** File counts and
      per-folder breakdowns for `src/Pegasus.Core` and `tests/Pegasus.Core.Tests`,
      stated **as observed**, never copied from the research Facts. Use
      `git ls-files 'src/Pegasus.Core/*.cs'` — the `'…/**/*.cs'` form drops the
      root-level `CoreAssembly.cs`, `LondonCalendar.cs` and `LondonCalendarTests.cs`
      (research F-1, F-2), and `LondonCalendar.cs` is a real working-day policy that
      belongs in the gap list.
- [ ] **U-2 · per-folder policy inventory, marked covered / partially covered /
      uncovered.** One table per Core folder: file count as the denominator, every
      public use case and policy it declares, each marked by a search of the test tree
      **for its type name**. Never by folder name — research F-3 proves folder-name
      matching misses `Actors` (which holds `StaffActorFactory.cs`, the fail-closed seam
      every desktop request must satisfy) and wrongly calls `Eva` uncovered when
      `tests/Pegasus.Core.Tests/Qdos/EvaBundleContractTests.cs` covers part of it.
      Structure the pass with the `test-gap-analysis` skill; do not eyeball it.
- [ ] **U-3 · the lowest reliable boundary for every gap**, named from the twelve-tier
      vocabulary at `docs/engineering.md:76-88`: tier 2 for a pure policy, tier 3 for an
      extraction or format rule, tier 4 for a concurrency, lease or
      reference-allocation rule, tier 5 **only** where the behaviour is observable
      solely through the route. Lower is better — a rule provable at tier 2 must not be
      pushed to tier 5.
- [ ] **U-4 · the four proposal §22.1 items per gap:** the current entry point as
      `path:line`; the fixture source as a real path under `reference/` or a **named**
      `corpus/` cohort; the existing result and side effects to capture; and whether the
      behaviour is **intentional** or **accidental**. Where it looks accidental, mark it
      "needs approval before change" and **propose no fix**. Never invent fixture data;
      `corpus/` is ignored and immutable (`AGENTS.md` § Safety rails).
- [ ] **U-5 · the `assertion-quality` pass over the two highest-risk lifecycles.**
      `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` (629 lines) against its four test
      files, and `src/Pegasus.Core/Triage/TriageLifecycle.cs` (561 lines) against its
      two. **A test that asserts only "no exception" is a gap even though a test file
      exists** — record it as a gap with that reason. Folder-level counts look healthy
      here (`Lifecycle`: 2 Core files, 4 test files) and say nothing about which lines
      are asserted, which is exactly why this box exists.
- [ ] **U-6 · the `gap:` cells folded in from [[FND-016]] (plan handle `DSK-01-03`) and
      [[FND-017]] (plan handle `DSK-01-04`)**, each cross-referenced to its `PAR` row
      id so the matrix and the gap list agree. **Blocked input, not a defect:**
      `grep -c 'gap:' docs/desktop/01-inventory-and-parity/parity-matrix.md` → **0**
      today, and both tickets are at `backlog` with no documents. If they still have not
      run, record which `PAR` rows are therefore unrepresented — and invent no cell.
- [ ] **U-7 · the numbered dependency-rule target list**, every entry phrased as
      "assembly X must not (transitively) reference Y". At minimum: the desktop projects
      must not reference `Pegasus.Infrastructure`; nor EF Core
      (`Microsoft.EntityFrameworkCore*`); nor any Azure SDK (`Azure.*`,
      `Microsoft.Azure.*`); nor `Box.Sdk.Gen`, Playwright or Graph packages; nor
      `Pegasus.Web`; and `Pegasus.Core` must keep zero package references. The list must
      also settle two things the existing tests force: whether `System.Net.Http` is
      forbidden to the desktop as it is to Core — it cannot be if [[FND-031]] (plan
      handle `DSK-02-06`) is to hold an HTTP pipeline, so the rule set is **per
      assembly** — and what the **post-desktop project-reference map** is, because
      `ProjectReferencesFollowTheModularMonolithDirection`
      (`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:111-125`) is an
      exact-equality assertion that breaks the moment a desktop project joins
      `Pegasus.slnx`.
- [ ] **U-8 · per-rule status against the existing tests** — **already asserted**,
      **needs extending**, or **new**. The head start is real:
      `ForbiddenCoreDependencyPrefixes` (`DependencyDirectionTests.cs:23-39`) already
      forbids fifteen prefixes to Core, with **no** WebView2 prefix and **no**
      Playwright prefix among them. **Write no test here** — [[FND-037]] (plan handle
      `DSK-02-12`) owns the assertions and already carries a plan and a 19-box
      checklist; this ticket delivers the targets it consumes.
- [ ] **U-9 · the two constraints recorded, and any pilot-ring gap named.** That xunit
      **2.9.3** is the only framework in all three test projects, and that every
      proposed test runs on the local production-mimicking stack. Then, explicitly: any
      gap that can only be proven with a real external provider and therefore belongs to
      the **pilot ring** rather than the gap list. The research assumption A-01-8 says
      there should be none; if there is one, say which.
- [ ] **U-10 · the list written back and the gate re-run.** The gap tables and the
      numbered rule list are in the `research` document;
      `docs/desktop/01-inventory-and-parity/README.md` § 4 Target state carries a
      **pointer** to the rule list rather than a duplicate of it;
      `dotnet test tests/Pegasus.ArchitectureTests` is green **before and after**
      (this ticket changes no test); `pwsh ./scripts/Test-DocumentationLinks.ps1` and
      `pwsh ./scripts/Test-MarkdownPlacement.ps1` exit 0; and `git status --porcelain`
      shows nothing beyond the intended documentation edit.
- [ ] **U-11 · the reviewer's spot-check of five gap entries** — each genuinely absent
      from `tests/Pegasus.Core.Tests`, with no false positive. The `Eva` / `Qdos`
      folder-name mismatch in research F-3 is the exact shape of the false positive this
      check exists to catch.

## Parked (explicitly deferred)

Everything below this heading is **not** counted by the gate.

- [ ] Correcting the ticket body's line "note there is **no** `Documents` or `Eva` test
      folder", which is incomplete in one direction (`Actors` has none either) and
      misleading in the other (`Eva` is partly covered under
      `tests/Pegasus.Core.Tests/Qdos/`). **Default taken:** leave the ticket text alone
      and carry the correction in this ticket's own output, because step 3 already
      instructs "confirm at the current head" and confirming is what produces the
      correction. Ticket wording is [[FND-052]]'s to groom. Reopened if a later ticket
      cites the two-folder claim as a fact.
- [ ] Whether the numbered dependency-rule list should also be written into the
      repository rather than kept in this ticket's `research` document. **Default
      taken:** keep it here and add only a pointer to
      `docs/desktop/01-inventory-and-parity/README.md` § 4 — the ticket's own
      § Documentation changes says the list is "kept in the ticket research document,
      not duplicated in the tree", and ticket-transient documents live in Kanmer under
      the Markdown-placement rule. Reopened if [[FND-037]] finds the pointer
      insufficient to work from.

## Not open questions — settled, and not to be re-raised

- **A second test framework.** xunit 2.9.3 is the only one in
  `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests` and `Pegasus.IntegrationTests`
  alike, and the ticket's Guardrails put a second framework out of bounds.
- **An Azure test environment for any characterization run.** Settled by **L-02** and
  **ADR-0014**: there is no Azure dev/test/staging environment. A gap that needs a real
  provider goes to the pilot ring (U-9), not to a new environment.
- **Consolidating a second implementation if the analysis finds one.**
  `docs/engineering.md:95-104` § One Core owner would have it consolidated, but this
  ticket makes no code change: record it as a finding and name the ticket that should
  own it. That is the body's instruction, not a question.
