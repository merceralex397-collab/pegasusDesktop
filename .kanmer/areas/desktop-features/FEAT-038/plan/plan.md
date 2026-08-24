# Plan — FEAT-038: accept ADR-0108 after Phase 7 evidence

## Scope

This ticket begins only after [[FND-007]] has merged ADR-0108 as `proposed`, [[FEAT-040]] has recorded packaged-controller validation, and [[FEAT-041]] has recorded passing golden-file parity. It changes only ADR-0108 frontmatter and `docs/adr/README.md`.

## Steps

1. Verify the proposed ADR and evidence references; confirm the documented reversal condition has not fired.
2. Confirm `git diff` has no planned ADR-body change and the index has no ADR-0108 row.
3. Change `status` to `accepted` and set the acceptance date in ADR-0108 frontmatter.
4. Add one accepted-table index row.
5. Run the two documentation checks, obtain independent review, and write proof citing [[FEAT-040]] and [[FEAT-041]].

## Verification

| Check | Expected result |
| --- | --- |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |
| ADR-0108 diff | status/date frontmatter only |
| ADR index | exactly one ADR-0108 accepted-table row |

## Boundaries

- No new ADR and no ADR body change.
- No WebView2 host selection or renderer implementation.
- No re-run of packaged-controller or parity tests; cite their ticket proof instead.

## Simplification pass

`n/a — two-document acceptance change only.`
