---
id: INTK-007
type: ticket
title: >-
  upstream:INTK-033 · A triage-request email creates no Triage and no
  Unidentified item — it is stranded
status: backlog
area: intake-processing
assignee: ''
profile: fix
labels:
  - production-defect
  - found-during-qa
  - triage
  - closed-composition-gate
  - upstream-carryover
  - upstream-INTK-033
  - gateway-worker-ticket
groups:
  - EPIC-014
links: []
refs:
  - docs/frd/frd-03-triage.md
  - docs/frd/frd-09-provider-and-intermediary-routes.md
docs_todo: true
archived: false
created: '2026-08-24T11:52:39.628Z'
updated: '2026-08-24T11:52:39.628Z'
---

## What

Make a classified triage-request e-mail produce the outcome the operator's own notes require: with a known vehicle registration it opens a Triage; without one it is held as Unidentified until the registration is known. Today it produces neither — it is routed into automatic case allocation, fails on a deliberately absent case type, and is visible only in the inbox.

**Verify after the sync before implementing.** This work is implemented upstream on branch `task/intk-033-triage-from-intake` (commit `7b43ab17`) and sits at `review` there, on an unmerged branch. It is **not** in the 32-commit range [[DSK-01-10]] pins at `7d6a948a`. Re-check at sync time on the PLAT-039 pattern: if the branch has merged upstream and arrived, this ticket becomes a verification of the merged behaviour; if it has not, carry the full fix here.

## Why

The Triage composition gate has **never been open in production**. `src/Pegasus.Infrastructure/DependencyInjection.cs:152` composes `NoAcceptedIntakeTriageMatcher`, the only implementation of `IIntakeTriageMatcher` and one that by construction accepts nothing, and `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:97` (`ProductionProfileKeepsTheTriageMatcherInactive`) pins it closed so it cannot be activated by accident. `CreateTriageIfQualifyingAsync` (`src/Pegasus.Core/Intake/DurableIntake.cs:893-905`) requires an `AcceptedTriageMatch` evidence entry that only that matcher can produce. So no Triage has ever been created from intake.

[[DSK-05-11]] builds the native Triage list, detail and actions, and [[DSK-05-12]] builds Unidentified — **two native queues over a gate that produces nothing**, with [[DSK-03-13]] publishing the endpoints that feed them. `AGENTS.md:207` is explicit: "A closed composition or feature gate is a disabled flag, not a partially shipped feature. Do not ship, release, merge as delivered, claim, or document a feature behind one as delivered." Converting those screens without this fix would do exactly that, and would ship the operator's original complaint intact — the inbox labels a message "Triage" and nothing behind the label runs.

**No seeded ticket may make the fix.** [[DSK-05-11]] and [[DSK-05-12]] are desktop slices; [[DSK-03-13]] is a gateway projection whose scope is `src/Pegasus.Web/Api/**`; [[DSK-05-09]]'s scope boundary permits `src/Pegasus.Core/Intake/` "only for rules moved in with a characterization test" and forbids `src/Pegasus.Infrastructure` outright — and this fix changes `src/Pegasus.Infrastructure/DependencyInjection.cs` composition and deletes a Core port. [[DSK-05-09]]'s traps say upstream INTK-033 is "absorbed or arrive[s] by upstream sync"; it does not arrive by the pinned sync, and under **D-001** anything unmerged upstream vanishes at the freeze. `coverage-decision.md` also directs that INTK-033 join TICK-102 in [[DSK-01-09]] step 10's re-check-at-sync list.

## Source of truth

