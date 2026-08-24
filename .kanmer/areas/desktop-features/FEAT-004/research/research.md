# Research — FEAT-004: the draft-to-case mapping, and where its rules live

## Question

What rules does the web create screen apply between a typed instruction draft
and an allocated case reference, which of them live in `Pegasus.Core` and which
only in the page model, and what must `POST /api/v1/cases` do so allocation
outcomes match the web exactly?

## Current behaviour

`src/Pegasus.Web/Pages/Cases/Create.cshtml.cs` (689 lines, verified `wc -l`).
`CreateModel` takes six dependencies (`:46-52`): `IGetIntake`, `IResolveIntake`,
`IAllocateIntake`, `IInspectionAddressResolutionStore`,
`IProviderInspectionModeStore`, `ILogger<CreateModel>`. Two handlers:

- `OnGetAsync(Guid receiptId, …)` (`:210-265`) — `Guid.Empty` → `NotFound()`
  (`:216-220`); an already-allocated receipt redirects to the case (`:230-235`);
  otherwise it prefills every bound field from `Receipt.InstructionDraft`
  (`:238-249`), mints `OperationId = NewOperationKey()` (`:261`) and records
  `ExpectedReceiptVersion = Receipt.Version` (`:262`).
- `OnPostCreateAsync` (`:266-435`) — validate everything, then write **three
  times in sequence**.

**Creation is a three-write sequence, not one call** (`:319-377`):

1. `resolveIntake.ExecuteAsync(new(Receipt.Id, ExpectedReceiptVersion, actor,
   $"case-create-draft:{operationId:N}", reason, IntakeResolutionKind.CorrectDraft, postedDraft), …)`
   (`:322-331`) — always run, never skipped on a "nothing changed" test;
2. `addressResolutionStore.ResolveAsync(…, DeriveOperationId(operationId, "address"), …)`
   (`:336-358`), only when `AsksForAddress`;
3. `allocateIntake.AttemptStaffCreateAsync(new(Receipt.Id, version, actor,
   $"intake-accept:{operationId:N}", reason, CaseType, principalCode,
   new(InstructionComplete, ImagesComplete, InstructionConfirmedByStaff, ImagesConfirmedByStaff),
   null, corrected.InstructionDraft?.InspectionDate), …)` (`:360-376`).

The class remarks (`:22-42`) state the three rules that govern the sequence and
that the desktop must preserve:

- **One button.** "Creating a case takes up to three writes… They are sequenced
  here, on one submit, because the operator's action is a single one."
- **The version chain.** "Each step takes the version the *previous step
  returned* — never a re-read. Re-reading would reintroduce exactly the race the
  acceptance replay guard exists to prevent."
- **Replay.** "Every operation key derives from one page-level operation id…
  `ExpectedReceiptVersion` is deliberately *not* advanced when a later step
  fails: the correction's replay fingerprint includes the version it expected,
  so changing it would turn a resumed submit into a conflict."

Parity matrix row: **`PAR-09`** (13.3 Case lifecycle, FRD-01 + FRD-02,
`Cases/Create.cshtml.cs` (689) — `OnGetAsync`, `OnPostCreateAsync`, status
`inventoried`) at `docs/desktop/01-inventory-and-parity/parity-matrix.md:54`.
The matrix holds 46 `PAR-` rows (`grep -c '^| PAR-' …` → `46`), all keyed to
page models under `src/Pegasus.Web/Pages/**`.

## Findings

### Facts

Verified at `HEAD` `bbd1c549` (2026-08-24). `git diff --stat 191ddf33..HEAD -- src tests`
is empty, so the plan set's line references still hold. **`bbd1c549` is the
revision characterized.**

