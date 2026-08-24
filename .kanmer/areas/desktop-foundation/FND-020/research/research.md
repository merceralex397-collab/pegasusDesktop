# Research — FND-020: flow records 4–6 (Box custody, DVLA/DVSA lookup, report rendering)

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This document is the spike's **output**. `get_doc_gates FND-020` resolves profile
`spike` to one gated boundary — `enter-done` needs `research` and
`questions-resolved` — so its existence is what would let the ticket close. It is a
pre-work scaffold: everything under **Facts** was verified by a read-only command
quoted beside it, every answer the ticket owes is a literal `NOT YET CAPTURED`
block, and `open-questions` carries one unticked `- [ ]` box per uncaptured item.

## Question

For the three flows that decide what the desktop may do directly and what must stay
behind the gateway — Box custody, DVLA/DVSA vehicle lookup and report rendering —
what does the code do today, and what must be settled before [[FND-006]] (plan handle
`DSK-00-06`) can author ADR-0107 and [[FND-007]] (plan handle `DSK-00-07`) can author
ADR-0108? Two guesses are specifically dangerous: that `Box.Sdk.Gen` can issue
constrained short-lived transfer URLs when it cannot, and that WebView2 print
fidelity matches Playwright's when nobody has measured it.

## Current behaviour

### Record 4 — Box custody

- `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` (1,016 lines) is the
  canonical adapter; `using Box.Sdk.Gen;` at `:7` is the only direct SDK import in
  the Custody folder (`grep -n "^using Box" src/Pegasus.Infrastructure/Custody/*.cs`).
  `BoxDocumentContentStore.cs` (240) is the content store;
  `LocalCaseCustody.cs` (549) and `LocalDocumentContentStore.cs` (183) are the
  DevelopmentOffline equivalents.
- The unresolved-Key-Vault-reference guard is real and is where upstream `PLAT-013`
  bites: the predicate `IsUnresolvedKeyVaultReference` is at
  `BoxCaseCustody.cs:82-84`, and the named throw it feeds is at `:44-51`.
- The Worker owns the live custody work:
  `src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs:9`
  (`[Function(nameof(ExternalWorkFunction))]`).
- Credentials: the Web container app resolves `box-config-json` and
  `box-client-secret` as Key Vault-backed Container App secrets
  (`infra/modules/platform.bicep:382-397`) and binds them at `:427-428`; the Worker
  uses `@Microsoft.KeyVault(SecretUri=…)` app settings at `:555-556`.

Parity row: **`PAR-13`** — `Cases/Custody.cshtml.cs` (270) with
`OnPostRetryCustodyAsync`, `OnPostUploadDocumentAsync`, `OnPostRemoveDocumentAsync`,
`OnPostConfirmThirdPartyVehicleEvidenceAsync`, `OnPostCreateRequestUploadLinkAsync`,
`OnPostRevokeRequestUploadLinkAsync`
(`docs/desktop/01-inventory-and-parity/parity-matrix.md:58`).

### Record 5 — DVLA/DVSA vehicle lookup

- `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs` (412) is the live
  adapter; `Vehicle/DvlaDvsaAdapters.cs` (222) holds the DevelopmentOffline replay
  adapter. Core owns the contracts:
  `src/Pegasus.Core/Vehicle/LookupContracts.cs`, `LookupWorkItem.cs`,
  `VehicleMileagePolicy.cs`, `VehicleWorkflow.cs` — four files, no cache type.
- Credentials: `Dvla__BaseUri`/`Dvla__ApiKey` at `infra/modules/platform.bicep:557-558`
  and `Dvsa__BaseUri`/`TokenUri`/`ClientId`/`ClientSecret`/`ApiKey`/`Scope` at
  `:559-564`, every secret a `@Microsoft.KeyVault(SecretUri=…)` reference on the
  **Worker only**. The Web container app carries no DVLA/DVSA secret at all
  (`:382-397` holds three secrets, none of them a provider key) — which is already
  most of the answer to `Q5.3`.

Parity row: **`PAR-14`** — `Cases/Vehicle.cshtml.cs` (149) with
`OnPostRequestVehicleLookupAsync`, `OnPostAcceptVehicleSuggestionAsync`,
`OnPostGenerateEvaHandoffAsync` (`parity-matrix.md:59`).

