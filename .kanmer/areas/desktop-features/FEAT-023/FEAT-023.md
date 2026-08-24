---
id: FEAT-023
type: ticket
title: DSK-05-23 · Extract `OperatorLabels` to the shared assembly
status: backlog
area: desktop-features
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-05
  - phase-3
  - tier-1
  - tier-2
  - tier-5
groups:
  - EPIC-006
  - HZN-004
links: []
refs:
  - docs/frd/frd-12-operator-experience.md
docs_todo: true
archived: false
created: '2026-08-24T08:02:19.029Z'
updated: '2026-08-24T08:02:19.029Z'
---

## What

Move `OperatorLabels` out of `src/Pegasus.Web/Presentation/` into the shared assembly so one operator-vocabulary list serves the web, the gateway and the desktop, re-pointing every existing consumer with no behaviour change.

## Why

`src/Pegasus.Web/Presentation/OperatorLabels.cs` (685 lines) is "the single place a persisted code becomes words an operator reads" and is consumed by 24 `.cshtml` files and 16 `.cs` files. It has no ASP.NET dependency — only `Pegasus.Core` namespaces — so it can move. If the desktop built its own map, the settled business vocabulary would silently drift between the two clients, which the one-list-per-concept rule in `AGENTS.md` § Simplicity rails forbids. Every UI slice from [[DSK-05-01]] onward binds states and dates through this list, so it must move before the first slice ships. **Coordinate with [[DSK-03-16]]**, which is the same relocation seen from the gateway side — it is one piece of work, not two.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-23`
- Plan detail: `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Pegasus.Web — REPLACE pages, KEEP the host` (the `Presentation/OperatorLabels.cs` row: EXTRACT to the shared assembly, `Pegasus.Contracts` preferred); § 3 of `README.md` ("Extract `OperatorLabels` to a shared assembly — the final home is decided in DSK-05-23 with the gateway author")
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — [[DSK-03-16]] `OperatorLabels` relocation row in `docs/desktop/03-gateway-api-and-data/README.md` § 5
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 5.4 Recommended solution structure, § 14.10 Theme system
- Repository evidence: `src/Pegasus.Web/Presentation/OperatorLabels.cs` (685 lines; `using` list is `Pegasus.Core.Assessment`, `Cases`, `Documents`, `ImageIntake`, `Intake`, `Tasks`, `Workflow`, `Identity`, `Vehicle`, `Intake.Unidentified` plus `System.Globalization` and `System.Text` — no ASP.NET); 24 `.cshtml` consumers and 16 `.cs` consumers including `src/Pegasus.Web/Presentation/MailClassificationSelection.cs`, `UploadCaseDecision.cs`, `UploadOutcome.cs`; `docs/design/README.md` § status vocabulary
- Binding decisions: L-01 the gateway is `Pegasus.Web` evolved in place, so the web keeps consuming the list from its new home; L-04 routing named on the ticket
- Depends on: `DSK-02-04` the `src/Pegasus.Contracts` project the list moves into

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (owns the move and the web re-point); `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (independent review that no vocabulary changed)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/run-tests/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; `chore` needs `plan` and `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the `reuse-map.md` `OperatorLabels` row, § 3 of the area plan, and the [[DSK-03-16]] row in `docs/desktop/03-gateway-api-and-data/README.md` § 5. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-23-operator-labels` and worktree `../pegasus-worktrees/dsk-05-23-operator-labels` from `origin/dev`.
2. **Resolve the duplicate-ticket question before any code change.** [[DSK-03-16]] describes the same relocation. Agree with the gateway author which ticket performs the move and which one closes as covered, and record the decision under this ticket's open questions — an unticked open question blocks the Kanmer move.
3. **Decide and record the final home.** The plan leaves it open between `src/Pegasus.Contracts` (preferred, because the map is presentation vocabulary) and `src/Pegasus.Core`. Decide with the gateway author on the evidence — whether any `Pegasus.Contracts` consumer would pull a Core dependency it should not have — and record the decision and its rationale in the ticket plan before moving a file.
4. Enumerate the consumers exactly: `grep -rn "OperatorLabels" src/ --include=*.cs --include=*.cshtml` and record the full list (24 `.cshtml`, 16 `.cs` at the SHA you read) in the plan, so the re-point is verifiable rather than assumed.
5. Move `src/Pegasus.Web/Presentation/OperatorLabels.cs` into the decided project, keeping the type name and every member signature identical and changing only the namespace. Do not reorganize, rename or "tidy" any label — two of its maps are settled business vocabulary and must not drift.
6. Update the `using`/`@using` in every enumerated consumer, including `_ViewImports.cshtml` where that is the cleaner single point for the Razor files.
7. Add a project reference so the decided home is reachable from `src/Pegasus.Web`, and confirm `src/Pegasus.Desktop` can reference it without pulling `Pegasus.Infrastructure`, EF Core or ASP.NET — [[DSK-02-12]]'s dependency-direction facts must stay green.
8. Add unit tests for the map in the test project that owns the new home (`tests/Pegasus.Core.Tests` if it lands in Core, otherwise the Contracts test project scaffolded by [[DSK-02-13]]): every enum value in each mapped Core enum resolves to a label, an unmapped value fails loudly rather than returning a raw `ToString()`, and the settled status vocabulary strings match `docs/design/README.md` exactly, including casing.
9. Add an architecture fact asserting there is exactly one `OperatorLabels`-shaped vocabulary type in the solution, so a later slice cannot quietly add a desktop copy.
10. Run the canonical build and test commands and confirm no behaviour changed: the existing web tests must be green with no assertion edited to accommodate the move. If a test needed editing, that is evidence the move changed behaviour — stop and investigate.
11. Update `docs/current-architecture.md` with the shared-vocabulary assembly row, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] One `OperatorLabels` exists, in the recorded shared home, with identical type and member signatures.
- [ ] Every enumerated consumer compiles against the new home; no label text changed.
- [ ] The desktop can reference the vocabulary without pulling `Pegasus.Infrastructure`, EF Core or ASP.NET.
- [ ] Unit tests cover every mapped enum value and assert the settled status vocabulary exactly.
- [ ] An architecture fact prevents a second vocabulary type from appearing.
- [ ] Existing web tests pass unchanged — no assertion was edited to accommodate the move.
- [ ] The overlap with [[DSK-03-16]] is resolved and recorded; the work is done once.

