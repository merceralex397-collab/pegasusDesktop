# Research — FND-025: characterization-test gap list for Core policies and dependency-rule targets

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This document is the spike's **output**, not an input to it. `get_doc_gates FND-025`
resolves profile `spike` to one gated boundary — `enter-done` needs `research` and
`questions-resolved` — so the existence of this file is what would let the ticket
close. It is a pre-work scaffold: everything under **Facts** was verified by a
read-only command quoted beside it, every analysis the ticket owes is a literal
`NOT YET CAPTURED` block, and the `open-questions` document carries one unticked
`- [ ]` box per uncaptured item. **Those boxes are the actual gate; this banner is
prose.**

## Question

Which `src/Pegasus.Core` policies have no characterization test at the lowest reliable
boundary, and what are the target dependency rules that area 02 will turn into
architecture-test assertions? Proposal §22.1 forbids moving a business rule before its
entry point is identified, fixtures created, existing results captured, the behaviour
judged intentional or accidental, and a characterization test written — "This prevents
a clean rewrite from silently losing obscure business behaviour." §24 Phase 0's fourth
exit-gate item is "Target dependency rules compile as architecture tests or documented
checks."

## Current behaviour

**No parity-matrix row covers this, and none should.** The matrix holds
`PAR-01`…`PAR-46` — `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md`
→ **46** — and every row is keyed to a page model under `src/Pegasus.Web/Pages/**`
(`parity-matrix.md:46` onward; the column is "Current entry point"). Test coverage and
assembly-reference direction are not observable capabilities, so the section earns its
keep by naming the repository mechanisms that do this job today:

- **`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`** (520 lines) is the
  file area 02 extends. It holds the forbidden-prefix list at `:23`–`:39`
  (`ForbiddenCoreDependencyPrefixes`, 15 entries), the assertion
  `CoreHasNoInfrastructureOrHostDependencies` at `:42`–`:47`,
  `CoreProjectHasNoForbiddenDirectDependencies` at `:81`, and
  `ProjectReferencesFollowTheModularMonolithDirection` at `:111`–`:125`, which asserts
  the **exact** project-reference map for the four current projects.
- **`tests/Pegasus.Core.Tests`** (69 files) is where a characterization test at tier 2
  lands; **`tests/Pegasus.IntegrationTests`** (136 files) is where a tier 4 or tier 5
  one lands.
- **`scripts/Invoke-TestShard.ps1`** (216 lines) drives the integration shards, and
  `.github/workflows/ci.yml` runs `unit` (`:131`), `sql-integration` (`:149`),
  `sql-integration-coverage` (`:185`) and `browser` (`:207`).
- **`docs/engineering.md:76-88`** § Required evidence tiers is the boundary vocabulary
  every gap must use — tiers 1…12, quoted verbatim in the Implications below.
- **`docs/engineering.md:95-104`** § Engineering invariants — "One Core owner" — is the
  rule a second implementation violates, and the reason step 7's finding is a finding
  rather than a fix.

The two upstream tickets that produce this ticket's other input — the `gap:` cells —
have not run: `grep -c 'gap:' docs/desktop/01-inventory-and-parity/parity-matrix.md`
→ **0**. See Fact F-9.

## Findings

- The ticket's headline counts are **correct**, but only under one of the two globs
  a reader would try, because git's pathspec `*` matches `/`. Both numbers are in
  Fact F-1 and F-2 with the exact commands; quote the command, not the number alone.
- The ticket body's claim that "there is **no** `Documents` or `Eva` test folder" is
  **incomplete in one direction and misleading in the other**: a third Core folder,
  `Actors`, also has no test folder, and `Eva`'s tests exist under a differently-named
  folder. Fact F-3.
  - Sub-finding: `Actors` holds `StaffActorFactory.cs` (40 lines), which
    [[FND-019]] (plan handle `DSK-01-06`) records as the fail-closed transport-neutral
    seam every token client must satisfy. A folder with no test folder that gates
    every future desktop request is the highest-value entry the gap list can carry.
