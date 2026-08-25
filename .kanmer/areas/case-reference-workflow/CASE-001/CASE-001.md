---
id: CASE-001
type: ticket
title: >-
  upstream:CASE-021 · Refuse Review for a case with no images instead of
  asserting its images are complete
status: implementing
area: case-reference-workflow
assignee: codex-mcp-client
profile: fix
stageEntered:
  preparing: '2026-08-24T21:23:44.361Z'
  review: '2026-08-25T06:06:37.232Z'
  implementing: '2026-08-25T06:17:42.877Z'
taken_at: '2026-08-25T05:33:44.181Z'
branch: case-001-observed-images
worktree: .worktrees/case-001
labels:
  - qdos26013
  - production-defect
  - found-during-qa
  - readiness
  - upstream-carryover
  - upstream-CASE-021
  - gateway-worker-ticket
  - needs-operator
groups:
  - EPIC-014
links: []
blocks:
  - FEAT-005
  - GWY-006
  - FEAT-001
  - DUI-006
refs:
  - docs/frd/frd-01-case-identity-and-lifecycle.md
docs_todo: true
commits:
  - 29c1b83b030f402c349576e6fc4f7e1ab1184430
archived: false
created: '2026-08-24T11:42:25.781Z'
updated: '2026-08-25T06:17:42.877Z'
---

## What

Replace the hard-coded `AutomaticCompleteness` constant in `src/Pegasus.Core/Intake/IntakeAllocation.cs:224-228` with an `ImagesComplete` value **observed** from the receipt's own retained assets, so an automatically created case that carries no photographs is born `NotReady` with its seven-day chase scheduled rather than `Review`.

Scoped **verify-after-sync**. The fix is live upstream on branch `task/case-021-observed-images` with a complete plan, so this ticket's first job is to establish whether the upstream sync brought it; it carries the full change only if it did not.

## Why

The desktop conversion inherits this defect whole and no seeded conversion ticket is permitted to fix it.

`AllocateIntake.AutomaticCompleteness` asserts `ImagesComplete: true` for every automatically created case, whatever arrived. `EfCaseAcceptanceStore.cs:262` picks `Review` or `NotReady` straight from `request.CompletenessEvaluation.SatisfiesPolicy`, which — with `RequireCompleteImagesBeforeEngineerAssignment` seeded `true` — reduces to that constant. The Review gate never looks at an image. The EVA export disagrees: `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs:80-99` counts custody-confirmed `DocumentSemanticRole.Image` occurrences and finds none, so the same case is simultaneously "ready to enter into EVA" and refused by EVA.

The conversion makes the lie more visible, not less. Every native readiness surface reads the same flag:

- [[DSK-05-01]] renders the queues and work counts, so an image-free case appears in the operator's Review queue as work that is ready.
- [[DSK-03-06]] computes the rail figures the shell shows, and its acceptance is parity with the Razor sources — so it would faithfully reproduce a wrong number.
- [[DSK-06-06]] renders the status chip, giving the wrong state a confident label in the settled vocabulary.
- [[DSK-05-05]] already names this defect in its Traps — *"upstream CASE-021 (refuse Review for a case with no images) is a gateway rule that must be true before this row reaches parity"* — which makes it a precondition of that slice, not work that slice owns.

No seeded ticket may make the fix. `src/Pegasus.Core/Intake/IntakeAllocation.cs` is the file that must change, and [[DSK-03-10]]'s scope boundary states *"Must not touch `src/Pegasus.Core/Intake/**`"*; [[DSK-05-09]] may touch `src/Pegasus.Core/Intake/` *"only for rules moved in with a characterization test"*, which is a lift-and-shift permission, not a licence to change a shipped rule; [[DSK-03-06]] and [[DSK-05-01]] may not touch Core at all. `docs/desktop/06-ui-design/screen-specs.md:230-231` lists CASE-021 among the "Upstream carry-over absorbed" ids, which is a plan defect — a screen specification cannot deliver a Core constant. That correction is owned by [[DSK-03-07]], coordinated with [[DSK-06-13]].