### Record 6 — report rendering

- `src/Pegasus.Core/Reports/AssessmentReportRendering.cs` (312) declares
  `IAssessmentReportRenderer`; `AssessmentReportProjection.cs` (362) builds the
  snapshot; `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`
  (326) is the only implementation, serialised behind a `SemaphoreSlim(1,1)` with a
  lazily created, cached browser.
- Pins: `Directory.Build.props:17` `<PlaywrightVersion>1.61.0</PlaywrightVersion>`;
  `src/Pegasus.Web/Pegasus.Web.csproj:28`
  `<ContainerBaseImage>mcr.microsoft.com/playwright/dotnet:v$(PlaywrightVersion)-noble</ContainerBaseImage>`
  — the two agree by property substitution, not by a copied literal (ADR-0028).

Parity row: **`PAR-15`** — `Cases/Assessment/Index.cshtml.cs` (740), whose
`OnPostGenerateReportDraftAsync` is the render entry point (`parity-matrix.md:60`).

## Findings

- Record 6's template count is wrong in the published record and the ticket is right
  to correct it: six `.scriban` files plus `report.css`, not seven `.scriban` files
  (F-1).
- The Playwright pin is already single-sourced on the production side, and the one
  place it can desynchronise is a test project — a finding that belongs to
  [[FND-027]] (plan handle `DSK-02-02`), not to this ticket (F-2).
- `Q5.2` looks like a lookup and is really a "there is no such thing" answer: no
  cache, TTL or expiry concept exists anywhere under `src/Pegasus.Core/Vehicle/` or
  `src/Pegasus.Infrastructure/Vehicle/` (F-4). Idempotency comes from the durable
  request rows instead.
- `Q5.3`'s answer is already half-proved from infrastructure: the Web app holds no
  provider secret, so the gateway request path *cannot* call the provider inline even
  if a code path tried (F-5). The code trace still has to be done.
- `Q6.3` is an **operator** answer, not a code answer, and cannot be captured by any
  command in this repository (U-8).

### Facts

Each fact carries the command that produced it. Run in
`C:\Users\PC\Documents\GitHub\pegasusDesktop` on 2026-08-24 at `bbd1c549`.

- **F-1 — six templates plus one stylesheet; the record's "seven `.scriban` files"
  is wrong.** `ls docs/design/assets/report-renderer/templates/` →
  `advert_evidence_pack.scriban`, `assessment_fee_note.scriban`,
  `assessment_report.scriban`, `expert_report.scriban`, `fee_note.scriban`,
  `market_valuation_evidence.scriban`, `report.css`. That is **six** `.scriban` files
  and `report.css`. `flow-records.md:390-395` says "seven `.scriban` files … with
  `report.css`" and lists only six names followed by an ellipsis; the ellipsis is the
  error. Correct the record to six plus the stylesheet, exactly as the ticket's
  acceptance criterion requires.
- **F-2 — the Playwright pin is `1.61.0`, single-sourced on the production side and
  duplicated once in a test project.**
  `git grep -n "PlaywrightVersion" Directory.Build.props src/Pegasus.Web/Pegasus.Web.csproj`
  → `Directory.Build.props:17: <PlaywrightVersion>1.61.0</PlaywrightVersion>` and
  `src/Pegasus.Web/Pegasus.Web.csproj:28:
  <ContainerBaseImage>mcr.microsoft.com/playwright/dotnet:v$(PlaywrightVersion)-noble</ContainerBaseImage>`.
  `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:26` also derives from the
  property. The one literal repeat is
  `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:17`
  (`<PackageReference Include="Microsoft.Playwright" Version="1.61.0" />`). Record the
  version and the base-image tag in the record as the ticket asks; removing the
  duplicate literal is [[FND-027]]'s (plan handle `DSK-02-02`) acceptance criterion,
  not this ticket's edit.
- **F-3 — the named `@Microsoft.KeyVault(` failure path still exists as the record
  describes.** `git grep -n "Microsoft.KeyVault(" src/Pegasus.Infrastructure` →
  `Custody/BoxCaseCustody.cs:46` (the explanatory comment) and
  `Custody/BoxCaseCustody.cs:84` (the predicate
  `value.TrimStart().StartsWith("@Microsoft.KeyVault(", StringComparison.OrdinalIgnoreCase)`).
  The throw the predicate guards is at `:44-51`. Upstream `PLAT-013` is the ticket
  that produced this guard.
