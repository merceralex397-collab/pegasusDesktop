# Plan — FEAT-029: DSK-07-03 Mail endpoints reuse

**Diff estimate: ~7 files, ~1,450 lines.**

Derived from the `files` document, not asserted. `src/Pegasus.Contracts`: 2 new
files ~280 (list page + summary ~70; the **separate** deleted-search page + item
~55; detail carrying `RetainedMailFolderRecommendation`, `SuggestedMove` and
`LatestFolderMove` ~90; preview ~20; six command requests ~45).
`src/Pegasus.Web`: 1 new endpoint file ~420 — nine endpoints, each with its own
exception→problem translation, plus the option-key parse — and ~6 lines of
registration in [[GWY-002]]'s group file. `tests/Pegasus.Api.ContractTests`:
1 file ~480 (paging, freshness, preview inertness, prepare→confirm ×2,
classification and its version conflict, move with reason, **both** mover
compositions, gate-off 404, 401, 403, no-credential).
`tests/Pegasus.IntegrationTests`: 1 file ~260 for the step-9 parity facts.
Documentation: ~8 lines in `endpoint-map.md` § `Mail workspace` (`:96-107`, four
rows corrected and one added) and ~6 in
`docs/frd/frd-08-email-mailbox-and-background-processing.md`.

## Approach

Project the two page models onto nine `/api/v1` endpoints by **copying
`src/Pegasus.Web/Mcp/MailMcpTools.cs`'s shape rather than the Razor handlers'** —
a second non-Razor ingress over these exact Core owners already exists
(`pegasus_mail_list` at `:128`, `pegasus_mail_get` at `:189`,
`pegasus_mail_correct_classification` at `:239`), and it has already solved the
three problems a fresh projection would hit: how to carry an operation key on a
command Core does not take one for (`:258-262` passes it to the audit ledger,
not to Core), how to name a classification on the wire (the option key from
`MailClassificationSelection`), and what a projection of `RetainedMailSummary`
looks like without Razor types. The alternative — deriving DTOs directly from
the page models — was rejected because the page models carry Razor-only
artefacts that would become permanent contract defects: `pageNumber` instead of
`page` (a reserved-route-key workaround the page documents at
`Index.cshtml.cs:36-42`), a `Reason` parameter capitalised for model binding
(`Message.cshtml.cs:325`), and `TempData` notice strings. The second design
decision is that the detail endpoint **projects** Core's folder recommendation
whole instead of computing the capability boolean the ticket's step 7 describes:
`RetainedMailFolderRecommendation.CanMove` is already set at `RetainedMail.cs:613`
from `folderMover?.IsAvailable == true && !isCurrentLocation`, and
"unavailable" already carries five distinct operator sentences (`:574-576`,
`:580-582`, `:589-592`, `:596-602`, plus `CanMove: false`). Resolving
`IRetainedMailFolderMover` in Web would be a second availability authority over
the one Core owns, against `AGENTS.md` § Simplicity rails, and a single boolean
would erase four of the five reasons.

## Governing docs

The ticket carries
`refs: ["docs/frd/frd-08-email-mailbox-and-background-processing.md"]` and
`docs_todo: true` — confirmed in `get_doc_gates FEAT-029`, which reports
`governing-doc` **satisfied** at `leave-backlog`.

**Meets — `docs/frd/frd-08-email-mailbox-and-background-processing.md`.**
`:198` requires that preview "never changes classification, association, read
state, Case state, or source custody" — step 4's preview endpoint is a `GET`
over `GetRetainedMail` with no mutation, asserted at step 8. `:205-207` requires
that "Sent mail and read-only Deleted Items search remain separate folder
scopes" — step 5 keeps Deleted Items a separate route over a separate Core use
case. `:243-247` requires that classification, linking and folder-move actions
be available "only from opened messages" and that UI-10 provide "no bulk
classification, linking or folder-move action" — step 6 adds message-scoped
verbs only and no batch endpoint. `:19` requires the durable mailbox identity
and the mailbox address to be kept as separate named identities — the list and
detail DTOs carry `mailboxId` and `mailboxAddress` as distinct fields. The FRD
gains a desktop **behaviour** clause at step 11; no existing FRD text is
modified.

