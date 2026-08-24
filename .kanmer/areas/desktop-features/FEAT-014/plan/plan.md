# Plan — FEAT-014: S14 Documents and custody (Box browser, transfer queue, preview)

**Diff estimate: ~19 files, ~2,300 lines** if `TransferQueueService` and
`CaseDocumentsViewModel` already exist from [[FEAT-032]] (plan handle
`DSK-07-06`); **~23 files, ~3,050 lines** if they do not and this slice creates
them to that ticket's pinned shape. Step 6 and step 7 record which case applied
and the estimate is restated then.

Derived from the `files` document, not asserted. `src/Pegasus.Contracts` document
DTOs — 3 files, ~250 lines; extensions to `CaseDocumentsViewModel` and
`CaseDocumentsView.xaml` (export, custody retry, permission-checked removal,
canonical indicator, request-link commands) — 4 files, ~520 lines, plus ~750
lines if the view model and view are created rather than extended; extensions to
`TransferQueueService` — 2 files, ~180 lines, plus ~380 if created;
`/api/v1` gap-closing in `src/Pegasus.Web` — 1 file, ~70 lines;
`tests/Pegasus.Api.ContractTests` — 2 files, ~480 lines (six commands plus
content and export, the no-credential assertion, the inert request-link fact);
`tests/Pegasus.IntegrationTests` transfer-failure facts extending
`CustodyOutboxIntegrationTests.cs` — 1 file, ~330 lines;
`tests/Pegasus.Desktop.ViewModelTests` — 2 files, ~350 lines;
`tests/Pegasus.Desktop.UITests` — 1 script, ~110 lines; documentation — 3 files,
~110 lines.

## Approach

Build the two request-upload-link commands as **honestly inert** rather than
hiding them, deferring them or making them work — because the capability is
composed closed in production
(`src/Pegasus.Infrastructure/DependencyInjection.cs:431-441` registers
`UnavailableDocumentRequestStore`, which throws
`DocumentRequestUnavailableException` at
`src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs:19,28`),
and `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:110-136` pins
that closed with the reason in its own comment. The screen spec nevertheless says
the commands are "findable" (`docs/desktop/06-ui-design/screen-specs.md:353-355`).
Two alternatives were considered and rejected: **hiding the commands** — it
contradicts the spec and leaves an operator unable to discover why the capability
is missing; and **stubbing a link locally** so the flow "works" — it would be a
second issuer, would need `ProductionCompositionTests` edited, and would put a
fabricated URL in front of an operator. The honest inert state costs one problem
type and one sentence, and flips to working the day upstream CASE-022 (board
[[CASE-002]]) activates INT-31, with no change here. Everything else on this tab
extends types [[FEAT-032]] owns rather than growing rivals, because one screen
gets one view model and one transfer service.

## Governing docs

The ticket carries `refs: ["docs/frd/frd-05-documents-extraction-and-custody.md"]`
and `docs_todo: true` (confirmed in `get_doc_gates FEAT-014`, which reports
`governing-doc` satisfied at `leave-backlog`).

**Meets — `docs/frd/frd-05-documents-extraction-and-custody.md`.** Steps 5 and 9
keep custody identity intact by carrying the custody state and a canonical-copy
indicator on every document record, so a local working copy is never mistaken for
the canonical one; steps 10 and 11 evidence that removal stays logical and
reasoned (`src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:160`) and that an
interrupted transfer leaves no partial canonical document. The FRD is not
modified by this ticket.

> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the gateway;
> no long-lived provider secret in the package), authored by [[FND-005]] (plan
> handle `DSK-00-05`). **Consumed, not authored, by this ticket** — its step 14
> secret scan is the evidence ADR-0107 will cite.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]]. Same condition.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review`:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §12.2, §13.7, §14.6 | Native document handling with a transfer queue, no hidden overwrite, and visible evidence the canonical copy was saved | Steps 6, 7, 9 |
| `docs/desktop/06-ui-design/screen-specs.md:343-361` | Folder/file list, transfer queue with kept failed rows, preview for safe types, Open externally, reasoned logical Remove, "Create / Revoke public upload link (findable — CASE-022)", canonical-versus-local, no hidden overwrite | Steps 7–9 |
| upstream CASE-022 (board [[CASE-002]]) | Single ownership of activating INT-31 and of the accepted-limits change | Step 8 and § Out of scope |
| `docs/desktop/05-implementation-and-migration/reuse-map.md:42-48` | The desktop never references `Pegasus.Infrastructure`, EF Core, Azure SDKs, Box or Graph SDKs | Steps 4, 14 |
| Proposal §16.1 | Explicit retry of a failed transfer item | Step 6 |
| Area 10 (temporary files) | Per-user path, restrictive ACLs, bounded retention, deleted on completion or abandonment | Step 6 |
| `docs/engineering.md` § One Core owner | One view model per screen, one transfer service | Steps 6, 7 |
| L-01 | The gateway brokers Box | Step 4 |
| L-02 | Transfer-failure tests on the local Test/UAT stack | Step 11 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 15 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`;
  `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`;
  `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `dotnet-webapi` (dotnet/skills
  `98f84851`) → `minimal-api-file-upload` (dotnet/skills `98f84851`) →
  `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's fifteen implementation steps in the same order and
with the same ownership.

1. **Orient and take.** Read the plan row `DSK-05-14`,
   `docs/desktop/05-implementation-and-migration/vertical-slices.md:488-522`,
   `docs/desktop/06-ui-design/screen-specs.md:343-361`,
   `docs/frd/frd-05-documents-extraction-and-custody.md`, the [[FEAT-033]] (plan
   handle `DSK-07-07`) spike outcome, and the upstream CASE-022 (board
   [[CASE-002]]) body. Call `get_doc_gates FEAT-014`, then `take_ticket` with
   branch `task/dsk-05-14-documents-custody` and worktree
   `../pegasus-worktrees/dsk-05-14-documents-custody` from `origin/dev`.
2. **Read and tabulate.** Read `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs`
   and both Documents pages in full. Append to `research` the six custody handlers
   (`:28`, `:74`, `:138`, `:162`, `:186`, `:237`) with their Core calls, the
   permission rules for removal, the reason requirements, the request-upload-link
   policy bounds from `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` (469
   lines), and how the export builds its archive. Note that removal is **logical**
   — `Custody.cshtml.cs:160` says "The document occurrence was logically removed;
   custody content and history were retained." **Record the SHA read** — upstream
   PLAT-039 (Box token refresh) and PLAT-041 (folder resolve once per export)
   arrive via the one-way sync.
3. **Transfer mode.** Read the [[FEAT-033]] spike result and record here which
   mode this slice implements: **gateway streaming** (the default) or direct
   transfer using a short-lived, file-scoped downscoped Box token. **Do not decide
   it here — if the spike has not landed, the ticket stays in Preparing.**
4. **Confirm the endpoints.** From [[FEAT-031]] (plan handle `DSK-07-05`) and
   [[GWY-011]] (plan handle `DSK-03-11`): document list with metadata,
   `GET /api/v1/cases/{id}/documents/{docId}/content` with `ETag` and range, the
   upload-session triple, `DELETE /api/v1/cases/{id}/documents/{docId}` (soft and
   reasoned), `POST /api/v1/cases/{id}/custody/retry`,
   `POST`/`DELETE /api/v1/cases/{id}/request-upload-links`, and
   `POST /api/v1/cases/{id}/documents/export`.
5. **Contracts.** Add the document DTOs to `src/Pegasus.Contracts` *(created by
   [[FND-029]], plan handle `DSK-02-04`)* carrying file type, size, source,
   uploader, timestamp, custody state and a **canonical-copy indicator**, so the UI
   distinguishes local temporary from canonical without inference.
6. **`TransferQueueService` — extend or create.** Check whether it already exists
   in `src/Pegasus.Desktop.Infrastructure` from [[FEAT-032]], which owns it. **If
   it does**, extend it in place and change no existing member. **If it has not
   landed**, create it with exactly the shape [[FEAT-032]] step 3 pins, restated
   here verbatim so the two cannot drift: a bounded queue of upload and download
   items, each with `notStarted`/`running`/`succeeded`/`failed`/`cancelled` state,
   a correlation id, progress in bytes, cancellation via
   `CancellationTokenSource`, and explicit retry of a failed item (proposal
   §16.1); uploads use the three-step session from [[FEAT-031]] and a cancelled or
   failed upload **never** calls `complete`. **Record here which case applied.**
   Either way this slice's own requirement holds: temporary files on a per-user
   path with restrictive ACLs and bounded retention as area 10 specifies, deleted
   when the transfer completes or is abandoned. **Never a second transfer service.**