- **F-4 — there is no vehicle-lookup cache anywhere.**
  `grep -rn -i "cache\|ttl\|expiry\|expire" src/Pegasus.Core/Vehicle/` returns only
  `LookupContracts.cs:9` and `:116`/`:134` (`VehicleLookupOutcome.Throttled`),
  `LookupContracts.cs:49` (`DateOnly? ExpiryDate` — a *vehicle* document expiry, not a
  cache), `LookupWorkItem.cs:22` (`LeaseExpiresAtUtc` — a work lease) and
  `VehicleMileagePolicy.cs:107` (a comment). `grep -rn -i "cache" src/Pegasus.Infrastructure/Vehicle/`
  returns nothing at all. So the honest `Q5.2` answer is that no central cache
  lifetime is defined, because there is no cache: repeat safety comes from the
  durable, idempotent lookup request row per case and registration.
- **F-5 — no DVLA/DVSA secret reaches the Web container app.**
  `grep -n "Dvla__\|Dvsa__\|Box__" infra/modules/platform.bicep` puts every
  `Dvla__`/`Dvsa__` setting at `:557-564`, inside the **Worker** app settings; the Web
  container app's own secret list at `:382-397` holds `box-config-json`,
  `box-client-secret` and `automation-mcp-client-secret` and nothing else. An inline
  provider call from the gateway request path would have no credential to make it
  with. This is evidence for `Q5.3`, not a substitute for the code trace.
- **F-6 — the Worker owns both live external paths.**
  `grep -rn '\[Function(' src/Pegasus.Worker --include=*.cs` →
  `Functions/ExternalWorkFunctions.cs:9` `ExternalWorkFunction` and `:27`
  `ExternalPoisonFunction`, alongside the seven intake/mail functions. Custody work
  and vehicle lookup both arrive through `ExternalWorkFunction`.
- **F-7 — the Box adapter's SDK surface is narrow and centralised.**
  `grep -n "^using Box\|Box\.Sdk" src/Pegasus.Infrastructure/Custody/*.cs` returns a
  single hit: `BoxCaseCustody.cs:7: using Box.Sdk.Gen;`. Whatever `Q4.1` concludes
  about constrained URLs, the SDK types are reachable from one file, which bounds the
  reading the implementer has to do.
- **F-8 — the Core document surface is two files.**
  `ls src/Pegasus.Core/Documents/` → `DocumentContracts.cs`, `RequestUploadPolicy.cs`.
  `Q4.2`'s field inventory starts in `DocumentContracts.cs` and continues in the
  document entity configuration inside
  `src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs`.

### Assumptions

- **A-01-5 — `Box.Sdk.Gen 1.12.0` cannot issue a constrained, short-lived
  upload/download URL suitable for direct desktop transfer (`Q4.1`).** Confirmed by
  reading the SDK types reachable from `BoxCaseCustody.cs` and checking Box's own
  published API documentation for a scoped-token or pre-signed-URL operation. If it is
  wrong the desktop can transfer directly and the Container App does not need sizing
  for streaming; if it is right, the record's own consequence applies — stream
  through the gateway and size the Container App accordingly. Do **not** state this as
  a fact until the SDK surface has been read.
- **A-01-6 — the document metadata for file type, size, source, uploader and
  timestamp is partly present and partly projection work (`Q4.2`).** Confirmed by
  listing each of the five fields against `src/Pegasus.Core/Documents/DocumentContracts.cs`
  and the document entity, with `path:line` for the ones that exist. Breaks the
  §14.6 Documents-tab design if a field turns out to be derivable only from Box.
- **A-01-7 — no provider contract permits a direct call from a public/native client
  (`Q5.1`).** Proposal §12.3 sets this as the default and requires an exception to be
  proved. Confirmed by finding contrary evidence, or by recording "no evidence found;
  default no". Breaking it would mean shipping a provider key in an MSIX, which
  ADR-0107 exists to forbid.
