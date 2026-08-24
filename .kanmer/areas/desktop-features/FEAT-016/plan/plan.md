# Plan — FEAT-016: S16 Images and gallery

**Diff estimate: ~18 files, ~1,850 lines.**

Derived from the `files` document, not asserted. `ImageThumbnailService` in
`src/Pegasus.Desktop.Infrastructure` — 2 files, ~260 lines (decode-to-display-size
plus a bounded LRU with item and byte ceilings and prompt disposal); the
`ImageGallery` control in `src/Pegasus.Desktop` — 3 files, ~380 lines (virtualized,
progressive, keyboard-traversable, per-item accessible name); the `ImageViewer`
surface — 3 files, ~330 lines (open in place, previous/next with end stops,
download, close, `Escape`, click-outside, focus trap and return); contracts —
1 file, ~70 lines (item-kind discriminator, accessible-name metadata, size hint);
`/api/v1` image byte endpoints gaining a size hint and a weak `ETag` in
`src/Pegasus.Web` — 1 file, ~80 lines; adoption on four screens including deleting
any rendering they grew — 4 files, ~230 lines;
`tests/Pegasus.Desktop.ViewModelTests` — 2 files, ~340 lines (nine named fact
groups); `tests/Pegasus.Desktop.UITests` — 1 script, ~110 lines;
`tests/Pegasus.ArchitectureTests` — 1 file, ~50 lines. Documentation is two blocks
totalling ~40 lines.

## Approach

Deliver the grid **and** the viewer in one slice, and branch on **item kind**
rather than growing a second renderer — because upstream CASE-011 asks for a
reusable gallery *viewer*, and a grid with no way to look at anything is not that.
The alternative considered and rejected was shipping the grid now and the viewer
as a follow-up: it would leave every image-bearing screen with a tile that either
does nothing or opens a raw byte URL, and a raw byte URL is one of the ticket's
named stop conditions. For documents the opposite call is made: the gesture is
identical (one click previews in place) but the **rendering** is handed to
[[FEAT-032]] (plan handle `DSK-07-06`) through its named entry point, because that
ticket already owns the preview pane, the `Case.Documents.Preview` AutomationId,
the safe-type list and the single binding to [[FEAT-040]] (plan handle
`DSK-07-14`)'s isolated document-render path — so this slice references
[[FEAT-040]] nowhere in code. Thumbnails decode to display size against a bounded
cache rather than relying on CSS constraint the way
`src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml:2-5` does today, because that
comment describes tiles that are full downloads and §15.2 requires the desktop to
hold a memory budget across 2–20+ files per case.

## Governing docs

The ticket carries `refs: ["docs/frd/frd-05-documents-extraction-and-custody.md"]`
and `docs_todo: true` (confirmed in `get_doc_gates FEAT-016`, which reports
`governing-doc` satisfied at `leave-backlog`).

**Meets — `docs/frd/frd-05-documents-extraction-and-custody.md`.** Step 8 has the
Evidence tab read **document records** rather than the custody ledger (upstream
DOCS-012), so custody remains the single record of what is held and the gallery is
a view over it; steps 6 and 7 route every download through [[FEAT-014]] (plan
handle `DSK-05-14`)'s transfer service so no byte path bypasses the custody-aware
one. The FRD is not modified by this ticket.

