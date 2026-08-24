# Research — FND-025: characterization-test gap list and dependency-rule targets

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This document is the spike's **output**. `get_doc_gates FND-025` resolves profile
`spike` to one gated boundary — `enter-done` needs `research` and
`questions-resolved` — so its existence is what would let the ticket close. It is a
pre-work scaffold: the **denominators** of the gap list are measured below with the
command that produced each, and every per-policy verdict the ticket owes is a literal
`NOT YET CAPTURED` block. `open-questions` carries one unticked `- [ ]` box per
uncaptured item.

## Question

Which business policies in `src/Pegasus.Core` can change meaning during the conversion
without a failing test, at what boundary should each missing characterization test be
written, and what are the dependency rules area 02 must turn into architecture
assertions? Proposal §22.1 requires — before any current business rule is moved — that
the entry point is identified, fixtures created, existing results captured, the
behaviour judged intentional or accidental, and a characterization test written at the
lowest reliable boundary. §24 Phase 0 exit-gate item 4 is the other half: "Target
dependency rules compile as architecture tests or documented checks."

## Current behaviour

**No parity row covers this work, and none should.** The matrix holds
`PAR-01`…`PAR-46` (`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md`
→ `46`), every row keyed to a page model under `src/Pegasus.Web/Pages/**`
(`parity-matrix.md:36-38`). Core policies and architecture tests are not screens. The
closest existing repository mechanisms — the things that enforce this today — are:

- `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` (**520 lines**,
  custom reflection) — the file area 02 extends. Its helper
  `tests/Pegasus.ArchitectureTests/TypeInspection.cs` is **12 lines**, not a framework;
  any rule this ticket writes must be expressible against something that small.
- `tests/Pegasus.Core.Tests` — the policy lane (69 `.cs` files).
- `tests/Pegasus.IntegrationTests` — 136 `.cs` files, three CI shards driven by
  `scripts/Invoke-TestShard.ps1`.
- `src/Pegasus.Core/Pegasus.Core.csproj` — 14 lines, **zero `PackageReference`
  items**, `TargetFramework net10.0`, `RuntimeIdentifiers linux-x64;win-x64`, one
  `InternalsVisibleTo Include="Pegasus.Core.Tests"`. That last line is itself a
  dependency fact: the Core test project is the only assembly with internal access.

## Findings

- The folder-level denominators are measurable now and are the honest starting point
  (F-1, F-2). They are **not** the answer: three Core folders have no matching test
  folder, yet two of them have types referenced from tests elsewhere (F-4). "No test
  folder" and "uncovered" are different claims, and conflating them would produce false
  positives the reviewer's spot-check would catch.
- The ticket body names `Documents` and `Eva` as the folders with no test counterpart.
  **There is a third: `Actors`** (F-3). It matters more than either, because
  `Actors/StaffActorFactory.cs` is the transport-neutral seam every `/api/v1` call must
  pass through, and it has **no test file naming it anywhere in the repository** (F-4).
- The two declared dependencies have not produced their inputs yet: [[FND-016]] (plan
  handle `DSK-01-03`) and [[FND-017]] (plan handle `DSK-01-04`) both report
  `docs: {}` — no `research` document exists on either, so step 1's `get_ticket_doc`
  will return `content: null` and step 8 has no `gap:` cells to fold in (F-6).
- The rule set of step 9 is expressible against the existing 12-line helper only if
  every rule is phrased as "assembly X must not (transitively) reference Y"; anything
  richer needs new machinery, which is area 02's work, not this ticket's.

### Facts

Each fact carries the command that produced it. Run in
`C:\Users\PC\Documents\GitHub\pegasusDesktop` on 2026-08-24 at `bbd1c549`.

- **F-1 — `src/Pegasus.Core` holds 107 `.cs` files across 19 folders plus 2 at the
  project root.**
  `git ls-files src/Pegasus.Core | grep -c "\.cs$"` → `107`. The two root files are
  `src/Pegasus.Core/CoreAssembly.cs` and `src/Pegasus.Core/LondonCalendar.cs`
  (`git ls-files src/Pegasus.Core | grep "\.cs$" | awk -F/ 'NF==3'`). The per-folder
  denominators, from
  `git ls-files 'src/Pegasus.Core/**/*.cs' | sed 's|src/Pegasus.Core/||' | cut -d/ -f1 | sort | uniq -c`:

  | Core folder | files | | Core folder | files |
  | --- | --- | --- | --- | --- |
  | `Actors` | 3 | | `Intake` | 32 |
  | `Address` | 2 | | `Lifecycle` | 2 |
  | `AiWork` | 2 | | `Operations` | 4 |
  | `Assessment` | 5 | | `ReferenceData` | 3 |
  | `Cases` | 8 | | `Reports` | 2 |
  | `Custody` | 2 | | `Tasks` | 5 |
  | `Documents` | 2 | | `Triage` | 4 |
  | `Eva` | 2 | | `Vehicle` | 4 |
  | `Identity` | 8 | | `Workflow` | 8 |
  | `ImageIntake` | 7 | | | |

  Total across folders: 105, plus the 2 root files = 107.
