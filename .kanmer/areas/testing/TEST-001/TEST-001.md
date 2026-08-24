---
id: TEST-001
type: ticket
title: >-
  DSK-08-01 · Scaffold `tests/Pegasus.Api.ContractTests` (xunit 2.9.3,
  WebApplicationFactory, locked restore)
status: preparing
area: testing
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:34:12.811Z'
labels:
  - desktop-conversion
  - plan-08
  - phase-2
  - tier-5
groups:
  - EPIC-009
  - HZN-003
links: []
blocks:
  - TEST-002
docs_todo: true
archived: false
created: '2026-08-24T07:46:12.549Z'
updated: '2026-08-24T21:34:12.811Z'
---

## What

Create `tests/Pegasus.Api.ContractTests`, a new xunit 2.9.3 test project that owns the `/api/v1` contract: the committed OpenAPI snapshot, the generated-client compile check, request/response serialization and the problem-response shapes. It is the project every later contract ticket in this epic adds cases to.

## Why

Proposal §22.2 (API contract tests) requires an OpenAPI snapshot, generated-client compilation, serialization, problem responses, authentication and authorization, version compatibility, concurrency conflicts, paging/filtering/sorting and backward compatibility during rollout. The repository has no such project: `Pegasus.slnx` lists exactly three test projects (`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`). Once the desktop is a generated-client caller, an undeclared contract change is not a compile error in `Pegasus.Web` — it reaches an installed MSIX on an operator's workstation as a runtime failure. The snapshot test is what makes that change visible in the PR that causes it. Feeds [[DSK-08-02]], [[DSK-08-03]] and [[DSK-08-11]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-01`
- Plan detail: `docs/desktop/08-testing/README.md` § 4 (target state, row "API contract tests") and § 2 (facts: xunit 2.9.3 is the only framework, hand-rolled fakes)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "API contract tests", § 21.2 stage 6
- Repository evidence:
  - `Pegasus.slnx:10-15` — the `/tests/` solution folder the new project must join
  - `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` — the exact package set and property block to copy (`net10.0`, `RestorePackagesWithLockFile`, xunit 2.9.3, `xunit.runner.visualstudio` 3.1.4, `Microsoft.NET.Test.Sdk` 17.14.1, `coverlet.collector` 6.0.4)
  - `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:14` — `Microsoft.AspNetCore.Mvc.Testing` 10.0.10, the pinned version to reuse
  - `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` — `IntakeWebApplicationFactory : WebApplicationFactory<Program>`, the pattern for a host-backed factory
  - `src/Pegasus.Web/Program.cs:101-122` — `Runtime:Profile` accepts only `DevelopmentOffline` (Development environment) or `Production`; any other value throws
  - `Directory.Build.props:1-12` — `TreatWarningsAsErrors=true` applies to test projects
  - `docs/runbook.md` § Locked restore, build, and test — the canonical commands the new project must not break
- Binding decisions:
  - L-01 — the gateway is `Pegasus.Web` evolved in place, so the contract tests boot `Pegasus.Web`, not a new host.
  - L-02 — Test/UAT is local; this project runs against `WebApplicationFactory` and LocalDB only, never an Azure resource.
