# Plan — FEAT-023: Extract `OperatorLabels` to the shared assembly

**Diff estimate: ~50 files, ~950 lines.** Derived from the measured inventory below: 1 file moved
(685 lines, namespace line only), 24 `.cshtml` and 16 `.cs` consumers re-pointed (`@using` /
`using` lines, ~40 files × ~2 lines), 2 page-local maps deleted and folded in (~26 lines removed),
2 project files edited for the new reference, 2 new test files (~250 lines), 1 architecture fact
(~40 lines), and 2 documentation files (~30 lines). The line count is dominated by the new tests,
not by the move: the move itself changes one namespace declaration.

**Chore inventory** — this profile owes no `research` or `files` document, so the measured surface
area is stated here (`docs/engineering.md` § plan sizing: the estimate comes from a real inventory,
not an assertion). All measurements taken at `bbd1c549` (`git rev-parse --short HEAD`, 2026-08-24).

| Path | Measured today | What happens to it |
| --- | --- | --- |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs` | **685 lines** (`wc -l`). `using` list is `System.Globalization`, `System.Text`, and `Pegasus.Core.{Assessment, Cases, Documents, ImageIntake, Intake, Tasks, Workflow, Identity, Vehicle, Intake.Unidentified}` (`:1-12`) — **no ASP.NET**. Namespace `Pegasus.Web.Presentation` (`:14`). Doc comment at `:16-18`: "The single place a persisted code becomes words an operator reads." | Moved whole; type name and every member signature unchanged; only the namespace line changes. |
| Consumers, `.cshtml` | **24** (`grep -rl "OperatorLabels" src/ --include=*.cshtml \| wc -l`) | `@using` re-pointed, ideally at `_ViewImports.cshtml`. |
| Consumers, `.cs` | **17 hits**, of which one is the file itself → **16 consumers**: `Pages/Administration/Automation/Activity.cshtml.cs`, `Pages/Administration/Mailboxes.cshtml.cs`, `Pages/Cases/Create.cshtml.cs`, `Pages/Cases/Details.cshtml.cs`, `Pages/ImageIntake/Index.cshtml.cs`, `Pages/Intake/Details.cshtml.cs`, `Pages/Mail/Index.cshtml.cs`, `Pages/Mail/Message.cshtml.cs`, `Pages/Triage/Details.cshtml.cs`, `Pages/Triage/Index.cshtml.cs`, `Pages/Unidentified/Details.cshtml.cs`, `Pages/Upload.cshtml.cs`, `Pages/UploadStatus.cshtml.cs`, `Presentation/MailClassificationSelection.cs`, `Presentation/UploadCaseDecision.cs`, `Presentation/UploadOutcome.cs` | `using` re-pointed. |
| `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` | `public static string DecisionLabel(IntakeDecision decision)` at **`:350`**, running to `:361`. `OcrRequired => "Document text required"` at **`:357`**; `TechnicalFailure => "Technical failure"` at **`:358`**. *(The ticket body cites `:349-360`, `:356` and `:357` — a one-line offset at this revision; the body's intent is unambiguous and the code is as described.)* | Deleted; folded into the single map with the reconciled words. |
| `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` | `private static string OutcomeLabel(IntakeDecision? decision)` at **`:1014`**, running to `:1025`; `OcrRequired` at **`:1019`**, `TechnicalFailure` at **`:1020`** — exactly as the body cites. A second `OutcomeLabel(RetainedMailSummary)` overload sits above it at `:1005`. | Deleted; folded in. The overload at `:1005` delegates to it at `:1011` and must be re-pointed, not deleted. |
| `docs/design/README.md` | The binding table rows at **`:541`** (`` `OcrRequired` `` → `Needs text extraction`) and **`:542`** (`` `TechnicalFailure` `` → `Failed`), with the clarifying note at **`:550`** | The authority for the one permitted text change. |
| `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:593-602` | **A third page-local map**, `SuggestionOutcomeLabel(ImageVrmSuggestion)`, mapping `VrmRecognitionOutcomeKind` — including `TechnicalFailure => "Technical failure"` at **`:598`**. It is a **different enum** and the `docs/design/README.md:541-542` table does not govern it. | See § Steps step 5: it is a page-local decision-to-label map and folds into the single list, but its **text does not change**. It also breaks the ticket's verification grep as written — see § Verification. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs:164` | A `<see cref="Pegasus.Web.Pages.Mail.MessageModel.OutcomeLabel(IntakeDecision)"/>` inside the `MailOperationalDestinationLabel` remark | **Breaks on the move**: a `Pegasus.Contracts` file cannot `cref` a `Pegasus.Web` type. Must be re-pointed at the folded-in member in the same change. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs:436-455` | `InOffice` — the single Europe/London conversion, with `TimeZoneInfo.FindSystemTimeZoneById("Europe/London")` at `:446` and a deliberate UTC fallback at `:451`, documented at `:436-440` | Moves unchanged, but the fallback now runs on **Windows** as well as Linux; see § Risks. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs:353-363`, `:383` | `IntakeCannotBecomeCaseReason(IntakeDecision)` already maps `IntakeDecision` for a different question; `HistoryEvent` maps `"image_intake_registered"` → `"Vehicle images registered"` at `:383` | Evidence that `IntakeDecision` vocabulary already lives here; the fold-in joins it rather than adding a new concern. |
| `tests/Pegasus.ArchitectureTests/` | 11 `.cs` files; `DependencyDirectionTests.cs` **520 lines** — the reflection-based fact style to extend | Gains the single-vocabulary fact. |