- **A-01-8 — WebView2 print-to-PDF fidelity against `PlaywrightAssessmentReportRenderer`
  output is unknown and is measured by the Phase 7 spike, not here (`Q6.2`, `Q6.4`).**
  Confirmed only by the Phase 7 measurement. Presenting a documentation page as
  evidence of fidelity is the specific defect this assumption exists to prevent: the
  Microsoft Learn pages document the API surface and settings, never that two Chromium
  builds produce byte-comparable PDFs.

## Execution placement

This ticket writes documents and places no responsibility itself. The six-question
test is answered below for the **responsibilities the three records describe**, so
that ADR-0107 and ADR-0108 can take the answers directly. A "yes" names *where* the
responsibility lands; on this programme that is often a host that already exists.

### Responsibility D — Box credentials and document transfer

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **Yes** | Box folders are the canonical document store every operator sees; folder identity is the database-stored remote folder id (record 4, "Data owned"). |
| Unattended execution | **Yes** | Custody work runs through `ExternalWorkFunction` (`src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs:9`) with every desktop closed. Lands on the existing always-on Worker, already *Retain* in the register. |
| Protected credentials | **Yes** | `Box__ConfigJson` and `Box__ClientSecret` are Key Vault-backed (`infra/modules/platform.bicep:382-397`, `:555-556`). A Box JWT configuration inside an MSIX on ten workstations is exactly what ADR-0107 forbids. |
| Public callback | **No** | Nothing at Box calls Pegasus back; the adapter calls out. |
| Central enforcement | **Yes** | Case-level authorization, the immutable folder-identity rule and the custody outbox are enforced server-side; custody retry is a human-only Core use case with no automatic business retry. |
| Measured operational advantage | **No measured evidence** | None collected. The four "yes" answers already place it, so no measurement is owed. |

**Placement:** gateway and Worker. The desktop gets brokered endpoints (list,
metadata, upload session, content download, export) and a bounded local working
cache. No Box secret in the package.

### Responsibility E — DVLA/DVSA lookup

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **Yes** | Durable lookup request rows are idempotent per case and registration, and the accepted vehicle evidence is shared case state (`src/Pegasus.Core/Vehicle/LookupWorkItem.cs`). |
| Unattended execution | **Yes** | The release-15 reconciliation sweep enqueues one lookup per active case with an un-looked-up registration, with every desktop closed. Lands on the existing Worker. |
| Protected credentials | **Yes** | `Dvla__ApiKey`, `Dvsa__ClientId`, `Dvsa__ClientSecret`, `Dvsa__ApiKey` are Key Vault references on the Worker (`platform.bicep:557-564`) and appear nowhere on the Web app (F-5). |
| Public callback | **No** | Request/response only. |
| Central enforcement | **Yes** | Provider rate limits must be coordinated across ten desktops; the Worker already serialises the live adapter, and `VehicleLookupOutcome.Throttled` is a first-class outcome (`LookupContracts.cs:9`). |
| Measured operational advantage | **No measured evidence** | Not needed. |

**Placement:** gateway plus Worker (ADR-0107). One part *does* land on the desktop and
must be said out loud: **registration normalisation** is a Core policy and travels
into the desktop through the shared Core assembly, so the desktop normalises input
before it asks. That is a desktop placement, not a cloud one.

### Responsibility F — report rendering

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **No** | A draft render is one operator's action over one case's frozen snapshot (`AssessmentReportProjection.cs`). |
| Unattended execution | **Yes, for one path** | Today the only trigger is the operator's `OnPostGenerateReportDraftAsync` (`Cases/Assessment/Index.cshtml.cs`, `PAR-15`) — no. But upstream `DOCS-001` (board [[DOCS-001]]) asks for generation triggered from complete accepted assessments, which is unattended by definition. That path lands on the gateway/Worker and is one of the reasons L-03 keeps the gateway renderer rather than deleting it. |
| Protected credentials | **No** | Templates, `report.css` and the brand assets are repository content (F-1); the report model comes from the gateway over the authenticated session. Nothing long-lived ships. |
| Public callback | **No** | — |
| Central enforcement | **Yes** | The canonical copy of the final PDF goes to Box through custody and the report revision record is server-side, so **registering** a final report stays a gateway command even when the **rendering** happens locally. |
| Measured operational advantage | **Owed, not absent** | This is the one row where a measurement is planned rather than skipped: the Phase 7 spike measures WebView2 output against `PlaywrightAssessmentReportRenderer` output on the fixture set. Until it reports, "the desktop renders faster" is not an answer. |

