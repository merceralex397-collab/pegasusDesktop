# Plan — FND-007: merge ADR-0108 as proposed in Phase 0

## Scope correction — 2026-08-25

FND-007 ends with its own proposed-ADR delivery. It does not remain open for packaged-controller validation, golden-file parity, or ADR acceptance. Those Phase 7 responsibilities belong to [[FEAT-040]], [[FEAT-041]], and [[FEAT-038]] respectively.

## Scope

The branch contains exactly three documentation changes: `docs/adr/0108-desktop-webview2-report-rendering.md`, `docs/desktop/00-governance-and-workflow/README.md`, and `docs/desktop/07-integrations/README.md`. ADR-0108 remains `proposed`; the ADR index is deliberately unchanged.

## Delivery sequence

1. Wait for [[FND-005]] to merge to `dev`, then update this branch from `dev` so its PR contains only the three FND-007 documents.
2. Re-run `pwsh ./scripts/Test-DocumentationLinks.ps1`, `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`, and `git diff --check`.
3. Open a PR to `dev` and obtain independent review that the ADR is a narrow never-UI rendering exception, not a WebView shell.
4. After the Phase 0 delivery reaches `main` through the normal release route, write proof that ADR-0108 is still `proposed`, has no accepted-index row, and the documentation checks/review passed. Close FND-007.

## Explicit hand-off

- [[FEAT-040]] implements the renderer and validates the fixed `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)` controller in the packaged app.
- [[FEAT-041]] produces the approved-fixture parity results.
- [[FEAT-038]] alone changes ADR-0108 from `proposed` to `accepted`, sets the acceptance date, and adds the accepted ADR index row after the evidence exists.

## Verification

| Command | Expected result |
| --- | --- |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |
| `git diff --check` | no output |
| `grep -n '^status:' docs/adr/0108-*.md` | `proposed` |
| `grep -n '0108' docs/adr/README.md` | no output |

## Simplification pass

2026-08-25 — `n/a — docs-only`. The documented `HWND_MESSAGE` controller replaces the obsolete host-selection alternatives; no new abstraction, code path, or dependency is added.

## Independent review — 2026-08-25

PASS from `pegasus-desktop-reviewer` (agent `01a0374e-ea58-7111-8aaa-c9721c43b2b4`), no findings. The review covered the exact three-file diff against `origin/dev`, the proposed status, never-visible/never-UI boundary, `HWND_MESSAGE` controller, gateway fallback, parity gate, untouched accepted index, documentation validation, and simplification entry.

## Closeout correction — 2026-08-26

The earlier delivery-blocker note is superseded. PR #13 (`d4c17fdd`) merged into `dev` on 2026-08-25. Its exact-head repository-check run `32897874831` passed all applicable documentation, changes, local-development-script and reference-data checks; build, infrastructure, integration, browser and coverage lanes were correctly skipped for this docs-only diff. Read-only checks against `origin/main` `80d9f96d` confirm the final ADR-0108 body and both source-plan corrections are present, with `status: proposed` and no ADR-0108 row in `docs/adr/README.md`. Independent review PASS and `n/a — docs-only` simplification are already recorded. The ticket is ready for proof and Done; Phase 7 validation and later acceptance remain with [[FEAT-040]], [[FEAT-041]] and [[FEAT-038]].
