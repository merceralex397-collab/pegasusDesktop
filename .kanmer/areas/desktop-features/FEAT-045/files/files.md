# Files — FEAT-045

Surveyed on 2026-08-24 at fork `main`. Paths that do not exist today carry the named ticket that
creates them; every other path was confirmed with `ls`, `wc -l`, `sed` or `grep`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/Providers/ProviderProblemTypes.cs` | **New type in an assembly created by [[GWY-001]] (plan handle `DSK-03-01`).** One static class of stable slugs: `not-found`, `invalid-request`, `not-authorized`, `rate-limited`, `provider-unavailable`. Four already exist in the § 3 catalogue (`docs/desktop/03-gateway-api-and-data/README.md:167`) and are **reused, not redefined**; only `invalid-request` is added. Breaks if a near-duplicate slug is introduced beside an existing one. |
| `src/Pegasus.Contracts/Providers/ProviderDisposition.cs` | **New.** The three-valued disposition — `terminal`, `transient`, `unknown` — with the rule per value copied from `docs/current-architecture.md:85-90`. A **second axis** from the slug, deliberately: three Core enums already carry three differently-named "uncertain" members, and folding disposition into the slug would lose those distinctions. |
| `src/Pegasus.Contracts/Providers/ProviderProblemExtensions.cs` | **New.** The four problem-detail extension member names defined once: `disposition`, `providerCode`, `retryable`, `retryAfterSeconds`. Named to match the Core fields they project — `VehicleLookupFailure(Code, Retryable, RetryAfter)` at `src/Pegasus.Core/Vehicle/LookupContracts.cs:53-56`. A per-endpoint variation in these names is the defect the ticket exists to prevent. |
| — the Core-outcome → slug + disposition mapping table | **XML doc comments on the catalogue type**, *not* a new `.md`. `scripts/Test-MarkdownPlacement.ps1:29-32` allows markdown only under `docs/(prd\|frd\|adr\|design\|desktop)` and five other roots, and inspects added/copied/renamed files — so a new `.md` beside the `.cs` fails the CI `documentation` job. Doc comments also travel into the generated OpenAPI descriptions, which is where a reader of the contract will look. |
| `src/Pegasus.Web` — the `/api/v1` integration route groups | **Edit** (groups created by [[GWY-002]] (plan handle `DSK-03-02`)). Emit the catalogue from the endpoints of [[FEAT-027]] (`DSK-07-01`), [[FEAT-028]] (`DSK-07-02`), [[FEAT-029]] (`DSK-07-03`), [[FEAT-031]] (`DSK-07-05`), [[FEAT-035]] (`DSK-07-09`) and [[FEAT-037]] (`DSK-07-11`), with the status-per-slug pairing of body step 8. |
| `src/Pegasus.Desktop.Infrastructure` — slug-to-state mapping | **Edit** (project created by [[FND-031]] (plan handle `DSK-02-06`)). **One** mapping, consumed by every screen — including [[FEAT-030]] (`DSK-07-04`) and [[FEAT-036]] (`DSK-07-10`). A per-screen `switch` is duplication even as "just strings". |
| `openapi/pegasus-v1.json` | **Regenerated and committed** so the new problem schemas and the four extension members appear. Owned by [[GWY-004]] (plan handle `DSK-03-04`); it is **reviewed evidence, not a build artefact** (Guardrails). |
| `docs/desktop/03-gateway-api-and-data/README.md` | **Edit** the `Problem details` row at `:167` — add `invalid-request` to the slug list and name the four extension members. 305 lines today. |
| `docs/frd/frd-12-operator-experience.md` | **Edit** — the operator-visible provider-state vocabulary. 131 lines. The body offers frd-12 "or" `frd-13-desktop-operator-experience.md`; **frd-13 does not exist** (`ls docs/frd/`), so the "or" resolves to frd-12 unless a named sibling authors frd-13 first. |
| `tests/Pegasus.Api.ContractTests/Providers/ProviderErrorTaxonomyTests.cs` | **New** (project created by [[TEST-001]] (plan handle `DSK-08-01`) and [[GWY-004]]). Status/slug pairing, the conflation-prevention pairs, content-safety, and the snapshot fact. |
| `tests/Pegasus.Desktop.ViewModelTests/Providers/ProviderStateTests.cs` | **New** (project created by [[TEST-004]] (plan handle `DSK-08-04`) and [[FND-038]] (plan handle `DSK-02-13`)). One distinct operator sentence per slug, `unknown` never success, no colour-only state. |
| `tests/Pegasus.ArchitectureTests` | **Edit** (exists; extended for desktop boundaries by [[FND-037]] (plan handle `DSK-02-12`)). The fact that no second provider-error enum exists outside `Pegasus.Contracts`. |

## Context files

What the implementer must **read** first, and the specific constraint each one holds.

| Path | What it tells the implementer |
| --- | --- |
| `docs/current-architecture.md:85-90` | The disposition rule as a repository **invariant**, in the repository's own words: "External clients and catch paths distinguish `terminal`, `transient`, and `unknown`; terminal outcomes stop retries, **unknown outcomes remain unknown**, and metrics count successful effects rather than attempts." Copy the per-value rule from here rather than paraphrasing. The same bullet list also forbids horizontal `Common`/`Helpers`/`Utilities` packages and `V2`/`New`/`Manager` names — so the catalogue must be a named domain concept in a `Providers` namespace, not a utility bag. The metrics clause is why the Guardrails forbid adding attempt-counting telemetry here. |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | The catalogue being **extended**, not created: RFC 9457 via `AddProblemDetails`, `urn:pegasus:problem:<slug>`, and thirteen slugs of which four are already the ones this ticket needs. Also fixes two body rules — "Body never carries payload dumps; `correlationId` always present" — and identifies itself as a "Port of `AutomationMcpErrors.cs`". **Read the thirteen before adding anything**: adding `not-found-provider` beside the existing `not-found` would be the exact failure the ticket forbids. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` | The behavioural precedent, and the content-safety standard to match. `:7-16` states the rule: safe domain messages pass through, "anything unexpected collapses to a generic failure so no infrastructure detail crosses the boundary", and refusals name the guard and the current case version but never "a token or other holder material". `:30-53` shows the four explicitly mapped exceptions; `:54-60` the three passed through; `:62-66` the catch-all. **Match this vocabulary; do not invent new codes.** |
| `src/Pegasus.Core/Vehicle/LookupContracts.cs` | The richest source enum and the origin of three of the four extension members. `:3-12` `VehicleLookupOutcome` — **seven** values: `Current`, `Stale`, `Partial`, `NotFound`, `Throttled`, `Unavailable`, `Failed`. **The trap lives here**: `Current`, `Stale` and `Partial` are *successful* reads with caveats and must map to **no** slug — `Stale` looks like a degraded state and is not one. `NotFound` and `Unavailable` are distinct members and must never share a slug. `:53-56` `VehicleLookupFailure(string Code, bool Retryable, TimeSpan? RetryAfter)` — `RetryAfter` is **nullable**, so `retryAfterSeconds` is optional and a retry window is never fabricated. |
| `src/Pegasus.Core/Operations/EmailOperations.cs:12-18` | `EmailOperationState` — **four** values: `Pending`, `Succeeded`, `Failed`, `Unknown`. **The ticket body's step 2 says five; the measured count is four**, and [[FEAT-037]] (plan handle `DSK-07-11`)'s plan step 4 independently recorded "Core has four states, not five". `Unknown` here is the canonical "we do not know" and is the reason disposition is a separate axis. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:344-365` | `MailFreshnessState` at `:344-349` — **three** values: `Current`, `Stale`, `Unavailable`. `MailPollHealth` at `:361-365` carries `LastFailureCode`. **The summary at `:355-359` draws the line to respect**: "Raw facts only: turning them into a freshness state is **policy** and belongs to `GetRetainedMailFreshness`." The catalogue projects that policy's output; it must not start classifying raw failure codes itself. |
| `src/Pegasus.Core/Custody/CustodyContracts.cs:297-305` | `RetryCaseCustodyOutcome` — **five** values: `Pending`, `Replay`, `Conflict`, `Refused`, `NotFound`. `Replay` is a *success* (the idempotent re-run) and maps to no slug; `Conflict` and `Refused` are refusals with existing slugs (`operation-conflict`, `not-authorized` or `validation`) rather than provider problems — a provider slug here would misattribute a Pegasus refusal to the provider. |
| `docs/desktop/06-ui-design/keyboard-and-accessibility.md` | 155 lines. `:82` — "No information by colour alone: every chip carries text and glyph". `:155` makes it a reviewer check: "one vocabulary, one primary action, no colour-only state) in the ticket plan". This is why step 11's view-model tests assert a distinct **sentence** per slug, not a distinct colour. |
| `scripts/Test-MarkdownPlacement.ps1` | `:29-32` holds the allow-list regex `^((docs/(prd\|frd\|adr\|design\|desktop))\|workspaces/document-extraction\|\.agents/skills\|\.design-sync\|\.grok\|\.stitch\|design/planning-and-old-designs)/.+\.md$`, and `:59-61` shows it inspects only added, copied and renamed files. **Tells the implementer that the body's "the contracts project's own documentation" must mean XML doc comments, not a new `.md`.** Its `-Base` and `-Head` parameters are `[Parameter(Mandatory)]`; CI's `documentation` job runs the regression suite `./scripts/Test-TestMarkdownPlacement.ps1` (`.github/workflows/ci.yml:84`) rather than the validator directly. |
| `docs/desktop/07-integrations/README.md` § 7 | The trap row this ticket most directly answers: "Poison-queue visibility lost behind a friendly status — Operations surface shows poison counts and last failure code; **never collapses `unknown` into success**." § 1's §16.2 row is the requirement: "Provider error taxonomy and retry rules carried into every endpoint." |
| `docs/engineering.md:72-88` | Tier 3 — "Parser/adapter contracts … **stable contract codes**, and deterministic external failures". The catalogue *is* the stable contract codes, and tier 3 obliges proving it per endpoint rather than asserting it once. `:201-207` § Plan sizing requires the diff estimate first and a facts/assumptions split. |
| `AGENTS.md` § Simplicity rails | "One list per concept" — an exception taxonomy or state vocabulary lives in exactly one place, and a second copy in another layer is duplication **even when it is "just strings"**. This is the sentence the whole ticket implements, and the architecture test in step 12 is what makes it enforceable rather than aspirational. |

## Ripple effects

- **Six endpoint groups adopt the catalogue** — [[FEAT-027]], [[FEAT-028]], [[FEAT-029]],
  [[FEAT-031]], [[FEAT-035]], [[FEAT-037]] — and **two desktop screens render it** — [[FEAT-030]],
  [[FEAT-036]]. Each is a separate ticket; this one supplies the list and applies it to what has
  already been built, and any endpoint landing afterwards consumes it rather than re-deciding.
- **This ticket blocks [[GWY-014]]** (plan handle `DSK-03-14`, the vehicle-lookup and assessment
  endpoint group) — recorded on the ticket itself as `"blocks": ["GWY-014"]`. Landing the catalogue
  first is materially cheaper than retrofitting it across endpoints that already shipped their own
  strings.
- **OpenAPI and the generated client.** New problem schemas and extension members change
  `openapi/pegasus-v1.json`, which [[GWY-004]] owns and whose snapshot test must fail on an
  unreviewed change; the Kiota client from [[GWY-005]] (plan handle `DSK-03-05`) regenerates with
  it. On this board a contract change ripples into both, and both must land in the same PR.
- **Documentation.** The `Problem details` row at `docs/desktop/03-gateway-api-and-data/README.md:167`
  and the operator vocabulary in `docs/frd/frd-12-operator-experience.md`.
- **Architecture test.** A new fact forbidding a second provider-error enum outside
  `Pegasus.Contracts` — which will constrain every future integration ticket, deliberately.
- **No Core change, and no Worker change.** Guardrails: "Must not change Core enums or Core refusal
  semantics — this ticket projects them, it does not redefine them." The unattended paths that
  produce these outcomes are untouched.
- **No migration.** No table, no runtime-role `Grant*` migration, no
  `scripts/Test-MigrationGrants.ps1` involvement.

## Out of scope

Recorded because the ticket's Guardrails already forbid each one.

- **Changing any Core enum or refusal semantics.** `VehicleLookupOutcome`, `EmailOperationState`,
  `MailFreshnessState` and `RetryCaseCustodyOutcome` are read and projected, never edited. Adding a
  value to one of them to make the mapping tidier would be redefining the domain to suit the wire.
- **A second copy of the taxonomy in the desktop.** `AGENTS.md` § Simplicity rails — duplication
  even as "just strings". One mapping in `Pegasus.Desktop.Infrastructure`, consumed by every screen.
- **Inventing a slug locally.** The list at `docs/desktop/03-gateway-api-and-data/README.md:167` is
  [[GWY-001]]'s single surface; this ticket adds exactly one agreed value (`invalid-request`) and
  coordinates anything further rather than adding it.
- **Attempt-counting telemetry.** `docs/current-architecture.md:85-90` — "metrics count successful
  effects rather than attempts". Named in the Guardrails.
- **Any credential, raw provider payload or URL in a problem body.** ADR-0107, and step 9's
  assertion makes it testable.
- **Collapsing `unknown` into success.** The failure mode the invariant was written to prevent, and
  how poison-queue visibility gets lost behind a friendly status
  (`docs/desktop/07-integrations/README.md` § 7).
- **Regenerating the OpenAPI snapshot silently.** It is reviewed evidence; the snapshot test must be
  shown to fail on an unreviewed change.
- **Creating `src/Pegasus.Contracts`, the `/api/v1` groups, the contract-test project, the desktop
  infrastructure project or the desktop test project.** Created by [[GWY-001]], [[GWY-002]],
  [[TEST-001]] / [[GWY-004]], [[FND-031]] and [[TEST-004]] / [[FND-038]] respectively.
- **Any Azure write.** Guardrail: "Azure: no write."
