---
id: EPIC-014
kind: epic
title: Upstream carry-over — the subset the desktop conversion still needs
archived: false
created: '2026-08-24T10:00:58.667Z'
updated: '2026-08-24T10:03:34.211Z'
---
Holds only the upstream tickets the desktop conversion genuinely still needs as tickets of their own. **This is deliberately not a wholesale import.**

Source: read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at `a5b28111` ("chore(kanmer): sync board 2026-08-24T09:58:09.026Z"), read 2026-08-24. That head carries **114** open, non-archived tickets — five more than the 109 in the 2026-08-23 triage (`DOCS-013`, `ENG-014`, `ENG-015`, `INTK-034`, `INTK-035` postdate it); no triaged ticket has since closed.

Each of the 114 was classified against the 208 seeded conversion tickets, not against its upstream disposition alone:

- **covered by the plan set** — a conversion ticket already delivers the outcome in the desktop world. Not imported; the cross-reference is recorded on the covering ticket.
- **partially covered** — a conversion ticket owns the area but would ship without the requirement. Not imported; the requirement is added to that ticket.
- **import needed** — real work no conversion ticket covers. These land here.
- **moot after conversion** — exists only because of the Razor front end and dies with it. Not imported; what supersedes it is recorded.
- **out of scope / post-alpha** — future capability work outside the conversion. Not imported; `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` stays their register.

A Razor UI ticket is not automatically moot: its *intent* usually survives into a desktop screen spec even though its *implementation* does not. Only the implementation being Razor makes something moot.

`DSK-01-09` (FND-022) owns re-running this classification against the then-current upstream head before Phase 3, and `DSK-00-04` (FND-004) owns verifying the counts.