> **New ADR** — ADR-0100 (native WinUI 3 / Windows 11 desktop client, converted
> inside this fork, no WebView shell), authored by [[FND-005]] (plan handle
> `DSK-00-05`). Relevant here because the viewer must be a native surface and the
> preview path must never be a WebView hosting app UI.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0108 (report rendering in the desktop through an isolated,
> non-UI WebView2 HTML→PDF path), authored by [[FEAT-038]] (plan handle
> `DSK-07-12`). Named only to record the boundary: **this slice binds nothing to
> it** — [[FEAT-032]] is its single consumer for document preview.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review`:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §13.7 | Images and metadata handled without stalling the UI | Steps 4, 5, 11 |
| Proposal §15.2 | The memory budget holds after prolonged use | Steps 4, 11 |
| Proposal §14.9 | Keyboard and accessibility | Steps 5, 6, 12 |
| upstream CASE-011 (absorbed; **no fork ticket**) | A pop-out viewer with previous, next, close and outside-click dismissal, reused across every image-bearing page | Step 6 |
| upstream DOCS-011 (absorbed; **no fork ticket**) | Click-to-preview for documents as an inline disposition, with download as the fallback for anything not previewable | Step 7 |
| upstream DOCS-012 (absorbed; **no fork ticket**) | Evidence read from document records, not the custody ledger | Step 8 |
| `docs/engineering.md` § One Core owner | One gallery, one image viewer, one thumbnail cache | Steps 5, 6, 9 |
| `docs/design/README.md` § No explanatory copy | No colour-only state, no full-page spinner | Step 5 |
| `docs/desktop/06-ui-design/screen-specs.md:349-351,357-361` | Preview pane, safe-type list and `Case.Documents.Preview` are [[FEAT-032]]'s; the viewer contract is unstated and this ticket records it | Steps 7, 13 |
| L-01 | The gateway serves the bytes, the desktop renders | Steps 3, 4 |
| L-02 | Memory and latency measured on the local Test/UAT workstation | Step 11 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 13 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`;
  `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml` (performance
  and memory measurement)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `analyzing-dotnet-performance`
  (dotnet/skills `98f84851`,
  `plugins/dotnet-diag/skills/analyzing-dotnet-performance/SKILL.md`) →
  `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`) →
  `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search` for `BitmapImage.DecodePixelWidth` /
  `DecodePixelHeight` semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's thirteen implementation steps in the same order
and with the same ownership.

1. **Orient and take.** Read the plan row `DSK-05-16`,
   `docs/desktop/05-implementation-and-migration/vertical-slices.md:550-573`,
   `docs/desktop/06-ui-design/screen-specs.md:343-361` and proposal §15.2. Call
   `get_doc_gates FEAT-016`, then `take_ticket` with branch
   `task/dsk-05-16-image-gallery` and worktree
   `../pegasus-worktrees/dsk-05-16-image-gallery` from `origin/dev`.
2. **Read the web's gallery and list every image-bearing screen.** Read
   `src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml` (23 lines) and
   `src/Pegasus.Web/Presentation/GalleryImage.cs` (4 lines) and record in
   `research` every screen that shows images today **and the metadata each has
   available for alt text** — noting that `GalleryImage` carries only `(Href,
   FileName)`, so a richer accessible name has to come from the document or asset
   record. **Record the SHA read.**
3. **Size hint and validator on the byte endpoints.** Confirm the byte endpoints
   expose a size hint and a weak `ETag` so a thumbnail request does not fetch a
   full-resolution image. Today they do not:
   `src/Pegasus.Web/Pages/Intake/Image.cshtml.cs:48` sets only `nosniff` and `:53`
   returns `File(source.Content.ToArray(), …)`, materialising the whole image.
   Where the hint is missing, add it to the `/api/v1` image endpoints with
   [[GWY-010]] (plan handle `DSK-03-10`)'s conventions.
4. **`ImageThumbnailService`.** Use `microsoft_docs_search` for
   `BitmapImage.DecodePixelWidth` and `DecodePixelHeight` to confirm
   decode-to-display-size semantics, then implement the service in
   `src/Pegasus.Desktop.Infrastructure` *(created by [[FND-031]], plan handle
   `DSK-02-06`)*: decode to the display size, **never full resolution**; a bounded
   LRU cache with an explicit **item and byte ceiling**; prompt disposal when an
   item leaves the cache or the view unloads.
5. **The `ImageGallery` control.** Implement it in `src/Pegasus.Desktop`
   *(created by [[FND-030]], plan handle `DSK-02-05`)* as a single reusable
   control: virtualized; **progressive** — a placeholder appears immediately and is
   replaced as each thumbnail arrives; keyboard traversable with arrow keys and
   Enter to open; and an accessible name per item taken from the record's
   metadata. **Never a colour-only state and never a full-page spinner.**
6. **The image viewer.** Implement the surface the gallery opens into — the half
   upstream CASE-011 asks for, without which this ticket ships a thumbnail grid
   with no way to look at anything. One `ImageViewer` in `src/Pegasus.Desktop`
   beside the control, **opened in place over the current screen** rather than in a
   new window, tab or shell navigation, carrying previous, next, download and
   close. Previous and next move within the same item set the gallery is showing
   and **stop at its ends**. `Escape` **and** a click outside the viewer both
   dismiss it; **focus is trapped while it is open and returns to the originating
   thumbnail on close**. Download goes through [[FEAT-014]] (plan handle
   `DSK-05-14`)'s transfer service, never a second byte path. Every command carries
   an AutomationId beside `Case.Documents.Preview`.