- **F-2 — `tests/Pegasus.Core.Tests` holds 69 `.cs` files across 17 folders plus 1 at
  the project root.**
  `git ls-files tests/Pegasus.Core.Tests | grep -c "\.cs$"` → `69`; the root file is
  `LondonCalendarTests.cs`. Per folder: `Address` 1, `AiWork` 1, `Assessment` 2,
  `Cases` 6, `Custody` 2, `Identity` 5, `ImageIntake` 7, `Intake` 25, `Lifecycle` 4,
  `Operations` 2, `Qdos` 3, `ReferenceData` 1, `Reports` 2, `Tasks` 1, `Triage` 2,
  `Vehicle` 1, `Workflow` 3.
- **F-3 — three Core folders have no matching test folder, not two; and one test
  folder has no Core counterpart.** Differencing F-1 against F-2: Core folders with no
  test folder are **`Actors`, `Documents` and `Eva`**. The ticket body names only
  `Documents` and `Eva`; `Actors` is the third and is not in the body. The test folder
  with no Core counterpart is **`Qdos`** — a capability-shaped folder, not a Core
  folder, so it is not a gap.
- **F-4 — "no test folder" is not the same as "uncovered", and the difference is
  measurable.** `grep -rl "<TypeName>" tests/Pegasus.Core.Tests` and the same over
  `tests/Pegasus.IntegrationTests`:

  | Type | `Core.Tests` files naming it | `IntegrationTests` files naming it |
  | --- | --- | --- |
  | `RequestUploadPolicy` (`src/Pegasus.Core/Documents/RequestUploadPolicy.cs`, 469 lines) | 1 | 1 |
  | `EvaBundleSchema` (`src/Pegasus.Core/Eva/EvaBundleSchema.cs`, 916 lines) | 1 | 1 |
  | `CaseEvaMapping` (`src/Pegasus.Core/Eva/CaseEvaMapping.cs`, 272 lines) | 2 | 2 |
  | `ActorDisplayNames` (`src/Pegasus.Core/Actors/ActorDisplayNames.cs`, 69 lines) | 3 | 1 |
  | `DocumentContracts` (`src/Pegasus.Core/Documents/DocumentContracts.cs`, 304 lines) | **0** | **0** |
  | `StaffActorFactory` (`src/Pegasus.Core/Actors/StaffActorFactory.cs`, 40 lines) | **0** | **0** |
  | `StaffSessionPolicy` (`src/Pegasus.Core/Actors/StaffSessionPolicy.cs`, 14 lines) | **0** | **0** |

  So `Documents` and `Eva` are partly covered from other folders, while `Actors`
  contains the one type with genuinely zero references from either test project — and
  that type is the seam the whole `/api/v1` surface will depend on.
- **F-5 — the high-risk policy files and their measured sizes.**
  `wc -l` gives: `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` **629**;
  `Lifecycle/CaseCommandSeams.cs` **280**; `Triage/TriageLifecycle.cs` **561**;
  `Triage/TriageContracts.cs` **378**; `Eva/EvaBundleSchema.cs` **916**;
  `Workflow/CaseWorkflowContracts.cs` **456**; `Assessment/AssessmentPolicy.cs`
  **499**; `Documents/RequestUploadPolicy.cs` **469**;
  `Identity/StaffAuthorization.cs` **78**. Note two the body states differently:
  `CaseWorkflowContracts.cs` is 456 lines and `StaffAuthorization.cs` is only 78 — the
  twelve `StaffAccessRight` values fit in a small fail-closed switch, so its risk is
  semantic, not size.
- **F-6 — the two declared dependencies have produced no documents yet.**
  `list_items --area desktop-foundation` reports `"docs": {}` for both [[FND-016]]
  (plan handle `DSK-01-03`) and [[FND-017]] (plan handle `DSK-01-04`). Step 1's
  `get_ticket_doc` will return `content: null` for each, and step 8 has no `gap:` cells
  to fold in. Both are also `blocked: true` on [[FND-014]] (plan handle `DSK-01-01`).
