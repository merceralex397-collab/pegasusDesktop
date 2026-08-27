# Open questions — FEAT-023: Extract `OperatorLabels` to the shared assembly

This document exists because the ticket body directs it. Step 2 reads: "**Resolve the
duplicate-ticket question before any code change.** [[GWY-016]] describes the same relocation. Agree
with the gateway author which ticket performs the move and which one closes as covered, and record
the decision under this ticket's open questions — an unticked open question blocks the Kanmer move."

An unticked `- [ ]` item above `## Parked` blocks exactly three boundaries — `leave-preparing`,
`enter-review` and `enter-done`. It does **not** gate `leave-backlog`. Blocking `leave-preparing`
is the intended behaviour here: the relocation must not be performed twice, and it must not be
performed by neither ticket.

## Blocking

- [x] **Which ticket performs the `OperatorLabels` relocation — this one or [[GWY-016]] (plan handle
  `DSK-03-16`) — and which closes as covered?**
  Both describe the same move. [[GWY-016]]'s title is "Relocate `OperatorLabels` to
  `Pegasus.Contracts` as one shared vocabulary list"; its row is in
  `docs/desktop/03-gateway-api-and-data/README.md` § 5. This ticket additionally carries the
  fold-in of the two page-local `IntakeDecision` maps and the `docs/design/README.md:541-542`
  reconciliation (upstream `INTK-004`'s label half), which [[GWY-016]] does not mention.
  **Answered by:** the gateway author who holds [[GWY-016]], with the implementer of this ticket.
  **What the answer must record:** the performing ticket, the covered ticket, and — if [[GWY-016]]
  performs the move — where the fold-in and reconciliation land, because they must not be lost.
  Tick this box only when the agreement is written into both tickets.

## Parked (explicitly deferred)

- **Whether `InOffice`'s UTC fallback should become louder on the desktop.**
  `src/Pegasus.Web/Presentation/OperatorLabels.cs:441-455` falls back to `TimeZoneInfo.Utc` when
  `FindSystemTimeZoneById("Europe/London")` throws, deliberately, because "a missing zone database
  is an operational fault and a blank screen would be a worse answer than an hour's offset"
  (`:436-440`). Once the desktop consumes this map that fallback can fire on a workstation and
  silently show every time an hour early through British Summer Time.
  **Deferred because** this ticket changes no behaviour: the code moves verbatim, and altering the
  fallback would be an unsanctioned change to a documented decision. Recorded in the
  post-implementation report; a separate ticket is raised if the desktop needs a louder signal.

- **The final home — `Pegasus.Contracts` or `Pegasus.Core`.**
  **Deferred from this document, not from the work.** The ticket body routes this decision to the
  plan, not to open questions: step 3 says "record the decision and its rationale in the ticket plan
  before moving a file." It is recorded in the plan's § Steps step 3 and § Risks, with the deciding
  evidence — `OperatorLabels.cs:1-12` imports ten `Pegasus.Core` namespaces and no ASP.NET, so
  whichever home is chosen must reference `Pegasus.Core`, and `reuse-map.md` and plan 05 § 3 both
  prefer `Pegasus.Contracts`.

## Defaults taken rather than asked

- **The third page-local map is folded in with its wording unchanged.**
  `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:593-602` (`SuggestionOutcomeLabel`, over
  `VrmRecognitionOutcomeKind`) renders `"Technical failure"` at `:598`. It is a page-local
  decision-to-label map, so one-list-per-concept folds it into the single list; but it maps a
  different enum and `docs/design/README.md:541-542` does not govern it, so its **text does not
  change**. Taken as a default rather than raised, because the ticket names exactly one sanctioned
  text change and this is not it. The consequence for the ticket's verification grep is recorded in
  the plan's § Verification.


## Resolution — 2026-08-27

GWY-016 performs the one relocation into `Pegasus.Contracts`. It absorbs the two `IntakeDecision` fold-ins and the binding text reconciliation, while preserving the separately typed `VrmRecognitionOutcomeKind` wording. FEAT-023 is covered and will not create a second branch, move, or PR.
