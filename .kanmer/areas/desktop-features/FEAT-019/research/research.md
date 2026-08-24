# Research — FEAT-019: S19 Administration

Repository revision read: `bbd1c549` (`git rev-parse --short HEAD`, 2026-08-24). Every line number
below came from `grep -n` / `sed -n` at that revision.

## Question

Which administration screens this slice actually owns (the plan says "sixteen page models", which
is not what the folder contains), which `StaffAccessRight` gates each one, where the authorization
decision really lives, and what must never be retained after a channel-token rotation.

## Current behaviour

`src/Pegasus.Web/Pages/Administration/` contains **16 `.cs` files: 15 page models plus the shared
base** `AdministrationPageModel.cs` (7 lines). Measured with `wc -l`:

| Page model | Lines | Handlers (`grep -n`) | Owner |
| --- | --- | --- | --- |
| `Configuration.cshtml.cs` | 128 | `OnGetAsync` `:40`, `OnPostAsync` `:52` | this ticket |
| `MailCategories.cshtml.cs` | 74 | `OnGetAsync` `:24`, `OnPostSaveAsync` `:32` | this ticket |
| `Mailboxes.cshtml.cs` | 362 | `OnGetAsync` `:45`, `OnPostUpdateAsync` `:58`, `OnPostResolveFoldersAsync` `:167` | this ticket |
| `Access/Index.cshtml.cs` | 102 | `OnGetAsync` `:26`, `OnPostReviewAsync` `:37` | this ticket |
| `Accounts/Index.cshtml.cs` | 102 | `OnGetAsync` `:32`, `OnPostCreateAsync` `:43` | this ticket |
| `Accounts/Edit.cshtml.cs` | 96 | `OnGetAsync` `:22`, `OnPostDisableAsync` `:34` | this ticket |
| `Roles/Index.cshtml.cs` | 135 | `OnGetAsync` `:48`, `OnPostAssignAsync` `:59` | this ticket |
| `Automation/Index.cshtml.cs` | 260 | `OnGetAsync` `:45`, `OnPostSetEnabledAsync` `:57`, `OnPostSetSendToAiEnabledAsync` `:95`, `OnPostUpdateConnectorAsync` `:128`, `OnPostRotateChannelTokenAsync` `:168`, `OnPostClearChannelTokenAsync` `:207` | this ticket |
| `Automation/Activity.cshtml.cs` | 73 | `OnGetAsync` `:23` | this ticket |
| `Index.cshtml.cs` | 35 | — (landing page) | this ticket |
| `Organizations/Index.cshtml.cs` | 126 | | [[FEAT-007]] (plan handle `DSK-05-07`) |
| `Organizations/Edit.cshtml.cs` | 146 | | [[FEAT-007]] |
| `Principals/Index.cshtml.cs` | 31 | | [[FEAT-007]] |
| `Principals/Create.cshtml.cs` | 137 | | [[FEAT-007]] |
| `Principals/Replace.cshtml.cs` | 199 | | [[FEAT-007]] |

**Ten page models are in scope here**, not sixteen. The plan set's "sixteen" counts every `.cs`
file in the folder including the shared base; five of the page models are the organizations and
principals screens the ticket body already excludes. Every handler line number the ticket body
cites was checked and is correct at this revision.

`AdministrationPageModel.cs` is 7 lines — a marker base, not the authorization implementation. The
authorization decision lives in Core.

## Findings

- **The authorization matrix is one fail-closed switch in Core.**
  `src/Pegasus.Core/Identity/StaffAuthorization.cs:7-20` declares **twelve** `StaffAccessRight`
  values: `AccessStaffApplication`, `PerformCasework`, `ManageStaffAccounts`, `ReviewStaffAccess`,
  `AssignStaffRoles`, `ManageOrganizationsAndPrincipals`, `ManageWorkflowConfiguration`,
  `ManageApprovedMailboxes`, `ManageApprovedOutlookCategories`, `ManageAutomationClients`,
  `ExecuteSystemWork`, `SubmitRequestUpload`. `IsAuthorized` at `:29-57` is a `switch` ending in
  `_ => false`.
- **The Automation Actor is denied every management right, in Core, with the reason written down.**
  `StaffAuthorization.cs:38-42` comments that "The Automation Actor is granted only the ordinary
  operational casework surface (ADR-0011). Every management, configuration, credential,
  system-work, and request-upload right below stays denied for it, and unknown combinations fail
  closed." The eight management rights resolve to
  `actor.Kind == ActorKind.Staff && actor.IsInRole(StaffRole.Administrator)` (`:52`), and
  `PerformCasework` is the only right the Automation Actor holds (`:44-45`).
  - This is why the acceptance criterion names the Automation Actor explicitly, and why the
    desktop must **never** re-implement the matrix: it would become a second policy owner.
