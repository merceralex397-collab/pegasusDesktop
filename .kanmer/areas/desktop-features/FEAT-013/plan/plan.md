# Plan — FEAT-013: S13 Uploads (manual, status, groups)

**Diff estimate: ~22 files, ~2,400 lines.**

Derived from the `files` document, not asserted. `src/Pegasus.Contracts` — 3
files, ~230 lines (limits payload, upload-session triple, status payload binding,
group requests); `src/Pegasus.Desktop.Infrastructure` `UploadQueueService` — 2
files, ~320 lines (per-file streaming, cancel, per-file failure isolation);
`src/Pegasus.Desktop` `UploadViewModel` plus XAML plus the status and group
surfaces — 5 files, ~700 lines (the three page models it replaces total 491 lines,
plus the queue, the picker and the derived poll interval, which are new);
`/api/v1` gap-closing in `src/Pegasus.Web` — 1 file, ~60 lines;
`tests/Pegasus.Api.ContractTests` — 2 files, ~420 lines (three limit boundaries,
replay, the `retry_scheduled` state fact, the resolved-`caseId` fact, 401/403);
`tests/Pegasus.Desktop.ViewModelTests` — 2 files, ~360 lines;
`tests/Pegasus.Desktop.UITests` — 1 script, ~120 lines; documentation — 6 files,
~190 lines (three parity rows, the `screen-specs.md` Upload block, FRD-02's new
state, FRD-13's section, `docs/capabilities.md`).

## Approach

Read every bound from the server at startup and mirror it client-side, rather
than encoding 10 MiB and 20 files in the desktop — because
`src/Pegasus.Core/Intake/IntakeContracts.cs:7-56` is the only authority and its
own remark records a real incident (a 16.69 MB forward refused as
`message_too_large` on 2026-08-05) caused by applying one bound where another
belonged. The alternative considered and rejected was a compile-time constant
shared through `Pegasus.Contracts`: it is simpler, and it makes the desktop's
idea of the limit a *deployment* fact that can silently disagree with the running
gateway, which is exactly the class of error the incident above is an instance
of. For the status, this slice deliberately renders three facts it does not
derive — `dueAtUtc`, the `retry_scheduled` state value and the
association-or-link `caseId` are all [[GWY-011]] (plan handle `DSK-03-11`)'s
(its step 6) — because the association-or-link rule already has exactly one owner
at `IntakeContracts.cs:406-407` and a client-side copy would be the third.

## Governing docs

The ticket carries `refs: ["docs/frd/frd-02-intake-and-source-identity.md"]` and
`docs_todo: true` (confirmed in `get_doc_gates FEAT-013`, which reports
`governing-doc` satisfied at `leave-backlog`).

**Meets — `docs/frd/frd-02-intake-and-source-identity.md`.** Steps 6, 9 and 10
keep the receipt's source identity and its replay semantics intact: the receipt
token remains the single replay key and a malformed one is refused rather than
regenerated, mirroring `src/Pegasus.Web/Pages/Upload.cshtml.cs:52-64`.

**Modifies — `docs/frd/frd-02-intake-and-source-identity.md`.** Step 8 adds the
named retry-scheduled staff-visible state to FRD-02, once the operator word has
been reconciled against the settled vocabulary in `docs/design/README.md`. This
is an explicit documentation change listed in the ticket body's `## Documentation
changes`, so the authorisation is the ticket itself; the wire value stays
`retry_scheduled` and belongs to [[GWY-011]].

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the gateway;
> no long-lived provider secret in the package), authored by [[FND-005]]. Same
> condition — the authority for the artifact store being reached only through the
> gateway.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review`:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §13.4, §13.7 | Manual intake with a truthful status | Steps 6–8 |
| upstream INTK-001 (absorbed; **no fork ticket**) | A `retry_scheduled` item is never shown as Received; a completed receipt offers Open case whenever a case exists by link **or** association | Step 8 |
| `src/Pegasus.Core/Intake/IntakeContracts.cs:7-56` | The five real limits, enforced server-side before Core | Steps 2, 4, 7, 10 |
| `src/Pegasus.Web/Program.cs:525-530` | The multipart envelope bounded to the whole batch | Step 4 |
| `docs/desktop/05-implementation-and-migration/README.md` § 7 "Binary endpoints and limits" | The desktop streams, it does not buffer | Step 6 |
| `docs/design/README.md:412-421` | Banned operator words; the waiting word comes from the settled vocabulary | Step 8 |
| `docs/desktop/06-ui-design/screen-specs.md:309-317` | The Upload block, corrected by this ticket | Steps 8, 13 |
| `docs/engineering.md` § One Core owner | One case-id resolution and one waiting-state derivation, both [[GWY-011]]'s | Steps 5, 8 |
| L-01 | The gateway stages the bytes and owns the artifact store | Steps 4, 6 |
| L-02 | Verification on the local Test/UAT stack | Steps 10–12 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 13 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`;
  `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml` (file-picker
  automation)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `minimal-api-file-upload`
  (dotnet/skills `98f84851`,
  `plugins/dotnet-aspnetcore/skills/minimal-api-file-upload/SKILL.md`) →
  `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) →
  `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-ui-testing`
  (`.codex/skills/winui-ui-testing/SKILL.md`) → `run-tests` → `winui-code-review`
  at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search` for `FileOpenPicker` window-handle initialization in a
  packaged WinUI 3 app)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's thirteen implementation steps in the same order
and with the same ownership.

1. **Orient and take.** Read the plan row `DSK-05-13`,
   `docs/desktop/05-implementation-and-migration/vertical-slices.md:459-487` and
   `docs/desktop/06-ui-design/screen-specs.md:309-317`. Call
   `get_doc_gates FEAT-013`, then `take_ticket` with branch
   `task/dsk-05-13-uploads` and worktree `../pegasus-worktrees/dsk-05-13-uploads`
   from `origin/dev`.
2. **Resolve the limit discrepancy first.** Read
   `src/Pegasus.Core/Intake/IntakeContracts.cs:7-56` and
   `src/Pegasus.Web/Pages/Upload.cshtml.cs`. The plan text describes a one-file
   upload; the code accepts a batch of up to `MaximumBatchFileCount` (20) files,
   each bounded by `MaximumContentLength` (10 MiB), with the request bounded by
   `MaximumBatchContentLength` (20 × 10 MiB + 64 KiB `MultipartOverhead`). The
   real limits are recorded with evidence in `research`; **raise the discrepancy
   under the ticket's open questions and get it resolved before leaving
   Preparing** — do not implement to the plan prose over the code. Note also that
   `MaximumMailboxContentLength` (750 MiB, `:34`) is a *received message* bound and
   must never be surfaced as an upload limit.
3. **Record the accepted types and the replay rule.** Copy the extension list
   verbatim from `src/Pegasus.Web/Pages/Upload.cshtml:36` — `.eml`, `.pdf`,
   `.docx`, `.doc`, `.msg`, `.jpg`, `.jpeg`, `.png` — with their MIME types, and
   record `ExternalReceiptToken`'s replay semantics from
   `Upload.cshtml.cs:52-64`: a malformed token is refused, never silently
   regenerated, so a replay never becomes a second receipt. **Record the SHA read.**
4. **Confirm the endpoints.** From [[GWY-011]]:
   `POST /api/v1/uploads/upload-session` → `PUT` bytes → `POST …/complete`
   (complete is idempotent on the receipt token),
   `GET /api/v1/uploads/{receiptId}/status`, `POST /api/v1/uploads/groups`,
   `POST /api/v1/uploads/groups/{gid}/attach`,
   `GET /api/v1/uploads/groups/{gid}`. Load `minimal-api-file-upload` and confirm
   the server enforces every limit **before Core** is called — the envelope check
   is `src/Pegasus.Web/Program.cs:525-530`.
5. **Contracts, including the widened status.** Add the upload DTOs to
   `src/Pegasus.Contracts` *(created by [[FND-029]], plan handle `DSK-02-04`)*,
   including a limits payload the client reads at startup so the client-side check
   mirrors the server rather than hard-coding a number. **Check first that
   [[GWY-011]] has landed the widened upload-status payload, and restate its shape
   from that ticket's step 6 before writing a line of client code** — it owns
   `GET /api/v1/uploads/{receiptId}/status`, `QueuedIntakeStatus` and
   `IQueuedIntakeStatusQueries`, and this ticket consumes the payload without
   re-deriving any part of it. Its three facts, which `QueuedIntakeStatus` does not
   carry today: (a) `dueAtUtc` — the work item's `DueAtUtc`
   (`src/Pegasus.Core/Intake/DurableIntake.cs:41`) in UTC, null only when the
   receipt has no work item; (b) a fifth state value `retry_scheduled`, appended to
   `QueuedIntakeStatusKind` as `4` with the existing four numeric assignments
   untouched (`DurableIntake.cs:79-85`), spelled as
   `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:722` already
   persists it, so a scheduled retry is no longer collapsed into `Received`; and
   (c) `caseId` — the same member as today (`DurableIntake.cs:93`) with corrected
   semantics: the value `IntakeReceipt.CurrentCaseId` yields
   (`src/Pegasus.Core/Intake/IntakeContracts.cs:406-407`), covering a link **or**
   an association, resolved in `EfQueuedIntakeStatusQueries` rather than from
   `CaseIntakeLinks` alone. If any of the three is missing from the generated
   client, **stop and raise it on [[GWY-011]]** — do not add a second case-id
   resolution, a client-side inference of the waiting state, or a local copy of
   either rule here.
6. **`UploadQueueService`.** Implement it in `src/Pegasus.Desktop.Infrastructure`
   *(created by [[FND-031]], plan handle `DSK-02-06`)*: per-file streaming with
   progress, cancel, and per-file failure isolation — one rejected file does not
   abandon the batch, mirroring the per-file sentences at
   `Upload.cshtml.cs:74-89`. Nothing is buffered whole in memory.
7. **`UploadViewModel` and the picker.** Implement it in `src/Pegasus.Desktop`
   *(created by [[FND-030]], plan handle `DSK-02-05`)* with drag-and-drop and a
   `FileOpenPicker`; use `microsoft_docs_search` for the packaged WinUI 3
   window-handle initialization the picker requires. Apply the client-side
   extension and size checks **from the limits payload** and show a per-file
   rejection reason drawn from the shared vocabulary — the precedent for the
   wording is `src/Pegasus.Web/Presentation/UploadOutcome.cs` (304 lines).
8. **The honest status view (upstream INTK-001, absorbed here and shared with
   [[FEAT-009]] and [[GWY-011]]).** Over
   `GET /api/v1/uploads/{receiptId}/status`, two specific defects must not be
   re-specified.
   **(a)** `QueuedIntakeStatusKinds.FromWorkState`
   (`src/Pegasus.Core/Intake/DurableIntake.cs:96-114`) collapses
   `IntakeWorkState.RetryScheduled` into `Received` at `:104-107`, and the retry is
   due 30 minutes to 2 hours away: a receipt whose work item is `retry_scheduled`
   is shown as a **named waiting state**, never as Received, reading the payload's
   `retry_scheduled` state value **directly** — never inferring it from a due time —
   and the poll interval is derived from the payload's `dueAtUtc` rather than fixed
   at two seconds. **Clamp bounds recorded here: minimum 2 s, maximum 60 s, target
   = the time remaining to `dueAtUtc` bounded into that range; a null `dueAtUtc`
   falls back to the minimum.** **This ticket owns the operator-facing word only**:
   take the waiting word from the settled operator vocabulary in
   `docs/design/README.md` rather than inventing one, and reconcile it with FRD-02;
   the wire value stays `retry_scheduled` and belongs to [[GWY-011]].
   **(b)** A completed receipt offers **Open case** whenever a case exists by link
   **or** by association — that is exactly what the payload's `caseId` now means,
   resolved once in [[GWY-011]] through `IntakeReceipt.CurrentCaseId` — offering
   **Open receipt** only when `caseId` is null. Do not re-resolve the case id here.
   INTK-001's `document.hidden` half is moot on the desktop, where there is no
   background tab; record that rather than inventing a window-visibility rule.
9. **Groups.** Implement group register and attach as explicit commands with their
   own operation keys, mirroring `OnPostRegisterGroupAsync`
   (`src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs:64`) and
   `OnPostAttachGroupAsync` (`:130`).
10. **Contract tests.** In `tests/Pegasus.Api.ContractTests` *(created by
    [[TEST-001]], plan handle `DSK-08-01`)*: a file at exactly
    `MaximumContentLength` succeeds and one byte over is refused with a problem; a
    batch at `MaximumBatchFileCount` succeeds and one file more is refused; a
    request over `MaximumBatchContentLength` is refused **before Core**; replay of
    the same receipt token returns the existing receipt rather than a second one; a
    `retry_scheduled` work item's status carries the `retry_scheduled` state and a
    non-null `dueAtUtc`, never `Received`; a receipt associated to a case **without**
    a `CaseIntakeLinks` row still returns the resolved case in `caseId`; 401 and
    403. Enable `Features:DesktopGateway` explicitly.
11. **View-model tests.** In `tests/Pegasus.Desktop.ViewModelTests` *(created by
    [[TEST-004]], plan handle `DSK-08-04`)*: queue progress, cancel, per-file
    rejection, status polling states, the poll interval derived and clamped from
    `dueAtUtc` at both bounds, Open case versus Open receipt for the linked,
    associated and neither cases, and group register/attach.
12. **UI and accessibility.** Add a `winapp ui` script under
    `tests/Pegasus.Desktop.UITests` *(created by [[TEST-006]], plan handle
    `DSK-08-06`)* driving the file picker (`-w <HWND>` per the `winui-ui-testing`
    skill) end to end: pick a file, watch progress, reach a terminal status —
    **without sleeps**. Run the `axe-windows` scan from [[TEST-009]] (plan handle
    `DSK-08-09`) on the screen and attach both artefacts.
13. **Documentation, simplification pass, PR.** Update `parity-matrix.md` rows
    `PAR-28`, `PAR-29` and `PAR-30` — leave `PAR-31` alone, it is
    `legacy path retained` by decision. Correct the `screen-specs.md:309-317`
    Upload block: the four-state list gains the named waiting state, "polling every
    two seconds" becomes the interval derived from `dueAtUtc` and clamped with
    manual refresh kept, and the block's existing "completion links to the case or
    retained receipt" is restored as behaviour by step 8. **This block only** — the
    `endpoint-map.md` § Intake row is [[GWY-011]]'s. Add the named
    retry-scheduled staff-visible state to
    `docs/frd/frd-02-intake-and-source-identity.md`; add the upload section to
    `docs/frd/frd-13-desktop-operator-experience.md` (created by [[DUI-013]], plan
    handle `DSK-06-13` — contribute the content there if it has not landed); add the
    `DSK` rows to `docs/capabilities.md`. Run the simplification pass over this
    branch's diff, record it under a dated `## Simplification pass` heading below,
    then open the PR into `dev`.

