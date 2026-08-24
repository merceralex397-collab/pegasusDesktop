# Plan — FEAT-010: S10 Mail workspace (list, message, link/unlink, classify, move)

**Diff estimate: ~31 files, ~3,600 lines — split S10a ~1,050, S10b ~1,600,
S10c ~950.**

Derived from the `files` document rather than asserted. **S10a**: contracts 2
files ~140; `MailListViewModel` + list/preview XAML 3 files ~430; the coalesced
refresh in `Pegasus.Desktop.Infrastructure` 1 file ~70; contract tests 1 file
~180; view-model tests 1 file ~150; parity row `PAR-21` ~15. **S10b**: contracts
3 files ~260 (detail is the projection of a 1,025-line page model);
`MailMessageViewModel` + message XAML 4 files ~700; contract tests 1 file ~250;
view-model tests 1 file ~220; UI dialog script 1 file ~90; parity row `PAR-22`
~15. **S10c**: contracts 2 files ~110 (the move request alone carries four
version fields); view-model and XAML additions in place ~230; contract tests 1
file ~230; view-model tests ~180; FRD-13 and `docs/capabilities.md` ~120.

## Approach

Split on the seam the code already has — `Index.cshtml.cs` is the list and
preview, `Message.cshtml.cs:157-445` is the detail and the two prepare/confirm
pairs, `Message.cshtml.cs:448-565` is classification and move — so each PR is a
whole capability rather than an arbitrary third of a screen. The alternative
considered and rejected was landing the message page in one PR and following it
with a small "commands" PR: it puts a 1,025-line page model's worth of behaviour
in a single review, which is exactly what
`docs/desktop/05-implementation-and-migration/README.md:261-267` names as "the
two giants" trap and forbids. Prepare-then-confirm is kept as two round trips
rather than collapsed into one call, because the prepare step is what supplies
the confirmation's content and a desktop that composes its own sentence drifts
from the approved copy at `docs/design/README.md:408` on the first edit.

## Governing docs

The ticket carries
`refs: ["docs/frd/frd-08-email-mailbox-and-background-processing.md"]` and
`docs_todo: true` (confirmed in `get_doc_gates FEAT-010`, which reports
`governing-doc` satisfied at `leave-backlog`).

**Meets — `docs/frd/frd-08-email-mailbox-and-background-processing.md`.** Steps 4
and 5 render the retained set and its freshness without changing what retention
means; steps 6–8 keep every mail mutation on the Core use cases FRD-08 already
governs (`ILinkIntake`, `IReverseIntakeLink`, `CorrectRetainedMailClassification`,
`MoveRetainedMailFolder`); step 9 evidences the explicit draft/queued/sent/failed
distinction at route level. The FRD is not modified by this ticket.