Under **D-001** the fork becomes the single release source and upstream is frozen, so an upstream fix that has not merged by the freeze does not arrive late — it vanishes. That is why this row exists on the fork board even though it is `implementing` upstream today.

## Source of truth

- **Upstream provenance** — ticket `CASE-021`, upstream area `case-reference-workflow`, upstream status `implementing` (assignee `claude-code`, taken 2026-08-24T08:53:04Z, branch `task/case-021-observed-images`, worktree `../pegasus-worktrees/case-021-observed-images`), upstream profile `fix`, upstream labels `qdos26013`, `production-defect`, `found-during-qa`, `readiness`. Read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111` on **2026-08-24**. Upstream carries `docs_todo: true`, `links: []`, `deployment: not-deployed`.
- **Upstream pipeline documents copied verbatim onto this ticket**: `files`, `plan`. Upstream has no `research`, `checklist` or `open-questions` document, so none was invented. The upstream plan already records a dated `## Simplification pass` and a `## Known consequences` section — both are part of the copied document and are the authority on what the branch actually did.
- **Triage row**: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:110` — disposition `gateway-worker-ticket`, plan area `03 (Core lifecycle rule)`, fork area `case-reference-workflow`.
- **Import decision**: `FND-022` (`DSK-01-09`) step 3 — *"`case-reference-workflow` (2): `CASE-021` (`fix`, scope it verify-after-sync — it is at `implementing` upstream on `task/case-021-observed-images`)"* and step 12 — *"porting it fresh would duplicate and conflict"*.
- **Repository evidence (this fork, read 2026-08-24)**:
  - `src/Pegasus.Core/Intake/IntakeAllocation.cs:224-228` — the `private static readonly CaseCompleteness AutomaticCompleteness` constant, still present, still `ImagesComplete: true`.
  - `src/Pegasus.Core/Intake/IntakeAllocation.cs:269` — the single call site, inside `AttemptAutomaticAsync`, building `IntakeAllocationCommand` from the constant. `receipt` at that point is a fresh `receiptQueries.GetAsync` with assets included.
  - `src/Pegasus.Core/Intake/IntakeContracts.cs:402` and `:465` — `IReadOnlyList<IntakeAssetRecord> AssetRecords => Assets ?? [];`, the retained asset list the observation reads.
  - `src/Pegasus.Core/Intake/InstructionEvidenceImages.cs:41` — `public static IReadOnlyList<IntakeAssetRecord> Select(IEnumerable<IntakeAssetRecord> assets)`, with `EmbeddedPhotographMinimumBytes = 40_000` (`:22`) and `MaximumPhotographSideRatio = 3.0` (`:39`). Its class doc already states it is "the one owner of which of a receipt's retained assets count as the instruction's evidence photographs".
  - `src/Pegasus.Infrastructure/Persistence/EfCaseAcceptanceStore.cs:262` — `var initialState = request.CompletenessEvaluation.SatisfiesPolicy ? CaseInitialState.Review : CaseInitialState.NotReady;`
  - `src/Pegasus.Infrastructure/Persistence/EfCaseAcceptanceStore.cs:305-321` — the `NotReady` branch adding `CaseDueWork` with `MissingMaterialReason = "Details are incomplete"` and `NextChaseAtUtc = CaseChaseSchedule.FirstChaseAt(acceptedAtUtc)`. Nothing is stranded.
  - `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:555` and `:575` — the Engineer-assignment gate reading `ImagesComplete`, the second surface the corrected flag moves.
  - `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs:80-99` — the export's image query (custody-confirmed `DocumentSemanticRole.Image` occurrences, `image/jpeg` or `image/png`, not third-party confirmed). **Note the divergence**: the upstream body names the refusal `EvaHandoffPolicy.NoRetainedImagesReason`; that symbol does not exist in this fork at the read SHA. The behaviour it describes does — this query — so cite the query, not the symbol.
  - `src/Pegasus.Core/Eva/CaseEvaMapping.cs:207` — `if (!evidence.InstructionComplete || !evidence.ImagesComplete)` yielding "Completeness has not been confirmed."
  - `tests/Pegasus.Core.Tests/Cases/AutomaticCaseReadinessTests.cs:38` — `TheWaiverCoversStaffReviewOnlyAndNotMissingEvidence`, the CASE-013 regression guard the upstream plan relies on.
  - `tests/Pegasus.Core.Tests/Intake/AllocateDefinitiveIntakeTests.cs` — the class the upstream simplification pass moved the new facts into, and the home of the reusable `IntakeReceipt` builder.
  - `docs/desktop/06-ui-design/screen-specs.md:230-231` — the incorrect "Upstream carry-over absorbed" line naming CASE-021.
- **Governing document**: `docs/frd/frd-01-case-identity-and-lifecycle.md` (case readiness and lifecycle). `docs/operator-notes.md` carries the operator sentence the upstream plan quotes: *"Not ready means something is missing, almost always images or instructions. Ready means ready to enter into EVA but not yet entered."* A conversion ADR is still owed for the readiness surface, so `docs_todo` stays `true`.
- **Binding decisions**:
  - **L-01** — the gateway is `Pegasus.Web` evolved in place; the readiness rule stays in Core and is read by `/api/v1`, never re-implemented client-side.
  - **L-02** — Test/UAT is the local production-mimicking stack; the LocalDB integration fact is the only environment this is proven in. No Azure test resource.
  - **L-05** — this fork board is the single work register, which is why the row exists here rather than being left upstream.
  - **D-001** — the fork becomes the single release source at the first production gateway change and upstream is then frozen; an upstream fix unmerged at the freeze never arrives.
- **Depends on**: the first one-way upstream sync [[DSK-01-10]] and the standing later-sync cadence it establishes — step 2 cannot be answered before one of them has run. No other dependency.

### Upstream ticket CASE-021 (verbatim)

Copied exactly from `.kanmer/areas/case-reference-workflow/CASE-021/CASE-021.md` at clone commit `a5b28111`, read 2026-08-24. Not paraphrased, not corrected — where it disagrees with this fork's tree (the `EvaHandoffPolicy.NoRetainedImagesReason` symbol, the `IntakeAllocation.cs:225` line number) the Repository evidence above is the authority.

````markdown
## What the operator saw

`a.QDOS26013` — an audit instruction with an original report and **no
photographs** — was created straight into **Review**. Operator, 2026-08-23:

> *"This went into review despite lacking images. Review is for cases ready to
> pass to engineer. Lacking images should keep the case in 'Not Ready'. Export
> didn't work due to lacking images (this is correct, but the case shouldn't be
> in review if export doesn't work for it). Images are an EVA requirement /
> Report Requirement."*

That is exactly right, and the two halves are already consistent in Core — the
export refuses on `EvaHandoffPolicy.NoRetainedImagesReason`. Only the readiness
gate disagrees.

## Root cause

`AllocateIntake.AutomaticCompleteness` (`IntakeAllocation.cs:225`) is a **static
constant**:

```csharp
private static readonly CaseCompleteness AutomaticCompleteness =
    new(InstructionComplete: true,
        ImagesComplete: true,          // <- asserted, never observed
        InstructionConfirmedByStaff: false,
        ImagesConfirmedByStaff: false);
