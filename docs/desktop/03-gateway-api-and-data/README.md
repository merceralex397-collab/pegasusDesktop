# 03 · Gateway API and data

Area plan for the cloud gateway: the versioned JSON API that the native
desktop calls, evolved **inside `Pegasus.Web`** (locked decision L-01), the
shared contracts, the generated client, and the concurrency, idempotency,
audit, and data rules every endpoint follows. The companion file
[endpoint-map.md](endpoint-map.md) lists every endpoint and the Razor page
handler it replaces.

## 1. Purpose and proposal coverage

The gateway is "the minimum trusted boundary for a multi-user system"
(proposal §10.1): authentication, authorization, authoritative writes,
central audit, provider-secret brokering, and the client-compatibility gate.
This plan implements:

| Proposal section | What this plan delivers |
| --- | --- |
| §4 cloud-justification test, §4.1 placement table | Each endpoint group carries its placement reason (shared authority, central enforcement, protected credentials) |
| §5.2 deployment units, §5.3 layers | One deployable (the existing Web Container App); desktop → generated client → `/api/v1` |
| §10.2 API style, §10.3 generated client, §10.4 concurrency, §10.5 transactions and audit, §10.6 query strategy | API conventions, contracts, OpenAPI, client generation, concurrency, audit, paging |
| §16.1 operation model, §16.2 provider resilience (gateway half) | Correlation ids, problem details, cancellation, retry eligibility |
| §13 capability inventory (data needs) | Endpoint map per capability group |
| §21.2 CI stages 6 (contract + integration tests) | Contract tests, snapshot, generated-client compile check |
| §24 Phase 2 (compatibility endpoint, generated-client pipeline) and the API half of Phases 3–8 | Phased work breakdown |

Out of scope here: the token endpoint and session lifecycle (area 04), the
desktop HTTP pipeline and credential store (area 02), UI (area 06), provider
adapters (area 07), packaging (area 09).

## 2. Evidence base

### Facts

Repository evidence (fork `main` at `191ddf33`, read 2026-08-23):

- `src/Pegasus.Web/Program.cs` (1,216 lines) is the composition root. The
  only non-Razor HTTP surface today is `GET /diagnostics/version`
  (`Program.cs:954`), `/health/live` and `/health/ready` (`Program.cs:939-950`),
  and the OpenIddict + MCP endpoints `POST /connect/token`, `/authorize`,
  `/mcp` (`src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:134-137`). There
  is **no OpenAPI document, no Swashbuckle/NSwag/Kiota, no controllers**.
- The MCP surface is the existing machine-readable projection of Core:
  35 `[McpServerTool]` tools across `src/Pegasus.Web/Mcp/*McpTools.cs`
  (`CaseMcpTools`, `IntakeMcpTools`, `DocumentMcpTools`,
  `AssessmentMcpTools`, `MailMcpTools`, `UnidentifiedMcpTools`,
  `TriageMcpTools`), registered at `AutomationMcpExtensions.cs:108-118`,
  gated by `Features:AutomationMcp` (`src/Pegasus.Web/Mcp/AutomationMcp.cs:12`,
  ADR-0026). `AutomationActorResolver.cs:26` resolves the actor;
  `AutomationMcpErrors.cs:17-70` maps `StaffAuthorizationException`,
  `CaseEditLeaseExpiredException`, `CaseEditLeaseConflictException`,
  `CaseVersionConflictException`, argument/invalid-operation/invalid-data
  exceptions, and cancellation into transport errors; its
  `RequireOperationKey` enforces a 100-character `mcp:`-prefixed key.
