# Plan — FND-037: Extend `DependencyDirectionTests` for the desktop boundaries and the no-WebView rule

**Diff estimate: ~2 files, ~130 lines.** Derived from the files document, not asserted:
`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` gains ~125 lines — a 13-entry
prefix array (~17), four new facts (~12 + ~13 + ~18 + ~28), an optional
`ContractsProjectDependsOnNothing` (~9), two explanatory comments (~8), plus ~8 modified
lines where `ForbiddenDirectDependencies` (`:480-491`) gains a parameter and its two call
sites at `:86` and `:107` follow, and ~4 lines added to the expected solution array at
`:141-149`. `docs/current-architecture.md` § Architecture invariants gains ~4 lines.
`docs/engineering.md:203` § Plan sizing requires the estimate first; these numbers are sized
against the existing facts they mirror, each of which was measured.

## Approach

**Extend the existing file with text-and-XML facts, parameterise the one helper that already
does the work, and prove each fact red twice — once with an inline fixture and once with a
real planted violation.** The alternative rejected is **reflection over the desktop
assemblies**, the obvious way to write these facts: it is impossible here, because
`tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj:4` targets `net10.0` and
must stay Linux-buildable (`docs/runbook.md:21-23`), so it can never reference a
`net10.0-windows10.0.26100.0` project. The second alternative rejected is **a second helper
`ForbiddenDesktopDirectDependencies`** copied from `:480-491`: `docs/engineering.md` § One
Core owner treats the second copy as the moment to consolidate, and the helper differs only
in which prefix list it filters by — so it takes a parameter instead. The third alternative,
**NetArchTest or Mono.Cecil**, is ruled out by `docs/desktop/02-architecture-and-foundation/README.md`
§ 7; the repository enforces direction with hand-rolled reflection and csproj parsing on
purpose.

The double proof matters and is not ceremony. The inline fixture proves the *matcher* fires
on a forbidden name; only planting a real `PackageReference` in a real desktop csproj proves
the fact is *wired to the real project files*. A fact that reads a file the desktop csproj
does not actually populate would pass forever — the guard that has never fired, which
`docs/engineering.md` § Lessons from the predecessor says is deleted.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(confirmed by `get_doc_gates FND-037`: `"refs": []`, `"docs_todo": true`, and the
`leave-backlog` `governing-doc` requirement already reported `satisfied: true`). No existing
PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client converted inside this fork, which
> authorises the desktop projects whose boundaries these facts guard), authored by
> [[FND-026]] (plan handle `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`) also claims
> ADR-0100 — see [[FND-026]]'s plan for the ownership reconciliation.
> A second decision this plan is written *around* rather than to: ADR-0108 (isolated
> non-UI WebView2 HTML→PDF rendering), authored by [[FEAT-038]] (plan handle `DSK-07-12`);
> [[FND-007]] (plan handle `DSK-00-07`) also claims ADR-0108 — see [[FEAT-038]]'s plan for
> the ownership reconciliation. ADR-0108 does not exist yet (`ls docs/adr/010*` returns
> nothing, 2026-08-24), which is exactly why the no-WebView fact is absolute today.
> This plan is written to the decisions as recorded in `docs/desktop/README.md`
> § Locked decisions (L-03) and `docs/desktop/02-architecture-and-foundation/README.md` § 4;
> if either ADR lands differently this plan is revised before implementation.