**Placement:** the *rendering compute* moves to the desktop as an isolated, non-UI
WebView2 HTML→PDF path (L-03, ADR-0108); the *projection*, the *registration* and any
unattended trigger stay on the gateway, and the gateway renderer is retained until
golden-file parity passes. Proposal §23.2 is a hard constraint to record here and
build nowhere: an isolated WebView2 use needs an ADR and must never host Pegasus UI.

## NOT YET CAPTURED

Each block names the exact command and the question its output must answer. Each has
a matching unticked box in `open-questions`.

### NOT YET CAPTURED — U-1 · `Q4.1` constrained short-lived Box transfer URLs

```
git grep -rn "Box.Sdk.Gen" src/Pegasus.Infrastructure --include=*.csproj
sed -n '1,120p' src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs
sed -n '1,120p' src/Pegasus.Infrastructure/Custody/BoxDocumentContentStore.cs
<Box's own published API documentation for scoped/downscoped tokens and pre-signed URLs>
```

Must answer: yes/no, plus **the named SDK method** or the explicit statement that none
exists. If no, record the record's own consequence: stream through the gateway and
size the Container App accordingly.

### NOT YET CAPTURED — U-2 · `Q4.2` document metadata field inventory

```
sed -n '1,200p' src/Pegasus.Core/Documents/DocumentContracts.cs
git grep -n "Document" src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs
```

Must answer: for each of file type, size, source, uploader and timestamp (proposal
§14.6) — exists at `path:line`, or needs projection work.

### NOT YET CAPTURED — U-3 · `Q4.3` upstream `PLAT-041` ordering

```
grep -n "PLAT-041" docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md
```

Must answer: the state of upstream `PLAT-041` (resolve the Box case folder once per
export, not once per image) in the triage table, and the ordering constraint it places
on area 07's export endpoint and on [[FND-023]] (plan handle `DSK-01-10`). Note that
upstream `PLAT-041` sits outside the first sync's range, so it may still be open when
this ticket runs.

### NOT YET CAPTURED — U-4 · record 5's verification re-run

```
git grep -n "class DvlaDvsaProductionAdapter" src/Pegasus.Infrastructure/Vehicle/
git grep -n "VehicleLookupRequests" src/Pegasus.Infrastructure/Persistence/Migrations | head
git grep -n "Dvla__\|Dvsa__" infra/modules/platform.bicep
```

Must answer: that the record's adapter, migration and secret citations still match the
head this ticket runs on, with each correction made in the record.

### NOT YET CAPTURED — U-5 · `Q5.1` direct native provider call

```
<DVLA Vehicle Enquiry Service and DVSA MOT History published contracts>
```

Must answer: any published clause permitting a public/native client to call the
provider directly, or the literal sentence "no evidence found; default no" as proposal
§12.3 requires. Never infer permission from silence in the other direction.

### NOT YET CAPTURED — U-6 · `Q5.3` gateway never calls the provider inline

```
git grep -n "OnPostRequestVehicleLookupAsync" -A 40 src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs
git grep -rn "DvlaDvsaProductionAdapter" src/
```

Must answer: the trace from `OnPostRequestVehicleLookupAsync` to the durable request
row, and proof that the live adapter is constructed only on the Worker path
(`src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs`). F-5 is corroborating
infrastructure evidence, not the trace.

### NOT YET CAPTURED — U-7 · `Q6.1` template scope for the desktop

```
grep -n "TICK-206" docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md
grep -rn "scriban" src/Pegasus.Infrastructure/Reports/ src/Pegasus.Core/Reports/
```

