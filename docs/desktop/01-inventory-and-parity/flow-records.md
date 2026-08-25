# Current-flow records

Six records of how Pegasus works today on the paths the desktop conversion
depends on most. Each record is pre-filled from the 2026-08-23 code
inspection (fork `main` `191ddf33`); the Phase 0 tickets DSK-01-06 and
DSK-01-07 complete them, answer the open questions, and attach the read-only
command outputs. A record is closed when every open question has a code
citation or a line in `docs/open-decisions.md`.

Record template (every record below uses it):

1. Purpose — why the desktop needs this flow understood.
2. Current entry points — pages, functions, endpoints.
3. Current code paths — files and lines.
4. Data owned — tables, blobs, secrets, external objects.
5. Failure modes — what breaks today and how it is visible.
6. What the desktop needs from it — the gateway/desktop split (proposal §4.1).
7. Open questions — must be answered before the dependent phase starts.
8. Read-only verification — commands to re-check the facts.

## Record 1 — Staff authentication and session

**Purpose.** The desktop keeps the existing Pegasus accounts and login
(proposal §8); the gateway must issue a desktop-compatible session from the
same identity store without Microsoft login.

**Current entry points.** `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs`
(`OnPostAsync`, 106 lines), `SignOut.cshtml.cs`, `PasswordChange.cshtml.cs`,
`AccessDenied.cshtml.cs`; OpenIddict `/connect/token` and `/authorize` for
the Automation client only (`src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:134`).

**Current code paths.**

- Identity: `AddIdentity<PegasusIdentityUser, IdentityRole<Guid>>`,
  `Password.RequiredLength = 8`, complexity off,
  `Lockout.AllowedForNewUsers = false`, `RequireConfirmedAccount = false`
  (`src/Pegasus.Web/Program.cs:262-274`).
- Sign-in check: `CheckPasswordSignInAsync(user, Password,
  lockoutOnFailure: false)` (`Pages/Account/SignIn.cshtml.cs:63`) — ADR-0013
  clause 12: throttling, not lockout.
- Rate limiting: `RejectionStatusCode=429`, `Retry-After: 60`; policies
  `StaffSignIn` (fixed window per remote IP,
  `StaffSessionPolicy.SignInAttemptsPerClientPerMinute = 10`) and
  `AutomationMcp` (120/min); a singleton global `FixedWindowRateLimiter`
  (100/min, `SignInAttemptsGlobalPerMinute`) applied only to
  `POST /Account/SignIn` (`Program.cs:275-327`, `:797-817`). Security events
  `sign_in_rate_limited` etc. written through `ISecurityEventWriter`.
- Auth scheme: policy scheme `"Pegasus"` forwarding to
  `DevelopmentOfflineAuthenticationHandler` in DevelopmentOffline, else the
  Identity application cookie (`Program.cs:328-350`).
- Cookie: `__Host-Pegasus`, HttpOnly, `SameSite=Strict`, `Secure`,
  `ExpireTimeSpan = StaffSessionPolicy.IdleLifetime` (2 h), sliding;
  `OnSigningIn` stamps `pegasus:original-issued-at`; `OnValidatePrincipal`
  enforces the 8 h absolute lifetime and re-checks `user.IsEnabled` on every
  request (`SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`,
  `Program.cs:353`, `:368-457`).
- Authorization: fallback policy `RequireAuthenticatedUser()`; named policy
  `Administrator`; `[Authorize(Roles=…)]` on page models
  (`Program.cs:517-522`); Core rights matrix
  `src/Pegasus.Core/Identity/StaffAuthorization.cs` (12 `StaffAccessRight`
  values, fail-closed); claims → actor via
  `src/Pegasus.Core/Actors/StaffActorFactory.cs`; lifetimes and attempt
  limits in `src/Pegasus.Core/Actors/StaffSessionPolicy.cs`.
- Forced password change: middleware redirect (`Program.cs:875-899`).
- OpenIddict 7.6 (`OpenIddict.AspNetCore`, EF stores): one seeded
  Automation client; client credentials and auth-code+PKCE for external
  connectors (ADR-0026/0027); access token 10 min, refresh 14 days
  (`Mcp/AutomationMcp.cs`).
- Data Protection keys persisted to blob `authentication-ring/keys.xml`
  (`Program.cs:172-176`).

