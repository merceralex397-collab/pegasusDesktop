# Research — REL-001

## Question

Does the FND-005-owned ADR-0105 already satisfy Area 09 §3 and the REL-001 acceptance criteria, and if not, what exact in-repository documentation change is required?

## Findings

1. **Canonical ownership and collision state were verified live on 2026-08-26.** Kanmer search_items ADR-0105 returned FND-005 as done with the canonical ref docs/adr/0105-msix-app-installer-and-minimum-version-gate.md; FND-042 is also done and references the same path. No active ADR-0105 authoring ticket was found. The local docs/adr/0105* check on the current task branch is empty because this branch predates the merged documentation; git cat-file -e origin/dev:docs/adr/0105-msix-app-installer-and-minimum-version-gate.md succeeds, and origin/dev and origin/main are both 36dccd8fa1c883c38977b6721d86b745c45c9a94.
2. **The canonical file exists on the merged repository tip.** git show origin/dev:docs/adr/0105-msix-app-installer-and-minimum-version-gate.md reports 155 lines, id ADR-0105, status accepted, and date 2026-08-24. The index already contains exactly one row for ADR-0105.
3. **Existing coverage.** The ADR records signed MSIX/App Installer distribution, the gateway pre-session minimum-version gate, the fail-open package / fail-closed gateway split, D-002 self-managed certificate trust, D-003 UNC/SMB hosting, C-01 private-repository constraints, and a six-row cloud-justification table.
4. **Missing Area 09 requirements.** Area 09 §3 explicitly requires the App Installer 2021 schema with OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="true" plus AutomaticBackgroundTask; package version 1.<minor>.<build>.0 with CI run as build and revision 0; one CollisionEngineers.Pegasus identity with pilot/prod feeds and reinstall for ring changes; and rollback using ForceUpdateFromAnyVersion="true". The accepted ADR currently mentions supported schema and selected attributes but does not state these exact schema/version/channel/rollback decisions.
5. **Governance boundary.** REL-001's resolved operator ownership says it reviews the single FND-005-owned file and extends that file only for a genuinely missing release requirement; it must never create a second ADR-0105. The reconciliation is documentation-only and does not claim package, feed, runtime, Azure, or release evidence.

## Implication

REL-001 must append the missing Area 09 decision clauses to the canonical ADR-0105 in its own branch, keep the existing index row unchanged, run the documentation gates, obtain independent review, and produce merged-main proof. No upstream sync or cloud write is needed.