- **There is no "create from blank" on the web today.** Both handlers are
  receipt-scoped: `OnGetAsync` takes a `receiptId` and refuses `Guid.Empty`
  (`:216-220`); `OnPostCreateAsync` begins with `LoadAsync(ReceiptId, …)`
  (`:268-272`) and the whole sequence writes against `Receipt.Id`. The class
  remarks say it plainly (`:14-21`): "this is the only place in the application
  that begins a staff allocation through `IAllocateIntake`", reached by an
  upload landing "here directly with what extraction found already in the
  boxes".
  - **The ticket body requires create "from an instruction draft or from
    blank", and the body is settled and outranks this document.** The finding is
    recorded so the plan sizes it honestly: the blank path is **new behaviour**,
    not a port. It needs a receipt-less create path in Core or a synthesised
    empty receipt, and it has no web oracle to compare against — so the parity
    comparison at step 11 covers the draft path only, and the blank path is
    proved by its own contract and characterization facts. Flagged to the
    reviewer rather than silently resolved.
- **Rules that already live in Core** (each verified by reading the file):
  | Rule | Owner | Location |
  | --- | --- | --- |
  | Which draft fields block allocation | `InstructionDraftCompleteness.MissingIdentityCriticalFieldNames` — exactly three: Claimant name, Claim number, Vehicle registration | `src/Pegasus.Core/Intake/InstructionDraftCompleteness.cs:96-116` |
  | Whether a corrected draft is complete enough to be decided | `InstructionDraftCompleteness.MissingFieldNames` — the fuller list | same file, `:25-…` |
  | Principal code normalization and its 20-character bound | `CasePrincipalCode.Normalize` / `MaximumLength = 20` | `src/Pegasus.Core/Cases/CaseContracts.cs:74-86` |
  | Whether the address question is asked at all | `InspectionAddressResolutionPolicy.SatisfiesCaseCreation(state, providerIsImageBased)` | `src/Pegasus.Core/Address/InspectionAddressResolution.cs:135-138` |
  | Whether a person already settled the address | `InspectionAddressResolutionPolicy.IsStaffResolved` | same file, `:116` |
  | The Image-Based-Assessment sentinel | `Ext18InspectionAddressPolicy.ImageBasedAssessment` | `src/Pegasus.Core/Address/Ext18InspectionAddressPolicy.cs` |
  | Whether a receipt can become a case at all | `IntakeDecisionPolicy.CanBecomeCase` | referenced at `Create.cshtml.cs:588-592` |
  | The refusal sentence for a decision that cannot | `OperatorLabels.IntakeCannotBecomeCaseReason` | `src/Pegasus.Web/Presentation/OperatorLabels.cs:353` |
  | Allocation itself, and replay | `IAllocateIntake.AttemptStaffCreateAsync` (`src/Pegasus.Core/Intake/IntakeAllocation.cs:181`), implemented by `AllocateIntake` (`:208`) | — |
- **Rules that live only in the page model** — these are the characterization
  gap this ticket must close:
  | Rule | Location | What it decides |
  | --- | --- | --- |
  | The reason is required and ≤ 500 characters | `Create.cshtml.cs:445-456` | Refusal before any write |
  | `CaseType.Audit` cannot be created manually | `ValidateAuditCannotBeManuallyCreated`, `:548-559` | Refusal; the sentence is "Audits are created automatically from the retained Audit instruction and original report." |
  | The address-choice matrix — suggestion present vs absent, accept vs correct vs supply, stale fingerprint | `ValidateAddressChoice`, `:503-546` | Which `InspectionAddressStaffDecision` is sent, and four distinct refusal sentences |
  | Which address value actually goes into the draft | `EffectiveInspectionAddress`, `:562-582` | Image-based provider → resolved ?? suggestion ?? `Ext18InspectionAddressPolicy.ImageBasedAssessment`; already-settled → resolved; otherwise the chosen or entered value |
  | Whether the screen refuses the receipt outright | `DescribeRefusal`, `:584-601` | Audit classification → its own sentence; `OcrRequired` or `CanBecomeCase` → allowed; otherwise the Core refusal label |
  | The operation-key derivation | `DeriveOperationId`, `:658-665`, plus the two literal prefixes at `:326` and `:364` | Three keys from one page-level id, so a resumed submit replays rather than duplicates |
  | Which principal code is offered as the draft's suggestion vs the confirmed one | `ValidateAndBuildDraft`, `:476-480` (`Optional(SuggestedPrincipalCode) ?? Optional(PrincipalCode)`) | The draft carries the suggestion; the allocation carries the confirmed code |