## Approach

Move the file whole into the home decided with the gateway author, changing only its namespace,
then re-point every enumerated consumer and fold the two page-local `IntakeDecision` maps into it —
reconciling `OcrRequired` and `TechnicalFailure` against the binding `docs/design/README.md:541-542`
table as the **one stated exception** to the no-label-text-changed rule, recorded word by word. An
architecture fact then prevents a second vocabulary type or a re-grown page-local map.

Rejected: **leaving `OperatorLabels` in `Pegasus.Web` and giving the desktop its own map.** The
settled business vocabulary would drift between two clients, which `AGENTS.md` § Simplicity rails
forbids (one list per concept). Also rejected: **moving the file and folding the page-local maps in
two separate tickets** — sequencing this before [[GWY-010]] (plan handle `DSK-03-10`) freezes the
intake decision codes into the Intake detail DTO is the point; a second pass would arrive after the
generated client already carried the mismatched words.

## Governing docs

The ticket's `refs` is `docs/frd/frd-12-operator-experience.md`, which exists.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-12 § `Operator experience` (`:4`ff) | Settled operator vocabulary is what the operator reads; a state has one name | Steps 5–6 (one map, one name per decision), Step 9 (the architecture fact) |
| FRD-12 § `Queues: tabs and filters` (`:58`ff) | Queue and list states are presented in the settled vocabulary | Step 4's reconciliation, which is exactly a queue-state wording correction |

`docs_todo: true`, confirmed in `get_doc_gates FEAT-023`. Profile `chore`: `leave-preparing`
requires `plan` and `questions-resolved`; `enter-done` requires `proof`.