7. **Hand a non-image item to [[FEAT-032]] instead of rendering it (upstream
   DOCS-011).** Check first that [[FEAT-032]] has landed its preview pane, and
   restate its shape from that ticket's step 7 before writing a line of code — it
   is the single owner of the document-preview path. The interlock, stated in the
   same words in both bodies:
   > *Document preview has one owner: [[FEAT-032]]. It owns the Documents-tab
   > preview pane, the `Case.Documents.Preview` AutomationId, the safe-type list,
   > and the single binding to [[FEAT-040]]'s isolated document-render path.
   > [[FEAT-016]] owns the image gallery and the image viewer — previous, next,
   > download, close, `Escape` and click-outside — across every image-bearing
   > screen. When a gallery item is a document rather than an image, [[FEAT-016]]
   > does not render it: it hands off to [[FEAT-032]]'s preview surface through
   > that ticket's named entry point, and where the type is not on [[FEAT-032]]'s
   > safe list it offers download through [[FEAT-014]]'s transfer service instead
   > of opening raw bytes. Two surfaces, one entry point per kind: a second
   > document-render binding, a second safe-type list, or a PDF opened by the
   > gallery itself is a stop condition.*
   Concretely: the gesture is identical for both kinds — one click on a tile
   previews in place — but the branch is on **item kind**; the image branch opens
   the step 6 viewer and the document branch calls [[FEAT-032]]. **This ticket
   references [[FEAT-040]] nowhere in code.** If [[FEAT-032]] has not landed,
   **define the seam it will implement, record that here, and render no document in
   the meantime.**
8. **Adopt on the case Evidence tab**, reading document records from
   [[FEAT-014]] rather than the custody ledger — this is upstream DOCS-012,
   absorbed by S14/S16.
9. **Adopt on the other three screens.** The Received item screen ([[FEAT-009]],
   plan handle `DSK-05-09`), the Vehicle images screen ([[FEAT-012]], plan handle
   `DSK-05-12`) and the Triage detail screen ([[FEAT-011]], plan handle
   `DSK-05-11`, upstream INTK-034 — **read-only, from the origin receipt's retained
   assets, not retained a second time**). **If any of them already grew its own
   image rendering, delete it in this slice** — a second implementation is a stop
   condition (`docs/engineering.md` § One Core owner).
10. **View-model tests.** In `tests/Pegasus.Desktop.ViewModelTests` *(created by
    [[TEST-004]], plan handle `DSK-08-04`)*: progressive load ordering; cache
    eviction at the ceiling; cancellation when the view unloads; alt-text derivation
    from metadata; previous/next boundaries at the ends of the item set; `Escape`
    and outside-click dismissal; a **document** item routing to [[FEAT-032]]'s
    preview seam rather than to the `ImageViewer`; and a type outside [[FEAT-032]]'s
    safe list falling back to download rather than opening bytes.
11. **Operator step — measure on the baseline Test/UAT workstation.** Navigation
    is never blocked while thumbnails load, and working-set memory returns to a
    steady level after repeatedly opening and leaving an image-heavy case. Use
    `analyzing-dotnet-performance` for the method and record the figures **and the
    workstation specification** in the proof. Figures from a developer machine are
    not acceptable.
12. **UI and accessibility.** Add a `winapp ui` script under
    `tests/Pegasus.Desktop.UITests` *(created by [[TEST-006]], plan handle
    `DSK-08-06`)* traversing the gallery by keyboard on an image-heavy case, opening
    the image viewer, paging with previous and next, downloading, and dismissing
    with **both** `Escape` and an outside click. Run the `axe-windows` scan from
    [[TEST-009]] (plan handle `DSK-08-09`) over the **open viewer** as well as the
    grid, and attach both artefacts.