- **The refusal path is a redirect with a message, not a rendered outcome.**
  When allocation does not succeed, `Create.cshtml.cs:378-384` sets
  `TempData["IntakeDetailsError"]` and redirects to `/Intake/Details`. The
  approved sentence "No case or reference was created; review the missing or
  conflicting evidence." lives in `docs/design/README.md:404`; the page uses
  `allocation.State.SafeReason ?? "The case could not be created. No reference
  was allocated."` (`:380-381`). The desktop renders the outcome in place and
  keeps proposed values in memory — screen spec `:242-245`.
- **Six distinct failure branches** with six distinct operator sentences
  (`:391-424`): `StaffAuthorizationException` → `Forbid()`;
  `InspectionAddressResolutionConcurrencyException`;
  `CaseAcceptanceOperationConflictException`;
  `CaseIdentitySequenceExhaustedException`;
  `IntakeVersionConflictException` / `IntakeOperationConflictException`;
  and anything `IntakeExceptionPolicy.IsRecoverable`. Each is a problem type the
  gateway must translate distinctly; collapsing them loses operator meaning.
- **Provenance is computed, and its rule is one line.**
  `src/Pegasus.Web/Presentation/InstructionDraftFieldsView.cs` (64 lines)
  `ProvenanceWord(fieldName)` returns `"Extracted"` when extraction offered a
  candidate for that field and `"Staff"` otherwise (`:58-60`). The closed
  seven-value list — `Staff · Extracted · AI · E-mail · Lookup · Principal ·
  Automatic` — is at `docs/design/README.md:177`, together with the rule that
  provenance is "an icon with a one-word tooltip, shown on hover **and** on
  keyboard focus with a matching accessible name", and that "Source labels,
  policy keys and provenance sentences do not appear in markup."
  `InstructionDraftFieldsView`'s own remarks (`:9-22`) record why the view model
  exists: eleven values are asked for by two screens, and "a second copy of this
  markup would be a second place to forget a field."
- **`AcceptIntakeRequest`** (`src/Pegasus.Core/Intake/IntakeContracts.cs:814-826`)
  is `(ReceiptId, ExpectedVersion, Actor, OperationKey, Reason, CaseType,
  PrincipalCode, Completeness, StandaloneAuditEvidenceId?, AcceptedInspectionDeadline?,
  AllocationAttemptId?, AllocationCompletedAtUtc?)`. `InstructionDraft`
  (`:352-364`) is twelve members. Both are already transport-shaped.
- **The `TempData` machinery the desktop must not reproduce** is
  `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs`: keys at `:20-30`, the
  budgets `MaximumRetainedProposedCharacters = 8000` (`:38`) and
  `MaximumRetainedProposedValueCharacters = 2000` (`:39`), and the
  `RetainableFormFields` allow-list of 41 names at `:46-88`. Note that
  `CreateModel` derives from `StaffPageModel`, **not** `CaseMutationPageModel`
  (`:52`), so the create screen does not itself use that retention — it
  re-renders its own bound properties. The ticket body cites `:36-80` as the
  mechanism the desktop must not reproduce, and that is right for the programme;
  for this screen specifically the mechanism to avoid is `ModelState`
  re-rendering plus the re-issued `OperationId`/`ExpectedReceiptVersion` at
  `:432-434`.
