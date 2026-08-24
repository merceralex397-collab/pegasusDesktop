# Open questions — REL-001 (plan handle `DSK-09-01`): ADR-0105 ownership

## Resolved

- [x] **Which claimant authors `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`?**

  **Answered 2026-08-24 by the operator: [[FND-005]] owns ADR-0105.** [[REL-001]] is not an alternative author. It reviews the single FND-005-owned ADR against the Area-09 distribution and minimum-version-gate requirements, and extends that one file only if a genuinely missing release requirement is identified. It must never create a second file for ADR-0105.

  This supersedes the former “first claimant to be worked” tie-break. Before implementation, record the review/extension outcome in this ticket's plan so the execution shape remains explicit.

## Unresolved

- [ ] **Has the FND-005-owned ADR-0105 file been authored, and what is REL-001's resulting review/extension scope?**

  The ownership decision is resolved, but the current file state must still be checked immediately before this ticket is implemented. Run both checks and record their dated result in the plan:

  - `mcp__kanmer__search_items ADR-0105`
  - `ls docs/adr/0105*`

  If the FND-005 file exists, compare it with Area 09 §3 and record only genuinely missing distribution or minimum-version-gate clauses to extend in place. If FND-005 is actively authoring it, coordinate rather than race. Do not re-open ownership or create another ADR-0105 path.

## Parked (explicitly deferred)

- **Whether ADR-0105 should have been ADR-0030, the next free number.** Not open: settled by
  the operator on 2026-08-23, who confirmed the reserved block ADR-0100–ADR-0110 for the
  conversion precisely so a one-way sync from the still-active upstream ADR sequence cannot
  collide. Recorded at `AGENTS.md:80-88`. Plan step 3 re-reads that sentence before using
  0105; it is a confirmation, not a question.

- **FRD-13.** Not open, and not this ticket's: `docs/frd/README.md` lists FRD-01…FRD-12 and
  [[FND-008]] (plan handle `DSK-00-08`) authors FRD-13. A decision a named sibling ticket
  owns is a scope boundary, not an open question. Plan step 8 writes the forward pointer as
  prose with no relative link, so `scripts/Test-DocumentationLinks.ps1` stays green.

- **D-002 (self-managed certificate) and D-003 (in-house UNC share).** Not open and not to be
  re-opened — both decided by the operator on 2026-08-23. The ADR records them; it does not
  re-evaluate them.
