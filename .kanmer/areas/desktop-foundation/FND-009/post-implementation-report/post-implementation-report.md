# FND-009 post-implementation report

## Implementation

PR #24 merged the release-tag convention into `dev` at merge commit `f26b5b01d509ad21d9db58bca9fb00afe77c384a`; its exact ticket head was `322e18bda664f3b800c9614a64513a4be6b11e34`. The convention is present in `docs/engineering.md`, `.agents/skills/pegasus-release/SKILL.md`, and the byte-identical `.codex/skills/pegasus-release/SKILL.md`.

## Validation

- GitHub Actions `33009752135` matched the exact ticket head and passed documentation, changes, local-development-scripts and reference-data; .NET, SQL and browser lanes were path-skipped for the docs-only diff.
- `git tag --list 'gateway/*' 'desktop/*'` — no tags.
- `git ls-remote --tags origin 'refs/tags/gateway/*' 'refs/tags/desktop/*'` — no tags.
- Independent review was retrospective, not pre-merge.

## Acceptance status

The convention exists, but the canonical guidance needs a small wording remediation for the explicit post-promotion read-back and C-01/2x runner-cost rationale. The first `gateway/r<N>` tag and its `docs/operations.md` record are still pending the next authorized production release. This ticket is not eligible for Done and no release or tag proof is claimed.

## External handback

At the next authorized release, the release actor must apply the computed immutable `gateway/r<N>` (and desktop version tag when applicable) only after exact `main` read-back, then hand back `git tag --list 'gateway/*'` and `git ls-remote --tags origin` output plus the operations-document change.
