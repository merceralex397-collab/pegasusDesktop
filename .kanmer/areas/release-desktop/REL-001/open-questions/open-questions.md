# Open questions — REL-001 (plan handle DSK-09-01): ADR-0105 ownership

## Resolved

- [x] **Which claimant authors docs/adr/0105-msix-app-installer-and-minimum-version-gate.md?**

  **Answered 2026-08-24 by the operator: [[FND-005]] owns ADR-0105.** [[REL-001]] reviews the single FND-005-owned ADR against Area 09 and extends that one file only if a genuinely missing release requirement is identified. It must never create a second file.

- [x] **Has the FND-005-owned ADR-0105 file been authored, and what is REL-001's resulting review/extension scope?**

  **Answered 2026-08-26 by live repository/Kanmer checks:** FND-005 and FND-042 both reference the canonical file, and FND-005 is done; no active ADR-0105 authoring ticket exists. The file exists on both origin/dev and origin/main at 36dccd8fa1c883c38977b6721d86b745c45c9a94; the index has one ADR-0105 row. The existing ADR covers the two-layer split, D-002, D-003, C-01, and the cloud table, but omits Area 09 §3's explicit 2021-schema/update attributes, package version 1.<minor>.<build>.0 with build/revision rule, pilot/prod channel identity and ring-change rule, and rollback's ForceUpdateFromAnyVersion. REL-001's bounded scope is to append those already-settled clauses to the canonical ADR only, without a second ADR or index change. No product decision remains open.

## Parked (explicitly deferred)

- **Whether ADR-0105 should have been ADR-0030, the next free number.** Not open: settled by the operator on 2026-08-23, who confirmed the reserved block ADR-0100–ADR-0110.
- **FRD-13.** Not open and not this ticket's: [[FND-008]] owns it; REL-001 may only refer to it as a future pointer.
- **D-002 and D-003.** Not open and not to be re-evaluated; both were decided by the operator on 2026-08-23.
