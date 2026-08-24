# Plan — PLAT-015

## Objective

Add an authenticated `GET /api/v1/admin/health` that describes gateway, database, Worker last successful cycle per function, Box, DVLA/DVSA, update-feed reachability and the current minimum client version — each with a state and an "obtained at" — and surface it in the desktop Operations/Settings screen. No secrets, no connection strings, no endpoint URLs that disclose credentials.

## Chosen approach

Proposal §18.3 `:1229-1241` requires simple authenticated health information that "describes dependencies, not discloses secrets", and Phase 8 ("administration and hardening") includes integration health. The existing `/health/live` and `/health/ready` endpoints are anonymous liveness probes for the platform (`src/Pegasus.Web/Program.cs:939-950`) and deliberately say nothing useful about Box, Graph or the providers; `/diagnostics/version` returns only build identity (`:954`). With the App Insights blind window (PLAT-034) an administrator has no way to answer "is Box down or is it us". Operator-visible consequence: every integration question becomes a developer investigation. Siblings: [[DSK-10-14]] (telemetry), [[DSK-10-09]] (bundle), [[DSK-10-17]] (provider states in the UI).

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. Keep `docs_todo: true`; planned desktop governing documents must not be linked until they exist on `origin/dev`.
- Use the ticket's Source of truth and its area plan until a real governing doc can be linked.

## Routing

- **Subagents**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (endpoint); `winui-dev` — `.codex/agents/winui-dev.toml` (desktop view)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, plugin `dotnet-aspnetcore`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`) for the desktop surface
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for ASP.NET Core health-check publishing and for `IHealthCheck` timeout patterns
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

1. Orientation. Read the plan row, proposal `:1229-1241`, and `src/Pegasus.Web/Program.cs:523-524` and `:939-954` so the new endpoint complements the probes rather than replacing them. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-15-admin-health-surface` from `dev`.
3. Define the contract in `src/Pegasus.Contracts` (created by `DSK-02-04`/`DSK-03-01`): `AdminHealthResponse { ObtainedAtUtc, MinimumClientVersion, CurrentGatewayVersion, Dependencies: [ { Name, State, ObtainedAtUtc, Detail, LastSuccessAtUtc? } ] }` where `State` is a closed enum — `healthy`, `degraded`, `unavailable`, `unknown`. Every dependency entry carries its own `ObtainedAtUtc` so a cached answer is visibly cached (proposal §16.2 `:1135`).
4. Enumerate the dependency rows exactly as §18.3 lists them: gateway reachable (trivially true if the response was produced); database reachable (reuse `DatabaseReadinessHealthCheck`); Worker last successful cycle **per function** (from the intake-status data of `DSK-07-01`, covering each `AzureWebJobs.*` function the estate runs); Box connectivity; DVLA state; DVSA state; update-feed reachability; current minimum client version.
5. Implement the endpoint in the `/api/v1` administration route group behind the `Administrator` policy and the `StaffAccessRight` filter from `DSK-03-03`. Return `200` with the payload for an administrator, the standard `not-authorized` problem for a non-administrator, `401` when unauthenticated. Never `AllowAnonymous`.
6. Bound every probe: each dependency check runs with its own short timeout and returns `unknown` with the timeout recorded rather than hanging the response; the whole endpoint returns within a fixed budget. Use `microsoft_docs_search` for the health-check timeout pattern rather than inventing one. A health endpoint that blocks on a dead provider is itself an outage.
7. Make provider checks cheap and safe: prefer a cached last-known outcome from the provider adapters over issuing a live third-party call on every request, and state the cache age in `ObtainedAtUtc`. A live call per request would multiply provider load and could trip rate limits (proposal §16.2).
8. Implement update-feed reachability as a test of the D-003 UNC path: can the gateway (or the desktop, whichever the plan for `DSK-09-10` established) stat the `.appinstaller` on the share, and what version does it advertise. Report the path in a redacted form — the share name, not credentials.
9. Add the secrets test: a contract test that serializes the response for a fully populated fixture and asserts it contains no connection string, no `vault.azure.net` URI, no client id or secret, no mailbox address and no file path beyond the redacted feed name. Reuse the pattern list from [[DSK-10-01]].
10. Add authorization tests: administrator → 200; staff without the administration right → the documented problem; automation token → refused; unauthenticated → 401. Each refusal writes the expected security/action-history record.
11. Build the desktop surface on the Operations/Settings screen (`DSK-05-20`): one row per dependency showing name, state, `ObtainedAtUtc` and detail, with `AutomationProperties.AutomationId` on every row so `winapp ui` can assert it. Follow `docs/design/README.md` operator-copy rules — state words are operator vocabulary ("Unavailable", "Last checked 12:04"), not protocol jargon. Show state with text plus colour, never colour alone.
12. Write the `winapp ui` script that opens the screen, waits for the rows and asserts every dependency has a state and an obtained-at value; file it with the UI suite from `DSK-08-06`.
13. Run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` and the UI script against the Test/UAT stack. Both green.
14. Update `docs/current-architecture.md` with the health surface as a retained fact and add a `DSK` capability row for the admin health view once the `DSK` family exists (`DSK-00-08`).
15. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --filter "FullyQualifiedName~AdminHealth"` — expected: contract, authorization and no-secrets facts pass.
- [ ] `winapp ui` health script against the Test/UAT stack — expected: every dependency row present with a non-empty state and obtained-at.
- [ ] Manual check with a provider stopped on the local stack — expected: that row reports `unavailable` or `unknown` within the endpoint budget and the rest of the response is still returned.

## Risks and constraints

- **Azure**: no write. The endpoint reads provider state through the existing adapters; it must not call Azure management APIs.
- **Scope boundary**: may touch `src/Pegasus.Contracts`, the `/api/v1` administration group in `src/Pegasus.Web`, the desktop Operations screen, and the contract/UI test projects. Must not change `/health/live` or `/health/ready` — the platform probes depend on their current shape and short-circuiting. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a health endpoint that issues live third-party calls per request becomes a load amplifier and a rate-limit risk; "describes dependencies, not discloses secrets" is the whole design constraint — detail text is the easiest place to leak a URI; anonymous access would hand an outsider a dependency map; colour-only state fails the accessibility review (`docs/design/README.md`); the update-feed check is SMB under D-003, not an HTTPS probe.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Simplification pass

Before the PR, independently assess the branch diff for unnecessary abstractions, duplicated policy, or scope expansion and record the disposition here.
