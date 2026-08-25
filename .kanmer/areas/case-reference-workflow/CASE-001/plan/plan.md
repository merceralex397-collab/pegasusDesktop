# Plan

## The defect in one sentence

Export counts photographs; Review asserts a constant.

`EfCaseAcceptanceStore.cs:262` picks `Review` or `NotReady` from
`CompletenessEvaluation.SatisfiesPolicy`, which — with
`RequireCompleteImagesBeforeEngineerAssignment` seeded `true` — reduces to
`completeness.ImagesComplete`. That value is the hardcoded `true` at
`IntakeAllocation.cs:226`. **The Review gate never looks at an image.** The EVA
export refuses the same case at `EvaHandoffStore.cs:717` because it counts
custody-confirmed `DocumentSemanticRole.Image` occurrences and finds none.

The operator's framing is exactly right, and `operator-notes.md` settles which
side wins:

> *"**Not ready** means something is missing, almost always images or
> instructions. **Ready** means ready to enter into EVA but not yet entered."*

Review *is* "ready to enter into EVA". If the bundle refuses, the case is Not
ready. FRD-01 says the same normatively.

## Verified before planning

- **Production config is not the cause.** Read-only query against
  `WorkflowConfigurations`: `RequireCompleteInstructionsBeforeEngineerAssignment`
  and `RequireCompleteImagesBeforeEngineerAssignment` are both `True`. The fault
  is code, and this fix will bite.
- **`ImagesComplete` is observable at allocation.** `AttemptAutomaticAsync`
  already holds the receipt, and `IntakeReceipt.AssetRecords` is the retained
  asset list. No extra load, no new query.
- **The genuine-photograph rule already exists**, from INTK-030:
  `InstructionEvidenceImages.Select` — attachments must be `image/*`, embedded
  images must clear the 40 KB floor, both must pass `IsPhotographShaped` (side
  ratio < 3.0, which is what separates a letterhead banner from a photograph),
  inline signature graphics never qualify, duplicates collapse by hash.

## The change

Delete the constant. Build the record at the call site:

```csharp
new CaseCompleteness(
    InstructionComplete: true,
    ImagesComplete: InstructionEvidenceImages.Select(receipt.AssetRecords).Count > 0,
    InstructionConfirmedByStaff: false,
    ImagesConfirmedByStaff: false)
```

`InstructionComplete: true` **stays**. The receipt reached
`IntakeDecision.CaseCreated` — a definitive authorised instruction. That is a
real observation, not an assertion.

## Why not derive at Review time instead

Because Review is not a transition an automatic case *attempts*.
`EfCaseAcceptanceStore.cs:262` decides it as the case is created, before custody
has run and before a single `DocumentOccurrence` exists. There is nothing to
derive from at that instant except the receipt's own assets. Deferring the
decision into custody promotion would be a much larger change and is not needed:
custody promotion is already the path for material that arrives later.

## Regression risk — not reintroducing CASE-013

CASE-013's failure was structural, not a value: `IsReadyForReview` had no callers,
and two layers each carried a stricter copy of the rule demanding staff
confirmation nobody would ever give. Its fix was the automatic-definitive waiver
in `CaseCompletenessPolicy`, the actor test in `AcceptIntake`, and giving Core's
rule a caller. **This change touches none of that.**

CASE-013 in fact already decided this boundary, and its own test pins it —
`TheWaiverCoversStaffReviewOnlyAndNotMissingEvidence` passes all four flags false
with `automaticallyDefinitive: true` and asserts the policy is *not* satisfied. So
CASE-013 established that false evidence flags must block an automatic case. It
simply never supplied an honest one. This does.

Three guarantees:

1. The four existing regression tests construct `CaseCompleteness` directly and
   call the policy — they never touch `AllocateIntake`. The constant was untested
   input to a tested rule.
2. A forwarded instruction carrying photographs still evaluates `true`, still
   satisfies the policy under the waiver, still reaches Review with no staff
   action. That is the exact case CASE-013 existed to unstick.
3. No case is stranded. An image-free case sits in `NotReady` with its seven-day
   chase already scheduled by `EfCaseAcceptanceStore.cs:311-321`, and staff have
   two existing routes out — `ValidateReviewReadiness` is an **OR**, so confirming
   both readiness boxes drives a legitimately image-free case to Review.

## Is any legitimate case image-free?