- Core is transport-neutral and already has the shapes an API needs:
  `CaseMutationRequest(Guid CaseId, long ExpectedVersion, ActionActor Actor,
  string OperationKey, string Reason, string EditLeaseToken)` at
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182`; conflict
  exceptions at `:125` (version), `:135` (lease conflict), `:143` (lease
  expired), `:151` (operation-key conflict); lease replay semantics on
  `ILeaseCaseForEdit` (`:322-334`); lease commands at
  `src/Pegasus.Core/Workflow/CaseCommandContracts.cs:77-91`; token length
  64 hex and fixed-time comparison in
  `src/Pegasus.Core/Workflow/CaseEditAuthority.cs`.
- Authorization: `StaffAccessRight` has 12 values and one fail-closed
  `switch` (`src/Pegasus.Core/Identity/StaffAuthorization.cs:7-21`);
  `ActionActor` kinds `Staff/SystemWorker/RequestLink/Automation`
  (`src/Pegasus.Core/Identity/IdentityContracts.cs:22-30`);
  `StaffActorFactory.TryCreate(subjectId, roleNames, out actor)`
  (`src/Pegasus.Core/Actors/StaffActorFactory.cs:8`) is the claims → actor
  seam. The Automation actor holds `PerformCasework` only (ADR-0011).
- Operation keys are raw strings, caller-supplied, maximum 100 characters
  in most areas (`src/Pegasus.Core/Cases/OrganizationAdministration.cs:274`,
  `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:410`,
  `src/Pegasus.Core/Intake/DurableIntake.cs:256`) and 200 in
  `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:398`.
- Query ports that already exist: `IDashboardQueries`, `ICaseQueryStore`,
  `ICaseDataQueries`, `ITriageQueries`, `IRetainedMailQueries`,
  `IIntakeReceiptQueries`, `IImageIntakeQueries`, `IStaffAccountQueries`,
  `ICaseTaskQueries`, `IVehicleEvidenceQueries`, `IEvaHandoffQueries`,
  `ICaseWorkflowQueries` (all under `src/Pegasus.Core/`).
- Razor surface to be projected: 53 page models (~10.8k LOC) under
  `src/Pegasus.Web/Pages/`; 65 `RedirectToPage` calls in 27 models;
  TempData used by 29 models; `Pages/Cases/CaseMutationPageModel.cs` (339
  lines) carries lease/operation-key/proposed-values across requests in
  cookie TempData with chunk budgets (8,000 / 2,000 characters) — web-only
  state that must not be reproduced. Seven handlers return bytes
  (`Cases/Documents/Download`, `Cases/Documents/Export`, `Cases/Eva/Download`,
  `Cases/Assessment/Index` report, `Intake/Asset`, `Intake/Image`,
  `Intake/Source`); four accept `IFormFile` (`Upload`, `Cases/Custody`,
  `Cases/Assessment/Index` estimate import, `Uploads/Request`);
  `Mail/Index.cshtml.cs:176` is the only JSON handler (`OnGetPreviewAsync`).
  `Presentation/RailCountsPageFilter.cs` (51 lines) fills rail counts per
  request; `Presentation/OperatorLabels.cs` (685 lines) is the single
  code → operator-vocabulary map consumed by 24 `.cshtml` files.
- Form limits: `FormOptions.MultipartBodyLengthLimit =
  IntakeEnvelopeLimits.MaximumBatchContentLength` (`Program.cs:525-530`);
  single upload 10 MiB (`docs/current-architecture.md § Accepted local
  inputs`).
- Rate limiting and sign-in throttling exist (`Program.cs:275-327`,
  ADR-0013 clause 12): `StaffSignIn` fixed window per IP, `AutomationMcp`
  120/min per client, a global 100/min sign-in limiter.
- Persistence: EF Core SQL Server, 64 migrations under
  `src/Pegasus.Infrastructure/Persistence/Migrations/` (latest
  `20260822044425_GrantWorkerCaseDocuments`); runtime roles
  `pegasus_web_runtime_role` / `pegasus_worker_runtime_role` created in
  `20260729176000_AzureSqlRuntimeLeastPrivilege.cs`; every new table needs a
  `Grant*` migration, enforced by `scripts/Test-MigrationGrants.ps1` in CI
  (`.github/workflows/ci.yml:58-60`) and
  `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs`.
  Migrations are applied by the release-owned `efbundle`
  (`scripts/Build-ReleaseArtifacts.ps1:70`), never at startup.
- Tooling pins: `.config/dotnet-tools.json` pins only `dotnet-ef 10.0.10`
  (`rollForward: false`); `global.json` pins SDK `10.0.302`;
  `Directory.Build.props` sets `TreatWarningsAsErrors=true`,
  `AnalysisLevel=latest-recommended`, `Deterministic=true`.
- Tests: xunit 2.9.3 only; `WebApplicationFactory<Program>` in 59 of 136
  integration files (shared factory `IntakeWebTestSupport.cs:26`); LocalDB
  per test database; CI shards SQL tests three ways.
- Design authority rule relevant to list endpoints: "filters are dropdowns;
  tables sort newest first" (`docs/design/README.md` § No explanatory copy
  and page economy).

Official documentation (fetched 2026-08-23):

- ASP.NET Core OpenAPI document generation with
  `Microsoft.AspNetCore.OpenApi` (`AddOpenApi`/`MapOpenApi`):
  <https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi>
- Minimal API route groups and filters:
  <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers>
- Problem Details (RFC 9457) in ASP.NET Core (`AddProblemDetails`,
  `IProblemDetailsService`):
  <https://learn.microsoft.com/aspnet/core/fundamentals/error-handling>
- Kiota API client generation for .NET:
  <https://learn.microsoft.com/openapi/kiota/quickstarts/dotnet>
- Response compression middleware:
  <https://learn.microsoft.com/aspnet/core/performance/response-compression>

### Assumptions

- A-1 The existing Container App can absorb the JSON surface without a
  resource change (the same process already serves Razor, OpenIddict, and
  MCP). To be re-checked by the Phase 3 performance baseline (area 10).
- A-2 OpenIddict bearer validation for staff tokens lands in area 04 before
  the first authenticated `/api/v1` group is enabled; this plan depends on
  it but does not own it.
- A-3 Kiota's generated client compiles under `TreatWarningsAsErrors` with
  the repository analyzers, or the generated folder is excluded from
  analysis by a `Directory.Build.props` condition. To be verified in
  `DSK-03-05`.
- A-4 Azure SQL S0 tolerates the additional read traffic from ten desktop
  clients polling dashboards; the coalescing/refresh rules in area 02 and
  the paging rules below are designed to keep this true. Measured in area
  10.

## 3. Decisions and assumptions

| Topic | Decision | Rationale / source |
| --- | --- | --- |
| Hosting | **Evolve `Pegasus.Web` in place** (L-01): versioned route groups under `/api/v1` registered in `Program.cs` beside Razor Pages; same Container App, same release route (`pegasus-release`). No new deployment unit, so no ADR for a unit and no Azure change for hosting. | Proposal §19 "Existing API hosting — Retain, simplified"; operator decision 2026-08-23 |
| Composition gate | `Features:DesktopGateway` boolean; when false nothing under `/api/v1` is mapped and bearer authentication for staff is not registered (same shape as `Features:AutomationMcp`, ADR-0026). Production enablement is a Container App app-setting change (⚠ Azure write, exact-target approval) done once, before the Phase 2 pilot. | Repository rule: a closed composition gate is a disabled flag, not a shipped feature |
| Projection style | Endpoints are thin argument-mappers over Core ports, exactly like the MCP tools; no business rule in Web; MCP and API remain two ingresses over one Core (never two policy engines) | AGENTS.md product invariants; ADR-0011 |
| Contracts | `src/Pegasus.Contracts` holds request/response/problem DTOs (no EF, no ASP.NET, no WinUI references); desktop never hand-writes DTOs; Core records are **not** exposed directly (they carry `ActionActor` and server-only members) | Proposal §10.3 "prevent handwritten duplicate DTOs" |
| Operator vocabulary | Move the pure code → label map out of `src/Pegasus.Web/Presentation/OperatorLabels.cs` into `Pegasus.Contracts` (pure strings, no ASP.NET dependency) so web and desktop share one list. **Deviation:** the proposal is silent; the repository's one-list-per-concept rule requires it. | AGENTS.md § Simplicity rails |
| Idempotency | Keep the existing caller-supplied `OperationKey`; carry it as an explicit body field on every command DTO (not a header), because Core validates it per command and replay semantics are per use case. Desktop generates `desk:<guid>` keys (≤100 chars; 200 only where Core allows). | `RequireOperationKey` precedent in `AutomationMcpErrors.cs`; `ILeaseCaseForEdit` replay rules |
| Concurrency | Explicit body fields `expectedVersion` (long) and `editLeaseToken` on every mutation, mirroring `CaseMutationRequest`; reads return `version` in the body **and** an `ETag` (weak, `W/"<version>"`) so clients may send `If-None-Match` for cache revalidation. `If-Match` is **not** the concurrency mechanism (Core's semantics are per aggregate and lease-aware). Conflicts → `409` problem carrying `currentVersion`. **Deviation:** proposal §10.4 offers ETag or row version; chosen for fidelity to Core. | `CaseWorkflowContracts.cs:125-151`; `CaseEditAuthority.cs` |
| Problem details | RFC 9457 via `AddProblemDetails`; stable `type` URIs `urn:pegasus:problem:<slug>`: `validation`, `not-authorized`, `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`, `client-unsupported`, `password-change-required`, `account-disabled`, `provider-unavailable`, `not-found`, `rate-limited`, `maintenance`. Body never carries payload dumps; `correlationId` always present. | Port of `AutomationMcpErrors.cs`; proposal §14.8, §16.1 |
| Correlation & client version | `X-Correlation-Id` accepted or generated, echoed, logged; `X-Pegasus-Client-Version` required on every `/api/v1` request and enforced by the compatibility middleware (area 04); absence → `client-unsupported`. | Proposal §8.2 step 7, §9.1 |
| Paging/filter/sort | Cursor-less offset paging (`page`, `pageSize` ≤ 200) with `sort` whitelist per endpoint and newest-first default; filters are explicit query parameters matching the dropdown filters the design authority allows; totals returned only where the existing query port already counts. | Design authority "tables sort newest first"; proposal §10.6 |
| Case detail | Sectioned: `GET /cases/{id}` (header + overview), then `/vehicle`, `/assessment`, `/documents`, `/communications`, `/tasks`, `/reports`, `/history` loaded lazily by the desktop. | Proposal §10.6, §14.5 |
| Bytes & uploads | Byte endpoints stream with `Content-Length`, range support, `ETag`; uploads use a two-step upload session (`POST …/upload-session` → `PUT` bytes → `POST …/complete`) reusing `IntakeEnvelopeLimits`; `Uploads/Request` stays an anonymous Razor page (external audience). | Proposal §10.2 "upload-session", §12.2; `minimal-api-file-upload` skill |
| OpenAPI & client | `Microsoft.AspNetCore.OpenApi` document at `/openapi/v1.json`, exported in the build to `openapi/pegasus-v1.json` and snapshot-tested; **Kiota** generates the C# client into `src/Pegasus.Desktop.Infrastructure/Api/Generated/` through `eng/api/Generate-ApiClient.ps1`; generated code is committed; CI fails if regeneration changes the tree. Kiota is pinned in `.config/dotnet-tools.json` (`microsoft.openapi.kiota`). **Deviation:** none — proposal §10.3 allows committing the generated client. | Proposal §10.3; Kiota quickstart (above) |
| Retry | Desktop retries only idempotent `GET`s (bounded, jittered); commands are never retried automatically; provider-backed endpoints carry provider-specific timeouts. | Proposal §10.3, §16.2 |
| Audit & transactions | Reuse `IActionHistoryWriter` / `ISecurityEventWriter` and the existing store transactions (authenticate → authorize → version → invariants → change → audit → outbox → commit → return version, §10.5). No event sourcing; outbox = existing `external-work` queue. | Proposal §10.5; `docs/current-architecture.md § Idempotency` |
| Compression | `AddResponseCompression` for JSON/problem responses only; bytes (PDF, images) are excluded. | Proposal §15.2 |
| Schema | No schema change for the API itself. New tables (OpenIddict desktop client is a row in existing tables; an optional `MinimumClientVersion` setting table is owned by area 04) require `Grant*` migrations; all API changes are expand-then-contract so the web app and older desktop versions keep working through rollout. | PLAT-035 trap; proposal §9.3 |

⚠ Azure writes in this area: none for code; one app-setting change
(`Features__DesktopGateway=true`, and later the minimum-version setting if
it is not DB-backed) on the production Container App at the Phase 2 release
— conditional on exact-target approval and recorded in area 11.

## 4. Target state and exit gate

Target state: every desktop capability in the parity matrix (area 01) has
an `/api/v1` endpoint in [endpoint-map.md](endpoint-map.md); the desktop
calls only the generated client; the OpenAPI snapshot is the contract; the
web app continues to work unchanged during coexistence.

Exit gate (proposal §24 Phase 2 API items, §22.2 contract tests):

- `openapi/pegasus-v1.json` snapshot test passes; regeneration is a no-op.
- Generated client compiles under the repository's warning policy.
- Every command endpoint has tests for: authorized success, unauthorized
  (wrong role), version conflict, lease conflict, operation-key replay,
  validation failure, and the problem-details shape.
- Every list endpoint has paging/filter/sort contract tests and a
  newest-first default test.
- `Features:DesktopGateway=false` leaves no `/api/v1` route (404 test).
- Backward compatibility: a contract test runs the previous snapshot against
  the current server for the supported client range.
- Razor page tests (`tests/Pegasus.IntegrationTests`) stay green.

## 5. Work breakdown

Tier numbers follow `docs/engineering.md` § Required evidence tiers
(1 static/build/architecture · 2 Core · 3 adapter contracts · 4 LocalDB ·
5 Web/API/MCP caller · 7 browser/accessibility · 9 security/observability ·
10 performance · 12 integrated workflow). Profiles are Kanmer profiles.
Routing = subagent · skills · MCP.

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-03-01 | `Pegasus.Contracts` project: DTO conventions, problem types, paging envelope, `OperationKey`/version fields | feature | DSK-02 foundation (solution, CPM) | Project builds; architecture test forbids EF/ASP.NET/WinUI references; problem-type catalogue documented in the project | `dotnet build`; `tests/Pegasus.ArchitectureTests` new test | 1 | pegasus-gateway-dev · dotnet-webapi, microsoft-code-reference · Microsoft Learn, Kanmer |
| DSK-03-02 | `/api/v1` route-group skeleton behind `Features:DesktopGateway`, problem-details mapping ported from `AutomationMcpErrors.cs`, correlation id, client-version header hook | feature | DSK-03-01 | Gate off → 404; gate on → group exists; exception → problem mapping tested for each Core exception | `WebApplicationFactory` tests; gate on/off tests like `LocalIntakeAccessTests.cs` | 5 | pegasus-gateway-dev · dotnet-webapi · Microsoft Learn, Kanmer |
| DSK-03-03 | Staff bearer actor resolution: claims → `StaffActorFactory.TryCreate` → endpoint filter requiring `StaffAccessRight` per group | feature | DSK-04 token flow, DSK-03-02 | Each right has a positive and negative test; disabled account rejected; Automation tokens rejected on `/api/v1` | Integration tests per right | 5, 9 | pegasus-gateway-dev · dotnet-webapi, code-testing-agent · Kanmer |
| DSK-03-04 | OpenAPI document + committed snapshot `openapi/pegasus-v1.json` + snapshot test in `tests/Pegasus.Api.ContractTests` | feature | DSK-03-02 | Snapshot test fails on any unreviewed contract change; document includes problem schemas | `dotnet test --filter FullyQualifiedName~ContractTests` | 1, 5 | pegasus-gateway-dev · dotnet-webapi · Microsoft Learn (OpenAPI .NET 10) |
| DSK-03-05 | Kiota client generation script `eng/api/Generate-ApiClient.ps1`, tool pin in `.config/dotnet-tools.json`, committed output, CI no-op check | feature | DSK-03-04 | Regeneration idempotent; client compiles under `TreatWarningsAsErrors`; desktop references only the generated client | CI job step; `git diff --exit-code` after regeneration | 1 | pegasus-gateway-dev, pegasus-release-packager · dotnet-webapi, authoring-github-workflows · Microsoft Learn (Kiota) |
| DSK-03-06 | Compatibility + dashboard + rail-counts endpoints (`GET /client-compatibility`, `GET /dashboard`, `GET /dashboard/rail-counts`) | feature | DSK-03-03 | Counts equal `RailCountsPageFilter` output for the same data; compatibility payload matches area 04 contract | Integration tests; parity comparison test | 5 | pegasus-gateway-dev · dotnet-webapi · Kanmer |
| DSK-03-07 | Cases read endpoints: list/search (paged, sorted newest first, dropdown filters), detail sections, history/audit | feature | DSK-03-03 | Paging/sort/filter contract tests; sections load independently; no N+1 (EF query review) | Integration tests; `optimizing-ef-core-queries` review on `EfCaseQueryStore` paths | 4, 5 | pegasus-gateway-dev · dotnet-webapi, optimizing-ef-core-queries · Kanmer |
| DSK-03-08 | Cases command endpoints: create, update, lease claim/renew/release, confirm completeness, workflow commands (hold, release hold, return to review, assign engineer, start work, record finding, linked replacement), closure (report approval, close, reopen, archive) | feature | DSK-03-07 | Each command: success, unauthorized, version conflict 409, lease conflict, replay returns same result, validation problem | Integration tests mirroring `CaseWorkflowPersistenceTests.cs` scenarios | 4, 5 | pegasus-gateway-dev · dotnet-webapi, code-testing-agent · Kanmer |
| DSK-03-09 | Tasks, notes, chasers, report-evidence links endpoints | feature | DSK-03-08 | Same command test matrix | Integration tests | 5 | pegasus-gateway-dev · dotnet-webapi · Kanmer |
| DSK-03-10 | Intake/received endpoints: detail, retry allocation, block, reevaluate, correct draft, claim case lease, link/reverse link, register image intake, dismiss suggestion; byte endpoints for asset/image/source | feature | DSK-03-03 | Byte endpoints send `Content-Length`, `ETag`, range; no-sniff; safe filename; mutations follow the command matrix | Integration tests reusing `MultiFormatIntakeWebTests.cs` fixtures | 5 | pegasus-gateway-dev · dotnet-webapi, minimal-api-file-upload · Kanmer |
| DSK-03-11 | Upload-session endpoints (stage, status, group register/attach) and case document upload/remove/custody retry, export, EVA handoff generate/download | feature | DSK-03-10 | Limits enforced from `IntakeEnvelopeLimits`; interrupted upload leaves no receipt; export produces the same archive as `Documents/Export` | Integration tests; comparison with `CASE-019` export proof | 5 | pegasus-gateway-dev · minimal-api-file-upload, dotnet-webapi · Microsoft Learn |
| DSK-03-12 | Mail workspace endpoints: list, preview, message detail, prepare/link/unlink case, correct classification, move to recommended folder | feature | DSK-03-03 | Behaviour equals `Pages/Mail/*` handlers (same Core calls, same versions/leases); move control absent when provider unavailable | Integration tests reusing `MailWorkspaceWebTests.cs` scenarios | 5 | pegasus-gateway-dev · dotnet-webapi · Kanmer |
| DSK-03-13 | Triage, Unidentified, Image-intake, Operations endpoints (explicit named commands; no dispatcher string) | feature | DSK-03-03 | Every action in `Pages/Triage/Details.cshtml.cs:116-204` has an explicit endpoint; operations retry/revoke audited | Integration tests | 5 | pegasus-gateway-dev · dotnet-webapi · Kanmer |
| DSK-03-14 | Vehicle lookup request/accept and assessment endpoints (get, save damage, generate report draft, import estimate, accept specification, send, reconcile) | feature | DSK-03-08, DSK-07 provider plans | Provider failure distinguishable from not found; report draft endpoint returns bytes with `ETag` until L-03 moves rendering local | Integration tests with replay adapters | 5 | pegasus-gateway-dev · dotnet-webapi · Kanmer |
| DSK-03-15 | Administration endpoints: configuration, mail categories, mailboxes (update, resolve folders), access review, accounts (create, disable), roles, automation (enable, connector, channel token), activity, organizations, principals (create, replace) | feature | DSK-03-03 | Administrator-only; every mutation writes a security/action-history record; non-admin gets `not-authorized` | Integration tests | 5, 9 | pegasus-gateway-dev · dotnet-webapi · Kanmer |
| DSK-03-16 | `OperatorLabels` relocation to `Pegasus.Contracts` (one vocabulary list) with web consumers re-pointed | chore | DSK-03-01 | No behaviour change in Razor pages; desktop consumes the same map; no second copy | Existing web tests green; architecture test forbids a second label map | 1, 5 | pegasus-gateway-dev · dotnet-webapi · Kanmer |
| DSK-03-17 | Performance and resilience: response compression (JSON only), `ETag`/`If-None-Match` on reads, cancellation propagation, provider timeouts per endpoint, compat-range test against the previous snapshot | feature | DSK-03-07…15 | Compression negotiated; cancelled requests release DB connections; previous-snapshot test passes | Integration tests; `dotnet-counters` sample under ten concurrent clients (area 10) | 5, 10 | pegasus-gateway-dev, pegasus-test-engineer · dotnet-webapi, analyzing-dotnet-performance, test-gap-analysis · Kanmer |
| DSK-03-18 | Contract and authorization gap review across all command endpoints (independent) | chore | DSK-03-08…15 | Every command has the seven-case test matrix; gaps filed as tickets | `test-gap-analysis` report in the ticket | 5 | pegasus-desktop-reviewer · test-gap-analysis, assertion-quality · Kanmer |

Sequencing: 01 → 02 → 03 → 04 → 05 (foundation, Phase 2); 06–07 (Phase 3
read slice); 08–09 (Phase 4); 10–12 (Phase 5); 11, 14 (Phase 6–7); 15
(Phase 8); 16–18 run alongside from Phase 3. Each endpoint ticket lands
with its desktop slice in area 05 so that nothing is built without a caller.

## 6. Routing table

| Work | Subagent | Skills (pinned source) | MCP tools |
| --- | --- | --- | --- |
| Endpoint design and implementation | `pegasus-gateway-dev` | `dotnet-webapi`, `minimal-api-file-upload` (dotnet/skills `98f84851`, plugin `dotnet-aspnetcore`); `microsoft-code-reference` (Microsoft Learn plugin) | Microsoft Learn `microsoft_docs_search`, `microsoft_code_sample_search` (Minimal APIs, OpenAPI, problem details, Kiota); Kanmer `take_ticket`, `set_ticket_doc`, `get_doc_gates` |
| EF query shape for list/section endpoints | `pegasus-gateway-dev` | `optimizing-ef-core-queries` (dotnet/skills, plugin `dotnet-data`) | Microsoft Learn |
| Tests and gap analysis | `pegasus-test-engineer`, `pegasus-desktop-reviewer` | `code-testing-agent`, `run-tests`, `test-gap-analysis`, `assertion-quality` (dotnet/skills, plugin `dotnet-test`) | Kanmer |
| Client generation and CI check | `pegasus-gateway-dev`, `pegasus-release-packager` | `authoring-github-workflows` (dotnet/skills `.agents/skills`); `directory-build-organization` (plugin `dotnet-msbuild`) | Microsoft Learn (Kiota) |
| Independent review | `pegasus-desktop-reviewer` | `winui-code-review` is not needed here; uses the project skill `pegasus-desktop` and `microsoft-code-reference` | Microsoft Learn |
| Reference projections to copy from | — | repository: `src/Pegasus.Web/Mcp/*McpTools.cs`, `AutomationMcpErrors.cs`, `Pages/Cases/CaseMutationPageModel.cs` (what **not** to reproduce) | — |

## 7. Risks and traps

- **Runtime-role grants.** Any new table or write path needs a `Grant*`
  migration mirrored in `Invoke-AzureDatabaseBootstrap.ps1` and the pinned
  census in `IntakePersistenceIntegrationTests.cs`; the class of failure
  "works locally, fails only in production" has shipped three times
  (PLAT-035; `.agents/skills/pegasus-release/SKILL.md` traps table).
- **Composition gate off = 404.** Tests must assert the gate both ways;
  production enablement is an app-setting change that needs approval.
- **Two policy engines.** API endpoints and MCP tools must both call Core
  use cases; any rule that appears in an endpoint filter is a defect.
- **TempData semantics.** Do not port `CaseMutationPageModel`'s
  proposed-values/lease chaining; the desktop keeps that state in memory and
  sends explicit fields.
- **`TreatWarningsAsErrors` + generated code.** Kiota output may need
  `<GeneratedCodeAttribute>`-based analyzer suppression or a folder-level
  `Directory.Build.props`; decide in `DSK-03-05`, never by lowering the
  repository policy.
- **Linux publish.** `Pegasus.Web` still publishes `linux-x64` into the
  Playwright base image; the API must not pull in Windows-only packages;
  `Pegasus.Contracts` must stay `net10.0`.
- **Coexistence.** Razor pages and the API share Identity, OpenIddict, rate
  limiters, and the `Features:*` gates; adding bearer authentication must
  not change the cookie scheme's defaults (`__Host-Pegasus`, `SameSite=Strict`).
- **Rate limiting.** Reuse the existing limiter policies for the token
  endpoint (area 04) and add a per-user policy for `/api/v1` writes; do not
  introduce a second limiter mechanism.
- **Observability blind spot.** App Insights ingestion is capped at
  0.1 GB/day (PLAT-034); API failures in production may leave no trace —
  problem details with correlation ids and the desktop diagnostics bundle
  are the compensating evidence.
- **Upstream drift.** Upstream `main` is 32 commits ahead; endpoint work on
  Mail/Box paths should start after the first upstream sync (area 00) to
  avoid re-projecting code that has since changed (PLAT-039, DOCS-010,
  MAIL-011/012).
- **Pilot ring compatibility.** Contract changes must be additive until the
  minimum client version advances; removing a field is a contract-test
  failure by design.

## 8. Documentation changes

- ADR-0101 (local-execution/cloud-authority split) and ADR-0103 (gateway,
  not direct database access) — write via `kanmer-docs`; ADR-0102 is owned by
  area 04.
- `docs/frd/frd-13-desktop-operator-experience.md` (area 06 owns the body)
  gains an "API contract" section linking `openapi/pegasus-v1.json`.
- `docs/capabilities.md`: `DSK` rows for the gateway contract, generated
  client, and compatibility endpoint.
- `docs/current-architecture.md`: add the `/api/v1` surface to "Current
  callers and entry points" and the `Features:DesktopGateway` gate to the
  feature-gate list after it merges; `docs/operations.md` records the
  production gate state after the Phase 2 release (same task as the release,
  per AGENTS.md safety rails).
- `docs/index.md`: already points at this plan set; add the OpenAPI file
  location when it exists.
- `AGENTS.md § Product invariants`: no change needed — Web remains a
  composition root; confirm in review that the invariant text still holds.
