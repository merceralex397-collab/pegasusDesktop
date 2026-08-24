# Research — FEAT-005: the case edit lease, version and completeness rules

## Question

Which parts of the web's edit path are business behaviour that must travel to
the desktop, which are web mechanics that must not, and what exactly does Core
require on every case mutation so a two-user conflict test passes with nothing
silently overwritten?

## Current behaviour

Five handlers on `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` (654 lines):

| Handler | Line | Parameters | Core call |
| --- | --- | --- | --- |
| `OnPostClaimLeaseAsync` | `:156` | `id`, `expectedVersion`, `operationKey` | `IAcquireCaseEditLease.ExecuteAsync(new ClaimCaseEditLeaseRequest(...))` |
| `OnPostRenewLeaseAsync` | `:203` | `id`, `expectedVersion`, `operationKey`, `editLeaseToken` | `IRenewCaseEditLease` |
| `OnPostReleaseLeaseAsync` | `:250` | `id`, `operationKey`, `editLeaseToken` | `IReleaseCaseEditLease` |
| `OnPostConfirmCompletenessAsync` | `:293` | `id`, `expectedVersion`, `operationKey`, `reason`, `editLeaseToken`, **four booleans** | `IConfirmCompleteness` |
| `OnPostSaveAsync` | `:324` | `id`, `expectedVersion`, `operationKey`, `reason`, `editLeaseToken`, **eighteen editable fields** | `ISaveCase` |

Every one of them ends in a PRG redirect back to the workspace
(`CaseMutationPageModel.RedirectToDetails`, `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:176-177`)
and communicates through `TempData["CaseStatus"]` / `TempData["CaseError"]`.

Parity matrix row: **`PAR-08`** at
`docs/desktop/01-inventory-and-parity/parity-matrix.md:53` — this ticket owns
its **edit handlers**; the read path is [[FEAT-003]] (plan handle `DSK-05-03`).
The matrix holds 46 `PAR-` rows (`grep -c '^| PAR-' …` → `46`), all keyed to
page models under `src/Pegasus.Web/Pages/**`.

## Findings

### Facts

Verified at `HEAD` `bbd1c549` (2026-08-24). `git diff --stat 191ddf33..HEAD -- src tests`
is empty, so the plan set's line references still hold. **`bbd1c549` is the
revision characterized.**

#### Business behaviour — carried over

- **Every case mutation must present five things, and Core enforces all five in
  one place.** `CaseLifecycleRules.ValidateMutation`
  (`src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:414-426`) requires:
  1. a non-empty `CaseId` and a non-negative `ExpectedVersion`
     (`ValidateCaseAndVersion`, `:583-594`);
  2. an actor authorized for `StaffAccessRight.PerformCasework`
     (`ValidateActor`, `:602-606`);
  3. an `OperationKey`, trimmed, control-character-free, **≤ 100 characters**
     (`ValidateActorAndOperation`, `:596-600`);
  4. a **`Reason`, required, ≤ 500 characters** (`:420`);
  5. an `EditLeaseToken` of **exactly 64 characters**
     (`CaseEditAuthority.LeaseTokenLength`, `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:18`;
     enforced at `CaseLifecycle.cs:421-425`).
  - Consequence: there is **no reason-free save**. A desktop Save that does not
    collect a reason cannot succeed, and the ticket's "deliberate Save" is
    therefore also a reasoned save. That is not obvious from the ticket body and
    is the single most important thing this research adds.