```

Every automatically created case is born claiming complete images, whatever
arrived. Confirmed in production — `Cases` for `a.QDOS26013`:
`ImagesComplete = True`, and `DocumentOccurrences` for that case holds exactly
three rows, none of them an `Image`:

| Ordinal | Role | File |
| ---: | --- | --- |
| 1 | OriginalSource | `…​.eml` |
| 2 | Instruction | `49378_1_LtrtoAuditEngin.pdf` |
| 3 | Instruction | `Bodyshopreport119508-V1.pdf` |

`QDOS26014`, forwarded seconds later *with* images, carries the same
`ImagesComplete = True` — so the flag distinguishes nothing.

## How it got here

The comment above the constant records it: CASE-013 changed all four fields
from `false` to `true` because every automatic case was born "details
incomplete" and could never reach Review. That fixed a real problem and
overshot — it replaced *always false* with *always true* rather than with
*observed*.

## Shape of the fix

Derive `ImagesComplete` from what the receipt actually retained, not from a
constant. `InstructionEvidenceImages.Select` is already the single Core owner
of "which assets count as this case's photographs" — custody uses it to decide
what to retain, so allocation asking the same question keeps one rule rather
than inventing a second definition of "has images".

`InstructionComplete: true` is defensible as-is: the receipt reached
`CaseCreated` only because a definitive authorised instruction was identified.
It is the *images* half that is unobserved.

## Watch for

- An audit with no photographs is a legitimate shape of work — it must still be
  **creatable**, just not **Review-ready**. The fix is a readiness gate, not a
  refusal to allocate.
- `CaseLifecycleRules` already reads `ImagesComplete` for the Engineer-assignment
  gate (`CaseLifecycle.cs:555,575`), so correcting the flag moves more than one
  surface. That is the point, but it wants checking rather than assuming.
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (server-side Core/Infrastructure change); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (the Core and LocalDB facts); `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (independent review; must not be the implementing agent).
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`) → `kanmer-execute` (`.grok/skills/kanmer-execute/SKILL.md`) → `kanmer-review` (`.grok/skills/kanmer-review/SKILL.md`) at review. No WinUI skill applies — nothing in this ticket touches XAML.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `get_ticket_doc`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`). No Azure MCP tool. No Microsoft Learn lookup is needed — every API touched is first-party repository code.
- **Kanmer pipeline** for profile `fix`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Profile `fix` needs `files`, `plan` and `questions-resolved` to leave Preparing — **both are already present**, copied verbatim from upstream, so read them with `get_ticket_doc` before writing anything. Call `get_doc_gates <this ticket id>` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5).

