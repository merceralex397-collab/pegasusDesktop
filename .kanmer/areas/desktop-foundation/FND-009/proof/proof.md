# Verification proof

## Result

The documentation and release-skill convention is merged and verified on `main`. The first `gateway/r<N>` tag has not been applied because no production release is authorized in the current refactor phase; this acceptance item remains open and is not claimed as complete.

## Command log

- `git rev-parse origin/main origin/dev` → both `f7708625d5e960f0b6d27928393a96ae9ecf0ab9`.
- `git tag --list 'gateway/*'` → no output; no local gateway tag exists.
- `git ls-remote --tags origin 'refs/tags/gateway/*'` → no output; no remote gateway tag exists.
- `git grep -n -E 'gateway/r|desktop/v|origin/main|2×' origin/main -- docs/engineering.md .agents/skills/pegasus-release/SKILL.md .codex/skills/pegasus-release/SKILL.md` → convention hits in `docs/engineering.md` lines 38–43; both release-skill copies contain the read-back, immutable-tag rule, exact commands, and both tag forms at lines 80–89.
- `Get-FileHash` over the two release-skill files from `origin/main` → both `67B14C1F0A970D813BABBE05505673FF250CDBDCD506A96731BCCCE6E9DD79E1`.
- `gh run view 33285536347 --json ...` → `success`, push at exact head `f7708625d5e960f0b6d27928393a96ae9ecf0ab9`; local-development-scripts, documentation, changes, and reference-data passed; unit, infrastructure, SQL integration, browser, and coverage lanes were skipped for the docs-only diff.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` → `All relative Markdown links resolve (238 files checked).`
- `git diff --check` → passed on the remediation branch before merge.

## Merge evidence

- PR #52 exact reviewed head: `5d8be6841043c095b5fc7a2bc27127dbfa47a2e6`.
- Independent review: Heisenberg the 2nd, `PASS`, no findings.
- Merge commit on `dev` and promoted exact SHA: `f7708625d5e960f0b6d27928393a96ae9ecf0ab9`.

## Remaining handback

At the next authorized production gateway release, apply and push the first `gateway/r<N>` tag on the promoted `main` SHA, record it beside that release in `docs/operations.md`, and hand back `git tag --list 'gateway/*'` plus `git ls-remote --tags origin`. Until then FND-009 remains in `verifying`.