- **The lease lasts five minutes and the desktop cannot know that.**
  `EditLeaseDuration = TimeSpan.FromMinutes(5)`
  (`src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs:20`, applied at
  `:173` and `:254`). It lives in **Infrastructure**, which the desktop must
  never reference (reuse-map boundary note). The renew timer must therefore be
  driven by `CaseEditLease.ExpiresAtUtc`
  (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:118-123`), which the
  claim and renew responses carry — never by a hard-coded five minutes.
- **Replay semantics are documented on the port and are exact.**
  `ILeaseCaseForEdit` (`CaseWorkflowContracts.cs:323-336`): "An exact claim or
  renewal replay returns the same opaque token and expiry, and an exact release
  replay returns success, **before** mutable-state, version, ownership, or
  expiry preconditions are evaluated. Reusing an operation key with different
  request material fails with `CaseOperationConflictException`. **Actor
  authorization always precedes replay recovery.**"
- **The four refusal exceptions are distinct types.**
  `CaseVersionConflictException` (`CaseWorkflowContracts.cs:125-132`) carries
  `CaseId`, `ExpectedVersion` and **`ActualVersion`**;
  `CaseEditLeaseConflictException` (`:134-140`) carries `CaseId` and
  `CaseVersion`; `CaseEditLeaseExpiredException` (`:142-148`) the same;
  `CaseOperationConflictException` (`:150-157`) carries `CaseId` and
  `OperationKey`.
- **The refusal order is business policy and it lives in Core.**
  `CaseEditAuthority.RequireLease` (`CaseEditAuthority.cs:38-66`) throws
  `CaseEditLeaseExpiredException` when the token is absent, the expiry has
  passed, the retained hash cannot be read or the holder is unknown; and
  `CaseEditLeaseConflictException` when the holder is someone else or the token
  does not match. `RequireVersion` (`:27-33`) throws
  `CaseVersionConflictException`. The class summary (`:5-11`) states the
  invariant: "A missing, expired, wrong-holder, or stale-version mutation is
  refused without overwriting newer work, and **there is no takeover, force, or
  bypass**."
- **Completeness has its own precondition, separate from the policy
  evaluation.** `CaseDataPolicy.ValidateCompleteness`
  (`src/Pegasus.Core/Cases/CaseDataOperations.cs:105-119`) refuses
  "Instructions cannot be confirmed while instruction evidence is incomplete."
  and the image equivalent. The *policy* evaluation is separate:
  `ConfirmCompleteness.ExecuteAsync` (`CaseDataOperations.cs:15-31`) reads the
  current `ICaseWorkflowConfiguration` and calls
  `CaseCompletenessPolicy.Evaluate`, whose four configuration switches are
  `RequireCompleteInstructionsBeforeEngineerAssignment`,
  `RequireCompleteImagesBeforeEngineerAssignment`,
  `RequireStaffInstructionReviewBeforeEngineerAssignment`,
  `RequireStaffImageReviewBeforeEngineerAssignment`
  (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:41-46`, evaluated at
  `CaseDataOperations.cs:79-88`). So a confirmation can be *accepted* and still
  not satisfy the policy — the projection carries both
  (`CaseCompletenessProjection`, `src/Pegasus.Core/Cases/CaseDataContracts.cs:105-107`).
- **Save normalization is thorough and belongs to Core, so the desktop can run
  the same rules locally.** `CaseDataPolicy.Normalize`
  (`CaseDataOperations.cs:121-161`) enforces: mileage ≥ 0; a defined
  `CaseInspectionMode`; no `DateOnly.MinValue`; whitespace collapse plus length
  caps — claimant 300, claim number 100, make 100, model 100, mileage unit 40,
  circumstances 2 000, contact name 300, e-mail 320, phone 100, VAT status 100,
  inspection address 1 000; registration compacted to 20 characters,
  upper-cased, letters and digits only (`:191-205`).
  `ValidateInspection` (`:163-190`) adds three cross-field rules: address and
  mode must be saved **together**; `ImageBasedAssessment` requires the exact
  `Ext18InspectionAddressPolicy.ImageBasedAssessment` value; and that value
  cannot be saved as a physical address.
- **Eighteen editable fields, and they are the same eighteen in three places.**
  `OnPostSaveAsync`'s parameter list (`Details.cshtml.cs:324-347`),
  `CaseEditableData` (`src/Pegasus.Core/Cases/CaseDataContracts.cs:125-143`) and
  the first eighteen entries of `RetainableFormFields`
  (`CaseMutationPageModel.cs:48-67`) agree.