## Implementation steps

1. **Orient.** Read this body in full, then read the two copied upstream documents with `get_ticket_doc` — `files` (the file-by-file change list, the *Reused, not written* note and the *Deliberately not in this diff* exclusion) and `plan` (the defect statement, the verified-before-planning facts, the CASE-013 regression argument, the dated `## Simplification pass` and `## Known consequences`). Read `docs/frd/frd-01-case-identity-and-lifecycle.md` § case readiness and `docs/operator-notes.md` on Ready/Not ready. Then `get_doc_gates <this ticket id>` and `take_ticket` with branch `task/upstream-case-021-observed-images` and worktree `../pegasus-worktrees/upstream-case-021-observed-images` from `origin/dev`.
2. **Establish whether the upstream fix has already arrived — this is the verify-after-sync scope decision and it comes before any code.** In the fork tree run `grep -n "AutomaticCompleteness" src/Pegasus.Core/Intake/IntakeAllocation.cs` and `git log --oneline -15 -- src/Pegasus.Core/Intake/IntakeAllocation.cs`. Done looks like one of two recorded answers: **(a) ARRIVED** — the `private static readonly CaseCompleteness AutomaticCompleteness` constant is gone and the call site builds `ImagesComplete` from `InstructionEvidenceImages.Select(receipt.AssetRecords).Count > 0`; go to step 8 and run this ticket as verification only. **(b) NOT ARRIVED** — the constant is still at `:224-228` with `ImagesComplete: true`; carry the full change from step 4. Record the answer, the fork `HEAD` SHA and the date in the ticket plan document before proceeding.
3. **Operator step** — the sync that would deliver (a) is owned by [[DSK-01-10]] and the standing later-sync cadence, and reading the upstream remote is an upstream-repository action. Ask the operator to confirm, from the upstream board and remote: whether `task/case-021-observed-images` merged into upstream `dev`/`main`, the upstream commit SHA if it did, and whether that SHA is inside the range the fork sync has already taken. Evidence to hand back: the upstream commit SHA or an explicit "not merged", plus the date. If it merged **after** the D-001 freeze, it never arrives and answer (b) is final regardless of upstream status.
4. **Make the Core change (answer (b) only).** In `src/Pegasus.Core/Intake/IntakeAllocation.cs`, delete the `AutomaticCompleteness` constant at `:224-228` and build the record at the call site (`:269`, inside `AttemptAutomaticAsync`) exactly as the copied `plan` document specifies:
   ```csharp
   new CaseCompleteness(
       InstructionComplete: true,
       ImagesComplete: InstructionEvidenceImages.Select(receipt.AssetRecords).Count > 0,
       InstructionConfirmedByStaff: false,
       ImagesConfirmedByStaff: false)
   ```
   `InstructionComplete: true` **stays** — the receipt reached `IntakeDecision.CaseCreated`, which is a real observation. Keep the CASE-013 warning in the doc comment and say which half is now observed, per the copied `files` document. Do **not** widen `AutomaticCompleteness`' visibility and do **not** add an `Any`-style predicate to `InstructionEvidenceImages`; the upstream simplification pass rejected both, with reasons, and re-adding them would re-introduce work already removed. Done looks like: `dotnet build --configuration Release` succeeds.
