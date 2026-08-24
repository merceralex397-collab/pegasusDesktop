# Files — GWY-003

Surveyed 2026-08-24 against fork `main`. Existing paths were confirmed with `ls`/`grep`; paths created
by another ticket name it.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Web/Api/StaffActorAccessor.cs` | **New.** A scoped service that calls `StaffActorFactory.TryCreate(User.FindFirstValue(ClaimTypes.NameIdentifier), User.FindAll(ClaimTypes.Role).Select(c => c.Value), out actor)` — the same factory `src/Pegasus.Web/Pages/StaffPageModel.cs:12-15` calls, never a second parser. It also carries step 4's non-staff rejection (Automation audience `pegasus-automation-mcp`, or any resolved `ActorKind` other than `Staff`) and writes the `ISecurityEventWriter` denial. Make it `internal sealed`, matching `AutomationActorResolver` (`src/Pegasus.Web/Mcp/AutomationActorResolver.cs:20`). **It must live under `Api/`, not under `Pages/`** — see the `WebPagesHaveOneStaffActorAndOperationKeyOwnerPerConcept` row below. |
| `src/Pegasus.Web/Api/RequireStaffRightFilter.cs` | **New.** An `IEndpointFilter` parameterised by a `StaffAccessRight`, resolving through the accessor and calling `Pegasus.Core.Identity.StaffAuthorization.IsAuthorized(actor, right)`. Refusal throws `StaffAuthorizationException` so [[GWY-002]]'s (plan handle `DSK-03-02`) handler produces the single 403 `not-authorized` problem — one translation point, not two. Also the `RouteGroupBuilder.RequireStaffRight(StaffAccessRight)` extension so each later group declares its right in one line. Its XML doc carries step 6's statement that this is a fail-fast boundary and that Core still authorizes every use case. |
| `src/Pegasus.Web/Api/DesktopGateway.cs` (created by [[GWY-002]]) | **Extend by one constant.** The `HttpContext.Items` key under which the filter stashes the resolved `ActionActor` for handlers (step 7). It belongs in the fixed-names class beside `FeatureFlag` and `BasePath`, exactly as `AutomationMcp.cs` collects every fixed name in one place. Change no existing member. |
| `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs` (created by [[GWY-002]]) | **Extend `AddPegasusDesktopGateway`** to register the accessor (and `IHttpContextAccessor` if the host does not already provide it). No change to `MapPegasusDesktopGateway`'s signature — later tickets depend on it returning the `RouteGroupBuilder`. |
| `src/Pegasus.Web/Api/DesktopGatewayProblems.cs` (created by [[GWY-002]]) | **Extend with two mappings only** (step 8): whatever [[GWY-021]] (plan handle `DSK-04-04`) signals for a disabled account → `account-disabled`, and for a forced password change → `password-change-required`. Map; do not implement the identity check. Leave every existing branch and the branch order untouched — all four case exceptions derive from `InvalidOperationException`, so reordering breaks them. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayAuthorizationTests.cs` | **New.** Twenty-seven facts: twelve right-positive, twelve right-negative, plus disabled-account, Automation-audience and anonymous. Read the *Ripple effects* note on what "positive" means for `ExecuteSystemWork` and `SubmitRequestUpload` before writing them. `[Trait("Category", "SqlServer")]`, like `LocalIntakeAccessTests.cs:10`. |

## Context files

