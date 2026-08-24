# Research — FEAT-010: the mail workspace, its seven message handlers and the three sub-slices

## Question

What do `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs` and
`src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` — the largest page model in the
repository — actually do, which versions does each command carry, and how does
the split into S10a / S10b / S10c fall out of that evidence rather than being
imposed on it?

## Current behaviour

Read at fork `main` `191ddf33`. The implementer re-reads after the latest
upstream sync and records the SHA characterized (ticket step 3) — upstream
MAIL-011 and MAIL-012 fixes arrive through the one-way sync.

| Surface | `path:line` | What it does |
| --- | --- | --- |
| Mail list | `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:69` `OnGetAsync` | 428-line page model over Core `ListRetainedMail` and `GetRetainedMailFreshness` |
| Preview (JSON) | `…/Mail/Index.cshtml.cs:158` `OnGetPreviewAsync` | Inert body preview through `src/Pegasus.Web/Presentation/MailBodyPresentation.cs` (43 lines) |
| Message detail | `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:157` `OnGetAsync` | 1,025-line page model over Core `GetRetainedMail` |
| Prepare link | `…/Message.cshtml.cs:199` `OnPostPrepareLinkCaseAsync` | Returns what the confirmation must state |
| Prepare unlink | `…:260` `OnPostPrepareUnlinkCaseAsync` | Same, for unlink |
| Link case | `…:318` `OnPostLinkCaseAsync` | `IAcquireCaseEditLease` + `ILinkIntake` |
| Unlink case | `…:383` `OnPostUnlinkCaseAsync` | `IReverseIntakeLink` |
| Correct classification | `…:448` `OnPostCorrectClassificationAsync` | `CorrectRetainedMailClassification` |
| Move to recommended folder | `…:511` `OnPostMoveToRecommendedFolderAsync` | `MoveRetainedMailFolder` |

Parity-matrix rows: **`PAR-21`** (list and preview) and **`PAR-22`** (the message
page), `docs/desktop/01-inventory-and-parity/parity-matrix.md`, both
`inventoried`. The matrix holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **`Message.cshtml.cs` is 1,025 lines and has exactly seven handlers** —
  `wc -l` and
  `grep -n "public async Task<IActionResult> On" src/Pegasus.Web/Pages/Mail/Message.cshtml.cs`
  return the seven lines tabulated above. `Index.cshtml.cs` is 428 lines with two.
  These are "the two giants" named at
  `docs/desktop/05-implementation-and-migration/README.md:261-267`, which says in
  its own words that they are "split into sub-slices (S10a list/preview, S10b
  message/link-unlink, S10c classify/move …) and never landed as one PR".
- **The move command carries four versions and a reason, not one.**
  `Message.cshtml.cs:525-533` builds its request from
  `ExpectedClassificationVersion`, `ExpectedRecommendationPolicyKey`,
  `ExpectedRecommendationPolicyVersion`, `ExpectedMailboxVersion`,
  `MoveOperationKey` and `Reason`. The endpoint map's `move-to-recommended-folder`
  row says "classification/recommendation/mailbox versions, `operationKey`,
  `reason`" — the same set, with the recommendation half being a **key plus a
  version**, which is easy to miss.
- **The move has three outcomes, not two.** `Message.cshtml.cs:541-546` switches
  `RetainedMailFolderMoveOutcome` over `Succeeded`, `Failed` and a default
  "uncertain" branch whose operator sentence is "The move result is uncertain.
  Retry this same confirmation to check its current location." An uncertain move
  is retried with **the same** confirmation — the operation key is the safety, so
  the desktop must not mint a new key on retry.
- **Provider-absent is a composition fact, not a runtime flag.**
  `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:134` declares
  `UnavailableRetainedMailFolderMover`, and `:72` declares
  `EmptyRetainedMailFolderMoveStore`. When that is what is composed, the control
  is **absent**, not disabled with an explanation — `docs/design/README.md`
  § No explanatory copy is a merge rule with the same force as the banned-word
  list.