- Existing test evidence, located by `ls tests/Pegasus.IntegrationTests`:
  `CaseCreateWebTests.cs` (918 lines), `CaseAcceptanceReplayTests.cs` (467),
  `QdosIntakeWebTests.cs`, `QdosAllocationRecoveryTests.cs`,
  `InstructionDraftWebTests.cs`, `ProviderInspectionModeAcceptanceTests.cs`,
  and `tests/Pegasus.IntegrationTests/Browser/QdosAllocationRecoveryBrowserTests.cs`
  (named in `parity-matrix.md:54`). `CaseCreateWebTests.cs` and
  `CaseAcceptanceReplayTests.cs` are more precise than the plan set's citation
  of the QDOS tests alone and are the primary oracles.
- **Target projects do not exist yet.** `Pegasus.slnx` lists four production and
  three test projects. `grep -rn "DesktopGateway" src/ tests/` returns nothing —
  the gate is introduced by [[GWY-002]] (plan handle `DSK-03-02`).

### Assumptions

- **`A-05-12` — `POST /api/v1/cases` will carry the whole three-write sequence
  server-side.** `endpoint-map.md:53` shows one row,
  `POST /cases` → "`src/Pegasus.Core/Cases/` create use case via
  `IAllocateIntake`/acceptance path", idempotent by key. The web performs three
  writes. Confirmed by: reading [[GWY-008]] (plan handle `DSK-03-08`)'s
  delivered request shape at step 4. Breaks if wrong: the desktop would have to
  orchestrate three round trips and hold the version chain itself — which
  re-creates the race the class remarks at `Create.cshtml.cs:29-35` say the
  replay guard exists to prevent, and which would be a second implementation of
  the sequencing rule. If [[GWY-008]] has not folded it, **stop and raise it
  there**.
- **`A-05-13` — the six failure branches will map to six distinct problem
  types.** `endpoint-map.md` does not enumerate them. Confirmed by: the contract
  facts at step 10 and [[GWY-002]]'s problem-details mapping. Breaks if wrong:
  the desktop cannot tell "the address evidence is stale" from "this item was
  already turned into a case using different details", and both would render the
  same sentence — a real loss of operator meaning.
- **`A-05-14` — the create endpoint returns the provenance per field on the
  draft read, not computed on the desktop.** `ProvenanceWord`
  (`InstructionDraftFieldsView.cs:58-60`) needs `ExtractedFields`, which the
  desktop only has if the draft read carries them. Confirmed by: reading the
  draft read's DTO. Breaks if wrong: the desktop cannot show provenance at all,
  or computes it from a partial view of the candidates — the first is a missing
  acceptance criterion, the second is a second implementation.
- **`A-05-15` — "create from blank" is new scope the gateway can serve.** No web
  path exists (see the first fact). Confirmed by: whether [[GWY-008]] accepts a
  create without a receipt id, or whether Core's acceptance path can be reached
  without one. Breaks if wrong: the blank path is deferred with a recorded
  reason and raised on [[GWY-008]] rather than synthesised on the desktop.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered for the case-create responsibility.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | The receipt is shared state with its own version, and the sequence takes the version each write returns (`Create.cshtml.cs:29-35`). Two operators creating from the same receipt must not both allocate. Lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **Yes, for the automatic route — no, for this one.** | `IAllocateIntake` has two entry points: `AttemptAutomaticAsync` (`src/Pegasus.Core/Intake/IntakeAllocation.cs:176`), which the Worker drives, and `AttemptStaffCreateAsync` (`:181`), which this screen drives. The automatic route is already unattended and already lands in the Worker (ADR-0106 territory); **this ticket's responsibility is the staff route only**, and it runs when an operator presses a button. Naming the host: `Pegasus.Worker` keeps the automatic route; nothing moves. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | The create sequence touches SQL and the artefact store, both behind the gateway. No provider secret is involved. |
| Public callback — must an external service call a stable public endpoint? | **No** | No external party creates a case. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | Reference allocation is the strongest invariant on the board: `AGENTS.md` § Product invariants — "Fail closed before case creation or normal Case/PO allocation when processing, limits, or principal identity are incomplete or ambiguous" and "Principal and reference are immutable after allocation". `AllocateIntake` owns replay and the identity sequence (`CaseIdentitySequenceExhaustedException`, `Create.cshtml.cs:409-416`). None of it can be trusted to a client. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **n/a** | No measurement exists either way and none is needed: questions 1, 2 and 5 already place the write. Immediate field validation runs on the desktop through the deterministic Core rules (reuse-map boundary note), and the gateway re-checks on write. |

