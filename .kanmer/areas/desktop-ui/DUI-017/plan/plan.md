# Plan — DUI-017 Capture prototype-to-page screen map

## Governing documents

Docs/design/README.md remains the authority. The new screen map is a durable reference artefact under docs/design/references/, linked from docs/index.md; it must not become a competing design specification.

## Steps

1. Retrieve the cited Claude Design project github.md using authorised project access and preserve source/provenance date.
2. Map each approved prototype to the corresponding replaces Pages heading in screen-specs.md; flag any unmapped/ambiguous row rather than guessing.
3. Add docs/design/references/screen-map.md with mapping, source identity and reference-only status.
4. Add the single index link, validate markdown/documentation checks, and tell DUI-013 exactly what reference it can consume.

## Verification

- [ ] Every claimed prototype/page mapping has an identified source line.
- [ ] The map is linked from the documentation index and has no normative behaviour.
- [ ] Documentation checks pass and no unrelated design authority was edited.

## Risks

Authorised access to the correct Claude Design project is required; .design-sync/config.json identifies a different project and is not a substitute.

## Execution evidence — 2026-08-25

- DesignSync was not available in this session, so the plan's documented fallback was used: upstream `origin/kanmer-board` at `a5b28111`, `platform-operations/PLAT-001/files/files.md`, whose provenance records Claude Design project `710bb42f-84ed-4d82-b216-7c5d60fb5aef` (Pegasus Design), `repo: collisionengineers/pegasus`, and `github.md` last sync `2026-08-16`.
- Added `docs/design/references/screen-map.md` as a reference artefact, with 21 source prototype mappings plus the shared Search row, the corrected fifteen replacement-heading count, explicit no-prototype headings, remote-only status, related in-tree hashes, and the fallback provenance.
- Added the single question-table link to `docs/index.md`; added the map link to `docs/desktop/06-ui-design/README.md`; extended PAR-22's design-source cell to reach the map. `docs/design/README.md` was not changed.
- `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` — passed.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` — passed; 233 Markdown files checked.
- Structural audit: map header/provenance/UUID/remote-only/SHA markers present; exact source table and heading sections are present; `git diff --check` passed.

## Simplification pass — 2026-08-25

- `n/a — docs-only`: the implementation adds one canonical reference map and links to it from the three required consumers; it does not duplicate the design authority or create a second asset register.
- Reused the existing `docs/design/references/mockups/` asset location and SHA convention; related asset hashes were recorded rather than copied or substituted.
- No DesignSync write, binary asset mutation, source/test change, new governing document, or edit to `docs/design/README.md` was introduced.

## Current status

Documentation implementation and local validation are complete. Commit, independent review, PR/CI, merge to `dev`, proof on merged `main`, and Kanmer closeout remain. The proof document is intentionally not written before merged-main verification per AGENTS.md.

## Review disposition — 2026-08-25

Independent `pegasus-desktop-reviewer` review of commit `540399d8` returned `FAIL` with one fidelity finding: the fallback source row for `Pages/Search/Index.cshtml` changed the literal first-column value `—` to `No prototype`. Corrected the row to preserve `—` verbatim and retained the explicit no-prototype explanation in the Notes column. No other finding was reported.

Post-fix validation: `Test-DocumentationLinks.ps1` passed with 233 files; `Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` passed; `git diff --check` passed; the intended four-file diff remains unchanged. Fresh independent review is required before merge.

## Fresh independent review — 2026-08-25

`pegasus-desktop-reviewer` reviewed commit `060dd9be` and returned `PASS` with no unresolved findings. It verified the Search row's literal `—` plus explicit no-prototype reason, fallback provenance, 21 source entries, 15 replacement headings, no-desktop/no-prototype cases, SHA/link evidence, exact scope, and docs-only simplification.

## Delivery blocker — 2026-08-25

- Branch `dui-017-screen-map` is pushed through commit `060dd9be`; independent review is `PASS`.
- `gh pr create --base dev --head dui-017-screen-map` failed with exact error: `pull request create failed: GraphQL: must be a collaborator (createPullRequest)`.
- Ticket cannot proceed to CI, merge, merged-main proof, or closeout until repository collaborator permission is available. No PR or proof is claimed.