**Data owned.** Identity tables in Azure SQL (users, roles, claims), OpenIddict
application/authorization/token tables, `SecurityEvents`/action history,
Data Protection key ring blob.

**Failure modes.** Disabled account → every request fails closed at
`OnValidatePrincipal`; rate limit → 429 with `Retry-After`; absolute lifetime
→ re-login; Data Protection ring unavailable → cookies unreadable (all users
signed out).

**What the desktop needs.** A staff-subject token path: a first-party public
OpenIddict client (`pegasus-desktop`) with password + refresh_token grants,
rolling refresh tokens, access token ≈10–15 min in memory, refresh token in
the Credential Locker/DPAPI, revocation on disable/password change, the same
rate limiter on the token endpoint, and `StaffActorFactory.TryCreate` from
token claims on every `/api/v1` call (area 04).

**Open questions.**

- Q1.1 Does the OpenIddict EF store already hold the tables needed for
  refresh-token rotation with the runtime role's grants (PLAT-035 class)?
- Q1.2 Which claims the token must carry for `StaffActorFactory.TryCreate`
  (subject id + role names) and whether `IsEnabled` must be re-checked per
  request (today: yes, every request) — decide the refresh interval.
- Q1.3 How the `MustChangePassword` state is surfaced to a token client
  (problem type vs claim).
- Q1.4 Whether DevelopmentOffline's `DevelopmentOfflineAuthenticationHandler`
  gets a token equivalent for the Test/UAT stack.

**Read-only verification.**
`git grep -n "CheckPasswordSignInAsync" src/Pegasus.Web`,
`git grep -n "AddRateLimiter\|FixedWindowRateLimiter" src/Pegasus.Web/Program.cs`,
`git grep -n "class StaffSessionPolicy" -A 12 src/Pegasus.Core/Actors/`,
`git grep -n "AllowPasswordFlow\|AllowRefreshTokenFlow\|AllowClientCredentialsFlow" src/Pegasus.Web`.

## Record 2 — Database and migration bundle

**Purpose.** The desktop never connects to the database (proposal §10.1); the
gateway keeps the single migration stream; API changes must follow
expand-and-contract so the pilot ring and the web app can coexist.

**Current entry points.** `dotnet run -- --migrate-development` (local);
release-owned `efbundle.exe` applied before application packages
(`docs/runbook.md:908-951`, `.azure/deployment-plan.md:104-105`).

**Current code paths.**

- `src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs` (1,526
  lines); `Persistence/Migrations/` — 64 migrations from
  `20260724104624_InitialProviderNeutralIntake` to
  `20260822044425_GrantWorkerCaseDocuments`; snapshot
  `PegasusDbContextModelSnapshot.cs`.
- Runtime roles created in `20260729176000_AzureSqlRuntimeLeastPrivilege.cs`
  (`pegasus_web_runtime_role`, `pegasus_worker_runtime_role`); per-table
  `GRANT` statements in the `Grant*` migrations; reconciliation in
  `20260729199000_RuntimeRoleReconciliation.cs`.
- Bundle build: `scripts/Build-ReleaseArtifacts.ps1:70`
  (`dotnet ef migrations bundle --self-contained -r win-x64 --project
  src/Pegasus.Infrastructure --startup-project src/Pegasus.Web`).
- CI gate: `scripts/Test-MigrationGrants.ps1` (every created table has a
  `GRANT` or an explicit `// no-runtime-grant:` opt-out,
  `.github/workflows/ci.yml:58-60`); test
  `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs`
  (776 lines); pinned migration census in
  `IntakePersistenceIntegrationTests.cs` (release trap: add every new id).
- Local: SQL Server LocalDB `(localdb)\MSSQLLocalDB` / `PegasusDevelopment`;
  tests restore `Pegasus_Test_*` from a per-process template
  (`docs/runbook.md:329-350`). Startup never applies migrations.
- Health: `/health/ready` checks DB + all committed migrations.

**Data owned.** Azure SQL `pegasus` (S0) — cases, intake receipts, retained
mail, identity, OpenIddict, action history, audit, search projections.

**Failure modes.** A migration that grants too little ships green locally
and fails only in production with an unclassifiable exception (PLAT-035;
shipped three times: `20260814092852`, `20260821095500`,
`20260822044425`); `efbundle` host construction fails on a non-shape-valid
`Box__ConfigJson`; schema rollback is not a down-migration.

