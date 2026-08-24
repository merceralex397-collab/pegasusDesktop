# Phase 7 — assessment and reports

## Delivers

Valuation and reporting on the desktop (proposal §24 Phase 7): the assessment and valuation screens with their calculations, local report rendering, preview and finalisation, the canonical upload back through the gateway, and golden report tests.

## Plan folders and ticket-handle ranges

- `docs/desktop/05-implementation-and-migration/` slices S17–S18 — DSK-05-17…DSK-05-18 → `desktop-features` (FEAT)
- `docs/desktop/07-integrations/` WebView2 renderer — DSK-07-01…DSK-07-19 → `desktop-features` (FEAT)

`DSK-00-07` authors ADR-0108 as `proposed`; it is accepted after this phase's packaged-controller validation and parity evidence. `DSK-05-25` parity evidence continues.

## Entry condition and exit gate

Entry: the Phase 6 exit gate is met — transfers recover safely, provider secrets are absent from the package, and document parity is approved.

Exit gate (proposal §24 Phase 7; **owner: plan 05**):

- Approved fixtures match expected values and content.
- No required report depends on the web renderer unless it is explicitly retained.
- The final document and its audit are correct.
- The performance target passes on baseline hardware.

## Decisions and constraints that bind this phase

- **L-03** (locked) — report rendering moves to the desktop through an **isolated, non-UI WebView2 HTML→PDF path**, and the gateway renderer is **retained until golden-file parity passes**. This is what ADR-0108 records; do not remove the server renderer early.
- **L-01** — finalisation, the canonical copy and the audit entry are gateway operations; the desktop renders and previews.
- **L-02** — golden-file and performance evidence is produced on the local Test/UAT stack and the named baseline workstation spec recorded in Phase 0, not on an Azure environment.
- **Characterization** — close the assessment save/import/reconcile rules (S17) and the report projection fixtures (S18) with Core tests before the slice moves them.

## Azure rule

Reads are free; every write is ⚠, exact-target approved (`docs/runbook.md` § Live operation approval matrix) and mirrored in `docs/desktop/11-azure-disposition/README.md`; nothing is deprovisioned before cutover, observed use and rollback approval. The gateway renderer's Container App resources stay in place through this phase by design — retiring them is a Phase 10 question, and only after golden-file parity has passed.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map
- `docs/desktop/05-implementation-and-migration/README.md` and `vertical-slices.md`
- `docs/desktop/07-integrations/README.md`
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
- `docs/desktop/06-ui-design/screen-specs.md`
- `docs/desktop/01-inventory-and-parity/flow-records.md`
- `docs/desktop/08-testing/README.md`
- `docs/desktop/10-security-observability-performance/README.md`
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5, § 13.9, § 23.2, § 24 Phase 7
- `.kanmer/groups/HZN-001/board-conventions.md`
