# Research — GWY-017: gateway performance and resilience for the finished `/api/v1` surface

## Question

What does the repository already provide — and conspicuously lack — for response
compression, conditional reads (`ETag`/`If-None-Match`), cancellation propagation and
per-provider timeouts, so that this ticket can harden the whole `/api/v1` surface as a set
rather than endpoint by endpoint? And what shape must the previous-snapshot compatibility
test take against the contract [[GWY-004]] (plan handle `DSK-03-04`) establishes?

## Current behaviour

The web application has **none of the four behaviours** today. Verified on the fork's
working tree, 2026-08-24:

- **Compression.** `grep -rn "AddResponseCompression\|UseResponseCompression" --include=*.cs src/`
  returns nothing. `src/Pegasus.Web/Program.cs` (1,216 lines) is the composition root and its
  middleware pipeline runs `UseHttpsRedirection` (`:794`) → `UseRouting` (`:796`) → the
  sign-in global limiter (`:797-817`) → `UseRateLimiter` (`:819`) → automation middleware
  (`:821-872`) → `UseAuthentication` (`:874`) → must-change-password redirect (`:875-898`) →
  `UseAuthorization` (`:899`). There is no compression stage anywhere in it.
- **HTTP `ETag` / `If-None-Match`.** No response ever sets one. The only `ETag` symbols in
  `src/` are *storage* concurrency tokens from providers — Box
  (`src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs:159`), Azure Blob
  (`src/Pegasus.Infrastructure/Intake/AzureBlobIntakeArtifactStore.cs:215`, `:436`) and the
  persisted `CaseEntity.CustodySourceETag`
  (`src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs:1092`). None of them reaches
  an HTTP response header. `grep -rn "EntityTagHeaderValue\|If-None-Match" --include=*.cs src/`
  is empty.
- **Cancellation.** 47 of the 53 page models under `src/Pegasus.Web/Pages/` declare a
  `CancellationToken cancellationToken` handler parameter
  (`grep -rln "CancellationToken cancellationToken" src/Pegasus.Web/Pages/ | wc -l` → 47;
  `find src/Pegasus.Web/Pages -name '*.cshtml.cs' | wc -l` → 53), which Razor Pages model
  binding fills from `HttpContext.RequestAborted`. Explicit `RequestAborted` appears only
  **five** times in the whole of `src/Pegasus.Web/`
  (`Presentation/RailCountsPageFilter.cs:42`, `Program.cs:476`, `:803`, `:811`, `:847`).
  Nothing enforces the convention: a handler that omits the parameter compiles and runs.
- **Provider timeouts.** `src/Pegasus.Infrastructure/DependencyInjection.cs` registers
  `HttpClient` **three separate times** — `:531-534` (Box custody), `:566-569` (Graph mail and
  DVLA/DVSA vehicle lookup), `:608-611` (the mailbox-identity resolver) — each with
  `Timeout = TimeSpan.FromSeconds(100)` and each through `TryAddSingleton` of the *same*
  `HttpClient` service type. Because `TryAddSingleton` is first-wins, all three providers
  share one client and one 100-second timeout in any host that composes more than one of
  them. There is no per-provider timeout anywhere.

