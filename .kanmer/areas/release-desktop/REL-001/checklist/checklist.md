# Checklist — REL-001

- [x] Read the complete REL-001 ticket folder, EPIC-010 context, Area 09 release plan, signing/hosting matrix, and repository ADR conventions.
- [x] Run the live ADR-0105 collision/ownership check with Kanmer search and repository refs; record the result.
- [x] Compare the canonical ADR-0105 on origin/dev with Area 09 §3 and identify only the missing schema, version, channel, and rollback clauses.
- [x] Create the REL-001 branch/worktree and take the ticket after the Preparing gate passed.
- [x] Append the missing Area 09 clauses to the canonical ADR-0105 without creating a second ADR file or changing the index row.
- [x] Run the documentation-link, Markdown-placement, exact-single-file, and diff-scope checks; record exact outcomes.
- [x] Record the docs-only simplification pass as n/a — docs-only in the plan.
- [x] Write the post-implementation report, obtain independent review, and open/manage the PR if a repository diff is required.
- [ ] Address review findings and obtain a passing independent re-review at the final exact PR head.
- [ ] After merge, verify the merged main result and write proof; do not claim packaging or runtime evidence.
- [ ] Close out the ticket through Kanmer and release only this ticket's worktree/branch.

## Progress notes

- 2026-08-26 — live comparison found an existing canonical ADR-0105 with the two-layer and D-002/D-003 decisions, but missing Area 09's explicit schema/version/channel/rollback clauses.
- 2026-08-26 — branch task/rel-001-adr-0105-reconciliation taken in C:\Users\PC\Documents\GitHub\pegasus-worktrees\rel-001-adr-0105-reconciliation.
- 2026-08-26 — appended the Area 09 release contract; local documentation checks passed.
- 2026-08-26 — PR #22 opened. Independent review found two findings; commit 17c87e51 fixes both and PR CI is being rechecked at the new exact head.

## Review findings — 2026-08-26

- [x] Fix the missing Relates section and distinguish the App Installer file from Package.appxmanifest (17c87e51).
- [ ] Resolve the accepted-ADR immutability conflict before merge; do not merge the in-place edit under the current AGENTS.md rule.
- [ ] Scope the cloud-justification evidence separately to the feed and gateway.
- [ ] Correct ForceUpdateFromAnyVersion to the canonical XML element/value form.
- [ ] Obtain a passing independent re-review at the final exact PR head.

## Independent review — 2026-08-26

- [x] Address missing Relates section and ambiguous App Installer terminology in 17c87e51.
- [ ] Obtain governance amendment or select a valid superseding-ADR route; do not merge PR #22 before this is resolved.
- [ ] Scope cloud justification separately to feed and gateway.
- [ ] Use the canonical ForceUpdateFromAnyVersion XML element/value wording.
- [ ] Re-review the final exact PR head and only then merge.
