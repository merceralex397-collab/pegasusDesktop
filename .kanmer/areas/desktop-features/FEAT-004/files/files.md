# Files — FEAT-004

Surface area of `DSK-05-04 · S4 Case create`. Paths that do not exist at `HEAD`
`bbd1c549` are marked with the ticket that creates them; every other path was
confirmed with `ls` or `wc -l`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Core/Cases/` and `src/Pegasus.Core/Address/` | **Only** for the seven rules that today live in the page model (the `research` table), each moved in **after** a characterization test in `tests/Pegasus.Core.Tests` pins current behaviour. The highest-risk move is `EffectiveInspectionAddress` (`src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:562-582`) — it picks between three address sources in a fixed order and getting the order wrong silently changes which address the case is created with. |
| `tests/Pegasus.Core.Tests/` | The characterization tests, written **first**, against current behaviour. `Cases/` and `Address/` already exist as folders (`ls tests/Pegasus.Core.Tests` → `Address`, `Cases`, …). |
| `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs` | Re-pointed at each moved Core rule. **Behaviour must not change** — `CaseCreateWebTests.cs` (918 lines) and `CaseAcceptanceReplayTests.cs` (467) are the guard. The page is not removed. |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | The create request, the draft read DTO and a **provenance value per field** from the closed seven-value list at `docs/design/README.md:177`. |
| `src/Pegasus.Web/` — the `/api/v1` cases **command** group only *(group by [[GWY-002]] (plan handle `DSK-03-02`); route by [[GWY-008]] (plan handle `DSK-03-08`))* | `POST /api/v1/cases`, idempotent by `operationKey`, carrying the whole three-write sequence server-side and returning 201 with the case id and version. Six distinct failure branches must map to six distinct problem types. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | `CaseCreateViewModel` (immediate field validation against the deterministic Core rules, deliberate Save, one `operationKey` per create attempt reused on retry) and the create XAML. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | The client call, and — only where proposal §11.1 justifies it — an **encrypted** local draft through the credential/cache abstraction. Never a `TempData` equivalent. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | Validation, dirty state, deliberate-save gate, operation-key reuse, the three allocation outcomes with their approved copy. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Create success, replay returning the same result, validation failure as a problem document, 401, 403 without `PerformCasework`, and one fact per distinct failure branch. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Row `PAR-09` (`:54`). |
| `docs/frd/frd-13-desktop-operator-experience.md` *(skeleton by [[FND-008]] (plan handle `DSK-00-08`))* | Create section, including the "from blank" path as a recorded new capability. |
| `docs/capabilities.md` | One `DSK` row for case create. |

## Context files

Read these before writing code. Each is here for one specific trap.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:13-42` (class remarks) | The three governing rules, stated by the author: **One button** ("Creating a case takes up to three writes… They are sequenced here, on one submit"); **The version chain** ("Each step takes the version the *previous step returned* — never a re-read"); **Replay** ("`ExpectedReceiptVersion` is deliberately *not* advanced when a later step fails: the correction's replay fingerprint includes the version it expected"). Everything else in this ticket follows from these. |
| `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:319-377` | The sequence itself, with its three literal operation keys: `case-create-draft:{operationId:N}` (`:326`), `DeriveOperationId(operationId, "address")` (`:354`), `intake-accept:{operationId:N}` (`:364`). Derived from one page-level `OperationId` minted at `:261`. |
| `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:443-500` (`ValidateAndBuildDraft`) | The reason bound (required, ≤ 500, `:445-456`), the principal-code bound (`CasePrincipalCode.MaximumLength`, `:457-467`), the case-type check (`:469-472`), and the fact that the draft carries `Optional(SuggestedPrincipalCode) ?? Optional(PrincipalCode)` (`:476-480`) while the allocation carries the **confirmed** code. |
| `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:503-546` (`ValidateAddressChoice`) | The address matrix and its four distinct refusal sentences — no suggestion at all; an undefined choice; entered-address chosen but empty; a stale fingerprint. One of the two rules that genuinely decides a business outcome. |
| `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:562-582` (`EffectiveInspectionAddress`) | The other one. Image-based provider → `AddressResolution.ResolvedValue ?? AddressSuggestion?.Value ?? Ext18InspectionAddressPolicy.ImageBasedAssessment`; already-settled → `ResolvedValue`; otherwise the chosen suggestion or the entered value. Characterize all three branches before moving it. |
| `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:548-559`, `:584-601` | `ValidateAuditCannotBeManuallyCreated` (Audit is refused, with an exact sentence) and `DescribeRefusal` (Audit classification; `OcrRequired` or `CanBecomeCase` allowed through; otherwise `OperatorLabels.IntakeCannotBecomeCaseReason`). |
| `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:391-424` | The **six** distinct failure branches and their six operator sentences. Collapsing them loses operator meaning; each is a problem type the gateway must translate distinctly. |
| `src/Pegasus.Core/Intake/InstructionDraftCompleteness.cs:96-116` | `MissingIdentityCriticalFieldNames` — exactly three fields block allocation: Claimant name, Claim number, Vehicle registration. Its remarks (`:88-95`) say why it is deliberately narrower than `MissingFieldNames`: "changing that would change the intake decision itself." This is also the minimum draft the "from blank" path must satisfy. |
| `src/Pegasus.Core/Address/InspectionAddressResolution.cs:109-138` | `InspectionAddressResolutionPolicy` — `IsStaffResolved` (`:116`) and `SatisfiesCaseCreation(state, providerIsImageBased)` (`:135-138`). These decide whether the address question is asked at all. |
| `src/Pegasus.Core/Address/Ext18InspectionAddressPolicy.cs` | The EXT-18 rule and the `ImageBasedAssessment` sentinel. EXT-18 "prohibits inferring an address" (`Create.cshtml.cs:57-59`), which is why the choice is always explicit. |
| `src/Pegasus.Core/Intake/IntakeAllocation.cs:174-188` | `IAllocateIntake`: `AttemptAutomaticAsync` (`:176`, the Worker's route), `AttemptStaffCreateAsync` (`:181`, this screen's route), `RetryAsync` (`:186`). The concrete `AllocateIntake` class begins at `:208` — the ticket body cites `:208` for the interface, which is the class; the interface is `:174`. `:206-207` calls it "the one Core owner for initial allocation, durable failure and reasoned staff retry." |
| `src/Pegasus.Core/Intake/IntakeContracts.cs:352-364`, `:814-826` | `InstructionDraft` (twelve members) and `AcceptIntakeRequest` (twelve members) — both already transport-shaped, so the DTOs mirror them rather than inventing a shape. |
| `src/Pegasus.Core/Cases/CaseContracts.cs:74-86` | `CasePrincipalCode.MaximumLength = 20` and `Normalize` (trim, upper-case, bound). The desktop's immediate validation uses this directly through the reuse-map boundary note. |
| `src/Pegasus.Web/Presentation/InstructionDraftFieldsView.cs` (64 lines) | The provenance rule in one method: `ProvenanceWord` returns `"Extracted"` when extraction offered a candidate for that field and `"Staff"` otherwise (`:58-60`). Its remarks (`:9-22`) record why the view model exists at all — "a second copy of this markup would be a second place to forget a field." |
| `docs/design/README.md:177` | The closed provenance list — `Staff · Extracted · AI · E-mail · Lookup · Principal · Automatic` — and the rule: "an icon with a one-word tooltip, shown on hover **and** on keyboard focus with a matching accessible name. Source labels, policy keys and provenance sentences do not appear in markup." |
| `docs/design/README.md:400-409` | The closed necessary-copy list, including the exact sentence "No case or reference was created; review the missing or conflicting evidence." |
| `docs/design/README.md:422-430` | "A field is a label and a control, nothing more. No hint sentence under a field, no 'Required.' or 'Optional.' text, no format guidance." A merge rule. |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:20-88` | The `TempData` machinery the desktop must not reproduce: the keys (`:20-30`), the budgets 8 000 / 2 000 (`:38-39`), and the 41-name `RetainableFormFields` allow-list (`:46-88`). Note `CreateModel` derives from `StaffPageModel`, **not** this class (`Create.cshtml.cs:52`) — for the create screen specifically the mechanism to avoid is `ModelState` re-rendering plus the re-issued `OperationId` / `ExpectedReceiptVersion` at `:432-434`. |
| `docs/desktop/06-ui-design/screen-specs.md:233-245` | The Case create block: reached from Cases (`Ctrl+N`) or from a received item; sections Principal and instruction, Vehicle, Inspection address, Dates; Create (primary) and Cancel; the refusal sentence rendered **in place** with proposed values kept in memory; AutomationIds `CaseCreate.<Section>.<Field>`, `CaseCreate.Submit`. |
| `docs/desktop/06-ui-design/screen-specs.md:28-30` | "Deferred capabilities are absent, not disabled." This is why `CaseType.Audit` must not appear in the case-type dropdown at all. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:53` | The `POST /cases` row: one endpoint, `PerformCasework`, `yes (key)`, returning "201 + case id + version". One row against the web's three writes — the assumption step 4 settles. |
| `docs/desktop/05-implementation-and-migration/README.md` § 3 | "Characterization before moving any rule", with "create-screen draft-to-case mapping (S4)" named in the gap list, and "a second implementation is a stop condition". |
| `tests/Pegasus.IntegrationTests/CaseCreateWebTests.cs` (918 lines) | The primary oracle. Every rule moved into Core must leave these green. |
| `tests/Pegasus.IntegrationTests/CaseAcceptanceReplayTests.cs` (467 lines) | The replay oracle — the behaviour the derived operation keys exist to produce. |
| `tests/Pegasus.IntegrationTests/QdosIntakeWebTests.cs`, `QdosAllocationRecoveryTests.cs` | The fixture set the ticket names for the step-11 comparison. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` | The shared `WebApplicationFactory<Program>`; `Features:DesktopGateway` must be enabled explicitly there. |
| `docs/desktop/08-testing/test-uat-stack.md:22` | The Test/UAT configuration for the tier-8 genuine-corpus run — which is **local only**, and whose material is never committed. |

## Ripple effects

- **The Razor create page is re-pointed, so its tests are the regression
  gate.** `CaseCreateWebTests.cs` (918), `CaseAcceptanceReplayTests.cs` (467),
  `QdosIntakeWebTests.cs`, `QdosAllocationRecoveryTests.cs`,
  `InstructionDraftWebTests.cs` and
  `ProviderInspectionModeAcceptanceTests.cs` must all stay green after each
  rule moves. This is the one ticket in the S1–S8 set that legitimately edits a
  Razor page model.
- **`tests/Pegasus.Core.Tests` grows before `src/Pegasus.Core` changes.** The
  reuse-map records this: "REUSE; grows with every characterization test written
  before a rule moves."
- **`InstructionDraftFieldsView` and `_InstructionDraftFields.cshtml` have a
  second caller** — the received-item correction screen
  (`InstructionDraftFieldsView.cs:9-22`). Moving a rule out of the create page
  must not change what that screen renders; [[FEAT-009]] (plan handle
  `DSK-05-09`) owns the received-item slice and consumes the same view model.
- **Generated client and OpenAPI snapshot.** [[GWY-005]] (plan handle
  `DSK-03-05`) commits Kiota output with a CI no-op check; [[TEST-001]] (plan
  handle `DSK-08-01`) fails the snapshot test on an undeclared change.
- **Architecture tests.** `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`
  (520 lines), extended by [[FND-037]] (plan handle `DSK-02-12`), fails on an
  ASP.NET/EF/WinUI type inside `Pegasus.Contracts` and on a
  `Pegasus.Infrastructure` reference from the desktop. The desktop's direct
  `Pegasus.Core` reference for deterministic validation is explicitly permitted
  by the reuse-map boundary note and must stay within it.
- **Screen-spec provenance ripples.** If the DTO carries more than the two
  provenance values `ProvenanceWord` produces today, [[DUI-011]] (plan handle
  `DSK-06-11`) owns the glyph and its accessible name for all seven.
- **Downstream tickets.** `FEAT-004` blocks `FEAT-022`, `FEAT-025`, `TEST-007`
  and `TEST-016`.
- **Documentation link check.** `scripts/Test-DocumentationLinks.ps1` runs over
  repository documentation, so a broken relative link in the new FRD-13 section
  fails CI.

## Out of scope

Recorded so the reviewer sees each was a decision.

- **The Razor create page is not removed**, only re-pointed at moved Core rules
  with its behaviour unchanged. The cut is [[FEAT-026]] (plan handle
  `DSK-05-26`).
- **No `TempData` equivalent, no `RetainableFormFields` allow-list, no
  8 000 / 2 000-character budgets**, and no PRG or antiforgery in the desktop
  path.
- **`CaseType.Audit` is not offered.** It is absent from the case-type dropdown,
  not disabled (`screen-specs.md:28-30`), and the gateway still refuses it if
  the UI is bypassed.
- **No second implementation of any moved rule.** If a rule ends up in both
  Core and the page model, that is a stop condition, not a migration step.
- **No editing of an allocated case.** Save, lease and completeness are
  [[FEAT-005]] (plan handle `DSK-05-05`); the workflow commands are [[FEAT-006]]
  (plan handle `DSK-05-06`). |
- **No received-item screen.** The draft **read** is consumed here; the intake
  detail surface and its ten commands are [[FEAT-009]] (plan handle `DSK-05-09`).
- **Genuine-corpus material is never committed.** Tier-8 evidence stays local
  (`docs/engineering.md` § Required evidence tiers, tier 8: "Detailed evidence
  remains ignored and local").
- **No Azure write.** Enabling `Features:DesktopGateway` in production is
  [[PLAT-024]] (plan handle `DSK-11-06`).