- **Deleted Items search is capped in Core at 100.**
  `src/Pegasus.Core/Intake/DeletedMailSearch.cs:54` declares `SearchDeletedMail`
  and `:56` holds `internal const int MaximumMessages = 100`; `:86` additionally
  bounds `pageSize` to `1…100`. The cap is a Core constant, so the desktop states
  it honestly rather than implying completeness.
- **The four retained-mail use cases are in one file.**
  `src/Pegasus.Core/Intake/RetainedMail.cs` holds `CorrectRetainedMailClassification`
  (`:267`), `ListRetainedMail` (`:386`), `GetRetainedMail` (`:480`) and
  `GetRetainedMailFreshness` (`:641`); `MailClassificationConcurrencyException` is
  at `:195`. `SearchDeletedMail` is the exception — it lives in
  `DeletedMailSearch.cs`, not in `RetainedMail.cs`.
- **The unlink sentence is approved necessary copy and is exact.**
  `docs/design/README.md:408` reads `Unlinking this email cancels case
  <reference>.` — it appears again in the enumerated list at `:1291`. The screen
  spec repeats it at `docs/desktop/06-ui-design/screen-specs.md:259-261`.
- **The approved mockups exist.**
  `ls docs/design/references/mockups/inbox-message-page/` returns `Main.dc.html`,
  `Case.dc.html`, `CaseLinked.dc.html`, `Correcting.dc.html`, `Dialogs.dc.html`,
  `Filed.dc.html`, `FolderStates.dc.html`, `canvas.json` and `README.md`. The
  screen spec at `screen-specs.md:248-269` describes the same design: head band
  where the subject wraps and never truncates, four tabs Message · Attachments n ·
  Thread · Case, a Decision card in the right column, and **no Open case button —
  "Filed to" is the link**.
- **The Automation Actor half already exists in part.**
  `src/Pegasus.Web/Mcp/MailMcpTools.cs` exists today and declares four
  `McpServerTool`s (`grep -c "McpServerTool"` → 4). upstream AUTO-003 (board
  [[AUTO-001]]) adds `pegasus_mail_move_to_recommended_folder`,
  `pegasus_mail_case_link` and `pegasus_mail_case_unlink` over the **same** Core
  use cases this slice's endpoints call.
- **`vertical-slices.md` § S10's "Absorbs upstream" line names AUTO-003**
  (`docs/desktop/05-implementation-and-migration/vertical-slices.md:404-407`).
  Correcting that line is [[AUTO-001]] step 11's, and must not be corrected a
  second time here.
- **The existing test evidence is large.**
  `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` (2,045 lines),
  `RetainedMailPersistenceTests.cs` (1,696), `Browser/MailWorkspaceBrowserTests.cs`
  (150). `src/Pegasus.Web/Presentation/MailClassificationSelection.cs` is 102
  lines and `MailBodyPresentation.cs` is 43.
- **The projects this slice writes into do not exist yet.** `ls src` returns only
  `Pegasus.Core Pegasus.Infrastructure Pegasus.Web Pegasus.Worker`; `ls tests`
  only `Pegasus.ArchitectureTests Pegasus.Core.Tests Pegasus.IntegrationTests`.

### Assumptions

- **A-05-10-1 — [[GWY-012]] (plan handle `DSK-03-12`) and [[FEAT-029]] (plan
  handle `DSK-07-03`) land the mail endpoints in the endpoint map's shapes.**
  Confirmed by: reading the generated client at step 3. Breaks if: the move
  endpoint carries fewer than the four versions the page model sends — then a move
  can succeed against a stale recommendation, which is the concurrency hole the
  version set exists to close. Stop and raise there.
- **A-05-10-2 — the provider-absent state is observable from the response, not
  inferred.** Confirmed by: a contract test asserting the response distinguishes
  "move not available" from "move failed". Breaks if: the gateway returns a
  generic failure — then the desktop cannot tell absent from failed, and would
  have to guess, which the design authority forbids.
- **A-05-10-3 — the mail parity comparison (step 12) can run on the local
  Test/UAT stack with real retained mail.** `docs/desktop/08-testing/test-uat-stack.md`
  is the stack definition. Breaks if: no retained mail is available locally — the
  tier-12 evidence then cannot be produced, and asking for an Azure test
  environment is out of bounds (L-02, ADR-0014).