> **New ADR** — ADR-0106 (Graph intake worker stays central: unattended
> execution, protected credentials), authored by [[FND-005]] (plan handle
> `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the gateway;
> no long-lived provider secret in the package), authored by [[FND-005]]. Same
> condition. Cited here for the step-10 no-credential assertion, which applies
> the same rule to the Graph credential behind Deleted-Items search.

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]]. Same condition.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review` to check against the diff:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 12.1 | Graph intake stays central; no desktop poller, no desktop Graph credential | Steps 5 and 10; the empty `src/Pegasus.Worker` and `GraphApprovedSources.cs` diff at Verification |
| Proposal § 13.8 | Source e-mails, attachments, history and an explicit state distinction on the native client | Steps 4 and 6 |
| Proposal § 16.2 | Provider failure is distinguishable from absence | Step 5's `state` / `isTruncated` and step 7's five unavailability reasons |
| Proposal § 13.11 | No scope creep into MAIL-12/13/17/19 | § Out of scope in `files` |
| L-01 | Gateway is `Pegasus.Web` evolved in place — route groups, no new deployment unit | Steps 2 and 4–6 |
| L-02 | Parity proven on the local stack with the existing fakes, never an Azure test resource | Steps 8–9 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| ADR-0106 | The desktop never polls Graph; the only Graph traffic stays the Web host's GET-only Deleted-Items read | Step 5 |
| ADR-0107 | No Graph token, mailbox secret or raw provider payload in a response | Step 10 |
| `docs/current-architecture.md:104` | Deleted Items GET-only, capped, neither retained nor backfilled; the move handler accepts exactly six inputs; the provider is unavailable by default and **the control is absent in that composition** | Steps 5, 6, 7 |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Projection style" | Endpoints are thin argument-mappers over Core ports; MCP and API remain two ingresses over one Core | § Approach; steps 4–7 |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Paging" | `pageSize` ≤ 200 — **bent to 100 here** because Core throws above it (`RetainedMail.cs:406`) | Step 4 and the endpoint-map correction at step 11 |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Idempotency" | Every command carries `operationKey` as a body field — **satisfied as an audit correlation id** for classification, which Core takes no key for | Step 6 and the endpoint-map correction at step 11 |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | Only the thirteen catalogued problem types; `correlationId` always present; no payload dumps | Steps 4–7 |
| `docs/desktop/06-ui-design/screen-specs.md:248-269` | The Inbox affordances shown "only when populated and available", and the unlink consequence sentence | Steps 6–7 |
| `docs/engineering.md` § Plan sizing | Diff estimate first; facts split from assumptions | This heading; `research` § Facts / Assumptions |
| `AGENTS.md` § Simplicity rails | One list per concept — one classification vocabulary, one folder-availability authority | § Approach both rejections |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 12 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`) → `run-tests` → `test-gap-analysis` (dotnet/skills
  `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search` for Microsoft Graph mail read semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's twelve implementation steps in the same order and
with the same ownership.

1. **Orient and take.** Read the plan row `DSK-07-03`
   (`docs/desktop/07-integrations/README.md` § 5), the endpoint map
   § `Mail workspace` (`:96-107`) with its Conventions header (`:11-27`), the
   Inbox screen spec (`docs/desktop/06-ui-design/screen-specs.md:248-269`), and
   `docs/frd/frd-08-email-mailbox-and-background-processing.md`. Call
   `get_doc_gates FEAT-029`, then `take_ticket` with branch
   `task/dsk-07-03-mail-endpoints` and a worktree cut from `origin/dev`.
2. **Settle the route-group collision before writing code.** Call
   `get_item GWY-012` and `get_doc_gates GWY-012` ([[GWY-012]], plan handle
   `DSK-03-12`). If it has landed the mail group, **extend** it with the
   availability and Deleted-Items rules below and add no second group; if it has
   not, create the group here and record that [[GWY-012]] reviews rather than
   re-creates it. Write the answer into this plan under a dated note before any
   code. This is the body's step 2 and it is not optional.
3. **Characterise both page models and record the SHA.** Append to `research`
   the handler table already begun there, adding per handler: the Core call, the
   versions it verifies, the reason it requires and the exact operator sentence
   it produces — including the unlink consequence "Unlinking this email cancels
   case &lt;ref&gt;" and the three move-outcome sentences
   (`Message.cshtml.cs:536-541`). **Record the commit SHA read** — upstream
   MAIL-011/012 arrive with [[FND-023]] (plan handle `DSK-01-10`).
4. **Implement the three retained reads.**
   `GET /api/v1/mail?mailbox&folder&page&pageSize&q&queue` over
   `ListRetainedMail.ExecuteAsync` plus `ListMailboxesAsync` plus
   `GetRetainedMailFreshness.ExecuteAsync`, newest first, `version` and a weak
   `ETag` per the endpoint-map conventions. **Cap `pageSize` at 100**, not the
   convention's 200: `RetainedMail.cs:406` throws above it, so a legal gateway
   request would otherwise become a 500. Use plain `page` — the Razor page's
   `pageNumber` binding is a reserved-route-key workaround
   (`Index.cshtml.cs:36-42`) that does not apply here. Reject a request carrying
   **both** a destination and a detailed classification as `validation`,
   mirroring `RetainedMail.cs:417-421`. Then
   `GET /api/v1/mail/{id}/preview` over `GetRetainedMail` projecting the same
   nine inert fields as `Index.cshtml.cs:174-190`, and `GET /api/v1/mail/{id}`
   over `GetRetainedMail.ExecuteAsync(actor, id, searchTerm, …)` — pass the
   search term through, or the desktop loses the match highlighting the web has.
5. **Keep the Deleted-Items path exactly as it is, on its own route.**
   `GET /api/v1/mail/deleted?mailbox&search&page&pageSize` over
   `SearchDeletedMail.ExecuteAsync`, capped at the 100 newest
   (`DeletedMailSearch.cs:55`), GET-only against the resolved `deleteditems`
   folder, nothing retained, nothing backfilled. Its mailbox list comes from
   `SearchDeletedMail.ListMailboxesAsync` (`:57`), not the retained one — the
   page switches between them at `Index.cshtml.cs:114-116`. Carry `state` and
   `isTruncated` on the response: `UnavailableDeletedMailSearchSource` returns an
   **empty list with `State = Unavailable`** (`:106-124`), and an endpoint that
   returns only items reports "no matches" when the truth is "not composed
   here". Add no write path, no subscription and no change-notification
   callback — that would need a new accepted decision under proposal § 4.
6. **Implement the six mutations as explicit verbs.**
   `POST /api/v1/mail/{id}/link-case/prepare` and `.../unlink-case/prepare` over
   `IGetCase` + `IAcquireCaseEditLease`, applying the four preconditions each
   (`Message.cshtml.cs:222-231`, `:281-295`) and returning the lease token that
   joins prepare to confirm. `POST /api/v1/mail/{id}/link-case` and
   `.../unlink-case` over `ILinkIntake` / `IReverseIntakeLink`, requiring the
   prepared tuple, both versions, the lease token, the operation key and the
   reason before Core is touched — an endpoint that accepts a confirm without a
   matching prepare is a different concurrency model, not a projection. Spell
   the field `reason`, lower case: the Razor `Reason` capitalisation
   (`:325`, `:390`) is a model-binding artefact. `POST /api/v1/mail/{id}/classification`
   over `CorrectRetainedMailClassification`, parsing the option key through
   `MailClassificationSelection.TryParse` — the one vocabulary the page and the
   MCP tool already share — and carrying `operationKey` to the **audit ledger**
   rather than to Core, which has no such field
   (`MailMcpTools.cs:258-262` is the precedent); map
   `MailClassificationConcurrencyException` → `version-conflict`.
   `POST /api/v1/mail/{id}/move-to-recommended-folder` over
   `MoveRetainedMailFolder`, with the three `int` versions, the policy key, the
   required 1–500 character reason, and an operation key that **must be a bare
   GUID** — `RetainedMailFolderMove.cs:114-117` calls `Guid.TryParse` and the
   gateway's `desk:<guid>` format fails it. Return all three outcomes distinctly
   with their approved sentences; `Uncertain` instructs a replay of the **same**
   key and `Failed` instructs a **new** confirmation, so collapsing them loses
   the operator's next action.
7. **Represent availability honestly by projecting, not computing.** The detail
   response carries `RetainedMailFolderRecommendation` whole — `folderType`,
   `policyKey`, `policyVersion`, `reason`, `mailboxVersion`, `canMove` — plus
   `suggestedMove` and `latestFolderMove` (`RetainedMail.cs:531-539`). Do **not**
   resolve `IRetainedMailFolderMover` in Web and do **not** reduce the five
   distinct unavailability reasons (`:574-576`, `:580-582`, `:589-592`,
   `:596-602`, and `CanMove: false`) to one boolean. Note for the record that the
   ticket body names `EmptyRetainedMailFolderMoveStore` as the signal; measured,
   that type is only `GetRetainedMail`'s null-object default (`:494`) and the
   composition always registers the real store
   (`DependencyInjection.cs:87-88`) — the real signal is
   `IRetainedMailFolderMover.IsAvailable` (`RetainedMailFolderMove.cs:42`),
   defaulting to `UnavailableRetainedMailFolderMover` (`:135`) via
   `TryAddSingleton` (`DependencyInjection.cs:85`).
8. **Contract tests in `tests/Pegasus.Api.ContractTests`** *(created by
   [[TEST-001]], plan handle `DSK-08-01`)* mirroring the scenarios already proven
   in `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` (2,045 lines):
   paging and freshness, the `pageSize > 100` validation refusal, preview
   inertness (nothing changes read state), prepare-then-confirm with version
   verification for link and unlink, classification correction and its
   `version-conflict`, move with reason, the `desk:`-prefixed move key rejected
   as `validation` and a bare GUID accepted, and **both** folder-mover
   compositions asserted explicitly — supply a mover in one, rely on the
   `Unavailable` default in the other, and never assume which is the default.
   Add gate-off 404, 401, and 403 `not-authorized` for every route, with
   `Features:DesktopGateway` enabled explicitly in the positive tests.
9. **Parity test that the endpoint and the Razor handler produce the same Core
   effect for the same input** — same versions consumed, same association
   written, same classification dossier appended — so "same Core owner" is
   proven rather than asserted. Follow the fixture patterns in
   `MailWorkspaceWebTests.cs` rather than building a new harness, and keep every
   existing fact in that file green.
10. **Assert no credential leakage.** Review every DTO field against
    `RetainedMailSummary`, `RetainedMailDetail` and `DeletedMailSearchItem`, and
    add a contract fact that no response carries a Graph token, mailbox secret,
    connection string or raw provider JSON (ADR-0107, proposal § 12.3 applied to
    mail). A field that cannot pass the review is omitted and raised, not passed
    through.
11. **Endpoint map and FRD.** Correct the § `Mail workspace` rows (`:96-107`)
    rather than only adding: the list row's `pageSize` cap is **100**; the
    classification row's "Idempotent?" is version-based, not `yes (key)`; the
    detail row's `Returns` gains the folder recommendation and capability
    fields; add the separate deleted-search row that `PAR-21` already
    anticipates as `~GET /api/v1/mail/deleted?search`. Add the desktop behaviour
    clause to `docs/frd/frd-08-email-mailbox-and-background-processing.md` —
    behaviour, not mechanism.
12. **Simplification pass and PR.** Run the pass over this branch's own diff
    (`AGENTS.md` § Repository task workflow step 4), record it under a dated
    `## Simplification pass` heading below, then open the PR into `dev`.

## Verification

Evidence tier from the body: **5** — Web/API/MCP caller. Tier 5 obliges evidence
that the real routes reach Core with authentication, validation, versions and
exception translation observable, for every mail endpoint added; a registration
or a green build does not satisfy it.

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release`
  — expected: all mail endpoint facts pass, **including both provider
  compositions** and the no-credential fact. This output is the tier-5 evidence.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~MailWorkspaceWebTests"`
  — expected: every one of the existing facts in the 2,045-line file stays green.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`
  — expected: the new parity facts pass.
- `git diff --stat origin/dev -- src/Pegasus.Worker src/Pegasus.Infrastructure/Email src/Pegasus.Web/Pages/Mail`
  — expected: **empty output**. This single command proves the Guardrails' three
  named scope boundaries at once and belongs in the proof verbatim.

## Risks / open questions

- **[[GWY-012]] (plan handle `DSK-03-12`) owns the same route group.** The body
  calls them "one contract, not two". Mitigation: step 2 settles ownership from
  the board before any code and records the answer in this plan. Answered by:
  [[GWY-012]].
- **Two gateway conventions bend for mail and could look like defects to a
  reviewer.** `pageSize` caps at 100 because `RetainedMail.cs:406` and
  `DeletedMailSearch.cs:84` throw above it; the classification command is
  version-idempotent because `CorrectMailClassificationRequest` has no
  `OperationKey`. Mitigation: both are corrected in the endpoint map at step 11,
  with the `path:line` beside them, so the deviation is a recorded decision.
- **The move command rejects the desktop's own key format.**
  `Guid.TryParse` at `RetainedMailFolderMove.cs:114` refuses `desk:<guid>`
  (assumption A-07-03-4). Mitigation: a contract fact both ways at step 8, and
  the constraint documented on the DTO. If [[GWY-001]] (plan handle `DSK-03-01`)
  mandates the prefix in the key type, the exception is recorded there rather
  than worked around here.
- **The "move absent" test could silently pass for the wrong reason.** If some
  test host registers a fake mover, the default-composition assertion would be
  testing a different composition (assumption A-07-03-3). Mitigation: step 8
  asserts **both** compositions explicitly rather than relying on a default.
- **The preview endpoint is not cheaper than the detail.** Both call
  `GetRetainedMail` (`Index.cshtml.cs:167`), so a desktop that previews every
  row on hover pays detail cost per row. Mitigation: record it in the endpoint
  map's `Returns` note; the coalescing and refresh rules belong to
  [[FEAT-010]] (plan handle `DSK-05-10`) and area 02. Answered by:
  [[FEAT-010]].
- **Upstream MAIL-011/012 may change these handler signatures** (assumption
  A-07-03-6). Mitigation: step 3 re-derives the handler table after the sync and
  records the SHA before the DTOs are frozen.
- **`MailClassificationSelection` and `OperatorLabels` move under this ticket's
  feet.** [[GWY-016]] (plan handle `DSK-03-16`) and [[FEAT-023]] (plan handle
  `DSK-05-23`) relocate them to `Pegasus.Contracts`. Mitigation: consume them,
  never fork them; expect a namespace change and coordinate rather than
  duplicating the list. Answered by: [[GWY-016]].
- **The provider failure vocabulary is not settled.** [[FEAT-045]] (plan handle
  `DSK-07-19`) owns `terminal` / `transient` / `unknown` and the five provider
  problem types. This ticket carries the Core outcome enums verbatim and defines
  no rival list. Answered by: [[FEAT-045]].
- **Scope creep into MAIL-12/13/17/19 is the named trap.** Compose, mailbox
  mutation beyond the existing folder move, idempotent report send and automatic
  chasers are out of conversion scope (proposal § 13.11). Mitigation: recorded
  in `files` § Out of scope so the reviewer sees it was a decision.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._

## Route ownership decision — 2026-08-30

Live Kanmer recheck before implementation: [[GWY-012]] (the board's DSK-03-12 mail-endpoints ticket) is still in Preparing with no claim and no landed implementation; its `get_item` has no commits/PR and the current `origin/dev` contains no mail API endpoint file. This ticket therefore creates the single `/api/v1/mail` group and its endpoints. [[GWY-012]] remains the planned reviewer/contract owner; no second mail route group will be created. The existing `DesktopGateway` skeleton from [[GWY-002]] is present on `origin/dev` and is the only group this ticket extends.

## Simplification pass — 2026-08-30

- Reused the existing `Pegasus.Core` mail, association, classification and folder-move owners plus the established `MailMcpTools` projection conventions; no new Core policy, provider adapter, worker path, Razor path, or compatibility layer was added.
- Kept the folder recommendation as the Core-owned whole projection instead of introducing a Web-side availability authority. Kept Deleted Items as the existing GET-only `SearchDeletedMail` path.
- Removed one unused `Scope` helper and the unused `System.Globalization` import found during the pass. No additional abstraction or dependency was justified by this diff.
- Added the focused SQL-backed parity fact required by step 9. It runs equivalent link-confirm flows through the Razor handler and `/api/v1/mail`, then compares persisted association and mutation-history version effects.
- Regenerated `openapi/pegasus-v1.json` after the final contract shape and verified the complete API contract suite against it.
- Kiota generation is deliberately not duplicated here: `eng/api/Generate-ApiClient.ps1` and the generated tree are owned by [[GWY-005]], whose documented prerequisites [[FND-031]] and [[GWY-004]] are not yet available on this branch. The API snapshot is the completed contract artifact for this ticket; the client handoff remains an explicit downstream dependency rather than an invented parallel implementation.