5. **Prove it through the real path, not around it (answer (b) only).** Add the facts to `tests/Pegasus.Core.Tests/Intake/AllocateDefinitiveIntakeTests.cs`, driving `AttemptAutomaticAsync` and asserting on the completeness the recording acceptance actually received — no photographs → `ImagesComplete: false`; one attached photograph → `true`; a letterhead banner only → `false`. Reuse that class's existing `IntakeReceipt` builder (extend it with `params IntakeAssetRecord[] assets` if it does not already take assets) rather than adding a second builder. Leave `tests/Pegasus.Core.Tests/Cases/AutomaticCaseReadinessTests.cs` as the pure policy class it is — its four existing facts, including `TheWaiverCoversStaffReviewOnlyAndNotMissingEvidence` at `:38`, must stay green untouched.
6. **Add the end-to-end fact (answer (b) only).** In `tests/Pegasus.IntegrationTests`, assert that an automatic allocation from a receipt with no photographs lands in `NotReady` with its chase scheduled — not `Review` — because the Core tests do not reach `EfCaseAcceptanceStore`. Assert the `CaseDueWork` row exists with `NextChaseAtUtc` set (`EfCaseAcceptanceStore.cs:305-321`). Done looks like: the new fact fails against `main` and passes on this branch.
7. **Pin the three known consequences rather than discovering them later.** The copied `plan` names them: photographs embedded in the message body rather than attached; embedded PDF images under the 40 KB floor (`InstructionEvidenceImages.EmbeddedPhotographMinimumBytes`); and photographs arriving on a later receipt, because the grouped image-intake path runs after allocation and nothing recomputes the flag. Add or confirm a test for each and record in the plan document that all three are intended behaviour, each sitting in `NotReady` with its chase and the staff-confirmation route out.
8. **Re-express the operator-visible half against the desktop and the gateway — the upstream ticket assumes the Razor Queues and case pages, which the conversion retires, so this step replaces its UI reasoning without changing its requirement.** Nothing here builds a screen. Instead: (a) record in the plan document that the corrected flag flows to the desktop only through `/api/v1`, so [[DSK-03-06]]'s rail-count and dashboard endpoints and [[DSK-03-07]]'s case read endpoints must be regenerated against post-fix data before their parity tests are trusted — a parity fixture captured before this lands encodes the defect as the expected answer; (b) note for [[DSK-06-06]] that the corrected state renders through the settled status vocabulary with no new chip value, because `NotReady` already exists; (c) note for [[DSK-05-01]] that the Review queue count legitimately falls when this lands and that the fall is the fix, not a regression. Do not edit those tickets — this ticket only records the consequence.
9. **Run the verification set** in `## Verification` below and paste the output into the ticket `proof` document. `dotnet test` against `tests/Pegasus.IntegrationTests` needs LocalDB from the local Test/UAT stack (L-02); there is no Azure test environment to fall back on.
10. **Documentation.** Add the dated `## Simplification pass` entry over this branch's own diff, and record in the plan document the fork `HEAD` SHA read in step 2, the operator answer from step 3, and whether the ticket ran as (a) verification-only or (b) full fix. `docs/desktop/06-ui-design/screen-specs.md:230-231` must stop claiming CASE-021 is absorbed — **that edit belongs to [[DSK-03-07]]**, coordinated with [[DSK-06-13]] so the line changes once; raise it there rather than editing the file here.
11. **Open the PR into `dev`.** Task branch → PR into `dev` → exact-SHA promotion to `main` with the literal `MERGE AUTH GRANTED`. Never merge upstream straight into `main`.

