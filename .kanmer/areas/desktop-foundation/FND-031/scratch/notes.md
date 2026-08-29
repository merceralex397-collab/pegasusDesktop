## 2026-08-28 checkpoint — implementation preserved, acceptance dependency remains

Committed and pushed branch `task/desktop-infrastructure` at `c39ea6f0` (origin/pegasusDesktop). The new infrastructure project, package lock, solution/architecture registrations, desktop project reference, and current-architecture update are present.

Validation completed in the ticket worktree:
- `dotnet restore .\\src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj -r win-x64 --force-evaluate` — passed.
- `dotnet restore .\\Pegasus.slnx --locked-mode` — passed.
- `dotnet build .\\Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false` — passed with 0 warnings/errors.
- `dotnet test .\\tests\\Pegasus.ArchitectureTests\\Pegasus.ArchitectureTests.csproj --configuration Release --no-build --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — 121/121 passed.
- Temporary non-repository behavior probe — DPAPI round-trip/corruption, request headers/correlation, cache expiry, and diagnostics redaction passed. It is not repository proof.

Acceptance is not complete: the required desktop behavior tests cannot be added yet because `tests/Pegasus.Desktop.ViewModelTests` does not exist. FND-038 is the explicitly named area-02 owner for that project but is board-blocked by FND-031, while TEST-004 is the unblocked area-08 ticket for the same scaffold. No duplicate project was created. Next action is to execute the existing unblocked TEST-004 ownership path, then revisit FND-031 after the shared test home is merged. FND-031 remains implementing and is released below so work can proceed on that independent ticket.

2026-08-29: Took FND-031 on task/desktop-infrastructure at c39ea6f0246b4ef664f1b96bfe2a0bf7abc9eac0. Solution Release build passed with 0 warnings/0 errors; Pegasus.ArchitectureTests passed 121/121; infrastructure forbidden-reference scan returned no matches; diff check passed. tests/Pegasus.Desktop.ViewModelTests is absent and is owned by FND-038, so credential/header tests remain open and this ticket is not done. Independent reviewer Erdos was assigned to determine whether this implementation may merge as the prerequisite for FND-038.

2026-08-29: Erdos independent review found GET retry request reuse as a correctness blocker. Fixed in 879055551f30c23da6a69e7fda2f1078ae19990f by rebuilding a fresh GET request for each retry; infra project build passed 0 warnings/0 errors. Pushed to origin task/desktop-infrastructure and updated PR #42. Review accepts the FND-038-owned test-scaffold sequencing in principle, but FND-031 remains not done until its tests and merged-main proof exist.

2026-08-29: PR #42 run 33261304295 failed NU1004 because ViewModelTests/packages.lock.json lacked the new Desktop.Infrastructure project dependency; all shard test failures were downstream. Merged origin/dev into the own branch (adc7b9d2e2c0adfcb6b07a56ccad41f779e25f35), regenerated only that test lock, committed 26aae2fae0c69e99d6dc4bf4bf6fcebfe2748055, and pushed. Fresh exact-head CI and review are pending.

2026-08-29: exact-head CI run 33261673009 completed green for 26aae2fa (all required lanes; infrastructure intentionally skipped). Independent Erdos review confirms retry fix and permits prerequisite-only merge to dev, while FND-031 remains incomplete until FND-038 adds its required DPAPI/header/correlation/retry/boundary tests and merged-main proof.

2026-08-29 — PR #42 exact head 26aae2fa passed CI run 33261673009 and merged to dev as 89fcfa20cb570845dbb1ad9b2f3c45fdd83723e4. Prerequisite merge only; FND-031 stays incomplete pending FND-038-owned Tier-2 tests and merged-main proof.

Independent reviewer Erdos rechecked after merge: PASS for prerequisite merge into dev; FND-031 remains blocked for Done until FND-038 adds the ticket-specific DPAPI/header/correlation/retry/isolation tests and merged-main proof. Reviewer also flagged that post-implementation report/plan references should be refreshed to actual head/merge SHAs before final proof.

2026-08-29: Commit 627d3f613234a75203f1c7115ea590a2a176b199 fixes complete bearer redaction under sensitive keys and preserves pipe-delimited context. Lagrange independently reviewed the exact commit and passed it. Infrastructure build 0 warnings/errors, existing ViewModelTests 6/6, direct redaction/retention smoke and diff check passed. Prerequisite merge only; FND-038 tests and merged-main proof remain open.

2026-08-29: PR #43 exact head 627d3f6 passed run 33265617566 (all required lanes, including SQL shards and aggregate coverage) and merged to dev as 52a1741. Removed only the documented FND-031 -> FND-038 implementation-prerequisite edge; FND-031 remains review/incomplete pending FND-038 tests and proof.