**Parity-matrix row: none, and none should exist.** The matrix holds 46 rows
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → 46), every one
keyed to a page model under `src/Pegasus.Web/Pages/**`
(`docs/desktop/01-inventory-and-parity/parity-matrix.md:44` "**Current entry point** — page
model path under `src/Pegasus.Web/Pages/`"). Cross-cutting transport behaviour is not a page,
so it has no row. The closest existing repository mechanisms — the ones this ticket must
match rather than replace — are:

- `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:61-64`, which rethrows
  `OperationCanceledException` unchanged instead of collapsing it into a transport error.
  That is the precedent for how a cancelled call must surface.
- `Directory.Build.props:17` `<PlaywrightVersion>1.61.0</PlaywrightVersion>`, whose comment at
  `:10-16` states the single-source-of-truth rule the ticket body asks step 6 to copy for
  timeout constants. (The body cites `Directory.Build.props:16`; that is the last line of the
  comment block — the property itself is at `:17`.)
- `PAR-45` (`docs/desktop/01-inventory-and-parity/parity-matrix.md:90`) is the nearest row in
  spirit — `/health/live`, `/health/ready` (`Program.cs:939-950`) and `GET /diagnostics/version`
  (`Program.cs:954`) are the only machine-shaped HTTP responses that exist today — but it
  covers health and build identity, not compression or conditional reads.

## Findings

### Facts

- **F1 — the whole `/api/v1` surface is created by named earlier tickets, not by this one.**
  `src/Pegasus.Web/Api/` does not exist on the working tree (`ls src/Pegasus.Web` →
  `AiWork Authentication Data Health Mcp Pages Presentation Program.cs Properties …`). It is
  created by [[GWY-002]] (plan handle `DSK-03-02`) and filled by [[GWY-006]] (`DSK-03-06`)
  through [[GWY-015]] (`DSK-03-15`). Likewise `openapi/` and `tests/Pegasus.Api.ContractTests/`
  do not exist yet: the snapshot is established by [[GWY-004]] (`DSK-03-04`) and the test
  project is scaffolded by [[TEST-001]] (plan handle `DSK-08-01`).
- **F2 — compression must be opt-in by MIME type, not by default.** The plan's *Compression*
  row (`docs/desktop/03-gateway-api-and-data/README.md:184`) says "`AddResponseCompression` for
  JSON/problem responses only; bytes (PDF, images) are excluded." The byte surface is
  concrete and countable: **seven** handlers return bytes today, in seven distinct page models
  — `Pages/Cases/Assessment/Index.cshtml.cs`, `Pages/Cases/Documents/Download.cshtml.cs`,
  `Pages/Cases/Documents/Export.cshtml.cs`, `Pages/Cases/Eva/Download.cshtml.cs`,
  `Pages/Intake/Asset.cshtml.cs`, `Pages/Intake/Image.cshtml.cs`,
  `Pages/Intake/Source.cshtml.cs` (`grep -rln "return File(" --include=*.cshtml.cs src/Pegasus.Web/Pages/`).
  Their `/api/v1` projections are owned by [[GWY-010]] (`DSK-03-10`), [[GWY-011]] (`DSK-03-11`)
  and [[GWY-014]] (`DSK-03-14`), and each is an exclusion this ticket must assert.
- **F3 — the byte endpoints are the ones that also carry range requests.** The plan's *Bytes &
  uploads* row (`docs/desktop/03-gateway-api-and-data/README.md:189`) requires "stream with
  `Content-Length`, range support, `ETag`". Compressing a ranged response changes the byte
  offsets a range refers to, which is why the exclusion in F2 is a correctness requirement and
  not only a CPU saving.
- **F4 — weak `ETag`s are already specified, and their value is already decided.** The plan's
  *Concurrency* row (`README.md:181`) fixes the format: reads return `version` in the body
  **and** a weak `ETag` `W/"<version>"`; `If-Match` is explicitly **not** the concurrency
  mechanism. `docs/desktop/03-gateway-api-and-data/endpoint-map.md:18` repeats it ("Reads
  return `version` and a weak `ETag`") and the map's per-row **Concurrency token** column
  already names `ETag` on the read rows (for example `GET /dashboard` and
  `GET /dashboard/rail-counts`, `endpoint-map.md:43-44`). So step 4 is an audit for
  *consistency*, not a design decision.
- **F5 — provider timeouts have exactly one place to land and one anti-pattern to remove.**
  See *Current behaviour*: three `TryAddSingleton(static _ => new HttpClient { Timeout =
  TimeSpan.FromSeconds(100) })` registrations, `DependencyInjection.cs:531`, `:566`, `:608`.
  Named `HttpClient` registrations per provider are the mechanism that makes the constants
  observable; the constants themselves belong in one file per the `PlaywrightVersion`
  precedent.
- **F6 — the retry rule is already written down and is asymmetric.** Plan *Retry* row
  (`README.md:187`): "Desktop retries only idempotent `GET`s (bounded, jittered); commands are
  never retried automatically." The endpoint map's **Idempotent?** column
  (`endpoint-map.md:22-23`) is the machine-readable form of that rule — `yes (key)` for
  operation-key replay, `GET` for natural idempotence — so step 7's assertion has a real column
  to read rather than a new annotation to invent.
- **F7 — cancellation has a Core-side contract to respect.** Core's conflict exceptions
  (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:125`, `:135`, `:143`, `:151`) and the
  transaction shape (`docs/desktop/03-gateway-api-and-data/README.md:191`, "authenticate →
  authorize → version → invariants → change → audit → outbox → commit → return version") mean
  a cancelled command must abort before commit, not between commit and audit. Stores open
  `Serializable` transactions explicitly (for example
  `src/Pegasus.Infrastructure/Persistence/EfWorkflowConfigurationStore.cs:35-37`), so a
  cancelled command rolls back with the transaction — the fact to assert in step 9 is "no
  partial write", which is observable in the database, not a sleep.
- **F8 — the App Insights blind spot makes local evidence mandatory.** Plan risk
  (`README.md:296-298`): App Insights ingestion is capped at 0.1 GB/day (PLAT-034), so a
  production measurement may leave no trace. L-02 (`docs/desktop/README.md`) forbids an Azure
  test environment and ADR-0014 stands. The `dotnet-counters` sample therefore runs on the
  local production-mimicking stack and is attached to the ticket.
- **F9 — the budget the measurement is judged against does not exist yet.**
  `docs/desktop/10-security-observability-performance/README.md:100` states the baseline
  capture ([[PLAT-010]], plan handle `DSK-10-10`) "records it before any budget is treated as
  pass/fail", and `:143` that budgets are measured on the recorded baseline workstation. The
  ticket's own Guardrails say "Do not invent a latency threshold." The measurement is therefore
  recorded as numbers plus a statement about assumption A-4, not as a pass/fail gate.
- **F10 — the compat-range test needs a second snapshot file.** The ticket body names
  `openapi/pegasus-v1.previous.json` alongside `openapi/pegasus-v1.json` as snapshots
  "[[GWY-004]] established". Neither file exists yet, so this ticket must confirm which of the
  two [[GWY-004]] actually produces before assuming both; if only the current snapshot exists,
  producing the `previous` file (a copy taken at the last released contract) is part of step 8.
- **F11 — CI already enforces a locked-restore, warnings-as-errors policy.**
  `Directory.Build.props:8` `TreatWarningsAsErrors=true`, `:7` `AnalysisLevel=latest-recommended`,
  `:6` `Deterministic=true`; `global.json` pins the SDK. New test files inherit this, so a
  compat test that reads JSON must not introduce an unpinned package.

### Assumptions

- **A-GWY017-1 — the previous-snapshot test can validate responses against a stored schema
  without a new package.** `tests/` uses xunit 2.9.3 only. Validating a live response against
  an OpenAPI schema may need a JSON-schema validator that is not currently referenced.
  *Confirmed by*: listing `tests/Pegasus.IntegrationTests/*.csproj` package references and the
  test project [[TEST-001]] creates. *Breaks if wrong*: step 8 needs a package addition and a
  `packages.lock.json` update, adding roughly 1 file and 30 lines to the estimate, and the
  addition must clear `TreatWarningsAsErrors`.
- **A-GWY017-2 — response compression can be scoped to the `/api/v1` group without changing
  Razor or static-asset behaviour.** `app.MapStaticAssets()` (`Program.cs:952`) already serves
  pre-compressed static assets; adding a global compression stage could interact with it.
  *Confirmed by*: the response-compression documentation fetch in step 2 plus a test that a
  Razor page and a static asset are byte-identical before and after. *Breaks if wrong*: the
  middleware must be branched onto the `/api/v1` path prefix rather than registered globally
  — a different registration shape, same file, no estimate change.
- **A-GWY017-3 — a cancelled request's database connection return is observable without a
  timing test.** The ticket body forbids a sleep-based assertion. *Confirmed by*: asserting on
  the `OperationCanceledException` path and on connection-pool counters, as the body's step 9
  directs. *Breaks if wrong*: the fact degrades to "no partial write" alone and the connection
  claim moves to the `dotnet-counters` sample in step 11.
- **A-GWY017-4 — assumption A-1 of plan 03 still holds after the JSON surface lands.** Plan 03
  A-1 (`README.md:150-152`): the existing Container App absorbs the JSON surface without a
  resource change. *Confirmed by*: the step 11 measurement. *Breaks if wrong*: a Container App
  scale change is an ⚠ Azure write, out of bounds for this ticket, and becomes a new ticket in
  area 11.
- **A-GWY017-5 — every read endpoint has a `version` to put in its `ETag`.** The weak-`ETag`
  format is `W/"<version>"`, but list responses and rail counts are projections without an
  aggregate version. *Confirmed by*: reading each read endpoint as [[GWY-006]] through
  [[GWY-015]] land it. *Breaks if wrong*: list endpoints need a derived validator (a hash of
  the page's identifiers and versions), which is a helper change, not a contract change, and
  is still `W/`-weak.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** — lands in the existing `Pegasus.Web` Container App | The `/api/v1` surface is the authoritative read/write path for all operators over one SQL database; L-01 places it in `Pegasus.Web` evolved in place (`docs/desktop/README.md` L-01; plan 03 § 3 *Hosting* row, `README.md:160`). This ticket hardens that surface; it moves nothing. |
| Unattended execution — must it run with every desktop closed? | **no** | Compression, conditional reads and cancellation are per-request behaviours of an already-running host. Nothing in this ticket runs on a schedule; the Worker's unattended paths are untouched (Guardrails: scope is `src/Pegasus.Web/Api/**`, the compression registration in `Program.cs`, and two test projects). |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** — lands on the same Container App, where the credential already is | Step 6's timeouts govern calls to Box, Graph and DVLA/DVSA, whose credentials are brokered by the gateway under ADR-0107 (`docs/desktop/00-governance-and-workflow/README.md` ADR table). The timeout constants live beside the credential boundary, not on the workstation. |
| Public callback — must an external service call a stable public endpoint? | **no** | No provider calls back into anything this ticket adds. The only public callback surface in the repository is `Pages/Uploads/Request.cshtml.cs`, explicitly out of the `/api/v1` projection (`endpoint-map.md:138`). |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** — lands in the gateway's CI contract-test lane, not in Azure | The previous-snapshot test (step 8) enforces the compatibility range centrally: a pilot client one version behind keeps working because the **server** is tested against the previous contract. The lane is `tests/Pegasus.Api.ContractTests` running under GitHub Actions (`.github/workflows/ci.yml`), an in-repository mechanism with no Azure resource. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement exists that would justify moving anything. The `dotnet-counters` sample in step 11 measures the *existing* placement against assumption A-4; the baseline it is judged against is not captured until [[PLAT-010]] (`DSK-10-10`). Claiming an advantage before that baseline would be inventing a threshold, which the Guardrails forbid. |

Three "yes" answers, and all three land the responsibility where it already is: the existing
`Pegasus.Web` Container App (L-01) and the repository's own CI lane. Nothing moves to a new
Azure resource, and no ⚠ Azure write is implied — consistent with the ticket's Guardrails
("Azure: no write") and with L-02/ADR-0014, which forbid an Azure load-test environment.

## Implications

1. **This ticket is an audit with a small amount of new code, not a build.** Four of its twelve
   steps (4, 5, 7, and the compression exclusion half of 3) walk a surface someone else wrote
   and fix what is inconsistent. The `files` document must therefore enumerate that surface as
   *context to be audited*, and the plan's inventory must count both the files it will
   certainly change and the endpoint-group files it will change only where the audit finds a
   gap.
2. **Compression is the only behaviour with a real risk of breaking something.** F3 makes the
   byte exclusion a correctness requirement (range offsets), and A-GWY017-2 makes the
   registration shape a decision to verify against the documentation rather than assume.
   Everything else is additive.
3. **The `ETag` work is de-duplication, not invention.** F4 means the format and the semantics
   are already fixed by plan 03 and the endpoint map. Step 4's real content is "extract the
   helper if endpoints duplicated it as they were added" — a simplification pass over other
   tickets' output, which is exactly what the ticket's dependency on [[GWY-007]] through
   [[GWY-015]] is for.
4. **The measurement is evidence, not a gate.** F9 rules out a pass/fail budget. The
   post-implementation report records numbers plus an explicit statement about A-4;
   [[PLAT-010]] and [[PLAT-013]] (plan handle `DSK-10-13`) own the budget table and the
   release gate respectively.
5. **Step 8 has a precondition the ticket does not state.** F10: if [[GWY-004]] produces only
   `openapi/pegasus-v1.json`, this ticket also produces `openapi/pegasus-v1.previous.json`.
   That is a scope question about a sibling ticket's output, not an unsettled decision, so it
   belongs in the plan's *Risks / open questions* naming [[GWY-004]] — not in an
   `open-questions` document.
6. **Provider timeouts touch `Pegasus.Infrastructure`, which the Guardrails do not list.** The
   ticket's scope boundary names `src/Pegasus.Web/Api/**`, `Program.cs`, and the two test
   projects. The three `HttpClient` registrations that need per-provider timeouts live in
   `src/Pegasus.Infrastructure/DependencyInjection.cs`. The plan resolves this by keeping the
   *constants* in the gateway's own file and the *named-client registration* minimal and
   additive in `DependencyInjection.cs`, and by recording the boundary stretch explicitly for
   the reviewer.

## Open questions

- None that block. Two items are scope boundaries owned by named sibling tickets and are
  recorded in the plan's *Risks / open questions* rather than as blocking questions: whether
  [[GWY-004]] emits `openapi/pegasus-v1.previous.json` (F10), and whether the budget table
  from [[PLAT-010]] exists at implementation time (F9). Neither is an unsettled decision.
- The Guardrails' scope boundary omits `src/Pegasus.Infrastructure/DependencyInjection.cs`
  while step 6 requires per-provider timeouts that only exist there (implication 6). The plan
  is written to the body — step 6 stands — and the boundary stretch is called out for the
  reviewer rather than resolved by dropping the step.
