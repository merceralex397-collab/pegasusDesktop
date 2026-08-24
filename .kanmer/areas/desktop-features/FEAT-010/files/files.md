# Files — FEAT-010

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today — `ls src` returns only `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`; `ls tests` only
`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

The **S10a / S10b / S10c** column says which sub-slice's PR touches each file.
Nothing lands as one PR (`docs/desktop/05-implementation-and-migration/README.md:261-267`).

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`)* | Mail DTOs. **S10a**: list item, freshness, preview. **S10b**: message detail (thread, attachments, classification, queue, outcome, association, move result, suggested move) and the prepare/confirm request-response pairs for link and unlink. **S10c**: classification-correction and folder-move requests, the latter carrying **four** version fields — classification, recommendation key, recommendation version, mailbox. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]], plan handle `DSK-02-05`)* | **S10a** `MailListViewModel` + list/preview XAML. **S10b** `MailMessageViewModel` + the message page to `docs/desktop/06-ui-design/screen-specs.md:248-269` (four tabs, Decision card, no Open case button). **S10c** the classification and move commands added to `MailMessageViewModel` in place. AutomationIds are fixed by the spec: `Inbox.Scope.Mailbox`, `Inbox.Scope.Folder`, `Inbox.Table`, `Inbox.Row.<Id>`, `Message.Tabs.<Tab>`, `Message.Decision.Correct`, `Message.Decision.Move`, `Message.Case.Search`, `Message.Case.Link`, `Message.Case.Unlink`. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]], plan handle `DSK-02-06`)* | **S10a** the coalesced manual-refresh call to `POST /api/v1/mail/refresh`. Coalescing lives here, not in the view model, so a double click does not become two refreshes. |
| `src/Pegasus.Web/` — the `/api/v1` mail group only | Only where [[GWY-012]] (plan handle `DSK-03-12`) or [[FEAT-029]] (plan handle `DSK-07-03`) left a gap this slice must close to consume its own contract. The group sits behind `Features:DesktopGateway` ([[GWY-002]], plan handle `DSK-03-02`). |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`)* | Per sub-slice: **S10a** list scoping, freshness, preview, the Deleted-Items cap; **S10b** prepare/confirm for link and unlink; **S10c** classification version handling, the four-version move, provider-absent behaviour. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[TEST-004]], plan handle `DSK-08-04`)* | Scoping and freshness, preview inertness, prepare-then-confirm flows, the exact unlink sentence, classification versioning, and the absent-move-control case. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]], plan handle `DSK-08-06`)* | **S10b** `winapp ui` dialog scripts for the link and unlink confirmations, using the dialog contract from [[DUI-009]] (plan handle `DSK-06-09`); plus the `axe-windows` scan from [[TEST-009]] (plan handle `DSK-08-09`) on the list and message screens. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Rows `PAR-21` (list, preview) and `PAR-22` (message page) advance from `inventoried`. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by [[DUI-013]], plan handle `DSK-06-13`)* | Mail section, citing FRD-08. The file does not exist today (`ls docs/frd` shows `frd-01`…`frd-12`). |
| `docs/capabilities.md` | `DSK` rows for the mail workspace. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:511-565` | The move handler. It sends **four** version fields plus an operation key and a reason (`:525-533`), and its outcome switch (`:541-546`) has a third branch: "The move result is uncertain. Retry this same confirmation to check its current location." An uncertain move is retried with the **same** operation key — minting a new one turns a check into a second move. |
| `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:199-445` | The prepare/confirm pairs. The prepare step is what supplies the confirmation's content; a desktop that composes its own sentence drifts from the approved copy immediately. |
| `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:158` | `OnGetPreviewAsync` returns JSON built by `Presentation/MailBodyPresentation.cs`. Note that `docs/desktop/03-gateway-api-and-data/endpoint-map.md` cites this handler as `:176`; the measured line is `:158`. Trust the code. |
| `src/Pegasus.Web/Presentation/MailBodyPresentation.cs` (43 lines) | How the preview is made inert. The desktop renders text only and loads no remote content — this file is the precedent for what "inert" means here. |
| `src/Pegasus.Web/Presentation/MailClassificationSelection.cs` (102 lines) | How the correction UI chooses a classification today, and which versions it carries alongside. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:195,267,386,480,641` | `MailClassificationConcurrencyException`, `CorrectRetainedMailClassification`, `ListRetainedMail`, `GetRetainedMail`, `GetRetainedMailFreshness`. The concurrency exception at `:195` exists because two staff can correct the same message — it is the 409 the desktop must render through the [[FEAT-008]] (plan handle `DSK-05-08`) pattern. |
| `src/Pegasus.Core/Intake/DeletedMailSearch.cs:54,56,86` | `SearchDeletedMail`, `MaximumMessages = 100`, and a page-size bound of `1…100`. The cap is a Core constant, so the desktop states it honestly rather than implying a complete search. **Note it is not in `RetainedMail.cs`.** |
| `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:72,134` | `EmptyRetainedMailFolderMoveStore` and `UnavailableRetainedMailFolderMover`. Provider-absent is a *composition* fact, so the move control is **absent**, not disabled with an explanation. |
| `docs/design/README.md:408` (and the enumerated list at `:1291`) | `Unlinking this email cancels case <reference>.` — approved consequence copy. Verbatim, with no explanatory text around it. |
| `docs/design/README.md:412-421` | The banned-word list — `intake`, `lease`, `artifact`, `durable`, `bytes` among them — with the file's own statement that nothing in CI enforces it. It is a merge rule. |
| `docs/desktop/06-ui-design/screen-specs.md:248-269` | The Inbox spec: head band where the subject wraps and never truncates, four tabs, the Decision card, "rows/actions only when populated and available", **no Open case button — Filed to is the link**, and the ten AutomationIds. |
| `docs/design/references/mockups/inbox-message-page/` | The approved mockups — `Main`, `Case`, `CaseLinked`, `Correcting`, `Dialogs`, `Filed`, `FolderStates`. `Dialogs.dc.html` is the one to read before writing either confirmation. |
| `docs/desktop/05-implementation-and-migration/README.md:261-267` | "The two giants" trap, in the plan's own words: `Message.cshtml.cs` (1,025) is split S10a/S10b/S10c and never landed as one PR. |
| `src/Pegasus.Web/Mcp/MailMcpTools.cs` | Four `McpServerTool`s today. [[AUTO-001]] — cite it as `upstream AUTO-003 (board [[AUTO-001]])` — adds three more over the **same** Core use cases these endpoints call. Read it before adding anything gateway-side, and raise overlap there rather than building a rival path. |
| `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` (2,045 lines) | The scenarios the tier-12 parity comparison runs against. There is already a harness; do not invent one. |
| `docs/desktop/08-testing/test-uat-stack.md` | The local Test/UAT stack the parity run uses. L-02 forbids an Azure test environment. |

## Ripple effects

- **OpenAPI and the generated client.** Mail DTOs in `src/Pegasus.Contracts`
  change `openapi/pegasus-v1.json` and the generated client. A DTO renamed after
  [[GWY-012]] merges breaks its contract tests too.
- **`tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` (2,045) and
  `RetainedMailPersistenceTests.cs` (1,696)** stay green — this slice changes no
  Razor mail page.
- **`Browser/MailWorkspaceBrowserTests.cs`** remains the web-side accessibility
  evidence; the desktop's equivalent is the `axe-windows` artefact from
  [[TEST-009]].
- **[[AUTO-001]]** touches `src/Pegasus.Web/Mcp/MailMcpTools.cs` over the same
  Core use cases. Overlap is raised there.
- **Three PRs, in order.** S10a → S10b → S10c, each with its own commit series,
  its own simplification pass under its own dated heading, and its own PR into
  `dev`.
- **`docs/frd/frd-13-desktop-operator-experience.md` does not exist yet** — it is
  authored by [[DUI-013]]. Contribute the mail section there if it has not landed.

## Out of scope

- **`src/Pegasus.Infrastructure/Email/`** — the Graph adapter. Credentials stay
  central (ADR-0106).
- **`src/Pegasus.Worker`** — the mailbox poll.
- **The Razor mail pages.** They stay deployable until `PAR-21` and `PAR-22`
  reach `UAT passed`.
- **The Automation Actor tools.** upstream AUTO-003 (board [[AUTO-001]]) owns
  `pegasus_mail_move_to_recommended_folder`, `pegasus_mail_case_link` and
  `pegasus_mail_case_unlink`. Do not build a second path.
- **`vertical-slices.md:404-407`'s "Absorbs upstream" line.** [[AUTO-001]] step 11
  corrects it; correcting it a second time here is forbidden by the ticket's
  Traps.
- **upstream MAIL-011 and MAIL-012.** They arrive by the one-way sync
  ([[FND-023]], plan handle `DSK-01-10`); this slice re-reads the page models
  after the latest sync and records the SHA rather than fixing forward.
- **Rendering remote HTML or loading remote content in the preview.** Text only,
  always inert.
- **Any Azure write.**
