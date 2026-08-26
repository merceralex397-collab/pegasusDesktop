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
