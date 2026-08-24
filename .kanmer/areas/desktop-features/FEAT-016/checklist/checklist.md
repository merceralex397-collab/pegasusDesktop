# Checklist — FEAT-016

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")`; append progress notes below.

- [ ] Read plan row `DSK-05-16`, `vertical-slices.md:550-573`, `screen-specs.md:343-361` and proposal §15.2; call `get_doc_gates FEAT-016`; `take_ticket` on branch `task/dsk-05-16-image-gallery`, worktree `../pegasus-worktrees/dsk-05-16-image-gallery`, from `origin/dev`
- [ ] Read `_ImageGallery.cshtml` and `GalleryImage.cs`, list in `research` every screen that shows images today and the metadata each has available for alt text, and record the SHA read
- [ ] Confirm the `/api/v1` image byte endpoints expose a size hint and a weak `ETag`; add them with [[GWY-010]]'s conventions where missing, so a thumbnail request does not fetch a full-resolution image
- [ ] Confirm `BitmapImage.DecodePixelWidth` / `DecodePixelHeight` decode-to-display-size semantics with `microsoft_docs_search` before writing the thumbnail service
- [ ] Implement `ImageThumbnailService` in `src/Pegasus.Desktop.Infrastructure`: decode to display size, a bounded LRU with an explicit item **and** byte ceiling, and prompt disposal on eviction or view unload
- [ ] Implement the `ImageGallery` control as one reusable, virtualized, progressive control with arrow-key traversal and Enter to open, and no colour-only state and no full-page spinner
- [ ] Give every gallery item an accessible name derived from the record's metadata
- [ ] Implement the `ImageViewer` opened **in place** over the current screen — not a new window, tab or shell navigation — with previous, next, download and close
- [ ] Make previous and next move within the gallery's current item set and stop at its ends
- [ ] Make `Escape` **and** a click outside both dismiss the viewer, trap focus while it is open, and return focus to the originating thumbnail on close
- [ ] Route the viewer's download through [[FEAT-014]]'s transfer service and give every viewer command an AutomationId beside `Case.Documents.Preview`
- [ ] Confirm [[FEAT-032]] has landed its preview pane and restate its named entry point in the plan document; if it has not, define the seam, record that, and render no document in the meantime
- [ ] Branch on item kind: an image opens the `ImageViewer`; a document calls [[FEAT-032]]'s preview surface; a type outside its safe list offers download through [[FEAT-014]] — with no reference to [[FEAT-040]] anywhere in this slice's code
- [ ] Adopt the control on the case Evidence tab, reading document records from [[FEAT-014]] rather than the custody ledger
- [ ] Adopt the control on the Received item ([[FEAT-009]]), Vehicle images ([[FEAT-012]]) and Triage detail ([[FEAT-011]], read-only from the origin receipt's retained assets) screens
- [ ] Delete any image rendering those screens grew of their own, leaving one gallery, one viewer and one thumbnail cache
- [ ] Add view-model tests for progressive load ordering, cache eviction at the ceiling, cancellation on view unload, and alt-text derivation from metadata
- [ ] Add view-model tests for previous/next boundaries at the ends of the item set, `Escape` and outside-click dismissal, a document item routing to [[FEAT-032]]'s seam rather than the `ImageViewer`, and an unsafe type falling back to download rather than opening bytes
- [ ] Measure on the **baseline Test/UAT workstation** that navigation is never blocked while thumbnails load and that working-set memory returns to a steady level after repeatedly opening and leaving an image-heavy case; record the figures and the workstation specification
- [ ] Add the `image-gallery` `winapp ui` script: keyboard traversal, open the viewer, page with previous/next, download, dismiss with `Escape` and with an outside click
- [ ] Run the `axe-windows` scan over the **open viewer** as well as the grid and attach both artefacts
- [ ] Update `parity-matrix.md` for the image and evidence rows
- [ ] Record the viewer contract in `screen-specs.md` § `§13.7 Documents and evidence` with the matching AutomationIds beside `Case.Documents.Preview` — this block only, leaving the `:230-231` case-workspace line to [[GWY-007]]
- [ ] Add the `DSK` row to `docs/capabilities.md`, leaving the FRD-13 gallery note and viewer contract to [[DUI-013]]
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test` for `Pegasus.Desktop.ViewModelTests` and `Pegasus.ArchitectureTests`, `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script image-gallery`, the axe artefacts and the performance record; this box produces `proof`
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