- **A-05-10-4 — the three sub-slices can each stand alone on `dev`.** S10a ships
  a list and a preview with no commands; S10b adds link/unlink; S10c adds
  classify/move. Confirmed by: each PR merging green on its own. Breaks if: a
  shared view-model member forces them together — in which case the shared member
  lands in S10a and the later slices extend it in place.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Classification, recommendation and mailbox versions are all optimistic-concurrency tokens on shared state (`Message.cshtml.cs:525-533`), and `MailClassificationConcurrencyException` (`RetainedMail.cs:195`) exists because two staff can correct the same message. Lands in the gateway (L-01). |
| Unattended execution — must it run with every desktop closed? | **yes** | The mailbox poll retains mail continuously; `GetRetainedMailFreshness` (`RetainedMail.cs:641`) exists precisely to tell an operator how stale the retained set is. Lands in the existing `src/Pegasus.Worker` (ADR-0106) — not in this slice. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The Microsoft Graph credential. Lands behind the gateway and Worker (ADR-0106); the desktop calls only `/api/v1` and never holds a Graph token. |
| Public callback — must an external service call a stable public endpoint? | **no** | Graph is polled, not called back. Nothing external calls into this surface. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | `StaffAccessRight.PerformCasework` (`src/Pegasus.Core/Identity/StaffAuthorization.cs:10`), the Deleted-Items cap of 100 (`DeletedMailSearch.cs:56`), the four-version check and the move audit must hold whatever the client is. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement in this repository supports rendering the workspace centrally. The one measured constraint pushes the other way: the preview must render inert text with no remote content, which is a client-side rendering rule. |

Conclusion: four "yes" answers place the retained set, the four commands, the
Deleted-Items cap and the audit in the gateway (L-01), and the mailbox poll in
the existing Worker (ADR-0106). Rendering, scoping, preview inertness and the
confirmation dialogs belong in the desktop. No new Azure resource, and no Azure
write in this ticket.

## Implications

- **The split is evidence-led, not stylistic.** S10a is everything
  `Index.cshtml.cs` does (list, freshness, preview); S10b is
  `Message.cshtml.cs:157-445` (detail plus the two prepare/confirm pairs); S10c is
  `Message.cshtml.cs:448-565` (classification correction and the folder move).
  The seam falls between handler 4 and handler 5 of the seven.
- **Prepare-then-confirm is two round trips by design.** The prepare step
  returns what the confirmation must state; the confirm step carries the message
  and receipt versions, the case `expectedVersion` and the `editLeaseToken` from
  the [[FEAT-005]] (plan handle `DSK-05-05`) session. A desktop that skips prepare
  and composes its own sentence would drift from the approved copy.
- **The move's third outcome drives the retry rule.** Because "uncertain" is
  retried with the *same* confirmation (`Message.cshtml.cs:544-545`), the desktop
  must keep the operation key stable across a retry of an uncertain move. Minting
  a fresh key would turn a check into a second move.
- **Absent, not disabled.** When the provider port is
  `UnavailableRetainedMailFolderMover`, the move control does not render at all.
  The screen spec's "rows/actions only when populated and available" rule
  (`screen-specs.md:255-257`) says the same thing.
- **The Automation Actor path is a rival, not a dependency.** [[AUTO-001]]
  (upstream AUTO-003) adds MCP tools over the same Core use cases. Any overlap is
  raised there rather than built here, and the `vertical-slices.md:404-407`
  correction is [[AUTO-001]] step 11's alone.

## Open questions

None that block. Both points that could look like questions have named owners:

- The `vertical-slices.md` § S10 "Absorbs upstream" line is [[AUTO-001]] step 11's
  correction to make — a scope boundary, not a question.
- Whether the gateway's move response distinguishes absent from failed is
  [[GWY-012]]'s contract to state; if it does not, this slice stops and raises it
  there (assumption A-05-10-2), which is a stop condition rather than an
  unanswered question.