- Most of the dependency rules step 9 asks for **already exist for `Pegasus.Core`**.
  The work is to extend them to the desktop assemblies and to a project-reference map
  that has no desktop entry today. Facts F-6, F-7, F-8.
- The helper the ticket body names for evaluating the rules does not do that job.
  Fact F-6 records what does. This is recorded, not corrected — the body outranks this
  document.

### Facts

Each fact carries the command that produced it. Commands were run in
`C:\Users\PC\Documents\GitHub\pegasusDesktop` on 2026-08-24 at `main`-descendant
`bbd1c549` (branch `task/desktop-plan-segmentation`).

- **F-1 — `src/Pegasus.Core` holds 107 `.cs` files, and which number you get depends
  on the glob.**
  `git ls-files 'src/Pegasus.Core/*.cs' | wc -l` → **107**. Git's pathspec `*`
  matches `/`, so that glob is recursive.
  `git ls-files 'src/Pegasus.Core/**/*.cs' | wc -l` → **105** — the two files it omits
  are the root-level `src/Pegasus.Core/CoreAssembly.cs` and
  `src/Pegasus.Core/LondonCalendar.cs`. The ticket's "107 files" is right; the
  step 2 command must be the first form, or the two root files silently vanish.
  `LondonCalendar.cs` is a real business policy (UK working-day calculation) and
  belongs in the gap list, not in a rounding error.
- **F-2 — `tests/Pegasus.Core.Tests` holds 69 `.cs` files, with the same glob trap.**
  `git ls-files 'tests/Pegasus.Core.Tests/*.cs' | wc -l` → **69**;
  `... '**/*.cs' ...` → **68**. The omitted file is
  `tests/Pegasus.Core.Tests/LondonCalendarTests.cs`, the test for the root-level policy
  of F-1.
- **F-3 — three Core folders have no same-named test folder, not two; and one of the
  three is covered under another name.**
  `git ls-files 'src/Pegasus.Core/*.cs' | sed 's|src/Pegasus.Core/||' | cut -d/ -f1 | sort -u`
  → 19 folders: `Actors`, `Address`, `AiWork`, `Assessment`, `Cases`, `Custody`,
  `Documents`, `Eva`, `Identity`, `ImageIntake`, `Intake`, `Lifecycle`, `Operations`,
  `ReferenceData`, `Reports`, `Tasks`, `Triage`, `Vehicle`, `Workflow`.
  `git ls-files 'tests/Pegasus.Core.Tests/*.cs' | sed 's|tests/Pegasus.Core.Tests/||' | cut -d/ -f1 | sort -u`
  → 17 folders: the same list **minus `Actors`, `Documents`, `Eva`**, **plus `Qdos`**.
  - **`Actors`** (3 files — `ActorDisplayNames.cs`, `StaffActorFactory.cs` 40 lines,
    `StaffSessionPolicy.cs`) has no test folder, and the ticket body does not name it.
    Its tests are partly elsewhere: `tests/Pegasus.Core.Tests/Identity/ActorDisplayNamesTests.cs`
    and `.../Identity/AutomationActorTests.cs` exist, but no file names
    `StaffActorFactory` or `StaffSessionPolicy`.
  - **`Documents`** (2 files — `DocumentContracts.cs`, `RequestUploadPolicy.cs`) has no
    test folder and no obviously-renamed home; this is the clean case the body names.
  - **`Eva`** (2 files — `CaseEvaMapping.cs`, `EvaBundleSchema.cs` **916 lines**) has no
    `Eva` test folder, but `tests/Pegasus.Core.Tests/Qdos/EvaBundleContractTests.cs` and
    `.../Qdos/EvaHandoffPolicyTests.cs` exist. **A folder-name comparison overstates the
    Eva gap and understates the Actors gap** — the analysis must be by type name, not by
    folder name, which is exactly what step 4 instructs.
  - `Qdos` is the one test folder with no Core folder of that name: the Qdos policies
    live at `src/Pegasus.Core/Intake/DirectProviders/Qdos/` (4 files) and are tested at
    `tests/Pegasus.Core.Tests/Intake/Qdos/` (4 files) **as well as** at
    `tests/Pegasus.Core.Tests/Qdos/` (3 files).