## Verification

- [ ] `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — expected: succeeds with `TreatWarningsAsErrors=true`.
- [ ] `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"` — expected: every existing test passes with no edited assertion, plus the new vocabulary facts.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: dependency-direction facts green and the single-vocabulary fact passes.
- [ ] `grep -rn "OperatorLabels" src/ --include=*.cs --include=*.cshtml` — expected: every hit resolves to the single new home; none references `Pegasus.Web.Presentation.OperatorLabels`.

## Evidence tier

Tier 1 — Static/build/architecture. Tier 2 — Core/domain. Tier 5 — Web/API/MCP caller.
Tier 1 obliges compiling the approved projects and enforcing dependency direction and one policy owner; tier 2 obliges positive and failure cases for the vocabulary map itself; tier 5 obliges evidence that the actual web routes still render the same operator words after the move.

## Documentation changes

- `docs/current-architecture.md` — implementation-map row for the shared vocabulary assembly
- `docs/desktop/05-implementation-and-migration/reuse-map.md` — record the decided home in the `OperatorLabels` row

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Contracts` (or `src/Pegasus.Core`, per the recorded decision), the `using` lines of the enumerated `src/Pegasus.Web` consumers, the project references, and the test projects. It changes no label text and no page behaviour.
- **Traps**: the settled status vocabulary must not drift — a changed string is a business-vocabulary regression, not a tidy-up; a second vocabulary list anywhere is a stop condition (`AGENTS.md` § Simplicity rails, one list per concept); this ticket and [[DSK-03-16]] describe the same move and must not both perform it; if the move would drag ASP.NET or EF into the chosen home, the choice is wrong — record and re-decide rather than adding a reference.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