- **A note takes no lease and no version, deliberately.**
  `TasksModel.OnPostAddNoteAsync`'s remarks
  (`src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:28-32`): "A note takes no edit
  lease and no expected version: it adds to the case's record rather than
  changing the case, so it must not contend with an engineer editing the same
  case (CASE-017)." That command belongs to [[FEAT-006]] (plan handle
  `DSK-05-06`), but the fact bounds what "editing requires a lease" means.

#### Web mechanics — not carried over

- **Cookie `TempData` retention of proposed values.**
  `CaseMutationPageModel.RetainProposedValues` (`:195-244`) serializes the
  refused form's own fields, bounded by `MaximumRetainedProposedCharacters = 8000`
  (`:38`) and `MaximumRetainedProposedValueCharacters = 2000` (`:39`), filtered
  through the 41-name `RetainableFormFields` allow-list (`:46-88`), and reports
  rather than silently discards an oversized payload (`:236-242`).
- **The reacquisition rule expressed as TempData clearing.**
  `IsLeaseLoss` (`:303-304`) is `CaseEditLeaseExpiredException or CaseEditLeaseConflictException`;
  `RequiresReacquisition` (`:313-314`) adds `CaseVersionConflictException`. Its
  remarks (`:306-312`) state the business rule underneath: the rejected editor
  must "reload and reacquire rather than merge or force the save", and clearing
  the page's lease state "does not release the server-owned authority". The
  **rule** travels; the TempData clearing does not.
- **Operation-key persistence across the redirect.**
  `GetOrCreateClaimLeaseOperation` (`Details.cshtml.cs:426-441`) and
  `GetOrCreateOperationKey` (`:442-453`) keep a stable key in `TempData` so a
  resubmitted claim replays. The desktop keeps the key in the view model instead.
- **`CanRecoverLease`** (`:88-98`, set at `:421-422`): the server says the actor
  holds the lease but this browser lost the token, so the page offers recovery.
  The desktop's in-memory `CaseEditSession` cannot get into that state within a
  session — but it can across a restart, so the state is still meaningful and is
  [[FEAT-008]]'s (plan handle `DSK-05-08`) to render.
- **PRG redirects and `TempData["CaseStatus"]` / `["CaseError"]`**
  (`CaseMutationPageModel.cs:141-174`). Antiforgery is replaced by bearer tokens
  (area 04).
- Confirmed by the reuse-map: `Pages/Cases/CaseMutationPageModel.cs (339)` →
  **REPLACE (web keeps it until cutover)**, target `DSK-05-24` — that is
  [[FEAT-024]].

#### Other

- Existing test evidence, located by `ls tests/Pegasus.IntegrationTests` and
  `ls tests/Pegasus.Core.Tests/Workflow`:
  `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs` (2,194 lines),
  `CaseDetailsWebTests.cs` (1,286), `CaseEditModeWebTests.cs` (126),
  `ConcurrencyTokenPersistenceTests.cs` (271),
  `CaseDataCompletenessPersistenceTests.cs`,
  `tests/Pegasus.Core.Tests/Workflow/CaseEditAuthorityTests.cs`,
  `tests/Pegasus.Core.Tests/Cases/CaseDataOperationsTests.cs`.
  `CaseEditModeWebTests.cs` and `ConcurrencyTokenPersistenceTests.cs` are more
  precise than the plan set's citation and are the primary oracles.
- **Target projects do not exist yet.** `Pegasus.slnx` lists four production and
  three test projects. `grep -rn "DesktopGateway" src/ tests/` returns nothing —
  the gate is introduced by [[GWY-002]] (plan handle `DSK-03-02`).