- Import decision: `coverage-decision.md` § Import list — row `INTK-033` ("recreate with a re-check at sync time on the PLAT-039 pattern"); § Plan gaps — "The 208-ticket set contains no owner for Worker and Core/Infrastructure intake defects… INTK-033 means the Triage composition gate has never been open in production"; § Plan gaps — "Only the FIRST upstream sync has an owner"
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:159` — `INTK-033 | intake-processing | backlog | feature | production-defect, found-during-qa, triage | … | gateway-worker-ticket | 07 | intake-processing` (the register's status and label list are stale: upstream is at `review` and also carries `closed-composition-gate`)
- Governing documents: `docs/frd/frd-03-triage.md` § Normal workflow and completion evidence; `docs/frd/frd-09-provider-and-intermediary-routes.md` (both are the upstream ticket's own `refs`)
- Repository rule: `AGENTS.md:207-211` — a closed composition or feature gate is a disabled flag, not a partially shipped feature
- Repository evidence (fork `main`, read 2026-08-24 — the fix is **absent** from this tree):
  - `src/Pegasus.Infrastructure/DependencyInjection.cs:152` — `services.TryAddSingleton<IIntakeTriageMatcher, NoAcceptedIntakeTriageMatcher>();`; `:160` — the matcher passed into `QdosInstructionExtractionPolicy`
  - `src/Pegasus.Core/Intake/IntakeContracts.cs:553` — `IIntakeTriageMatcher`; `:560` — `NoAcceptedIntakeTriageMatcher`; `:116` — `IntakeEvidenceFinding.AcceptedTriageMatch`
  - `src/Pegasus.Core/Intake/DurableIntake.cs:893-905` — `CreateTriageIfQualifyingAsync` and its evidence filter; `:497` and `:618` — its two call sites
  - `src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosInstructionExtractionPolicy.cs:8`, `:13-14` — the matcher constructor parameter and its null fallback; `:135` — where the evidence would be emitted; `:29-36` — the `Vehicle registration` field definition with `IsUkRegistration` and `NormalizeRegistration`; `:383`, `:396` — `SubjectFactLines`
  - `src/Pegasus.Core/Triage/TriageLifecycle.cs:488` and `src/Pegasus.Infrastructure/Persistence/EfTriageStore.cs:72` — the two downstream re-checks of the same evidence; both stay untouched
  - `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs:1333`, `:1345` — the `accepted_triage_match` persisted code
  - `src/Pegasus.Core/Intake/Classification/MailOperationalDestinationPolicy.cs` and `src/Pegasus.Core/Intake/Classification/MailClassificationContracts.cs` — the two copies of the `"triage-request"` literal the upstream plan collapses
  - `src/Pegasus.Core/Intake/InstructionFieldExtraction.cs:384` `IsUkRegistration`, `:400` `NormalizeRegistration` — reused by the subject registration rule
  - `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:97` — `ProductionProfileKeepsTheTriageMatcherInactive`, the pin that must be replaced rather than deleted; `tests/Pegasus.IntegrationTests/AutomationIntakeParityIngressTests.cs:59`, `:101-123` — the `AcceptedTriageMatchPolicy` stub
  - `tests/Pegasus.Core.Tests/Intake/Qdos/` — where the extraction and classification facts land
- Upstream pipeline documents, copied verbatim onto this ticket: `research`, `files`, `plan`, `checklist`. The upstream folder also holds a `post-implementation-report` describing the work already done on `task/intk-033-triage-from-intake`; it was **not** copied, because this ticket enters at Backlog and has implemented nothing. Read it at `.kanmer/areas/intake-processing/INTK-033/post-implementation-report/post-implementation-report.md` in the read-only upstream clone when performing the sync re-check.
- Binding decisions: **L-01** the gateway is `Pegasus.Web` evolved in place; **L-02** verification is the local production-mimicking stack with Azurite; **L-05** the fork board is the single work register; **D-001** the fork becomes the single release source and upstream is frozen, so an unmerged upstream branch is lost unless the fork owns the work
- Depends on: `DSK-01-10` — the first one-way upstream sync, and the point at which the re-check in step 2 happens

### Upstream ticket INTK-033 (verbatim)

Provenance — upstream area `intake-processing`; upstream status **`review`**; upstream profile `feature`; upstream labels `production-defect`, `found-during-qa`, `triage`, `closed-composition-gate`; upstream assignee `claude-code`; upstream branch `task/intk-033-triage-from-intake`; upstream `refs` `docs/frd/frd-03-triage.md`, `docs/frd/frd-09-provider-and-intermediary-routes.md`; upstream `deployment` `not-deployed`. Read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111`, read date **2026-08-24**. Copied unedited. The fork profile is `fix` where upstream is `feature`; the upstream body argues at length that it is a feature because the capability was never delivered — that argument is preserved verbatim below and the profile difference changes only which documents each stage gate requires.

