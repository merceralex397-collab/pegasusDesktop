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
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
archived: false
created: '2026-08-24T07:57:08.715Z'
updated: '2026-08-24T07:57:08.715Z'
---

## What

Build one reusable image gallery control used by every image-bearing screen, with progressive thumbnails that never block navigation, a bounded thumbnail cache, prompt disposal, keyboard traversal and alt text from metadata; the case Evidence tab reads document records through it.

## Why

Proposal §13.7 and §15.2 require images and metadata to be handled without stalling the UI, and the memory budget to hold after prolonged use. Today the web has one partial, `src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml`, with the tiny view model `src/Pegasus.Web/Presentation/GalleryImage.cs` and receipt-asset image endpoints; upstream CASE-011 asks for a reusable gallery viewer and this slice absorbs it. Without a single control each image-bearing screen would grow its own decoding and caching behaviour, which the one-list-per-concept rule forbids. Siblings: [[DSK-05-14]] supplies the transfer service and document records, [[DSK-05-09]] and [[DSK-05-12]] are the other image-bearing surfaces.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-16`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S16 · Images and gallery (DSK-05-16)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Intake (received items), uploads, image intake` (image byte endpoints) and § `Cases` (document content with `ETag` and range)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.7 Documents and evidence — Case workspace › Documents tab` (evidence gallery)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.7 Documents and evidence, § 15.2 Implementation practices, § 14.9 Keyboard and accessibility
- Repository evidence: `src/Pegasus.Web/Pages/Shared/_ImageGallery.cshtml`, `src/Pegasus.Web/Presentation/GalleryImage.cs`, `src/Pegasus.Web/Pages/Intake/Image.cshtml.cs` (receipt-asset image endpoint), `src/Pegasus.Core/ImageIntake/`
- Binding decisions: L-01 the gateway serves the bytes, the desktop renders; L-02 the memory and latency measurements run on the local Test/UAT workstation; L-04 routing named on the ticket
- Depends on: `DSK-05-14` the transfer service, document records and the Documents tab this gallery first lands in

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
6. Adopt the control on the case Evidence tab, reading document records from [[DSK-05-14]] rather than the custody ledger — this is upstream DOCS-012, absorbed by S14/S16.
7. Adopt the same control on the Received item screen ([[DSK-05-09]]) and the Vehicle images screen ([[DSK-05-12]]). If either already grew its own image rendering, delete it in this slice — a second implementation is a stop condition (`docs/engineering.md` § One Core owner).
8. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for progressive load ordering, cache eviction at the ceiling, cancellation when the view unloads, and alt-text derivation from metadata.
9. **Operator step** — measure on the baseline Test/UAT workstation: navigation is never blocked while thumbnails load, and working-set memory returns to a steady level after repeatedly opening and leaving an image-heavy case. Use `analyzing-dotnet-performance` for the method and record the figures and the workstation specification in the ticket proof.
10. Add a `winapp ui` script under `tests/Pegasus.Desktop.UITests` traversing the gallery by keyboard on an image-heavy case, and run the `axe-windows` scan; attach both artefacts.
11. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the image and evidence rows, add the gallery note to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] One gallery control serves every image-bearing screen; no screen keeps a second implementation.
- [ ] Thumbnails appear progressively and never block navigation.
- [ ] Images decode to display size; the cache is bounded and items are disposed promptly.
- [ ] Memory is steady after repeated navigation through an image-heavy case.
- [ ] The gallery is keyboard traversable and every item has an accessible name from metadata.
- [ ] The Evidence tab reads document records, not the custody ledger.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: progressive-load, eviction, cancellation and alt-text facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script image-gallery` — expected: keyboard traversal passes; axe report attached with no critical finding.
- [ ] Performance record in the ticket proof — expected: navigation unblocked during thumbnail load and steady memory after repeated navigation, with the workstation specification stated.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: no duplicate gallery or thumbnail implementation.

## Evidence tier

Tier 7 — Browser/accessibility. Tier 10 — Performance/concurrency.
Tier 7 obliges keyboard, focus, semantic-label and text-plus-colour evidence from a real run of the gallery; tier 10 obliges measured memory and responsiveness against the stated per-case file volumes (2–20+ files per case) rather than an asserted budget.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — image and evidence rows
- `docs/frd/frd-13-desktop-operator-experience.md` — gallery behaviour note
- `docs/capabilities.md` — `DSK` row for the image gallery

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` image byte endpoints in `src/Pegasus.Web` and the test projects. Must not touch `src/Pegasus.Infrastructure/Vision/` or any Razor partial.
- **Traps**: one gallery implementation only — encountering a second is a stop condition; decode to display size and dispose promptly, or the memory budget fails; no colour-only state and no full-page spinner (`docs/design/README.md`); performance figures must come from the baseline workstation, never from a developer machine; upstream CASE-011 is absorbed here and must not be re-raised.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