**What the desktop needs.** Nothing directly. The gateway needs: new tables
for the compatibility gate or desktop preferences (if any) with grants;
OpenIddict client seed; concurrency tokens already present
(`ExpectedVersion`); expand/contract rule in the release plan (area 09).

**Open questions.**

- Q2.1 Does any desktop feature need a new table (drafts are local; audit
  exists) — expected answer: none in Phases 0–4.
- Q2.2 How the OpenIddict desktop client is seeded (migration data seed vs
  bootstrap command) and which role needs which grant on the OpenIddict
  token tables.
- Q2.3 Whether PLAT-035's build-time grant check lands before the first
  gateway schema change (carry-over ticket).

**Read-only verification.**
`git ls-files src/Pegasus.Infrastructure/Persistence/Migrations | grep -c "_.*\.cs$"`,
`git grep -ln "pegasus_web_runtime_role" src/Pegasus.Infrastructure/Persistence/Migrations | wc -l`,
`pwsh scripts/Test-MigrationGrants.ps1`, `sed -n 60,80p scripts/Build-ReleaseArtifacts.ps1`.

## Record 3 — Microsoft Graph intake

**Purpose.** Intake must continue while every desktop is closed (proposal
§12.1); the Worker stays the only poller; the desktop only shows status and
failures through the gateway.

**Current entry points.** Worker timers `InboxPollFunction`
(`src/Pegasus.Worker/MailboxFunctions.cs:15`, schedule
`%ApprovedInboxPollSchedule%`), `SentEvidencePollFunction`
(`EmailEvidenceFunctions.cs:16`), queue functions `IntakeWorkFunction` /
`IntakePoisonFunction` (`IntakeFunctions.cs:33`, `:50`), dispatcher
`PendingWorkDispatchFunction` (`IntakeFunctions.cs:13`), reconciliation
`StagedArtifactReconciliationFunction` (`IntakeFunctions.cs:75`). Staff
surface: `/Inbox` (`Pages/Mail/Index.cshtml.cs`, `Message.cshtml.cs`);
admin `Pages/Administration/Mailboxes.cshtml.cs`.

**Current code paths.**

- Graph adapter `src/Pegasus.Infrastructure/Email/GraphApprovedSources.cs`
  (1,125 lines: inbox/sent/deleted-search/mailbox resolver), settings
  `ApprovedSourceSettings`, local replay sources
  `LocalDurableApprovedInboxSource` / `LocalDurableApprovedSentSource`
  (DevelopmentOffline).
- Core: `PollApprovedInbox` iterates the approved-mailbox estate (Core-owned,
  database-backed, ADR-0022); each mailbox holds its own lease, cursor, and
  last-failure code; retained-message read model written once between
  `ReceiveIntake` and the cursor advance (`Core/Intake/RetainedMail.cs`,
  store `EfRetainedMailboxMessageStore.cs`); stable-identity target ADR-0024
  (not yet implemented, tracked upstream).
- Queue transport: Azure Storage queues `intake-work`, `intake-work-poison`,
  `external-work`, `external-work-poison` (`infra/modules/platform.bicep:129-152`);
  `host.json` `batchSize 4`, `visibilityTimeout 00:05:00`,
  `maxDequeueCount 5`, `maxPollingInterval 00:00:02`.
- Secrets: Graph via managed identity (Exchange Application RBAC, live-verified
  release 1); Worker identity `pegasus-prod-worker-id-*`.
- Worker activation fail-closed: all nine `AzureWebJobs.<fn>.Disabled` set
  unless `workerActivation == 'approved-live-worker'`
  (`platform.bicep:36`, `:531-539`).

**Data owned.** Approved-mailbox rows (identity, enablement, lease, cursor,
last failure), retained-message read model (`BodyPlainText`, attachments
metadata), intake receipts, `IntakeSearchDocuments`, transient blob staging
(`transient-intake`), queue messages.

**Failure modes.** Mailbox failure releases that mailbox alone; poison queue
reconciliation on timer; Graph throttling; a triage-request email that
creates neither Triage nor Unidentified (upstream INTK-033, production
defect); queue message never arrives (upstream INTK-003).

