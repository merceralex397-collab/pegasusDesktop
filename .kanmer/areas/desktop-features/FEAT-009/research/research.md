# Research — FEAT-009: the Received item surface, its nine commands and its three byte reads

## Question

What does `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` and its three byte
pages actually do today — handler by handler, parameter by parameter — and which
of that behaviour is business logic that must move into `Pegasus.Core` before a
native Received item screen can consume it through `/api/v1`?

## Current behaviour

Read at fork `main` `191ddf33` (planning baseline, `docs/desktop/README.md`).
The implementer re-reads and records the SHA characterized (ticket step 2);
the line numbers below are the values measured on 2026-08-24.

| Surface | `path:line` | What it does |
| --- | --- | --- |
| Detail read | `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:95` `OnGetAsync` | Loads the receipt through Core `GetIntake` |
| Retry allocation | `…/Details.cshtml.cs:111` `OnPostRetryAllocationAsync` | Core `IAllocateIntake` |
| Block | `…:157` `OnPostBlockAsync` | reasoned block command |
| Re-evaluate | `…:178` `OnPostReevaluateAsync` | re-evaluation command |
| Correct draft | `…:192` `OnPostCorrectDraftAsync` | typed-draft correction |
| Claim case lease | `…:240` `OnPostClaimCaseLeaseAsync` | `IAcquireCaseEditLease` |
| Link case | `…:274` `OnPostLinkCaseAsync` | `ILinkIntake` |
| Reverse case link | `…:310` `OnPostReverseCaseLinkAsync` | `IReverseIntakeLink` |
| Register vehicle images | `…:513` `OnPostRegisterImageIntakeAsync` | image-intake registration |
| Dismiss suggestion | `…:535` `OnPostDismissSuggestionAsync` | suggestion dismissal |
| Source bytes | `src/Pegasus.Web/Pages/Intake/Source.cshtml.cs` (78 lines) | Core `DownloadIntakeSource` |
| Asset bytes | `src/Pegasus.Web/Pages/Intake/Asset.cshtml.cs` (80 lines) | per-receipt asset read |
| Image bytes | `src/Pegasus.Web/Pages/Intake/Image.cshtml.cs` (79 lines) | per-receipt image read |

Parity-matrix rows that cover this: **`PAR-19`** (the detail page and its
handler list) and **`PAR-20`** (the three byte pages), both
`docs/desktop/01-inventory-and-parity/parity-matrix.md`, both currently
`inventoried`. The matrix holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → 46),
all keyed to page models under `src/Pegasus.Web/Pages/**`.

## Findings

### Facts

Each verified by reading the file named.

- **The page model exposes nine POST handlers plus `OnGetAsync`** —
  `grep -n "public async Task<IActionResult> On" src/Pegasus.Web/Pages/Intake/Details.cshtml.cs`
  returns exactly the ten lines tabulated above (`:95` plus the nine command
  lines). The nine commands are the complete action set this slice delivers, and
  the ticket's Why line counts the same ten handlers ("ten handlers at … plus
  `OnGetAsync` at `:95`"). Nothing named in the ticket's What is missing from
  that list, and nothing in that list is absent from the What.
