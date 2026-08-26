# Plan

One change, in dependency order. Each step names what it reuses.

## 1. Name the triage-request category once

`MailClassificationContracts.cs`: a `TriageRequestSubtype` constant on
`MailCategory`, an `IsTriageRequest` predicate on `MailCategory`, and one on
`MailClassificationResult` (`Classified` **and** the category matches).
`MailOperationalDestinationPolicy` and `QdosMailClassificationPolicy` both use
it instead of their own literal.

**Reuses:** the existing `MailCategory` factory and taxonomy validation. This is
the "one list per concept" rail — the literal exists twice today.

## 2. A triage request is not a case

`ProcessIntake.AssessAsync`, immediately after the existing ambiguous-case-match
override (`:515-520`):

```csharp
if (decision == IntakeDecision.CaseCreated
    && mailClassificationDecision?.IsTriageRequest == true)
{
    decision = IntakeDecision.NeedsSorting;
    reason = "A Triage request is pre-case work; no case is created from it.";
}
```

The instruction draft is deliberately **kept** — it carries the registration the
Triage needs.

**Reuses:** the override shape already used twice in this method for exactly
this kind of classification-derived correction.

**Effect:** `AttemptAutomaticAsync` returns null for a non-`CaseCreated`
decision (`IntakeAllocation.cs:237`), so the `case_type_unavailable` failure
stops happening — without touching allocation, which was failing closed
correctly.

## 3. Derive the accepted triage match from the classification

Same method, appended to the evidence list: one `AcceptedTriageMatch` entry
built from the classification decision, per the mapping table in research §2.
The `Source` and `Signal` come from whichever tell fired, read off the recorded
predicate results — so the evidence says *which* template this was.

Then delete `IIntakeTriageMatcher`, `NoAcceptedIntakeTriageMatcher`,
`IntakeTriageMatch`, the extraction policy's matcher parameter and its
`ValidateTriageMatch`, and the `DependencyInjection` registration.

**Reuses:** the whole downstream contract — `CreateTriageIfQualifyingAsync`,
`TriageLifecycle.ValidateAcceptedMatchEvidence`,
`EfTriageStore.CreateFromIntakeAsync` — unchanged. Their gate simply starts
passing.

**Why deletion rather than a real matcher:** research §2. A second owner of a
question the accepted route classification policy already answers is a stop
condition, and FRD-03 names the classification policy as the trigger.

Bump `QdosInstructionExtractionPolicy.Version` 5 → 6: its evidence output
changes.

## 4. Registration from the subject template

`SubjectFactLines`: a rule for `Vehicle Registration [:.]? <value>`, validated
with the existing `InstructionFieldEngine.IsUkRegistration`, emitted as the
label `Vehicle Registration:` — which the field definitions already read. Both
corpus spacings are covered by one bounded pattern; no ambiguous nested
quantifier, checked deliberately after release 26's self-inflicted ReDoS.

Add a negative lookahead to the existing vehicle rule so it stops capturing
`Registration : VO75DFJ` as a vehicle description.

**Reuses:** `IsUkRegistration`, the `Vehicle registration` field definition and
its `NormalizeRegistration` canonicaliser. Nothing new is introduced.

**Scope note:** the body-phrase template already extracts its registration from
the letter's `Registration:` line — verified against the corpus. Only the
subject template needs this.

## 5. Both branches of the operator's rule

`IsUnidentifiedEligible` defers a triage request, exactly as it defers image-only
material and for the same reason — a later step may resolve it.
`CreateTriageIfQualifyingAsync` returns whether it created a Triage;
`SynchronizeUnidentifiedAsync` registers the receipt as Unidentified when it did
not.

**Reuses:** the image-only deferral mechanism wholesale. No second deferral
concept.

| Registration | Triage | Unidentified |
| --- | --- | --- |
| known | created, `Open` | none |
| not known | none | registered |

## 6. Documents

Per the files table. `docs/open-decisions.md` is the one that *must* change: it
currently asserts the opposite of what ships.

## 7. Tests

Core: the decision, the evidence, both branches of the rule, both subject
spacings, and the vehicle-description regression. Integration: the four suites
whose `AcceptedTriageMatchPolicy` stub is now the wrong shape move to the real
classification path — which makes them stronger, since they stop asserting
against a fixture the production code no longer has.

Negative tests worth pinning: a message carrying *both* tells classifies
`Ambiguous` and creates no Triage; an audit instruction still becomes a case.

## Verification

- `dotnet build --configuration Release`
- `dotnet test tests/Pegasus.Core.Tests`
- `dotnet test tests/Pegasus.IntegrationTests --filter "Category!=Corpus"` —
  ~28 minutes, chunked, log kept. CI's three shards on the exact SHA are the
  authority if the local run is interrupted.

## Risks

- **Retiring a pinned composition.** The production composition test exists so
  the matcher can never be activated by accident. Replacing it with a test that
  pins the *active* route keeps that protection pointed at the real mechanism
  rather than deleting it.
- **Four integration suites depend on the stub.** They are the largest part of
  the diff and the most likely source of churn.
- **A triage request that also looks like an instruction.** Cannot happen: two
  category candidates resolve to `Ambiguous`, which is neither a case nor a
  Triage. Pinned as a test.

## Execution amendment — 2026-08-26

The inherited upstream carry-over text is provenance and repository evidence only. Per the current operator boundary, this ticket must not fetch, compare, merge, or sync any upstream repository or branch. The upstream commit and source snapshot are therefore not a prerequisite and no upstream re-check will be performed.

Implementation source is the configured `origin` remote for this repository (the PegasusDesktop repository), based on `origin/dev`. Carry the fix in this repository regardless of the upstream branch state. Replace the prior sync-recheck acceptance item with: **the in-repository implementation and its acceptance criteria are verified on the merged PegasusDesktop `main` result**.

The documentation note originally describing addition to an upstream-sync re-check list is amended to record INTK-007 as an in-repository carry-over owned by this board; it must not prescribe or imply a future upstream operation. No cloud, mailbox, Box, credential, deployment, or upstream write is in scope.

## Governing docs and current-state checks

- `docs/frd/frd-03-triage.md` is the linked behavior authority: the exact accepted route classification starts Triage work; a usable VRM opens Triage and a missing VRM remains Unidentified.
- `docs/frd/frd-09-provider-and-intermediary-routes.md` is the linked route authority: QDOS predicates remain route-owned and fail closed on ambiguity; this change does not add a second classifier or route.
- `docs/operator-notes.md` was read as binding business truth and is not edited; its Stage 0 rule already states the VRM branch.
- `docs/capabilities.md` was checked: TRI-02 remains the canonical owner and its owner does not move, so no capabilities-registry edit is required.
- `docs/open-decisions.md` is updated because its inactive-matcher statement is superseded by the accepted route-owned implementation.
- No upstream operation is required or permitted. No cloud, mailbox, Box, credential, deployment, or external write is required.
