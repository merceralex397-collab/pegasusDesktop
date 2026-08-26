# Checklist — REL-001

- [x] Read the complete REL-001 ticket folder, EPIC-010 context, Area 09 release plan, signing/hosting matrix, and repository ADR conventions.
- [x] Run the live ADR-0105 collision/ownership check with Kanmer search and repository refs; record the result.
- [x] Compare the canonical ADR-0105 on origin/dev with Area 09 §3 and identify only the missing schema, version, channel, and rollback clauses.
- [ ] Create the REL-001 branch/worktree and take the ticket after the Preparing gate passes.
- [ ] Append the missing Area 09 clauses to the canonical ADR-0105 without creating a second ADR file or changing the index row.
- [ ] Run the documentation-link, Markdown-placement, exact-single-file, and diff-scope checks; record exact outcomes.
- [ ] Record the docs-only simplification pass as n/a — docs-only in the plan.
- [ ] Write the post-implementation report, obtain independent review, and open/manage the PR if a repository diff is required.
- [ ] After merge, verify the merged main result and write proof; do not claim packaging or runtime evidence.
- [ ] Close out the ticket through Kanmer and release only this ticket's worktree/branch.

## Progress notes

- 2026-08-26 — live comparison found an existing canonical ADR-0105 with the two-layer and D-002/D-003 decisions, but missing Area 09's explicit schema/version/channel/rollback clauses.
