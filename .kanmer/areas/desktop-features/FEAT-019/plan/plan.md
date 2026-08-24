# Plan — FEAT-019: S19 Administration

**Diff estimate: ~48 files, ~4,200 lines.** Derived from the files document: ten screens, each
costing roughly one view model plus one XAML page (~20 files, ~2,000 lines); ~7 contracts DTO files
(~450); ~8 gateway endpoint files covering the twenty routes in the endpoint map's administration
table (~800); ~9 test files — 5 contract (the authorization matrix is the bulk), 4 view-model
(~800); and 3 documentation files (~150). The People consolidation (accounts + roles + access
review into one area) is why ten screens do not become ten separate navigation entries.

## Approach

One view model per screen over the `/api/v1` administration group, with **the gateway as the only
authorization decision** — `StaffAuthorization.IsAuthorized` (`src/Pegasus.Core/Identity/StaffAuthorization.cs:29-57`)
stays the single fail-closed owner and the desktop hides entries for usability only. Accounts,
roles and access review consolidate into one **Administration › People** area (upstream PLAT-027).
Every mutation is an explicit command carrying an `operationKey` and, where Core requires one, a
reason through [[DUI-009]]'s (plan handle `DSK-06-09`) dialog contract; every sensitive mutation is
asserted to have written an audit record inside its own contract test, because FRD-04 `:29` makes
the history write part of the business transaction.

Rejected: **mirroring the rights matrix in the desktop so screens can validate before calling**. It
would create a second policy owner (`docs/engineering.md` § One Core owner) and would drift the
moment a right is added — and it buys nothing, because the gateway must refuse a forged call
regardless. Also rejected: **one Administration screen with tabs over a single "admin" right**,
which would collapse seven distinct rights into one and make the 403 matrix untestable.

## Governing docs

The ticket's `refs` is `docs/frd/frd-04-parties-accounts-and-access.md`, which exists.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-04 § `Staff role access matrix` (`:13-26`) | `Engineer` and `User` must not access accounts, roles, access review, principals, workflow configuration, the mailbox allowlist or authentication-client administration; authorization is enforced in Core use cases **and at every caller boundary**, failing closed without revealing case or source data | Step 4 (endpoints carry their own right), Step 11 (403 per endpoint, per non-holding right and for the Automation Actor) |
| FRD-04 § `Staff role access matrix` (`:24`) | No person, name, email address or bypass is hard-coded into authorization; automated processing uses a distinct durable machine identity and is not an independent policy owner | Step 11 (Automation Actor 403 fact on every endpoint), § Approach (no matrix in the desktop) |
| FRD-04 § `Permanent action history` (`:27-31`) | Every business mutation, material denial and automated result is recorded with actor, caller, time, policy/version, before/after values, outcome and reason; the history write is part of the transaction | Step 11 (audit-record assertion per sensitive mutation), Step 3 (per-screen tabulation of which writer each mutation uses) |
| FRD-04 § `Permanent action history` (`:33`) | Sign-ins and authentication failures stay in the **security log**, not the action history | Step 3 (the tabulation distinguishes `ISecurityEventWriter` from `IActionHistoryWriter` so the test asserts against the right store) |

`docs_todo: true`, confirmed in `get_doc_gates FEAT-019` — the `governing-doc` requirement at
`leave-backlog` reads `satisfied: true`.