## Acceptance criteria

- [ ] The verify-after-sync decision is recorded before any code change: the fork `HEAD` SHA, the `grep` result for `AutomaticCompleteness`, the operator's answer on whether `task/case-021-observed-images` merged upstream, and which of answer (a) or (b) this ticket ran.
- [ ] `src/Pegasus.Core/Intake/IntakeAllocation.cs` contains no constant asserting `ImagesComplete`; the value is computed at the call site from `InstructionEvidenceImages.Select(receipt.AssetRecords)`.
- [ ] `InstructionComplete: true` is unchanged and its justification (the receipt reached `IntakeDecision.CaseCreated`) is stated in the code comment.
- [ ] A receipt with no photographs produces `ImagesComplete: false`; a receipt with one attached photograph produces `true`; a receipt carrying only a letterhead banner produces `false` — each asserted by driving `AttemptAutomaticAsync`, not by constructing `CaseCompleteness` by hand.
- [ ] An automatic allocation from an image-free receipt lands in `NotReady` with its chase scheduled, proven end to end against LocalDB.
- [ ] The four existing facts in `tests/Pegasus.Core.Tests/Cases/AutomaticCaseReadinessTests.cs` are unchanged and green — CASE-013 is not re-introduced.
- [ ] The three known consequences (body-embedded photographs, sub-40 KB embedded images, photographs on a later receipt) are each pinned by a test and recorded as intended.
- [ ] No second definition of "has images" is introduced anywhere; `InstructionEvidenceImages` remains the single owner.
- [ ] The consequence for [[DSK-03-06]], [[DSK-03-07]], [[DSK-05-01]] and [[DSK-06-06]] parity fixtures is recorded in the plan document, and no seeded ticket is edited by this one.

## Verification

- [ ] `dotnet build ./Pegasus.slnx --configuration Release` — expected: build succeeds with no new warnings.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build` — expected: the new observed-images facts in `AllocateDefinitiveIntakeTests` pass and all four `AutomaticCaseReadinessTests` facts stay green.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: the new image-free-allocation fact passes and no existing acceptance fact regresses.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: green; the one-Core-owner and dependency-direction rules still hold after the change.
- [ ] `git diff --stat origin/dev...HEAD` — expected: touches only `src/Pegasus.Core/Intake/IntakeAllocation.cs` and the two test projects; no Worker, Web, Razor or Infrastructure file appears.
- [ ] Operator record in the ticket `proof` — expected: the named operator's answer and date on whether the upstream branch merged before the D-001 freeze.

## Evidence tier

Tier 2 — Core/domain. Tier 4 — LocalDB persistence.
Tier 2 obliges positive, contradictory and failure cases for the completeness and lifecycle rule: no photographs, one photograph, a letterhead banner only, and the CASE-013 waiver still behaving. Tier 4 obliges proving the initial state and the scheduled chase against a real LocalDB through `EfCaseAcceptanceStore`, because the Core tests do not reach it and the defect is only operator-visible once the case row exists.

## Documentation changes

- Ticket `plan` document — the verify-after-sync record (fork `HEAD`, operator answer, answer (a) or (b)), the dated `## Simplification pass`, and the recorded consequence for the four seeded tickets named above.
- `docs/desktop/06-ui-design/screen-specs.md:230-231` — the "Upstream carry-over absorbed" line must stop naming CASE-021. **Not changed here**: [[DSK-03-07]] owns that edit and [[DSK-06-13]] adopts the section into FRD-13; raise it there so the line changes once.
- `docs/frd/frd-01-case-identity-and-lifecycle.md` and `docs/operator-notes.md` — no change. Both already state the behaviour normatively; this makes the code match them, which is why the upstream `files` document records "No document change."

