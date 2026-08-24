# Plan — PLAT-007

## Objective

Give every desktop-written file — bounded cache, thumbnails, temporary document working copies, diagnostic logs — a per-user ACL, a bounded retention policy, an opaque file name that carries no case reference or personal data, and a clearing path on logout and uninstall. Verify the result on a clean Windows 11 machine.

## Chosen approach

Proposal §17.1 `:1168` requires secure temporary-file ACLs and cleanup, §16.3 `:1143` requires per-user access controls and bounded retention for temporary document files, §11.1 `:641-651` lists exactly what may be cached locally, and §22.2 `:1621` makes temporary-file permissions a security test. A shared or roaming workstation is the first threat in §17.3. Operator-visible consequence: a case document extracted for preview stays readable by the next person to log on to that machine, or a file name in `%TEMP%` discloses a claimant's name. Siblings: [[DSK-10-09]] (log redaction and the bundle), [[DSK-10-17]] (encrypted drafts), [[DSK-10-01]] (register).

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. `docs_todo: true` is intentionally retained: planned desktop decisions must not be linked until they exist on `origin/dev`.
- Use the ticket Source of truth and area plan; add a real ref only after its file exists.

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`; win-dev-skills v0.5.0 `f1028dd5`) — use `BuildAndRun.ps1` for the build/run loop → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`) for the security checklist
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `ApplicationData.Current.TemporaryFolder` and `LocalCacheFolder` semantics under package identity, and for `FileSystemAccessRule` / `DirectorySecurity` on a per-user folder
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

1. Orientation. Read the plan row, proposal `:641-651` and `:1138-1146`, and the abstractions from `DSK-02-06`/`DSK-02-11`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-07-desktop-file-hygiene` from `dev`.
3. Write the storage inventory into the ticket's `files` document: one row per location the desktop writes — log folder, bounded cache, thumbnail cache, temporary document working copies, diagnostics bundles, encrypted drafts ([[DSK-10-17]]) — with its folder, its retention bound (size and age), its naming scheme and its clearing trigger. Anything not on proposal §11.1's list must not exist; if it does, raise an open question rather than legitimising it here.
4. Use `microsoft_docs_search` for `ApplicationData LocalCacheFolder TemporaryFolder packaged app` and confirm which folders a packaged app already gets per-user isolation on. Prefer the package-provided per-user folders over a hand-rolled path under `%TEMP%`; record the decision and its evidence in the plan document.
5. Implement a single `IDesktopStorageLocations` (or extend the equivalent abstraction `DSK-02-06` created) that is the only source of these paths. No component may compose its own path — add an architecture test in `tests/Pegasus.ArchitectureTests` or the desktop test project asserting that `Path.GetTempPath`, `Environment.GetFolderPath` and `ApplicationData.Current` are referenced from that one type only.
6. Where a folder is created by the app rather than by the package, set an explicit ACL granting only the current user and denying inheritance, using `DirectorySecurity`/`FileSystemAccessRule`. Assert the resulting ACL in a test that reads it back; do not assume the default.
7. Implement bounded retention: each location has a maximum total size and a maximum age, enforced on startup and after each write, oldest-first. Log the eviction count at debug level with no file names. Unit-test the eviction with a fake clock (the shared `FixedTimeProvider` from `DSK-08-04`).
8. Implement opaque naming: file names derive from an opaque identifier (for example a GUID or a hash of the server identifier), never from a case reference, claimant name, registration or original attachment name. Keep the display name in memory or in an index file that is itself covered by the ACL and retention rules. Add a test that a document with a personal-data-bearing display name produces a stored name matching `^[0-9a-f-]{36}(\.[a-z0-9]{1,8})?$`.
9. Implement clearing: on logout, delete every temporary working copy and every cached item that is not a pure preference; on uninstall, rely on package removal and additionally document what survives. Where secure deletion is not feasible (SSD wear levelling, no `FILE_FLAG_DELETE_ON_CLOSE` path), write the limitation into the plan document as a recorded residual risk rather than claiming a guarantee that does not hold.
10. Extend `eng/packaging/Test-Package.ps1` / `tests/Pegasus.Packaging.Tests` (from `DSK-08-10`) with an install → use → logout → uninstall scenario that enumerates the folders from the inventory and asserts: ACL is user-only after install, files exist after use, temporary copies are gone after logout, and only the intended settings survive uninstall/reinstall.
11. **Operator step** — run the scenario on a clean Windows 11 machine (or a fresh VM snapshot) with a second local user account: log on as user A, use the app, log off, log on as user B and attempt to read user A's cache folder. Hand back the `icacls` output for each folder and a screenshot or transcript of the access denial. This is the only way the per-user boundary can be proved; an in-process test cannot.
12. Run `dotnet test` on the desktop test project and `pwsh ./eng/packaging/Test-Package.ps1 -Scenario FileHygiene`. Both green.
13. Update the threat register rows "lost or shared workstation session" and "sensitive information in logs/temp files" with the test names and the recorded residual risk ([[DSK-10-01]]).
14. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test` on the desktop test project filtered to the storage/retention tests — expected: all pass, including the naming-pattern assertion.
- [ ] `pwsh ./eng/packaging/Test-Package.ps1 -Scenario FileHygiene` — expected: exit 0 with the folder enumeration in the log.
- [ ] `icacls <cache folder>` on the clean machine — expected: only the installing user and SYSTEM, no `BUILTIN\Users`, inheritance disabled.

## Risks and constraints

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Desktop` (only where a view model triggers clearing), the desktop test projects and `eng/packaging/`. Must not touch `src/Pegasus.Core`, `src/Pegasus.Web`, `src/Pegasus.Infrastructure`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a bounded cache quietly growing into a local replica is forbidden by ADR-0104 — keep the bounds enforced, not advisory; claiming "secure delete" where the platform cannot guarantee it is worse than recording the limitation; a second credential store next to the DPAPI refresh store would split the security review — there is exactly one.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Simplification pass

Before the PR, independently review the branch diff for reuse, unnecessary abstraction, duplicated policy and scope expansion; record findings and dispositions here.
