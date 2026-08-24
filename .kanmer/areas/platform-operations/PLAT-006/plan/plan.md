# Plan — PLAT-006

## Objective

Prove that the `/api/v1` upload-session and byte endpoints enforce `IntakeEnvelopeLimits` and the reader resource limits **before** anything reaches Core, that path traversal and unsafe filenames cannot escape their folder, and that content sniffing is refused.

## Chosen approach

Proposal §17.3 names "malicious or malformed attachment" as a meaningful threat and §22.2 `:1615-1616` lists malformed uploads and unsafe file paths as security tests. The web Upload path already enforces its bounds (`src/Pegasus.Core/Intake/IntakeContracts.cs:7-57` and the `FormOptions` limit at `src/Pegasus.Web/Program.cs:527-533`), but the desktop moves uploading onto new gateway endpoints (`DSK-03-10`, `DSK-03-11`) where those bounds have to be re-established rather than inherited. `docs/current-architecture.md:222-236` records that reader and resource limits are enforced before Core; that ordering is the property under test. Operator-visible consequence: an oversized or hostile attachment either crashes the gateway or lands as a case document with a filename that escapes its folder. Siblings: [[DSK-10-05]], [[DSK-10-07]], [[DSK-10-01]].

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. `docs_todo: true` is intentionally retained: planned desktop decisions must not be linked until they exist on `origin/dev`.
- Use the ticket Source of truth and area plan; add a real ref only after its file exists.

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `run-tests` (same pin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for ASP.NET Core multipart limits and `Content-Disposition` filename handling
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

1. Orientation. Read the plan row, `src/Pegasus.Core/Intake/IntakeContracts.cs:7-57` in full including the remarks (they explain why the mailbox bound is 750 MiB and the staff bound is 10 MiB — confusing the two is the recorded defect of 2026-08-05), and `docs/desktop/03-gateway-api-and-data/README.md` § 5 rows `DSK-03-10`/`DSK-03-11`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-06-malformed-upload-tests` from `dev`.
3. Add `tests/Pegasus.IntegrationTests/ApiUploadLimitTests.cs` (or extend the file `DSK-03-11` created). Reuse the fixture helpers in `tests/Pegasus.IntegrationTests/MultiFormatIntakeWebTests.cs` rather than writing new fixture plumbing.
4. Size cases: one file at exactly `IntakeEnvelopeLimits.MaximumContentLength` succeeds; one byte over is refused; a batch of 20 files succeeds; 21 files is refused; a body over `MaximumBatchContentLength` is refused by the request pipeline. Assert the refusal is produced **before** any Core use case runs — assert it by observing that no intake record, no staged artifact and no action-history entry was written, not merely by the status code.
5. Filename cases: `..\..\evil.pdf`, `../../evil.pdf`, an absolute path `C:\Windows\System32\evil.pdf`, a device name (`CON`, `NUL`, `COM1`), a name with a trailing dot or space, a name containing a NUL byte, and a 300-character name. Each must be refused or normalised to a safe stored name; assert the persisted name and that nothing was written outside the intended folder.
6. Content cases: a file whose declared extension and actual magic bytes disagree; a zero-byte file; a truncated PDF; a deeply nested archive if the endpoint accepts archives. Assert the documented problem type and a stable contract code, not an unhandled exception.
7. Byte-endpoint cases (`DSK-03-10`): assert `Content-Length` is sent, `ETag` is present, a range request behaves, `X-Content-Type-Options: nosniff` is set on the response, and that the download filename is the safe stored name.
8. Cancellation case: abort a request mid-upload and assert no receipt, no partial case document and no orphaned staged artifact remain (the plan for `DSK-03-11` states "interrupted upload leaves no receipt").
9. Run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~ApiUploadLimit"`. All green.
10. Run `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` then `pwsh ./scripts/Test-TestShard.ps1` so the new tests are assigned to exactly one shard — the repository's shard guard fails CI otherwise.
11. Update the threat register row "malicious or malformed attachment" with the test names ([[DSK-10-01]]).
12. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~ApiUploadLimit"` — expected: all facts pass.
- [ ] `pwsh ./scripts/Test-TestShard.ps1` — expected: exit 0 (new tests assigned to exactly one shard).
- [ ] `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` — expected: exit 0.

## Risks and constraints

- **Azure**: no write. Blob interaction runs against Azurite in the local stack (L-02).
- **Scope boundary**: may add tests and fixtures under `tests/Pegasus.IntegrationTests`. Must not relax a limit to make a test pass, and must not edit `IntakeEnvelopeLimits` — a limit that is wrong is a separate decision. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: applying `MaximumMailboxContentLength` (750 MiB) to a staff upload is the exact confusion the constant's remarks warn about — a 16.69 MB instruction was once refused outright by the inverse mistake; corpus material must never be used as a fixture (`docs/runbook.md` § Corpus safety and evaluation); asserting only the HTTP status hides a refusal that happened *after* Core already wrote something.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Simplification pass

Before the PR, independently review the branch diff for reuse, unnecessary abstraction, duplicated policy and scope expansion; record findings and dispositions here.