Because `refs` is empty, the programme-level authorities that bind today, each with the step
that satisfies it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 5.3 (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md`) | Desktop projects must not reference Entity Framework database contexts, Azure SDK credentials or server integration implementations | Steps 3–4 |
| Proposal § 27 acceptance criterion 9 | Desktops never connect directly to the production database | Steps 3–4, and the planted-violation run in step 10 |
| Proposal § 27 acceptance criterion 2 | No primary workflow embeds or depends on the web application | Step 7 (no WebView element) and step 3 (`Microsoft.AspNetCore` and `Pegasus.Web` in the forbidden list) |
| Plan 02 § 4 exit-gate row "No WebView/web dependency in the package" | No `WebView2` XAML element until ADR-0108 lands | Step 7 |
| Plan 02 § 4 exit-gate row "Architecture boundaries enforced" | New facts red on a forbidden reference, **proved by a temporary failing fixture** | Steps 8 and 10 |
| Plan 02 § 4 dependency-direction sentence | Desktop.Infrastructure → Contracts + Core; Desktop → Contracts + Core + Desktop.Infrastructure; Contracts → nothing | Steps 5–6 |
| Plan 02 § 7 trap | No NetArchTest, no Mono.Cecil | The whole approach; asserted by verification command 4 |
| L-03 (`docs/desktop/README.md` § Locked decisions) | WebView2 only through the isolated non-UI path ADR-0108 authorises | Step 7's comment, which names the allow-list rule |
| `docs/engineering.md` § One Core owner | One implementation per concept; stop at the third copy | Steps 4 and 6 (parameterise, do not duplicate; do not re-add `ContractsProjectDependsOnNothing`) |
| `docs/engineering.md` § Required evidence tiers, tier 1 | "enforce dependency direction and one policy owner… This proves consistency only" | The Verification section, which states the limit |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 11 |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing, reviewer `pegasus-desktop-reviewer` |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
  (verified present, 2026-08-24).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `code-testing-agent` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `run-tests`
  (same pin). Neither dotnet skill is vendored under `.agents/skills/` today — that
  directory holds only `pegasus-release` and `project/` — so they arrive with [[TOOL-002]]
  (plan handle `DSK-12-02`); record which were actually loadable.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-037` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's eleven implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient and take.** Read `docs/desktop/02-architecture-and-foundation/README.md` § 4 and
   § 7, then `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` in full (520
   lines) — particularly the fixture at `:89-108` and the helpers at `:475-519`. Confirm the
   three prerequisite projects exist: `ls src/Pegasus.Contracts src/Pegasus.Desktop
   src/Pegasus.Desktop.Infrastructure`. If any is missing, stop — this ticket is blocked
   behind [[FND-029]] (plan handle `DSK-02-04`), [[FND-030]] (plan handle `DSK-02-05`) and
   [[FND-031]] (plan handle `DSK-02-06`). Then `get_doc_gates FND-037`, `take_ticket
   FND-037`, and branch `task/desktop-architecture-tests` from `origin/dev`.
2. **Record the hard constraint in code, not only here.** Add a comment above the new facts
   stating that `tests/Pegasus.ArchitectureTests` targets `net10.0`
   (`Pegasus.ArchitectureTests.csproj:4`) and therefore cannot reference the
   `net10.0-windows10.0.26100.0` desktop projects, so desktop facts are csproj/XAML text and
   XML analysis; reflection-based desktop facts belong in
   `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle `DSK-02-13`). Without the
   comment the next author will "fix" the facts by adding a project reference and break the
   Linux build.
3. **Add `ForbiddenDesktopDependencyPrefixes`** beside `ForbiddenCoreDependencyPrefixes`
   (`:23-40`), holding at least the thirteen names the body lists:
   `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`, `Azure`, `Microsoft.Graph`,
   `Box`, `MimeKit`, `Microsoft.Data.SqlClient`, `OpenIddict`, `Microsoft.Playwright`,
   `Microsoft.Web.WebView2`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`. Do
   **not** merge it into the Core list: Core forbids `System.Net.Http` and the desktop
   requires it, so one shared list would be wrong in both directions.
4. **Add `DesktopProjectsHaveNoForbiddenDirectDependencies`.** Change
   `ForbiddenDirectDependencies` (`:480-491`) to take `string[] forbiddenPrefixes` and match
   against it instead of closing over the Core array — then update its two existing call
   sites at `:86` and `:107` to pass `ForbiddenCoreDependencyPrefixes`, and confirm both
   Core facts still pass unchanged before adding anything else. Load
   `src/Pegasus.Desktop/Pegasus.Desktop.csproj` and
   `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj` with
   `XDocument.Load(Path.Combine(FindRepositoryRoot(), …))` (`:509`) and assert
   `Assert.Empty` for both.
5. **Add `DesktopProjectReferencesFollowTheDesktopDirection`**, mirroring
   `ProjectReferencesFollowTheModularMonolithDirection` (`:110-125`) and using the existing
   `ProjectReferences(root, path)` helper (`:493`), which already normalises backslashes and
   sorts ordinally:
   `src/Pegasus.Desktop.Infrastructure` → exactly `["Pegasus.Contracts", "Pegasus.Core"]`;
   `src/Pegasus.Desktop` → exactly
   `["Pegasus.Contracts", "Pegasus.Core", "Pegasus.Desktop.Infrastructure"]`. Both arrays are
   already in ordinal order, so write them as they stand.
6. **Add `ContractsProjectDependsOnNothing` only if it does not exist.** First
   `grep -n 'Contracts' tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`. If
   [[FND-029]] added the fact, extend it rather than adding a second — one list per concept.
   If it did not, assert that `src/Pegasus.Contracts/Pegasus.Contracts.csproj` declares no
   `PackageReference`, `ProjectReference` or `FrameworkReference`. Record which case applied
   in this document under a dated note.
7. **Add `DesktopXamlContainsNoWebView`.** Enumerate `*.xaml` under **both**
   `src/Pegasus.Desktop` and `src/Pegasus.Desktop.Infrastructure` (assumption A-02-12-3 in
   the research document: XAML may not be confined to the app project) and assert no file
   contains `WebView2` or `<WebView` as an element name. Fail with the offending file path
   in the message. Add a comment naming **ADR-0108** and stating that when the isolated
   non-UI report renderer lands (area 07, [[FEAT-038]]), the exemption is a **named-file
   allow-list**, never the removal of this fact.
8. **Add `DesktopDependencyGuardDetectsForbiddenAndAllowedFixtures`** in the style of
   `:89-108`: an `XDocument.Parse` fixture carrying `Microsoft.EntityFrameworkCore.SqlServer`,
   `Azure.Identity`, `Microsoft.Web.WebView2.Core` and one permitted package (for example
   `CommunityToolkit.Mvvm`), asserting the exact ordered forbidden set; plus a companion
   XAML-**string** fixture proving the WebView scan detects `<WebView2 />` and passes a clean
   fragment. Cover `PackageReference Update`, `FrameworkReference` and bare `Reference` as
   the Core fixture does — `ForbiddenDirectDependencies` reads all four element names, so a
   fixture that only covers `Include` under-proves the guard.
9. **Extend `ApplicationSolutionExcludesSourceWorkspaces`** (`:127-153`; the expected array
   is `:141-149`) so it lists exactly what `Pegasus.slnx` holds. Read `Pegasus.slnx` first —
   [[FND-028]] (plan handle `DSK-02-03`) and [[FND-038]] may already have extended it, and a
   second edit of the same array is a merge conflict, not a contribution.
10. **Prove red twice, then green.**
    (a) `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj
    --configuration Release` — expected: every fact passes, zero skipped, and the two fixture
    guards demonstrate the forbidden cases.
    (b) Temporarily add `<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />`
    to `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj` in the
    worktree, re-run, and confirm `DesktopProjectsHaveNoForbiddenDirectDependencies` fails
    **naming the package**; revert.
    (c) Temporarily add a `<WebView2 />` element to a desktop XAML file, re-run, confirm
    `DesktopXamlContainsNoWebView` fails **naming the file**; revert.
    Capture all three outputs — they are the ticket proof and the evidence [[FND-041]] (plan
    handle `DSK-02-16`) reuses for its "Architecture boundaries enforced" gate row.
11. **Documentation, simplification pass, PR.** Extend `docs/current-architecture.md`
    § Architecture invariants (`:69-91`) with the desktop boundary and the no-WebView rule,
    matching that section's reporting tone rather than stating a new rule (it defers rule
    ownership to `AGENTS.md` § Product invariants at `:77-79`). Run the four-lens
    simplification pass over this branch's own diff, record it under a dated
    `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**
(`docs/engineering.md:76`). The obligation is dependency direction and one policy owner
enforced by executable facts. The tier's own words limit the claim — "This proves consistency
only" — so the proof must **not** claim the desktop is free of a WebView dependency at
runtime or in the package; the package-content scan is [[FND-041]]'s gate row, not this
ticket's. Proof types: `test-output` and `command-log`.

| Command / observation | Expected evidence |
| --- | --- |
| `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` | `Passed!`, zero skipped; the new facts named in the output |
| Same command with `<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />` planted in `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj` | `DesktopProjectsHaveNoForbiddenDirectDependencies` fails, message names `Microsoft.EntityFrameworkCore.SqlServer` |
| Same command with `<WebView2 />` planted in a desktop XAML file | `DesktopXamlContainsNoWebView` fails, message names the file |
| `grep -n 'NetArchTest\|Mono.Cecil' tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` | no match — the four packages stay four |
| `git diff --name-only` at PR time | exactly `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` and `docs/current-architecture.md`; **no production csproj** |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit `0` (the CI `documentation` job runs it over the `docs/` change) |

Behaviours to state in the proof rather than infer: whether step 6 found an existing
`ContractsProjectDependsOnNothing`; whether step 9 found the solution array already current;
and whether both Core facts passed unchanged after the helper was parameterised.

## Risks / open questions

- **Risk — a fact that reads nothing and therefore never fires** (research assumptions
  A-02-12-1 and A-02-12-2: the desktop csproj might carry its dependencies through an
  imported `.props` rather than literal items). Mitigation: step 10(b) plants a real
  reference in a real csproj; a guard that stays green under a real violation is the failure
  this catches, and `docs/engineering.md` § Lessons from the predecessor says such a guard is
  deleted rather than kept.
- **Risk — someone "fixes" a desktop fact by adding a project reference from this `net10.0`
  project**, breaking the Linux build. Mitigation: step 2's in-code comment, and the fact
  that CI's ubuntu jobs would catch it — but the comment is what stops the attempt.
- **Risk — merge conflict on the expected solution array** at `:141-149`, which four Phase 1
  tickets touch. Mitigation: step 9 reads `Pegasus.slnx` first and records what it found;
  the array is edited once per ticket, never speculatively.
- **Risk — the desktop and Core prefix lists drift into one.** `System.Net.Http` is
  forbidden for Core and required by the desktop, so a merged list is wrong in both
  directions. Mitigation: step 3 states it explicitly and the two arrays stay adjacent so the
  difference is visible.
- **Scope boundary, not an open question — who authors ADR-0108.** [[FEAT-038]] (plan handle
  `DSK-07-12`) owns it, with [[FND-007]] (plan handle `DSK-00-07`) as the other claimant.
  This ticket writes the fact absolute and the comment names the allow-list rule; it takes no
  view on the ADR's content.
- **Scope boundary, not an open question — whether `ContractsProjectDependsOnNothing` exists.**
  [[FND-029]] owns `src/Pegasus.Contracts` and may have added it. Step 6 is a `grep`, not a
  question for anyone.
- **Open questions**: none. No `open-questions` document is created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch changes
C# and documentation, so `n/a — docs-only` does not apply._
