---
id: FEAT-038
type: ticket
title: >-
  DSK-07-12 · Accept ADR-0108 after packaged-renderer validation and golden-file
  parity
status: preparing
area: desktop-features
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:31:44.222Z'
labels:
  - desktop-conversion
  - plan-07
  - phase-7
  - tier-1
groups:
  - EPIC-008
  - HZN-008
links: []
blocks:
  - FEAT-018
  - FEAT-043
docs_todo: true
archived: false
created: '2026-08-24T08:24:13.959Z'
updated: '2026-08-24T23:49:47.862Z'
---

## What

After [[FND-007]] has merged ADR-0108 as `proposed`, accept that existing ADR only after [[FEAT-040]] supplies packaged-controller evidence and [[FEAT-041]] supplies passing approved-fixture parity evidence.

This ticket changes exactly two documentation locations: ADR-0108 frontmatter (`status: proposed` to `accepted` plus the acceptance date) and its row in `docs/adr/README.md`. It never creates a second ADR-0108, edits the ADR body, implements the renderer, chooses a host, or runs the parity suite.

## Why

The Phase 0 proposed ADR records the narrow exception before renderer code exists. Acceptance is a separate Phase 7 decision: it is justified only by packaged-app validation and parity evidence, while the gateway renderer remains available until that evidence exists.

## Source of truth

- `docs/desktop/07-integrations/README.md` — DSK-07-12 through DSK-07-15.
- `docs/desktop/00-governance-and-workflow/README.md` — ADR-0108 and reserved-block conventions.
- ADR-0108 itself, `docs/adr/README.md`, L-03, and proposal §23.2.
- Depends on: [[FND-005]] and [[FND-007]] for the Phase 0 decision record, [[FEAT-040]] for packaged-controller evidence, and [[FEAT-041]] for parity evidence.

## Routing

- **Subagent**: `pegasus-desktop-reviewer`.
- **Skills**: `pegasus-desktop` then `kanmer-docs`; Microsoft Learn is not re-researched here because the accepted ADR body is immutable.
- **Kanmer pipeline**: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout`.

## Implementation steps

1. Read the merged proposed ADR and the evidence supplied by [[FEAT-040]] and [[FEAT-041]]. Confirm the reversal condition has not fired.
2. Confirm the ADR body stays untouched and `docs/adr/README.md` has no ADR-0108 row before this change.
3. Change only ADR-0108 frontmatter to `status: accepted` and set its acceptance date.
4. Add exactly one `ADR | Title | Related FRD` row to `docs/adr/README.md`.
5. Run documentation checks, obtain independent review, and record proof referencing the two evidence tickets.

## Acceptance criteria

- [ ] A merged proposed ADR-0108 exists before this ticket starts.
- [ ] FEAT-040's packaged-controller evidence and FEAT-041's parity result are recorded and pass the ADR reversal condition.
- [ ] ADR-0108 body is byte-for-byte unchanged; only its status/date frontmatter changes.
- [ ] ADR-0108 reads `accepted` and `docs/adr/README.md` has exactly one accepted-table row.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — exits 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — exits 0.
- [ ] `git diff --name-only` — only ADR-0108 and `docs/adr/README.md`.
- [ ] `git diff -- docs/adr/0108-desktop-webview2-report-rendering.md` — frontmatter status/date only.
- [ ] `grep -n '0108' docs/adr/README.md` — exactly one accepted-table row.

## Evidence tier

Tier 1 for the documentation change, relying on the cited Tier 3 packaged-controller and golden-file evidence. This ticket does not reproduce that evidence.

## Documentation changes

- `docs/adr/0108-desktop-webview2-report-rendering.md` — frontmatter status/date only.
- `docs/adr/README.md` — one accepted-table row.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: no ADR body edit, renderer code, host selection, test or fixture change, or gateway-renderer removal.
- **Ownership**: [[FND-007]] owns the proposed ADR; [[FEAT-040]] and [[FEAT-041]] provide evidence; this ticket owns only acceptance and the index row.

## Outcome

_Filled at closeout._