> **New ADR** — ADR-0106 (Graph intake worker stays central: unattended
> execution, protected credentials), authored by [[FND-005]] (plan handle
> `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]]. Same condition.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review` to check against the diff:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §13.4, §13.8 | Source e-mails, attachments, communication history and an explicit draft/queued/sent/failed distinction, correlated to a case | Steps 4–8 |
| Proposal §10.2 (via `endpoint-map.md` Conventions) | Explicit command verbs, never a generic action endpoint | Steps 6, 8 |
| `docs/desktop/05-implementation-and-migration/README.md:261-267` | "The two giants" — split S10a/S10b/S10c, never one PR | Step 2, and the three PRs at step 13 |
| `docs/design/README.md:408` | `Unlinking this email cancels case <reference>.` verbatim | Step 7 |
| `docs/design/README.md` § No explanatory copy | An unavailable control is absent, not disabled with an explanation | Step 8 |
| `docs/desktop/06-ui-design/screen-specs.md:248-269` | Four tabs, Decision card, no Open case button, the ten AutomationIds | Steps 4–8 |
| L-01 | Gateway and Worker own Graph; the desktop calls only `/api/v1` | Steps 3, 9 |
| L-02 | Verification on the local Test/UAT stack with the replay/absent provider | Steps 9, 12 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| `docs/engineering.md` § One Core owner | One implementation of every mail rule; the Automation path is [[AUTO-001]]'s | Step 3 and § Risks |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over each sub-slice's own diff before its PR | Step 13 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`;
  `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `dotnet-webapi` (dotnet/skills
  `98f84851`) → `run-tests` → `winui-code-review` at review
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

These refine the ticket body's thirteen implementation steps in the same order
and with the same ownership.

1. **Orient and take.** Read the plan row `DSK-05-10`,
   `docs/desktop/05-implementation-and-migration/vertical-slices.md:371-407`,
   `docs/desktop/06-ui-design/screen-specs.md:248-269`, the approved mockups under
   `docs/design/references/mockups/inbox-message-page/` (start with
   `Dialogs.dc.html`), and `docs/design/README.md:396-421`. Call
   `get_doc_gates FEAT-010`, then `take_ticket` with branch
   `task/dsk-05-10-mail-workspace` and worktree
   `../pegasus-worktrees/dsk-05-10-mail-workspace` from `origin/dev`.
2. **Record the split in this document.** **S10a** list, freshness and preview;
   **S10b** message detail with prepare/link and prepare/unlink; **S10c**
   classification correction and move to recommended folder. Each is its own
   commit series and its own PR into `dev`. The checkpoint after each is: its
   contract tests and view-model tests green, its simplification pass recorded
   under its own dated heading, its PR merged, then the next begins.
3. **Read both page models in full and tabulate.** Append to `research`: the
   seven message handlers with their Core calls; which versions each command
   carries — noting that the move sends **four** (`Message.cshtml.cs:525-533`:
   classification version, recommendation policy key, recommendation policy
   version, mailbox version) plus an operation key and a reason; where `reason`
   is required (move); and how the provider-absent case removes the move control
   (`src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:134`
   `UnavailableRetainedMailFolderMover`). **Record the SHA read** — upstream
   MAIL-011 and MAIL-012 arrive through the one-way sync ([[FND-023]], plan handle
   `DSK-01-10`) and must be re-checked.
4. **S10a — list.** Implement `MailListViewModel` over
   `GET /api/v1/mail?mailbox&folder&page&pageSize&q&deleted` with mailbox and
   folder scope as dropdown filters, newest first, the freshness value rendered
   through the shared vocabulary map ([[DUI-005]], plan handle `DSK-06-05`) in the
   page header from [[DUI-012]] (plan handle `DSK-06-12`), and a coalesced manual
   refresh calling `POST /api/v1/mail/refresh`. Deleted Items search is capped by
   the gateway at the 100 newest (`src/Pegasus.Core/Intake/DeletedMailSearch.cs:56`)
   — show the cap honestly rather than implying completeness. Use the data-table
   pattern from [[DUI-007]] (plan handle `DSK-06-07`).
5. **S10a — preview.** Implement the preview pane over
   `GET /api/v1/mail/{id}/preview`, rendering **inert text only** — the precedent
   is `src/Pegasus.Web/Presentation/MailBodyPresentation.cs` (43 lines). The
   desktop never renders remote HTML and never loads remote content for a message.
6. **S10b — detail and the two prepare/confirm pairs.** Implement
   `MailMessageViewModel` over `GET /api/v1/mail/{id}` (thread, attachments,
   classification, queue, outcome, association, move result, suggested move) and
   the prepare/link and prepare/unlink command pairs. The prepare step returns
   what the confirmation must state; the confirm step carries the message and
   receipt versions, the case `expectedVersion` and the `editLeaseToken` obtained
   through the [[FEAT-005]] (plan handle `DSK-05-05`) session. `MailClassificationConcurrencyException`
   (`src/Pegasus.Core/Intake/RetainedMail.cs:195`) surfaces through the shared
   conflict pattern from [[FEAT-008]] (plan handle `DSK-05-08`).
7. **S10b — the unlink sentence.** The confirmation must show exactly
   `Unlinking this email cancels case <reference>.` from
   `docs/design/README.md:408`. Do not paraphrase it and add no explanatory text
   around it. Build the dialog on [[DUI-009]] (plan handle `DSK-06-09`)'s contract.
8. **S10c — classification and move.** Implement classification correction over
   `POST /api/v1/mail/{id}/classification` (carrying the classification version)
   and the recommended-folder move over
   `POST /api/v1/mail/{id}/move-to-recommended-folder` carrying all four version
   fields plus a required `reason`. Render the move's **three** outcomes
   distinctly, including the uncertain branch
   (`Message.cshtml.cs:541-546`) — and retry an uncertain move with the **same**
   operation key, never a fresh one. When the provider port is unavailable the
   move control is **absent**, not disabled with an explanation.
9. **Contract tests.** In `tests/Pegasus.Api.ContractTests` *(created by
   [[TEST-001]], plan handle `DSK-08-01`)*, per sub-slice: success, 401, 403,
   stale-version 409, replay of the same `operationKey`, provider-absent behaviour
   for move, and the Deleted Items cap. Enable `Features:DesktopGateway`
   explicitly, or a gated endpoint returns 404 and the test lies.
10. **View-model tests.** In `tests/Pegasus.Desktop.ViewModelTests` *(created by
    [[TEST-004]], plan handle `DSK-08-04`)*: list scoping and freshness, preview
    inertness, prepare-then-confirm for link and unlink, the exact unlink
    sentence, classification version handling, the stable operation key across an
    uncertain-move retry, and the absent-move-control case.
11. **UI and accessibility.** Add `winapp ui` dialog scripts under
    `tests/Pegasus.Desktop.UITests` *(created by [[TEST-006]], plan handle
    `DSK-08-06`)* for the link and unlink confirmations, and run the
    `axe-windows` scan from [[TEST-009]] (plan handle `DSK-08-09`) on the list and
    message screens. Attach both artefacts.
12. **Tier-12 parity comparison.** On the local Test/UAT stack
    (`docs/desktop/08-testing/test-uat-stack.md`), for the same mailbox and folder
    scope, web and desktop must show the same retained messages and produce
    identical link and unlink outcomes, against the scenarios in
    `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` (2,045 lines).
    Record the table in the proof. Never an Azure test environment (L-02,
    ADR-0014).
13. **Documentation, three simplification passes, three PRs.** Update
    `parity-matrix.md` rows `PAR-21` and `PAR-22`; add the mail section to
    `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-08 (created by
    [[DUI-013]], plan handle `DSK-06-13` — contribute the content there if it has
    not landed); add the `DSK` rows to `docs/capabilities.md`. Run the
    simplification pass over **each** sub-slice's branch diff, record each under
    its own dated `## Simplification pass` heading below, then open the PRs into
    `dev` in the S10a → S10b → S10c order.

## Verification

Evidence tiers from the body: **5** (Web/API/MCP caller), **7**
(Browser/accessibility), **12** (Integrated workflow).

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — mail list/preview/message/command facts pass including provider-absent and
  cap cases (tier 5: authorization, versioning and exception translation per
  route).
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — scoping, preview, prepare/confirm and classification facts pass.
- `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script mail-link-unlink`
  — dialog assertions pass, including the exact unlink sentence (tier 7:
  keyboard, focus, dialog and semantic-label evidence from a real run).
- Parity table in the proof — message sets and link/unlink outcomes match the
  `MailWorkspaceWebTests.cs` scenarios on the same data (tier 12: the
  source-communication-through-to-case run on real retained mail, operator view
  and audit compared against the web).

## Risks / open questions

- **Landing it as one PR.** The single largest risk on this ticket. Mitigation:
  step 2 records the split in this document before any code, and each sub-slice
  has its own checkpoint, simplification pass and PR.
- **Parity drift.** upstream MAIL-011 and MAIL-012 arrive by the one-way sync.
  Mitigation: re-read both page models after the latest sync and record the SHA
  characterized (step 3). Owner of the sync: [[FND-023]] (plan handle `DSK-01-10`).
- **A second path to the same Core use cases.** [[AUTO-001]] — cite it as
  `upstream AUTO-003 (board [[AUTO-001]])` — adds
  `pegasus_mail_move_to_recommended_folder`, `pegasus_mail_case_link` and
  `pegasus_mail_case_unlink` to `src/Pegasus.Web/Mcp/MailMcpTools.cs` over the
  same use cases. Its own trap says "do not build a second path". Raise any
  overlap on [[AUTO-001]]. Answered by: [[AUTO-001]].
- **The `vertical-slices.md:404-407` "Absorbs upstream" line is wrong.**
  [[AUTO-001]] step 11 corrects it. Scope boundary, not a question — correcting
  it a second time here is forbidden by the ticket's Traps.
- **Uncertain-move retry could become a second move.** Mitigation: the operation
  key is held stable across the retry (step 8), and a view-model test asserts it
  (step 10).
- **Absent-versus-failed may not be distinguishable on the wire.** If
  [[GWY-012]] (plan handle `DSK-03-12`) returns a generic failure for the
  provider-absent case, the desktop cannot tell them apart and would have to
  guess, which the design authority forbids. Stop and raise it on [[GWY-012]].
  Answered by: [[GWY-012]].
- **Tier-12 evidence needs real retained mail locally.** If the local Test/UAT
  stack has none, the tier-12 table cannot be produced and the ticket stops;
  asking for an Azure test environment is out of bounds (L-02, ADR-0014).

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
each sub-slice's own branch diff before its PR, recorded here under a dated
heading per sub-slice (S10a, S10b, S10c)._