````
## What the operator saw

> *"E-mail 3 - Triage Request E-mail. Identified in the inbox as Triage. Did not
> create a triage case. Did not show in the triage queue."*

Half of that is [[MAIL-012]] working. The other half is a capability that has
never run in production.

## What actually happened, from production

Receipt `d42a5515-a962-42e0-88f7-57a63501d106`, 2026-08-23 14:57:29Z:

| Record | Value |
| --- | --- |
| Classification | `classified` · `pre-instruction-emails` / `triage-request` · policy v4 |
| Classification `CaseType` | *(none — correct; a triage is not a case)* |
| Receipt decision | **`case_created`** — "A definitive instruction was identified and is eligible for case allocation." |
| Allocation attempt | **`failed`** |
| `FailureKind` | **`case_type_unavailable`** |
| `RecoveryDisposition` | `manual_review` |
| `Triage` rows | **0** |
| `UnidentifiedItems` rows | **0** |

So intake classified it as a triage request, then **still attempted automatic
case allocation**, which failed for want of a case type — and produced nothing
at all. No case, no Triage, no Unidentified item. The message is visible only
in the inbox; it appears in no queue anyone works.

## The required behaviour is already written

`operator-notes.md` § Stage 0 — Triage, step 2, verbatim:

> *"keep it as **Unidentified** (formerly `Needs sorting`) **until a vehicle
> registration is known, then open the Triage**"*

Operator, 2026-08-23, confirming: *"Its not a question on the triage, its
explicitly defined in my notes. Since the registration is known, its not
unidentified."*

So the rule is a branch on one fact, and Unidentified is the holding state for a
**missing** registration only:

| Registration on the triage request | Outcome |
| --- | --- |
| known | **open the Triage** |
| not known | **Unidentified**, until it is |

Email 3's subject carries `GD65TVY`. It should have opened a Triage.

## Three faults

**1. Classification is not consulted before allocation.** A `triage-request`
carries no `CaseType` by design, yet `AllocateIntake.AttemptAutomaticAsync`
runs anyway and fails on its absence. A classification that says "this is not a
case" must route to the Triage path, not into case allocation.

**2. Triage creation is behind a closed composition gate.**
`ProcessQueuedIntake.CreateTriageIfQualifyingAsync` (`DurableIntake.cs:893`)
requires an `AcceptedTriageMatch` evidence finding of `Strong` strength with a
matcher key and version. That finding can only come from `IIntakeTriageMatcher`
— and production composes:

```csharp
services.TryAddSingleton<IIntakeTriageMatcher, NoAcceptedIntakeTriageMatcher>();
```

The null matcher, which by name and construction never accepts anything. **The
gate can never pass.** No Triage has ever been created from intake in
production, and none can be until this is composed.

Note also that `CreateTriageIfQualifyingAsync` keys off *evidence findings*,
never off the triage **classification** — so even a working matcher would be
answering a different question from the one the operator's email asks. The
qualifying condition has to become "this message is a triage request", which is
now a recorded classification.

**3. The registration is not extracted from a triage request.** The branch above
turns entirely on whether a registration is known, and today nothing reads one
off a triage request — `CreateTriageIfQualifyingAsync` reads
`receipt.InstructionDraft?.VehicleRegistration`, which is populated by the
*instruction* extraction path. A triage request is not an instruction. Getting
the registration out of the subject (`… Vehicle registration GD65TVY`) or body
is in scope, because without it every triage request falls to Unidentified and
the rule's first branch never fires.

## Repository position

CLAUDE.md: *"A closed composition or feature gate is a disabled flag, not a
partially shipped feature. Do not ship, release, merge as delivered, claim, or
document a feature behind one as delivered."*

Triage-from-intake is therefore **not delivered**, and this is a feature ticket,
not a bug fix. The operator reasonably expected otherwise, because the inbox
labels the message "Triage" — the label is real and the work behind it is not.
That gap is the most important thing here.

