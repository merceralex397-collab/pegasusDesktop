# Checklist — GWY-009: Case tasks, notes, manual chasers and report-evidence link endpoints

One box per plan step, in plan order. The last box produces `proof`.

- [ ] Read the four Tasks rows in `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Cases,
      § 3 of that area README, and the upstream DOCS-012 / upstream CASE-004 bodies quoted in the
      ticket; run `get_doc_gates GWY-009` and `take_ticket`.
- [ ] Confirm on disk that `src/Pegasus.Contracts/`, `src/Pegasus.Web/Api/`,
      `openapi/pegasus-v1.json` and `src/Pegasus.Desktop.Infrastructure/Api/Generated/` all exist;
      if any is missing, stop and report the blocking ticket rather than creating it.
- [ ] Read `src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs` in full and write out, per handler, the
      Core port, the request record and which version it expects (case, task, both, or neither).
- [ ] Run `grep -n "CaseWorkflowEvents" src/Pegasus.Infrastructure/Persistence/Migrations/20260729199000_RuntimeRoleReconciliation.cs`
      and confirm `SELECT, INSERT` at `:122` (Web role) and `:181` (Worker role); record that no
      `Grant*` migration is needed.
- [ ] Add `src/Pegasus.Contracts/Cases/Commands/CaseTaskCommands.cs` — four request DTOs
      (create: `expectedVersion` only; assign/complete/cancel: `expectedVersion` **and**
      `taskExpectedVersion`) plus `CaseTaskResponseDto` carrying both `version` and `caseVersion`.
- [ ] Add `src/Pegasus.Contracts/Cases/Commands/CaseNoteAndChaseCommands.cs` —
      `AddCaseNoteRequestDto` with `operationKey` and `note` only (no version, no lease token, per
      CASE-017), `RecordManualChaseRequestDto`, `ReportEvidenceLinkRequestDto` and the two
      response DTOs.
- [ ] Add `src/Pegasus.Web/Api/CaseTaskEndpoints.cs` mapping the eight named routes onto the
      `cases` sub-group, and register it from that group — no dispatcher endpoint.
- [ ] In each handler, resolve the actor from `HttpContext.Items` and construct the Core request
      record identically to the matching page handler; add no authorization, no length cap, no
      TempData and no PRG.
- [ ] Add the `CaseTaskVersionConflictException` arm to the `/api/v1` problem-details mapper
      (409, `urn:pegasus:problem:version-conflict`, carrying `taskId`, `expectedVersion`,
      `currentVersion`) — in that file, not in the endpoint file.
- [ ] Validate `operationKey` at the boundary (`desk:` prefix, ≤ 100 characters, no whitespace or
      control characters) and attach the existing `/api/v1` write rate-limit policy.
- [ ] Make each of the eight responses return the identifiers and versions the endpoint-map rows
      name, so the desktop task strip needs no re-read after a command.
- [ ] Write the new automatic case note on document logical removal in
      `EfDocumentCustodyStore.cs:419-461` — one `CaseWorkflowEvents.Add` in the
      `EfCaseNoteStore.cs:48-63` shape, reason from `command.Reason`, replay key from
      `command.OperationKey`.
- [ ] Move `custody_confirmed` (`EfQueuedCustodyProcessor.cs:594-605`) and
      `audit_custody_confirmed` (`:661`) off `CaseHistory` onto `CaseWorkflowEvents` — moved, not
      duplicated, event-type strings unchanged.
- [ ] Move `custody_failed` (`EfExternalWorkStore.cs:450-456`, `:608-614`) and
      `audit_custody_failed` (`:481-487`, `:635-641`) the same way, leaving `:210` untouched.
- [ ] Add `tests/Pegasus.IntegrationTests/DesktopGatewayCaseTaskTests.cs` with the seven-case
      matrix for all eight commands, each conflict fact asserting the problem `type` URI.
- [ ] Add the wrong-version-scope fact: a task command sent with the case version in
      `taskExpectedVersion` (and the reverse) fails rather than silently succeeding.
- [ ] Add the seven upstream DOCS-012 facts asserted through `GET /cases/{id}/history` — one entry
      per removal, custody confirm, custody fail, and the audit variants — plus the negative that
      `CaseHistory` gains no row for any of them.
- [ ] Regenerate and commit `openapi/pegasus-v1.json` and the Kiota client under
      `src/Pegasus.Desktop.Infrastructure/Api/Generated/` via `eng/api/Generate-ApiClient.ps1`.
- [ ] Run the simplification pass over this branch's diff and record it under a dated
      `## Simplification pass` heading in the plan document.
- [ ] Run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayCaseTaskTests"`,
      then `--filter "FullyQualifiedName~CaseTasksWebTests"`, then
      `grep -rn "CaseHistoryEntity" src/Pegasus.Infrastructure/Persistence/EfQueuedCustodyProcessor.cs src/Pegasus.Infrastructure/Persistence/EfExternalWorkStore.cs`;
      capture all three outputs as the tier-5 `proof`.

## Progress notes