Must answer: whether upstream `TICK-206` ("Map renderer templates to capabilities and
decide proposed retirements") is resolved. If it is not, this is a **decision**: add
one line to `docs/open-decisions.md` and list all six templates with their current
caller so the decision has an inventory to work from. Note: upstream `TICK-206` is on
the drop list of [[FND-022]] (plan handle `DSK-01-09`) — it has **no fork ticket** —
so nobody on this board will resolve it; the decision line is the answer.

### NOT YET CAPTURED — U-8 · `Q6.3` WebView2 runtime on the ten workstations

```
<Operator step — no repository command can answer this.>
```

**Operator step.** The operator confirms, per workstation, the Windows 11 build and
the installed Evergreen WebView2 runtime version, or states that a fixed-version
runtime must be shipped. Evidence to hand back: one line per workstation with OS build
and runtime version. If the operator cannot answer yet, add it to
`docs/open-decisions.md`.

### NOT YET CAPTURED — U-9 · `Q6.2` and `Q6.4` from official documentation

```
microsoft_docs_search "CoreWebView2 PrintToPdfAsync"
microsoft_docs_search "CoreWebView2PrintSettings"
microsoft_docs_fetch <the page documenting page size, margins, header/footer and font behaviour>
<PDFsharp published documentation for post-processing behaviour>
```

Must answer: the documented settings surface, each with a URL **and a fetch date**;
and the explicit sentence that fidelity against `PlaywrightAssessmentReportRenderer.cs`
output is **measured by the Phase 7 spike, not settled here**. Do the same for
PDFsharp post-processing on WebView2 output.

### NOT YET CAPTURED — U-10 · records 4–6 written back and closed

```
ls docs/design/assets/report-renderer/templates/
git grep -n "PlaywrightVersion" Directory.Build.props src/Pegasus.Web/Pegasus.Web.csproj
git grep -n "Microsoft.KeyVault(" src/Pegasus.Infrastructure
pwsh ./scripts/Test-DocumentationLinks.ps1
pwsh ./scripts/Test-MarkdownPlacement.ps1
```

Must answer: that every `Q4.x`/`Q5.x`/`Q6.x` heading in
`docs/desktop/01-inventory-and-parity/flow-records.md` reads `Answered <date>: …` or
`Moved to docs/open-decisions.md <date>`, that the template count now says six plus
`report.css`, that the pinned version and base image tag are both stated, and that
both scripts exit 0.

## Implications

1. **Correct the template count in the same edit as the answers** (F-1). It is an
   explicit acceptance criterion, and the ellipsis in the published record is what
   made the count wrong.
2. **`Q5.2` is answered by absence** (F-4). Write it as "no central cache lifetime is
   defined because there is no cache; repeat safety is the durable idempotent request
   row per case and registration", with the `path:line` for the request row — not as
   an open decision.
3. **`Q6.1` will almost certainly become a `docs/open-decisions.md` line**, because
   upstream `TICK-206` has no fork ticket and nothing on this board will resolve it
   (U-7). Supply the six-template inventory with callers so the decision is
   actionable.
4. **Never present a documentation page as fidelity evidence** (A-01-8). Record the
   API surface with URL and fetch date, and say plainly that the Phase 7 spike
   measures.
5. **Build nothing.** The scope boundary is read-only over `src/`, `tests/`, `infra/`
   and `docs/design/assets/`; the only editable files are
   `docs/desktop/01-inventory-and-parity/flow-records.md` and
   `docs/open-decisions.md`. Do not render a report, call Box or DVLA/DVSA, add a
   WebView2 host, or touch the renderer or its templates.
6. **Do not "fix forward" what upstream owns.** The Box token-renewal defect fixed
   upstream by `PLAT-039` arrives with the first sync, which is [[FND-023]]'s (plan
   handle `DSK-01-10`) work. Record the dependency; do not patch it here.
7. **C-01 applies to every answer.** The repositories become private, so no answer may
   assume anonymous public HTTPS — including any Box or template distribution idea.
8. **No Azure call is needed at all.** The Key Vault reference *names* come from
   `infra/modules/platform.bicep`; never read a secret value.

## Open questions

The ten flow-record questions (`Q4.1`–`Q4.3`, `Q5.1`–`Q5.3`, `Q6.1`–`Q6.4`) are this
spike's subject and are tracked as U-1…U-10 above and as boxes in `open-questions`.

One item is parked rather than blocking:

- The duplicate `Microsoft.Playwright` literal at
  `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:17` (F-2) is the one
  place the ADR-0028 pin can desynchronise. It is [[FND-027]]'s (plan handle
  `DSK-02-02`) acceptance criterion, not an edit for this ticket, so it is a scope
  boundary and not an open question.

Nothing here re-opens a settled operator decision. In particular, Send to AI is a
**recorded exclusion with a reactivation condition** (`docs/capabilities.md:269`), not
an open question, and no `open-questions` item is created for it on this ticket.
