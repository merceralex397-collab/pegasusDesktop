---
id: AUTO-001
type: ticket
title: >-
  upstream:AUTO-003 · Expose the completed email-workspace actions through the
  Automation Actor
status: backlog
area: automation-integrations
assignee: ''
profile: feature
labels:
  - follow-up
  - MCP-05
  - mail-workspace
  - post-alpha
  - upstream-carryover
  - upstream-AUTO-003
  - gateway-worker-ticket
groups:
  - EPIC-014
links: []
blocks:
  - FEAT-026
refs:
  - docs/frd/frd-08-email-mailbox-and-background-processing.md
  - docs/frd/frd-10-mcp-automation-and-actor-boundary.md
docs_todo: true
archived: false
created: '2026-08-24T11:39:42.036Z'
updated: '2026-08-24T12:36:47.335Z'
---

## What

Extend the retained gateway MCP surface `src/Pegasus.Web/Mcp/MailMcpTools.cs` with thin, typed Automation Actor tools for the mail-workspace actions upstream `TICK-062` deliberately left out — recommended-folder move, Case link and Case unlink — each one calling the same Core use case the `/api/v1` mail endpoints call, under the single existing `automation.mail` scope, with no new business policy and no new scope.

## Why

The MCP tool surface is not a screen and does not die with the Razor front end. `docs/desktop/05-implementation-and-migration/reuse-map.md:87` marks `Mcp/` (14 files, ~3,200 LOC, 35 tools) **KEEP — the reference projection for the gateway**, and [[DSK-05-26]]'s cut list step 4 explicitly preserves "the whole `Mcp/` folder". So the Automation Actor's mail tool set survives the conversion exactly as it stands today: three tools — `pegasus_mail_list`, `pegasus_mail_get`, `pegasus_mail_correct_classification` (`src/Pegasus.Web/Mcp/MailMcpTools.cs:128`, `:189`, `:239`) — against a staff surface that can also file a message to its recommended folder and attach it to a case.

No seeded conversion ticket is a delivery of the missing tools. Every board mention of AUTO-003 is a *warning not to build a rival path*:

- [[DSK-05-10]]'s Guardrails trap reads, verbatim: "upstream AUTO-003 (expose the email-workspace actions through the Automation Actor) is gateway-side and shares the same Core use cases — do not build a second path".
- `docs/desktop/06-ui-design/screen-specs.md:268` records "Upstream carry-over absorbed: AUTO-003 (expose completed workspace actions to Automation — gateway side)".
- `docs/desktop/05-implementation-and-migration/vertical-slices.md:404` repeats "same Core use cases; gateway side".
- Parity row `PAR-46` (`docs/desktop/01-inventory-and-parity/parity-matrix.md:91`) records "`/mcp` unchanged" — which confirms the conversion neither delivers nor retires the tool surface.

[[DSK-03-12]] and [[DSK-07-03]] build the `/api/v1` mail routes and [[DSK-05-10]] builds the desktop screen over them; none of the three exposes an Automation Actor tool, and each is scoped to the desktop path. "Absorbed — gateway side" is a routing note, not an owner.

Operator-visible consequence: after cutover an Automation client can read retained mail and correct a classification, but cannot do the two things a member of staff does most often on that screen — file a message to its recommended folder and attach it to a case. Any automation of the mail workspace stops half-way and a human finishes it by hand, while the desktop client can do both. That is a capability regression relative to the staff surface, introduced by nobody deciding it.

## Source of truth