**Placement:** the desktop validates fields locally with the same Core rules and
confirms the operator's single deliberate action; the gateway sequences the
three writes, allocates the reference, and audits. Three "yes" answers — two
naming the gateway, one naming the Worker for a route this ticket does not
touch. No Azure resource is involved and no Azure write occurs.

## Implications

- **The characterization work is the first half of this ticket, not a
  formality.** Seven rules live only in the page model (table above). Each needs
  a test in `tests/Pegasus.Core.Tests` written against **current** behaviour
  before it moves, and the Razor page re-pointed at the moved rule. The address
  matrix (`ValidateAddressChoice`, `:503-546`) and `EffectiveInspectionAddress`
  (`:562-582`) are the two that genuinely decide business outcomes; the reason
  bound and the Audit refusal are simpler but are still rules.
- **`EffectiveInspectionAddress` is the highest-risk move.** It reaches into
  `Ext18InspectionAddressPolicy.ImageBasedAssessment` and picks between three
  sources in a fixed order. Getting the order wrong changes which address the
  case is created with, silently. Characterize all three branches — image-based
  provider, already-settled, and the choose-or-enter path — before touching it.
- **The three-write sequence belongs to the gateway or nowhere.** See `A-05-12`.
  A desktop that made three calls would own the version chain and the replay
  fingerprint, both of which the class remarks say must not be re-derived.
- **One `operationKey` per create attempt, reused on retry.** The web mints one
  `OperationId` per page render (`:261`) and derives three keys from it
  (`:326`, `:354`, `:364`); on failure it re-renders the **same** id
  (`:432-434`) so pressing again resumes. The desktop's equivalent is: generate
  once per create attempt, reuse on transport retry, and mint a new one only
  when the operator deliberately starts again.
- **`ExpectedReceiptVersion` must not advance on a mid-sequence failure.**
  `Create.cshtml.cs:36-41` explains why: "the correction's replay fingerprint
  includes the version it expected, so changing it would turn a resumed submit
  into a conflict." If the gateway folds the sequence, this becomes the
  gateway's rule — but the desktop must not "helpfully" re-read the version
  after a failure either.
- **Provenance is a rendering of what the read carries, never a desktop
  inference.** `ProvenanceWord` is two-valued today (`Extracted` / `Staff`) even
  though the closed list has seven values. The DTO should carry the provenance
  value per field so the other five become expressible without a desktop-side
  rule.
- **"From blank" needs its own acceptance evidence.** With no web oracle
  (first fact), the parity table at step 11 covers the draft path; the blank
  path is proved by contract facts plus a Core characterization test for the
  minimum draft that can allocate — which is exactly
  `MissingIdentityCriticalFieldNames`'s three fields
  (`InstructionDraftCompleteness.cs:96-116`).
- **Audit is refused at create, and must stay refused.** `CaseType.Audit`
  (`:548-559`) with its exact sentence. The desktop must not offer Audit in the
  case-type dropdown at all — "Deferred capabilities are absent, not disabled"
  (`docs/desktop/06-ui-design/screen-specs.md:28-30`) — and the gateway must
  still refuse it if the UI is bypassed.

## Open questions

None that block the plan. `A-05-12` and `A-05-15` are settled by step 4's
reading of [[GWY-008]] (plan handle `DSK-03-08`)'s delivered contract, `A-05-13`
by the contract facts at step 10, and `A-05-14` by reading the draft read's
DTO. The "from blank" divergence from current web behaviour is recorded above
and in the plan's *Risks / open questions* section as a **deliberate new
capability carried by the settled ticket body**, not as a question — the body
outranks this document. No `open-questions` document is created; the body does
not ask for one and nothing here is unsettled in a way the plan would silently
assume.
