# Research — FEAT-030: what the Operations screen must show, and the three honesty rules the web page already encodes

## Question

What exactly does `Pages/Operations/Index.cshtml.cs` render and refuse to
render, which of its properties are load-bearing honesty rules rather than
incidental code, where do the integration-health rows the screen spec names
actually come from, and which of them do **not** exist as a contract yet?

## Current behaviour

Read at fork `main` `191ddf33` on 2026-08-24. The implementer re-reads after the
latest upstream sync ([[FND-023]], plan handle `DSK-01-10`) and records the SHA.

| Surface | `path:line` | What it does today |
| --- | --- | --- |
| Load | `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:57` `OnGetAsync` | Calls `GetRequestOperations.ExecuteAsync` (`:64`) and nothing else for the read; sets `LoadedAtUtc` at `:67`, **after** the await |
| Freshness honesty | `…/Operations/Index.cshtml.cs:41-45`, `:46` | The property's own remark: "When this list was last read. Set only after the query returns, so a failed load never claims to be fresh (FRD-12)." |
| Default projection | `…/Operations/Index.cshtml.cs:47-49` | `Operations` starts as an **empty** `RequestOperationsProjection` with `LimitReached: false` — the pre-load state is empty, not null |
| Retry external work | `…/Operations/Index.cshtml.cs:71` `OnPostRetryExternalAsync` | `RetryExternalWork` with `expectedAttemptCount` (an `int`) + `operationKey`; four operator sentences at `:92-94`, `:100-102`, `:104-106` |
| Revoke upload link | `…/Operations/Index.cshtml.cs:112` `OnPostRevokeLinkAsync` | Acquires a case edit lease (`:136`), calls `IRevokeRequestUploadLink` (`:153`), then releases the lease quietly (`:171`); four operator sentences at `:128`, `:147`, `:157`, `:165` |
| Reason preservation | `…/Operations/Index.cshtml.cs:218-235` `PreserveReason` | On any revoke failure the typed reason is stashed in `TempData` (≤ 500 chars) and re-rendered, so the operator does not retype it |
| State vocabulary | `…/Operations/Index.cshtml.cs:176-187` `StateLabel` | The eight `RequestOperationState` members with their operator words; `UnknownExternal` → "Unknown external", never folded into a success word; the `_` arm **throws** |

Parity-matrix row: **`PAR-27`** — "13.10 Administration and operations · FRD-12 ·
`Operations/Index.cshtml.cs` (236) — `OnGetAsync`, `OnPostRetryExternalAsync`,
`OnPostRevokeLinkAsync`; `[ValidateAntiForgeryToken]`", verification column
`to locate`, status **`not inventoried`**
(`docs/desktop/01-inventory-and-parity/parity-matrix.md:72`). The matrix holds
`PAR-01`…`PAR-46` (`grep -c '^| PAR-' …/parity-matrix.md` → 46). The ticket's
Documentation changes moves this row to `implemented`; note that it starts from
`not inventoried`, so the row's `Verification` cell is filled here for the first
time.

## Findings

### Facts

- **The whole web page is one Core read.** `GetRequestOperations.ExecuteAsync`
  is the only query (`Index.cshtml.cs:64`). Everything the screen spec calls
  "integration health rows (Graph last successful cycle per mailbox, Box,
  DVLA/DVSA, update feed, minimum client version)" is **new surface** on this
  screen — none of it exists in `Pages/Operations/`. Its data comes from
  [[FEAT-027]] (plan handle `DSK-07-01`)'s
  `GET /api/v1/operations/intake-status` for the mailbox half, and from
  [[PLAT-015]] (plan handle `DSK-10-15`)'s `GET /api/v1/admin/health` for Box,
  DVLA/DVSA, update feed and minimum client version
  (`docs/desktop/03-gateway-api-and-data/endpoint-map.md:37` — auth right
  `ManageWorkflowConfiguration`, phase 8).
- **Two of the five health rows are gated behind a different right, and one is
  a different phase.** `GET /admin/health` is `ManageWorkflowConfiguration` and
  phase 8; the Operations screen is `PerformCasework` and phase 5. An operator
  with casework rights alone will not receive those rows. The screen spec's own
  rule settles the rendering — health rows are "absent when not composed"
  (`docs/desktop/06-ui-design/screen-specs.md:391-396`) — so the phase-5 screen
  renders the mailbox rows and omits the rest until the right and the endpoint
  exist.