7. **`CaseDocumentsViewModel` — extend or create.** Check whether it already
   exists from [[FEAT-032]], which owns that type and its view. **If it does**, add
   the export, custody-retry and permission-checked removal commands in place and
   change no existing member. **If it has not landed**, create it with exactly the
   members [[FEAT-032]] step 5 pins (`[ObservableProperty]` partial properties,
   `[RelayCommand]`, no UI types in the view model) and **record here which case
   applied**. Either way this slice's own surface is the same: folder and file
   list, the transfer queue with per-item state, a preview pane for safe types
   only, an explicit "open externally" command, export, removal behind the
   permission check, custody retry, and request-link create and revoke as reasoned
   commands. **Never a second view model for the Documents tab.**
8. **The honest inert request-link state (upstream CASE-022, board
   [[CASE-002]]) — the mirror of [[GWY-011]] step 8.** In production the capability
   is composed as `UnavailableDocumentRequestStore`
   (`src/Pegasus.Infrastructure/DependencyInjection.cs:431-441`), which throws
   `DocumentRequestUnavailableException`
   (`UnavailableDocumentRequestStore.cs:19,28`); `IGetRequestUpload` returns
   `null` (`:40-43`) so the anonymous `/Uploads/{token}` page 404s; and
   `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:116,130` pins that
   closed. [[GWY-011]] therefore makes
   `POST`/`DELETE /api/v1/cases/{id}/request-upload-links` return the named
   problem `urn:pegasus:problem:provider-unavailable` with a stable operator
   sentence saying the upload-link capability is not active. **This tab renders
   that state and nothing more**: the create and revoke commands are present and
   discoverable, their unavailability is stated **in words** on the surface rather
   than shown as a bare failure, and no link, expiry, QR code or copyable URL is
   ever fabricated. **Do not** work around it — no second issuer in
   `src/Pegasus.Desktop.Infrastructure`, no locally generated token, no change to
   `ProductionCompositionTests`, and no offline stub that behaves like a link.
   **Record here** that the commands become live when [[CASE-002]] activates INT-31
   to the operator's accepted limits, and that until then this ticket's own
   acceptance is met by the honest inert state, not by a working link. If the
   screen spec or a design asset shows a live link, raise it against [[CASE-002]]
   rather than implementing to the picture.
9. **Canonical versus local.** Make the distinction explicit in the UI per
   proposal §14.6 and show evidence that the canonical copy was saved. There is
   **no hidden automatic overwrite**: a name collision surfaces a decision, and the
   conflict handling itself is [[FEAT-034]] (plan handle `DSK-07-08`).
10. **Contract tests.** In `tests/Pegasus.Api.ContractTests` *(created by
    [[TEST-001]], plan handle `DSK-08-01`)*, for each endpoint: success, 401, 403,
    409 stale version, replay of the same `operationKey`, reason required on
    removal, range download, an assertion that **no Box credential or token appears
    in any response**, and one fact that
    `POST /api/v1/cases/{id}/request-upload-links` under the production composition
    returns the named `provider-unavailable` problem rather than a 500 or a
    fabricated link.
11. **Transfer-failure tests.** A large transfer interrupted mid-stream leaves no
    partial canonical document and is retryable; a cancelled upload leaves no
    orphan; a failed custody item can be retried through the human-only retry
    command. **Extend the `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs`
    (1,796 lines) patterns rather than inventing a parallel harness.**
12. **View-model tests.** In `tests/Pegasus.Desktop.ViewModelTests` *(created by
    [[TEST-004]], plan handle `DSK-08-04`)*: queue state transitions, cancel, retry,
    permission-gated removal, preview-type gating, the canonical indicator, and the
    request-link commands surfacing the named unavailable state **with no fabricated
    link value**.