- Depends on: `DSK-03-01` — `Pegasus.Contracts` supplies the DTOs, problem types and paging envelope the tests serialize. `DSK-03-02` — the `/api/v1` route-group skeleton behind `Features:DesktopGateway` must exist for a document to snapshot (the plan row cites `DSK-03-01` and describes it as the route-group skeleton; in area 03 that skeleton is `DSK-03-02`, so both are listed).

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `scaffold-dotnet-test-project` (`dotnet/skills` `98f84851`, plugin `dotnet-test`) → `run-tests` (`dotnet/skills` `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-01`, § 4 and § 7, then `docs/desktop/12-agent-tooling/skill-routing.md` § "Work type routing" row *Test authoring / grading*. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own git worktree and branch (`AGENTS.md` § Repository task workflow steps 1–2).
2. Load `pegasus-desktop`, then `scaffold-dotnet-test-project`, and follow its layout section. Create `tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj` by copying the property block of `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` verbatim (`<TargetFramework>net10.0</TargetFramework>`, `ImplicitUsings`, `Nullable`, `IsPackable=false`, `RestorePackagesWithLockFile=true`) and the four test packages at the same pinned versions. Add `Microsoft.AspNetCore.Mvc.Testing` `10.0.10`. Add `ProjectReference` entries to `src/Pegasus.Web/Pegasus.Web.csproj` and `src/Pegasus.Contracts/Pegasus.Contracts.csproj` (the Contracts project is created by [[DSK-03-01]]/[[DSK-02-04]]; if it does not exist yet, stop and take that ticket first). Add `<Using Include="Xunit" />` as the other test projects do.
3. Register the project in `Pegasus.slnx` inside the existing `<Folder Name="/tests/">` element, in alphabetical position. Done when `dotnet restore ./Pegasus.slnx --locked-mode` fails only for the missing `packages.lock.json`, and succeeds after one unlocked `dotnet restore ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj` writes it; commit the generated `packages.lock.json`.
4. Add `tests/Pegasus.Api.ContractTests/ContractTestWebApplicationFactory.cs` — a `WebApplicationFactory<Program>` modelled on `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26`. It must set `UseEnvironment("Development")` and configure `Runtime:Profile=DevelopmentOffline` and `Features:DesktopGateway=true`; any other profile value throws at `src/Pegasus.Web/Program.cs:118-121`. Keep it free of LocalDB where a test does not need persistence — persistence coverage belongs to [[DSK-08-03]].
5. Extend the `OpenApiSnapshotTests.cs` [[DSK-03-04]] creates — that ticket owns the file and its normalisation contract. Run `ls tests/Pegasus.Api.ContractTests/OpenApiSnapshotTests.cs` first: if it exists, add cases to it in place and change none of its normalisation; if [[DSK-03-04]] has not landed, create that one file under [[DSK-03-04]]'s four rules, restated here verbatim so the two cannot drift — stable property ordering, two-space indent, `\n` line endings, no server-specific host — and never a second snapshot test file. The test resolves the OpenAPI document from the running factory, serializes it under exactly those four rules, and compares it byte-for-byte with the committed snapshot `openapi/pegasus-v1.json` (committed by [[DSK-03-04]]). When that snapshot file is absent, write the produced document to `openapi/pegasus-v1.json.actual` and fail with a message naming that path and the command to promote it. Done when an added endpoint makes exactly this test red.
6. Add `GeneratedClientCompilesTests.cs`: assert that the committed Kiota output (generated by `eng/api/Generate-ApiClient.ps1`, [[DSK-03-05]]) is referenced and that a representative request builder and model type resolve at runtime through `typeof(...)`. A compile-time reference is the real gate; the test exists so a deleted or stale generated file fails a test rather than only a build.
7. Add `ProblemResponseContractTests.cs` covering the problem-details shape produced by the `/api/v1` mapping ported from `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs`: `type`, `title`, `status`, `detail` and the correlation identifier field. Assert the exact strings, not "contains".
8. Put `[Trait("Category", "Contract")]` on every test class, matching the convention at `tests/Pegasus.IntegrationTests/AdministrationPolicyPersistenceTests.cs:10`. The trait is what lets `ci.yml` and the runbook select this project ([[DSK-08-13]]).
9. Run `dotnet restore ./Pegasus.slnx --locked-mode`, then `dotnet build ./Pegasus.slnx --configuration Release --no-restore`. `TreatWarningsAsErrors=true` is on for test projects (`Directory.Build.props:6`); suppress any analyzer warning per file with a written reason, never globally.
10. Run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`. Done when it is green and reports a non-zero test count.
11. Run the canonical solution command `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"` and confirm the three existing projects are unaffected.
12. Add the focused command for the new project to `docs/runbook.md` § Locked restore, build, and test beside the existing focused forms, and record the `Contract` trait in `docs/operations.md` § Evidence profiles.
13. Run the simplification pass over the branch diff (`AGENTS.md` step 4) and record it under a dated `## Simplification pass` heading in the ticket plan document. Open the PR into `dev`.

## Acceptance criteria

- [ ] `tests/Pegasus.Api.ContractTests` exists, is listed in `Pegasus.slnx`, and has a committed `packages.lock.json`.
- [ ] The OpenAPI snapshot test fails when an endpoint, parameter or schema changes without the snapshot being regenerated.
- [ ] The generated client is referenced and its types resolve.
- [ ] Every test class carries `[Trait("Category", "Contract")]`.
- [ ] The canonical `dotnet test ./Pegasus.slnx --filter "Category!=Corpus"` selection stays green.

## Verification

- [ ] `dotnet restore ./Pegasus.slnx --locked-mode` — expected: exit 0, no lock-file drift reported.
- [ ] `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — expected: exit 0, zero warnings.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: `Passed!` with a non-zero total.
- [ ] Temporarily add a dummy `/api/v1/__probe` endpoint, rerun the project — expected: `OpenApiSnapshotTests` fails naming `openapi/pegasus-v1.json`; remove the probe and confirm green again.

## Evidence tier

Tier 5 — Web/API/MCP caller. It obliges that actual `/api/v1` routes are reached through a real host: authentication, validation, exception translation and the serialized contract are observed over HTTP, not asserted against handler objects.

## Documentation changes

- `docs/runbook.md` § Locked restore, build, and test — add the focused `dotnet test` command for the new project.
- `docs/operations.md` § Evidence profiles — register the `Contract` trait and what it proves.
- `docs/desktop/08-testing/README.md` § 4 — mark the "API contract tests" row as existing.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create `tests/Pegasus.Api.ContractTests/**` and edit `Pegasus.slnx`, `docs/runbook.md`, `docs/operations.md`. Must not change `src/Pegasus.Web` behaviour, must not touch `tests/Pegasus.IntegrationTests`, and must not add a fourth `Runtime:Profile`.
- **Traps**: `TreatWarningsAsErrors=true` applies to test projects — analyzer warnings in the generated client or test fakes break the build; suppress per file with a reason. New tests must carry a `Category` trait or the CI filters silently lose them. xunit 2.9.3 only; hand-rolled fakes, no Moq and no FluentAssertions. Never fabricate domain data; fixtures come from `reference/` and the existing builders, never `corpus/`. Overlaps [[DSK-03-04]], which also creates `tests/Pegasus.Api.ContractTests` and registers it in the same `Pegasus.slnx` folder: one project, one scaffold — this ticket owns it, so run `ls tests/Pegasus.Api.ContractTests` before step 2 and extend the existing project in place if [[DSK-03-04]] landed first, and neither ticket ever creates a second project. `OpenApiSnapshotTests.cs` runs the other way — [[DSK-03-04]] owns that one file and its four normalisation rules (stable property ordering, two-space indent, `\n` line endings, no server-specific host), and step 5 extends it rather than writing a second snapshot test.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