- **`lease` is a banned operator word** (`docs/design/README.md:412-420`). The
  settled operator vocabulary for this concept is "edit mode" — the web already
  uses it: "Edit mode is active until …" (`Details.cshtml.cs:180`), "Edit mode
  was renewed until …" (`:222`), "Edit mode was left safely." (`:264`), and
  three matching refusal sentences (`:196-197`, `:240-241`, `:280`).

### Assumptions

- **`A-05-16` — [[GWY-008]] (plan handle `DSK-03-08`) will surface all five Core
  fields on the wire and return a 409 carrying the current version.**
  `endpoint-map.md:54-56` names `expectedVersion`, `editLeaseToken` and
  `operationKey`, and the `Cases` header row promises "conflicts are 409
  problems carrying the current version"
  (`vertical-slices.md` § Common to every slice). `CaseVersionConflictException`
  carries `ActualVersion` (`CaseWorkflowContracts.cs:129`), so the value exists.
  Confirmed by: step 3. Breaks if wrong: the desktop cannot show the current
  version and [[FEAT-008]]'s comparison has nothing to compare against.
- **`A-05-17` — the lease-conflict 409 will carry the holder's display name, not
  the subject id.** `IDescribeCaseEditAuthorityHolder`
  (`CaseEditAuthority.cs:83-90`) resolves it and
  `CaseEditAuthorityHolder` (`:75-81`) is the shape; the exception itself
  (`CaseEditLeaseConflictException`) carries only `CaseId` and `CaseVersion`.
  Confirmed by: step 3. Breaks if wrong: the desktop either renders an
  identifier — which `CaseEditAuthority.cs:68-73` forbids — or cannot name the
  holder at all.
- **`A-05-18` — the renewal window is derived from `ExpiresAtUtc` and not
  published as a duration.** `EditLeaseDuration` is an Infrastructure constant
  (`EfCaseWorkflowStore.cs:20`). Confirmed by: reading the claim response DTO.
  Breaks if wrong (the duration is published instead): the desktop would hold a
  copy of an Infrastructure constant, which drifts silently when the constant
  changes.
- **`A-05-19` — the completeness precondition and the policy evaluation both
  reach the desktop.** `CaseCompletenessProjection`
  (`CaseDataContracts.cs:105-107`) carries `Values` and `Evaluation`; the
  operator needs to know both "the confirmation was accepted" and "it does not
  yet satisfy the policy". Confirmed by: step 3. Breaks if wrong: the desktop
  reports a successful confirmation on a case that is still Not ready and the
  operator cannot see why.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered for the case-edit responsibility.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | This is the definitional case. The lease exists because several operators edit one record; `CaseEditAuthority`'s summary (`CaseEditAuthority.cs:5-11`) says the guard exists so a mutation is "refused without overwriting newer work". Lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **No** | An operator edits when present. Nothing schedules a save. The Automation Actor can also hold the lease (`CaseEditAuthorityHolder.Automation`, `:79-80`), and it runs in the gateway/Worker already — nothing moves. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No, and one near-miss.** | No provider secret is involved. But the **lease token is a secret-shaped value**: 64 hex characters compared in fixed time against a retained hash (`CaseEditAuthority.cs:34-37`). It is short-lived (five minutes) and scoped to one case, so it is not a "long-lived secret" — but it is held in memory only, never written to disk or a log, which is why the ticket's step 4 says so explicitly. |
| Public callback — must an external service call a stable public endpoint? | **No** | No external party edits a case. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | `CaseLifecycleRules.ValidateMutation` (`CaseLifecycle.cs:414-426`) and `CaseEditAuthority.RequireLease` (`CaseEditAuthority.cs:38-66`) are the enforcement, and "there is no takeover, force, or bypass". `ICaseWorkflowStore`'s summary (`CaseWorkflowContracts.cs:338-342`) makes the version check, the lease check, the change, the idempotency record and the permanent action history **one atomic transaction**. None of it can be trusted to a client. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **n/a** | No measurement exists either way and none is needed: questions 1 and 5 already place the write. Field-level validation runs on the desktop through the same `CaseDataPolicy.Normalize` rules and is re-checked by the store inside the transaction. |

