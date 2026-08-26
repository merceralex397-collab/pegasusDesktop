# Files map — REL-001

## Owned changes

| File | Change | Risk / validation |
| --- | --- | --- |
| docs/adr/0105-msix-app-installer-and-minimum-version-gate.md | Append the missing Area 09 schema, package-version, channel, and rollback clarifications to the existing FND-005-owned ADR. | Accepted decision record; preserve prior decision text and validate headings, links, and explicit decision wording. |
| Kanmer REL-001 documents | Record collision evidence, exact scope, checklist, review, and merged-main proof through Kanmer MCP. | Board gate and traceability risk; use optimistic document versions and get_doc_gates before each move. |

## Existing evidence consumed

| File / source | Why it matters |
| --- | --- |
| docs/desktop/09-release-update-and-distribution/README.md §3 | Canonical Area 09 release decisions and exact missing clauses. |
| docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md | Settled D-002/D-003 choices and their trade-offs; no re-evaluation. |
| docs/adr/README.md | Confirms the single existing ADR-0105 index row. |
| AGENTS.md ADR conventions and workflow | Reserved numbering, accepted-ADR handling, docs-only review, and no upstream/cloud boundary. |
| Kanmer FND-005 and FND-042 live items | Confirms ownership and prior merged delivery. |

## Ripple effects

No source, tests, packaging scripts, CI workflow, operations snapshot, cloud resource, or upstream repository is in scope. Downstream packaging and release tickets consume the clarified ADR; they remain responsible for proving generated artifacts and runtime behavior.