- **Seven of the twelve rights are in this slice's scope**, one per screen group:
  `ManageWorkflowConfiguration` (configuration), `ManageApprovedOutlookCategories` (mail
  categories), `ManageApprovedMailboxes` (mailboxes), `ReviewStaffAccess` (access review),
  `ManageStaffAccounts` (accounts), `AssignStaffRoles` (roles), `ManageAutomationClients`
  (automation). `ManageOrganizationsAndPrincipals` belongs to [[FEAT-007]].
- **Mailbox folder resolution is a Web-only Graph read.** The endpoint map row
  (`docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Administration and audit`) records
  "resolver is Web-only Graph read", consistent with `reuse-map.md`'s `Email/` row: "Graph
  credentials never reach the desktop (ADR-0106)". The desktop calls
  `POST /admin/mailboxes/{id}/resolve-folders` and never Graph.
- **The endpoint map already enumerates every route, each with its own right**, in
  § `Administration and audit`: `GET/PUT /admin/configuration`;
  `GET/PUT /admin/mail-categories`; `GET /admin/mailboxes`, `PUT /admin/mailboxes/{id}`,
  `POST /admin/mailboxes/{id}/resolve-folders`; `GET/POST /admin/access-review`;
  `GET /admin/accounts`, `POST /admin/accounts`, `GET /admin/accounts/{id}`,
  `POST /admin/accounts/{id}/disable` (**disable requires `reason`**, and "disabled → tokens
  revoked"); `GET /admin/roles`, `POST /admin/roles/assign`; `GET /admin/automation` plus five
  automation commands and `GET /admin/automation/activity`. Every mutation row is "yes (key)"
  idempotent.
- **`Automation/Activity.cshtml:67` renders a raw `AggregateId`** —
  `<td>@(record.AggregateId ?? "—")</td>` — and the same view uses
  `Pegasus.Web.Presentation.OperatorLabels.Humanise(...)` at `:64` and `:66` and
  `Model.SubjectLabel(record.SubjectId)` at `:65`. Upstream PLAT-015 names this Target column as one
  of the identifier breaches the conversion must not reproduce; [[FEAT-022]] (plan handle
  `DSK-05-22`) carries the sweep, and this slice is where the Activity screen is actually built, so
  the raw identifier must not be carried across in the first place.
- **FRD-04 makes the audit write part of the transaction**, not a follow-up:
  `docs/frd/frd-04-parties-accounts-and-access.md:29` — "A history write is part of the mutable
  business transaction; a failed write cannot leave an unrecorded successful mutation. History is
  append-only." That is why the audit assertion belongs inside the contract test rather than a
  separate observability ticket. The same file's `:13-26` staff role access matrix records that
  `Engineer` and `User` "must not access" accounts, roles, access review, principals, workflow
  configuration, mailbox allowlist or authentication-client administration.
- **Send to AI appears here as a toggle only.** `OnPostSetSendToAiEnabledAsync`
  (`Automation/Index.cshtml.cs:95`) is an administration setting. The capability itself is a
  recorded exclusion with a reactivation condition (`docs/capabilities.md:269`;
  `reuse-map.md:38` puts `AiWork/` out of parity scope). The toggle is carried across as a setting;
  no Send-to-AI capability work is pulled in.
- Parity-matrix rows: the administration screens are covered by the `PAR-` rows for §13.10; the
  matrix holds 46 `PAR-` rows in total (`grep -c '^| PAR-'`), each keyed to a page model under
  `src/Pegasus.Web/Pages/**`. The exact administration row numbers are confirmed against the matrix
  at implementation step 2 rather than guessed here.

### Facts

- 16 `.cs` files under `src/Pegasus.Web/Pages/Administration/`; 15 page models plus a 7-line base
  (`find … -name "*.cshtml.cs"`, `wc -l`).
- Twelve `StaffAccessRight` values; `IsAuthorized` ends `_ => false`
  (`StaffAuthorization.cs:7-20`, `:56`).
- `src/Pegasus.Core/Identity/StaffAccountAdministration.cs` exists and is the account
  administration owner (referenced by the endpoint map's accounts row).
- `src/Pegasus.Desktop`, `src/Pegasus.Contracts`, `tests/Pegasus.Api.ContractTests` and
  `tests/Pegasus.Desktop.ViewModelTests` do not exist yet (`ls src/`, `ls tests/`).

### Assumptions

- `A-05-19-1` — the channel token is returned exactly once by
  `POST /admin/automation/channel-token/rotate` and is not retrievable afterwards. *Confirm:* read
  `Automation/Index.cshtml.cs:168-206` and the Core rotate command before implementing step 9.
  *If wrong:* the "revealed once" rule is unenforceable client-side and the requirement moves to the
  gateway, which is where it belongs anyway.
- `A-05-19-2` — [[GWY-015]] (plan handle `DSK-03-15`) publishes every row in the endpoint map's
  administration table with the rights it names. *Confirm:* read the merged contract before writing
  the client. *If wrong:* only route names and DTO shapes change.
- `A-05-19-3` — the consolidation of accounts, roles and access review into one "People" area
  (upstream PLAT-027) does not require a Core change, only a navigation and screen change.
  *Confirm:* the three page models call three separate Core use cases with three separate rights;
  the consolidation is presentational. *If wrong:* a Core change appears that this slice's
  guardrails do not cover and it becomes a separate ticket.
- `A-05-19-4` — the audit records written by the sensitive administration mutations go through
  `ISecurityEventWriter` / `IActionHistoryWriter` (`src/Pegasus.Core/Identity/`). *Confirm:* the
  per-screen tabulation at implementation step 2 records which writer each mutation uses.
  *If wrong:* the contract-test assertion looks in the wrong table and passes vacuously.

## Execution placement

Six-question test from `docs/desktop/00-governance-and-workflow/README.md` § 3 (`:169-176`):

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Workflow configuration, the approved-mailbox allow-list and the approved Outlook categories are estate-wide settings every operator and the Worker read. Lands in the **gateway** (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **yes, for the effect** | The settings this screen writes are consumed by `src/Pegasus.Worker`'s poll and dispatch functions with every desktop closed (`reuse-map.md` § `Pegasus.Worker`). The *screen* is operator-driven; the **setting store** must be central. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The automation channel token and the connector configuration are credentials. They are issued and stored by the **gateway**; the desktop shows a rotated token once and retains nothing. Mailbox folder resolution uses the approved-mailbox Graph credential, which never leaves the server (ADR-0106, `reuse-map.md` `Email/` row). |
| Public callback — must an external service call a stable public endpoint? | **no** | Automation clients call *in* through the existing MCP ingress; this screen configures them and receives no callback. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | `StaffAuthorization.IsAuthorized` (`:29-57`) is fail-closed and denies the Automation Actor every management right (`:38-52`); FRD-04 `:29` makes the history write part of the transaction. All client-independent. Lands in the **gateway**. |
| Measured operational advantage — measured evidence central is materially better? | **yes, for the Graph folder read** | Resolving Outlook folders needs the approved-mailbox Graph credential and the existing `GraphApprovedSources` adapter (1,125 lines, `reuse-map.md` `Email/` row). Doing it centrally is not a preference; the credential cannot be on a workstation. |

Five "yes" answers, each naming **the gateway** — the existing `Pegasus.Web` Container App under
L-01 — not a new Azure resource. The desktop keeps form entry, immediate validation and list
presentation. No Azure write.

## Implications

1. **Ten screens, not sixteen.** The plan set's count includes the shared base and the five
   organizations/principals models. Building sixteen would duplicate [[FEAT-007]].
2. **Never re-implement the rights matrix.** `StaffAuthorization.IsAuthorized` is the single owner
   and it fails closed. The desktop hides or disables commands for usability only
   (`vertical-slices.md` § Common to every slice); [[FND-046]]'s (plan handle `DSK-04-10`)
   role-aware shell supplies the hiding, and the gateway still refuses a forged call.
3. **The Automation Actor is a real test case, not a hypothetical.** `PerformCasework` is the only
   right it holds, so every administration endpoint must return 403 for it — a contract fact per
   endpoint, as the ticket requires.
4. **The rotated token is the hardest requirement to prove.** "Never persisted to cache, log or
   diagnostics bundle" needs a positive test: a view-model fact that the value is gone after the
   dialog closes, and a scan of the diagnostics bundle produced by [[FND-036]] (plan handle
   `DSK-02-11`). The security lane in [[TEST-011]] (plan handle `DSK-08-11`) is the wider net;
   this slice owns the view-model fact.
5. **Do not carry the raw `AggregateId` across.** `Automation/Activity.cshtml:67` renders it today;
   the desktop Activity screen resolves it to the Case/PO reference or omits the column. Building it
   the web's way and waiting for [[FEAT-022]]'s sweep to catch it wastes both tickets.
6. **`disable` requires a reason and revokes tokens.** The endpoint map row says so, and
   [[GWY-022]] (plan handle `DSK-04-05`) owns the revocation. The desktop's disable command
   therefore carries a reason and the operator is told the account's sessions end — using approved
   copy only.

## Open questions

None that belong in an `open-questions` document.

- The exact administration endpoint paths and rights — owned by [[GWY-015]] (plan handle
  `DSK-03-15`). A decision a named sibling ticket owns is a scope boundary, recorded in the plan's
  *Risks / open questions*.
- Refresh-token revocation on disable — owned by [[GWY-022]] (plan handle `DSK-04-05`); same
  treatment.
- Send to AI beyond the existing toggle — a recorded exclusion with a reactivation condition
  (`docs/capabilities.md:269`), settled by the operator on 2026-08-24. No question is opened for it
  on any ticket.
