# 07 · Integrations — Graph intake, Box, DVLA/DVSA, mail, reports, OCR

Area plan for the external-system seams of the native desktop conversion.
It decides, per integration, what the desktop does locally, what the gateway
(`Pegasus.Web` evolved in place, locked decision L-01) brokers, and what stays
in the unattended Worker — and it carries the operator's locked decision L-03
that report rendering moves to the desktop through an isolated WebView2
HTML→PDF path. Routes named here are defined by
[03 · gateway API](../03-gateway-api-and-data/README.md); authentication by
[04 · auth, session, update and startup](../04-auth-session-update-and-startup/README.md);
the screens by [06 · UI design](../06-ui-design/README.md); the slices that
deliver them by [05 · implementation and migration](../05-implementation-and-migration/README.md).

## 1. Purpose and proposal coverage

| Proposal section | What this plan does with it |
| --- | --- |
| §4.1 placement rows — Graph intake, Box browsing, DVLA/DVSA lookup, report generation, scheduled work, file preview, OCR | Answers the six-question cloud-justification test per integration and records the split |
| §12.1 Microsoft Graph intake | Worker unchanged; gateway exposes status, failures and retry; no desktop Graph credential |
| §12.2 Box | Gateway brokers tokens, authorisation and metadata; desktop owns browser, transfer queue, preview, bounded cache; direct transfer only after a spike proves short-lived constrained URLs |
| §12.3 DVLA/DVSA | Gateway/Worker own keys, rate limits, cache; desktop validates input and shows provenance |
| §12.4 Email sending and other service actions | Desktop builds and confirms commands; gateway authorises, executes idempotently, audits provider ids |
| §12.5 Documents, PDFs and reports | **L-03**: local WebView2 HTML→PDF rendering of the existing Scriban templates; gateway renderer kept until golden-file parity |
| §12.6 OCR, image analysis and future AI | ONNX VRM engine stays server-side initially; local preprocessing is a later spike; no cloud AI introduced |
| §13.4 Intake, §13.5 Vehicle, §13.7 Documents, §13.8 Communications, §13.9 Assessment/reporting | Capability groups whose integration half is planned here (their screens are in 06, their slices in 05) |
| §16.2 External provider resilience | Provider error taxonomy and retry rules carried into every endpoint |
| §24 Phases 5, 6, 7 | Exit gates restated in section 4 |

## 2. Evidence base

### Facts

Repository (fork `main` @ `191ddf33`, inspected 2026-08-23):

- Worker functions are the only unattended callers: `IntakeFunctions.cs:13`
  `PendingWorkDispatchFunction` (timer), `:33` `IntakeWorkFunction`
  (queue `intake-work`), `:50` `IntakePoisonFunction`, `:75`
  `StagedArtifactReconciliationFunction`; `MailboxFunctions.cs:15`
  `InboxPollFunction`; `EmailEvidenceFunctions.cs:16`
  `SentEvidencePollFunction`, `:53` `DueWorkSweepFunction`;
  `Functions/ExternalWorkFunctions.cs:9` `ExternalWorkFunction` (queue
  `external-work`: vehicle lookup, custody), `:27` `ExternalPoisonFunction`.
  `src/Pegasus.Worker/host.json`: queues `batchSize 4`, `visibilityTimeout
  00:05:00`, `maxDequeueCount 5`, `maxPollingInterval 00:00:02`.
- Inbound Graph polling is per-mailbox with its own lease, cursor and
  last-failure code (ADR-0022, ADR-0024, FRD-08;
  `docs/current-architecture.md:444-469`); the poll writes the retained-mail
  read model once, between `ReceiveIntake` and the cursor advance
  (`src/Pegasus.Infrastructure/Persistence/EfRetainedMailboxMessageStore.cs`;
  `docs/current-architecture.md:470-478`). Deleted-Items search is a GET-only
  Graph read composed in the Web host
  (`AddProductionApprovedMailboxResolver`, `docs/current-architecture.md:104`).
  Folder moves use a narrow provider port that is unavailable by default and
  has no production writer (`docs/current-architecture.md:104`). Sent-evidence
  polling is configuration-driven for one mailbox
  (`docs/current-architecture.md:453`). Graph adapters:
  `src/Pegasus.Infrastructure/Email/GraphApprovedSources.cs` (1,125 lines).