Creatable without images: yes — an Audit arrives as instruction plus original
report, and `a.QDOS26013` is that shape. Deliverable without them: no. The
operator answered it directly: *"Lacking images should keep the case in 'Not
Ready' … Images are an EVA requirement / Report Requirement."* The rule is
absolute at the gate, with the existing staff escape above. **No operator question
is open.**

## Verification

- `dotnet build --configuration Release`
- `dotnet test tests/Pegasus.Core.Tests`
- `dotnet test tests/Pegasus.IntegrationTests --filter "Category!=Corpus"` (CI's
  three shards on the exact SHA are the authority)
- Simplification pass over the branch diff before the PR, recorded here.

## Known residual, not fixed here

The due-work reason is the flat string `"Details are incomplete"`, so the operator
sees a Not-ready case without being told it is the images. FRD-01 dislikes exactly
that opaque aggregate, and CASE-013 deferred naming the missing evidence as
"worth doing once the flags mean something". This ticket is what makes them mean
something. Follow-up, not scope creep here.

## Simplification pass — 2026-08-24

Run over the branch's own diff with an independent lens before the PR.

**Applied:**

- **The tests proved nothing they appeared to prove.** They called
  `AutomaticCompleteness(...)` and then *separately* called
  `CaseCompletenessPolicy.Evaluate(...)`, re-implementing by hand the very wiring
  the diff changed. Nothing exercised `AllocateIntake` feeding the value into the
  acceptance command. They now live in `AllocateDefinitiveIntakeTests`, drive
  `AttemptAutomaticAsync`, and assert on the completeness `RecordingAcceptance`
  actually received.
- **A receipt builder was duplicated.** `AutomaticCaseReadinessTests` was a pure
  policy class; the diff added a 22-argument `IntakeReceipt` builder to it while
  a near-identical one already existed one directory over. The move above reuses
  that builder (extended with `params IntakeAssetRecord[] assets`) and leaves the
  readiness class about the policy, as it was.
- **`internal` is no longer needed.** Driving the real path made the visibility
  widening unnecessary; `AutomaticCompleteness` is `private` again.
- **A 22-line doc comment for 5 lines of code.** The paragraph defending against
  a return to CASE-013's all-false flags was arguing with a change nobody
  proposed — ticket archaeology, which belongs here rather than in the tree. Cut,
  along with an inline comment restating what `InstructionEvidenceImages`' own
  class doc already says.

**Considered and deliberately not changed, with reasons:**

- **`InstructionEvidenceImages.Select(...).Count > 0` allocates an array to test
  emptiness.** There is no `Any`-style predicate on that type, and adding one for
  a single caller is an abstraction with no second concrete caller — forbidden
  outright. One small array, once per allocation, and reusing the one selection
  owner is the whole point of the change.
- **Ordering was checked, not assumed.** The receipt at the call site is a fresh
  `receiptQueries.GetAsync` with assets eagerly included, and asset retention
  commits in the same unit of work as the receipt row, strictly before allocation
  runs. The flag cannot be spuriously false from staleness.

## Known consequences, pinned rather than discovered later

Three real intake shapes now evaluate `ImagesComplete: false` where they
previously sailed through. All three are the intended behaviour — the flag is
finally telling the truth — but they are consequences, not accidents:

1. **Photographs embedded in the message body** rather than attached.
   `InstructionEvidenceImages` counts attachments and embedded images, never
   inline ones. Now pinned by a test.
2. **Embedded PDF images under the 40 KB floor.**
3. **Photographs arriving on a later receipt** — the grouped image-intake path
   runs *after* allocation, and nothing recomputes the flag afterwards. So an
   instruction whose photographs follow separately is born images-incomplete
   until staff confirm.

None strands a case: each sits in Not ready with its seven-day chase, and the
review-readiness rule accepts staff confirmation in place of complete evidence.
Shape 3 is the one most likely to be seen in practice and is worth an operator
sentence if it proves annoying — a follow-up, not scope creep here.

## Verify-after-sync decision — 2026-08-25

A live read-only check of `https://github.com/collisionengineers/pegasus.git` returned `dev` and `main` at `7afd18037acfa78927c4b4ffdf8e0f74c7ecc688`; no `task/case-021-observed-images` ref was returned. The fork worktree HEAD is `5770eb21`. In this fork, `src/Pegasus.Core/Intake/IntakeAllocation.cs` still contains the `AutomaticCompleteness` constant with `ImagesComplete: true` at lines 224–228, and the call site passes it at line 269. Therefore the upstream fix has **NOT ARRIVED** and this ticket runs the full fix (answer b), not verification-only. Operator confirmation was not supplied; this live remote check is the recorded evidence, and no upstream or Azure write was performed.

