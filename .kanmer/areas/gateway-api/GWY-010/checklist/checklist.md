# Checklist — GWY-010: Intake (received items) endpoints

One box per plan step, in plan order. The last box produces `proof`.

- [ ] Read the five endpoint rows in `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
      § Intake, the area README § 3 rows *Bytes & uploads* and *Compression*, and the bodies of
      [[INTK-001]], [[INTK-004]] and [[INTK-006]]; run `get_doc_gates GWY-010` and `take_ticket`.
- [ ] Confirm [[FND-023]] (the first one-way upstream sync) has landed, and that
      `src/Pegasus.Contracts/`, `src/Pegasus.Web/Api/`, `openapi/` and
      `src/Pegasus.Desktop.Infrastructure/Api/Generated/` all exist; stop and name the blocker if
      any is missing.
- [ ] Read `Intake/Details.cshtml.cs`, `Source.cshtml.cs`, `Asset.cshtml.cs` and `Image.cshtml.cs`
      in full and produce the per-handler table: Core port, expected version, whether `reason` is
      required, and the exact byte-route response headers set today.
- [ ] Add `src/Pegasus.Contracts/Intake/IntakeResponses.cs` — the detail, summary and page DTOs,
      **projecting** `CurrentCaseId`, `AssociationWasStaffDecision`, `UnlinkCancelsCase` and
      `CurrentCaseReference` rather than recomputing them.
- [ ] Map the DTO's decision vocabulary onto the single table [[INTK-001]] establishes, or — if it
      has not landed — record in the plan which enumeration was read and that a fourth copy was
      deliberately avoided.
- [ ] Add `src/Pegasus.Contracts/Intake/IntakeCommands.cs` — nine command DTOs, the three
      case-association ones carrying the receipt version **and** the case `expectedVersion` **and**
      `editLeaseToken`.
- [ ] Add `src/Pegasus.Web/Api/IntakeEndpoints.cs` with a `received` sub-group carrying
      `.RequireStaffRight(StaffAccessRight.PerformCasework)` and all fourteen named routes.
- [ ] Cap `GET /received` at `pageSize` 100 (Core's own bound, lower than the board's 200) and map
      all three of `ListIntake`'s `ArgumentOutOfRangeException` throws to `validation` problems.
- [ ] Expose the decision filter only on `GET /received`, and record in the plan why `queue`/
      `state` as written in the endpoint-map cannot be honoured.
- [ ] Implement the three byte routes with `Content-Length`, weak `ETag`, range processing,
      `nosniff`, and a filename validated by the `AutomationMcpErrors.RequireFileName` rule.
- [ ] Carry across the asset route's media-type gate (non-`image/*` → not found) and its
      `Cache-Control: private, no-store`.
- [ ] Add the `IntakeArtifactIntegrityException` arm to the `/api/v1` problem mapper so the byte
      routes return a problem document instead of 409 `text/plain`.
- [ ] Record in the plan that the three byte routes must be exempt from response compression when
      [[GWY-017]] adds the middleware.
- [ ] Wire `case-lease/claim`, `link-case` and `reverse-case-link` to `IAcquireCaseEditLease`,
      `ILinkIntake` and `IReverseIntakeLink` exactly as `Details.cshtml.cs:240`, `:274`, `:310` do.
- [ ] Add `tests/Pegasus.IntegrationTests/DesktopGatewayIntakeTests.cs` with the seven-case matrix
      for all nine commands, reusing `IntakeWebTestSupport.cs`.
- [ ] Add the paging-bound facts (`pageSize=101`, `page=0`, undefined decision) and the detail's
      `version` + `ETag` facts.
- [ ] Add six byte-safety facts per byte route, including the `206` range slice and the
      **refused** hostile filename, plus the asset media-type fact.
- [ ] Write the dated *Pre-snapshot record* in the plan — one line each for [[INTK-001]],
      [[INTK-004]] and [[INTK-006]], landed or deferred-with-reason — **before** running the
      export.
- [ ] Regenerate and commit `openapi/pegasus-v1.json` and the Kiota client via
      `eng/api/Generate-ApiClient.ps1`.
- [ ] Run the simplification pass over this branch's diff and record it under a dated
      `## Simplification pass` heading in the plan.
- [ ] Run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayIntakeTests"`,
      then `--filter "FullyQualifiedName~MultiFormatIntakeWebTests"`; capture both outputs together
      with the completed *Pre-snapshot record* as the tier-5 `proof`.

## Progress notes