- **`RequestOperationProjection` has twenty-four members**
  (`src/Pegasus.Core/Operations/RequestOperations.cs:32-56`) plus an
  `ActiveEditLease` init property (`:58`). The ones this screen binds are `Kind`,
  `State`, `CaseReference`, `PrincipalCode`, `LastActivityAtUtc`, `ExternalKind`,
  `AttemptCount`, `FailureCode`, `FailureReason`, `CanRetry`, `CanRevoke`,
  `CaseVersion`, `Version`, `CaseEditLeaseState` and
  `CaseEditLeaseExpiresAtUtc`. **`CanRetry` and `CanRevoke` are fields of the
  projection** (`:51`, `:52`), not derived — the server owns eligibility, which
  is exactly what the ticket's step 6 requires.
- **The revoke command needs a lease the screen must obtain first.**
  `OnPostRevokeLinkAsync` acquires a case edit lease with its **own** operation
  key (`Index.cshtml.cs:132-138`), calls revoke with `expectedVersion`,
  `expectedCaseVersion`, `reason`, `operationKey` and `lease.Token` (`:153`),
  then releases the lease in a swallow-everything `ReleaseQuietlyAsync`
  (`:188-206`). Two operation keys per revoke, not one. The projection already
  carries `CaseEditLeaseState` and `CaseEditLeaseExpiresAtUtc` (`:54-55`) so the
  screen can show a row whose case is already leased by someone else — the web
  page's own failure sentence for that is "This link's case is open for editing
  by someone else. Try again in a few minutes." (`:147`).
- **The four revoke sentences and the four retry sentences are approved operator
  copy.** Retry (`:92-94`, `:100-102`, `:104-106`): "External work was already
  scheduled for retry." / "External work was scheduled for retry." / "The
  external work retry request was invalid. Refresh and try again." / "The
  external work failure changed before retry. Refresh and try again." Revoke
  (`:128`, `:147`, `:157`, `:165`): "The link could not be withdrawn. Refresh
  and try again." / the lease sentence above / "The link was withdrawn." / "The
  link changed before it could be withdrawn. Refresh and try again." A native
  screen that writes new sentences forks the operator vocabulary.
- **Retry distinguishes replay from first effect in the operator's words.**
  `result.IsReplay` chooses between "already scheduled" and "scheduled"
  (`:92-94`). That distinction must survive into the desktop or the operator
  cannot tell a duplicate click from a fresh action.
- **The state vocabulary throws rather than degrading.** `StateLabel`'s default
  arm is `throw new InvalidOperationException($"Unknown request operation state
  value '{(int)state}'.")` (`:185`). A desktop `switch` that falls through to
  "Unknown" would silently absorb a new Core state the web app refuses to
  render — the opposite of the intended behaviour.
- **`src/Pegasus.Desktop` does not exist yet.** `ls src` returns
  `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`;
  `ls tests` returns `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`,
  `Pegasus.IntegrationTests`. There is no `src/Pegasus.Desktop`, no
  `src/Pegasus.Desktop.Infrastructure`, no
  `tests/Pegasus.Desktop.ViewModelTests`, no `tests/Pegasus.Desktop.UITests`,
  no `src/Pegasus.Contracts`, no `openapi/`, no `eng/` and no
  `BuildAndRun.ps1`. Every command in the ticket's step 11 and Verification
  depends on scaffolding from other tickets.
