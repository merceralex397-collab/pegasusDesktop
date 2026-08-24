# Checklist — GWY-011: Upload sessions, case documents, custody, export and EVA handoff

One box per plan step, in plan order. The last box produces `proof`.

- [ ] Read the endpoint-map rows quoted in the ticket, the area README § 3 row *Bytes & uploads*,
      and the body of [[CASE-002]] (upstream `CASE-022`); load the `minimal-api-file-upload` skill;
      run `get_doc_gates GWY-011` and `take_ticket`.
- [ ] Confirm [[GWY-010]] has merged and that `src/Pegasus.Contracts/`, `src/Pegasus.Web/Api/`,
      `openapi/` and `src/Pegasus.Desktop.Infrastructure/Api/Generated/` exist; stop and name the
      blocker if any is missing.
- [ ] Read the seven projected page models in full and produce the per-handler table, including
      the two headers the endpoint map omits — `X-Content-SHA256`
      (`Cases/Documents/Download.cshtml.cs:54`) and `Content-Digest`
      (`Cases/Eva/Download.cshtml.cs:60-61`).
- [ ] Determine whether the upload session can reuse the existing intake staging path or needs its
      own table; if it needs one, add the migration, the `Grant*` migration, the
      `scripts/Invoke-AzureDatabaseBootstrap.ps1` mirror and the census entry in
      `IntakePersistenceIntegrationTests.cs`.
- [ ] Write the *Limits record* in the plan: the `IntakeEnvelopeLimits` values in force, whether
      [[CASE-002]] had landed, and the storage answer above.
- [ ] Add `src/Pegasus.Contracts/Uploads/` DTOs for the session, the completion command and the
      status response, reusing the shared mutation fields from [[GWY-001]].
- [ ] Add `src/Pegasus.Web/Api/UploadEndpoints.cs` with the two shared session routes and the five
      staff-upload routes; **stream** the `PUT` body rather than buffering it.
- [ ] Add `DateTimeOffset? DueAtUtc` to `QueuedIntakeStatus` (`DurableIntake.cs:87-94`) and carry
      `IntakeWorkItem.DueAtUtc` (`:41`) through the `IQueuedIntakeStatusQueries` projection in UTC.
- [ ] Append `RetryScheduled = 4` to `QueuedIntakeStatusKind` (`:79-85`) without moving `0`–`3`,
      stop `FromWorkState` folding it into `Received`, serialise it as `retry_scheduled`, and
      correct the doc comment at `:98-101`.
- [ ] Replace the `CaseIntakeLinks`-only `CaseId` subquery in
      `EfQueuedIntakeStatusQueries.cs:25-28` with resolution through
      `IntakeReceipt.CurrentCaseId` — one rule, not a re-expression of it.
- [ ] Surface the five-value state, `dueAtUtc` and the corrected `caseId` on the status response
      DTO, with no operator-facing sentence in the payload.
- [ ] Add `src/Pegasus.Web/Api/CaseDocumentEndpoints.cs` with the ten case-scoped routes on the
      `cases` sub-group.
- [ ] Add a `DocumentRequestUnavailableException` arm to the `/api/v1` problem mapper returning
      `urn:pegasus:problem:provider-unavailable`, so the two request-upload-link routes refuse
      honestly instead of throwing.
- [ ] Write the *Inactive-capability record* in the plan and mirror the inert-until-`CASE-022`
      statement into [[FEAT-014]]'s traps.
- [ ] Make the session leave nothing behind when abandoned: only `…/complete` calls Core, and the
      session carries an expiry so an abandoned one is collected.
- [ ] Apply the byte conventions to `…/documents/{docId}/content` and
      `…/eva-handoff/{revision}/bundle` — `Content-Length`, weak `ETag`, range, `nosniff`,
      sanitised filename, compression exemption — and carry across both digest headers.
- [ ] Confirm `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs` is untouched and has no API
      equivalent.
- [ ] Add `tests/Pegasus.IntegrationTests/DesktopGatewayUploadTests.cs` with the session lifecycle
      facts (full session, oversized `PUT` asserted against the constant, abandoned session leaves
      nothing, replayed `…/complete`).
- [ ] Add the seven-case matrix for each of the eleven command endpoints, and the export-archive
      comparison against the upstream `CASE-019` proof fixture.
- [ ] Add the three upstream-INTK-001 facts: `retry_scheduled` + non-null `dueAtUtc` (never
      `Received`); an associated-but-unlinked receipt returns its case in `caseId`; a linked
      receipt returns what it did before.
- [ ] Add the inert-link fact: `POST /cases/{id}/request-upload-links` under the production
      composition returns the named `provider-unavailable` problem, not a 500 and not a fabricated
      link.
- [ ] Correct the `GET /uploads/{receiptId}/status` row in
      `docs/desktop/03-gateway-api-and-data/endpoint-map.md` to the five-state list plus `dueAtUtc`
      and the association-or-link `caseId`.
- [ ] Regenerate and commit `openapi/pegasus-v1.json` and the Kiota client via
      `eng/api/Generate-ApiClient.ps1`.
- [ ] Measure the effective request-body ceiling by sending progressively larger bodies and record
      it in the *Limits record*.
- [ ] Run the simplification pass over this branch's diff and record it under a dated
      `## Simplification pass` heading in the plan.
- [ ] Run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release`
      with `--filter "FullyQualifiedName~DesktopGatewayUploadTests"`, then `~CaseCustodyWebTests`,
      then `~UploadConfirmationWebTests`, then `~ProductionCompositionTests`; capture all four
      outputs as the tier-5 `proof`.

## Progress notes