## Verification

Evidence tiers from the body: **5** (Web/API/MCP caller), **7**
(Browser/accessibility).

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — per-file, batch-count, batch-envelope, replay, `retry_scheduled`-state,
  resolved-`caseId`, 401 and 403 facts pass (tier 5: validation, limits and
  idempotency enforced before Core, and failures translated correctly).
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — queue, cancel, rejection, status, derived-poll-interval, Open case / Open
  receipt and group facts pass.
- `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script upload`
  — the file-picker script completes to a terminal status without sleeps (tier 7:
  keyboard, focus, progress and error-state evidence from a real run, including
  the picker).
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"`
  — existing upload web tests stay green.

## Risks / open questions

- **Open question — single file versus batch.** Recorded in the
  `open-questions` document because the ticket body instructs it; an unticked
  `- [ ]` there holds `leave-preparing`, `enter-review` and `enter-done`, which is
  the intended behaviour. Answered by: the plan owner for area 05, against the
  evidence in `research`. Recommended answer: the code — a batch of up to 20 files.
- **[[GWY-011]] may not have landed the three status facts.** Step 5 is a hard
  gate. Adding a second case-id resolution or inferring the waiting state
  client-side is a stop condition. Answered by: [[GWY-011]].
- **Appending a fifth enum value could break an exhaustive switch.**
  `FromWorkState`'s `default` throws (`DurableIntake.cs:113`), so [[GWY-011]]'s
  change must update it. This slice's contract tests are what notice if it was
  missed. Owner: [[GWY-011]].
- **No settled operator word may exist for the waiting state.** The ticket
  requires taking one from `docs/design/README.md` rather than inventing one and
  reconciling it with FRD-02. If none exists, the word becomes an operator question
  raised at that point rather than a coined string — recording an invented word in
  an FRD would be worse than asking.
- **Surfacing the wrong limit.** `MaximumMailboxContentLength` (750 MiB) is a
  received-message bound, not an upload bound, and its own remark records the
  incident that came of confusing the two. Mitigation: the limits payload carries
  only the three upload bounds.
- **A fixed poll interval.** A two-second poll against a retry due in two hours is
  the waste the honest status removes. Mitigation: the derived-and-clamped interval
  (step 8) with a view-model test at both bounds.
- **`FileOpenPicker` in a packaged app.** Needs explicit window-handle
  initialization; the doc search at step 7 is the check, and the `winapp ui` script
  at step 12 is the proof.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