## Governing docs

`operator-notes.md` § Stage 0 is the authority above; `docs/frd/frd-03-triage.md`
§ Normal workflow and completion evidence holds the canonical transitions. Read
both before planning — the rule is settled and does not need re-deciding, only
implementing.
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; tests by `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`) → `test-gap-analysis` (dotnet/skills `98f84851`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `get_ticket_doc`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `fix`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; `fix` needs `files`, `plan` and `questions-resolved` to leave Preparing, `post-implementation-report` to enter Review, `proof` to enter Done). The upstream `research`, `files`, `plan` and `checklist` are already on this ticket — read them with `get_ticket_doc` before writing anything new.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient. Read the verbatim upstream body above and the four copied pipeline documents on this ticket (`research`, `files`, `plan`, `checklist`) with `get_ticket_doc` — they are the upstream author's own analysis and they are already correct about this tree. Then read `docs/frd/frd-03-triage.md` § Normal workflow and completion evidence and `docs/operator-notes.md` § Stage 0. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/upstream-intk-033-triage-from-intake` and worktree `../pegasus-worktrees/upstream-intk-033-triage-from-intake` from `origin/dev`.
2. **Sync re-check, before any code.** After [[DSK-01-10]] lands, resolve whether upstream `task/intk-033-triage-from-intake` (commit `7b43ab17`) has merged and arrived on the fork: `git log --oneline upstream/main --grep INTK-033` and `git merge-base --is-ancestor 7b43ab17 origin/main`. **If it has arrived**, this ticket becomes verification only — confirm the acceptance criteria below against the merged code and record the commit; do not re-implement. **If it has not**, carry the full fix from the copied `plan`. Record which branch was taken, with the command output, in the ticket `plan`. This is the same pattern [[DSK-01-09]] step 10 applies to PLAT-039 and TICK-102, and INTK-033 belongs on that same re-check list.
3. Name the triage-request category once. Add a `TriageRequestSubtype` constant and an `IsTriageRequest` predicate to `src/Pegasus.Core/Intake/Classification/MailClassificationContracts.cs`, and use it from `MailOperationalDestinationPolicy` and `QdosMailClassificationPolicy` instead of their two `"triage-request"` literals. Reuse the existing `MailCategory` factory and taxonomy validation.
4. Stop routing a triage request into case allocation. In `ProcessIntake.AssessAsync`, immediately after the existing ambiguous-case-match override, downgrade `IntakeDecision.CaseCreated` to `NeedsSorting` when the classification `IsTriageRequest`, with the reason "A Triage request is pre-case work; no case is created from it." **Keep** the instruction draft — it carries the registration the Triage needs. Reuse the override shape the method already uses twice; do not touch `AllocateIntake`, which is failing closed correctly.
5. Derive the `AcceptedTriageMatch` evidence from the classification decision, per the mapping table in the copied `research` § 2 (`MatcherKey` ← `classification.PolicyKey`, `MatcherVersion` ← `classification.PolicyVersion`, `Signal` and `Detail` ← the matched predicate, `Source` ← `EmailBody` or `Subject`, `Strength` ← `Strong`), exactly one entry. Then delete `IIntakeTriageMatcher`, `NoAcceptedIntakeTriageMatcher` and `IntakeTriageMatch` from `src/Pegasus.Core/Intake/IntakeContracts.cs:553-570`, the matcher parameter and `ValidateTriageMatch` from `QdosInstructionExtractionPolicy` (`:8`, `:13-14`, `:135`), and the registration at `src/Pegasus.Infrastructure/DependencyInjection.cs:152`, `:160`. Bump `QdosInstructionExtractionPolicy.Version` because its evidence output changes. The three downstream re-checks — `CreateTriageIfQualifyingAsync`, `TriageLifecycle.ValidateAcceptedMatchEvidence` (`src/Pegasus.Core/Triage/TriageLifecycle.cs:488`) and `EfTriageStore.CreateFromIntakeAsync` (`src/Pegasus.Infrastructure/Persistence/EfTriageStore.cs:72`) — stay untouched; their gate simply starts passing.
6. Extract the registration from the subject template. Add a `Vehicle Registration [:.]? <value>` rule to `SubjectFactLines` (`QdosInstructionExtractionPolicy.cs:396`) validated with `InstructionFieldEngine.IsUkRegistration` (`src/Pegasus.Core/Intake/InstructionFieldExtraction.cs:384`) and emitted under the label the field definitions already read (`:29-36`), plus a negative lookahead on the existing vehicle-description rule so it stops capturing the label. One bounded pattern, no ambiguous nested quantifier. Per the copied `research` § 3, the body-phrase template already extracts its registration and needs nothing.
7. Wire both branches of the operator's rule. `IsUnidentifiedEligible` defers a triage request exactly as it defers image-only material; `CreateTriageIfQualifyingAsync` reports whether it created a Triage; `SynchronizeUnidentifiedAsync` registers the receipt as Unidentified only when it did not. Reuse the existing image-only deferral mechanism — a second deferral concept is a stop condition.
8. Replace, do not delete, the composition pin: `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:97` currently pins the matcher inactive; it must become a test pinning the **active** route — production composes the classification policy as the triage trigger and no `IIntakeTriageMatcher` remains. Coordinate with [[DSK-02-12]] and the imported `upstream:INTK-002`, which both add composition facts; sequence rather than collide.
9. **Re-expressed for the desktop world.** The upstream ticket verifies through the Razor `Pages/Triage/Index` and `Pages/Triage/Details`, which [[DSK-05-26]]'s cut list deletes. State the same requirement against what replaces them and record it in the `plan`: after this lands, a triage-request e-mail with a registration must appear in [[DSK-05-11]]'s native Triage list through [[DSK-03-13]]'s triage endpoints, and one without a registration must appear in [[DSK-05-12]]'s Unidentified queue through the same group — with neither slice adding a second trigger, and with `AGENTS.md:207` satisfied because the gate is genuinely open. Do not edit those tickets; record the note for them.
10. Add the Core and integration facts from the copied `checklist`: a triage request is never `CaseCreated`; it carries exactly one `AcceptedTriageMatch`; with a registration it opens a Triage and registers no Unidentified item; without one it registers Unidentified and opens no Triage; both subject spacings extract; the vehicle-description rule no longer swallows the label; a message carrying both tells classifies `Ambiguous` and creates no Triage; an audit instruction still becomes a case.
11. Update `docs/open-decisions.md` (its triage-matcher activation paragraph currently asserts the opposite of what ships), `docs/frd/frd-03-triage.md`, `docs/frd/frd-09-provider-and-intermediary-routes.md`, `docs/principal-rules-and-mappings/qdos.md` and — checked, not assumed — `docs/capabilities.md`. `docs/operator-notes.md` is **not** edited; it already states the rule.
12. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the ticket `plan`, then open the PR into `dev`.