> **New ADR** — ADR-0100 (native WinUI 3 / Windows 11 desktop client converted inside this fork;
> records the deviation that `Pegasus.Core` is **not** split into Domain and Application), authored
> by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:156`) and in
> `docs/desktop/05-implementation-and-migration/README.md` § 3; if the ADR lands differently this
> plan is revised before implementation. ADR-0103 (gateway evolved in place) is authored by the
> same ticket and is why the web keeps consuming the list from its new home.

Programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `AGENTS.md` § Simplicity rails | One list per concept; a second list is a stop condition | § Approach, Steps 5–6, Step 9 |
| `docs/engineering.md` § One Core owner | A business rule has one implementation; migrate or delete the replaced code, registrations, tests and documentation **in the same slice** | Steps 5–6, Step 11 |
| `docs/engineering.md` § Required evidence tiers (1, 2, 5) | Tier 1 obliges compiling the approved projects and enforcing dependency direction and one policy owner; tier 2 obliges positive and failure cases for the map; tier 5 obliges evidence the real web routes still render the same words | Steps 7–10, § Verification |
| `docs/design/README.md:541-542` and the note at `:550` | The binding decision→label table: `OcrRequired` → `Needs text extraction`, `TechnicalFailure` → `Failed` | Step 4 |
| Plan 05 § 3 ("Extract `OperatorLabels` to a shared assembly") | Home is `Pegasus.Contracts` (preferred) or Core; the final home is decided in this ticket with the gateway author | Step 3, and the open question |
| `reuse-map.md` (`Presentation/OperatorLabels.cs` row) | EXTRACT to the shared assembly, `Pegasus.Contracts` preferred; "losing it would silently regress business vocabulary" | Step 3, Step 5 |
| L-01 | The gateway is `Pegasus.Web` evolved in place, so the web keeps consuming the list from its new home | Steps 6–7 |
| L-04 | Routing named on the ticket | § Routing |
| Upstream `INTK-004` (label half, absorbed here) | "The design README binds"; the decision→label mapping exists twice in Web and must become one table | Steps 4–6 |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | Upstream `INTK-004` has **no fork ticket**; the board's `INTK-004` is upstream `INTK-027`, a different ticket | § Risks |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (owns the move and
  the web re-point); `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (independent review that no vocabulary changed beyond the recorded exception)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`,
  `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `run-tests` (dotnet/skills
  `98f84851`, `plugins/dotnet-test/skills/run-tests/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` →
  `kanmer-closeout` (call `get_doc_gates <id>` before every move; `chore` needs `plan` and
  `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refining the ticket body's eleven steps. Body step numbers in brackets.

1. **[body 1] Orient and take.** Read the plan row, the `reuse-map.md` `OperatorLabels` row, § 3 of
   the area plan, and the [[GWY-016]] (plan handle `DSK-03-16`) row in
   `docs/desktop/03-gateway-api-and-data/README.md` § 5. Call `get_doc_gates FEAT-023`, then
   `take_ticket` with branch `task/dsk-05-23-operator-labels` and worktree
   `../pegasus-worktrees/dsk-05-23-operator-labels` from `origin/dev`.
2. **[body 2] Resolve the duplicate-ticket question before any code change.** [[GWY-016]] describes
   the same relocation. Agree with the gateway author which ticket performs the move and which
   closes as covered. **This is recorded as an unticked item in this ticket's `open-questions`
   document, as the body directs — an unticked open question blocks `leave-preparing`,
   `enter-review` and `enter-done`, which is the intended behaviour here.** Relevant evidence
   already gathered: [[GWY-016]]'s title is "Relocate `OperatorLabels` to `Pegasus.Contracts` as one
   shared vocabulary list", so the gateway-side ticket has already named a home.
3. **[body 3] Decide and record the final home, in this plan.** The choice is
   `src/Pegasus.Contracts` (preferred by both `reuse-map.md` and plan 05 § 3, because the map is
   presentation vocabulary) or `src/Pegasus.Core`. Decide on the **evidence**: whether any
   `Pegasus.Contracts` consumer would pull a `Pegasus.Core` dependency it should not have. The
   measured input: `OperatorLabels.cs:1-12` imports ten `Pegasus.Core` namespaces and no ASP.NET, so
   whichever home is chosen must reference `Pegasus.Core`. Record the decision and its rationale
   here **before moving a file**.
4. **[body 4] Enumerate, then fold in and reconcile.** Run
   `grep -rn "OperatorLabels" src/ --include=*.cs --include=*.cshtml` and record the full list —
   24 `.cshtml` and 16 `.cs` at the SHA read (the 17th `.cs` hit is the file itself). Then fold in
   the two page-local `IntakeDecision` maps: `Pages/Intake/Details.cshtml.cs` `DecisionLabel`
   (`:350-361`) and `Pages/Mail/Message.cshtml.cs` `OutcomeLabel(IntakeDecision?)` (`:1014-1025`).
   While folding, reconcile against `docs/design/README.md:541-542`:

   | Decision | Before (both maps) | After (binding table) | Governing line |
   | --- | --- | --- | --- |
   | `IntakeDecision.OcrRequired` | `Document text required` | **`Needs text extraction`** | `docs/design/README.md:541` |
   | `IntakeDecision.TechnicalFailure` | `Technical failure` | **`Failed`** | `docs/design/README.md:542` |

   These two rows are the **one stated exception** to the no-label-text-changed rule. Record them
   with before, after and governing line in this plan and again in the post-implementation report.
   Note the differing default arms — `Details.DecisionLabel` throws on an unknown value (`:360`)
   while `Message.OutcomeLabel` returns `"Not yet processed"` (`:1024`); the folded map keeps the
   **fail-loud** behaviour, and the "not yet processed" case becomes an explicit null/absent
   argument rather than an unknown-enum fallback. Also re-point the
   `OutcomeLabel(RetainedMailSummary)` overload at `Message.cshtml.cs:1005`, which delegates at
   `:1011` — it is not deleted.
   Sequence this ticket **before** [[GWY-010]] (plan handle `DSK-03-10`) freezes the intake decision
   codes into the Intake detail DTO, so a generated client never carries the mismatched words.
5. **[body 4, refinement] Handle the third page-local map.** `Pages/Intake/Details.cshtml.cs:593-602`
   `SuggestionOutcomeLabel(ImageVrmSuggestion)` maps `VrmRecognitionOutcomeKind` and renders
   `"Technical failure"` at `:598`. It is a page-local decision-to-label map — so under
   one-list-per-concept it folds into the single list — but it maps a **different enum** and the
   `docs/design/README.md:541-542` table does **not** govern it. Its text therefore does **not**
   change: the sanctioned exception is the two `IntakeDecision` rows only. Fold the member in with
   its wording intact and record the reasoning; the alternative — leaving it behind — would let the
   architecture fact at step 9 fail or force it to be written loosely.
6. **[body 5] Move the file.** Move `src/Pegasus.Web/Presentation/OperatorLabels.cs` into the
   decided project, keeping the type name and every member signature identical and changing only the
   namespace. Do not reorganize, rename or "tidy" any label. Fix the `cref` at `:164`, which points
   at `Pegasus.Web.Pages.Mail.MessageModel.OutcomeLabel(IntakeDecision)` — a type that will no
   longer be reachable — re-pointing it at the folded-in member.
7. **[body 6] Re-point the consumers.** Update the `using` / `@using` in every enumerated consumer,
   using `_ViewImports.cshtml` where that is the cleaner single point for the Razor files, and
   re-point `Intake/Details.cshtml.cs` and `Mail/Message.cshtml.cs` at the folded-in map rather than
   leaving either local copy behind.
8. **[body 7] Wire the references.** Add a project reference so the decided home is reachable from
   `src/Pegasus.Web`, and confirm `src/Pegasus.Desktop` can reference it **without** pulling
   `Pegasus.Infrastructure`, EF Core or ASP.NET. [[FND-037]]'s (plan handle `DSK-02-12`)
   dependency-direction facts must stay green.
9. **[body 8] Unit tests in the new home's test project** (`tests/Pegasus.Core.Tests` if it lands in
   Core, otherwise the Contracts test project scaffolded by [[FND-038]] (plan handle `DSK-02-13`)):
   every enum value in each mapped Core enum resolves to a label; an unmapped value **fails loudly**
   rather than returning a raw `ToString()`; the settled status vocabulary strings match
   `docs/design/README.md` exactly including casing; and every `IntakeDecision` value —
   `OcrRequired` and `TechnicalFailure` included — resolves through **one** map to the word the
   binding table gives.
10. **[body 9] The architecture fact.** In `tests/Pegasus.ArchitectureTests`, in the reflection
    style of `DependencyDirectionTests.cs` (520 lines): exactly one `OperatorLabels`-shaped
    vocabulary type exists in the solution, and no page-local decision-to-label map survives. The
    failure message names the offending type and points at this ticket.
11. **[body 10] Run the canonical commands and check what changed.** The existing web tests must be
    green, and the **only** edited assertions are the ones asserting the two reconciled words. Any
    other edited assertion is evidence the move changed behaviour — stop and investigate.
12. **[body 11] Documentation, simplification, PR.** Update `docs/current-architecture.md` with the
    shared-vocabulary assembly row; record the decided home in the `OperatorLabels` row of
    `docs/desktop/05-implementation-and-migration/reuse-map.md` and note that the page-local decision
    maps were folded in; run the simplification pass over the branch diff under a dated
    `## Simplification pass` heading; open the PR into `dev`.

## Verification

Evidence tiers from the body: **1** (Static/build/architecture), **2** (Core/domain),
**5** (Web/API/MCP caller).

- `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — succeeds with
  `TreatWarningsAsErrors=true`.
- `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"` —
  every existing test passes with no edited assertion beyond the two reconciled words, plus the new
  vocabulary facts.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`
  — dependency-direction facts green and the single-vocabulary fact passes.
- `grep -rn "OperatorLabels" src/ --include=*.cs --include=*.cshtml` — every hit resolves to the
  single new home; none references `Pegasus.Web.Presentation.OperatorLabels`.
- `grep -rn "Document text required\|Technical failure" src/` — **the ticket expects no output, and
  as written this command will still return one line.** At `bbd1c549` it returns five hits:
  `Pages/Intake/Details.cshtml.cs:357`, `:358`, **`:598`**, and `Pages/Mail/Message.cshtml.cs:1019`,
  `:1020`. Four of them are the two `IntakeDecision` maps and disappear at step 4. The fifth,
  `:598`, is `VrmRecognitionOutcomeKind.TechnicalFailure => "Technical failure"` — a different enum
  whose wording the binding table does not govern (step 5). **Run the narrowed command instead and
  record why:**
  `grep -rn "Document text required" src/` — expected: no output; and
  `grep -rn "IntakeDecision.TechnicalFailure" src/` — expected: one hit, in the single map, mapping
  to `Failed`. Record the `:598` hit in the proof as a known, governed exception rather than
  silently editing it to make a grep pass.

Evidence that becomes `proof`: the build output, the two test outputs, the four grep outputs, and
the before/after/governing-line table for the two reconciled words.

## Risks / open questions

- **The duplicate-ticket overlap with [[GWY-016]]** — recorded as an **unticked item in this
  ticket's `open-questions` document**, exactly as the ticket body directs. It blocks
  `leave-preparing`, `enter-review` and `enter-done` (it never gates `leave-backlog`), and that is
  the intended behaviour: the work must not be done twice. Evidence in hand: [[GWY-016]]'s title
  already names `Pegasus.Contracts` as the home.
- **The final home** — step 3 decides it with the gateway author and records it here, as the body
  directs. It is not an open question because the body routes it to the plan; the deciding evidence
  is that `OperatorLabels.cs:1-12` imports ten `Pegasus.Core` namespaces and no ASP.NET, so the
  chosen home must reference `Pegasus.Core` either way. If the move would drag ASP.NET or EF into
  the chosen home, the choice is wrong — record and re-decide rather than adding a reference.
- **The `cref` at `:164` breaks silently under `TreatWarningsAsErrors`… or does not.** It points at
  a `Pegasus.Web` type from a file that will live in another assembly. Mitigation: step 6 fixes it
  in the same change; do not discover it in CI.
- **A third page-local map exists that the ticket body does not name** —
  `SuggestionOutcomeLabel` at `Intake/Details.cshtml.cs:593-602`, over `VrmRecognitionOutcomeKind`.
  Mitigation: step 5 folds it in **without changing its text**, and § Verification narrows the grep
  and records the exception. Editing `:598` to make the body's grep pass would be an unsanctioned
  vocabulary change — the exact regression this ticket exists to prevent.
- **The two folded maps have different default arms.** `Details.DecisionLabel` throws on an unknown
  value; `Message.OutcomeLabel` returns `"Not yet processed"`. Mitigation: step 4 keeps the
  fail-loud behaviour and makes "not yet processed" an explicit absent-argument case, so no unknown
  code silently reads as "not yet processed".
- **`InOffice`'s UTC fallback now runs on Windows too.** `OperatorLabels.cs:441-455` catches
  `TimeZoneNotFoundException` / `InvalidTimeZoneException` around
  `FindSystemTimeZoneById("Europe/London")` and falls back to UTC, documented at `:436-440` as a
  deliberate "an hour's offset beats a blank screen". Once the desktop consumes this map, that
  fallback can fire on a workstation. Mitigation: record it in the post-implementation report and,
  if the desktop needs a louder signal, raise it as a separate ticket — this ticket changes no
  behaviour.
- **Namespace collision.** Upstream `INTK-004` — whose label half this ticket carries — has **no
  fork ticket**, and the board's `INTK-004` is upstream `INTK-027`, a different ticket entirely.
  Mitigation: [[FEAT-009]] (plan handle `DSK-05-09`) § Source of truth routes all four upstream
  intake ids, and the join table is in the `HZN-001` group document `board-conventions.md`.
- **Sequencing against [[GWY-010]] (plan handle `DSK-03-10`)** — if that ticket freezes the intake
  decision codes into the Intake detail DTO first, the mismatched words are generated into every
  client. Mitigation: step 4 states the ordering; the owner of that sequencing is the gateway author
  who holds both tickets.
- **[[DUI-005]] (plan handle `DSK-06-05`) is blocked by this ticket** and binds every desktop state
  and date through this list, so a delay here delays the first UI slice.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
