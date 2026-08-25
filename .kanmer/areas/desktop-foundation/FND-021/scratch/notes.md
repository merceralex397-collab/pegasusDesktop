## Material checkpoint — 2026-08-25

FND-021 remains implementing. Evidence and canonical docs are updated:
- live Azure MCP subscription/group/RG/per-type reads captured in research and scratch;
- safe CLI supplements captured where installed Azure MCP schemas lack child commands;
- register rows now have exact path:line owners and exact §19 target vocabulary;
- dated declared-absent checks and dated Drift observed section added;
- area 11 disposition note mirrors the drift without proposing or applying repair;
- tags remain unapplied with exact-target approval text recorded;
- Test-DocumentationLinks, Test-MarkdownPlacement, and git diff --check all pass.

Open acceptance blockers:
1. U-1 remains unchecked because the authenticated identity is Owner at subscription scope, not Reader-scoped.
2. U-9 remains unchecked because current spend 29.50478827580924 GBP versus 75.0 GBP is proven, but no usable forecast amount was exposed by Azure MCP/pricing/budget reads; forecast endpoint attempts returned 415/400 and made no state change.

Next action: obtain a reader-scoped Azure session and a usable read-only forecast result (or explicit operator decision that the unavailable forecast is an accepted external dependency), then refresh the open questions, rerun the documentation checks, obtain independent review, and follow the PR/merge workflow. No Azure write occurred.
