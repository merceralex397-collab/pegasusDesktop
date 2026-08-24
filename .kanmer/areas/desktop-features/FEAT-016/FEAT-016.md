---
id: FEAT-016
type: ticket
title: DSK-05-16 · S16 Images and gallery
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-6
  - tier-7
  - tier-10
  - needs-operator
groups:
  - EPIC-006
  - HZN-007
links: []
blocks:
  - FEAT-022
  - FEAT-025
  - FEAT-044
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
archived: false
created: '2026-08-24T07:57:08.715Z'
updated: '2026-08-24T12:53:45.417Z'
---

## What

Build one reusable image gallery control used by every image-bearing screen, with progressive thumbnails that never block navigation, a bounded thumbnail cache, prompt disposal, keyboard traversal and alt text from metadata, and the pop-out image viewer it opens into; the case Evidence tab reads document records through it, handing a non-image off to [[DSK-07-06]]'s preview surface rather than rendering it here.

## Why

Proposal §13.7 and §15.2 require images and metadata to be handled without stalling the UI, and the memory budget to hold after prolonged use. Today the web has one partial, `src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml`, with the tiny view model `src/Pegasus.Web/Presentation/GalleryImage.cs` and receipt-asset image endpoints; upstream CASE-011 asks for a reusable gallery viewer and this slice absorbs it. Without a single control each image-bearing screen would grow its own decoding and caching behaviour, which the one-list-per-concept rule forbids. The same rule cuts the other way for documents: [[DSK-07-06]] already owns the Documents-tab preview pane, the safe-type list and the single binding to [[DSK-07-14]]'s isolated document-render path, so this ticket delivers the **image** viewer and calls that surface for a document rather than becoming a second document renderer. Siblings: [[DSK-05-14]] supplies the transfer service and document records, [[DSK-07-06]] owns the document preview, [[DSK-05-09]] and [[DSK-05-12]] are the other image-bearing surfaces.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-16`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S16 · Images and gallery (DSK-05-16)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Intake (received items), uploads, image intake` (image byte endpoints) and § `Cases` (document content with `ETag` and range)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.7 Documents and evidence — Case workspace › Documents tab` (evidence gallery)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.7 Documents and evidence, § 15.2 Implementation practices, § 14.9 Keyboard and accessibility
- Repository evidence: `src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml`, `src/Pegasus.Web/Presentation/GalleryImage.cs`, `src/Pegasus.Web/Pages/Intake/Image.cshtml.cs` (receipt-asset image endpoint), `src/Pegasus.Core/ImageIntake/`
- Upstream evidence: **upstream `CASE-011`** (a pop-out viewer with previous, next, close and outside-click dismissal, reused across every image-bearing page) and **upstream `DOCS-011`** (the same click-to-preview for documents, an inline disposition distinct from the existing download disposition, and download as the fallback for anything not previewable) — the viewer half this ticket delivers. **Neither was imported: there is no fork ticket for either, so never write them as board wiki-links.** CASE-011's viewer is delivered here in full. DOCS-011's *gesture* is delivered here — a document tile in the gallery is clickable and previews in place — while its *rendering* is [[DSK-07-06]]'s, which already owns the preview pane, the safe-type list and the [[DSK-07-14]] binding.
- Binding decisions: L-01 the gateway serves the bytes, the desktop renders; L-02 the memory and latency measurements run on the local Test/UAT workstation; L-04 routing named on the ticket
- Depends on: `DSK-05-14` the transfer service, document records and the Documents tab this gallery first lands in; `DSK-07-06` the document-preview surface this gallery hands a non-image item to — it owns that path and this ticket calls it

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`; `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml` (performance and memory measurement)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `analyzing-dotnet-performance` (dotnet/skills `98f84851`, `plugins/dotnet-diag/skills/analyzing-dotnet-performance/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`) → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for `BitmapImage.DecodePixelWidth` / `DecodePixelHeight` semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S16, the screen spec evidence-gallery section and proposal §15.2. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-16-image-gallery` and worktree `../pegasus-worktrees/dsk-05-16-image-gallery` from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml` and `src/Pegasus.Web/Presentation/GalleryImage.cs`, and list in `research` every screen that shows images today and the metadata each has available for alt text. Record the SHA read.
3. Confirm the byte endpoints expose a size hint and a weak `ETag` so a thumbnail request does not fetch a full-resolution image; where the hint is missing, add it to the `/api/v1` image endpoints with [[DSK-03-10]]'s conventions.
4. Use `microsoft_docs_search` for `BitmapImage.DecodePixelWidth` and `DecodePixelHeight` to confirm decode-to-display-size semantics, then implement `ImageThumbnailService` in `src/Pegasus.Desktop.Infrastructure`: decode to the display size, never full resolution; a bounded LRU cache with an explicit item and byte ceiling; prompt disposal when an item leaves the cache or the view unloads.
5. Implement the `ImageGallery` control in `src/Pegasus.Desktop` as a single reusable control: virtualized, progressive (a placeholder appears immediately and is replaced as each thumbnail arrives), keyboard traversable with arrow keys and Enter to open, and an accessible name per item taken from the record's metadata. Never a colour-only state and never a full-page spinner.
6. Implement the **image** viewer the gallery opens into — the half upstream CASE-011 asks for, without which this ticket ships a thumbnail grid with no way to look at anything. One `ImageViewer` surface in `src/Pegasus.Desktop` beside the control, opened in place over the current screen rather than in a new window, tab or shell navigation, carrying previous, next, download and close commands. Previous and next move within the same item set the gallery is showing and stop at its ends. `Escape` and a click outside the viewer both dismiss it; focus is trapped while it is open and returns to the originating thumbnail on close. Download goes through [[DSK-05-14]]'s transfer service, never a second byte path. Every command carries an AutomationId beside `Case.Documents.Preview`.
7. **Hand a non-image item to [[DSK-07-06]] instead of rendering it (upstream DOCS-011). Check first that [[DSK-07-06]] has landed its preview pane, and restate its shape from that ticket's step 7 before writing a line of code — it is the single owner of the document-preview path.** The interlock, stated in the same words in both bodies: *Document preview has one owner: [[DSK-07-06]]. It owns the Documents-tab preview pane, the `Case.Documents.Preview` AutomationId, the safe-type list, and the single binding to [[DSK-07-14]]'s isolated document-render path. [[DSK-05-16]] owns the image gallery and the image viewer — previous, next, download, close, `Escape` and click-outside — across every image-bearing screen. When a gallery item is a document rather than an image, [[DSK-05-16]] does not render it: it hands off to [[DSK-07-06]]'s preview surface through that ticket's named entry point, and where the type is not on [[DSK-07-06]]'s safe list it offers download through [[DSK-05-14]]'s transfer service instead of opening raw bytes. Two surfaces, one entry point per kind: a second document-render binding, a second safe-type list, or a PDF opened by the gallery itself is a stop condition.* Concretely: the gesture is identical for both kinds — one click on a tile previews in place — but the branch is on item kind, the image branch opens the step 6 viewer and the document branch calls [[DSK-07-06]]. This ticket references [[DSK-07-14]] nowhere in code. If [[DSK-07-06]] has not landed, define the seam it will implement, record that in the plan document, and do not render a document in the meantime.
8. Adopt the control on the case Evidence tab, reading document records from [[DSK-05-14]] rather than the custody ledger — this is upstream DOCS-012, absorbed by S14/S16.
9. Adopt the same control on the Received item screen ([[DSK-05-09]]), the Vehicle images screen ([[DSK-05-12]]) and the Triage detail screen ([[DSK-05-11]], upstream INTK-034 — read-only, from the origin receipt's retained assets, not retained a second time). If any of them already grew its own image rendering, delete it in this slice — a second implementation is a stop condition (`docs/engineering.md` § One Core owner).
10. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for progressive load ordering, cache eviction at the ceiling, cancellation when the view unloads, alt-text derivation from metadata, previous/next boundaries at the ends of the item set, `Escape` and outside-click dismissal, a document item routing to [[DSK-07-06]]'s preview seam rather than to the `ImageViewer`, and a type outside [[DSK-07-06]]'s safe list falling back to download rather than opening bytes.
11. **Operator step** — measure on the baseline Test/UAT workstation: navigation is never blocked while thumbnails load, and working-set memory returns to a steady level after repeatedly opening and leaving an image-heavy case. Use `analyzing-dotnet-performance` for the method and record the figures and the workstation specification in the ticket proof.
12. Add a `winapp ui` script under `tests/Pegasus.Desktop.UITests` traversing the gallery by keyboard on an image-heavy case, opening the image viewer, paging with previous and next, downloading, and dismissing with both `Escape` and an outside click; run the `axe-windows` scan over the open viewer as well as the grid, and attach both artefacts.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the image and evidence rows, add the gallery note to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] One gallery control serves every image-bearing screen; no screen keeps a second implementation.
- [ ] Opening an **image** from the gallery shows it in place in a single pop-out viewer with previous, next, download and close controls, dismissed by `Escape` or by clicking outside it, and the same viewer is used from every image-bearing screen.
- [ ] Clicking a **document** in the gallery previews it through [[DSK-07-06]]'s preview surface — the single owner of the document-preview path, its safe-type list and its [[DSK-07-14]] binding — and a type outside that safe list offers download through [[DSK-05-14]]'s transfer service rather than opening raw bytes. This slice contains no second document renderer, no second safe-type list and no reference to [[DSK-07-14]] in code (upstream DOCS-011).
- [ ] Thumbnails appear progressively and never block navigation.
- [ ] Images decode to display size; the cache is bounded and items are disposed promptly.
- [ ] Memory is steady after repeated navigation through an image-heavy case.
- [ ] The gallery is keyboard traversable and every item has an accessible name from metadata.
- [ ] The Evidence tab reads document records, not the custody ledger.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: progressive-load, eviction, cancellation, alt-text, previous/next boundary, dismissal, document-routes-to-`DSK-07-06` and download-fallback facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script image-gallery` — expected: keyboard traversal passes, the image viewer opens in place and pages with previous/next, `Escape` and an outside click both dismiss it; axe report attached with no critical finding for the grid or the open viewer.
- [ ] Performance record in the ticket proof — expected: navigation unblocked during thumbnail load and steady memory after repeated navigation, with the workstation specification stated.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: no duplicate gallery, viewer or thumbnail implementation, and no reference from this slice to the isolated document-render path that [[DSK-07-06]] alone binds.

