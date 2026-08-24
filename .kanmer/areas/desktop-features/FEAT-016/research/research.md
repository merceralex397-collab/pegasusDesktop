# Research — FEAT-016: the one gallery, the image viewer it opens into, and the seam to the document-preview owner

## Question

What does the web's single gallery partial actually do — including what metadata
is available for alt text and whether the byte endpoints support a thumbnail
request at all — and where exactly does this slice's image viewer stop and
[[FEAT-032]] (plan handle `DSK-07-06`)'s document-preview surface begin?

## Current behaviour

Read at fork `main` `191ddf33`. The implementer re-reads and records the SHA
(ticket step 2).

| Surface | `path:line` | What it does |
| --- | --- | --- |
| The gallery | `src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml` (23 lines) | A `<ul>` of `<li><a href><img loading="lazy"></a></li>`, plus an empty state |
| Its view model | `src/Pegasus.Web/Presentation/GalleryImage.cs` (4 lines) | `public sealed record GalleryImage(string Href, string FileName);` |
| Receipt-asset image bytes | `src/Pegasus.Web/Pages/Intake/Image.cshtml.cs` (79 lines) | `Response.Headers.XContentTypeOptions = "nosniff"` at `:48`; `return File(source.Content.ToArray(), source.ContentType)` at `:53` |
| Document content bytes | `src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs` (112 lines) | `IDocumentContentStore`, no-sniff attachment |

