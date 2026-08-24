# Research — PLAT-015

## Question

Add an authenticated `GET /api/v1/admin/health` that describes gateway, database, Worker last successful cycle per function, Box, DVLA/DVSA, update-feed reachability and the current minimum client version — each with a state and an "obtained at" — and surface it in the desktop Operations/Settings screen. No secrets, no connection strings, no endpoint URLs that disclose credentials.

## Findings

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-15`
- Plan detail: same file § 1 (§18.3 Health), § 4 (target state — "an administrator health view describes dependencies without secrets")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 18.3 Health `:1229-1241`; § 16.2 External provider resilience `:1128-1136`; § 13.10 Administration and operations `:870-879`
- Repository evidence:
  - `src/Pegasus.Web/Program.cs:523-524` — `AddHealthChecks().AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"])`, the existing check to reuse
  - `src/Pegasus.Web/Program.cs:939-950` — `/health/live` (predicate false) and `/health/ready` (tag `ready`), both `AllowAnonymous().ShortCircuit()` — do not change these
  - `src/Pegasus.Web/Program.cs:954` — `/diagnostics/version` returning `version` and `sourceSha`
  - `src/Pegasus.Web/Program.cs:517-522` — the `Administrator` policy (`policy.RequireRole(StaffRoleNames.Administrator)`) this endpoint uses
  - `src/Pegasus.Core/Identity/StaffAuthorization.cs:1-21` — `StaffAccessRight.ManageWorkflowConfiguration` and the administration rights set
  - `docs/operations.md:784-802` — how secrets are referenced, so the endpoint knows what must never be echoed (Key Vault URIs, connection strings, client ids)
  - New: the administration endpoints from `DSK-03-15`; intake-status endpoints from `DSK-07-01`; the minimum-version setting from `DSK-04-06`; the desktop Operations screen from `DSK-05-20`
- Binding decisions:
  - **L-01** — the endpoint lives in `Pegasus.Web` beside the Razor Pages; no new deployment unit.
  - **ADR-0107** (to be authored) — Box and DVLA/DVSA credentials stay behind the gateway; the desktop learns provider state only through this endpoint.
  - **D-003** — the update feed is a UNC share; "update-feed reachability" is an SMB path check, not an HTTPS probe.
- Depends on: `DSK-03-15` (administration endpoints and their authorization filter), `DSK-07-01` (gateway intake-status endpoints: per-mailbox last cycle, failures, poison counts).

## Implications

Proposal §18.3 `:1229-1241` requires simple authenticated health information that "describes dependencies, not discloses secrets", and Phase 8 ("administration and hardening") includes integration health. The existing `/health/live` and `/health/ready` endpoints are anonymous liveness probes for the platform (`src/Pegasus.Web/Program.cs:939-950`) and deliberately say nothing useful about Box, Graph or the providers; `/diagnostics/version` returns only build identity (`:954`). With the App Insights blind window (PLAT-034) an administrator has no way to answer "is Box down or is it us". Operator-visible consequence: every integration question becomes a developer investigation. Siblings: [[DSK-10-14]] (telemetry), [[DSK-10-09]] (bundle), [[DSK-10-17]] (provider states in the UI).

## Constraints

- **Azure**: no write. The endpoint reads provider state through the existing adapters; it must not call Azure management APIs.
- **Scope boundary**: may touch `src/Pegasus.Contracts`, the `/api/v1` administration group in `src/Pegasus.Web`, the desktop Operations screen, and the contract/UI test projects. Must not change `/health/live` or `/health/ready` — the platform probes depend on their current shape and short-circuiting. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a health endpoint that issues live third-party calls per request becomes a load amplifier and a rate-limit risk; "describes dependencies, not discloses secrets" is the whole design constraint — detail text is the easiest place to leak a URI; anonymous access would hand an outsider a dependency map; colour-only state fails the accessibility review (`docs/design/README.md`); the update-feed check is SMB under D-003, not an HTTPS probe.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Conclusion

The ticket's cited evidence is sufficient to plan the bounded change. No planned canonical document is linked or claimed to exist.