## Acceptance criteria

- [ ] The sync re-check is performed and recorded before any code: either the upstream commit `7b43ab17` arrived and this ticket verifies it, or it did not and the fix is carried here.
- [ ] A classified triage request is never `IntakeDecision.CaseCreated` and never reaches automatic case allocation; `case_type_unavailable` stops occurring for triage requests without any change to `AllocateIntake`.
- [ ] A triage request with a known vehicle registration opens a Triage and registers **no** Unidentified item; one without a registration registers Unidentified and opens **no** Triage.
- [ ] Exactly one `AcceptedTriageMatch` evidence entry is derived from the classification decision; `IIntakeTriageMatcher`, `NoAcceptedIntakeTriageMatcher` and `IntakeTriageMatch` no longer exist, and `DependencyInjection.cs` no longer registers them.
- [ ] `TriageLifecycle.ValidateAcceptedMatchEvidence` and `EfTriageStore.CreateFromIntakeAsync` are unchanged — the downstream contract starts receiving evidence rather than being rewritten.
- [ ] The production composition test pins the **active** route; the protection is re-pointed, not removed.
- [ ] Both subject spacings extract the registration, and the vehicle-description rule no longer captures the `Vehicle Registration` label.
- [ ] `docs/open-decisions.md` no longer asserts that the triage matcher is awaiting predicates.

