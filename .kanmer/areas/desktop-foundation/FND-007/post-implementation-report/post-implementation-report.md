# Post-implementation report — FND-007

## Scope correction — 2026-08-25

FND-007 is a Phase 0 proposed-ADR delivery only. It closes after its own merged and verified documentation PR; it no longer waits for Phase 7 renderer validation, parity, or the accepted-ADR change.

## Delivered branch work

- `docs/adr/0108-desktop-webview2-report-rendering.md` — proposed ADR recording the isolated, never-visible WebView2 report-rendering exception, fixed documented `HWND_MESSAGE` host, retained gateway renderer, parity gate, and reversal condition.
- `docs/desktop/00-governance-and-workflow/README.md` — Phase 0 wording aligned to the fixed host.
- `docs/desktop/07-integrations/README.md` — Phase 7 validates packaged integration and parity; it does not select a host.

No renderer code, test, package reference, accepted ADR index row, Azure resource, or change to ADR-0025/ADR-0028 is included.

## Branch evidence

- Commits: `39c704dc`, `d3762780`, and `f328076d`.
- `pwsh -NoProfile -File scripts/Test-DocumentationLinks.ps1` — passed.
- `pwsh -NoProfile -File scripts/Test-TestMarkdownPlacement.ps1` — passed.
- `git diff --check` — passed.

## Remaining delivery

FND-005 must merge to `dev` first so this descendant branch can be updated and opened as a three-file PR. After independent review and the normal merge/proof route, FND-007 closes with ADR-0108 still `proposed` and absent from `docs/adr/README.md`.

## Successor ownership

- [[FEAT-040]]: packaged desktop renderer and fixed-controller evidence.
- [[FEAT-041]]: golden-file parity evidence.
- [[FEAT-038]]: the only later `proposed` to `accepted` frontmatter change and accepted-index row.

Those tickets are successors, not FND-007 closure conditions.

## Independent review — 2026-08-25

PASS from `pegasus-desktop-reviewer` (agent `01a0374e-ea58-7111-8aaa-c9721c43b2b4`), which did not implement the ticket. No findings. The reviewer confirmed plan coverage, dependency boundaries, never-visible/never-UI scope, documented `HWND_MESSAGE` usage, gateway fallback, parity gates, untouched ADR index, documentation checks, and honest `n/a — docs-only` simplification. No code or .NET test evidence is claimed.

## Delivery blocker — 2026-08-25

- Branch `fnd-007-webview2-adr` pushed at `aa562e12`.
- `gh pr create --base dev` failed: `pull request create failed: GraphQL: must be a collaborator (createPullRequest)`.
- No PR, CI, merge, proof, or Done claim is made. The ticket remains blocked at repository integration until a collaborator-capable PR path is available.
