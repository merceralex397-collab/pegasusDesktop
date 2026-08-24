# File map — PLAT-015

## Direct change surface

- `docs/current-architecture.md` — the health surface as a retained fact.
- `docs/capabilities.md` — a `DSK` row for the administrator health view (after `DSK-00-08` creates the family).
- `docs/runbook.md` — how support reads the health view during an incident.

## Context files

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

## Ripple effects

- [ ] Every §18.3 dependency has a row with a closed-enum state and its own `ObtainedAtUtc`; the Worker row covers each function separately with its last successful cycle.
- [ ] The endpoint is administrator-only; non-administrator, automation and unauthenticated calls are refused with the documented shapes and audited.
- [ ] No secret, credential, vault URI, connection string, mailbox address or unredacted path appears in the response — asserted by a test.
- [ ] Each probe is individually bounded and the whole endpoint returns within its budget even when a provider is dead.
- [ ] The desktop Operations view shows every row with state, obtained-at and detail, with AutomationIds and text-plus-colour states.

## Out of scope

- **Azure**: no write. The endpoint reads provider state through the existing adapters; it must not call Azure management APIs.
- **Scope boundary**: may touch `src/Pegasus.Contracts`, the `/api/v1` administration group in `src/Pegasus.Web`, the desktop Operations screen, and the contract/UI test projects. Must not change `/health/live` or `/health/ready` — the platform probes depend on their current shape and short-circuiting. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a health endpoint that issues live third-party calls per request becomes a load amplifier and a rate-limit risk; "describes dependencies, not discloses secrets" is the whole design constraint — detail text is the easiest place to leak a URI; anonymous access would hand an outsider a dependency map; colour-only state fails the accessibility review (`docs/design/README.md`); the update-feed check is SMB under D-003, not an HTTPS probe.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.