**What the desktop needs.** Read-only status: last successful poll per
mailbox, failures by mailbox, retry/poison counts (proposal §18.3); the
Inbox workspace data (record PAR-21/22); no Graph credential in the desktop
(ADR-0106). Gateway exposes `~GET /api/v1/integrations/status` and the mail
endpoints; Worker unchanged.

**Open questions.**

- Q3.1 Which operations view fields already exist for "last successful
  cycle" per mailbox (Core `Operations/` snapshot) vs need adding.
- Q3.2 Whether the retained-mail search projection writer (Worker) and
  reader (Web `SELECT`) grants cover the gateway's new read endpoints (they
  should — same Web role).
- Q3.3 ADR-0024 migration timing relative to the desktop Inbox slice
  (Phase 5): do it before, or design the desktop against the current key.

**Read-only verification.**
`git grep -n "Function(\"" src/Pegasus.Worker`,
`sed -n 1,40p src/Pegasus.Worker/host.json`,
`git grep -n "class PollApprovedInbox" src/Pegasus.Core`,
Azure MCP `functionapp` show for `pegasus-prod-worker-252ow37gij` (settings
`AzureWebJobs.*.Disabled`).

## Record 4 — Box custody

**Purpose.** Box remains the canonical document store (proposal §12.2); the
gateway brokers credentials and authorization; the desktop browses, uploads,
previews, and caches locally.

**Current entry points.** `Pages/Cases/Custody.cshtml.cs` (upload, retry,
remove, request links), `Pages/Cases/Documents/Download.cshtml.cs`,
`Pages/Cases/Documents/Export.cshtml.cs`, Worker
`ExternalWorkFunction` (`Functions/ExternalWorkFunctions.cs:9`, custody
work), MCP `pegasus_document_*` tools.

**Current code paths.**

- `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` (1,016 lines),
  `BoxDocumentContentStore.cs` (240), local equivalents
  `LocalCaseCustody.cs` (549), `LocalDocumentContentStore.cs` (183);
  `Box.Sdk.Gen 1.12.0`.
- Folder identity: immutable Case/PO reference names the final folder; an
  audit carries one identity (`a.`/`ap.` prefix) since release 18; a
  predeclared creation-owner token is used only in a transient staging
  folder; ETag-guarded same-parent promotion completes creation; durable
  identity is the database-stored remote folder id, no marker file
  (release 15 decision).
- Attachments retained flat at ordinals `002`+; embedded photographs
  promoted ≥40 KB (`InstructionEvidenceImages.Select`, release 16); evidence
  gallery served through the case-document route (release 18).
- Secrets: `Box__ConfigJson`, `Box__ClientSecret` from Key Vault references
  (Worker) / Container App secrets (Web) (`platform.bicep:382-398`,
  `:555-563`); unresolved `@Microsoft.KeyVault(` placeholder fails with a
  named error (`BoxCaseCustody.cs:82-84`, PLAT-013).