## Evidence tier

Tier 7 — Browser/accessibility. Tier 10 — Performance/concurrency.
Tier 7 obliges keyboard, focus, semantic-label and text-plus-colour evidence from a real run of the gallery; tier 10 obliges measured memory and responsiveness against the stated per-case file volumes (2–20+ files per case) rather than an asserted budget.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — image and evidence rows
- `docs/desktop/06-ui-design/screen-specs.md` § `§13.7 Documents and evidence` — record the viewer contract (open in place, previous/next, download, close, `Escape` and click-outside dismissal, a document previewed through [[DSK-07-06]]'s preview surface, download offered for a type outside its safe list) with the matching AutomationIds beside `Case.Documents.Preview`; §13.7 promises the CASE-011 gallery viewer today and the specification never states its behaviour. **Coordination rule, stated in the same words in [[DSK-03-07]] and [[DSK-05-16]]:** `screen-specs.md` is edited **once per block, by that block's owner** — [[DSK-03-07]] owns the `:230-231` "Upstream carry-over absorbed" line in the case-workspace block, [[DSK-05-16]] owns the § `§13.7 Documents and evidence` viewer contract, and neither edits the other's block. [[DSK-06-13]] then adopts both blocks into `docs/frd/frd-13-desktop-operator-experience.md` and is the **only** ticket that writes FRD-13's §13.7 and case-workspace content — so **[[DSK-06-13]] must not adopt either block until both corrections have landed**, and must record in its plan document which case applied. If [[DSK-06-13]] runs first it freezes the uncorrected absorbed list and an unstated viewer contract into FRD-13, and both then have to be corrected twice.
- `docs/frd/frd-13-desktop-operator-experience.md` — gallery behaviour note, including the viewer contract; written by [[DSK-06-13]] under the coordination rule above, not by this ticket
- `docs/capabilities.md` — `DSK` row for the image gallery

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` image byte endpoints in `src/Pegasus.Web` and the test projects. Must not touch `src/Pegasus.Infrastructure/Vision/` or any Razor partial. **The document-preview path is not this ticket's**: the preview pane, its safe-type list and the binding to [[DSK-07-14]]'s isolated document-render path belong to [[DSK-07-06]]; this slice calls that surface and neither binds [[DSK-07-14]] nor renders a document itself.
- **Traps**: one gallery implementation only — encountering a second is a stop condition; one **image** viewer implementation only, and it is this one — a screen that opens an image in a new window, a shell navigation or a raw byte URL is a stop condition; decode to display size and dispose promptly, or the memory budget fails; no colour-only state and no full-page spinner (`docs/design/README.md`); the script-free `_ImageGallery.cshtml` was deliberate for accessibility, so keyboard and `Escape` are not traded for the viewer (upstream DOCS-011); performance figures must come from the baseline workstation, never from a developer machine; upstream CASE-011 and DOCS-011 are absorbed here — the grid **and** its image viewer — and must not be re-raised. **Document preview has one owner: [[DSK-07-06]].** It owns the Documents-tab preview pane, the `Case.Documents.Preview` AutomationId, the safe-type list, and the single binding to [[DSK-07-14]]'s isolated document-render path. [[DSK-05-16]] owns the image gallery and the image viewer — previous, next, download, close, `Escape` and click-outside — across every image-bearing screen. When a gallery item is a document rather than an image, [[DSK-05-16]] does not render it: it hands off to [[DSK-07-06]]'s preview surface through that ticket's named entry point, and where the type is not on [[DSK-07-06]]'s safe list it offers download through [[DSK-05-14]]'s transfer service instead of opening raw bytes. **Two surfaces, one entry point per kind: a second document-render binding, a second safe-type list, or a PDF opened by the gallery itself is a stop condition.** **Upstream ids and fork board ids do not match**: upstream CASE-011, DOCS-011, DOCS-012 and INTK-034 were none of them imported and have no fork tickets, so never write any of them as a board wiki-link — and the board's `INTK-001`…`INTK-007` are upstream INTK-002, INTK-003, INTK-026, INTK-027, INTK-031, INTK-032 and INTK-033, none of which is INTK-034.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
