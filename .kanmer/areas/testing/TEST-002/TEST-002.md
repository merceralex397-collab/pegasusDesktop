---
id: TEST-002
type: ticket
title: >-
  DSK-08-02 · Authorization and failure-path test template for every `/api/v1`
  command
status: preparing
area: testing
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:34:13.034Z'
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
  - TEST-003
  - TEST-011
docs_todo: true
archived: false
created: '2026-08-24T07:46:12.565Z'
updated: '2026-08-24T21:34:13.034Z'
---

## What

Add a data-driven template to `tests/Pegasus.Api.ContractTests` that gives every `/api/v1` command endpoint the same five checks — unauthenticated 401, wrong right 403, stale version 409, bad input 400 problem, replayed operation key idempotent — and a guard test that fails when a command endpoint has no entry in the coverage table.

## Why

Proposal §22.3 makes it a merge rule: *every API command must have authorization and failure-path tests*. Area 03 adds roughly a hundred command endpoints across `DSK-03-08` … `DSK-03-15`; written by hand, per endpoint, the coverage will be uneven and the gaps invisible. A missing 403 test on one command endpoint is an operator with the wrong right performing a case action nobody detects until audit. The guard test is what turns "we should test that" into a red build. Extends the project created by [[DSK-08-01]]; consumed by [[DSK-08-11]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-02`
- Plan detail: `docs/desktop/08-testing/README.md` § 4 (target state row "Server integration": *every `/api/v1` command has authorization and failure-path tests*) and § 7 (two-policy-engines trap)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "API contract tests", § 22.3 "Coverage policy"
- Repository evidence:
  - `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` — the existing error-to-problem mapping the `/api/v1` groups port ([[DSK-03-02]])
  - `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` — the factory pattern and its integration-test authentication switch
  - `docs/engineering.md` § Required evidence tiers, tier 5 — "authentication, antiforgery, validation, scope, idempotency, exception translation, and action-history actor are observable"
- Binding decisions:
  - L-01 — commands live in `Pegasus.Web` route groups; the tests enumerate that host's endpoints, not a list maintained by hand in a second place.
- Depends on: `DSK-08-01` — the contract test project, its factory and its trait convention.

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (`dotnet/skills` `98f84851`, plugin `dotnet-test`) → `test-gap-analysis` (same pin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-02` and § 4, and `docs/desktop/03-gateway-api-and-data/README.md` § 5 rows `DSK-03-03` and `DSK-03-08`. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Load `pegasus-desktop`, then `code-testing-agent`. Add `tests/Pegasus.Api.ContractTests/CommandCoverage/CommandEndpointCatalogue.cs` that resolves `EndpointDataSource` from the factory's service provider and returns every endpoint whose HTTP method is POST, PUT, PATCH or DELETE under the `/api/v1` prefix, with its route pattern, method and the `StaffAccessRight` its endpoint filter requires ([[DSK-03-03]]).
3. Add `CommandCoverageTable.cs` — one explicit row per command: route pattern, method, required right, a minimal valid request body, an invalid request body, and whether the command carries a version token and an operation key. Keep it a literal table in code; do not generate request bodies by reflection, because a wrong shape must be a visible edit, not a silent guess.
4. Add `CommandCoverageGuardTests.cs`: a fact that fails listing any catalogue endpoint with no table row, and any table row with no catalogue endpoint. Done when adding a new command endpoint in `Pegasus.Web` and running this project turns exactly this test red with the route pattern in the message.
5. Add `UnauthenticatedCommandTests.cs` — one theory over the table: call the command with no `Authorization` header; expect `401` and no `WWW-Authenticate` scheme other than `Bearer`.
6. Add `WrongRightCommandTests.cs` — one theory over the table: issue a staff token whose claims omit the required `StaffAccessRight`; expect `403` and a problem body, and assert the command's effect did not occur (no action-history row, no state change).
7. Add `StaleVersionCommandTests.cs` — one theory over the rows that carry a version token: send a version older than the current one; expect `409` with the concurrency problem type from `Pegasus.Contracts` and the current version echoed in the response.
8. Add `InvalidRequestCommandTests.cs` — one theory over the table: send the invalid body; expect `400` with a `ProblemDetails` payload whose `type` and `title` match the mapping ported from `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs`, and assert the response is not a bare framework validation dump.
9. Add `IdempotentReplayCommandTests.cs` — one theory over the rows that carry an operation key: send the same command twice with the same key; expect the same status and body on the replay, exactly one effect, and one action-history entry.
10. Load `test-gap-analysis` and run its pseudo-mutation pass over the five theory classes; close every gap it names that is inside this ticket's scope, and record the ones that are not (they belong to [[DSK-08-11]]).
11. Run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`. Done when the five theories run once per applicable table row and the guard test is green.
12. Record in `docs/desktop/08-testing/README.md` § 4 that the template exists and that every area 03 command ticket must add its rows, and run the simplification pass over the branch diff before the PR.

## Acceptance criteria

- [ ] One theory case per command endpoint for each of: unauthenticated 401, wrong right 403, stale version 409, bad input 400 problem, replayed operation key idempotent.
- [ ] The guard test fails when a command endpoint has no coverage row, naming the route pattern.
- [ ] Wrong-right and stale-version cases assert the absence of the effect, not only the status code.
- [ ] No rule is re-implemented in test code; expectations come from the endpoint and the Core use case it reaches.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: `Passed!`, theory counts equal to the number of applicable table rows.
- [ ] Add a throwaway `POST /api/v1/__probe` command endpoint and rerun — expected: `CommandCoverageGuardTests` fails with `/api/v1/__probe` in the message; remove it and confirm green.

## Evidence tier

Tier 5 — Web/API/MCP caller. It obliges that authorization, validation, idempotency and exception translation are observed on the real route through a real host, with the persisted effect (or its absence) checked, not just the status code.

## Documentation changes

- `docs/desktop/08-testing/README.md` § 4 — record that the command coverage template exists and that area 03 command tickets add their rows to it.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create and edit `tests/Pegasus.Api.ContractTests/**` and the one plan document line. Must not change endpoint behaviour in `src/Pegasus.Web` — a failing expectation is a finding for `pegasus-gateway-dev`, not a test edit.
- **Traps**: *two policy engines* — contract tests must assert that the API and the MCP tools reach the same Core use cases through shared fixtures, never re-implement the rule in test code. New tests must carry the `Contract` trait. xunit 2.9.3 with hand-rolled fakes only.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