- Import decision: `coverage-decision.md` § Import list — AUTO-003; § Plan gaps — "Two surfaces the conversion assumes are covered are delivered by nobody: the Automation Actor's mail-workspace tool set, and the design screen map…"
- Carry-over register row: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:81` — disposition `gateway-worker-ticket`, plan areas "03 (MCP/API parity), 07", fork area `automation-integrations`
- Governing documents: `docs/frd/frd-10-mcp-automation-and-actor-boundary.md` § MCP automation and actor boundary (typed, scoped tools; actor attribution; denial before side effects); `docs/frd/frd-08-email-mailbox-and-background-processing.md` (mail behaviour and external-write constraints)
- Repository evidence:
  - `src/Pegasus.Web/Mcp/MailMcpTools.cs:118` `[McpServerToolType]`, `:128` `pegasus_mail_list`, `:189` `pegasus_mail_get`, `:239` `pegasus_mail_correct_classification` — the three tools that exist today (341 lines)
  - `src/Pegasus.Web/Mcp/AutomationMcp.cs:33` `MailScope = "automation.mail"`, `:38` the closed `Scopes` list
  - `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` (154), `src/Pegasus.Web/Mcp/AutomationActorResolver.cs` (237) — the content-safe error and actor conventions
  - `src/Pegasus.Web/Mcp/TriageMcpTools.cs:138-143` — `pegasus_triage_case_link` / `pegasus_triage_case_unlink`, the exact precedent shape for a case-link tool (`triageId, caseId, expectedTriageVersion, expectedCaseVersion, caseEditLeaseToken, reason, operationKey`)
  - `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:88` `MoveRetainedMailFolder`, `:134` `UnavailableRetainedMailFolderMover`
  - `src/Pegasus.Core/Intake/DurableIntake.cs:1106` `LinkIntake`, `:1148` `ReverseIntakeLink`; `src/Pegasus.Core/Workflow/CaseCommandContracts.cs:77` `IAcquireCaseEditLease`
  - `src/Pegasus.Core/Intake/RetainedMail.cs:109` `RetainedMailFolderRecommendation`, `:124` `RetainedMailSuggestedMove`, `:128` `RetainedMailDetail`
  - `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:157,199,260,318,383,448,511` — today's seven staff handlers (1,025 lines), deleted by [[DSK-05-26]]
  - `tests/Pegasus.IntegrationTests/AutomationMailIngressTests.cs` (446), `AutomationMcpIngressTests.cs` (516), `AutomationMcpTestSupport.cs`, `MailWorkspaceWebTests.cs`
  - `docs/desktop/03-gateway-api-and-data/endpoint-map.md:104,106` — the `/api/v1` link/unlink and move-to-recommended-folder rows
- Binding decisions: **L-01** the gateway is `Pegasus.Web` evolved in place, so this tool surface stays in the same host and no deployment unit is added; **L-02** verification runs on the local production-mimicking stack with the absent/replay provider; **L-04** routing is named on this ticket; **L-05** the fork board is the single work register; **D-001** upstream is frozen after one more sync, so nobody upstream will do this work.
- Depends on: [[DSK-03-12]] and [[DSK-07-03]] — the landed `/api/v1` mail contracts this ticket must match rather than invent; [[DSK-05-10]] — the desktop caller whose "do not build a second path" trap this ticket honours by reusing the same Core use cases.

### Upstream ticket AUTO-003 (verbatim)

Provenance — read 2026-08-24 from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111`:

- Upstream area: `automation-integrations`
- Upstream status: `preparing` (entered 2026-08-20T09:23:50.263Z)
- Upstream profile: `feature`
- Upstream labels: `follow-up`, `MCP-05`, `mail-workspace`, `post-alpha`
- Upstream groups: `EPIC-005`, `EPIC-006` — **upstream** group ids; they are unrelated to this board's `EPIC-005`/`EPIC-006`
- Upstream links: `TICK-047`, `TICK-049`, `TICK-050`, `TICK-051`, `TICK-052`, `TICK-053`, `TICK-054`, `TICK-056`, `TICK-057`, `TICK-064`, `TICK-088`, `TICK-062`
- Upstream refs: `docs/frd/frd-08-email-mailbox-and-background-processing.md`, `docs/frd/frd-10-mcp-automation-and-actor-boundary.md`

The body below is copied exactly and is not edited or paraphrased. Its `[[TICK-062]]` reference and its `EPIC-006` reference are upstream ids.

