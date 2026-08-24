---
id: FEAT-010
type: ticket
title: 'DSK-05-10 · S10 Mail workspace (list, message, link/unlink, classify, move)'
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-5
  - tier-5
  - tier-7
  - tier-12
groups:
  - EPIC-006
  - HZN-006
links: []
blocks:
  - FEAT-022
  - FEAT-025
refs:
  - docs/frd/frd-08-email-mailbox-and-background-processing.md
docs_todo: true
archived: false
created: '2026-08-24T07:51:33.033Z'
updated: '2026-08-24T09:11:45.802Z'
---

## What

Deliver the native mail workspace in three sub-slices — S10a list and preview, S10b message detail with link and unlink, S10c classification correction and recommended-folder move — over the retained-mail endpoints, with Deleted Items search capped at the 100 newest and the move control absent when the provider is unavailable.

## Why

Proposal §13.4 and §13.8 require source emails, attachments, communication history and an explicit draft/queued/sent/failed distinction, correlated to a case. Today this is the largest page model in the repository: `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` (1,025 lines, seven handlers at `:199`, `:260`, `:318`, `:383`, `:448`, `:511` plus `OnGetAsync` at `:157`) with `Pages/Mail/Index.cshtml.cs` (428 lines, `OnGetAsync` and a JSON `OnGetPreviewAsync` at `:158`). The plan's "two giants" trap says it is never landed as one PR. Graph credentials stay in the Worker and gateway (ADR-0106) — no desktop holds them. Siblings: [[DSK-05-09]] supplies the received-item link/reverse-link plumbing, [[DSK-03-12]] and [[DSK-07-03]] supply the endpoints.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-10`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S10 · Mail workspace (DSK-05-10)`; § 7 of `README.md` ("The two giants" — split into S10a/S10b/S10c, never one PR)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Mail workspace`
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.4 Intake` → `Inbox` (approved mockups under `docs/design/references/mockups/inbox-message-page/`)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.4 Intake, § 13.8 Communications, § 12.1 Microsoft Graph intake
- Repository evidence: `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:158` (`OnGetPreviewAsync`), `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:157-560`, `src/Pegasus.Core/Intake/RetainedMail.cs` (`ListRetainedMail`, `GetRetainedMail`, `GetRetainedMailFreshness`, `SearchDeletedMail`), `src/Pegasus.Web/Presentation/MailBodyPresentation.cs` (43 lines), `MailClassificationSelection.cs` (102 lines); `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` (2,045 lines), `RetainedMailPersistenceTests.cs` (1,696 lines), `tests/Pegasus.IntegrationTests/Browser/MailWorkspaceBrowserTests.cs`
- Binding decisions: L-01 the gateway and Worker own Graph; L-02 verification runs on the local Test/UAT stack with the replay/absent provider; L-04 routing named on the ticket
- Depends on: `DSK-05-09` the received-item surface and the shared link/reverse-link commands; `DSK-03-12` the mail list, preview, message and command endpoints

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`; `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S10, the screen spec `Inbox` section, the approved mockups under `docs/design/references/mockups/inbox-message-page/`, and `docs/design/README.md` § `Voice, labels and necessary copy`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-10-mail-workspace` and worktree `../pegasus-worktrees/dsk-05-10-mail-workspace` from `origin/dev`.
2. Plan the split explicitly in the ticket plan: **S10a** list, freshness and preview; **S10b** message detail with prepare/link and prepare/unlink; **S10c** classification correction and move to recommended folder. Each sub-slice is its own commit series and its own PR into `dev`; the plan records the order and the checkpoint after each.
3. Read `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs` and `Message.cshtml.cs` in full and tabulate in `research`: the seven message handlers with their Core calls, which versions each command carries (classification version, recommendation version, mailbox version), where `reason` is required (move), and how the provider-absent case removes the move control. Record the SHA read — upstream MAIL-011 and MAIL-012 fixes arrive through the one-way sync and must be re-checked.
4. **S10a** — implement `MailListViewModel` over `GET /api/v1/mail?mailbox&folder&page&pageSize&q&deleted` with the mailbox and folder scope as dropdown filters, newest first, the freshness value rendered through the shared vocabulary map, and a coalesced manual refresh calling `POST /api/v1/mail/refresh`. Deleted Items search is capped by the gateway at the 100 newest — the desktop shows that cap honestly rather than implying completeness.
5. **S10a** — implement the preview pane over `GET /api/v1/mail/{id}/preview`, rendering inert text only. The desktop never renders remote HTML or loads remote content for a message.
6. **S10b** — implement `MailMessageViewModel` over `GET /api/v1/mail/{id}` (thread, attachments, classification, queue, outcome, association, move result, suggested move) and the prepare/link and prepare/unlink command pairs. The prepare step returns what the confirmation must state; the confirm step carries the message and receipt versions, the case `expectedVersion` and the `editLeaseToken` obtained through the session from [[DSK-05-05]].
7. **S10b** — the unlink confirmation must show exactly `Unlinking this email cancels case <reference>.` from the approved necessary-copy list in `docs/design/README.md`. Do not paraphrase it and do not add explanatory text around it.
8. **S10c** — implement classification correction over `POST /api/v1/mail/{id}/classification` (carrying the classification version) and the recommended-folder move over `POST /api/v1/mail/{id}/move-to-recommended-folder` (carrying classification, recommendation and mailbox versions plus a required `reason`). When the provider port is unavailable the move control is **absent**, not disabled with an explanation.
9. Add contract tests in `tests/Pegasus.Api.ContractTests` for every endpoint and command: success, 401, 403, stale version 409, replay of the same `operationKey`, provider-absent behaviour for move, and the Deleted Items cap. Enable `Features:DesktopGateway` explicitly.
10. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for list scoping and freshness, preview inertness, prepare-then-confirm flows for link and unlink, the exact unlink sentence, classification version handling, and the absent-move-control case.
11. Add `winapp ui` dialog scripts under `tests/Pegasus.Desktop.UITests` for the link and unlink confirmations (the dialog contract from [[DSK-06-09]]) and run the `axe-windows` scan on the list and message screens; attach the artefacts.
12. Run the parity comparison against the scenarios in `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs`: for the same mailbox and folder scope, web and desktop must show the same retained messages and produce identical link and unlink outcomes. Record the table in the ticket proof.
13. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` for the mail rows, add the mail section to `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-08, run the simplification pass over each sub-slice's branch diff, record each under a dated `## Simplification pass` heading, then open the PRs into `dev` in the S10a → S10b → S10c order.

