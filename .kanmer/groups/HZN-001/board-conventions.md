## Group count — read this before treating 25 as drift

The conversion seeds **24** groups: thirteen area epics `EPIC-001`…`EPIC-013`
(one per plan folder 00–12) and eleven phase horizons `HZN-001`…`HZN-011`
(proposal §24 phases 0–10). Every one of the 208 conversion tickets carries
exactly one epic and exactly one horizon.

`EPIC-014` — *Upstream carry-over — the subset the desktop conversion still
needs* — is a **fourteenth, intentionally empty** epic created 2026-08-24. It
is not board damage and not a seeding error. It exists to hold the upstream
tickets that a coverage analysis proves the conversion still needs as tickets
of their own.

Its membership is deliberately **not** a wholesale import of the original
board. Each of the 114 open, non-archived tickets on
`collisionengineers/pegasus` `kanmer-board` at `a5b28111` (2026-08-24) is
classified against the seeded 208 before anything is imported:

| Outcome | What happens |
| --- | --- |
| covered by the plan set | not imported; the cross-reference goes on the covering ticket |
| partially covered | not imported; the missing requirement is added to the covering ticket |
| import needed | becomes a ticket in `EPIC-014` |
| moot after conversion | not imported; what supersedes it is recorded |
| out of scope / post-alpha | not imported; the carry-over document stays its register |

A Razor UI ticket is **not** automatically moot — its *intent* usually
survives into a desktop screen spec even though its *implementation* does
not. Only the implementation being Razor makes something moot.

`DSK-01-09` (FND-022) owns re-running the classification against the
then-current upstream head before Phase 3; `DSK-00-04` (FND-004) owns
verifying the counts. If the operator would rather the group count read 24
until an import actually lands, archive `EPIC-014` with
`update_group(id: "EPIC-014", archived: true)` — archiving is reversible and
touches no ticket.
