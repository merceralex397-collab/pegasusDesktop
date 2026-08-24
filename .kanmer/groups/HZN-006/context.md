# Phase 5 — intake and communications

## What this phase delivers

The Graph intake pipeline surfaced as a native triage and mail experience
(proposal §24 Phase 5): intake status and failures, the native triage flow,
attachments, deduplication, provider matching and case resolution,
communication history, and outbound commands where they are supported. The
intake worker itself does not move — it stays central and unattended.

## Plan folders and ticket-handle ranges

| Plan folder | Handles | Board area |
| --- | --- | --- |
| `docs/desktop/05-implementation-and-migration/` slices S9–S13 | DSK-05-09 … DSK-05-13 | `desktop-features` (FEAT) |
| `docs/desktop/07-integrations/` (Graph, mail) | DSK-07-01 … DSK-07-19 | `desktop-features` (FEAT) |

Area 07 spans Phases 5–7; the rows this phase needs are the Graph-intake and
mail seams. `DSK-05-25` parity evidence continues.

## Entry condition and exit gate

Entry: the Phase 4 exit gate is met — the two-user conflict test passes, no
silent overwrite occurs, and UAT has approved the primary case workflow.

Exit gate (proposal §24 Phase 5; **owner: plan 05**):

- Intake arrives while the desktop is closed.
- Duplicate and failure paths pass.
- No desktop holds Graph service credentials.
- Full source-to-case traceability exists.

## Decisions and constraints that bind this phase

- **ADR-0106 / the cloud-justification test** — Graph intake stays central
  because it needs unattended execution and protected credentials. A desktop
  that holds a Graph service credential is a defect, not an optimisation.
- **L-01** — mail and intake commands are gateway endpoints; the desktop
  triages, it does not talk to Graph.
- **L-02** — verification uses the local Test/UAT stack with replay adapters
  and the intake corpus, not a live mailbox.
- **Characterization before moving a rule** — close the intake draft
  correction and link/unlink integrity gaps (S9, S10) and the triage action
  matrix (S11) with Core tests before the slice moves them.
- **Upstream sync trap** — Graph credential drift after an upstream sync;
  sync before this phase starts and rerun
  `MailboxIntakeIntegrationTests.cs` (`docs/desktop/07-integrations/README.md`
  § 7).

## Azure rule

Reads are free; every write is ⚠, exact-target approved
(`docs/runbook.md` § Live operation approval matrix) and mirrored in
`docs/desktop/11-azure-disposition/README.md`. Nothing is deprovisioned before
cutover, observed use and rollback approval — the mailbox, Worker and storage
this phase reads about all stay exactly where they are.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map
- `docs/desktop/05-implementation-and-migration/README.md`
- `docs/desktop/05-implementation-and-migration/vertical-slices.md`
- `docs/desktop/07-integrations/README.md`
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
- `docs/desktop/06-ui-design/screen-specs.md`
- `docs/desktop/01-inventory-and-parity/flow-records.md`
- `docs/desktop/08-testing/test-uat-stack.md`
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.1, § 13.4, § 13.8, § 24 Phase 5
- `.kanmer/groups/HZN-001/board-conventions.md`