- Box custody: `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs`
  (1,016 lines) and `BoxDocumentContentStore.cs` (240); immutable Case/PO
  folder names; predeclared creation-owner token in a transient staging
  folder and ETag-guarded same-parent promotion; durable folder identity is
  the stored remote folder id (`docs/current-architecture.md:528`). Custody
  retry is a separate human-only Core use case; no automatic business retry
  (`docs/current-architecture.md:526`, `:571`). Box credentials are Key
  Vault references on the Worker (`infra/modules/platform.bicep:555-556`)
  and Container App secrets on the Web (`platform.bicep:382-398`).
  `Box.Sdk.Gen` 1.12.0 is the SDK (`src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj`).
  Upstream `main` (`7d6a948a`) already carries the Box access-token refresh
  fix (PLAT-039, commits `79db11f`, `282ba44`) — it arrives with the first
  upstream sync ([01 · carry-over](../01-inventory-and-parity/upstream-kanmer-carryover.md)).
- DVLA/DVSA: `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs`
  (412 lines) plus the replay adapter in `DvlaDvsaAdapters.cs` (222);
  request→accept workflow in `src/Pegasus.Core/Vehicle/`; the lookup path is
  composed in both runtime profiles (Web records staff requests, the
  production Worker owns the live adapter) and since release 15 a
  reconciliation-timer sweep enqueues one lookup per active case whose
  registration has never been looked up (`docs/current-architecture.md:514`).
  Keys are Key Vault references on the Worker: `Dvla__ApiKey`,
  `Dvsa__ClientId`, `Dvsa__ClientSecret`, `Dvsa__ApiKey`
  (`infra/modules/platform.bicep:558-563`).
- Reports: `Pegasus.Core.Reports.IAssessmentReportRenderer` is implemented by
  `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`
  (326 lines): Scriban templates `assessment_report.scriban` and
  `assessment_fee_note.scriban` (`:74-75`) plus `templates.report.css`
  (`:146`) are embedded resources (`:313`) sourced from
  `docs/design/assets/report-renderer/templates/` (seven templates and
  `report.css`; `.gitattributes` pins them to LF); rendering is Chromium
  `page.PdfAsync` (`:120`), post-processed with PDFsharp
  (`PdfReader.Open`, `:133`), serialised behind `SemaphoreSlim(1,1)` (`:19`),
  registered as a singleton by `AddPegasusReportRendering()`
  (`src/Pegasus.Infrastructure/DependencyInjection.cs:446`) and run inside
  the Web Container App (ADR-0025, ADR-0028; `Pegasus.Web.csproj`
  `ContainerBaseImage = mcr.microsoft.com/playwright/dotnet:v1.61.0-noble`;
  container raised to cpu 1.0 / 2Gi for in-process Chromium,
  `platform.bicep:354-478`). Core owns the report model and finality:
  `src/Pegasus.Core/Reports/AssessmentReportRendering.cs` (312),
  `AssessmentReportProjection.cs` (362); FRD-11 owns correction/finality.
- OCR/vision: in-process ONNX VRM recognition (ADR-0019) in
  `src/Pegasus.Infrastructure/Vision/` (models embedded); no external OCR or
  Document Intelligence is called (`docs/current-architecture.md:149-150`,
  `:263`).
- Outbound mail today is limited to exact sent-message evidence; MAIL-12/13/17/19
  (compose, mailbox mutation, idempotent report send, automatic chasers) are
  open upstream capabilities, not conversion scope
  (`docs/capabilities.md`; [01 · carry-over](../01-inventory-and-parity/upstream-kanmer-carryover.md)).
- Provider failure semantics already in force: clients distinguish
  `terminal`, `transient`, `unknown`; terminal stops retries; metrics count
  effects, not attempts (`docs/current-architecture.md:85-90`).