## Guardrails

- **Azure**: no write. No Azure MCP tool is called. Verification runs entirely on the local production-mimicking Test/UAT stack under **L-02** — there is no Azure dev/test/staging and asking for one is out of bounds.
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/IntakeAllocation.cs`, `tests/Pegasus.Core.Tests/Intake/AllocateDefinitiveIntakeTests.cs`, `tests/Pegasus.Core.Tests/Cases/AutomaticCaseReadinessTests.cs` (only if a fact must move, per the copied `files` document) and `tests/Pegasus.IntegrationTests`. Must **not** touch `src/Pegasus.Worker`, any file under `src/Pegasus.Web/Pages/`, `src/Pegasus.Infrastructure/Persistence/EfCaseAcceptanceStore.cs`, `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs`, or `src/Pegasus.Core/Intake/InstructionEvidenceImages.cs`. Must not create a new desktop project or screen.
- **Explicitly out of this diff** (carried from the copied `files` document, and it is a real disagreement in the opposite direction, not an oversight): `src/Pegasus.Infrastructure/Persistence/EfQueuedCustodyProcessor.cs` files every `image/*` attachment as `DocumentSemanticRole.Image` using `IsImage` alone, without `IsPhotographShaped`. After this fix a receipt carrying only a letterhead banner is `ImagesComplete: false` yet still export-eligible. Do not fold that in — widening the diff risks the export side. Raise it as its own ticket and name this one as its origin.
- **Blocks (this must land before these can correctly ship)**: [[DSK-05-05]] — its Traps already state this rule "must be true before this row reaches parity"; [[DSK-03-06]] — the rail figures it computes are wrong while the flag is asserted, and its acceptance is parity against the Razor sources, so it would reproduce the wrong number faithfully; [[DSK-05-01]] — the Review queue it renders contains work that is not ready; [[DSK-06-06]] — its status chip labels the wrong state in the settled vocabulary. A later pass wires these as `blocks` links; this ticket does not.
- **Blocked by**: [[DSK-01-10]] and the standing later-sync cadence — step 2 cannot be answered until a sync has run, and under **D-001** an upstream fix unmerged at the freeze never arrives at all. This ticket must not be closed as "upstream will handle it".
- **Not owned here**: the `screen-specs.md:230-231` correction ([[DSK-03-07]] with [[DSK-06-13]]); the opaque `"Details are incomplete"` due-work reason, which the copied `plan` records as a deliberate follow-up now that the flags mean something; the `EfQueuedCustodyProcessor` banner disagreement above.
- **Traps**: the four existing readiness tests construct `CaseCompleteness` directly and call the policy — they never touch `AllocateIntake`, so they will stay green whether or not the fix works. Green there proves nothing about this change; the new facts must drive `AttemptAutomaticAsync`. Do not re-open CASE-013 by making any flag unconditionally `false`. Do not add an abstraction to `InstructionEvidenceImages` for a single caller. Ordering is already safe — the receipt at the call site is a fresh `receiptQueries.GetAsync` with assets eagerly included and retention commits in the same unit of work strictly before allocation, so the flag cannot be spuriously false from staleness; confirm rather than assume.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket plan document. Note that the copied upstream `plan` already carries its own dated pass for the upstream branch — append a new dated entry for this branch rather than editing that record.

## Outcome

_Filled at closeout._
