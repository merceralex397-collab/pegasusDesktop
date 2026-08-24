# Checklist — PLAT-006

## Implementation

- [ ] 1. Orientation. Read the plan row, `src/Pegasus.Core/Intake/IntakeContracts.cs:7-57` in full including the remarks (they explain why the mailbox bound is 750 MiB and the staff bound is 10 MiB — confusing the two is the recorded defect of 2026-08-05), and `docs/desktop/03-gateway-api-and-data/README.md` § 5 rows `DSK-03-10`/`DSK-03-11`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.

- [ ] 2. Branch `task/dsk-10-06-malformed-upload-tests` from `dev`.

- [ ] 3. Add `tests/Pegasus.IntegrationTests/ApiUploadLimitTests.cs` (or extend the file `DSK-03-11` created). Reuse the fixture helpers in `tests/Pegasus.IntegrationTests/MultiFormatIntakeWebTests.cs` rather than writing new fixture plumbing.

- [ ] 4. Size cases: one file at exactly `IntakeEnvelopeLimits.MaximumContentLength` succeeds; one byte over is refused; a batch of 20 files succeeds; 21 files is refused; a body over `MaximumBatchContentLength` is refused by the request pipeline. Assert the refusal is produced **before** any Core use case runs — assert it by observing that no intake record, no staged artifact and no action-history entry was written, not merely by the status code.

- [ ] 5. Filename cases: `..\..\evil.pdf`, `../../evil.pdf`, an absolute path `C:\Windows\System32\evil.pdf`, a device name (`CON`, `NUL`, `COM1`), a name with a trailing dot or space, a name containing a NUL byte, and a 300-character name. Each must be refused or normalised to a safe stored name; assert the persisted name and that nothing was written outside the intended folder.

- [ ] 6. Content cases: a file whose declared extension and actual magic bytes disagree; a zero-byte file; a truncated PDF; a deeply nested archive if the endpoint accepts archives. Assert the documented problem type and a stable contract code, not an unhandled exception.

- [ ] 7. Byte-endpoint cases (`DSK-03-10`): assert `Content-Length` is sent, `ETag` is present, a range request behaves, `X-Content-Type-Options: nosniff` is set on the response, and that the download filename is the safe stored name.

- [ ] 8. Cancellation case: abort a request mid-upload and assert no receipt, no partial case document and no orphaned staged artifact remain (the plan for `DSK-03-11` states "interrupted upload leaves no receipt").

- [ ] 9. Run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~ApiUploadLimit"`. All green.

- [ ] 10. Run `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` then `pwsh ./scripts/Test-TestShard.ps1` so the new tests are assigned to exactly one shard — the repository's shard guard fails CI otherwise.

- [ ] 11. Update the threat register row "malicious or malformed attachment" with the test names ([[DSK-10-01]]).

- [ ] 12. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~ApiUploadLimit"` — expected: all facts pass.
- [ ] `pwsh ./scripts/Test-TestShard.ps1` — expected: exit 0 (new tests assigned to exactly one shard).
- [ ] `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` — expected: exit 0.

## Progress notes

Record factual progress only; unresolved decisions remain in `open-questions`.