```markdown
## What

Complete MCP-05 by exposing the email-workspace Core queries and actions that were deliberately absent from [[TICK-062]] because their owning MAIL capabilities had not landed.

## Why

TICK-062 delivered retained-mail list/detail and classification correction only. EPIC-006 also requires thin Automation Actor callers for the completed folder recommendation/move, suggested actions, Case association and correction, message-state management, and outbound-mail capabilities without duplicating business policy.

## Approach

- Wait for each owning MAIL capability to land, then reuse its Core use case directly.
- Add only typed, scoped Automation Actor tools; do not introduce a generic mail-mutation framework or accept arbitrary destinations or recipients.
- Preserve the existing automation.mail authorization, exact-message identity, operation-key, concurrency, attribution and failure conventions.

## Verification

- [ ] Tool inventory and scope-denial tests cover every newly exposed action.
- [ ] Web and Automation callers produce equivalent Core outcomes and permanent history.
- [ ] No tool broadens Outlook/cloud authority beyond its owning MAIL capability.

## Outcome
```

The upstream `research`, `files` and `open-questions` documents are copied onto this ticket verbatim as well; read them with `get_ticket_doc` before planning.

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (the change lands in `src/Pegasus.Web`); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (the ingress and parity facts); `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `microsoft-code-reference` (Microsoft Learn plugin) → `code-testing-agent` and `run-tests` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/...`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `get_ticket_doc`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`) for `McpServerTool` attribute semantics and structured-content results
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <this ticket id>` before every move; a move crosses at most one gated boundary). Note the copied `open-questions` document below — a `feature` ticket cannot leave Preparing until every item on it is resolved.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read this body, then the upstream `research`, `files` and `open-questions` documents on this ticket via `get_ticket_doc`, then `docs/frd/frd-10-mcp-automation-and-actor-boundary.md` § MCP automation and actor boundary, `docs/desktop/05-implementation-and-migration/reuse-map.md:87`, and the carry-over row at `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:81`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/auto-003-mail-automation-tools` and worktree `../pegasus-worktrees/auto-003-mail-automation-tools` from `origin/dev`.
2. **Re-scope to what has actually landed — the upstream Approach re-expressed for the desktop era.** The upstream body lists five deferred families: folder recommendation/move, suggested actions, Case association and correction, message-state management (read/flag/delete/restore), and outbound mail. Three of those are owned by upstream capabilities this conversion drops as post-alpha and will not deliver — MAIL-13 (`TICK-054`), MAIL-17 (`TICK-075`) and MAIL-12 compose (`TICK-088`). Record in `research` the reduced, buildable set: recommended-folder **move** (`src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:88`), Case **link** and **unlink** (`src/Pegasus.Core/Intake/DurableIntake.cs:1106` and `:1148`, with `IAcquireCaseEditLease` from `src/Pegasus.Core/Workflow/CaseCommandContracts.cs:77`), and the recommendation/suggested-move values already carried on `RetainedMailDetail` (`src/Pegasus.Core/Intake/RetainedMail.cs:109`, `:124`, `:128`) and therefore already returned by `pegasus_mail_get`. State plainly that a dormant tool for an unlanded Core use case is forbidden by the upstream `files` document's Out-of-scope list.
3. **Re-point the parity oracle from Razor to the gateway — a deliberate re-expression of the upstream requirement, not a change to it.** The upstream `files` document names `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` as "the staff caller whose Core behavior the tools must match, not copy". [[DSK-05-26]] deletes that page model. Record in `files` that the matching caller for this ticket is the `/api/v1` mail group built by [[DSK-03-12]] and [[DSK-07-03]] — `POST /api/v1/mail/{id}/move-to-recommended-folder`, `POST /api/v1/mail/{id}/link-case`, `POST /api/v1/mail/{id}/unlink-case` (`docs/desktop/03-gateway-api-and-data/endpoint-map.md:104`, `:106`) — and that `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs` is a usable second oracle only for as long as the Razor page exists. The upstream requirement ("Web and Automation callers produce equivalent Core outcomes") is preserved unchanged; only the identity of "the Web caller" moves.
4. Add the tools to the **existing** `src/Pegasus.Web/Mcp/MailMcpTools.cs` — do not create a new tool type or a new file: `pegasus_mail_move_to_recommended_folder`, `pegasus_mail_case_link`, `pegasus_mail_case_unlink`. Copy the attribute shape from `src/Pegasus.Web/Mcp/TriageMcpTools.cs:138-143` verbatim in form — `[McpServerTool(Name = …, Title = …, ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]` — with explicit `expected…Version` parameters (classification, recommendation and mailbox versions on the move; message/receipt and case versions on the link pair), a required `reason`, a `caseEditLeaseToken` on the link pair, and an `mcp:`-prefixed `operationKey`. Done looks like: the file compiles and each new method is a delegation to a Core use case with no policy branch of its own.
5. Keep one scope. All three tools stay under `AutomationMcp.MailScope` (`src/Pegasus.Web/Mcp/AutomationMcp.cs:33`); do not add a per-action scope and do not extend the `Scopes` list at `:38`. The upstream `files` document records this as an explicit constraint ("do not create per-action scopes without a proven requirement").
6. Preserve the failure and attribution conventions: translate every Core failure through `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` and resolve the acting principal through `src/Pegasus.Web/Mcp/AutomationActorResolver.cs`. Scope denial must happen **before** any side effect (FRD-10). Do not add a second error map.
7. Handle the absent provider on the move tool. When the Graph port is not composed, `UnavailableRetainedMailFolderMover` (`src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:134`) is what is registered. The tool must surface that as a normalised, content-safe failure through the existing error convention — it must never construct a Graph client from MCP and must never report success. This is the Automation-side mirror of [[DSK-05-10]]'s desktop rule that an unavailable control is absent rather than explained.
8. Extend `tests/Pegasus.IntegrationTests/AutomationMcpIngressTests.cs` so the canonical tool inventory fact records the new total. Today the seven `*McpTools.cs` files carry 35 `[McpServerTool(` declarations (Assessment 5, Case 5, Document 3, Intake 2, Mail 3, Triage 13, Unidentified 4); after this ticket the expected total is 38.
9. Extend `tests/Pegasus.IntegrationTests/AutomationMailIngressTests.cs` with, for each new tool: success under `automation.mail`; scope denial with no side effect; exact-message identity (a wrong or unknown message id is refused, never coerced); stale-version conflict; replay of the same `operationKey` returning the same outcome without a second effect; durable actor attribution visible in permanent history; and, for the move, the provider-absent case.
10. Add the parity facts the upstream Verification demands: for the same seeded retained message, the Automation caller and the `/api/v1` caller produce the same Core outcome and the same permanent history entries. Record in the post-implementation report which oracle was used (`/api/v1` contract tests, and `MailWorkspaceWebTests.cs` while it still exists).
11. Update `docs/frd/frd-10-mcp-automation-and-actor-boundary.md` with the three tools and the authority boundary each inherits (no tool broadens Outlook or cloud authority beyond its owning capability). In the **same** commit make the three plan corrections listed under § Documentation changes, using the exact replacement wording recorded there: `docs/desktop/06-ui-design/screen-specs.md:268-269`, the § S10 bullet at `docs/desktop/05-implementation-and-migration/vertical-slices.md:404-406`, and the `AUTO-003` register row at `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:81`. Re-read each of the three lines before editing it and stop if it no longer reads as § Documentation changes quotes it. Touch only the S10 bullet in `vertical-slices.md` — § S9 of that document belongs to the imported upstream INTK-027 (board [[INTK-004]]), whose board id must never be read as upstream INTK-004. Hand the corrected `PAR-46` tool count to [[DSK-01-05]] rather than editing `docs/desktop/01-inventory-and-parity/parity-matrix.md:91` here — that row belongs to that ticket.
12. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in this ticket's `plan` document, then open the PR into `dev`.

## Acceptance criteria

- [ ] The upstream criterion, unchanged: tool inventory and scope-denial tests cover every newly exposed action.
- [ ] The upstream criterion, with the oracle re-pointed by step 3: the Automation caller and the `/api/v1` mail caller produce equivalent Core outcomes and equivalent permanent history for the same seeded message.
- [ ] The upstream criterion, unchanged: no tool broadens Outlook or cloud authority beyond its owning MAIL capability; the MCP path never constructs a Graph client.
- [ ] Exactly three tools are added, all inside `src/Pegasus.Web/Mcp/MailMcpTools.cs`, all under `automation.mail`, with `AutomationMcp.Scopes` unchanged.
- [ ] No tool exists for an unlanded Core use case; the dropped MAIL-12/13/17 families are recorded as out of scope with their upstream ids, not stubbed.
- [ ] Each new tool carries explicit expected versions, a required reason, an `mcp:`-prefixed operation key, and — on the link pair — a case edit-lease token.
- [ ] The provider-absent move returns a normalised content-safe failure and performs no side effect.
- [ ] No Core policy is duplicated: each tool delegates to the same use case the `/api/v1` endpoint calls.
- [ ] The three plan statements that still describe upstream AUTO-003 as absorbed or leave it unowned are corrected in the same edit and each names this ticket as the owner: `screen-specs.md:268-269`, the § S10 bullet of `vertical-slices.md:404-406`, and the `AUTO-003` row of `upstream-kanmer-carryover.md:81`. `parity-matrix.md` `PAR-46` is **not** edited here — [[DSK-01-05]] owns it.

## Verification

- [ ] `dotnet build ./Pegasus.slnx --configuration Release` — expected: succeeds with no new warning in `Pegasus.Web`.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~AutomationMailIngressTests"` — expected: every new authorization, identity, version, replay, attribution and provider-absent fact passes.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~AutomationMcpIngressTests"` — expected: the canonical tool-inventory fact passes at the new total.
- [ ] `git grep -c "McpServerTool(" -- src/Pegasus.Web/Mcp` — expected: the per-file counts sum to `38` (35 before this ticket).
- [ ] `git grep -n "AUTO-003" -- docs/desktop` — expected: exactly three hits — `screen-specs.md`, `vertical-slices.md` and `upstream-kanmer-carryover.md` — and none of them describes AUTO-003 as absorbed or leaves it without naming board ticket `AUTO-001`.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit code 0 after the FRD-10 edit.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 9 — Security/observability.
Tier 5 obliges observable evidence that each new tool route actually reaches Core with authentication, validation, scope, idempotency, exception translation and a durable action-history actor. Tier 9 obliges the denial evidence specifically: the scope check refuses before the client is constructed or the call is made, and no message content leaks into an error.

## Documentation changes

- `docs/frd/frd-10-mcp-automation-and-actor-boundary.md` — the three new tools and the authority boundary each inherits.
- `docs/desktop/06-ui-design/screen-specs.md:268-269` — the Inbox/Message block's last bullet reads today "Upstream carry-over absorbed: AUTO-003 (expose completed workspace actions to Automation — gateway side), MAIL-008 label maps." Rewrite it, keeping the file's existing line wrapping, as: "Upstream carry-over: MAIL-008 label maps absorbed. AUTO-003 (expose completed workspace actions to Automation — gateway side) is **not** absorbed here — it is owned by board ticket `AUTO-001` (`upstream:AUTO-003`) in `automation-integrations`, and this screen specification builds no Automation Actor tool." A screen specification cannot deliver an MCP tool and this ticket is that tool's owner, so the line must stop reading as absorbed. Make the edit before [[DSK-06-13]] adopts the block into FRD-13.
- `docs/desktop/05-implementation-and-migration/vertical-slices.md:404-406` — § S10's last bullet reads today "**Absorbs upstream**: AUTO-003 (expose completed email-workspace actions through the Automation Actor — same Core use cases; gateway side), MAIL-011/MAIL-012 fixes arrive via upstream sync." Rewrite it, keeping the file's existing line wrapping, as: "**Absorbs upstream**: MAIL-011/MAIL-012 fixes arrive via upstream sync. AUTO-003 (expose completed email-workspace actions through the Automation Actor — same Core use cases; gateway side) is **not** absorbed — it is owned by board ticket `AUTO-001` (`upstream:AUTO-003`), and this slice must not build a second path over the same Core use cases." Touch only the S10 bullet; § S9 of this document belongs to the imported upstream INTK-027 (board [[INTK-004]]) — the board id and the upstream id differ, so cite it in that full form.
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:81` — the AUTO-003 triage row's final `Fork area` cell reads `automation-integrations`. Replace that cell with ``automation-integrations — board ticket `AUTO-001`, created 2026-08-24`` so the register names the owner and the row stops reading as unowned. Change no other cell on the row, and change nothing in § Disposition categories — that section is [[DSK-01-09]] step 15's.
- `docs/desktop/01-inventory-and-parity/parity-matrix.md` `PAR-46` — **not edited here**. [[DSK-01-05]] owns that row: its step 10 counts the `pegasus_*` tools and its acceptance criterion pins the actual count. Step 11 hands it the corrected total.

## Guardrails

- **Azure**: no write. No Graph or other cloud client is constructed from the MCP path; Graph credentials stay in the Worker and the gateway composition under L-01, and the absent-provider case is a normalised failure, not a fallback call.
- **Scope boundary**: may touch `src/Pegasus.Web/Mcp/MailMcpTools.cs`, `tests/Pegasus.IntegrationTests/AutomationMailIngressTests.cs`, `tests/Pegasus.IntegrationTests/AutomationMcpIngressTests.cs` and `docs/frd/frd-10-mcp-automation-and-actor-boundary.md`. Must **not** touch `src/Pegasus.Core` policy (reuse the landed use cases), `src/Pegasus.Worker`, `src/Pegasus.Infrastructure/Email/`, the `/api/v1` route group (owned by [[DSK-03-12]] and [[DSK-07-03]]), `AutomationMcp.Scopes`, or the Razor mail pages.
- **Blocks**: this ticket blocks [[DSK-05-26]]. The cut list deletes `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` and `tests/Pegasus.IntegrationTests/Browser/`, which is the parity oracle the upstream Verification names; the cut list cannot correctly ship while an outstanding Automation ticket is measured against a surface it removes. Step 3 discharges that by re-pointing the oracle at `/api/v1` — once step 3 has landed, the block is satisfied by re-pointing rather than by completion.
- **Blocked by**: [[DSK-03-12]], [[DSK-07-03]] and [[DSK-05-10]]. The upstream `research` document is explicit — "Keep this ticket behind the owning MAIL tickets… re-read the landed Core contracts and reduce its scope to the actions that are actually available." Planning before those land would invent wire shapes.
- **Sibling, not duplicate**: [[DSK-05-10]]'s trap says do not build a second path. Step 4 honours it literally — the tools call the same Core use cases as the `/api/v1` endpoints and hold no policy of their own. If a plan step ever requires new Core behaviour, that is a different ticket.
- **Upstream label note**: `post-alpha` is carried verbatim as provenance. It records the upstream board's allocation of the *unlanded* MAIL capabilities, not a fork decision to defer this work; no horizon is set on this ticket because the carry-over phase is assigned by [[DSK-01-09]].
- **Traps**: the three plan statements above have exactly **one** owner — this ticket — and `PAR-46` has exactly one owner, [[DSK-01-05]]; never write a second copy of either correction; never add a generic mail-action envelope, a second policy engine or a duplicate taxonomy (`docs/engineering.md` § One Core owner); a dormant tool for an unlanded Core use case is forbidden; the upstream body's `EPIC-005` and `EPIC-006` are **upstream** group ids and do not correspond to this board's groups; upstream ids are written in full (`upstream:AUTO-003`), never abbreviated.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in this ticket's `plan` document.

## Outcome

_Filled at closeout._