## Implementation checkpoint — 2026-08-25

Implemented the answer-(b) fix in `src/Pegasus.Core/Intake/IntakeAllocation.cs`: the automatic command keeps `InstructionComplete: true` from the definitive `CaseCreated` decision and observes image completeness through `InstructionEvidenceImages.Select(receipt.AssetRecords)`. Added real-path Core tests in `AllocateDefinitiveIntakeTests` for no photographs, attached photographs, inline body images, under-floor embedded images, and letterhead banners. Added the LocalDB end-to-end `AutomaticAllocationWithoutPhotographsPersistsNotReadyWithScheduledChase` fact and extended only the existing `AllocationTestData` receipt builder with optional assets.

Validation completed: focused Core 12/12; full Core 921/921; focused LocalDB integration 1/1. Existing consequence coverage confirmed by `InstructionEvidenceImagesTests.SelectsAttachedImagesAndLargeEmbeddedImagesOnly`, `TheThresholdIsABoundaryNotAGuess`, `QdosTwentySixZeroZeroEightsLetterheadBannersAreNotEvidence`, and grouped-intake concurrency recovery facts. No Azure/cloud writes.

## Simplification pass — 2026-08-25

Applied an equivalent independent four-lens pass over `git diff origin/dev...HEAD`:

- **Reuse:** retained `InstructionEvidenceImages.Select` as the single production owner; reused the existing `RecordingAcceptance`, receipt test shape, `AllocationTestData` helper, workflow query, and established image-selection tests.
- **Simplification:** removed the asserted `AutomaticCompleteness` constant rather than wrapping it; the new behavior is one record at the existing call site. The only test helper additions are the focused asset factory and an optional `assets` argument on the existing integration receipt builder.
- **Efficiency:** no new query, cache, abstraction, or duplicate predicate was introduced; selection operates on the already-loaded receipt asset records.
- **Altitude:** the production comment now states the observed instruction/image boundary; tests drive `AttemptAutomaticAsync` and the persisted acceptance path rather than constructing the completeness record by hand.

No behavior-preserving simplification findings remain unapplied. The initial full integration run found a correctness fixture mismatch, not a simplification finding; it was fixed by making the seeded pending command match the now-observed no-image receipt and then revalidated.

## PR handoff blocker — 2026-08-25

The branch `case-001-observed-images` is pushed at commit `29c1b83b030f402c349576e6fc4f7e1ab1184430` and the required post-implementation report is written. `gh pr create --base dev --head case-001-observed-images` failed with the exact response `pull request create failed: GraphQL: must be a collaborator (createPullRequest)`. No PR, CI, merge, or proof claim is made. The next action is repository collaborator permission or an authorized PR workflow path.

## Independent review disposition — 2026-08-25

Hilbert's independent review of commit `29c1b83b030f402c349576e6fc4f7e1ab1184430` found one blocker and one warning. The cited grouped-image test covered image-intake reconciliation but did not allocate an image-free instruction case, process a later image receipt, and assert the case completeness state. The production comment also inaccurately described the former four-field value as all false.

Applied the bounded corrections:

- Added `QdosAllocationRecoveryTests.PhotographsArrivingAfterAllocationDoNotRewriteAllocationCompleteness`. It allocates a definitive instruction with no retained photographs, asserts `NotReady` and `ImagesComplete=false`, processes a later photograph through the Worker-shaped upload/automation path, asserts automatic image registration and association to the case, then asserts the case remains `NotReady`, image-incomplete, and staff-unconfirmed.
- Corrected the `IntakeAllocation` comment to state that the former route asserted image completeness and waived staff confirmation; it no longer claims all four fields were false.

Revalidation: focused later-receipt integration fact 1/1; Release solution build 0 warnings/errors; full Core 921/921; Architecture 99/99; full non-corpus/non-browser integration 873 passed, 3 skipped, 876 total; `git diff --check` passed. The test reuses the existing `AllocationTestData`, `IntakeWebDriver`, image automation, and case data query; no production policy, abstraction, or unrelated file was added.