## Acceptance criteria

- [ ] Web and desktop show the same retained messages for the same mailbox and folder scope, newest first, with the freshness time visible.
- [ ] Link and unlink outcomes are identical to the web; the unlink confirmation carries the approved sentence verbatim.
- [ ] The move control is absent when the provider is unavailable, and a move carries a reason.
- [ ] Deleted Items search is capped at the 100 newest and the cap is stated honestly.
- [ ] The preview renders inert text; no remote content is loaded.
- [ ] The slice ships as three PRs (S10a, S10b, S10c), never one.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: mail list/preview/message/command facts pass including provider-absent and cap cases.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: scoping, preview, prepare/confirm and classification facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script mail-link-unlink` — expected: dialog assertions pass, including the exact unlink sentence.
- [ ] Parity table in the ticket proof — expected: message sets and link/unlink outcomes match `MailWorkspaceWebTests.cs` scenarios on the same data.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 12 — Integrated workflow.
Tier 5 obliges route-level evidence per mail endpoint and command with authorization, versioning and exception translation; tier 7 obliges keyboard, focus, dialog and semantic-label evidence from a real run; tier 12 obliges the source-communication-through-to-case run on real retained mail, with the operator view and audit compared against the web.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — mail list, preview and message rows
- `docs/frd/frd-13-desktop-operator-experience.md` — mail section, citing FRD-08
- `docs/capabilities.md` — `DSK` rows for the mail workspace

## Guardrails

- **Azure**: no write. Graph credentials never reach the desktop (ADR-0106); the desktop calls only the gateway.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` mail group in `src/Pegasus.Web` and the test projects. Must not touch `src/Pegasus.Infrastructure/Email/`, `src/Pegasus.Worker`, or the Razor mail pages.
- **Traps**: the two giants — `Message.cshtml.cs` is 1,025 lines and this slice is split into S10a/S10b/S10c and never landed as one PR; the unlink sentence is an approved consequence sentence and must appear verbatim; a control that is unavailable is absent, not explained; parity drift — MAIL-011 and MAIL-012 arrive by upstream sync, so re-read the page models after the latest sync and record the SHA; upstream AUTO-003 (expose the email-workspace actions through the Automation Actor) is gateway-side and shares the same Core use cases — do not build a second path; `Features:DesktopGateway` must be enabled in tests.
- **Simplification pass** (`AGENTS.md` step 4): required over each sub-slice branch diff before its PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