- **F-4 — Core file counts per folder, the denominator of the gap list** (from the F-3
  command with `uniq -c`): `Intake` 32, `Workflow` 8, `Identity` 8, `Cases` 8,
  `ImageIntake` 7, `Tasks` 5, `Assessment` 5, `Vehicle` 4, `Triage` 4, `Operations` 4,
  `ReferenceData` 3, `Actors` 3, `Reports` 2, `Lifecycle` 2, `Eva` 2, `Documents` 2,
  `Custody` 2, `AiWork` 2, `Address` 2, plus the 2 root files of F-1 = **107**.
  Test files per folder: `Intake` 25, `ImageIntake` 7, `Cases` 6, `Identity` 5,
  `Lifecycle` 4, `Workflow` 3, `Qdos` 3, `Triage` 2, `Reports` 2, `Operations` 2,
  `Custody` 2, `Assessment` 2, `Vehicle` 1, `Tasks` 1, `ReferenceData` 1, `AiWork` 1,
  `Address` 1, plus the 1 root file of F-2 = **69**.
- **F-5 — the key policy owners and their sizes**, each verified with `wc -l`:
  `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` **629**;
  `src/Pegasus.Core/Triage/TriageLifecycle.cs` **561**;
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs` **456**;
  `src/Pegasus.Core/Eva/EvaBundleSchema.cs` **916**;
  `src/Pegasus.Core/Identity/StaffAuthorization.cs` **78**;
  `src/Pegasus.Core/Actors/StaffActorFactory.cs` **40**.
  `AssessmentPolicy` is `src/Pegasus.Core/Assessment/AssessmentPolicy.cs`, one of five
  files in that folder, and it **is** covered by
  `tests/Pegasus.Core.Tests/Assessment/AssessmentPolicyTests.cs`.
  `Lifecycle` has **2** Core files against **4** test files — a folder-level count that
  looks healthy and says nothing about which of `CaseLifecycle.cs`'s 629 lines are
  asserted, which is why step 7's `assertion-quality` pass exists.
- **F-6 — `tests/Pegasus.ArchitectureTests` holds 11 files, and the helper the ticket
  body names is not the one that evaluates reference rules.**
  `git ls-files 'tests/Pegasus.ArchitectureTests/*.cs'` → 11 files;
  `DependencyDirectionTests.cs` **520 lines**, `TypeInspection.cs` **12 lines**.
  `cat tests/Pegasus.ArchitectureTests/TypeInspection.cs` shows one internal helper,
  `OnlyConstructorParameterTypes(Type)`, which returns the parameter types of a type's
  only public constructor — it inspects **composition**, not assembly references.
  The rules of the form "assembly X must not reference Y" are evaluated today by
  `IsForbiddenCoreDependency` over `ForbiddenCoreDependencyPrefixes`
  (`DependencyDirectionTests.cs:23-39`, used at `:47`) and by the `ProjectReferences`
  reader over the `.csproj` XML (`:111-125`). **The ticket body's step 9 says to phrase
  the rules "so the existing reflection helper `TypeInspection.cs` can evaluate it";
  this document is written to the body and phrases every rule as
  "assembly X must not (transitively) reference Y" as instructed, and records the
  discrepancy here rather than silently retargeting it.** The practical effect is nil:
  the phrasing the body asks for is exactly what `IsForbiddenCoreDependency` consumes.
- **F-7 — most of the desktop dependency rules already exist, for `Pegasus.Core`.**
  `sed -n '23,39p' tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` lists
  15 forbidden prefixes: `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`,
  `Azure`, `Microsoft.Graph`, `Box`, `MimeKit`, `DocumentFormat.OpenXml`,
  `UglyToad.PdfPig`, `Microsoft.Data.SqlClient`, `System.Net.Http`, `OpenIddict`,
  `ModelContextProtocol`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`.
  **Not present:** any WebView2 prefix (`Microsoft.Web.WebView2`) and any Playwright
  prefix. The no-WebView rule is owned by [[FND-037]] (plan handle `DSK-02-12`), which
  already carries `research`, `files`, `plan` and a 19-box `checklist`; this ticket
  supplies the target, not the test.
