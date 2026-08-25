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
- [ ] Verification evidence is captured in proof.md after merged-result review.

## Progress notes
