# Research — FND-020: flow records 4–6

> **STATUS — COMPLETE FOR SPIKE SCOPE (2026-08-25).** This ticket is
> documentation/evidence only. It changes no product code, Azure state, Box
> content, provider state, or renderer assets. The three flow records now answer
> every Q4.x/Q5.x/Q6.x question or move the unresolved decision to
> `docs/open-decisions.md`.

## Question

For Box custody, DVLA/DVSA vehicle lookup, and report rendering, what does the
repository do today and what must be settled before [[FND-006]] and [[FND-007]]
author the ADRs for the desktop boundary?

## Evidence and answers

### Record 4 — Box custody

- `Box.Sdk.Gen` is pinned to 1.12.0 in
  `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj`. Reflection over
  the installed 1.12.0 assembly found these relevant named APIs:
  `DownloadsManager.GetDownloadFileUrlAsync`,
  `UploadsManager.PreflightFileUploadCheckAsync`,
  `ChunkedUploadsManager.CreateFileUploadSessionAsync` and URL-based
  upload-part/commit methods, and
  `BoxDeveloperTokenAuth.DownscopeTokenAsync`.
- The current production adapter is still the guarded `BoxContentClient` in
  `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs`; it uses the
  gateway/Worker authorization header, multipart upload, and Box content
  download calls. It does not expose a direct transfer URL to the desktop.
- Box’s official documentation says download URLs are temporary (normally
  about 15 minutes), and token exchange can restrict a token by resource and
  scopes such as `item_download` and `item_upload`. These facts prove
  available primitives, not approval to expose them. The default remains
  gateway streaming until the separate security/metadata spike proves the
  constrained direct-transfer contract.
- Core/EF metadata is already sufficient for the requested projection:
  `DocumentVersion`/`DocumentVersionEntity` provide `FileName`,
  `MediaType`, `ContentLength`, `CreatedBy`, `CreatedAtUtc`,
  `Sha256`, version, custody, current/removal state; the occurrence types
  provide `Source`, semantic role, source identity, and `RecordedAtUtc`.
- `PLAT-041` is still `backlog` in
  `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`. The
  desktop export endpoint must wait for the one-folder-per-export fix and is
  not exposed by this ticket.

Answers written to `flow-records.md`: Q4.1, Q4.2; Q4.3 moved to
`docs/open-decisions.md`.

### Record 5 — DVLA/DVSA vehicle lookup

- The Web page handler `OnPostRequestVehicleLookupAsync` calls Core’s
  `IRequestVehicleLookup`. `EfVehicleWorkflowStore` persists/enqueues the
  durable request. The Worker registers the production
  `IVehicleLookupAdapter` and its queued-lookup processor; the live
  `DvlaDvsaProductionAdapter` is not a Web inline caller.
- The repository has no central provider cache, TTL, or expiry policy. Durable
  observations record `RetrievedAtUtc`, optional `EffectiveAtUtc` and
  `SourceObservedAtUtc`; idempotency is provided by durable request/work-item
  identity, not a client cache.
- No DVLA/DVSA credential is present in the desktop package or Web container
  path. No repository evidence authorizes a direct public/native provider call.
  The literal disposition is: **no evidence found; default no**.
- The reconciliation timer enqueues missing lookups through the existing
  external-work path; it does not call the provider inline.

Answers written to `flow-records.md`: Q5.1, Q5.2, Q5.3.

### Record 6 — report rendering

- The authoritative asset directory contains six `.scriban` templates:
  `advert_evidence_pack`, `assessment_fee_note`,
  `assessment_report`, `expert_report`, `fee_note`, and
  `market_valuation_evidence), plus `report.css`.
- The current renderer calls `assessment_report.scriban` and
  `assessment_fee_note.scriban`; the other four assets remain governed
  repository assets pending TICK-206’s capability mapping. TICK-206 remains
  `preparing` in the carry-over table and has no fork ticket, so the
  capability/retirement question is recorded in `docs/open-decisions.md`.
- The production pin is `PlaywrightVersion 1.61.0` in
  `Directory.Build.props`; the Web project derives
  `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` through
  `ContainerBaseImage`. The duplicate integration-test package literal is
  [[FND-027]] scope.
- Microsoft Learn documents `CoreWebView2.PrintToPdfAsync`,
  `PrintToPdfStreamAsync`, and `CoreWebView2PrintSettings`; settings
  include page size, margins, scale, backgrounds, and headers/footers, and
  only one print operation may run per WebView. This is API evidence only:
  fidelity against Playwright is measured by the Phase 7 fixture/golden-file
  spike, not settled here.
- The application pins PDFsharp 6.2.4 and imports PDF pages with
  `PdfDocumentOpenMode.Import`. PDFsharp’s official documentation describes
  modifying/merging/splitting PDFs and importing pages into a new document; it
  does not establish WebView2-vs-Playwright fidelity.
- No operator evidence for ten target workstations is present. Q6.3 is
  therefore recorded in `docs/open-decisions.md`, with the required owner,
  OS-build, Evergreen-runtime, and fixed-version-fallback observation.

Answers written to `flow-records.md`: Q6.2 and Q6.4; Q6.1 and Q6.3 moved to
`docs/open-decisions.md`.

## Governing references

- `docs/frd/frd-05-documents-extraction-and-custody.md`
- `docs/frd/frd-06-vehicle-and-external-assessment.md`
- `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`
- `docs/desktop/01-inventory-and-parity/README.md`
- `docs/desktop/07-integrations/README.md`
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`
- Group context for EPIC-002 and the FND-020 ticket body

## Exact read-only validation

Run from the FND-020 worktree on 2026-08-25:

- `Get-ChildItem docs/design/assets/report-renderer/templates -File` listed
  six `.scriban` files and `report.css`.
- Reflection over
  `$env:USERPROFILE/.nuget/packages/box.sdk.gen/1.12.0/lib/net6.0/Box.Sdk.Gen.dll`
  listed the Box methods named above.
- `rg -n 'PlaywrightVersion|ContainerBaseImage' Directory.Build.props
  src/Pegasus.Web/Pegasus.Web.csproj` confirmed 1.61.0 and the noble image.
- `rg -n 'PDFsharp|PdfReader.Open' src/Pegasus.Infrastructure/Reports
  src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` confirmed PDFsharp
  6.2.4 and `PdfDocumentOpenMode.Import`.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` exited 0: all relative
  Markdown links resolve (232 files).
- `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD`
  exited 0: Markdown placement passed.
- `git diff origin/dev...HEAD --check` passed after the documentation commit.

Official references fetched 2026-08-25:

- Box: [Get Download URL](https://developer.box.com/guides/downloads/get-url),
  [Downscope a Token](https://developer.box.com/guides/authentication/tokens/downscope),
  [Preflight Check](https://developer.box.com/reference/options-files-content),
  [Uploads](https://developer.box.com/guides/uploads).
- Microsoft: [PrintToPdfAsync](https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.core.corewebview2.printtopdfasync?view=webview2-dotnet-1.0.4129.50),
  [CoreWebView2PrintSettings](https://learn.microsoft.com/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2printsettings?view=webview2-winrt-1.0.4129.50).
- PDFsharp: [features](https://docs.pdfsharp.net/PDFsharp/Overview/Features.html),
  [import example](https://docs.pdfsharp.net/PDFsharp/Topics/PDF-Features/Encryption.html).

## Scope and simplification

This is docs-only work. No simplification pass is applicable
(`n/a — docs-only`). No product code, test project, Azure resource, Box
location, provider, template, or runtime was changed.