- **F-7 — `Pegasus.Core` has zero package references today, and one
  `InternalsVisibleTo`.** `cat src/Pegasus.Core/Pegasus.Core.csproj` — 14 lines:
  `TargetFramework net10.0`, `RuntimeIdentifiers linux-x64;win-x64`,
  `ImplicitUsings`/`Nullable` enabled, and
  `<InternalsVisibleTo Include="Pegasus.Core.Tests" />`. No `PackageReference` item
  exists (`grep -rn 'PackageReference' src/*/*.csproj` returns nothing for
  `Pegasus.Core`). This is the state rule 6 of step 9 must keep true.
- **F-8 — Core declares 227 port interfaces.**
  `grep -rn "public interface I" src/Pegasus.Core --include=*.cs | wc -l` → `227`,
  matching the plan's figure exactly.
- **F-9 — the architecture-test surface is small.** `git ls-files tests/Pegasus.ArchitectureTests`
  → 11 `.cs` files plus the `.csproj` and `packages.lock.json`.
  `wc -l tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs
  tests/Pegasus.ArchitectureTests/TypeInspection.cs` → **520** and **12**. Two named
  facts a rule must survive: `DependencyDirectionTests.CoreProjectHasNoForbiddenDirectDependencies`
  parses `src/Pegasus.Core/Pegasus.Core.csproj` directly, and
  `DependencyDirectionTests.ApplicationSolutionExcludesSourceWorkspaces` pins the
  solution contents — so the `workspaces/` tree stays deliberately outside
  `Pegasus.slnx`.
- **F-10 — xunit 2.9.3 is the only test framework.**
  `grep -rn 'PackageReference' tests/*/*.csproj` shows `xunit 2.9.3` and
  `xunit.runner.visualstudio 3.1.4` in all three test projects and no alternative.
  Proposing a second framework is out of bounds.

### Assumptions

- **A-01-17 — every uncovered policy can be characterized at tier 2 or tier 3, and
  only concurrency/lease/reference-allocation rules need tier 4.** Confirmed per policy
  by identifying its entry point and its side effects; a rule with no persistence
  dependency has no business at tier 4 or 5. Breaks the gap list's usefulness if a
  rule is pushed to tier 5 for convenience — a slow route test that could have been a
  tier-2 policy test is the specific waste this assumption exists to prevent.
- **A-01-18 — the three uncovered `Actors` types are genuinely uncovered rather than
  exercised indirectly under another name.** F-4 measures references *by type name*;
  a test could exercise `StaffActorFactory` through a helper without naming it.
  Confirmed by reading the five tests the reviewer spot-checks. Breaks the gap entry —
  and this is the specific false-positive class the acceptance criterion "no false
  positive on a spot-check of five entries" guards against.
- **A-01-19 — the `gap:` cells from [[FND-016]] and [[FND-017]] will exist by the time
  this ticket runs.** They do not today (F-6). Confirmed by `get_ticket_doc` returning
  non-null `research` for both. If they still do not exist, this ticket cannot satisfy
  its own acceptance criterion "every `gap:` cell … appears in the list", and the
  honest response is to record the shortfall rather than to invent cells.
- **A-01-20 — the desktop projects named in step 9's rules will exist as
  `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`,
  `tests/Pegasus.Desktop.ViewModelTests`.** None exists today (`ls src tests`). The
  rules are therefore written as *targets* against names that [[FND-029]] (plan handle
  `DSK-02-04`), [[FND-030]] (plan handle `DSK-02-05`), [[FND-031]] (plan handle
  `DSK-02-06`) and [[FND-038]] (plan handle `DSK-02-13`) will create, and ADR-0100
  authorises. Breaks only the naming, not the rules.

## Execution placement

This ticket writes an analysis and places no product responsibility. The six-question
test is answered once, for the **responsibility the dependency rules encode** — where
business policy lives — because that is what step 9's rule list is actually asserting,
and answering it here lets area 02 turn the rules into assertions without re-deriving
the reasoning.