13. **Performance (tier 10).** Measure that a transfer in progress does not block
    navigation and that memory stays steady across repeated large transfers. Record
    the method and the figures in the proof, against the stated file-count and
    10 MiB limits.
14. **Prove no provider secret ships.** Run the secret scan from [[TEST-011]]
    (plan handle `DSK-08-11`) over the built package and the desktop logs, and
    record the clean result in the proof.
15. **Documentation, simplification pass, PR.** Update `parity-matrix.md` row
    `PAR-13` and the document rows `PAR-16`/`PAR-17`, recording that the
    request-upload-link capability is **inert** until [[CASE-002]] activates it.
    Add the export, custody-retry and permission-checked removal behaviour **inside
    the documents and transfer-queue section [[FEAT-032]] creates** in
    `docs/frd/frd-13-desktop-operator-experience.md` — a sub-heading under that
    section, **not a second documents section** (the file is created by
    [[DUI-013]], plan handle `DSK-06-13`; contribute the content there if it has
    not landed). Add the `DSK` rows to `docs/capabilities.md`. Run the
    simplification pass over this branch's diff, record it under a dated
    `## Simplification pass` heading below, then open the PR into `dev`.

## Verification

Evidence tiers from the body: **5** (Web/API/MCP caller), **7**
(Browser/accessibility), **10** (Performance/concurrency).

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — all document and custody endpoint facts pass, including the no-credential
  assertion and the inert request-upload-link fact (tier 5).
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"`
  — custody outbox tests plus the new interruption and cancellation facts pass,
  and `ProductionCompositionTests` stays **green and unchanged**.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — queue, permission, preview and request-link-unavailable facts pass.
- `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script documents`
  — upload, preview and export by keyboard pass; axe report attached (tier 7).
- Performance and secret-scan records in the proof — navigation unblocked during
  transfer, steady memory across repeated large transfers with the method stated,
  and a clean secret scan over package and logs (tier 10 and ADR-0107 evidence).

## Risks / open questions

- **[[FEAT-033]] may not have landed.** Then the transfer mode is undecided and
  **the ticket stays in Preparing** (step 3). A sequencing precondition with a
  named owner, not a question for this ticket. Answered by: [[FEAT-033]].
- **The temptation to make the link work.** The single largest risk here. A
  fabricated link, a locally issued token, a second document-request store, a
  stubbed expiry, an edit to `ProductionCompositionTests`, or a command that reads
  to an operator as though it worked — each is a stop condition. Mitigation: step
  8 states the honest state as the acceptance, step 10 pins it as a contract fact
  and step 12 pins "no fabricated link value" as a view-model fact.
  Owner of activation: [[CASE-002]].
- **[[GWY-011]] may return a bare 500 instead of the named problem.** Then the tab
  cannot state the unavailability in words. Stop and raise it there.
  Answered by: [[GWY-011]].
- **Two view models or two transfer services.** [[FEAT-032]] owns
  `CaseDocumentsViewModel`, `CaseDocumentsView.xaml` and `TransferQueueService`.
  Mitigation: steps 6 and 7 each have an explicit extend-or-create branch and
  record which applied; if [[FEAT-032]] lands mid-slice, the created types are
  reconciled with its pinned shape before either merges.
- **Box call budget on export and the gallery.** The export and evidence-gallery
  paths must resolve the case folder once per request and issue O(1) + N Box calls,
  not roughly nine per image, and **are not exposed until upstream PLAT-041 has
  landed via a sync** (flow record Q4.3). [[FEAT-031]] owns that budget and its
  measurement; this tab consumes it. Answered by: [[FEAT-031]] and [[FND-023]]
  (plan handle `DSK-01-10`).
- **Parity drift.** upstream PLAT-039 and PLAT-041 arrive by the one-way sync.
  Mitigation: check
  `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` before fixing
  anything forward, and record the SHA read (step 2).
- **A new table would need a GRANT migration.** upstream PLAT-035 and
  `scripts/Test-MigrationGrants.ps1`. Mitigation: this slice adds no table.
- **Temporary working copies are a security surface.** Mitigation: per-user path,
  restrictive ACLs, bounded retention, deletion on completion or abandonment
  (step 6), and the secret scan at step 14.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
