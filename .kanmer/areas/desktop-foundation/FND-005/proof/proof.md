# Verification proof — FND-005

## Merged result

- PR: [#1](https://github.com/merceralex397-collab/pegasusDesktop/pull/1), merged into `dev`.
- Verified checkout: ticket worktree `.worktrees/fnd-005`, detached at `origin/dev` merge commit `5770eb21c0d03620a6a6d99e0431bde91ec2ad6a`.
- Merge parents: `ecb9b7b4` and ticket commit `3c8f623c15c10c350999cc4d902eba9766eb94dc`.
- Scope: documentation-only; no runtime or deployment claim.

## Commands and results

- `pwsh -NoProfile -File ./scripts/Test-DocumentationLinks.ps1` — passed: all relative Markdown links resolve (232 files).
- `pwsh -NoProfile -File ./scripts/Test-TestMarkdownPlacement.ps1` — passed.
- `Get-ChildItem docs/adr/010*` — exactly six ticket-owned files: ADR-0100, ADR-0101, ADR-0103, ADR-0104, ADR-0105 and ADR-0110.
- Frontmatter and heading audit over all six ADR files — passed: all eight required keys and Status, Context, Decision, Consequences and Links headings present.
- Index audit — exactly one row for each of 0100, 0101, 0103, 0104, 0105 and 0110.
- `Select-String AGENTS.md 'Owner capability'` — 0 matches.
- `Select-String docs/desktop/00-governance-and-workflow/README.md 'superseded_by'` — 0 matches.
- ADR-0100 `supersedes: []` — present; ADR-0009 path diff — empty.
- ADR-0014 explicit non-supersession — present in ADR-0101 and ADR-0103.
- `git diff --check HEAD^1..HEAD` — passed (exit 0).

No runtime, Azure, release, or production-deployment evidence is claimed for this Tier-1 documentation ticket.

## Merge confirmation

`gh pr view 1 --json state,mergedAt,url,baseRefName,headRefName` → `MERGED`, `mergedAt=2026-08-25T00:12:46Z`, base `dev`, head `fnd-005-foundation-adrs`, URL `https://github.com/merceralex397-collab/pegasusDesktop/pull/1`.