**Responsibility G — ownership and execution of business policy (`Pegasus.Core`).**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **Yes** | Case lifecycle, triage, allocation and assessment rules must give every operator the same answer over the same shared case state (`src/Pegasus.Core/Lifecycle/CaseLifecycle.cs`, 629 lines; `Triage/TriageLifecycle.cs`, 561). Lands in the evolved gateway `Pegasus.Web` (L-01) executing `Pegasus.Core`, not a new unit. |
| Unattended execution | **Yes** | The Worker executes the same Core use cases with every desktop closed (`src/Pegasus.Worker/IntakeFunctions.cs:33`). Lands on the existing always-on Worker. |
| Protected credentials | **No** | `Pegasus.Core` has **zero package references** (F-7) and holds no secret; the adapters that do hold secrets live in `Pegasus.Infrastructure`. |
| Public callback | **No** | Core is transport-neutral; nothing calls into it from outside. |
| Central enforcement | **Yes** | `Identity/StaffAuthorization.cs` (78 lines, twelve `StaffAccessRight` values, fail-closed) and the `CaseMutationRequest` envelope (`Workflow/CaseWorkflowContracts.cs`, 456 lines) must hold regardless of what a client believes. |
| Measured operational advantage | **No measured evidence** | None collected, and none is needed: three "yes" answers already place the *execution* server-side. |

**Placement:** business-policy **execution** stays server-side. What travels into the
desktop is the *assembly* — Core is referenced by the desktop for transport-neutral
types and pure input policies (registration normalisation is the standing example),
which is why rule 6 below keeps Core at zero package references. This is the exact
distinction the dependency rules encode: the desktop may reference `Pegasus.Core`, and
must not reference `Pegasus.Infrastructure`, EF Core, an Azure SDK, `Box.Sdk.Gen`,
Playwright, Graph, or `Pegasus.Web`.

## NOT YET CAPTURED

Each block names the exact command and the question its output must answer. Each has a
matching unticked box in `open-questions`.

### NOT YET CAPTURED — U-1 · the `gap:` cells from the two dependencies

```
get_ticket_doc FND-016 research      # plan handle DSK-01-03
get_ticket_doc FND-017 research      # plan handle DSK-01-04
```

Must answer: every `gap:` line in both documents, folded into the list and
cross-referenced to its `PAR` row. **Both return `content: null` today** (F-6). If they
still do, record the shortfall explicitly rather than inventing cells — the acceptance
criterion cannot be met from nothing.

### NOT YET CAPTURED — U-2 · per-folder public surface

```
git grep -n "public sealed class\|public interface I\|public static class" -- 'src/Pegasus.Core/<folder>'
```

Must answer, for each of the 19 folders in F-1: its public use cases and policies, each
marked covered, partially covered or uncovered by searching the test tree for its type
name. Structure the pass with the `test-gap-analysis` skill; do not eyeball it.

### NOT YET CAPTURED — U-3 · the lowest reliable boundary per gap

```
<docs/engineering.md § Required evidence tiers, applied per policy>
```

Must answer, per uncovered or partially covered policy, one tier from the vocabulary
of `docs/engineering.md:72-89`: tier 2 Core/domain for a pure policy, tier 3
parser/adapter contract for an extraction or format rule, tier 4 LocalDB persistence
for a concurrency, lease or reference-allocation rule, tier 5 Web/API/MCP caller only
where the behaviour is observable solely through the route. Lower is better; a rule
provable at tier 2 must not be pushed to tier 5.

### NOT YET CAPTURED — U-4 · the four §22.1 items per gap

```
git grep -n "<TypeName>" src/     # entry point
ls -R reference/                  # fixture source
```

Must answer, per gap: the current entry point (`path:line`), the fixture source (a real
path under `reference/`, or a **named** `corpus/` cohort — never invented data), the
existing result and side effects to capture, and whether the behaviour looks
**intentional or accidental**. Where it looks accidental, say so and mark it "needs
approval before change"; propose no fix.

### NOT YET CAPTURED — U-5 · assertion quality on the two highest-risk lifecycles

```
<assertion-quality skill over tests/Pegasus.Core.Tests/Lifecycle and .../Triage>
```

Must answer: for `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` (629 lines, 4 test files
in `Lifecycle/`) and `src/Pegasus.Core/Triage/TriageLifecycle.cs` (561 lines, 2 test
files in `Triage/`), which existing tests assert only "no exception". Each such test is
a **gap even though a test file exists**; record it with the reason.

### NOT YET CAPTURED — U-6 · the `Actors` finding written up

```
grep -rl "StaffActorFactory" tests/          # measured: 0 files
grep -rl "StaffSessionPolicy" tests/         # measured: 0 files
```

Must answer: `Actors` recorded as a **third** Core folder with no test folder alongside
`Documents` and `Eva` (F-3), and `StaffActorFactory` (40 lines) plus
`StaffSessionPolicy` (14 lines) recorded as uncovered by name in either test project
(F-4). State the consequence: `StaffActorFactory.TryCreate` is the seam every
`/api/v1` call must pass, area 04's token flow is built directly on it, and
[[FND-019]]'s (plan handle `DSK-01-06`) `Q1.2` asks which claims it needs — a
characterization test at tier 2 is the cheapest possible insurance.

