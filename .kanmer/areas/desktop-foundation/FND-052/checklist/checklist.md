# Checklist — FND-052

Derived from the plan; this ticket edits only the named Kanmer ticket bodies through MCP.

- [x] Confirm the authoritative HZN-001 board-conventions join table and validator parameter blocks.
- [x] Normalize all 16 actual Markdown-placement command call sites to the single `-Base origin/dev -Head HEAD` form; FEAT-038's separate regression self-test was left unchanged.
- [x] Add the real Markdown-placement validator beside REL-013's regression self-test.
- [x] Add required `-ArtifactRoot ./artifacts/test-shards -ShardCount 3` to all five VerifyPartition call sites.
- [x] Replace PLAT-002's verification ellipsis with its concrete production-smoke invocation.
- [x] Qualify the seven high-value ambiguous upstream ids and the twelve DOCS-001 occurrences outside verbatim blocks; already-correct occurrences were not rewritten.
- [x] Normalize REL-007's six unresolved wiki-link sites: demote the two withdrawn handles and map four live DSK plan handles to their REL board tickets without changing its rationale.
- [x] Re-run the scoped board sweeps: zero unresolved fork-board-shaped targets outside verbatim blocks; DSK-* plan handles are reported separately under HZN-001 and the simplification pass is n/a — board-only.
- [x] Verification evidence is captured in proof.md after live board and repository validation.

## Progress notes

## Closeout — FND-052

- [x] PR merge verified — not applicable; this is board-only Kanmer work with no repository branch integration.
- [x] proof.md finalised with live-board commands and results.
- [x] Moved to final stage: Kanmer Done at 2026-08-25T05:03:08.723Z.
- [x] Outcome recorded in ticket body; no follow-up, commit, PR, deployment, or Azure write.
- [x] Main checkout used for cleanup; .worktrees/fnd-052 removed.
- [x] Local fnd-052-board-hygiene branch deleted; no remote branch existed.
- [x] git fetch --prune origin and git worktree prune completed.
- [x] Ticket claim released after cleanup.