- **F-8 — the project-reference map is an exact-equality assertion with no desktop
  entry.** `sed -n '110,126p' tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`:
  `ProjectReferencesFollowTheModularMonolithDirection` asserts `Assert.Empty` for
  `src/Pegasus.Core/Pegasus.Core.csproj`, `Assert.Equal(["Pegasus.Core"], …)` for
  Infrastructure, and `Assert.Equal(["Pegasus.Core", "Pegasus.Infrastructure"], …)` for
  Web and for Worker. Because these are exact-equality assertions, **adding any desktop
  project to the solution changes this test's expected values**, so the rule list must
  say what the post-desktop map is, not merely what is forbidden.
  `cat Pegasus.slnx` confirms the solution holds exactly 4 `src` projects and 3 `tests`
  projects today, and `ls Directory.Packages.props` → **absent** (central package
  management is [[FND-027]]'s work, plan handle `DSK-02-02`).
- **F-9 — the parity matrix has no `gap:` cell yet.**
  `grep -c 'gap:' docs/desktop/01-inventory-and-parity/parity-matrix.md` → **0**.
  Step 8's input does not exist until [[FND-016]] (plan handle `DSK-01-03`) and
  [[FND-017]] (plan handle `DSK-01-04`) have run — both are still at `backlog` with no
  documents. This is why this ticket is `blocked: true` on the board, and it is a
  dependency, not a defect.
- **F-10 — xunit 2.9.3 is the only framework, in all three test projects.**
  `grep -rn 'xunit' tests/*/*.csproj` → `xunit` **2.9.3** and
  `xunit.runner.visualstudio` **3.1.4** in `Pegasus.ArchitectureTests`,
  `Pegasus.Core.Tests` and `Pegasus.IntegrationTests` alike. Proposing a second
  framework is out of bounds.
- **F-11 — the integration surface, for boundary-choice context.**
  `git ls-files 'tests/Pegasus.IntegrationTests/*.cs' | wc -l` → **136**.
  A rule pushed from tier 2 to tier 5 lands here and costs a shard run; the tier
  discipline of step 5 is a cost decision as well as a correctness one.
- **F-12 — the evidence-tier vocabulary is at `docs/engineering.md:76-88`**, twelve
  numbered tiers. The four this ticket uses are `:77` tier 2 Core/domain, `:78` tier 3
  parser/adapter contracts, `:79` tier 4 LocalDB persistence and `:80` tier 5
  Web/API/MCP caller. `:76` tier 1 is where the dependency rules themselves land, and
  its own words are "enforce dependency direction and one policy owner".

### Assumptions

- **A-01-5 — every Core folder maps to a business capability, so a per-folder table is
  the right shape for the gap list.** Confirmed by `docs/engineering.md:106-111`
  § Capability organization, which forbids horizontal `Common`/`Helpers`/`Utilities`
  folders. Breaks if a folder turns out to be a technical grouping after all, in which
  case the table row for it must be split by type rather than left as one row.
- **A-01-6 — a type-name search across the test tree is a sound coverage proxy.**
  Confirmed by running it and spot-checking five entries against the file contents
  (the ticket's own final Verification item). Breaks in the `Eva`/`Qdos` case of F-3,
  where the test exists under an unrelated folder name and the type name is the only
  thing that finds it — which is why the proxy is by **type name**, never by folder.
- **A-01-7 — the desktop dependency rules are an extension of
  `ForbiddenCoreDependencyPrefixes`, not a new mechanism.** Confirmed by F-6 and F-7:
  the same prefix list and the same `IsForbiddenCoreDependency` predicate can be
  applied to a desktop assembly. Breaks if the desktop assemblies must *legitimately*
  reference something on that list — `System.Net.Http` is the live candidate, because
  a desktop HTTP client is the whole point of [[FND-031]] (plan handle `DSK-02-06`) —
  in which case the rule set is per-assembly, not one shared list, and the target list
  must say so per rule.
- **A-01-8 — no gap on this list requires a real external provider.** The Core policies
  are transport-neutral by construction (`src/Pegasus.Core` has zero package
  references, asserted at `DependencyDirectionTests.cs:81`). Breaks for a rule whose
  only observable form is a provider response shape; step 11 says such a gap belongs to
  the pilot ring, not to the local stack.
- **A-01-9 — `LondonCalendar.cs` and `CoreAssembly.cs` are the only root-level Core
  files at the head this ticket runs on.** Confirmed by re-running the F-1 command.
  Breaks if a later upstream sync ([[FND-023]], plan handle `DSK-01-10`) adds another,
  which would silently escape a `**`-globbed enumeration.

## Execution placement

**This ticket places no responsibility itself** — it is analysis and produces a gap
list and a rule list. The heading is kept, and the six questions are answered for the
**responsibility the dependency rules govern**, because that is the placement decision
those rules encode and [[FND-037]] (plan handle `DSK-02-12`) will turn into assertions.

### Responsibility G — executing Core business policy, and reaching the data behind it

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | One `pegasus` database is the record for every operator; the universal mutation envelope carries `ExpectedVersion` and `EditLeaseToken` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs`, 456 lines). Ten workstations must not each hold their own answer. |
| Unattended execution — must it run with every desktop closed? | **Yes, for part of it** | The Worker runs intake and due-work policy with every desktop closed. An always-on host satisfies this and today that host is the existing Function App — no new resource is implied. The **interactive** half (a case edit an operator performs) does not need it. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **Yes** | The database is Entra-only with runtime roles; a connection string on a workstation is exactly what ADR-0103 forbids, and is precisely the rule "desktop projects must not reference `Pegasus.Infrastructure`, EF Core or the Azure SDKs" exists to make unbuildable. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing external calls a Core policy. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | `src/Pegasus.Core/Identity/StaffAuthorization.cs` (78 lines) is a fail-closed rights matrix and `docs/engineering.md:95-104` requires **one** Core owner per policy. A second implementation compiled into the desktop is the defect the rules prevent. |
| Measured operational advantage — measured evidence that central is materially better? | **No measured evidence** | None was collected and none is needed: four "yes" answers already place it. Recording "no" here would be dishonest only if it were used to argue for putting policy on the workstation, and it is not. |

**Placement:** the Core assembly stays where it is and is reached through the evolved
gateway `src/Pegasus.Web` (**L-01**); the desktop links `Pegasus.Core` for
transport-neutral types only and never `Pegasus.Infrastructure`. **No Azure write, and
no new deployment unit.**

### Responsibility H — running the characterization suite and the architecture gate

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **Yes** | A gate that only some branches run is not a gate; the result must be the same for every PR. Lands in the existing GitHub Actions `repository-check` workflow (`.github/workflows/ci.yml`, jobs `unit` `:131`, `sql-integration` `:149`). |
| Unattended execution | **No** | Every run is triggered by a push or a PR; nothing runs with the developers away. |
| Protected credentials | **No** | These lanes are tier 1 and tier 2 and carry no credential: `Pegasus.Core` has zero package references and the architecture tests read `.csproj` XML and assembly metadata. This is the "PR lane using no production credential" case, and answering "yes" here to sound safe would be the dishonesty this section exists to catch. |
| Public callback | **No** | Nothing external calls the lane. |
| Central enforcement | **Yes** | The gate must be un-bypassable from a developer machine. Lands in CI, where it already is. |
| Measured operational advantage | **No measured evidence** | Not needed; two "yes" answers place it. Note **C-01**: private-repository Windows runners bill at 2×, so this ticket proposes no new CI lane — it produces targets that [[FND-037]] folds into an existing one. |

**Placement:** the existing CI workflow, unchanged in shape. **No Azure resource, no
new lane, no credential.**

## NOT YET CAPTURED

No analysis was performed to write this document, and none may be invented. Each block
names the exact command and the question its output must answer. Each has a matching
unticked box in `open-questions`.

### NOT YET CAPTURED — U-1 · the enumeration re-run at this ticket's own head

```
git ls-files 'src/Pegasus.Core/*.cs' | wc -l
git ls-files 'src/Pegasus.Core/*.cs' | sed 's|src/Pegasus.Core/||' | cut -d/ -f1 | sort | uniq -c
git ls-files 'tests/Pegasus.Core.Tests/*.cs' | wc -l
git ls-files 'tests/Pegasus.Core.Tests/*.cs' | sed 's|tests/Pegasus.Core.Tests/||' | cut -d/ -f1 | sort | uniq -c
```

Must answer: the actual Core and Core.Tests file counts and per-folder breakdowns at
the head this ticket runs on, stated as observed and **not** copied from F-1/F-2/F-4.
Use the `'…/*.cs'` form: the `'…/**/*.cs'` form drops the root-level files (F-1).

### NOT YET CAPTURED — U-2 · per-folder policy inventory, marked covered / partial / uncovered

```
git grep -n "public sealed class\|public interface I\|public static class" -- 'src/Pegasus.Core/<folder>'
git grep -rln "<TypeName>" -- tests/
```

Must answer, per Core folder: its file count (the denominator), every public use case
and policy it declares, and each one marked **covered**, **partially covered** or
**uncovered** by a search of the test tree **for its type name** — never by folder
name (F-3 shows folder-name matching gets `Eva` and `Actors` both wrong). Structure
the pass with the `test-gap-analysis` skill; do not eyeball it.

### NOT YET CAPTURED — U-3 · the lowest reliable boundary for every gap

```
sed -n '76,88p' docs/engineering.md        # the tier vocabulary, verbatim
```

Must answer, per gap, one tier from `docs/engineering.md:76-88`: **tier 2** for a pure
policy, **tier 3** for an extraction or format rule, **tier 4** for a concurrency,
lease or reference-allocation rule, **tier 5** only where the behaviour is observable
solely through the route. **Lower is better** — a rule provable at tier 2 must not be
pushed to tier 5, and the reason is cost as well as correctness (F-11: tier 5 lands in
a 136-file project behind a shard run, and C-01 bills those minutes at 2×).

### NOT YET CAPTURED — U-4 · the four §22.1 items per gap

```
<per gap: read the policy file and its callers>
ls -R reference/
```

Must answer, for every gap, the four things proposal §22.1 demands before a rule may
move: (a) the current entry point as `path:line`; (b) the fixture source — a **real**
path under `reference/` or a **named** `corpus/` cohort, never invented data
(`AGENTS.md` § Safety rails; `corpus/` is ignored and immutable); (c) the existing
result and side effects to capture; (d) whether the behaviour looks **intentional** or
**accidental**. Where it looks accidental, mark it "needs approval before change" and
**propose no fix**.

### NOT YET CAPTURED — U-5 · `assertion-quality` pass over the two highest-risk lifecycles

```
<assertion-quality skill over tests/Pegasus.Core.Tests/Lifecycle/*.cs and .../Triage/*.cs>
```

Must answer: for `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs` (629 lines, F-5) and
`src/Pegasus.Core/Triage/TriageLifecycle.cs` (561 lines), whether the existing tests
actually assert the behaviour or merely assert "no exception". The four existing
`Lifecycle` test files (`AssignCaseEngineerTests.cs`, `AutoLinkReportEvidenceTests.cs`,
`CaseEditLeaseTests.cs`, `TerminalCaseStateTests.cs`) and the two `Triage` files
(`GetTriageDisplayNameTests.cs`, `TriageReplayTests.cs`) are the subject.
**A test that asserts only "no exception" is a gap even though a test file exists** —
record it as a gap with that reason.

### NOT YET CAPTURED — U-6 · the `gap:` cells folded in from [[FND-016]] and [[FND-017]]

```
grep -c 'gap:' docs/desktop/01-inventory-and-parity/parity-matrix.md
get_ticket_doc FND-016 research
get_ticket_doc FND-017 research
```

Must answer: every `gap:` cell those two tickets wrote, folded into this list and
cross-referenced to its `PAR` row id, so the matrix and the gap list agree.
**Blocked input, not a defect:** the count is **0** today (F-9) and both tickets are at
`backlog` with no documents. If they still have not run, say so and record which rows
are therefore unrepresented — do not invent a `gap:` cell.

### NOT YET CAPTURED — U-7 · the numbered dependency-rule target list

```
<written from the analysis; phrased as "assembly X must not (transitively) reference Y">
```

Must answer: a **numbered** list, each entry phrased as an assertion. At minimum, per
the ticket body: the desktop projects must not reference `Pegasus.Infrastructure`;
must not reference EF Core (`Microsoft.EntityFrameworkCore*`); must not reference any
Azure SDK (`Azure.*`, `Microsoft.Azure.*`); must not reference `Box.Sdk.Gen`,
Playwright or Graph packages; must not reference `Pegasus.Web`; and `Pegasus.Core`
must keep zero package references. Two things the list must also settle, from F-7 and
F-8: whether `System.Net.Http` is forbidden to the desktop as it is to Core — it cannot
be, if [[FND-031]] (plan handle `DSK-02-06`) is to hold an HTTP pipeline, so the rule
set is per-assembly (assumption **A-01-7**) — and what the **post-desktop
project-reference map** is, because `ProjectReferencesFollowTheModularMonolithDirection`
is an exact-equality assertion (F-8) and any new project changes its expected values.

### NOT YET CAPTURED — U-8 · per-rule status against the existing tests

```
sed -n '23,39p;42,47p;80,90p;110,126p' tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs
cat tests/Pegasus.ArchitectureTests/TypeInspection.cs
```

Must answer, per rule: **already asserted**, **needs extending**, or **new**. F-7 gives
the head start — 15 prefixes already forbidden to Core, with **no** WebView2 and **no**
Playwright prefix among them. **Write no test here**: area 02 owns that work through
[[FND-037]] (plan handle `DSK-02-12`), which already carries a plan and a 19-box
checklist; this ticket delivers the targets it consumes.

### NOT YET CAPTURED — U-9 · the two constraints, and any pilot-ring gap

```
grep -rn 'xunit' tests/*/*.csproj
```

Must answer: that **xunit 2.9.3 is the only framework** (F-10) and that every proposed
test runs on the local production-mimicking stack (**L-02**; ADR-0014 stands, so there
is no Azure test environment to ask for) — plus, explicitly, any gap that can only be
proven with a real provider and therefore belongs to the **pilot ring** instead of the
gap list. Assumption **A-01-8** says there should be none; say so if there is one.

### NOT YET CAPTURED — U-10 · the list written back and the gate re-run

```
dotnet test tests/Pegasus.ArchitectureTests
pwsh ./scripts/Test-DocumentationLinks.ps1
pwsh ./scripts/Test-MarkdownPlacement.ps1
git status --porcelain
```

Must answer: that the gap list and the numbered rule list are written into this
`research` document (one table per Core folder plus one numbered rule list); that
`docs/desktop/01-inventory-and-parity/README.md` § 4 Target state carries a pointer to
the delivered rule list **without duplicating it in the tree**; that
`dotnet test tests/Pegasus.ArchitectureTests` is **green before and after**, because
this ticket changes no test; and that `git status --porcelain` shows nothing but the
intended documentation edit.

### NOT YET CAPTURED — U-11 · the reviewer's spot-check

```
<reviewer picks five gap entries and greps tests/Pegasus.Core.Tests for each type name>
```

Must answer: that each of five sampled gaps is **genuinely** absent from
`tests/Pegasus.Core.Tests` — no false positive. F-3's `Eva`/`Qdos` case is the exact
shape of a false positive this check exists to catch.

## Implications

1. **Enumerate with `'…/*.cs'`, not `'…/**/*.cs'`.** F-1 and F-2: git's pathspec `*`
   matches `/`, and the `**` form silently drops `CoreAssembly.cs`,
   `LondonCalendar.cs` and `LondonCalendarTests.cs`. `LondonCalendar.cs` is a real
   working-day policy, so the difference is a missing gap-list row, not a rounding
   error.
2. **Search by type name, never by folder name.** F-3: folder-name comparison names
   `Documents` and `Eva` and misses `Actors`, whose `StaffActorFactory.cs` is the
   fail-closed seam every desktop request will pass through; and it calls `Eva`
   uncovered when `tests/Pegasus.Core.Tests/Qdos/EvaBundleContractTests.cs` covers part
   of it. Both errors are corrected by step 4's type-name pass.
3. **The ticket body's "no `Documents` or `Eva` test folder" is a planning-baseline
   observation to be re-derived, not restated.** Step 3 says "confirm at the current
   head" and this document's F-3 is what confirming looks like. The correction belongs
   in this ticket's own output; the ticket wording itself is [[FND-052]]'s to groom.
4. **Most rules already exist; the extension is the work.** F-7: 15 forbidden prefixes
   already apply to `Pegasus.Core`. What is genuinely new is (a) applying a per-assembly
   variant to the desktop projects, (b) the WebView2 prefix, absent today and owned by
   [[FND-037]], and (c) the post-desktop project-reference map, because F-8 shows that
   assertion is exact equality and breaks the moment a desktop project joins
   `Pegasus.slnx` (4 `src` + 3 `tests` projects today).
5. **`System.Net.Http` cannot be forbidden to the desktop the way it is to Core.**
   A-01-7. The rule list must be per-assembly and say so per rule, or [[FND-031]]
   cannot build an HTTP pipeline.
6. **The `gap:` input does not exist yet** (F-9, count 0). This ticket is correctly
   `blocked` on [[FND-016]] and [[FND-017]]. If it runs before them, U-6 records which
   `PAR` rows are unrepresented; it never invents a cell.
7. **Write no test, change no project file, add no test project.** The scope boundary
   is read-only over `src/` and `tests/`; area 02 owns the architecture tests
   ([[FND-037]]) and area 08 owns the test projects ([[FND-038]], plan handle
   `DSK-02-13`). The editable files are
   `docs/desktop/01-inventory-and-parity/README.md` and, only where a cell is wrong,
   `parity-matrix.md`.
8. **A second implementation is a finding, not a consolidation.**
   `docs/engineering.md:95-104` § One Core owner says "On encountering a third
   implementation, stop and consolidate" — but consolidating is a code change, and this
   ticket makes none. Record it and name the ticket that should own it.
9. **A grant gap is not a coverage gap.** Upstream `PLAT-035`'s class of defect — a
   local full-privilege run proving nothing about deployed permissions — means a gap
   that is really a missing `GRANT` must say so rather than being written up as a
   missing test. That work is carried on this board by [[PLAT-018]] (plan handle
   `DSK-10-18`).
10. **No Azure call of any kind.** This ticket's Guardrails say "no write, and no Azure
    call", and nothing in the analysis needs one: every input is a file in the
    repository.

## Open questions

The eleven uncaptured items are this spike's subject, not a defect in it; they are
tracked as U-1…U-11 above and as boxes in `open-questions`. Two items are genuinely
open beyond that scope and are **parked** there rather than blocking. Two things that
look like open questions are not:

- **Whether to propose a second test framework.** Settled: xunit 2.9.3 is the only one
  in all three test projects (F-10), and the ticket's Guardrails put a second framework
  out of bounds. Not an open question.
- **Whether any characterization test could run against an Azure test environment.**
  Settled by **L-02** and **ADR-0014**: there is no Azure dev/test/staging environment.
  A gap that needs a real provider goes to the pilot ring (step 11), not to a new
  environment. Not an open question, and not to be raised as one.

Nothing here re-opens a settled operator decision. D-004 (`OPS-10` folds into the
desktop pilot approval) and the Send-to-AI recorded exclusion are untouched by this
analysis — note only that `src/Pegasus.Core/AiWork/` (2 files) is covered by
`tests/Pegasus.Core.Tests/AiWork/AiWorkTests.cs` and is **out of parity scope**
(`docs/desktop/05-implementation-and-migration/reuse-map.md:38`), so it is inventoried
in the gap table with that note and no gap is raised against it.