- Token lifetime: minted once per process today; upstream PLAT-039 (in
  upstream `main`: "Renew the Box access token before it expires", "Publish
  the Box token and its expiry as one value") — comes across with the first
  sync.
- Custody retry is a human-only Core use case; no automatic business retry.

**Data owned.** Box folders/files (canonical), database document records
(metadata, remote ids, versions), custody work items and outbox
(`CustodyOutboxIntegrationTests.cs` evidence), `box-links` blob container.

**Failure modes.** Custody failure is terminal and visible for staff retry;
Box token expiry (fixed upstream); folder resolve once per image on export
(PLAT-041, performance); PLAT-013 config shape.

**What the desktop needs.** Gateway endpoints for document list/metadata,
upload session (multipart or direct-to-Box URL only if `Box.Sdk.Gen` can
issue a constrained short-lived URL — to verify), content download (bytes,
range/resume), export; local bounded working cache; transfer queue with
retry; conflict detection via document version. No Box secret in the package
(ADR-0107).

**Open questions.**

- Q4.1 Can the Box SDK in use issue short-lived, constrained upload/download
  URLs suitable for direct desktop transfer (proposal §12.2)? If not, stream
  through the gateway and size the Container App accordingly.
- Q4.2 Which document metadata fields exist for "file type, size, source,
  uploader, timestamp" (proposal §14.6) and which need projection work.
- Q4.3 PLAT-041 (resolve folder once per export) must land before the export
  endpoint is exposed to avoid per-image Box calls from a desktop batch.

**Answers (2026-08-25).**

- Q4.1 **Answered.** `Box.Sdk.Gen` 1.12.0 exposes named APIs for this shape:
  `DownloadsManager.GetDownloadFileUrlAsync`,
  `UploadsManager.PreflightFileUploadCheckAsync`,
  `ChunkedUploadsManager.CreateFileUploadSessionAsync` and its URL-based
  upload-part/commit methods, plus `BoxDeveloperTokenAuth.DownscopeTokenAsync`.
  The current Pegasus `BoxContentClient` does not call those generated APIs:
  it keeps the authorization header and guarded upload/download HTTP calls in
  the gateway/Worker path. Box documents that generated SDK downloads normally
  return binary content, while a download URL is short-lived (normally about
  15 minutes), and that token exchange can restrict a token to a file/folder
  and scopes such as `item_download` or `item_upload`. This proves available
  primitives, not approval to expose them: direct desktop transfer remains
  conditional on the DSK-07-07 security/metadata spike; the default is still
  gateway streaming. Sources fetched 2026-08-25:
  [Box Get Download URL](https://developer.box.com/guides/downloads/get-url),
  [Box Downscope a Token](https://developer.box.com/guides/authentication/tokens/downscope),
  [Box Preflight Check](https://developer.box.com/reference/options-files-content),
  [Box Uploads](https://developer.box.com/guides/uploads).
- Q4.2 **Answered.** Core’s `DocumentVersion` and the matching EF
  `DocumentVersionEntity` carry file name/type (`FileName`, `MediaType`), size
  (`ContentLength`), uploader/actor (`CreatedBy`), and version timestamp
  (`CreatedAtUtc`); `Sha256`, custody status, version, current/removal state
  are also available. `DocumentOccurrence`/`DocumentOccurrenceEntity` carry
  source (`DocumentSource`/`Source`), semantic role, source-occurrence
  identity, and the occurrence timestamp (`RecordedAtUtc`). There is no
  separate uploader or source field missing from this projection; the gateway
  should expose these existing fields rather than infer them from Box names.
- Q4.3 **Moved to `docs/open-decisions.md` 2026-08-25.** PLAT-041 is still a
  backlog carry-over. The desktop export endpoint therefore remains gated on
  the one-folder-per-export fix; this ticket does not expose or implement it.

**Read-only verification.**
`git grep -n "class BoxCaseCustody" src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs`,
`git grep -n "Microsoft.KeyVault(" src/Pegasus.Infrastructure`,
`git log --oneline upstream/main -- src/Pegasus.Infrastructure/Custody | head`
(after the `upstream` remote exists).

## Record 5 — DVLA/DVSA vehicle lookup

**Purpose.** Lookup credentials and rate limits stay behind the gateway
(proposal §12.3); the desktop normalises input, triggers, and displays with
provider-state distinctions.

**Current entry points.** `Pages/Cases/Vehicle.cshtml.cs`
(`OnPostRequestVehicleLookupAsync`, `OnPostAcceptVehicleSuggestionAsync`),
Worker `ExternalWorkFunction` (vehicle lookup work) and the reconciliation
sweep that enqueues one lookup per active case with an un-looked-up
registration (release 15), MCP `pegasus_assessment_*` (prefill).

**Current code paths.**

- `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs` (412),
  `DvlaDvsaAdapters.cs` (222, includes the replay adapter used in
  DevelopmentOffline); Core `Vehicle/` (lookup contracts/work items, mileage
  policy, request→accept workflow).
- Secrets: `Dvla__ApiKey`, `Dvsa__ClientId/ClientSecret/ApiKey` via Key Vault
  references on the Worker (`platform.bicep:555-563`); Web records the
  request (replay in DevelopmentOffline, live-enabled in Production);
  Worker owns the live adapter.
- Mileage estimate feeds the assessment page (`Mileage`/`Source` prefill);
  conservative MOT mileage estimation relocated to FRD-06 (ADR-0012
  superseded).

**Data owned.** Durable lookup request rows (idempotent per case and
registration), vehicle evidence with source and timestamp, mileage estimate.

**Failure modes.** Provider outage vs not-found must be distinguishable
(proposal §16.2); request rows make retries idempotent; grants for
`VehicleLookupRequests` (`20260821095500_GrantWorkerVehicleLookupRequests`).

**What the desktop needs.** `~POST /api/v1/cases/{id}/vehicle-lookup`
(returns request id), `~GET .../vehicle` (evidence with provider state, cached
age), accept-suggestion command; registration normalisation rules copied to
the desktop (Core policy, shared assembly); no provider key in the package.

**Open questions.**

- Q5.1 Does the provider contract allow a direct public/native client call
  for any endpoint (proposal §12.3 says prove it; default no).
- Q5.2 Central cache lifetime for lookup results (defined where?).
- Q5.3 Rate-limit coordination across ten desktops — the Worker already
  serialises; confirm the gateway request path never calls the provider
  inline.

**Answers (2026-08-25).**

- Q5.1 **Answered.** No repository evidence authorizes a direct native
  provider call. The Web page model calls Core’s `IRequestVehicleLookup`, and
  the production `IVehicleLookupAdapter` is registered in the Worker; the
  package therefore receives no DVLA/DVSA credential and the default remains
  gateway/Worker mediation.
- Q5.2 **Answered.** No central provider-cache TTL is defined. The durable
  workflow records each observation’s `RetrievedAtUtc`, optional
  `EffectiveAtUtc` and `SourceObservedAtUtc`; lookup requests are idempotent
  durable work, not a client-side time-based cache. A future freshness policy
  must be accepted separately rather than inferred from the recorded source
  age.
- Q5.3 **Answered.** `OnPostRequestVehicleLookupAsync` calls
  `IRequestVehicleLookup`; `EfVehicleWorkflowStore` persists/enqueues the
  request, and the Worker’s queued-lookup processor owns the adapter
  invocation. The Web request does not call `DvlaDvsaProductionAdapter`
  inline. The reconciliation timer only enqueues missing lookups and uses the
  existing external-work queue.

**Read-only verification.**
`git grep -n "class DvlaDvsaProductionAdapter" src/Pegasus.Infrastructure/Vehicle/`,
`git grep -n "VehicleLookupRequests" src/Pegasus.Infrastructure/Persistence/Migrations | head`,
`git grep -n "Dvla__\|Dvsa__" infra/modules/platform.bicep`.

## Record 6 — Report rendering

**Purpose.** Reports are rendered server-side today; the decision (L-03) is
to render locally through an isolated WebView2 HTML→PDF path (ADR-0108),
keeping the gateway renderer until golden-file parity passes.

**Current entry points.** `Pages/Cases/Assessment/Index.cshtml.cs`
`OnPostGenerateReportDraftAsync`; Core `Reports/AssessmentReportRendering.cs`
(312) with `IAssessmentReportRenderer`; projection
`Reports/AssessmentReportProjection.cs` (362).

**Current code paths.**

- `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`
  (326): `AssessmentReportSnapshot` → Scriban templates
  (`assessment_report.scriban`, `assessment_fee_note.scriban`, `report.css`,
  embedded from `docs/design/assets/report-renderer/templates/`, plus brand
  logo and signature PNGs) → HTML → Playwright Chromium `PdfAsync` → PDFsharp
  post-processing → `*_assessment.pdf` and `*_fee_note.pdf`; serialised by a
  `SemaphoreSlim(1,1)`; browser lazily created and cached; `IAsyncDisposable`;
  registered singleton by `AddPegasusReportRendering()`
  (`DependencyInjection.cs:446`).
- Pins: `Directory.Build.props` `PlaywrightVersion 1.61.0` ↔
  `Pegasus.Web.csproj` `ContainerBaseImage
  mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` (ADR-0028, DELIV-012);
  Container App cpu 1.0 / 2 Gi for in-process Chromium.
- Templates: six `.scriban` files under
  `docs/design/assets/report-renderer/templates/` (`advert_evidence_pack`,
  `assessment_fee_note`, `assessment_report`, `expert_report`, `fee_note`,
  `market_valuation_evidence`) plus `report.css`, LF-forced by
  `.gitattributes`.
- Tests: `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs`,
  `AssessmentReportDraftWebTests.cs`; renderer workspace provenance
  `workspaces/README.md` (ADR-0025 folded CollisionRenderer in).
- Upstream decisions pending: TICK-206 (map templates to capabilities and
  retirements), TICK-216 (unaccepted wording/signature assets behind a gate),
  DOCS-001 (trigger generation from complete accepted assessments).

**Data owned.** Report drafts/final PDFs (canonical copy in Box via custody),
report revisions, frozen EVA bundle revisions; templates and brand assets in
the repository.

**Failure modes.** Chromium startup cost on first render; one render at a
time (semaphore); font/asset drift between container and local machine;
Container App memory.

**What the desktop needs.** The report model (projection) from the gateway
(`~GET /api/v1/cases/{id}/reports/model`), templates and CSS shipped in the
package, a hidden WebView2 render host that never shows Pegasus UI,
`CoreWebView2.PrintToPdfAsync` or `PrintToPdfStreamAsync`, PDFsharp
post-processing, golden-file tests against the Playwright output, a
registration endpoint for the final PDF (`~POST /api/v1/cases/{id}/reports`),
and the gateway renderer kept as fallback until parity (ADR-0108).

**Open questions.**

- Q6.1 Which templates are in scope for the desktop (TICK-206 outcome) and
  which remain unattended/server-side only.
- Q6.2 Print-to-PDF fidelity differences between WebView2 and Playwright's
  Chromium `PdfAsync` (page size, margins, headers/footers, fonts) — the
  Phase 7 spike measures against the fixture set.
- Q6.3 WebView2 runtime presence on the ten workstations (Windows 11 ships
  the Evergreen runtime; confirm versions and the fixed-version fallback).
- Q6.4 PDFsharp post-processing behaviour on WebView2 output (metadata,
  merge of fee note).

**Answers (2026-08-25).**

- Q6.1 **Moved to `docs/open-decisions.md` 2026-08-25.** TICK-206 remains a
  preparing decision ticket. The current repository asset scope is the six
  `.scriban` templates listed above plus `report.css`; the existing callers
  are `PlaywrightAssessmentReportRenderer.RenderPdfAsync` for
  `assessment_report.scriban` and `assessment_fee_note.scriban`, with the
  remaining templates retained as governed assets pending TICK-206’s
  capability mapping. No template is retired by this inventory ticket.
- Q6.2 **Answered.** Microsoft documents
  `CoreWebView2.PrintToPdfAsync(String, CoreWebView2PrintSettings)` and
  `PrintToPdfStreamAsync`; the settings cover page size, margins, scale,
  backgrounds, and headers/footers, and only one print operation may be in
  progress per WebView. Fidelity against the pinned Playwright renderer is
  not claimed here; Phase 7 golden-file testing measures it. Fetched
  2026-08-25 from [PrintToPdfAsync](https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.core.corewebview2.printtopdfasync?view=webview2-dotnet-1.0.4129.50)
  and [CoreWebView2PrintSettings](https://learn.microsoft.com/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2printsettings?view=webview2-winrt-1.0.4129.50).
- Q6.3 **Moved to `docs/open-decisions.md` 2026-08-25.** This repository
  contains no operator-run evidence for the ten target workstations, so it
  does not claim Evergreen runtime presence or a fixed-version fallback. The
  required workstation observation and owner are recorded in the decision
  register.
- Q6.4 **Answered.** The application pins `PDFsharp` 6.2.4 and uses
  `PdfReader.Open(..., PdfDocumentOpenMode.Import)` to combine the generated
  PDFs. Official PDFsharp documentation describes the library as able to
  modify, merge, and split PDFs and documents importing pages into a new
  document; it does not establish WebView2-vs-Playwright fidelity. That
  behavior remains a Phase 7 fixture test. Fetched 2026-08-25 from
  [PDFsharp features](https://docs.pdfsharp.net/PDFsharp/Overview/Features.html)
  and [PDFsharp encryption/import example](https://docs.pdfsharp.net/PDFsharp/Topics/PDF-Features/Encryption.html).

**Read-only verification.**
`git grep -n "PdfAsync\|PrintToPdf" src tests`,
`Get-ChildItem docs/design/assets/report-renderer/templates/`,
`git grep -n "PlaywrightVersion" Directory.Build.props src/Pegasus.Web/Pegasus.Web.csproj`,
Microsoft Learn `microsoft_docs_search "CoreWebView2 PrintToPdfAsync"` and
`microsoft_docs_fetch` for `CoreWebView2.PrintToPdfAsync` and
`CoreWebView2PrintSettings` (fetched 2026-08-25).