## Verification

- [ ] `git log --oneline upstream/main --grep INTK-033` and `git merge-base --is-ancestor 7b43ab17 origin/main` — expected: a definite yes or no, recorded in the `plan` with its output, before any code is written.
- [ ] `dotnet build --configuration Release` — expected: clean after the three types are deleted.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: decision, evidence, both branches of the rule, both subject spacings and the ambiguity case all pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: the triage-from-intake path creates a Triage end to end, and `ProductionCompositionTests` pins the active route.
- [ ] Local stack run (L-02) — expected: a triage-request message carrying a registration produces a Triage row and no Unidentified row; the same message without one produces the reverse. Command log captured as `proof`.

## Evidence tier

Tier 1 — Static/build/architecture. Tier 2 — Core/domain. Tier 6 — Functions/Azurite caller.
Tier 1 obliges the composition fact proving one policy owner and no surviving matcher port; tier 2 obliges positive, contradictory, ambiguous and failure cases for the decision, the evidence and both branches of the registration rule; tier 6 obliges the real Worker trigger against Azurite showing a Triage actually created from a queued intake, which has never happened in production.

## Documentation changes

- `docs/open-decisions.md` — close the triage-matcher activation paragraph; the predicates it waited on are accepted and the matcher is retired
- `docs/frd/frd-03-triage.md` — state the automatic route: an accepted route classification of triage-request opens a Triage when a registration is known and holds the material in Unidentified until it is
- `docs/frd/frd-09-provider-and-intermediary-routes.md` — record the accepted QDOS triage predicates beside the accepted case-association ones
- `docs/principal-rules-and-mappings/qdos.md` — §2 and §5: the triage tells now drive Triage creation; the subject registration fact; the new policy version
- `docs/capabilities.md` — canonical-owner row if the triage capability's owner moves; **check, do not assume**
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — annotate row `INTK-033` with this fork ticket id, correct its stale status and label list, and add it beside TICK-102 in the re-check-at-sync list [[DSK-01-09]] step 10 owns
- `docs/operator-notes.md` — **not** edited; it already states the rule

## Guardrails

- **Azure**: no write. Reading the production records that established the defect is a read and needs no approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`).
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/` (classification contracts, `ProcessIntake`, `DurableIntake`, `IntakeContracts`, the QDOS extraction policy), `src/Pegasus.Infrastructure/DependencyInjection.cs`, `tests/Pegasus.Core.Tests/`, `tests/Pegasus.IntegrationTests/` and the named documents. Must **not** touch `src/Pegasus.Core/Triage/TriageLifecycle.cs`, `src/Pegasus.Infrastructure/Persistence/EfTriageStore.cs`, `CreateTriageFromIntake`, `src/Pegasus.Web/Api/**`, or any desktop project — the downstream contract is correct and complete; this change only makes the evidence it requires arrive.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-11]], [[DSK-05-12]] and [[DSK-03-13]] — all three build a queue, a screen or an endpoint over a composition gate that has never passed, and `AGENTS.md:207` forbids shipping a feature behind a closed gate as delivered. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync, which is also when the step-2 re-check happens. [[DSK-01-09]] owns adding this ticket to its re-check-at-sync list. Composition-test coordination with [[DSK-02-12]] and the imported `upstream:INTK-002`.
- **Traps**: do **not** write a `QdosIntakeTriageMatcher` — the copied `research` § 2 shows it would be a second owner of a question the accepted route classification policy already answers, which ADR-0008 and `AGENTS.md` § Simplicity rails both forbid. Do not delete the production composition pin; re-point it. Do not register an Unidentified item for a receipt that is about to open a Triage — the ordering hazard is real and the image-only deferral already solves it. `accepted_triage_match` is a persisted code. The upstream author's own § 6 corrections (recorded at the end of the copied `research`) matter: `UnidentifiedResolutionTargetKind` **does** have a Triage member, and the supersession half is in scope.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Outcome

_Filled at closeout._