Official documentation (fetched 2026-08-23):

- WebView2 printing to PDF: <https://learn.microsoft.com/microsoft-edge/webview2/how-to/print>
  and the WinRT reference for `CoreWebView2.PrintToPdfAsync` /
  `PrintToPdfStreamAsync` / `CoreWebView2PrintSettings`
  (<https://learn.microsoft.com/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2>)
  — one print operation per WebView at a time; `PrintToPdfStreamAsync`
  returns a rewound PDF stream; settings cover margins, page size,
  backgrounds, header/footer, scale.
- App Installer and the no-WebView rule are covered in 04 and 09; the
  proposal's own §23.2 permits an isolated WebView2 for a specific document
  render when an ADR records it and it never hosts Pegasus UI.

### Assumptions

- WebView2 runtime is present on every target Windows 11 workstation (it
  ships with Windows 11); the startup check in 04 confirms it and names the
  install step when absent.
- `Box.Sdk.Gen` 1.12 can exchange the service token for a downscoped,
  resource-bound token (Box token exchange) usable for a single file
  download/upload — **unverified**; it is the subject of ticket DSK-07-07.
- Scriban output rendered by Chromium inside WebView2 matches Playwright's
  Chromium closely enough that golden-file tolerances (text, values, page
  count, key positions) pass; exact pixel equality is not the target.
- DVLA/DVSA terms do not permit a public/native client call without the
  API key — treated as true until the provider contract is read (ticket
  DSK-07-10 records the check).

## 3. Decisions and assumptions

| Decision | Source | Effect here |
| --- | --- | --- |
| L-01 gateway = `Pegasus.Web` in place | index | Every "gateway" endpoint below lives under `/api/v1` in `Pegasus.Web`, flag-gated (`Features:DesktopGateway`, see 03) |
| **L-03 local report rendering via isolated WebView2** | index | Section "Reports" below; ADR-0108; gateway renderer retained until parity |
| ADR-0106 Graph worker stays central | 00 | No desktop poller; no Graph change notifications initially |
| ADR-0107 Box and DVLA/DVSA credential boundary | 00 | No long-lived Box/DVLA/DVSA/Graph secret in the package; gateway brokers |
| ADR-0108 isolated WebView2 HTML→PDF rendering | 00 | The §23.2 exception recorded: never hosts Pegasus UI, off-screen, one purpose |
| D-002/D-003 signing and feed hosting | 09 | No effect on integrations except that the WebView2 runtime check is part of first-run |

Deviations and placement answers:

- **Deviation (L-03):** proposal §12.5 preferred that "HTML may remain an
  internal document-template format only if it is the most reliable
  renderer" and pushed toward native rendering. The operator chose WebView2
  HTML→PDF because the seven Scriban templates, `report.css`, brand logo and
  signatures already exist, are governed in `docs/design/assets/report-renderer/`,
  and pass today's renderer tests; re-laying them out in a native PDF
  library would duplicate a governed asset set. Recorded in ADR-0108.
- **Deviation (Box direct transfer):** §12.2 allows direct desktop↔Box bytes
  "when safely supported". Default here is *stream through the gateway*;
  direct transfer is enabled only if the spike (DSK-07-07) proves a
  short-lived, file-scoped token and the gateway can still record canonical
  metadata and audit.
- **Deviation (OCR/ONNX):** §12.6 defaults user-invoked preprocessing to the
  desktop; the ONNX engine lives in `Pegasus.Infrastructure` with embedded
  models and an accepted evaluation (ADR-0019). Moving it is a later spike
  (DSK-07-16); placement stays server-side until then.
- Cloud-justification answers (proposal §4): Graph intake — shared authority
  yes, unattended yes, protected credentials yes → cloud. Box — protected
  credentials yes, shared authority (metadata) yes → split. DVLA/DVSA —
  protected credentials yes, shared cache yes → split. Outbound mail —
  protected credentials yes, central enforcement (idempotent send, audit)
  yes → split. Report rendering — all six no → desktop (with canonical
  storage through the gateway). OCR/VRM — measured operational advantage
  today yes (accepted engine in place) → cloud for now.
- ⚠ Azure writes in this area: none required. Exposing intake status,
  Box brokering and DVLA/DVSA through the gateway reuses the Web Container
  App's existing identity, secrets and role assignments. A Key Vault
  secret rotation or role change would be an exact-target write and is not
  planned here.

## 4. Target state and exit gate

Target state:

- Graph intake keeps arriving while every desktop is closed; the desktop
  shows per-mailbox last successful cycle, failures and poison counts and
  can trigger the existing human retries through the gateway.
- Box documents are browsed, previewed, uploaded and downloaded from the
  desktop through gateway-brokered sessions with progress and cancellation;
  a bounded local working cache never masquerades as custody; conflicts and
  failed transfers are visible and retryable; no Box token in the package.
- Vehicle lookups are requested and accepted from the desktop; results show
  source and timestamp; provider failure is distinguishable from "not
  found"; keys stay in Key Vault.
- Reports render on the desktop through an isolated WebView2 from the same
  templates the gateway embeds, preview locally, and are finalised by
  uploading the PDF through the gateway into Box with the report record and
  audit; golden-file fixtures match; the gateway renderer remains only as
  the recorded fallback until parity is signed off.

Exit gates (proposal §24):

| Phase | Gate | Proof |
| --- | --- | --- |
| 5 | Intake arrives while desktop closed; duplicate and failure paths pass; no desktop holds Graph credentials; full source-to-case traceability | Worker integration tests (`MailboxIntakeIntegrationTests.cs`), gateway contract tests, package secret scan, UAT script |
| 6 | Large and failed transfers recover safely; provider secrets absent from package; provider rate/error handling passes; document parity approved | Transfer-queue tests with injected failures, secret scan of the MSIX, DVLA/DVSA replay tests, parity matrix rows |
| 7 | Approved fixtures match expected values/content; no required report depends on the web renderer unless explicitly retained; final document and audit correct; performance target on baseline hardware | Golden-file tests, report upload audit test, performance report ([10](../10-security-observability-performance/README.md)) |

## 5. Work breakdown

Tier numbers follow `docs/engineering.md` § Required evidence tiers.
Routing = subagent · skills · MCP.

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-07-01 | Gateway intake-status endpoints: per-mailbox last cycle, failures, poison counts, retry eligibility (`/api/v1/operations/intake-status`, `/api/v1/operations/external-work`) | feature | 03 skeleton | Read models come from the existing Operations projection and retained-mail queries; no new Worker code; failures carry `terminal/transient/unknown` | Contract tests; LocalDB integration test with seeded failures | 5 | pegasus-gateway-dev · dotnet-webapi, microsoft-code-reference · Learn, Kanmer |
| DSK-07-02 | Human retry commands through the gateway (retry external work, retry custody, retry allocation) with operation keys and audit | feature | DSK-07-01 | Each retry maps to the existing Core use case; replay with the same key is idempotent; denied without the right | Contract + authorization failure tests | 5 | pegasus-gateway-dev · dotnet-webapi · Learn, Kanmer |
| DSK-07-03 | Mail endpoints reuse: list, preview, message detail, link/unlink, classify, move-to-recommended (behind the unavailable provider port) | feature | 03 skeleton | Same Core owners as `Pages/Mail/*`; Deleted-Items search stays GET-only Graph via Web; move control absent when provider unavailable | `MailWorkspaceWebTests.cs` parity; contract tests | 5 | pegasus-gateway-dev · dotnet-webapi · Learn, Kanmer |
| DSK-07-04 | Desktop intake/operations status surface (Operations screen) bound to DSK-07-01/02 | feature | DSK-07-01 | Shows cached/obtained time; retry buttons only when eligible; disconnected state honest | VM tests; `winapp ui` script | 7 | winui-dev · winui-dev-workflow, winui-design · Learn |
| DSK-07-05 | Box broker endpoints: list case documents, metadata, download session, upload session (multipart or chunked), remove, confirm third-party evidence | feature | 03 skeleton | Authorisation checks the Pegasus case/document right before any Box call; canonical metadata and action history written; streaming through gateway | Contract tests; fake Box adapter integration tests; size limits enforced | 5 | pegasus-gateway-dev · dotnet-webapi, minimal-api-file-upload · Learn, Kanmer |
| DSK-07-06 | Desktop document browser, transfer queue with progress/cancel/retry, preview pane, bounded local working cache with ACLs and retention | feature | DSK-07-05, 06 specs | Local copy clearly distinct from canonical; no hidden overwrite; failed transfers retryable; cache bounded and purged | VM tests with injected transfer failures; `winapp ui` script; manual large-file run | 7 | winui-dev · winui-dev-workflow, winui-design · Learn |
| DSK-07-07 | Spike: can `Box.Sdk.Gen` 1.12 issue a short-lived, file-scoped downscoped token so the desktop can move bytes directly? | spike | DSK-07-05 | Written answer with SDK evidence, lifetime, scope, audit implications; default (gateway streaming) confirmed or a follow-up ticket raised | Spike research doc in the ticket; no production change | 3 | pegasus-gateway-dev · microsoft-code-reference · Learn, Kanmer |
| DSK-07-08 | Box conflict and version handling: detect newer canonical version before overwrite; surface conflict in desktop | feature | DSK-07-05, DSK-07-06 | No silent overwrite; conflict shows both versions' metadata | Integration test with concurrent upload | 5 | pegasus-gateway-dev, winui-dev · dotnet-webapi, winui-design · Learn |
| DSK-07-09 | DVLA/DVSA gateway endpoints: request lookup, accept suggestion, status; cache lifetime and provenance fields in the contract | feature | 03 skeleton | Provider failure ≠ not found; every call has correlation id; keys never leave Key Vault; replay adapter used in `DevelopmentOffline` | Contract tests; replay-adapter integration tests | 5 | pegasus-gateway-dev · dotnet-webapi · Learn, Kanmer |
| DSK-07-10 | Desktop vehicle workflow: VRM normalisation/validation, request, accept, source+timestamp display; record the provider-contract check that no direct desktop call is permitted | feature | DSK-07-09, 06 specs | Input normalised once (shared rule from Core); stale/cached state visible; contract check recorded | VM tests; `winapp ui` script | 7 | winui-dev · winui-design · Learn |
| DSK-07-11 | Outbound command pattern: desktop confirms, gateway authorises and executes with idempotency key, provider message id audited (sent evidence today; seam for MAIL-17 later) | feature | 03 skeleton | Duplicate send impossible by key; draft/queued/sent/failed states distinct | Contract tests with replayed key | 5 | pegasus-gateway-dev · dotnet-webapi · Learn, Kanmer |
| DSK-07-12 | ADR-0108 isolated WebView2 HTML→PDF rendering (scope, never-UI rule, fallback, parity gate) | chore | 00 ADR block | Accepted ADR with the §23.2 statement and reversal condition | Docs review | 1 | pegasus-desktop-reviewer · kanmer-docs · Kanmer |
| DSK-07-13 | Share templates once: build step embeds `docs/design/assets/report-renderer/templates/*` into both `Pegasus.Infrastructure` and `Pegasus.Desktop.Infrastructure` (one source, hash-checked) | feature | 02 projects | Both assemblies embed byte-identical resources; CI fails on drift | Resource-hash test in both test projects | 1 | pegasus-release-packager · directory-build-organization · Learn |
| DSK-07-14 | Desktop renderer: `IAssessmentReportRenderer` implementation in `Pegasus.Desktop.Infrastructure` using Scriban + isolated WebView2 (`PrintToPdfStreamAsync`) + PDFsharp post-processing; spike first whether a collapsed WinUI `WebView2` control or a `CoreWebView2Controller` on a hidden HWND is the cleaner off-screen host | feature | DSK-07-12, DSK-07-13, 02 | Renders assessment and fee note from the same snapshot; no visible UI; one render at a time; runtime-missing → named failure and gateway fallback | Golden-file tests (DSK-07-15); manual render on baseline hardware | 3 | winui-dev · winui-dev-workflow, microsoft-code-reference · Learn (WebView2 print docs), Kanmer |
| DSK-07-15 | Golden-file parity suite: fixtures from the Playwright renderer (text, values, page count, key element positions within tolerance) compared with WebView2 output | feature | DSK-07-14 | All approved fixtures pass; tolerances documented; renderer tests in `tests/Pegasus.IntegrationTests/Reports/` reused for the baseline | `dotnet test` suite with fixture catalogue | 3 | pegasus-test-engineer · code-testing-agent, run-tests, assertion-quality · Learn |
| DSK-07-16 | Report finalise endpoint: upload desktop PDF → Box custody + report record + audit, with FRD-11 finality/regeneration rules; desktop preview/finalise UX | feature | DSK-07-05, DSK-07-14 | Final document stored once; regeneration audited; web renderer path retained behind a flag until parity sign-off | Contract tests; custody integration test; UAT | 5 | pegasus-gateway-dev, winui-dev · dotnet-webapi, winui-design · Learn, Kanmer |
| DSK-07-17 | Carry-over disposition tickets: DOCS-001, TICK-206, TICK-208, TICK-214, TICK-216, TICK-081/096/097/100, DOCS-003/004 reconciled against L-03 (which templates ship, which retire, what stays gated) | chore | DSK-07-12 | Each upstream ticket has a disposition recorded in [01 · carry-over](../01-inventory-and-parity/upstream-kanmer-carryover.md) and either a fork ticket or "unchanged backlog" | Docs review | 1 | pegasus-parity-researcher · kanmer-tickets · Kanmer |
| DSK-07-18 | Spike: desktop-side ONNX VRM/image preprocessing placement (engine move, model size, accuracy parity, fleet CPU) | spike | Phase 6 slices | Written recommendation; no engine move without an accepted ADR | Spike research doc | 2 | winui-dev · microsoft-code-reference · Learn |
| DSK-07-19 | Provider error taxonomy in contracts: `terminal` / `transient` / `unknown` plus `not-found`, `invalid-request`, `not-authorized`, `rate-limited`, `unavailable` problem types for every integration endpoint | feature | 03 skeleton | One list in `Pegasus.Contracts`; desktop maps each to a state without colour-only meaning | Contract snapshot; VM tests | 3 | pegasus-gateway-dev, pegasus-test-engineer · dotnet-webapi, test-gap-analysis · Learn |

## 6. Routing table

| Work type | Subagent | Skills (pinned source) | MCP tools |
| --- | --- | --- | --- |
| Gateway endpoints (intake status, Box broker, DVLA/DVSA, outbound, report finalise) | `pegasus-gateway-dev` | `dotnet-webapi`, `minimal-api-file-upload` (dotnet/skills `98f84851`, plugin dotnet-aspnetcore); `microsoft-code-reference` (Microsoft Learn plugin) | Microsoft Learn `microsoft_docs_search`/`microsoft_code_sample_search` (Graph, Box SDK, WebView2); Kanmer `get_doc_gates`, `set_ticket_doc` |
| Desktop surfaces (Operations, documents browser, vehicle, report preview) | `winui-dev` | `winui-dev-workflow`, `winui-design` (+`winui-search.exe`) (win-dev-skills v0.5.0 `f1028dd5`) | Microsoft Learn (WebView2 printing, WinUI controls) |
| WebView2 renderer | `winui-dev` then `pegasus-desktop-reviewer` | `winui-dev-workflow`, `microsoft-code-reference`, `winui-code-review` (WebView2 interop rules `WUI4xxx`) | Microsoft Learn `microsoft_docs_fetch` on the print how-to and `CoreWebView2` reference |
| Golden-file and integration tests | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `assertion-quality`, `test-gap-analysis` (dotnet/skills, plugin dotnet-test) | — |
| Secret and resource checks (Key Vault references, storage containers) | `pegasus-azure-auditor` (read-only) | `azure-resource-lookup`; `azure-storage` only to read blob semantics (azure-skills `1a03acfb`) | Azure MCP read-only: `keyvault` (list, no values), `storage`, `group_resource_list` |
| Carry-over and ADR authoring | `pegasus-parity-researcher`, `pegasus-desktop-reviewer` | `kanmer-tickets`, `kanmer-docs` | Kanmer `create_item`, `link_doc` |

Not applicable here: `azure-messaging` (no Service Bus/Event Hubs exist —
`docs/operations.md:87` forbids adding them), `entra-app-registration`
(no Microsoft login), `azure-ai` (no cloud AI introduced).

## 7. Risks and traps

| Risk / trap | Mitigation |
| --- | --- |
| WebView2 off-screen hosting: a WinUI `WebView2` control needs a XAML root; a zero-size collapsed control may still initialise, but behaviour must be proven (DSK-07-14 spike); `CoreWebView2Controller` on a hidden HWND is the fallback host | Spike first; record the chosen host in ADR-0108; keep the renderer behind `IAssessmentReportRenderer` so the host can change |
| One print operation per WebView at a time (docs) — parallel renders throw | Same `SemaphoreSlim(1,1)` discipline as the Playwright renderer; queue renders |
| WebView2 runtime missing or outdated on a workstation | Startup check (04) with a named install step; gateway render fallback until fixed |
| Golden-file drift between Chromium builds (WebView2 runtime updates itself; Playwright is pinned to 1.61.0) | Tolerant comparisons (text, values, page count, positions within tolerance), fixture review on failure, not pixel equality |
| Template duplication: a second copy of the Scriban/CSS set in the desktop would break the one-list rule | DSK-07-13 embeds from the single governed source; hash test fails on drift |
| Box token in the package, or a "temporary" long-lived URL left in logs | Secret scan of the MSIX and logs (10); spike gate before any direct transfer |
| Custody retry automated "for convenience" | Forbidden: custody retry is human-only (`docs/current-architecture.md:571`); the desktop only exposes the existing use case |
| Poison-queue visibility lost behind a friendly status | Operations surface shows poison counts and last failure code; never collapses `unknown` into success |
| Graph credential drift after the first upstream sync (PLAT-039 token refresh) | Sync before Phase 6 work starts; rerun `MailboxIntakeIntegrationTests.cs` |
| Runtime-role GRANT trap: any new table (e.g. report-finalise records) needs a `Grant*` migration | `scripts/Test-MigrationGrants.ps1` in CI; PLAT-035 check in 10 |
| App Insights blind hours hide provider errors in production (PLAT-034) | Desktop diagnostics bundle carries correlation ids; gateway logs structured events; see 10 |
| Scope creep into MAIL-12/13/17/19, EXT-xx, AI-xx | Out of conversion scope (proposal §13.11); only the outbound command seam is built |

## 8. Documentation changes

- ADR-0107 (Box and DVLA/DVSA credential boundary) and ADR-0108 (isolated
  WebView2 HTML→PDF rendering) authored per 00; ADR-0106 cites the Worker
  facts above.
- FRD-05 (documents, extraction, custody), FRD-06 (vehicle evidence),
  FRD-08 (mailbox), FRD-11 (reports) gain the desktop behaviour clauses
  (transfer queue states, local working copy vs canonical, report preview
  and finalise) — behaviour, not mechanism.
- `docs/capabilities.md`: `DSK` rows for intake status, document transfer,
  vehicle lookup, local report rendering; existing DOC/RPT/MAIL rows keep
  their owners.
- `docs/current-architecture.md`: after each slice ships, the renderer
  composition (desktop + retained gateway fallback), the gateway broker
  endpoints and the embedded-template sharing step.
- `docs/operations.md`: no change until a production release carries these
  endpoints; then the release row and any flag state.
- [01 · carry-over](../01-inventory-and-parity/upstream-kanmer-carryover.md):
  dispositions for the report-decision tickets listed in DSK-07-17.
