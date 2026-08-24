# Open questions — FND-020

These boxes are the gate. For profile `spike` an unticked `- [ ]` line **above** the
`## Parked` heading blocks `enter-done` — and only `enter-done`; it never gates
`leave-backlog`. Verified with `get_doc_gates` (no id): `spike` resolves to
`enter-done: [research, questions-resolved]` and nothing else.

Every box corresponds to a `NOT YET CAPTURED` block in the `research` document. Tick a
box only when the answer is written into
`docs/desktop/01-inventory-and-parity/flow-records.md` (or moved to
`docs/open-decisions.md`) **and** recorded on the box itself.

- [ ] **U-1 · `Q4.1` — can `Box.Sdk.Gen 1.12.0` issue short-lived, constrained
      upload/download URLs for direct desktop transfer?** Matters because guessing yes
      when the answer is no produces an upload path that either fails or ships a
      provider secret. Answered by the implementer from the SDK surface reachable from
      `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs:7` plus Box's own published
      API documentation. Unblocked by: a yes/no **and** the named SDK method, or the
      explicit statement that none exists. Recommended: expect no (research A-01-5),
      and then record the record's own consequence — stream through the gateway and
      size the Container App accordingly.
- [ ] **U-2 · `Q4.2` — which document metadata fields already exist for file type,
      size, source, uploader and timestamp?** Matters because proposal §14.6's
      Documents tab is designed against those five fields. Unblocked by: one line per
      field, `path:line` where it exists, "needs projection work" where it does not.
      Start at `src/Pegasus.Core/Documents/DocumentContracts.cs` (research F-8).
- [ ] **U-3 · `Q4.3` — must upstream `PLAT-041` land before the export endpoint is
      exposed?** Matters because a desktop batch export against a per-image folder
      resolve multiplies Box calls. Answered from the triage table in
      `upstream-kanmer-carryover.md`; record the ordering constraint for area 07 and
      for [[FND-023]] (plan handle `DSK-01-10`). Note upstream `PLAT-041` is outside
      the first sync's range, so it may still be open.
- [ ] **U-4 · record 5's read-only verification re-run.** The adapter, migration and
      secret citations in record 5 must be re-checked at the head this ticket runs on
      and corrected where they disagree. Unblocked by: the three commands in research
      U-4 and the corrections made in the record.
- [ ] **U-5 · `Q5.1` — does either provider contract allow a direct call from a
      public/native client?** Matters because a yes would mean a provider key in an
      MSIX, which ADR-0107 exists to forbid. Proposal §12.3 makes the default **no**
      and requires an exception to be proved. Unblocked by: cited contrary evidence, or
      the literal sentence "no evidence found; default no".
- [ ] **U-6 · `Q5.3` — does the gateway request path ever call the provider inline?**
      Answered by tracing `OnPostRequestVehicleLookupAsync` in
      `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` to the durable request row and
      showing the live adapter is reached only from
      `src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs`. Research F-5 (no
      DVLA/DVSA secret on the Web app) is corroboration, not the trace.
- [ ] **U-7 · `Q6.1` — which templates are in scope for the desktop?** This depends on
      upstream `TICK-206`, which the carry-over triage classes `report-decision` and
      which has **no fork ticket** — it is on [[FND-022]]'s (plan handle `DSK-01-09`)
      drop list, so nobody on this board will resolve it. Recommended answer: record it
      as a decision — one line in `docs/open-decisions.md` — and list all six templates
      (`advert_evidence_pack`, `assessment_fee_note`, `assessment_report`,
      `expert_report`, `fee_note`, `market_valuation_evidence`) with their current
      caller so the decision has an inventory to work from.
- [ ] **U-8 · `Q6.3` — WebView2 runtime presence on the ten workstations.**
      **Operator step**; no repository command can answer it. The operator confirms per
      workstation the Windows 11 build and the installed Evergreen WebView2 runtime
      version, or states that a fixed-version runtime must be shipped. Evidence to hand
      back: one line per workstation. If the operator cannot answer yet, add it to
      `docs/open-decisions.md` and tick this box against that line.
- [ ] **U-9 · `Q6.2` and `Q6.4` answered from official documentation, never from
      memory.** Unblocked by: a Microsoft Learn URL **and a fetch date** beside the
      `CoreWebView2.PrintToPdfAsync` / `CoreWebView2PrintSettings` answer (page size,
      margins, header/footer, fonts), the same for PDFsharp post-processing, and the
      explicit sentence that fidelity against `PlaywrightAssessmentReportRenderer.cs`
      output is **measured by the Phase 7 spike, not settled here**.
- [ ] **U-10 · records 4, 5 and 6 written back and closed.** Every `Q` heading reads
      `Answered <date>: …` or `Moved to docs/open-decisions.md <date>`; the template
      count is corrected to six `.scriban` files plus `report.css`; the pinned
      Playwright version (`1.61.0`) and the `ContainerBaseImage` tag are both stated;
      `pwsh ./scripts/Test-DocumentationLinks.ps1` and
      `pwsh ./scripts/Test-MarkdownPlacement.ps1` both exit 0.

## Parked (explicitly deferred)

Everything below this heading is **not** counted by the gate.

- [ ] The duplicate `Microsoft.Playwright` version literal at
      `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:17` is the only
      place the ADR-0028 pin can desynchronise from
      `Directory.Build.props:17`. Safe to defer here because this ticket may not edit a
      `.csproj` at all, and removing the literal is already an acceptance criterion of
      [[FND-027]] (plan handle `DSK-02-02`). Reopened only if [[FND-027]] drops it.
- [ ] Whether record 6's "Upstream decisions pending" list should be re-stated once
      [[FND-022]] (plan handle `DSK-01-09`) has finished the carry-over triage — it
      names upstream `TICK-206`, `TICK-216` and upstream `DOCS-001` (board
      [[DOCS-001]]), whose dispositions that ticket settles. Safe to defer: the answers
      this ticket writes cite the triage table, which stays authoritative either way.
