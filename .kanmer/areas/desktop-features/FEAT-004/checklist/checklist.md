# Checklist — FEAT-004: S4 Case create

One box per plan step, in plan order. Tick with `set_ticket_doc`; append
progress notes below rather than rewriting.

- [ ] Read the plan row, `vertical-slices.md` § S4 and `docs/desktop/05-implementation-and-migration/README.md` § 3 (the characterization rule and its gap list); run `get_doc_gates FEAT-004`; `take_ticket` on branch `task/dsk-05-04-case-create`, worktree `../pegasus-worktrees/dsk-05-04-case-create`, from `origin/dev`
- [ ] Re-check parity drift: `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Create.cshtml.cs src/Pegasus.Core/Intake src/Pegasus.Core/Address src/Pegasus.Core/Cases` is empty, or re-read and update `research` with the new SHA
- [ ] Characterize `EffectiveInspectionAddress` (`Create.cshtml.cs:562-582`) in `tests/Pegasus.Core.Tests` across all three branches — image-based provider, already-settled, choose-or-enter — then move it into `src/Pegasus.Core/Address/` and re-point the Razor page
- [ ] Characterize `ValidateAddressChoice` (`:503-546`) across its four refusal outcomes, then move it and re-point the page
- [ ] Characterize `DescribeRefusal` (`:584-601`), then move it and re-point the page
- [ ] Characterize `ValidateAuditCannotBeManuallyCreated` (`:548-559`), then move it and re-point the page
- [ ] Characterize the reason bound (required, ≤ 500, `:445-456`), then move it and re-point the page
- [ ] Characterize the principal-code bound (`CasePrincipalCode.MaximumLength = 20`, `:457-467`), then move it and re-point the page
- [ ] Characterize the suggested-vs-confirmed principal split (`:476-480`), then move it and re-point the page
- [ ] Confirm after every move that no rule has two implementations — a second implementation is a stop condition, not a migration step
- [ ] Re-run `dotnet test ./tests/Pegasus.IntegrationTests/... --filter "Category!=Corpus&Category!=Browser"` after each move and confirm `CaseCreateWebTests`, `CaseAcceptanceReplayTests`, `InstructionDraftWebTests`, `ProviderInspectionModeAcceptanceTests` and the QDOS web tests stay green
- [ ] Confirm `POST /api/v1/cases` carries the whole three-write sequence server-side with the version chain taken from each write's return — **stop and raise it on [[GWY-008]]** if it has not folded the sequence
- [ ] Confirm `ExpectedReceiptVersion` is not advanced on a mid-sequence failure, so a resumed submit replays rather than conflicts
- [ ] Confirm the outcome vocabulary distinguishes created / withheld / failed exactly as `IntakeAllocationProjectionStatus` does
- [ ] Confirm the six failure branches (`Create.cshtml.cs:391-424`) map to six distinct problem types
- [ ] Add a contract fact that replaying the same `operationKey` returns the same result and never allocates a second reference
- [ ] Add the create request and draft-read DTOs to `src/Pegasus.Contracts`, carrying a provenance value per field from the closed list at `docs/design/README.md:177` and the extraction candidates behind it
- [ ] Implement `CaseCreateViewModel` with immediate field validation against the deterministic Core rules referenced from `Pegasus.Core`, and server validation surfaced next to the owning section
- [ ] Generate one stable `operationKey` per create attempt, reuse it on transport retry, and mint a new one only on a deliberate restart
- [ ] Hold unsaved state in the view model; where proposal §11.1 justifies a local draft, persist it encrypted through the [[FND-031]] cache abstraction — no `TempData` equivalent, no `RetainableFormFields` allow-list, no 8 000 / 2 000-character budgets
- [ ] Build the create XAML on the [[DUI-008]] form pattern — label and control only, no hint text, no "Required."/"Optional.", required state shown visually — with sections Principal and instruction, Vehicle, Inspection address, Dates
- [ ] Show a provenance glyph with its one-word tooltip beside each populated field per [[DUI-011]], on hover **and** keyboard focus with a matching accessible name
- [ ] Confirm `CaseType.Audit` is absent from the case-type dropdown (not disabled) and that the gateway still refuses it when the UI is bypassed
- [ ] Give every control an `AutomationId` (`CaseCreate.<Section>.<Field>`, `CaseCreate.Submit`)
- [ ] Write view-model tests: validation, dirty state, deliberate-save gate, operation-key reuse and deliberate-restart, and the three allocation outcomes with the exact approved refusal sentence
- [ ] Write contract tests: create success, replay, validation problem, 401, 403 without `PerformCasework`, and one fact per distinct failure branch — with `Features:DesktopGateway` enabled explicitly
- [ ] Produce the fixture comparison for the **draft path**: identical allocation outcome and reference behaviour web vs desktop across the `QdosIntakeWebTests` / `QdosAllocationRecoveryTests` fixtures
- [ ] Prove the **blank path** by its own evidence — the minimum-draft characterization (`InstructionDraftCompleteness.MissingIdentityCriticalFieldNames`, three fields) plus its contract facts — and record that it has no web oracle
- [ ] **Operator step** — run the case-create UAT script against the genuine corpus on the local Test/UAT stack (tier 8, local only, never committed) and capture the operator's sign-off text and date in the ticket proof
- [ ] Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-09`
- [ ] Add the create section to `docs/frd/frd-13-desktop-operator-experience.md`, recording the "from blank" path as a new capability with no web predecessor, and add a `DSK` row to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Verification run — `dotnet build ./Pegasus.slnx -c Release --no-restore`; `dotnet test` for Core.Tests, Api.ContractTests, Desktop.ViewModelTests and IntegrationTests (`--filter "Category!=Corpus&Category!=Browser"`); then write `proof` with the command log, the fixture comparison and the operator sign-off
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