What the implementer must **read**, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:29-58` | The decisive file. The twelve rights are **four** shapes, not twelve: `AccessStaffApplication` → any `Staff`; `PerformCasework` → any `Staff` **or `Automation`**; eight management rights → `Staff` **and** `IsInRole(StaffRole.Administrator)`; `ExecuteSystemWork` → `SystemWorker` and `SubmitRequestUpload` → `RequestLink`; then `_ => false`. Everything about the test matrix follows from this switch. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:3-6` and `:23-26` | The two summaries that make the *Two policy engines* rule a repository fact rather than a plan opinion: the enum "Names the application boundary being authorised. Business-state preconditions remain owned by their feature use cases", and the class is "The single Core role boundary shared by Web, Worker and later authenticated transports. Unknown actor/permission combinations fail closed." The filter is a *transport* of that boundary. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:69-78` | `StaffAuthorizationException` carries the refused `Permission`, so the 403 can name the right without the client guessing — and so throwing it is strictly better than constructing a response inside the filter. |
| `src/Pegasus.Core/Actors/StaffActorFactory.cs:8-38` | The three fail-closed rules the API must **not** re-derive: subject must parse as a non-empty `Guid` (`:15-18`); every role name must parse with `ignoreCase: false` and be `Enum.IsDefined` (`:21-29`) — so `"administrator"` fails; and an empty role set fails (`:31-34`). Decisively, it always returns `ActionActor.Staff(...)` — a bearer token can never yield `SystemWorker` or `RequestLink`. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:346-377` | **`WebPagesHaveOneStaffActorAndOperationKeyOwnerPerConcept` — read this before choosing a folder.** It enumerates `src/Pegasus.Web/Pages/**/*.cs` and asserts the set of files containing the literal `StaffActorFactory.TryCreate` is **exactly** `["StaffPageModel.cs"]`, and that the set containing `Guid.NewGuid().ToString("N")` is exactly `["StaffPageModel.cs", "Upload.cshtml.cs"]`. Two consequences: putting the new accessor anywhere under `Pages/` turns this fact red, and it is the repository's own written form of the "one owner per concept" rule this ticket is applying to the API. Because the enumeration root is `Pages/`, an accessor under `Api/` leaves the fact green — and that green is not luck, it is the reason for the folder choice. |
| `src/Pegasus.Web/Pages/StaffPageModel.cs:11-16` | The exact claim types in use today — `ClaimTypes.NameIdentifier` for the subject, every `ClaimTypes.Role` for roles — and the whole of the existing seam, which is one factory call. Copy the call, not the member. `NewOperationKey()` at `:17` is the other half of the fact above. |
| `src/Pegasus.Web/Mcp/AutomationActorResolver.cs:13-19` | The fail-closed doctrine in the repository's own words: resolve "before any tool touches a use case", failing closed on a missing principal, a disabled registration or a missing scope, "writing an attributable security event for every material denial". |
| `src/Pegasus.Web/Mcp/AutomationActorResolver.cs:26-90` | The working implementation to mirror: the ordered refusals in `RequireAsync`, and `DenyAsync` (`:74-90`) constructing `new SecurityEvent(Guid.NewGuid(), type, SecurityEventOutcome.Denied, subjectId, timeProvider.GetUtcNow(), httpContext.TraceIdentifier, reasonCode)` with snake_case reason codes (`automation_token_rejected`, `automation_client_disabled`, `automation_scope_denied`). Follow the vocabulary; **change the correlation source** — see *Ripple effects*. |
| `src/Pegasus.Core/Identity/IdentityContracts.cs:98-142` | `SecurityEventType` (seven values; `Token` and `Client` are the relevant ones), `SecurityEventOutcome` (`Succeeded`/`Denied`/`Failed`), the `SecurityEvent` record with its `string CorrelationId` member, and `ISecurityEventWriter.AppendAsync`. Reuse this writer; a second audit path is the defect step 4 forbids. |
| `src/Pegasus.Core/Identity/IdentityContracts.cs:5-28` | `StaffRole` = `Administrator`, `Engineer`, `User` (three), `StaffRoleNames.All`, and `ActorKind` = `Staff`, `SystemWorker`, `RequestLink`, `Automation` (four; the body cites `:22-30`, measured `:22-28`). The test matrix's role axis has three values, not more. |
| `src/Pegasus.Web/Mcp/AutomationMcp.cs:13`, `:22`, `:24` | `AuthenticationScheme = "PegasusAutomationMcp"`, `EndpointPolicy = "AutomationMcpEndpoint"`, `Audience = "pegasus-automation-mcp"` (the body cites `:31`; measured `:24`). The audience string to reject — via the constant, never a literal. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Conventions | "**Auth right** is the `StaffAccessRight` checked by the endpoint filter; `PerformCasework` implies `AccessStaffApplication`." That implication is a *convention for reading the map*, not something `StaffAuthorization` encodes — the switch treats the two independently. Declaring `PerformCasework` on a group does not also assert `AccessStaffApplication`; both happen to admit any staff actor, which is why the map can say it. |
| `docs/desktop/03-gateway-api-and-data/README.md` § 7 (*Two policy engines*, *Coexistence*, *Rate limiting*) | "any rule that appears in an endpoint filter is a defect"; "adding bearer authentication must not change the cookie scheme's defaults (`__Host-Pegasus`, `SameSite=Strict`)"; "reuse the existing limiter policies … do not introduce a second limiter mechanism". |
| `src/Pegasus.Web/Program.cs:275-296` | The rate-limiter `OnRejected` block: it already writes a `RateLimited` security event and picks `sign_in_rate_limited` / `automation_rate_limited` / `authentication_rate_limited` **by path**. `/api/v1` falls into the last today. Tells the implementer that limiter behaviour for the API already exists in a form, and that changing it is another ticket's. |
| `src/Pegasus.Web/Program.cs:517-520` | `SetFallbackPolicy(RequireAuthenticatedUser())`. An `/api/v1` endpoint with no explicit scheme still challenges the **cookie** scheme, producing a 302 rather than a 401 — which is why the anonymous fact in step 9 depends on [[GWY-021]] having attached the bearer scheme to the group, and why the fact is worth writing rather than assuming. |
| `src/Pegasus.Web/Program.cs:874-900` | The `MustChangePassword` redirect middleware, whose allow-list does not include `/api/v1`. Context for step 8: the *state* exists and already has a Razor behaviour; the API needs it as `password-change-required`, and [[GWY-021]] owns the signal. |
| `tests/Pegasus.IntegrationTests/LocalIntakeAccessTests.cs:10-56` | The integration-test idiom: class-level `[Trait("Category", "SqlServer")]`, `TheoryData` for a matrix, and assertions read from the composed host rather than from a mock. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26-141` | The shared factory, its `"Development"` default and `useIntegrationTestAuthentication` constructor flag — the existing seam for signing a test principal in, and therefore the starting point for issuing the twelve-right matrix's principals without standing up a real token flow. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 row `DSK-04-04` | What [[GWY-021]] actually registers — the bearer scheme, the claims it populates, and the per-request `IsEnabled`/security-stamp check this filter consumes. Read it before step 2's stop/go decision and before step 8's mapping. |

## Ripple effects

- **The twelve-right matrix has four shapes and two of them are counter-intuitive.** State this in the
  tests themselves, not only in the plan:
  - `AccessStaffApplication`, `PerformCasework` — positive: any staff role. Negative: **not** a
    different staff role (every role passes); the reachable negatives are anonymous and the
    Automation-audience token.
  - the eight management rights — positive: a staff `Administrator`. Negative: a staff `Engineer` or
    `User`.
  - `ExecuteSystemWork`, `SubmitRequestUpload` — **no staff actor of any role can ever be
    authorized**, because `StaffActorFactory.TryCreate` only produces `ActorKind.Staff`. Their
    "positive" fact is a *permanent refusal* fact. Do not widen `StaffActorFactory` to make a green
    test — `src/Pegasus.Core/Identity/**` is outside the Guardrails.
- **Folder choice is load-bearing for an existing architecture fact.**
  `WebPagesHaveOneStaffActorAndOperationKeyOwnerPerConcept`
  (`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:346-377`) asserts that within
  `src/Pegasus.Web/Pages` the only file containing `StaffActorFactory.TryCreate` is
  `StaffPageModel.cs`. `src/Pegasus.Web/Api/StaffActorAccessor.cs` is outside that root and leaves the
  fact green; anything under `Pages/` turns it red. Worth considering in review whether the fact should
  later be widened to cover `src/Pegasus.Web` as a whole with an expected set of two — that is a
  judgement for [[GWY-018]] (plan handle `DSK-03-18`), not a change to make here.
- **The denial `SecurityEvent` must use the request's correlation id.** `SecurityEvent.CorrelationId`
  (`IdentityContracts.cs:121`) is what joins the audit row to the operator-visible failure, and
  [[GWY-002]]'s `CorrelationIdEndpointFilter` accepts-or-generates the value the problem body reports.
  `AutomationActorResolver` uses `httpContext.TraceIdentifier` only because MCP has no correlation
  header. Two different values would make the trail unjoinable.
- **`grep -rn "StaffActorFactory.TryCreate" src/Pegasus.Web` goes from one call site to two.** One is
  `Pages/StaffPageModel.cs:12`, the other the new accessor. A third is a defect and the ticket's own
  verification says so.
- **[[GWY-002]]'s three files are extended, not rewritten.** `DesktopGateway.cs` gains one constant,
  `DesktopGatewayExtensions.cs` one registration, `DesktopGatewayProblems.cs` two mappings. Do not
  reorder the existing exception branches.
- **Seven downstream tickets attach their right through `RequireStaffRight`**: `blocks` names
  [[GWY-006]], [[GWY-007]], [[GWY-010]], [[GWY-012]], [[GWY-013]], [[GWY-015]] and [[PLAT-005]]. The
  extension method's signature is therefore a contract; keep it to one `StaffAccessRight` argument.
- **`openapi/pegasus-v1.json` — a *future* ripple.** `ls openapi` → *No such file or directory*. Once
  [[GWY-004]] (plan handle `DSK-03-04`) creates it, the 401/403 problem responses this filter produces
  become part of the documented contract for every group.
- **Documentation.** None — the ticket body's *Documentation changes* section says ADR-0102 is authored
  by area 04 and this ticket implements against it. Do not add a `docs/` file.
- **No new package, no new limiter, no cookie-scheme change.** The `Pegasus.Web` package set and
  `packages.lock.json` are unchanged.

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **`src/Pegasus.Core/Identity/**`** — untouched. The right set is Core's and is not extended, narrowed
  or reinterpreted here; in particular `StaffActorFactory` is not widened to produce a non-`Staff`
  actor kind.
- **`src/Pegasus.Web/Mcp/**`** — untouched. `AutomationActorResolver` is *read as a precedent* and
  never edited or refactored into a shared base: MCP and the API are two ingresses over one Core, and
  merging their resolvers would couple them.
- **`src/Pegasus.Web/Pages/**` and the `WebPagesHaveOneStaffActorAndOperationKeyOwnerPerConcept`
  fact** — untouched. The accessor lives under `Api/` precisely so that fact keeps its current
  expected set; widening the fact to cover the whole of `src/Pegasus.Web` is [[GWY-018]]'s judgement.
- **The cookie authentication configuration in `Program.cs`** — untouched; `__Host-Pegasus` and
  `SameSite=Strict` defaults must not move when a bearer scheme is present.
- **The bearer scheme itself, the OpenIddict `pegasus-desktop` client, and the per-request
  enabled/security-stamp check** — [[GWY-021]] (plan handle `DSK-04-04`) and [[GWY-019]] (plan handle
  `DSK-04-02`). Step 2 stops if they have not landed rather than inventing a second token pipeline.
- **The identity lookups behind `account-disabled` and `password-change-required`** — [[GWY-021]].
  This ticket maps a signal; it does not query `UserManager` from an endpoint filter.
- **A second rate limiter** — refused. `Program.cs:275-296` is the one mechanism; a per-user `/api/v1`
  write policy is a later ticket's.
- **Any business precondition in the filter** — refused; that is the *Two policy engines* defect, and
  Core still calls `StaffAuthorization.Require` in every use case.
- **Any endpoint under `/api/v1`** — none added; the twelve-right matrix uses a test-mapped endpoint,
  and real endpoints begin at [[GWY-006]] (plan handle `DSK-03-06`).
- **Azure** — no write of any kind.