- **The endpoint map already names all nine as explicit routes** —
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Intake (received
  items), uploads, image intake` splits them across two rows: six on
  `POST /received/{id}/retry-allocation|block|reevaluate|correct-draft|dismiss-suggestion|register-image-intake`
  and three on `POST /received/{id}/case-lease/claim|link-case|reverse-case-link`.
  The second row is the one that additionally carries the case `expectedVersion`
  and `editLeaseToken`.
- **`ListIntake` is at `src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:5` and
  `GetIntake` at `:43`**, and both open with
  `StaffAuthorization.Require(query.Actor, StaffAccessRight.PerformCasework)`
  (`IntakeQueryUseCases.cs:16`, `:53`). `StaffAccessRight.PerformCasework` is
  declared at `src/Pegasus.Core/Identity/StaffAuthorization.cs:10`.
- **`ListIntake` bounds paging in Core**: page `1…10_000`, page size `1…100`
  (`IntakeQueryUseCases.cs:16-32`). The desktop list must not exceed either.
- **`AllocateIntake` is the one Core owner of initial allocation, durable
  failure and reasoned staff retry** — its own summary comment says so at
  `src/Pegasus.Core/Intake/IntakeAllocation.cs:199-201`; the class declaration
  spans `:203-208` (the ticket cites `:208`, the closing `: IAllocateIntake`
  line of the primary constructor).
- **`LinkIntake` is declared at `src/Pegasus.Core/Intake/DurableIntake.cs:1106`
  and its `: ILinkIntake` line is `:1109`** — exactly the reference the ticket
  and the endpoint map both give. It takes `IIntakeMutationStore`,
  `IImageIntakeCasePairing` and `TimeProvider`, and its first act is
  `IntakeCommandValidation.RequireStaffMutation(request.ReceiptId,
  request.ExpectedIntakeVersion, request.Actor, request.OperationKey, …)`
  (`DurableIntake.cs:1116-1121`) — so receipt version, actor and operation key
  are all Core preconditions, not web-layer courtesy checks.
- **`DownloadIntakeSource` validates the content hash in fixed time** —
  `src/Pegasus.Core/Intake/DownloadIntakeSource.cs:40` computes
  `Convert.ToHexString(SHA256.HashData(content.Span))` and `:43` compares it with
  `FixedTimeHashEquals(actualHash, sourceAsset.ContentHash)`. A byte endpoint
  that streams without going through this use case would drop the integrity
  check, which is why the gateway keeps calling it.
- **The decision-label map lives in the page model, twice.**
  `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:350-361` (`DecisionLabel`) and
  `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:1014-1023` are two copies of the
  same `IntakeDecision` → operator-string switch. They already disagree with the
  binding table at `docs/design/README.md:535-546`: the design table says
  `OcrRequired` → "Needs text extraction" and `TechnicalFailure` → "Failed",
  while both page models say "Document text required" and "Technical failure".
  **That reconciliation is [[FEAT-023]] (plan handle `DSK-05-23`)'s, not this
  slice's** — this slice renders through that ticket's single `OperatorLabels`
  list and changes no label text.
- **`OperatorLabels` exists today at
  `src/Pegasus.Web/Presentation/OperatorLabels.cs`** and is already the source of
  `SourceChannelLabel` (`Details.cshtml.cs:364-365`) and of the upload size label
  (`src/Pegasus.Web/Pages/Upload.cshtml.cs:32-33`). Its relocation to
  `Pegasus.Contracts` is [[GWY-016]] (plan handle `DSK-03-16`) with the final home
  decided in [[FEAT-023]] (`docs/desktop/05-implementation-and-migration/README.md:145-149`).
- **Intake work runs unattended in the Worker.** `ProcessQueuedIntake` is
  declared at `src/Pegasus.Core/Intake/DurableIntake.cs:418` and takes
  `ICreateTriageFromIntake createTriage` at `:423`;
  `CreateTriageIfQualifyingAsync` is at `:893` and is that interface's only
  caller today. Nothing in this slice runs on that path.
- **The banned-word list is a merge rule, not a CI check.**
  `docs/design/README.md:412-421` bans `intake`, `artifact`, `durable`, `bytes`
  (among others) from operator-facing copy and says in its own words that
  "nothing in CI enforces it today". The approved necessary copy this screen
  needs is at `docs/design/README.md:402` ("Blocked — a reason is required.") and
  `:404` ("No case or reference was created; review the missing or conflicting
  evidence.").
- **The screen spec exists and names the AutomationId families** —
  `docs/desktop/06-ui-design/screen-specs.md:271-285` § `Received item (intake
  receipt detail)`: tabs Evidence / Draft / Decision / Case / History,
  `Received.Header.<Field>`, `Received.Tabs.<Tab>`, `Received.Actions.<Action>`,
  and the rule that the typed draft is read-only here and editable only on Case
  create.
- **The existing test evidence is real and large** —
  `tests/Pegasus.IntegrationTests/MultiFormatIntakeWebTests.cs` (1,429 lines),
  `QdosIntakeWebTests.cs` (368), `LocalIntakeAccessTests.cs` (184),
  `IntakeStablePersistenceTests.cs` (138). `tests/Pegasus.Core.Tests/Intake/`
  exists as a folder today.
- **None of the projects this slice writes into exist yet.**
  `ls src` returns `Pegasus.Core Pegasus.Infrastructure Pegasus.Web
  Pegasus.Worker`; `ls tests` returns `Pegasus.ArchitectureTests
  Pegasus.Core.Tests Pegasus.IntegrationTests`. `src/Pegasus.Contracts`,
  `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`,
  `tests/Pegasus.Api.ContractTests` and `tests/Pegasus.Desktop.ViewModelTests` are
  all created by named earlier tickets — see the `files` document.
- **`vertical-slices.md` § S9's "Absorbs upstream" line is wrong for three of
  its four ids.** `docs/desktop/05-implementation-and-migration/vertical-slices.md:369-373`
  reads "INTK-001 …, INTK-027 …, INTK-033 …, INTK-004 …". The ticket body's
  Source-of-truth list is the correct routing and this research is written to it.

### Assumptions

- **A-05-09-1 — [[GWY-010]] (plan handle `DSK-03-10`) lands the nine command
  routes and the three byte routes in the shapes the endpoint map states.**
  Confirmed by: reading the generated client after [[GWY-010]] merges (ticket
  step 4). Breaks if: a command is folded into a dispatcher or a byte route drops
  `ETag`/range — in which case this slice stops and raises it on [[GWY-010]]
  rather than working around it.
- **A-05-09-2 — the link and reverse-link integrity checks are page-model
  rules, not Core rules.** `docs/desktop/05-implementation-and-migration/README.md:163-170`
  lists "intake draft correction and link/unlink integrity checks (S9, S10)" as a
  characterization gap to close before the slice that moves them, which is this
  one. Confirmed by: reading `Details.cshtml.cs:274-345` in full and finding the
  check there rather than in `DurableIntake.cs`. Breaks if: the rule is already
  in Core — then nothing moves and the characterization tests simply pin what
  Core already does.
- **A-05-09-3 — the re-evaluation preconditions can be characterized as they
  behave today without encoding the transient-staging defect as intended.**
  Confirmed by: writing a test that names the defect and asserts the current
  (wrong) outcome with a comment pointing at upstream INTK-027 (board
  [[INTK-004]]). Breaks if: the current behaviour is non-deterministic — then the
  characterization records the range and the ticket says so.
- **A-05-09-4 — the reviewed corpus cohort used by `MultiFormatIntakeWebTests.cs`
  is available locally at implementation time.** Confirmed by: running the tier-8
  comparison (step 11). Breaks if: it is not — then the tier-8 evidence cannot be
  produced and the ticket cannot reach `UAT passed`; that is a stop-and-raise, not
  a substitution with synthetic material (L-02: never an Azure test resource).

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | The receipt is shared state with an optimistic version: `IntakeCommandValidation.RequireStaffMutation(… ExpectedIntakeVersion …)` at `src/Pegasus.Core/Intake/DurableIntake.cs:1116-1121`. Lands in the gateway (L-01, ADR-0103). |
| Unattended execution — must it run with every desktop closed? | **yes** | Queued intake processing is `ProcessQueuedIntake` (`DurableIntake.cs:418`), executed by `src/Pegasus.Worker`. Lands in the existing Worker (ADR-0106) — not in this slice, which only reads its outcome. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The artifact store credential behind `IIntakeArtifactStore` (`DurableIntake.cs:420`) and the Graph credential behind the mailbox poll. Lands behind the gateway and Worker (ADR-0106, ADR-0107); the desktop reaches bytes only through `/api/v1`. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing external calls back into this surface. The one anonymous external intake path, `src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs`, is a separate page recorded as `legacy path retained` (`parity-matrix.md` `PAR-31`) and is out of this ticket's scope. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | `StaffAuthorization.Require(…, StaffAccessRight.PerformCasework)` (`StaffAuthorization.cs:10`, applied at `IntakeQueryUseCases.cs:16`), operation-key replay and the SHA-256 integrity check at `DownloadIntakeSource.cs:40-43` must hold whatever the client is. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement in this repository supports rendering the review surface or buffering the bytes centrally; the opposite requirement is recorded (proposal §15.2, streaming with progress and cancel). |

Conclusion: four "yes" answers place the commands, the byte broker and the
audit in the gateway (L-01), and the unattended half in the existing Worker
(ADR-0106). The rendering, the streaming client and the command gating belong in
the desktop. No responsibility moves to a new Azure resource, and this ticket
performs no Azure write.

## Implications

- **Nine explicit commands, one endpoint each.** There is no dispatcher to
  replace here (unlike [[FEAT-011]], plan handle `DSK-05-11`) — the page model is
  already one handler per action, so the desktop mirrors it one command object
  per action and the plan simply pins each to its `/api/v1` route.
- **Three of the nine are lease-bearing.** Claim case lease, link case and
  reverse case link additionally carry the case `expectedVersion` and the
  `editLeaseToken` from the session [[FEAT-005]] (plan handle `DSK-05-05`) owns.
  The other six carry only the receipt `expectedVersion`, an `operationKey` and,
  where Core requires it, a `reason`.
- **The byte path is one implementation, and later tickets reuse it.**
  [[FEAT-011]] step 9 and [[FEAT-012]] (plan handle `DSK-05-12`) step 8 both
  explicitly reuse this slice's streaming service. Building it as a
  screen-private helper would break both.
- **Two upstream defects are deliberately not fixed here.** upstream INTK-027
  (board [[INTK-004]]) is a live defect on the `re-evaluate` action this screen
  exposes, and upstream INTK-033 (board [[INTK-007]]) closes the composition gate
  behind [[FEAT-011]] and [[FEAT-012]]. Both are out of this slice's scope
  boundary (`src/Pegasus.Infrastructure` and `src/Pegasus.Worker` are forbidden),
  and **neither arrives by upstream sync** — INTK-027 is `backlog` upstream with
  no branch, INTK-033 sits at `review` on the unmerged branch
  `task/intk-033-triage-from-intake` (`7b43ab17`), outside [[FND-023]] (plan
  handle `DSK-01-10`)'s pinned range.
- **Label text is not this ticket's.** Rendering goes through [[FEAT-023]]'s
  single `OperatorLabels` list; the `OcrRequired` / `TechnicalFailure`
  disagreement with `docs/design/README.md:535-546` is that ticket's stated
  exception to reconcile.
- **The tier-8 corpus run stays local.** L-02 forbids an Azure test resource;
  the corpus material and detailed evidence are never committed and only the
  pass/fail table reaches the proof.

## Open questions

None that block. Two points are recorded rather than asked, because each has a
named owner or a trivial default:

- The ticket's acceptance line says "All ten actions"; the dispatcher, the
  endpoint map and the ticket's own What all enumerate **nine** named commands
  plus the detail read. The default taken is: deliver every action the body names
  by name (all nine), and treat the detail read as the tenth handler the Why line
  counts. Nothing named in the body is dropped.
- The `OcrRequired` / `TechnicalFailure` label disagreement is [[FEAT-023]]'s
  scope boundary, not an open question here (§ Implications above).