- **`tests/Pegasus.Desktop.ViewModelTests` has two claimant tickets.** The body's
  step 10 names [[TEST-004]] (plan handle `DSK-08-04`, "Scaffold
  `tests/Pegasus.Desktop.ViewModelTests`"); [[FEAT-001]]'s `files` document
  names [[FND-038]] (plan handle `DSK-02-13`, "Create
  `tests/Pegasus.Desktop.ViewModelTests` with fakes…"). Both exist on the board.
  This ticket writes tests into whichever landed and does not create the project.
- **The AutomationId convention and this screen's ids are fixed.**
  `docs/desktop/06-ui-design/screen-specs.md:31-39` gives
  `<Screen>.<Region>.<Element>[.<Key>]`; `:397-398` fixes this screen's four:
  `Operations.External.Table`, `Operations.External.Retry`,
  `Operations.Links.Revoke`, `Operations.Health.<Dependency>`. Coverage must be
  100% and the ids are not this ticket's to choose.
- **The screen spec names the table columns.** `:391-393`: "Retryable external
  work (table: kind, case, last failure, attempts, next action) with Retry
  (reasoned), active public upload links with Revoke". Note **Retry is
  reasoned** in the spec, while the web page's retry handler takes **no
  reason** (`Index.cshtml.cs:72-76`) — only revoke does (`:117`). See
  Implications.
- **The state contract is authored twice and the desktop copy adds three rows.**
  `docs/design/README.md:764-772` is the authority; `screen-specs.md:417-427`
  restates it and adds a **Desktop-specific** row: "disconnected (saves
  disabled, content visible); update required; client unsupported; compatibility
  cached/expired; transfer queued/running/failed/cancelled; draft recovered
  after abnormal exit". The disconnected state the ticket's step 9 requires is
  a named contract state, not an invention.
- **The empty-state rule is explicit and cuts against a blank table.**
  `screen-specs.md:425-427`: "a read-only section with nothing recorded and no
  available action is absent; a query that legitimately returned zero shows `0`
  in its count position. 'No results' text appears only for a search the
  operator ran." A failed load is none of those three, which is why step 9's
  disconnected state must keep the previous rows labelled with their time.
- **The FRD sentence the freshness rule implements.**
  `docs/frd/frd-12-operator-experience.md:95-99`: "Every count and query exposes
  its last successful update time and current refresh state. `0`, loading,
  current, stale-with-last-good-time, partial, unavailable, and failed are
  distinct outcomes. A refresh never replaces a last-good value with a false
  zero, merges partial data into an apparently complete result, or implies that
  an external action succeeded." And `:101-103`: "Manual refresh reruns the same
  exact filtered query; it does not change policy or create a business
  transition."
- **The no-colour-alone rule is enforced by review, not by CI.**
  `docs/desktop/06-ui-design/keyboard-and-accessibility.md:82` — "No information
  by colour alone: every chip carries text and glyph"; `:88` — forced-colours
  mapping; `:96` — "Permanent consequences visible without hover or colour".
  `docs/design/README.md:417-420` records that CI does **not** enforce the
  banned-word list either; the reviewer is the only gate.
- **The operator-copy ban list applies to every string on this screen.**
  `docs/design/README.md:170`: "Do not expose Azure, OCR, AI, queue mechanics,
  extraction engines, deployment, adapter, lease/version, projection, ingress,
  or artifact terminology in operator copy. The word 'intake' never appears in
  operator-facing text (operator decision 2026-08-04)." This screen is the most
  exposed on the board: its subject matter **is** queue mechanics. "Poison
  count", "external work", "queue", "lease" and "projection" are all words the
  underlying data uses and the screen may not.
- **The label map already solves that for failure codes.**
  `src/Pegasus.Web/Presentation/OperatorLabels.cs` (685 lines) maps
  `"queue_poisoned"` → "Processing was attempted repeatedly without completing"
  (`:340`) and owns `OfficeTime` (`:412`) / `OfficeDate` (`:426`), which resolve
  `Europe/London` at `:446`. [[GWY-016]] (plan handle `DSK-03-16`) and
  [[FEAT-023]] (plan handle `DSK-05-23`) move it to `Pegasus.Contracts`. The
  desktop consumes it; a second office-time helper or a second sentence for a
  failure code is the defect.
- **The screen has an owner collision recorded in its own Guardrails.**
  [[FEAT-020]] (plan handle `DSK-05-20`, "S20 Operations and integration
  health") is the area-05 slice over the same screen. The body settles it: this
  ticket owns `OperationsViewModel` and `OperationsPage.xaml`; [[FEAT-020]]
  extends them; "a second view model for the same screen is a stop condition".
- **`docs/frd/frd-13-desktop-operator-experience.md` does not exist yet.**
  `ls docs/frd` returns FRD-01…FRD-12 and `README.md`. The skeleton is created
  by [[FND-008]] (plan handle `DSK-00-08`, "Write FRD-13 'Desktop operator
  experience'"), and [[DUI-013]] (plan handle `DSK-06-13`) adopts the screen
  specs as its sections.

### Assumptions

- **A-07-04-1 — [[FEAT-027]] (plan handle `DSK-07-01`) and [[FEAT-028]] (plan
  handle `DSK-07-02`) have landed their contracts before this ticket starts.**
  Stated in the body's "Depends on"; [[FEAT-028]] declares `blocks: [FEAT-030]`
  on the board. Confirmed by: step 2's regeneration of the API client producing
  the intake-status, external-work and retry types. Breaks if: they have not —
  the screen has nothing to bind and this ticket waits rather than hand-writing
  DTOs (`docs/desktop/03-gateway-api-and-data/README.md` § 3 Contracts:
  "desktop never hand-writes DTOs").
- **A-07-04-2 — `GET /api/v1/operations` ([[GWY-013]], plan handle `DSK-03-13`)
  supplies the upload-links half.** The intake-status endpoint covers mailboxes
  and external work; active public upload links come from the same Operations
  projection the Razor page reads. Confirmed by: checking [[GWY-013]]'s contract
  at step 2. Breaks if: no endpoint publishes `CanRevoke` and the link fields —
  the revoke table is then out of scope for this ticket and the gap is raised on
  [[GWY-013]], not filled with a second query here.
- **A-07-04-3 — the three non-mailbox health rows (Box, DVLA/DVSA, update feed,
  minimum client version) are not available at phase 5.** They come from
  [[PLAT-015]] (plan handle `DSK-10-15`)'s `GET /api/v1/admin/health`, which
  `endpoint-map.md:37` marks phase 8 and `ManageWorkflowConfiguration`.
  Confirmed by: reading that row at step 1. Breaks if: it lands early — the rows
  are then added under the same "absent when not composed" rule, with no change
  to the rest of the screen.
- **A-07-04-4 — the desktop can acquire a case edit lease for revoke.** The web
  handler acquires one with its own operation key
  (`Index.cshtml.cs:132-138`); the desktop needs the same through [[GWY-008]]
  (plan handle `DSK-03-08`)'s lease endpoints. Confirmed by: a view-model fact
  that revoke is refused with the lease sentence when the lease cannot be
  acquired. Breaks if: those endpoints have not landed — revoke is then deferred
  and retry ships alone, which is a smaller screen, not a broken one.
- **A-07-04-5 — `winapp ui` and `AxeWindowsCLI` are available to the
  implementing agent.** Required by the body's steps 11–12 and its Verification.
  Confirmed by: [[TEST-006]] (plan handle `DSK-08-06`) having scaffolded
  `tests/Pegasus.Desktop.UITests/ui-tests.ps1` and [[DUI-015]] (plan handle
  `DSK-06-15`) the accessibility automation. Breaks if: either is missing — the
  tier-7 evidence cannot be produced and the ticket stops rather than
  substituting a screenshot.
- **A-07-04-6 — no operator-facing string on this screen needs a word from the
  banned list.** Confirmed by: the copy review at step 5 against
  `docs/design/README.md:170` and `:412-420`, and by reusing
  `OperatorLabels`. Breaks if: a state genuinely has no operator sentence — that
  is a copy decision for the design authority, not a word to invent.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered. This ticket places a **presentation** responsibility over
data and commands already placed by [[FEAT-027]] and [[FEAT-028]].

| Question | Answer | Evidence, and where a "yes" lands |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | The retryable-work and upload-link tables are one shared queue every operator acts on, and two staff can race the same row: `RetryExternalWorkCommand.ExpectedAttemptCount` (`RequestOperations.cs:159`) and the case edit lease around revoke (`Index.cshtml.cs:132-138`) exist for exactly that. **Lands in the gateway** (L-01) — the desktop reads and asks; it arbitrates nothing. |
| Unattended execution — must it run with every desktop closed? | **yes**, for the work being reported on | The queue and polling work the screen reports runs unattended: `InboxPollFunction` (`src/Pegasus.Worker/MailboxFunctions.cs:8-15`), `ExternalWorkFunction` / `ExternalPoisonFunction` (`src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs:7`, `:24`). **Lands in the existing `src/Pegasus.Worker`** (ADR-0106). **The screen itself is a "no"** — nothing about rendering a table needs to run with the desktop closed, which is why the rendering belongs here. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes**, for what is reported on | The Graph, Box and DVLA/DVSA credentials behind every health row are Key Vault references and Container App secrets (`infra/modules/platform.bicep:382-398`, `:555-563`). **Lands behind the gateway and Worker** (ADR-0107). The screen shows "a state and a last-good time, nothing more" (the ticket's own Guardrail); step 9's disconnected state and step 7's freshness states are built from that alone. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing external calls this screen. The health rows are pulled by the desktop from the gateway on a manual or coalesced refresh; there is no subscription, no push and no webhook. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Three the client cannot be trusted with: `CanRetry` / `CanRevoke` are decided in the Core projection (`RequestOperations.cs:51-52`) and guarded by invariants at `:142` and `:149`; the case edit lease is acquired server-side; and `StaffAuthorization.Require(actor, PerformCasework)` sits inside the Core read (`:87`). **Lands in the gateway** — hence step 6's rule that the desktop never infers eligibility. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement in this repository supports rendering the operations view centrally. The one measured constraint points the other way: PLAT-034's App Insights blind hours (`docs/desktop/07-integrations/README.md` § 7) are why an operator needs a client-side surface at all. |

**Conclusion.** Four "yes" answers, and every one lands somewhere that already
exists: the reads and commands in the gateway (L-01), the queue and polling work
in the Worker (ADR-0106), the provider credentials behind both (ADR-0107).
**The rendering, the freshness presentation, the enablement state, the
confirmation dialogs and the disconnected behaviour land in the desktop** —
which is this ticket. **No new Azure resource and no Azure write.**

## Implications

- **`LoadedAtUtc` is the whole screen in one line.** The web page states the
  rule in its own remark (`Index.cshtml.cs:41-45`) and enforces it by assigning
  at `:67`, after the await. The view model reproduces it by setting the
  obtained-at timestamp **only** in the success branch; on failure it keeps the
  previous rows, labels them with their earlier time, and shows the failure
  sentence. That single discipline satisfies FRD-12 `:95-99`, the state contract
  `docs/design/README.md:764-772`, and the ticket's first acceptance criterion.
- **The health rows are new surface, not parity, and most of them are phase 8.**
  Only the mailbox rows have a phase-5 contract ([[FEAT-027]]). The screen
  spec's "absent when not composed" rule (`screen-specs.md:391-396`) is the
  correct rendering for the rest, and it is also the honest one — an empty
  "Box: —" row implies a health check that is not running.
- **The spec says Retry is reasoned; the Core command takes no reason.**
  `screen-specs.md:392` says "Retry (reasoned)", but
  `RetryExternalWorkCommand` (`RequestOperations.cs:157-161`) has no `Reason`
  member and the web handler collects none (`Index.cshtml.cs:72-76`); only
  revoke does (`:117`). Adding a reason field the gateway would discard is
  worse than not asking. The plan takes the trivial default and records it:
  **revoke uses the `ReasonDialog`; retry uses a plain confirmation.** If the
  design authority wants a reason on retry, that is a Core command change and a
  different ticket.
- **Two operation keys per revoke.** One for the lease, one for the revoke
  (`Index.cshtml.cs:132`, `:153`). A view model that generates one and reuses it
  will produce a refusal the operator cannot explain.
- **The operator-copy ban list is the sharpest constraint on this particular
  screen**, because its subject matter is queue mechanics. Every string is drawn
  from `OperatorLabels` or from the eight approved sentences the web handlers
  already produce; none is written fresh. "Poison" is a word the data uses and
  the screen may not — the approved sentence for `queue_poisoned` already exists
  at `OperatorLabels.cs:340`.
- **`StateLabel` throws rather than degrading, and the desktop must too.** A
  `switch` expression with a `_ => "Unknown"` arm would silently absorb a new
  Core state; the web app's refusal (`Index.cshtml.cs:185`) is the intended
  behaviour and the view-model test asserts it.
- **This screen owns a contract collision by design.** [[FEAT-020]] (plan handle
  `DSK-05-20`) extends `OperationsViewModel` and `OperationsPage.xaml`; a second
  view model is a stop condition per the Guardrails. Whichever lands first
  creates the type under exactly the members named here.
- **`PAR-27` starts at `not inventoried` with an empty `Verification` cell.**
  Moving it to `implemented` means filling that cell for the first time with the
  view-model, UI-script and accessibility evidence this ticket produces —
  [[FEAT-025]] (plan handle `DSK-05-25`) owns the matrix maintenance pattern.

## Open questions

None that block. Five points that could look like questions have named owners:

- Whether the upload-links half is published by [[GWY-013]] (plan handle
  `DSK-03-13`) is settled at plan step 2 by reading that ticket's contract; a
  gap is raised there. A scope boundary, not a question.
- Whether the lease endpoints exist for revoke is [[GWY-008]] (plan handle
  `DSK-03-08`)'s contract. Same treatment.
- When the Box / DVLA-DVSA / feed / minimum-version health rows arrive is
  [[PLAT-015]] (plan handle `DSK-10-15`)'s. Until then the rows are absent under
  the screen spec's own rule.
- Which ticket scaffolds `tests/Pegasus.Desktop.ViewModelTests` — [[TEST-004]]
  (plan handle `DSK-08-04`) per this body, or [[FND-038]] (plan handle
  `DSK-02-13`) per [[FEAT-001]]'s files document. This ticket writes into
  whichever landed and creates neither; see the plan's Governing docs for the
  reconciliation note.
- Whether Retry should be reasoned is a design-authority question about
  `screen-specs.md:392`. The trivial default is taken here (plain confirmation,
  because Core accepts no reason) and recorded, per the authoring rule to take a
  default rather than ask.