**Placement:** the gateway owns the lease, the version, the atomic transaction
and the audit; the desktop owns the dirty state, the renew timer, the navigation
guard and the field-level validation. Two "yes" answers, both naming the
gateway. No Azure resource is involved and no Azure write occurs.

## Implications

- **Save is reasoned, not merely deliberate.** `ValidateMutation` requires a
  non-empty `Reason` ≤ 500 characters on *every* mutation
  (`CaseLifecycle.cs:420`). The desktop's Save must therefore collect a reason —
  through the `ReasonDialog` contract, the same control the completeness
  confirmation uses. A Save button that posts without one produces an
  `ArgumentException`, not a validation message.
- **The renew timer reads `ExpiresAtUtc`, never a constant.**
  `EditLeaseDuration` is in Infrastructure (`EfCaseWorkflowStore.cs:20`) and the
  desktop cannot reference it. Renew at a fraction of the remaining window from
  the claim/renew response, and treat a failed renew as `LeaseLost`.
- **Version conflict and lease loss are different, and both mean "reacquire".**
  `RequiresReacquisition` (`CaseMutationPageModel.cs:313-314`) unions
  `IsLeaseLoss` with `CaseVersionConflictException`. The desktop must therefore
  clear its held token on a stale-version refusal too — not just on a lease
  failure — because the operator must reload and reacquire, not resubmit. That
  is a business rule, and the remarks at `:306-312` say so.
- **Operation keys: one per user-initiated attempt, reused on transport retry.**
  `ILeaseCaseForEdit`'s contract (`CaseWorkflowContracts.cs:323-336`) makes an
  exact replay return the same token and expiry *before* preconditions are
  evaluated — so a retried claim is safe. Reusing a key with *different* material
  fails with `CaseOperationConflictException`, so the key must not be reused
  across a changed payload.
- **Local validation can be genuinely thorough.** `CaseDataPolicy.Normalize` is
  pure, deterministic and in `Pegasus.Core` (`CaseDataOperations.cs:121-205`), so
  the desktop can run all eighteen field rules and all three cross-field
  inspection rules immediately and get exactly the server's answer. This is the
  clearest case on the board for the reuse-map's boundary note.
- **The inspection-address pair is the trap in local validation.** Address and
  mode must be saved together, and `ImageBasedAssessment` requires the exact
  sentinel string (`:163-190`). A form that lets an operator clear the address
  while leaving the mode set produces a refusal the operator cannot interpret;
  bind them as one control group.
- **Completeness needs two pieces of feedback, not one.** The confirmation may be
  accepted while `SatisfiesPolicy` is false
  (`CaseCompletenessProjection`, `CaseDataContracts.cs:100-107`). Render both, or
  the operator confirms and nothing appears to happen.
- **`lease` must not reach the screen.** Use the vocabulary the web already
  uses: "Edit mode is active until …", "Edit mode was renewed until …", "Edit
  mode was left safely.", and for refusals the three existing sentences at
  `Details.cshtml.cs:196-197`, `:240-241`, `:280`. They are settled operator copy
  and reusing them is cheaper and safer than writing new sentences.
- **Do not render the token.** 64 hex characters compared against a retained
  hash; the ticket's acceptance criterion bans it from the UI **and** from logs.

## Open questions

None that block the plan. `A-05-16` through `A-05-19` are settled by step 3's
reading of [[GWY-008]] (plan handle `DSK-03-08`)'s delivered contract, and each
has a named consequence in the plan's *Risks / open questions* section. The
upstream dependency named in the ticket's Guardrails — upstream `CASE-021`
(refuse Review for a case with no images), which **is** imported as board
[[CASE-001]] per the `HZN-001` group document's join table — is a named board
ticket and therefore a scope boundary recorded in the plan, not an open
question. No `open-questions` document is created; the ticket body does not ask
for one.
