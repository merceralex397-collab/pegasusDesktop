---
id: FEAT-045
type: ticket
title: >-
  DSK-07-19 · Provider error taxonomy in contracts: one list for
  terminal/transient/unknown and the five provider problem types
status: implementing
area: desktop-features
assignee: codex-mcp-client
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:47.154Z'
labels:
  - desktop-conversion
  - plan-07
  - phase-5
  - tier-3
groups:
  - EPIC-008
  - HZN-006
links: []
blocks:
  - GWY-014
docs_todo: true
archived: false
created: '2026-08-24T08:31:27.017Z'
updated: '2026-08-27T18:19:24.430Z'
---

## What

Put the provider error vocabulary in exactly one place. Add the disposition triple `terminal` / `transient` / `unknown` and the five provider problem types — `not-found`, `invalid-request`, `not-authorized`, `rate-limited`, `unavailable` — to `src/Pegasus.Contracts` as a single catalogue, apply it to every integration endpoint in this area, and make the desktop map each value to a distinct state that never relies on colour alone.

## Why

Proposal § 16.2 requires that "not found", "invalid request", "not authorized", "rate limited" and "provider unavailable" be **distinct**, and the repository already enforces the disposition triple: `docs/current-architecture.md:85-90` records that external clients and catch paths distinguish `terminal`, `transient` and `unknown`, that terminal outcomes stop retries, that unknown outcomes remain unknown, and that metrics count successful effects rather than attempts. Without one catalogue, each integration endpoint invents its own strings, the desktop grows a second copy of the vocabulary, and the operator sees "lookup failed" where the system knew "rate limited, retry after 60 seconds". `AGENTS.md` § Simplicity rails is explicit: an exception taxonomy or a state vocabulary lives in exactly one place, and a second copy in another layer is duplication even when it is "just strings". Siblings: [[DSK-07-01]], [[DSK-07-05]], [[DSK-07-09]] and [[DSK-07-11]] all consume this list; [[DSK-07-04]] and [[DSK-07-10]] render it.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-19`
- Plan context: `docs/desktop/07-integrations/README.md` § 1 (the §16.2 row — "provider error taxonomy and retry rules carried into every endpoint"), § 7 Risks and traps ("Poison-queue visibility lost behind a friendly status")
- Existing catalogue to extend, not replace: `docs/desktop/03-gateway-api-and-data/README.md` § 3, row `Problem details` — RFC 9457 via `AddProblemDetails`, stable `type` URIs `urn:pegasus:problem:<slug>` with `validation`, `not-authorized`, `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`, `client-unsupported`, `password-change-required`, `account-disabled`, `provider-unavailable`, `not-found`, `rate-limited`, `maintenance`; `correlationId` always present and no payload dumps
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 16.2 External provider resilience, § 16.1 Operation model, § 14.8 Notifications and errors
- Repository evidence: `docs/current-architecture.md:85-90` (the `terminal` / `transient` / `unknown` rule and the metrics rule); `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:18-70` (the existing translation of Core refusals into content-safe errors — the pattern the `/api/v1` mapping is ported from by [[DSK-03-02]]); `src/Pegasus.Core/Vehicle/LookupContracts.cs:3-12` (`VehicleLookupOutcome` — `Current`, `Stale`, `Partial`, `NotFound`, `Throttled`, `Unavailable`, `Failed`), `:52-56` (`VehicleLookupFailure(Code, Retryable, RetryAfter)`); `src/Pegasus.Core/Operations/EmailOperations.cs:12-18` (`EmailOperationState` including `Unknown`); `src/Pegasus.Core/Intake/RetainedMail.cs:344-364` (`MailFreshnessState`, `MailPollHealth.LastFailureCode`); `src/Pegasus.Core/Custody/CustodyContracts.cs:297-310` (`RetryCaseCustodyOutcome`)
- Binding decisions: L-01 — the catalogue ships in `Pegasus.Contracts` beside the gateway, consumed by both the Web host and the desktop. ADR-0107 — a problem detail names a provider state, never a provider credential, a raw provider payload or a URL.
- Depends on: `DSK-03-01` the `Pegasus.Contracts` project and its problem-type catalogue; `DSK-03-02` the `/api/v1` problem-details mapping; `DSK-03-04` the committed OpenAPI snapshot the new types must appear in

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, plugin `dotnet-aspnetcore`) → `test-gap-analysis` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `assertion-quality` → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for RFC 9457 problem-details extension members and `Retry-After` semantics in ASP.NET Core)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the `Problem details` row of `docs/desktop/03-gateway-api-and-data/README.md` § 3, `docs/current-architecture.md:85-90`, and proposal § 16.2. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-19-provider-error-taxonomy`.
2. Inventory what already exists before adding anything. Tabulate in `research` every Core enum that already expresses a provider outcome — `VehicleLookupOutcome` (seven values), `VehicleLookupFailure.Code`/`Retryable`/`RetryAfter`, `EmailOperationState` (five values), `MailFreshnessState` (three), `RetryCaseCustodyOutcome` (five), plus the `terminal`/`transient`/`unknown` disposition rule — and record which existing `urn:pegasus:problem:` slug each maps to. The catalogue is a projection of these, not a new vocabulary.
3. Add `ProviderProblemTypes` to `src/Pegasus.Contracts` as one static class of stable slugs: `not-found`, `invalid-request`, `not-authorized`, `rate-limited`, `provider-unavailable`. Reuse the slugs already in the § 3 catalogue where they exist (`not-found`, `not-authorized`, `rate-limited`, `provider-unavailable`) and add only `invalid-request`. Do not introduce a near-duplicate of an existing slug.
4. Add `ProviderDisposition` to the same project as a three-valued type — `terminal`, `transient`, `unknown` — with a documented rule per value copied from `docs/current-architecture.md:85-90`: terminal stops retries, transient may be retried under the endpoint's bounded policy, unknown **remains unknown** and is never reported as success or as failure.
5. Define the problem-detail extension members once: `disposition` (the triple), `providerCode` (the provider's own stable code where one exists), `retryable` (bool) and `retryAfterSeconds` (optional). Every provider-touching endpoint emits the same members with the same names — a per-endpoint variation is the defect this ticket exists to prevent.
6. Apply the catalogue to the integration endpoints already built: the intake-status and external-work reads ([[DSK-07-01]]), the retry commands ([[DSK-07-02]]), the mail endpoints ([[DSK-07-03]]), the Box broker ([[DSK-07-05]]), the vehicle endpoints ([[DSK-07-09]]) and the outbound command ([[DSK-07-11]]). Map each Core outcome to exactly one slug plus one disposition, and record the mapping table in the contracts project's own documentation — the table is the single list.
7. Preserve fidelity at the boundary. `NotFound` and `Unavailable` must never share a slug; `Throttled` maps to `rate-limited` and carries `retryAfterSeconds` when the provider supplied one; `Failed` with no further information maps to `provider-unavailable` with `disposition: unknown`, not to `not-found`. Add one test per pair that could plausibly be conflated.
8. Set the HTTP status per slug consistently: `not-found` → 404, `invalid-request` → 400, `not-authorized` → 403, `rate-limited` → 429 with a `Retry-After` header, `provider-unavailable` → 503. Assert the status/slug pairing in the contract tests so a future endpoint cannot pick a different status for the same meaning.
9. Keep bodies content-safe, following `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs`: no infrastructure detail, no raw provider payload, no credential, no stack. `correlationId` is always present. Add an assertion that no problem body contains `x-api-key`, `Authorization`, `client_secret`, `box.com` or a bearer token.
10. Regenerate and commit the OpenAPI snapshot `openapi/pegasus-v1.json` so the new problem schemas and extension members appear, and confirm the snapshot test from [[DSK-03-04]] fails on an unreviewed contract change and passes on the reviewed one.
11. Map each slug to a desktop state in `src/Pegasus.Desktop.Infrastructure` — one mapping, consumed by every screen — and add view-model tests asserting that each slug produces a distinct operator sentence with a copyable Reference, that `unknown` never renders as success, and that no state is conveyed by colour alone (`docs/desktop/06-ui-design/keyboard-and-accessibility.md`).
12. Run `test-gap-analysis` over the integration endpoints and record any endpoint that still emits an uncatalogued error string; fix it here or file it as a ticket. Add an architecture test forbidding a second provider-error enum outside `Pegasus.Contracts`.
13. Update `docs/desktop/03-gateway-api-and-data/README.md` § 3 so the `Problem details` row names the new extension members and `invalid-request`. Then run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, and open the PR into `dev`.

## Acceptance criteria

- [ ] One catalogue in `src/Pegasus.Contracts` holds the disposition triple, the five provider slugs and the four extension member names; no second copy exists in Web or desktop.
- [ ] Every integration endpoint in area 07 emits the same member names with the same meanings, and each Core outcome maps to exactly one slug plus one disposition.
- [ ] `not-found` and `provider-unavailable` are never conflated; `rate-limited` carries `retryAfterSeconds` and a `Retry-After` header when known.
- [ ] `unknown` is never rendered as success or as a definite failure, on the wire or on screen.
- [ ] No problem body carries a credential, a raw provider payload or infrastructure detail; `correlationId` is always present.
- [ ] The committed OpenAPI snapshot contains the new problem schemas, and the snapshot test fails on an unreviewed change.
- [ ] The desktop maps each slug to a distinct state with text as well as colour.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: the status/slug pairing, conflation-prevention, content-safety and snapshot facts pass.
- [ ] `git diff --exit-code openapi/pegasus-v1.json` after regeneration — expected: clean, proving the committed snapshot matches the generated document.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` — expected: one distinct state per slug, `unknown` never success, no colour-only state.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` — expected: the no-second-provider-error-enum fact passes.

## Evidence tier

Tier 3 — Parser/adapter contracts.
Tier 3 obliges adapter-contract evidence: **stable contract codes** and deterministic external failures — exactly what this catalogue fixes, proven per endpoint rather than asserted once.

## Documentation changes

- `docs/desktop/03-gateway-api-and-data/README.md` § 3 — the `Problem details` row gains `invalid-request` and the four extension members
- `openapi/pegasus-v1.json` — regenerated snapshot with the new problem schemas
- `docs/frd/frd-12-operator-experience.md` or `docs/frd/frd-13-desktop-operator-experience.md` — the operator-visible provider-state vocabulary

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Contracts`, the `/api/v1` groups in `src/Pegasus.Web`, `src/Pegasus.Desktop.Infrastructure`, `openapi/`, and the contract, view-model and architecture test projects. Must not change Core enums or Core refusal semantics — this ticket projects them, it does not redefine them.
- **Traps**: a second copy of the taxonomy in the desktop is duplication even as "just strings" (`AGENTS.md` § Simplicity rails); `unknown` collapsing into success is the failure mode `docs/current-architecture.md:85-90` was written to prevent, and it is how poison-queue visibility gets lost behind a friendly status; ADR-0107 — a problem body must never carry a provider credential, key or raw payload; metrics count successful effects, not attempts, so do not add attempt-counting telemetry here; the OpenAPI snapshot is reviewed evidence, not a build artefact to regenerate silently.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
