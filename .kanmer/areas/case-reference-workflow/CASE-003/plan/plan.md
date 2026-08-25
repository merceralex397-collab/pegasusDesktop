# Plan — Triage authority reconciliation

## Objective

Make the repository’s governing and directly affected downstream documents state one operator-approved Triage model: a Triage is its own aggregate and product case, identified by an immutable `T-00001`-style reference with its own custody. It is not the normal `Case` aggregate and receives no Principal or Case/PO until a later formal instruction passes the existing normal acceptance and allocation gates. Conversion creates a linked standard Case and an immutable, non-duplicating custody-transfer record.

## Governing docs

- `docs/operator-notes.md` — explicit 2026-08-25 operator authority permits this ticket to replace the outdated Stage 0 meaning while preserving material workflow/finding statements.
- `docs/prd/pegasus-product.md` — states the intended product outcome and permanent identity boundary.
- `docs/frd/frd-03-triage.md` — owns Triage identity, lifecycle, custody, conversion conditions, and transfer-record behaviour.
- `docs/frd/frd-01-case-identity-and-lifecycle.md` — keeps Principal and Case/PO allocation solely on the normal Case side of conversion.
- New `docs/adr/0030-triage-as-separate-aggregate.md` — records the durable Core aggregate boundary. The user’s explicit “separate aggregate” decision is architectural; the ADR will not contain FRD behaviour or storage design.

## Steps

1. Add thin ADR-0030 and its index row. Reuse ADR-0029’s established separate-record decision format, but do not conflate Triage with Image Intake or prescribe schema/Box mechanics.
2. Amend Stage 0 and the PRD. Replace “pre-case”/“does not count as a case” with the separate product-case aggregate and T-reference outcome; retain the existing finding, reply-evidence, non-definitive, and normal formal-Case boundaries.
3. Amend FRD-03 and FRD-01. FRD-03 defines its immutable T-reference, separate custody, conversion trigger, and transfer-record fields (source Triage reference, transfer time, actor/system, destination Case/PO, and transferred content/version identities). It explicitly forbids duplicate evidence copies. FRD-01 makes clear that normal Case allocation remains the conversion’s first Principal/Case/PO allocation point.
4. Align the capability registry and design authority where they repeat the obsolete “pre-case” boundary. Reuse FRD-03 as the behaviour owner; introduce no new UI, endpoint, data-model, or delivery claim.
5. Inspect the focused diff for accidental scope and wording divergence. Record `n/a — docs-only` for the required simplification pass.
6. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD`. Check ADR front matter/index and the changed-document diff. Record exact results in the post-implementation report and scratch.
7. Commit only the named documents, link ADR-0030 to CASE-003 after it exists, push the task branch, and open a PR to `dev`. Obtain independent documentation review. Do not merge unless review passes and GitHub exposes and passes the required CI checks.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Protected operator truth could be rewritten beyond the direct decision. | Restrict Stage 0 change to the precise Triage identity/aggregate/custody/conversion wording; independent review compares it to the ticket’s recorded decision. |
| Normal Case allocation gates could be weakened. | State only that conversion uses the existing normal formal-instruction, principal, and allocation gates; FRD-01 remains their owner. |
| A documentation task could imply a delivered architecture. | State that code has no T-reference or transfer behaviour yet; file/maintain a separately scoped implementation follow-up rather than inventing schema. |
| Downstream text could preserve the contradiction. | Align the capability registry and design statements that explicitly call Triage pre-case; leave unrelated plan/ticket material for its owning ticket to re-plan. |
| The PR cannot be truthfully merged. | Require independent review and actual registered/passing CI. Existing zero registered workflows is a known external merge blocker. |

## Proof

This task’s proof is the merged `dev` commit, independent review, passing documentation checks, and a final diff that demonstrates all authoritative statements agree. It makes no deployment, runtime, user-acceptance, or implementation claim.

## Simplification pass

2026-08-25 — n/a — docs-only. The plan changes only the minimum governing/downstream documents required to reconcile the direct operator decision; no code abstraction or compatibility path is introduced.

## Scope adjustment — 2026-08-25

A targeted contradiction search added the existing runbook verification line and desktop parity-matrix description to step 4. They are direct consumers of the former pre-case wording and remain documentation-only.

## Independent-review correction — 2026-08-25

The independent reviewer found a High finding: `docs/design/README.md`, PAR-24 in the desktop parity matrix, and S11 in `vertical-slices.md` still prescribed Triage Case link/unlink behaviour. That conflicts with the direct operator decision and FRD-03's one-way, normal-gate conversion with immutable non-duplicating transfer record.

Disposition: applied. The two design state tables now exercise conversion refused/pending/completed plus its transfer record. PAR-24 inventories existing link/unlink as legacy dispatcher behaviour to be replaced, while its target explicitly bans arbitrary Case link/unlink. S11 now requires conversion status/refusal/completion and bans arbitrary link/unlink. This is the smallest direct-downstream scope needed to satisfy the non-contradictory-model acceptance criterion; no UI, API, code, schema, or migration behaviour was added.
