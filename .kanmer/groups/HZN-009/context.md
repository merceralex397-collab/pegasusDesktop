# Phase 8 — administration and hardening

## What this phase delivers

The last of the surface, then the hardening that makes it releasable
(proposal §24 Phase 8): users, roles and reference data; integration health and
retries; accessibility remediation; performance remediation; the security
review; packaging and signing; and the operational runbooks.

## Plan folders and ticket-handle ranges

| Plan folder | Handles | Board area |
| --- | --- | --- |
| `docs/desktop/05-implementation-and-migration/` slices S19–S22 | DSK-05-19 … DSK-05-22 | `desktop-features` (FEAT) |
| `docs/desktop/10-security-observability-performance/` | DSK-10-01 … DSK-10-18 | `platform-operations` (PLAT) |

`DSK-05-22` is the hardening sweep across every slice. `DSK-05-21` (password
change and account lifecycle) depends on the area 04 session work.
Packaging and signing rows live in area 09 and are exercised here.

## Entry condition and exit gate

Entry: the Phase 7 exit gate is met — approved fixtures match, no required
report depends on the web renderer, and the report performance target passes on
baseline hardware.

Exit gate (proposal §24 Phase 8; **owner: plan 10**):

- The full automated suite passes.
- Accessibility critical issues are resolved.
- The security review has no unresolved high-risk item.
- A production-like package has been tested.

## Decisions and constraints that bind this phase

- **D-002** (2026-08-23) — production signing uses a self-managed certificate
  trusted per workstation in `LocalMachine\TrustedPeople`. The trust rollout
  and the rehearsed renewal (runbooks R5, R7) are **operator steps**.
- **D-003** (2026-08-23) — the update feed is a UNC file share served to App
  Installer over SMB; the production-like package test runs against it.
- **L-02** — the security review, accessibility scan and performance
  measurement all run on the local Test/UAT stack and the named baseline
  workstation spec; there is no Azure test environment.
- **C-01** — the repositories become private, so CI cost is a live constraint:
  private Windows runners bill at 2× (see
  `docs/desktop/08-testing/README.md` § 7 and `DSK-08-19`).
- **Copy rules** — operator-facing explanation is a defect
  (`AGENTS.md` § Simplicity rails); accessibility remediation must not add it.

## Azure rule

Reads are free; every write is ⚠, exact-target approved
(`docs/runbook.md` § Live operation approval matrix) and mirrored in
`docs/desktop/11-azure-disposition/README.md`. Observability work here uses the
**existing** Application Insights resource (ADR-0109) — no new telemetry fleet
— and nothing is deprovisioned before cutover, observed use and rollback
approval.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map
- `docs/desktop/05-implementation-and-migration/README.md`
- `docs/desktop/05-implementation-and-migration/vertical-slices.md`
- `docs/desktop/10-security-observability-performance/README.md`
- `docs/desktop/09-release-update-and-distribution/README.md`
- `docs/desktop/09-release-update-and-distribution/runbooks.md`
- `docs/desktop/06-ui-design/keyboard-and-accessibility.md`
- `docs/desktop/08-testing/README.md`
- `docs/desktop/11-azure-disposition/README.md`
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 15, § 17, § 18, § 24 Phase 8
- `.kanmer/groups/HZN-001/board-conventions.md`
