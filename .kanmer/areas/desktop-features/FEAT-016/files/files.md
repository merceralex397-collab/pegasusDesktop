# Files — FEAT-016

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today — `ls src` returns only `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`; `ls tests` only
`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Desktop.Infrastructure/` — `ImageThumbnailService` *(created by [[FND-031]], plan handle `DSK-02-06`)* | Decode to the **display size**, never full resolution; a bounded LRU cache with an explicit **item and byte ceiling**; prompt disposal when an item leaves the cache or the view unloads. This is the whole memory budget in one type. |
| `src/Pegasus.Desktop/` — `ImageGallery` control *(created by [[FND-030]], plan handle `DSK-02-05`)* | One reusable control: virtualized, progressive (a placeholder appears immediately and is replaced as each thumbnail arrives), keyboard traversable with arrow keys and Enter to open, and an accessible name per item from the record's metadata. Never a colour-only state and never a full-page spinner. |
| `src/Pegasus.Desktop/` — `ImageViewer` surface | The **image** viewer the gallery opens into, beside the control: opened **in place** over the current screen — not a new window, tab or shell navigation — with previous, next, download and close; previous/next move within the same item set and stop at its ends; `Escape` **and** click-outside both dismiss; focus trapped while open and returned to the originating thumbnail on close; download through [[FEAT-014]] (plan handle `DSK-05-14`)'s transfer service, never a second byte path. Every command carries an AutomationId **beside** `Case.Documents.Preview`. |
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`)* | Only what the gallery needs that is not already there: the item-kind discriminator that decides image-versus-document, the metadata field the accessible name is derived from, and the size hint the byte endpoints accept. |
| `src/Pegasus.Web/` — the `/api/v1` image byte endpoints only | Add a **size hint** and a **weak `ETag`** where they are missing, with [[GWY-010]] (plan handle `DSK-03-10`)'s conventions. Today `src/Pegasus.Web/Pages/Intake/Image.cshtml.cs:48,53` sets only `nosniff` and returns `File(source.Content.ToArray(), …)` — no hint, no validator, and the whole image materialised. |
| `src/Pegasus.Desktop/` — the four adopting screens | The case Evidence tab (step 8), plus the Received item ([[FEAT-009]], plan handle `DSK-05-09`), Vehicle images ([[FEAT-012]], plan handle `DSK-05-12`) and Triage detail ([[FEAT-011]], plan handle `DSK-05-11`) screens (step 9). **If any grew its own image rendering, delete it in this slice.** |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[TEST-004]], plan handle `DSK-08-04`)* | Progressive-load ordering, cache eviction at the ceiling, cancellation when the view unloads, alt-text derivation from metadata, previous/next boundaries at the ends of the item set, `Escape` and outside-click dismissal, a document item routing to [[FEAT-032]] (plan handle `DSK-07-06`)'s preview seam rather than to the `ImageViewer`, and a type outside that safe list falling back to download rather than opening bytes. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]], plan handle `DSK-08-06`)* | The `image-gallery` script: keyboard traversal on an image-heavy case, opening the viewer, paging with previous and next, downloading, and dismissing with **both** `Escape` and an outside click; plus the `axe-windows` scan from [[TEST-009]] (plan handle `DSK-08-09`) over the **open viewer** as well as the grid. |
| `tests/Pegasus.ArchitectureTests/` | No duplicate gallery, viewer or thumbnail implementation, and **no reference from this slice to the isolated document-render path** that [[FEAT-032]] alone binds. [[FND-037]] (plan handle `DSK-02-12`) owns the dependency-direction rules these sit beside. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | The image and evidence rows — `PAR-20` (receipt byte pages) and the document rows `PAR-13`/`PAR-16`. |
| `docs/desktop/06-ui-design/screen-specs.md` § `§13.7 Documents and evidence` | **Record the viewer contract** — open in place, previous/next, download, close, `Escape` and click-outside dismissal, a document previewed through [[FEAT-032]]'s preview surface, download offered for a type outside its safe list — with the matching AutomationIds beside `Case.Documents.Preview`. §13.7 promises the CASE-011 gallery viewer today (`:357-359`) and never states its behaviour. **This block only.** |
| `docs/capabilities.md` | `DSK` row for the image gallery. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml` (23 lines) | The web has **no thumbnails**: its own comment at `:2-5` says "Thumbnails are the full image CSS-constrained and lazy-loaded; each is a link that opens the full-size image on its own via the same authorised endpoint, so preview-and-expand works with no script and stays keyboard accessible." Two things follow — every tile is a full download today (the problem §15.2 asks the desktop to solve), and the script-free design was **deliberate for accessibility**, so keyboard access and `Escape` are not traded for the viewer. |
| `src/Pegasus.Web/Presentation/GalleryImage.cs` (4 lines) | `public sealed record GalleryImage(string Href, string FileName);` — **the only metadata available for alt text today is the file name**, and the partial renders `alt="@image.FileName"`. A richer accessible name must come from the document or asset record; where only a file name exists, that is what is used rather than a fabricated description. |
| `src/Pegasus.Web/Pages/Intake/Image.cshtml.cs:48,53` | `Response.Headers.XContentTypeOptions = "nosniff"` and `File(source.Content.ToArray(), source.ContentType)`. `.ToArray()` materialises the whole image, and there is **no size hint and no `ETag`** — this is the concrete gap step 3 closes on `/api/v1`. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (Documents row) and § `Intake …` (byte row) | The document content read is already specified with `ETag` and range; the receipt byte reads are specified with `ETag, range` too. The **size hint** is the addition, and [[GWY-010]] owns those conventions. |
| `docs/desktop/06-ui-design/screen-specs.md:349-351` | "preview pane for supported images/PDF (image decode to display size; PDF via the isolated report/preview path — never a WebView hosting app UI)". `Case.Documents.Preview` is the id and **[[FEAT-032]] owns it**, together with the safe-type list and the single [[FEAT-040]] (plan handle `DSK-07-14`) binding. |
| `docs/desktop/06-ui-design/screen-specs.md:357-361` | The gallery promise — "CASE-011 gallery viewer reused across image-bearing screens" — and the six AutomationIds, **none of which is a viewer id**. That absence is what this ticket's documentation change fixes. |
| `docs/design/README.md` § No explanatory copy and page economy | No colour-only state, no full-page spinner, no explanatory text. The progressive placeholder is a placeholder, not a message. |
| `docs/engineering.md` § One Core owner | One gallery, one image viewer, one thumbnail cache for the whole application. Encountering a second is a stop condition. |
| `src/Pegasus.Core/ImageIntake/` | Where image records come from — `ImageIntakeContracts.cs` and its siblings. Read for the record shape; `src/Pegasus.Infrastructure/Vision/` (the ONNX engine) is out of bounds. |
| `tests/Pegasus.IntegrationTests/ImageViewingWebTests.cs`, `ImageIntakeWebTests.cs` | The existing web-side image evidence. Read before writing the desktop's equivalents so the same behaviours are covered rather than a different set. |
| Group document `HZN-001` / `board-conventions.md` | The join table, and the reason none of upstream CASE-011, DOCS-011, DOCS-012 or INTK-034 may be written as a board wiki-link: **none was imported**. The board's `INTK-001`…`INTK-007` are upstream INTK-002, INTK-003, INTK-026, INTK-027, INTK-031, INTK-032 and INTK-033 — none of which is INTK-034. |