Parity-matrix rows this touches: **`PAR-20`** (the receipt byte pages) and
**`PAR-13`**/**`PAR-16`** (the case documents and their content read),
`docs/desktop/01-inventory-and-parity/parity-matrix.md`. The matrix holds
`PAR-01`…`PAR-46` (`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **The web has no thumbnails.** `_ImageGallery.cshtml`'s own comment (`:2-5`)
  says "Thumbnails are the full image CSS-constrained and lazy-loaded; each is a
  link that opens the full-size image on its own via the same authorised endpoint,
  so preview-and-expand works with no script and stays keyboard accessible."
  Every tile downloads the whole image. On a case with 2–20+ photographs that is
  the memory and latency problem proposal §15.2 requires the desktop to solve.
- **The script-free choice was deliberate, and for accessibility.** The same
  comment says so. The desktop's viewer must not trade keyboard access or `Escape`
  for richness — which is why the ticket's Traps name it.
- **The only metadata available for alt text today is the file name.**
  `GalleryImage` is a four-line record of `(Href, FileName)`, and the partial
  renders `alt="@image.FileName"` (`_ImageGallery.cshtml:17`). Any richer
  accessible name in the desktop has to come from the document or asset record,
  not from this shape.
- **The image byte endpoint buffers the whole image and offers no size hint and
  no `ETag`.** `src/Pegasus.Web/Pages/Intake/Image.cshtml.cs:53` calls
  `File(source.Content.ToArray(), source.ContentType)` — `.ToArray()` materialises
  the full byte array — and `:48` sets only `X-Content-Type-Options: nosniff`.
  There is no width or height parameter and no validator header. **That is exactly
  the gap ticket step 3 exists to close** on the `/api/v1` image endpoints, using
  [[GWY-010]] (plan handle `DSK-03-10`)'s conventions.
- **The document content endpoint is specified with `ETag` and range.**
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases`, Documents row:
  `GET /cases/{id}/documents/{docId}/content` → "bytes, no-sniff, safe filename"
  with `ETag, range` in the concurrency column. So the document side already has
  the validator the image side lacks.
- **The screen spec promises a gallery viewer and never states its behaviour.**
  `docs/desktop/06-ui-design/screen-specs.md:357-359` reads "Evidence gallery
  (instruction photographs) reads document records with paging and download
  (DOCS-011/012, CASE-011 gallery viewer reused across image-bearing screens)".
  The listed AutomationIds (`:359-361`) are `Case.Documents.Table`,
  `Case.Documents.Upload`, `Case.Documents.Queue`, `Case.Documents.Preview`,
  `Case.Documents.OpenExternally`, `Case.Documents.UploadLink.Create` — **there is
  no viewer id**. Recording the viewer contract there is this ticket's
  documentation change.
- **The preview pane, its safe-type list and the isolated render path are named
  in the same spec block** (`screen-specs.md:349-351`): "preview pane for
  supported images/PDF (image decode to display size; PDF via the isolated
  report/preview path — never a WebView hosting app UI)". `Case.Documents.Preview`
  is the id, and [[FEAT-032]] owns it.
- **Four image-bearing surfaces exist and are named by their own tickets.**
  The Received item screen ([[FEAT-009]], plan handle `DSK-05-09`), the Vehicle
  images screen ([[FEAT-012]], plan handle `DSK-05-12`), the Triage detail screen
  ([[FEAT-011]], plan handle `DSK-05-11`, whose step 10 explicitly binds to this
  control) and the case Evidence tab. [[FEAT-011]]'s Guardrails already make a
  second gallery a stop condition on its side.
- **`src/Pegasus.Core/ImageIntake/` is the image-record source** —
  `ImageIntakeContracts.cs`, `ImageIntakeLifecycle.cs`, `VrmRecognition.cs` and
  four others. `src/Pegasus.Infrastructure/Vision/` holds the ONNX engine and is
  out of bounds.
- **Existing web-side image evidence exists.**
  `tests/Pegasus.IntegrationTests/ImageViewingWebTests.cs` and
  `ImageIntakeWebTests.cs`.
- **None of the four upstream ids this slice absorbs or references was
  imported.** upstream CASE-011 (reusable image gallery viewer), upstream DOCS-011
  (click-to-preview for documents, an inline disposition distinct from the existing
  download disposition, download as the fallback), upstream DOCS-012 (evidence on
  the Evidence tab, not the custody ledger) and upstream INTK-034 (Triage evidence)
  **have no fork tickets**, so none may be written as a board wiki-link. The
  board's `INTK-001`…`INTK-007` are upstream INTK-002, INTK-003, INTK-026,
  INTK-027, INTK-031, INTK-032 and INTK-033 — **none of which is INTK-034**.
- **The projects this slice writes into do not exist yet.** `ls src` returns only
  `Pegasus.Core Pegasus.Infrastructure Pegasus.Web Pegasus.Worker`; `ls tests`
  only `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`,
  `Pegasus.IntegrationTests`.

### Assumptions

- **A-05-16-1 — `BitmapImage.DecodePixelWidth` / `DecodePixelHeight` decode to
  the requested size rather than decoding full and scaling.** This is the whole
  basis of the memory budget. Confirmed by: `microsoft_docs_search` at step 4 (the
  ticket routes that lookup explicitly) **and** the tier-10 measurement at step 11.
  Breaks if: the semantics differ — then the thumbnail service needs a different
  decode strategy and the measurement is what reveals it, not an assumption
  carried into code.
- **A-05-16-2 — the image byte endpoints can be given a size hint and a weak
  `ETag` under [[GWY-010]]'s conventions.** Today they have neither
  (`src/Pegasus.Web/Pages/Intake/Image.cshtml.cs:48,53`). Confirmed by: step 3.
  Breaks if: the hint cannot be added — then every thumbnail fetches a
  full-resolution image, the progressive-load property survives but the memory
  budget is at risk, and that is raised on [[GWY-010]] rather than absorbed
  silently.
- **A-05-16-3 — [[FEAT-032]] exposes a named entry point for document preview.**
  The interlock in both bodies says the gallery "hands off to [[FEAT-032]]'s
  preview surface through that ticket's named entry point". Confirmed by: reading
  that ticket's step 7. Breaks if: it has not landed — then this slice **defines
  the seam it will implement, records that in the plan, and renders no document in
  the meantime** (ticket step 7 says exactly that).
- **A-05-16-4 — the baseline Test/UAT workstation is available for the tier-10
  measurement.** Confirmed by: the operator step at 11. Breaks if: it is not —
  figures from a developer machine are explicitly forbidden by the ticket's Traps,
  so the measurement waits rather than being taken somewhere else.
- **A-05-16-5 — document records from [[FEAT-014]] (plan handle `DSK-05-14`)
  carry enough to derive an accessible name and to tell an image from a
  document.** The Evidence tab reads document records, not the custody ledger
  (upstream DOCS-012). Breaks if: the record does not carry a type discriminator —
  then the branch at step 7 cannot be made and it is raised on [[FEAT-014]].

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered. This is the rare ticket where the honest answer is six "no".

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | The control holds no state another user sees. It renders records owned by [[FEAT-014]] and [[FEAT-009]] and writes nothing — no version, no operation key, no mutation of any kind appears in this slice. |
| Unattended execution — must it run with every desktop closed? | **no** | Nothing here runs without a window. A thumbnail cache is per-session and per-user by design, and is discarded on unload. |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | The desktop holds no Box or Graph credential; the byte endpoints it calls are the gateway's and are owned by [[GWY-010]] and [[FEAT-031]] (plan handle `DSK-07-05`). This slice adds no credential and touches no adapter. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing external is involved. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **no** | The gallery makes no authorisation decision. Every byte read it triggers is authorised by the gateway endpoint it calls, which already enforces `StaffAccessRight.PerformCasework` (`src/Pegasus.Core/Identity/StaffAuthorization.cs:10`); the control adds nothing and can weaken nothing. |
| Measured operational advantage — measured evidence central is materially better? | **no** | The opposite, and this ticket measures it (tier 10): decode-to-display-size, a bounded cache and prompt disposal are workstation properties, and proposal §15.2 requires them there. |

All six "no" → **the responsibility belongs in the desktop**, which is the
correct conclusion for a rendering control. The one thing this slice asks of the
gateway — a size hint and a weak `ETag` on the image byte endpoints (step 3) — is
a refinement of an endpoint [[GWY-010]] already owns, not a new placement. No
Azure resource is involved and no Azure write is performed.

## Implications

- **Two surfaces, one entry point per kind.** The gesture is identical for both —
  one click on a tile previews in place — but the branch is on **item kind**: an
  image opens this slice's `ImageViewer`; a document calls [[FEAT-032]]'s preview
  surface; a type outside [[FEAT-032]]'s safe list offers download through
  [[FEAT-014]]'s transfer service instead of opening raw bytes. A second
  document-render binding, a second safe-type list, or a PDF opened by the gallery
  itself is a stop condition, and this slice references [[FEAT-040]] (plan handle
  `DSK-07-14`) **nowhere in code**.
- **The viewer is not optional.** Without it this ticket ships a thumbnail grid
  with no way to look at anything — the ticket says so in its own words at step 6.
  Upstream CASE-011's viewer is delivered here in full: previous, next, download,
  close; opened **in place** over the current screen rather than in a new window,
  tab or shell navigation; previous and next move within the same item set the
  gallery is showing and stop at its ends; `Escape` **and** a click outside both
  dismiss it; focus is trapped while open and returns to the originating thumbnail
  on close.
- **Alt text needs a richer source than the web has.** `GalleryImage` carries only
  a file name. The accessible name per item comes from the record's metadata, which
  means the DTO shape matters — and where only a file name exists, that is what is
  used rather than a fabricated description.
- **The byte endpoints must gain a size hint or every thumbnail is a full
  download.** `Intake/Image.cshtml.cs:53` proves the current shape buffers whole;
  step 3 closes that on `/api/v1` with [[GWY-010]]'s conventions.
- **Adoption is deletion as well as addition.** Step 9 adopts the control on the
  Received item, Vehicle images and Triage detail screens, and deletes any image
  rendering those slices grew in the meantime. [[FEAT-011]] step 10 anticipates
  exactly this and already forbids a second renderer on its side.
- **The Evidence tab reads document records, not the custody ledger** (upstream
  DOCS-012) — so the gallery's data source is [[FEAT-014]]'s document list, not
  `src/Pegasus.Core/Custody/`.
- **`screen-specs.md` is edited once per block, by that block's owner.**
  [[GWY-007]] (plan handle `DSK-03-07`) owns the `:230-231` "Upstream carry-over
  absorbed" line in the case-workspace block; this ticket owns the § `§13.7
  Documents and evidence` viewer contract; neither edits the other's block. And
  [[DUI-013]] (plan handle `DSK-06-13`) — the **only** ticket that writes FRD-13's
  §13.7 and case-workspace content — must not adopt either block until **both**
  corrections have landed, or it freezes an uncorrected absorbed list and an
  unstated viewer contract into FRD-13 and both then have to be corrected twice.

## Open questions

None that block. The three points that could look like questions each have a
named owner or a defined action:

- **Whether [[FEAT-032]]'s preview pane has landed** is answered by looking, and
  both answers have a defined action (step 7): bind to its named entry point, or
  define the seam, record it in the plan, and render no document in the meantime.
- **Whether the image byte endpoints already carry a size hint and a weak
  `ETag`** is answered by reading them; today they carry neither
  (`src/Pegasus.Web/Pages/Intake/Image.cshtml.cs:48,53`), and adding them under
  [[GWY-010]]'s conventions is step 3.
- **`BitmapImage.DecodePixelWidth` semantics** are confirmed by
  `microsoft_docs_search` at step 4 and then by the tier-10 measurement at step 11
  — a check, not an assumption.
