# Proof — REL-001

## Delivered and reviewed

- PR #22: https://github.com/merceralex397-collab/pegasusDesktop/pull/22
- Final PR head: `be26313fefa1ece3673dbad16d3b759dfe328e60`.
- Exact-head hosted CI run `32996686262` passed all applicable jobs; docs-only build/integration lanes were correctly skipped.
- Independent final review by Darwin passed after the governance correction.
- PR #22 merged into `dev` at `80d9f96d64b1dfbeea4658adfc99351f71b303d7`.
- `dev` was promoted to `main` by an exact non-force fast-forward after the required merge authorization.

## Merged-main evidence

Verified against `origin/main` at `80d9f96d64b1dfbeea4658adfc99351f71b303d7`:

- `docs/adr/0031-desktop-release-distribution-contract.md` exists with `id: ADR-0031`, `status: accepted`, and `supersedes: [ADR-0105]`.
- `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` exists with `status: superseded` and `superseded_by: [ADR-0031]`.
- ADR-0031 contains the scoped Feed/Gateway placement table, canonical rollback XML element, and the documented package/version contract.
- `git diff --check 36dccd8fa1c883c38977b6721d86b745c45c9a94 80d9f96d64b1dfbeea4658adfc99351f71b303d7` passed.

The final PR changed only the ADR-0031 document, ADR-0105 frontmatter, and the ADR index. No Azure, deployment, signing, feed publication, or upstream operation was performed.