### NOT YET CAPTURED — U-7 · the numbered dependency-rule target list

```
sed -n '1,520p' tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs
cat tests/Pegasus.ArchitectureTests/TypeInspection.cs
```

Must answer: a numbered list, each item phrased as **"assembly X must not
(transitively) reference Y"** so the 12-line helper can evaluate it, covering at
minimum — (1) the desktop projects must not reference `Pegasus.Infrastructure`;
(2) must not reference EF Core (`Microsoft.EntityFrameworkCore*`); (3) must not
reference any Azure SDK (`Azure.*`, `Microsoft.Azure.*`); (4) must not reference
`Box.Sdk.Gen`, `Microsoft.Playwright` or `Microsoft.Graph`; (5) must not reference
`Pegasus.Web`; (6) `Pegasus.Core` must keep **zero** package references (F-7). Plus,
per rule, whether `DependencyDirectionTests.cs` already asserts it, needs extending, or
is new. **Write no test here** — area 02 owns that work.

### NOT YET CAPTURED — U-8 · the two constraints recorded

```
grep -rn 'PackageReference' tests/*/*.csproj
```

Must answer: that xunit 2.9.3 is the only framework (F-10) and that every proposed test
must run on the local production-mimicking stack (L-02, ADR-0014) rather than an Azure
environment — plus a note of any gap provable only with a real provider, which belongs
to the pilot ring instead.

### NOT YET CAPTURED — U-9 · the counts re-run at the head this ticket runs on

```
git ls-files 'src/Pegasus.Core/**/*.cs' | wc -l
git ls-files 'tests/Pegasus.Core.Tests/**/*.cs' | wc -l
git ls-files 'tests/Pegasus.Core.Tests' | cut -d/ -f3 | sort -u
dotnet test tests/Pegasus.ArchitectureTests
```

Must answer: the actual Core and test file counts at that head (**107** and **69** at
`bbd1c549`; note that the `**/*.cs` glob form used in the ticket's Verification block
returns 105 and 68 because it misses the two Core root files and the one test root
file — state which form produced the number), the folder difference, and a green
architecture-test run **before and after**, since this ticket changes no test.

## Implications

1. **Report `Actors` as the third uncovered folder** (F-3, F-6/U-6). The body names two
   and asks the implementer to "confirm at the current head"; the confirmation finds a
   third, and it is the most consequential of the three.
2. **Do not equate "no test folder" with "uncovered"** (F-4). `RequestUploadPolicy`,
   `EvaBundleSchema`, `CaseEvaMapping` and `ActorDisplayNames` are all referenced from
   tests despite living in folders with no counterpart. Use the type-name search as the
   coverage signal and the folder diff only as the denominator.
3. **Phrase every rule as "assembly X must not (transitively) reference Y"** (F-9).
   `TypeInspection.cs` is 12 lines; a rule that needs more machinery than that is a new
   piece of area 02's work, and saying so is more useful than writing a rule nobody can
   evaluate.
4. **The dependency inputs may not be there** (F-6). If [[FND-016]] and [[FND-017]]
   have still produced nothing, record the shortfall against the acceptance criterion
   rather than inventing `gap:` cells.
5. **Write no test and change no project file.** Area 02 owns the architecture tests
   and area 08 owns the test projects; this ticket delivers targets. The editable files
   are `docs/desktop/01-inventory-and-parity/README.md` (a pointer in § 4) and, only
   where a cell is wrong, `parity-matrix.md`.
6. **One Core owner is an invariant, not a preference.** `docs/engineering.md`
   § Engineering invariants requires one Core owner per business policy; if the analysis
   finds a second implementation of a rule, record it as a finding rather than
   consolidating it here.
7. **A grant gap is not a test gap.** A local full-privilege run proves nothing about
   deployed permissions (upstream `PLAT-035`, carried here by [[PLAT-018]] (plan handle
   `DSK-10-18`)); if a gap is really a grant gap, say so.
8. **This ticket blocks [[FEAT-004]]**, so a partial gap list stalls the slice that
   consumes it.

## Open questions

The nine uncaptured items are this spike's subject and are tracked as boxes in
`open-questions`. Two things that look like open questions are **not**:

- Which test framework to use. **Settled**: xunit 2.9.3 is the only one (F-10), and
  proposing a second is explicitly out of bounds.
- Whether a gap could be closed against an Azure test environment. **Settled**: L-02
  and ADR-0014 mean none exists. A gap provable only with a real provider belongs to
  the pilot ring, and the gap list says so rather than asking.