> **New ADR** — ADR-0103 (gateway, evolved `Pegasus.Web`, never direct database access from
> workstations), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:158`); if the ADR lands
> differently this plan is revised before implementation. ADR-0102 (existing Pegasus credentials
> and identity store) is authored by the same ticket and also binds here through the disable →
> revoke path. ADR-0022 and ADR-0024 (approved mailbox identity) already exist and are not
> re-authored.

Programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 13.10 | Users, roles, reference-data administration, integration health, diagnostics and audit search on the desktop | Steps 5–8 |
| Proposal § 17 Security and privacy | Administration is administrator-only and audited; secrets are not exposed | Steps 8–11 |
| Plan 05 § 4, Phase 8 exit gate | Full automated suite passes; no unresolved high-risk security item | Steps 11–12, § Verification |
| `docs/engineering.md` § One Core owner | One implementation per rule; the desktop does not re-implement the matrix | § Approach, Step 8 |
| `docs/engineering.md` § Required evidence tiers (5, 7) | Tier 5 obliges route-level evidence per endpoint with the right action-history actor; tier 7 obliges keyboard, focus, validation-summary and semantic-label evidence from a real run of **every** screen | Steps 11–12, Step 13 |
| L-01 | The gateway owns authorization and audit | Steps 4, 11 |
| L-02 | Verification on the local Test/UAT stack | Step 13 |
| L-04 | Routing named on the ticket | § Routing |
| `docs/design/README.md:398-409`, `:412-445` | Approved copy is a closed list; a consequential action shows its consequence without hover; no how-it-works copy | Step 7 |
| ADR-0106 / `reuse-map.md` `Email/` row | Graph credentials never reach the desktop | Step 7 (folder resolution is a gateway call) |
| Operator decision, 2026-08-24 (Send to AI) | AI-09 is a recorded exclusion with a reactivation condition; the toggle is a setting, not capability work | Step 5's scope note; § Risks |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | A bare `<PREFIX>-<nnn>` is a fork board id; upstream ids are written `upstream <ID>` | Step 2 (carry-over check) |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` —
  `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` —
  `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`,
  `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`) →
  `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refining the ticket body's thirteen steps. Body step numbers in brackets.

1. **[body 1] Orient and take.** Read the plan row, `vertical-slices.md` § S19, the screen spec
   Administration section, FRD-04 § `Staff role access matrix`, and ADR-0022 and ADR-0024. Call
   `get_doc_gates FEAT-019`, then `take_ticket` with branch `task/dsk-05-19-administration` and
   worktree `../pegasus-worktrees/dsk-05-19-administration` from `origin/dev`.
2. **[body 1, refinement] Check the carry-over register before duplicating anything.** Read
   `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` for upstream PLAT-025,
   PLAT-026, PLAT-027, AUTO-006, AUTO-007 and PR-026, all absorbed here. Note the namespace trap:
   the board's own `PLAT-025`, `PLAT-026` and `PLAT-027` are `DSK-11-07`, `DSK-11-08` and
   `DSK-11-09` — entirely different tickets. The join table is in the `HZN-001` group document
   `board-conventions.md`.
3. **[body 2] Tabulate the ten in-scope screens.** Read each in-scope page model and
   `AdministrationPageModel.cs` (7 lines — a marker base, not the matrix). In `research`, tabulate
   per screen: the handlers, the Core use case each calls, the exact `StaffAccessRight` required,
   whether Core needs a version, an `operationKey` or a `reason`, and **which writer** each mutation
   uses — `ISecurityEventWriter` or `IActionHistoryWriter` (FRD-04 `:33` puts sign-ins in the
   security log, so a test asserting the wrong store passes vacuously). Record the SHA read.
   The in-scope set is `Configuration`, `MailCategories`, `Mailboxes`, `Access/Index`,
   `Accounts/Index`, `Accounts/Edit`, `Roles/Index`, `Automation/Index`, `Automation/Activity`,
   `Index` — ten page models. The five `Organizations/*` and `Principals/*` models are
   [[FEAT-007]]'s.
4. **[body 3] Confirm the endpoints.** Against [[GWY-015]] (plan handle `DSK-03-15`) and
   `endpoint-map.md` § `Administration and audit`, confirm every row has its own named route and
   its own right, including `POST /admin/mailboxes/{id}/resolve-folders`, the four accounts routes
   (disable requires a `reason` and revokes tokens), the two roles routes and the seven automation
   routes.
5. **[body 4] Add the DTOs.** One group per screen in `src/Pegasus.Contracts`, each carrying the
   resource's version. The channel-token rotate response carries the token **exactly once** and the
   DTO must not be persisted or logged by the client. The Send-to-AI toggle is carried as a plain
   setting field; no Send-to-AI capability work is added (recorded exclusion,
   `docs/capabilities.md:269`).
6. **[body 5] Implement the screens.** One view model per screen on [[DUI-007]]'s data-table
   pattern (plan handle `DSK-06-07`) and [[DUI-008]]'s form pattern (plan handle `DSK-06-08`).
   Consolidate accounts, roles and access review into one **Administration › People** area
   (upstream PLAT-027). Build the Activity screen from `Automation/Activity.cshtml.cs:23`'s read but
   **not** from its view: `Automation/Activity.cshtml:67` renders a raw `AggregateId`, which the
   desktop resolves to the Case/PO reference or omits (upstream PLAT-015, swept by [[FEAT-022]]).
7. **[body 6–7] Reasoned and consequential commands; folder resolution.** Every mutation Core
   requires a reason for uses [[DUI-009]]'s dialog contract (plan handle `DSK-06-09`). A destructive
   or consequential action — disable an account, clear a channel token — shows its consequence
   without hover, in approved copy from `docs/design/README.md:398-409`. Mailbox folder resolution
   is a distinct command that shows its result; the desktop calls the gateway endpoint and never
   Graph (ADR-0106).
8. **[body 8] Role-aware navigation.** Apply [[FND-046]]'s shell (plan handle `DSK-04-10`): an
   Administration entry is absent when the actor lacks its right. The gateway still refuses a forged
   call with a `not-authorized` problem — the hiding is usability, the 403 is the control.
9. **[body 9] One-time token reveal.** A rotated channel token is shown once, copyable, and never
   written to the local cache, a log or a diagnostics bundle. Add a view-model test asserting the
   value is gone after the dialog closes, and confirm against the bundle produced by [[FND-036]]
   (plan handle `DSK-02-11`). The wider secret scan is [[TEST-011]]'s (plan handle `DSK-08-11`).
10. **[body 4, refinement] Regenerate the contract artefacts.** `openapi/pegasus-v1.json` and the
    generated client, in this change.
11. **[body 10] Authorization contract tests, per endpoint.** In `tests/Pegasus.Api.ContractTests`,
    for **every** administration endpoint: 200 with the correct right; 403 `not-authorized` with any
    other right **and for the Automation Actor** (`StaffAuthorization.cs:44-52` gives it only
    `PerformCasework`); 401 without a token; 409 on a stale version; replay of the same
    `operationKey`; and an assertion that each sensitive mutation produced an audit record in the
    store the step-3 tabulation named. Enable `Features:DesktopGateway` explicitly — a gated-off
    endpoint returns 404 and would make a naive negative test pass for the wrong reason.
12. **[body 11] View-model tests.** Per screen: load, validation, reason-required commands, and the
    token non-retention rule.
13. **[body 12] Operator step.** Run the administration UAT script on the local Test/UAT stack
    covering a configuration change, a mailbox update and folder resolve, an access review, an
    account create and disable, a role assignment, and each automation control. Capture the
    operator's sign-off text and date in the ticket proof.
14. **[body 13] Documentation, simplification, PR.** Update the administration rows in
    `docs/desktop/01-inventory-and-parity/parity-matrix.md`, add the administration section to
    `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-04, add the `DSK` rows to
    `docs/capabilities.md`, run the simplification pass over the branch diff under a dated
    `## Simplification pass` heading, then open the PR into `dev`.

## Verification

Evidence tiers from the body: **5** (Web/API/MCP caller) and **7** (Browser/accessibility).

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — the authorization matrix and audit-record facts pass for every administration endpoint,
  including the Automation Actor 403.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — per-screen load, validation, reason and token-non-retention facts.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"`
  — existing administration web tests stay green; the Razor pages are untouched.
- **UAT record in the ticket proof** — a named operator's sign-off with a date, across every
  administration screen.

Evidence that becomes `proof`: the three test outputs (test-output tier) and the named UAT
sign-off. Tier 7's keyboard, focus, validation-summary and semantic-label evidence comes from a
real run of every screen; the automated `axe-windows` scan does not replace it
(`docs/engineering.md` § Required evidence tiers, tier 7).

## Risks / open questions

- **Re-implementing the rights matrix is the failure mode this slice invites.** Ten screens, seven
  rights, and a natural urge to validate client-side. Mitigation: § Approach forbids it; the
  reviewer checks for any `StaffAccessRight` switch in `src/Pegasus.Desktop*`; a second matrix is a
  stop condition.
- **A revealed secret must not reach the diagnostics bundle.** Mitigation: step 9's view-model fact
  plus a check against [[FND-036]]'s bundle; the wider scan is [[TEST-011]]'s. Owner of the
  end-to-end assurance: [[TEST-011]] (plan handle `DSK-08-11`).
- **The exact endpoint paths and rights** — owned by [[GWY-015]] (plan handle `DSK-03-15`). A scope
  boundary, not an open question; answer arrives when that ticket merges.
- **Disable → token revocation** — owned by [[GWY-022]] (plan handle `DSK-04-05`). The desktop's
  disable command tells the operator that sessions end; that statement is only true once
  [[GWY-022]] has landed. Mitigation: step 4 confirms the behaviour before the copy is written.
- **Namespace collision on the absorbed upstream tickets.** Board `PLAT-025` / `PLAT-026` /
  `PLAT-027` are `DSK-11-07` / `DSK-11-08` / `DSK-11-09` — different tickets from the upstream
  ones this slice absorbs. Mitigation: step 2, and the join table in `HZN-001`'s
  `board-conventions.md`.
- **The plan set says "sixteen page models"; the folder holds fifteen plus a 7-line base, and only
  ten are in scope.** Mitigation: step 3 fixes the in-scope list explicitly, so the parity rows and
  the screen count agree.
- **Send to AI** — a recorded exclusion with a reactivation condition
  (`docs/capabilities.md:269`), settled by the operator on 2026-08-24. The toggle is carried across
  as a setting. Not an open question; no `open-questions` document is created for it on any ticket.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