## Ripple effects

- **Four screens adopt the control, and one may need something deleted.** The case
  Evidence tab, [[FEAT-009]], [[FEAT-012]] and [[FEAT-011]]. [[FEAT-011]] step 10
  already anticipates this ticket and forbids a second renderer on its side; if any
  screen grew its own image rendering before this slice lands, it is **deleted
  here**.
- **The `/api/v1` image endpoints gain a size hint and a weak `ETag`**, which
  changes `openapi/pegasus-v1.json` and the generated client that [[GWY-010]] and
  the contract tests bind to.
- **[[FEAT-014]]** supplies the transfer service the viewer's download uses and
  the document records the Evidence tab reads (upstream DOCS-012 — evidence on the
  Evidence tab, **not** the custody ledger).
- **[[FEAT-032]]** owns the document-preview path; this slice calls its named
  entry point and binds [[FEAT-040]] nowhere.
- **`screen-specs.md` coordination, stated in the same words in [[GWY-007]] (plan
  handle `DSK-03-07`) and here**: the file is edited **once per block, by that
  block's owner**. [[GWY-007]] owns the `:230-231` "Upstream carry-over absorbed"
  line in the case-workspace block; this ticket owns the § `§13.7 Documents and
  evidence` viewer contract; **neither edits the other's block**. [[DUI-013]] (plan
  handle `DSK-06-13`) then adopts both blocks into
  `docs/frd/frd-13-desktop-operator-experience.md` and is the **only** ticket that
  writes FRD-13's §13.7 and case-workspace content — so **[[DUI-013]] must not
  adopt either block until both corrections have landed**, and must record in its
  plan which case applied. If [[DUI-013]] runs first it freezes the uncorrected
  absorbed list and an unstated viewer contract into FRD-13, and both then have to
  be corrected twice.
- **`docs/frd/frd-13-desktop-operator-experience.md` is written by [[DUI-013]],
  not by this ticket** — including the gallery behaviour note and the viewer
  contract.

## Out of scope

- **`src/Pegasus.Infrastructure/Vision/`** and any Razor partial.
- **The document-preview path.** The preview pane, its safe-type list and the
  binding to [[FEAT-040]]'s isolated document-render path belong to [[FEAT-032]];
  this slice calls that surface and neither binds [[FEAT-040]] nor renders a
  document itself. **This slice references [[FEAT-040]] nowhere in code.**
- **A second gallery, a second image viewer, a second thumbnail cache, a second
  safe-type list, or a PDF opened by the gallery itself.** Each is a stop
  condition.
- **Opening an image in a new window, a shell navigation or a raw byte URL.** The
  viewer opens **in place**.
- **Writing `docs/frd/frd-13-desktop-operator-experience.md`.** [[DUI-013]]'s,
  under the coordination rule above.
- **The `screen-specs.md` case-workspace block at `:230-231`.** [[GWY-007]]'s.
- **Performance figures from a developer machine.** They must come from the
  baseline Test/UAT workstation, with its specification stated.
- **Re-raising upstream CASE-011 or DOCS-011.** Both are absorbed here — the grid
  **and** its image viewer. Neither was imported, so neither may be written as a
  board wiki-link, and the same is true of upstream DOCS-012 and upstream INTK-034.
- **Any Azure write.**
