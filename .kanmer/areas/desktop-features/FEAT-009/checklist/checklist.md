# Checklist — FEAT-009

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")`; append progress notes below rather than
rewriting.

- [ ] Read plan row `DSK-05-09`, `vertical-slices.md:333-373`, `screen-specs.md:271-285` and `docs/design/README.md:396-421`; call `get_doc_gates FEAT-009`; `take_ticket` on branch `task/dsk-05-09-received-items`, worktree `../pegasus-worktrees/dsk-05-09-received-items`, from `origin/dev`
- [ ] Append to `research` the table of the nine POST handlers in `Intake/Details.cshtml.cs` — name, line, Core use case, required `expectedVersion`/`operationKey`/`reason`, operation-key bound, failure paths
- [ ] Append to `research` how `Source.cshtml.cs`, `Asset.cshtml.cs` and `Image.cshtml.cs` each validate and stream, including the SHA-256 fixed-time check at `DownloadIntakeSource.cs:40-43`, and record the SHA characterized
- [ ] Write characterization facts in `tests/Pegasus.Core.Tests/Intake/` for the link and reverse-link integrity checks against **current** behaviour, before moving anything
- [ ] Write characterization facts for the re-evaluation preconditions as they behave today, naming upstream INTK-027 (board [[INTK-004]]) in the test comment as a known defect owned there
- [ ] Move the link/reverse-link and re-evaluation rules into `src/Pegasus.Core/Intake/` and re-point `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` at them, leaving no second implementation
- [ ] Confirm against the generated client that [[GWY-010]] has landed `GET /api/v1/received/{id}`, the nine named commands and the three byte endpoints with `ETag`, range, no-sniff and a safe filename — stop and raise on [[GWY-010]] if any is missing
- [ ] Add the received-item DTOs to `src/Pegasus.Contracts`: detail, classification evidence, field suggestions with provenance, extracted-text availability, and the read-only typed draft
- [ ] Implement `ReceivedItemViewModel` in `src/Pegasus.Desktop` with one command object per action, each carrying its own `operationKey` and the receipt `expectedVersion`, and the [[FEAT-008]] conflict pattern on 409
- [ ] Wire `link-case`, `reverse-case-link` and `case-lease/claim` to acquire the case edit lease through the [[FEAT-005]] session and send the case `expectedVersion` plus `editLeaseToken`
- [ ] Implement the streaming byte-download service in `src/Pegasus.Desktop.Infrastructure` with progress, cancel, a per-user temporary path with restrictive ACLs and bounded retention — nothing buffered whole
- [ ] Build the Received item XAML: identity head, tabs Evidence/Draft/Decision/Case/History, only populated sections rendered, an `AutomationId` on every control
- [ ] Render the blocked and withheld states with the approved necessary copy verbatim from `docs/design/README.md:402` and `:404`, and confirm the words `intake`, `artifact`, `durable` and `bytes` appear nowhere in operator copy
- [ ] Bind every decision label to [[FEAT-023]]'s single `OperatorLabels` list, changing no label text in this slice
- [ ] Add contract tests in `tests/Pegasus.Api.ContractTests` applying the [[TEST-002]] seven-case matrix to each of the nine commands, with `Features:DesktopGateway` enabled explicitly
- [ ] Add contract tests for each byte endpoint: 200 with `ETag` and no-sniff, range request, 404, 403
- [ ] Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for `CanExecute` gating, reason-required commands, streaming progress and cancellation, and read-only draft rendering
- [ ] Run the tier-8 corpus comparison locally over the `MultiFormatIntakeWebTests.cs` reviewed cohort for all nine actions; record only the pass/fail table, commit no corpus content
- [ ] Update `parity-matrix.md` rows `PAR-19` and `PAR-20`
- [ ] Correct `vertical-slices.md:369-373`'s "Absorbs upstream" line, coordinating with [[FND-022]] so it changes once
- [ ] Add the received-items section to `docs/frd/frd-13-desktop-operator-experience.md` (or contribute it to [[DUI-013]] if that file has not landed) and add the `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test` for `Pegasus.Core.Tests`, `Pegasus.Api.ContractTests`, `Pegasus.Desktop.ViewModelTests` and the filtered `Pegasus.IntegrationTests`, plus the tier-7 keyboard/axe artefacts and the tier-8 table; this box produces `proof`
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
