# EPIC-014 — Upstream carry-over

Read this once before working any ticket in the upstream carry-over batch.

## Binding classification

This group holds only upstream work that the desktop conversion genuinely still needs as a ticket of its own. It is not a wholesale import. The source snapshot is the read-only `collisionengineers/pegasus` `kanmer-board` head `a5b28111`, read 2026-08-24.

Each upstream ticket was classified against the 208 seeded conversion tickets, not against its upstream disposition alone:

- **covered by the plan set** — the desktop plan already delivers the outcome; do not import it.
- **partially covered** — the conversion ticket owns the area but needs the requirement added; do not create a duplicate import.
- **import needed** — real work no conversion ticket covers; these are the members of this group.
- **moot after conversion** — only the Razor implementation is obsolete; record what supersedes it rather than importing it.
- **out of scope / post-alpha** — retain it in the carry-over register and do not pull it into this board.

A Razor UI ticket is not automatically moot. Its intent usually survives in a desktop screen specification even when its Razor implementation does not. Only the implementation being Razor makes it moot.

## Ownership and re-check

[[FND-022]] owns re-running this classification against the then-current upstream head before Phase 3. [[FND-004]] owns verifying the counts. Members must preserve the upstream evidence, distinguish upstream ids from fork board ids, and never infer a mapping by arithmetic.

## Operating constraints

No Azure write, mailbox mutation, Box mutation, or speculative desktop architecture is implied by group membership. Follow each member ticket's own refs, profile, plan, and acceptance gates.