13. **Documentation, simplification pass, PR.** Update `parity-matrix.md` for the
    image and evidence rows. Record the **viewer contract** in
    `docs/desktop/06-ui-design/screen-specs.md` § `§13.7 Documents and evidence` —
    open in place, previous/next, download, close, `Escape` and click-outside
    dismissal, a document previewed through [[FEAT-032]]'s preview surface, download
    offered for a type outside its safe list — with the matching AutomationIds
    beside `Case.Documents.Preview`; §13.7 promises the CASE-011 gallery viewer
    today (`:357-359`) and never states its behaviour. **Edit this block only**:
    `screen-specs.md` is edited once per block by that block's owner, [[GWY-007]]
    (plan handle `DSK-03-07`) owns the `:230-231` case-workspace line, and neither
    edits the other's. The FRD-13 gallery note **including the viewer contract is
    written by [[DUI-013]]** (plan handle `DSK-06-13`), not by this ticket. Add the
    `DSK` row to `docs/capabilities.md`. Run the simplification pass over this
    branch's diff, record it under a dated `## Simplification pass` heading below,
    then open the PR into `dev`.

## Verification

Evidence tiers from the body: **7** (Browser/accessibility), **10**
(Performance/concurrency).

- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — progressive-load, eviction, cancellation, alt-text, previous/next boundary,
  dismissal, document-routes-to-[[FEAT-032]] and download-fallback facts pass.
- `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script image-gallery`
  — keyboard traversal passes, the image viewer opens in place and pages with
  previous/next, `Escape` and an outside click both dismiss it; axe report attached
  with **no critical finding for the grid or the open viewer** (tier 7: keyboard,
  focus, semantic-label and text-plus-colour evidence from a real run).
- Performance record in the proof — navigation unblocked during thumbnail load and
  steady memory after repeated navigation, **with the workstation specification
  stated** (tier 10: measured against 2–20+ files per case, not an asserted
  budget).
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`
  — no duplicate gallery, viewer or thumbnail implementation, and no reference
  from this slice to the isolated document-render path that [[FEAT-032]] alone
  binds.

## Risks / open questions

- **Shipping a grid with no viewer.** The ticket's own step 6 names this: without
  the viewer the slice delivers a thumbnail grid with no way to look at anything.
  Mitigation: the viewer is in the same slice, with its own acceptance criterion,
  its own view-model facts and its own axe scan.
- **A second document renderer.** [[FEAT-032]] owns the preview pane, the
  safe-type list, the `Case.Documents.Preview` id and the [[FEAT-040]] binding.
  Mitigation: step 7 restates the interlock verbatim, the architecture test asserts
  no reference to the isolated render path, and a view-model fact asserts a document
  routes to the seam. If [[FEAT-032]] has not landed, the seam is defined and **no
  document is rendered in the meantime**. Answered by: [[FEAT-032]].
- **The byte endpoints may not accept a size hint.** Then every thumbnail fetches
  a full-resolution image and the memory budget is at risk. Mitigation: step 3 adds
  it under [[GWY-010]]'s conventions; if it cannot be added, raise it on
  [[GWY-010]] rather than absorbing it silently. Answered by: [[GWY-010]].
- **`DecodePixelWidth` semantics.** The whole memory budget rests on decode-to-size
  rather than decode-then-scale. Mitigation: `microsoft_docs_search` at step 4 is
  the check and the tier-10 measurement at step 11 is the proof.
- **Trading accessibility for the viewer.** The web's script-free gallery was
  deliberate — `src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml:2-5` says so.
  Mitigation: keyboard traversal, `Escape`, focus trapping and focus return are
  acceptance criteria, are asserted in view-model tests, and are scanned in the
  open viewer as well as the grid.
- **Performance figures from the wrong machine.** A named trap. Mitigation: step
  11 is an operator step on the baseline Test/UAT workstation and the proof states
  the specification.
- **An adopting screen may already have its own rendering.** Mitigation: step 9
  deletes it in this slice. [[FEAT-011]] step 10 already anticipates this ticket and
  forbids a second renderer on its side.
- **`screen-specs.md` double-editing and premature FRD-13 adoption.** The
  coordination rule is stated in the same words in [[GWY-007]] and here: one block,
  one owner; and **[[DUI-013]] must not adopt either block until both corrections
  have landed**, recording in its plan which case applied. If it runs first it
  freezes an uncorrected absorbed list and an unstated viewer contract into FRD-13
  and both must then be corrected twice. Answered by: [[DUI-013]] and [[GWY-007]].
- **Id hygiene.** upstream CASE-011, DOCS-011, DOCS-012 and INTK-034 were **none of
  them imported** and have no fork tickets, so none may be written as a board
  wiki-link; and the board's `INTK-001`…`INTK-007` are upstream INTK-002, INTK-003,
  INTK-026, INTK-027, INTK-031, INTK-032 and INTK-033 — none of which is INTK-034.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
