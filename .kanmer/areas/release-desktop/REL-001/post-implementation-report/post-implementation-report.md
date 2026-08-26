# Post-implementation report — REL-001

## Delivered

- Appended the Area 09 release contract to the existing canonical ADR-0105 at docs/adr/0105-msix-app-installer-and-minimum-version-gate.md.
- Recorded the explicit App Installer 2021 schema and update attributes, package version 1.<minor>.<build>.0 with CI build/revision rule, the single CollisionEngineers.Pegasus identity with pilot/prod ring model, rollback with ForceUpdateFromAnyVersion, and the dated D-002/D-003 no-Azure consequence.
- Left docs/adr/README.md unchanged because it already contains the single canonical ADR-0105 row.
- Created no second ADR-0105 and changed no source, tests, scripts, CI, operations, cloud, or upstream repository.

## Scope and governing evidence

The change reconciles the existing FND-005-owned ADR with docs/desktop/09-release-update-and-distribution/README.md §3 and the signing/hosting decision matrix. It records already-settled decisions; it does not change D-002 or D-003 and does not claim package generation, feed publication, deployment, runtime acceptance, or Azure work.

## Branch and validation

- Branch: task/rel-001-adr-0105-reconciliation
- Commit: 62e8e680
- Diff: one file, docs/adr/0105-msix-app-installer-and-minimum-version-gate.md
- git diff --check — passed.
- pwsh -NoProfile -File ./scripts/Test-DocumentationLinks.ps1 — passed; all relative Markdown links resolve (235 files checked).
- pwsh -NoProfile -File ./scripts/Test-TestMarkdownPlacement.ps1 — passed.
- Get-ChildItem docs/adr -Filter 0105* — exactly one file.
- docs/adr/README.md — existing ADR-0105 row unchanged.

## Review handoff

Independent review must confirm that the appended text is limited to Area 09's missing requirements, preserves the accepted decision, contains no unsupported product decision, and satisfies the ticket plan's simplification-pass record.

## Verification after merge

On merged main, re-run the two documentation scripts, inspect the rendered ADR for the five required decision areas, confirm one ADR-0105 file and one index row, and record proof. This ticket proves documentation consistency only; downstream packaging and release tickets must prove generated package/feed/runtime behavior.

## Review response — 2026-08-26

The independent reviewer identified two blocking clarity/completeness findings. Both were fixed in commit 17c87e51:

- Added the planned Relates section for ADR-0007, ADR-0014, and the future FRD-13 pointer owned by DSK-00-08/FND-008.
- Changed “package manifest” to “App Installer file” so it cannot be confused with Package.appxmanifest.

The documentation-link and Markdown-placement gates were rerun after the fix and passed. The PR head is now 17c87e51; CI must be rechecked at that exact head.

## Independent review — 2026-08-26

The second independent reviewer returned NEEDS CHANGES at commit 62e8e680, then identified the same scope on the final PR head. Blocking findings:

1. The branch edits a published status: accepted ADR in place, contrary to AGENTS.md and docs/adr/README.md, which require immutable accepted bodies and a new superseding ADR for a changed decision. This requires an explicit governance amendment or a valid superseding-ADR route; no merge is authorized under the current record.
2. The six-row cloud-justification table is not scoped to feed versus gateway. The next revision must make the feed answers and the gateway central-enforcement answer explicit, without inventing an Azure requirement.
3. ForceUpdateFromAnyVersion must be described as the App Installer XML element/value form, matching the canonical template, not as an attribute.

The review also confirmed the Relates section and file-name wording were fixed, exact local documentation checks passed, the one-file scope is otherwise correct, and no runtime/cloud/packaging claim is made. PR #22 remains open and must not be merged until the governance conflict is resolved.

## Independent review disposition — 2026-08-26

A second independent reviewer returned NEEDS CHANGES. The Relates and terminology findings were addressed in 17c87e51, but the following blockers remain:

- The branch modifies a published status: accepted ADR in place. AGENTS.md and docs/adr/README.md require accepted bodies to remain immutable and require a new superseding-ADR route for a changed decision. This needs an explicit governance amendment or a valid superseding ADR; PR #22 must not merge under the current record.
- The cloud-justification table must explicitly scope feed versus gateway. Feed answers must not be conflated with the gateway's central enforcement rationale.
- ForceUpdateFromAnyVersion must be described using the canonical App Installer XML element/value form, not as an attribute.

The review confirmed exact local validation, one-file scope, no unsupported runtime/cloud claims, and correct docs-only simplification evidence.
