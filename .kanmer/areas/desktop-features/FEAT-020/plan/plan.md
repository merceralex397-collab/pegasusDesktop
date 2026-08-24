# Plan — FEAT-020: S20 Operations and integration health

**Diff estimate: ~19 files, ~1,350 lines.** Derived from the files document: 2 contracts DTO files
(~150), 2 desktop files extended rather than created — `OperationsViewModel` and
`OperationsPage.xaml` (~400), 4 gateway endpoint/health-consumption files (~350), 6 test files —
3 contract, 2 view-model, 1 UI script (~350), and 5 documentation files including the conditional
`docs/current-architecture.md:291` correction (~100). The estimate is small because [[FEAT-030]]
(plan handle `DSK-07-04`) already owns the screen; this slice adds commands and a panel to it.

## Approach

Extend the Operations screen [[FEAT-030]] owns with two audited commands and an integration-health
panel, taking **every eligibility decision from the gateway** — `CanRetry` and `CanRevoke` are
computed in Core (`src/Pegasus.Core/Operations/RequestOperations.cs:50-51`) and the client offers a
command only where the snapshot says it may. Before rendering the received-intake row, settle the
upstream `INTK-004` disagreement with [[GWY-013]] (plan handle `DSK-03-13`): either the snapshot
resolves the real case link through the single `IntakeReceipt.CurrentCaseId` path, or
`docs/current-architecture.md:291` stops claiming a join. Rendering the row while the sentence
stands is the one outcome that is not allowed.

Rejected: **inferring retry eligibility client-side from the failure code**, which is how a client
comes to offer a retry the server will refuse, and which would put a second copy of an eligibility
rule in the desktop. Also rejected: **a new `OperationsViewModel` for this slice's additions** —
one view model per screen, and [[FEAT-030]] owns this one.

## Governing docs

The ticket's `refs` is `docs/frd/frd-12-operator-experience.md`, which exists.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-12 § `Dashboard freshness and reconciliation` (`:93`ff) | A surface states when it was last read, and a failed load never claims to be fresh — the rule the web already implements at `Pages/Operations/Index.cshtml.cs:41-45` with an explicit FRD-12 citation | Step 6 (the panel and both lists carry `LoadedAtUtc` semantics), Step 10 (view-model fact for a failed refresh) |
| FRD-12 § `Operator experience` (`:4`ff) | The operator sees state without inferring it; a surface does not assert more than it knows | Step 6 (state as text, never colour alone), Step 4 (health names each dependency and its last cycle, nothing more) |
| FRD-12 § `Queues: tabs and filters` (`:58`ff) | Lists are presented with explicit filters rather than inferred grouping | Step 6 (two lists on [[DUI-007]]'s data-table pattern; filters are dropdowns per `docs/design/README.md`) |

`docs_todo: true`, confirmed in `get_doc_gates FEAT-020` — the `governing-doc` requirement at
`leave-backlog` reads `satisfied: true`.

> **New ADR** — ADR-0109 (desktop diagnostics bundle plus existing App Insights; no new telemetry
> fleet), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:164`); if the ADR lands
> differently this plan is revised before implementation. ADR-0103 (gateway; never direct database
> access from workstations) is authored by the same ticket and also binds. ADR-0106 and ADR-0107
> (Graph, Box and DVLA/DVSA credentials stay behind the gateway) are why the health panel can show
> states but not endpoints.

Programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 13.10 | Failed-work and retry screens plus integration health appropriate to administrators | Steps 5–7 |
| Proposal § 18.3 Health | Health is described **without secrets** | Step 4, Step 8 (a fact that the payload holds no secret-shaped value) |
| Proposal § 24, scenario 13 (`Pegasus_Native_Desktop_Design_Proposal.md:1652`) | "An integration failure is visible and recoverable" | Step 10 |
| L-01 | The gateway owns the snapshot, the retries and the audit | Steps 3, 6 |
| L-02 | Verification on the local Test/UAT stack, never an Azure environment | Step 10 |
| L-04 | Routing named on the ticket | § Routing |
| `docs/engineering.md` § One Core owner | One case-id resolution; a second copy is a stop condition | Step 3 |
| `docs/engineering.md` § Required evidence tiers (5, 7) | Tier 5 obliges route-level evidence with authorization, idempotency and exception translation; tier 7 obliges keyboard, focus, semantic-label and **text-plus-colour** evidence from a real run | Steps 8–9, § Verification |
| `docs/design/README.md:412-445` | No colour-only state; banned operator words; only populated sections render | Step 6 |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | A bare `<PREFIX>-<nnn>` is a fork board id; an upstream id is written `upstream <ID>` | Step 2 |
| Recorded trap PLAT-034 | App Insights quota can hide failures, so pilot evidence is the desktop diagnostics bundle, not a telemetry query | § Risks |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` —
  `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` —
  `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`)
  → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refining the ticket body's eleven steps. Body step numbers in brackets.

1. **[body 1] Orient and take.** Read the plan row, `vertical-slices.md` § S20, the screen spec
   Operations section and `docs/desktop/10-security-observability-performance/README.md` for what
   the health surface may disclose. Call `get_doc_gates FEAT-020`, then `take_ticket` with branch
   `task/dsk-05-20-operations` and worktree `../pegasus-worktrees/dsk-05-20-operations` from
   `origin/dev`.
2. **[body 2] Read the page and record the snapshot.** Read
   `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs` in full. Record in `research` what the
   snapshot projects, which items are retry-eligible and why, what revoking an upload link does,
   and the reason each command requires. Record the SHA read. Also record the namespace facts:
   neither upstream `PLAT-023` nor upstream `INTK-004` has a fork ticket; the board's `PLAT-023` is
   `DSK-11-05` and the board's `INTK-004` is upstream `INTK-027`
   (`HZN-001` / `board-conventions.md`).
3. **[body 3] Confirm the endpoints, then settle the honest-case-link question.** Endpoints:
   `GET /api/v1/operations` (`ETag`), `POST /api/v1/operations/external-work/{wid}/retry`,
   `POST /api/v1/operations/upload-links/{lid}/revoke` from [[GWY-013]], the integration-health
   payload from [[FEAT-027]] (plan handle `DSK-07-01`), and `GET /api/v1/admin/health`.
   Then the decision, with the evidence this research already gathered:
   - `RequestOperationProjection` — what the Razor page renders — carries a **non-nullable**
     `CaseId` and `CaseReference` (`RequestOperations.cs:35-36`);
   - `EmailOperationProjection`'s received-intake row hard-codes `CaseId: null`
     (`EfOperationsStore.cs:159`);
   - `GetEmailOperations` has **no caller** (`EmailOperations.cs:62` and
     `DependencyInjection.cs:240` are the only hits), so the desktop screen would be the first
     surface to render it;
   - `docs/current-architecture.md:291` claims those surfaces "join the current allocation state
     and actual Case link".
   **Either** the snapshot carries the real case link for a received-intake row — resolved through
   the single `IntakeReceipt.CurrentCaseId` path [[FEAT-013]] (plan handle `DSK-05-13`) uses, never
   a second copy — **or** the claim is removed from `docs/current-architecture.md`. A row must not
   report a link it does not join. Decide with [[GWY-013]], who owns the projection, and record the
   decision and its evidence here.
4. **[body 4] Add the DTOs.** The operations snapshot DTO and the health DTO in
   `src/Pegasus.Contracts`. The health payload names each dependency, its state and its last-cycle
   time — no connection string, endpoint credential, token or internal host name. Regenerate
   `openapi/pegasus-v1.json` and the generated client in this change.
5. **[body 5] Extend, do not create.** Check whether `OperationsViewModel` already exists from
   [[FEAT-030]]. If it does, add the retry and revoke commands in place and change no existing
   member; if it has not landed, create it with exactly the members [[FEAT-030]] step 3 pins
   (`ObservableObject`, `[RelayCommand]`, no UI type in the view model). **Record here which case
   applied.** Never a second view model for this screen.
6. **[body 5–6] The lists, the panel and the commands.** Retryable external work and active upload
   links as two lists on [[DUI-007]]'s data-table pattern (plan handle `DSK-06-07`). The
   integration-health panel shows each dependency's state **as text** — never colour alone — and its
   last-cycle time in Europe/London through the shared vocabulary map. Retry and revoke are explicit
   commands carrying an `operationKey` and the reason Core requires, showing the outcome inline;
   revoke carries the same six values the web handler takes
   (`Index.cshtml.cs:112-119`). A retry is offered **only** when the gateway says the item is
   eligible. Carry the freshness rule from `Index.cshtml.cs:41-45`: a failed refresh must not leave
   a stale "last read" on screen. Distinguish the three retry failure translations the web already
   makes (`:95-107`) rather than collapsing them into one error.
7. **[body 7] Feed state and minimum client version.** Show them from the compatibility surface
   built by [[GWY-023]] (plan handle `DSK-04-06`), so an administrator can see why a workstation is
   being blocked.
8. **[body 8] Contract tests.** In `tests/Pegasus.Api.ContractTests`: snapshot 200 with `ETag`,
   401, 403; retry success and retry of an **ineligible** item refused with a problem; revoke
   success and replay returning the same result (`RetryExternalWork` already exposes `IsReplay`,
   `Index.cshtml.cs:91`); a fact that the health payload contains no secret-shaped value; and — per
   step 3's recorded decision — **either** a fact that a received-intake row for an associated
   receipt carries the resolved case link, **or** a fact that the row carries no link at all and the
   document no longer claims one. Enable `Features:DesktopGateway` explicitly.
9. **[body 9] View-model tests.** List loading, eligibility-driven command enablement, retry and
   revoke outcomes, health-state rendering including an unavailable dependency, and the freshness
   rule after a failed refresh.
10. **[body 10] Scenario 13 on the Test/UAT stack.** Proposal `:1652` — "An integration failure is
    visible and recoverable." Cause an external-work failure, see it on this screen, retry it, and
    see it clear. Record the run in the ticket proof. [[TEST-016]] (plan handle `DSK-08-16`) owns
    the script; plan 08 references scenarios 1–14 but does not enumerate them, so read the proposal
    for the text.
11. **[body 11] Documentation, simplification, PR.** Update the operations rows in
    `docs/desktop/01-inventory-and-parity/parity-matrix.md`; add the retry and revoke command
    behaviour as a **sub-heading inside** the Operations screen section [[FEAT-030]] creates in
    `docs/frd/frd-13-desktop-operator-experience.md` — not a second screen section; apply step 3's
    `docs/current-architecture.md:291` correction if that is the recorded decision; add the `DSK`
    rows to `docs/capabilities.md`; run the simplification pass over the branch diff under a dated
    `## Simplification pass` heading; open the PR into `dev`.

## Verification

Evidence tiers from the body: **5** (Web/API/MCP caller) and **7** (Browser/accessibility).

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — snapshot, retry, revoke, no-secret-in-health and honest-case-link facts pass.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — eligibility, outcome, freshness and health-state facts pass.
- `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script operations`
  — keyboard traversal and the retry command pass; the `axe-windows` report is attached.
- **Scenario-13 record in the ticket proof** — an induced external-work failure is visible on this
  screen and recoverable from it, on the Test/UAT stack.

Evidence that becomes `proof`: the two test outputs, the UI script output with its axe report, and
the scenario-13 run record. Tier 7's text-plus-colour evidence for the health panel comes from that
real run — an automated scan does not replace it (`docs/engineering.md` § Required evidence tiers).

## Risks / open questions

- **The honest-case-link decision** — owner **[[GWY-013]]** (plan handle `DSK-03-13`), who owns the
  projection. The ticket body directs the decision to this plan, and step 3 records the evidence
  for it. The sharpened form: the Razor page's rows already carry a real link, the null one is in
  `EmailOperationProjection`, and nothing consumes that projection today — so the choice is taken
  *before* the desktop first renders the row. A scope boundary, not an open question.
- **A second case-id resolution is a stop condition.** Mitigation: step 3 names
  `IntakeReceipt.CurrentCaseId` as the single path and the review checks for any other.
- **A second view model for the Operations screen is a stop condition.** Mitigation: step 5 records
  which case applied and the reviewer checks `src/Pegasus.Desktop` for exactly one
  `OperationsViewModel`. Owner of the type: [[FEAT-030]].
- **Health disclosure.** Mitigation: step 8's negative fact asserts on payload shape rather than
  trusting a review; ADR-0106 and ADR-0107 keep the credentials themselves out of reach.
- **Colour-only state** is the health panel's natural failure mode. Mitigation: state as text in
  step 6, and tier-7 evidence from a real run in `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script operations`.
- **Namespace collisions.** Neither upstream `PLAT-023` nor upstream `INTK-004` has a fork ticket;
  the board's `PLAT-023` is `DSK-11-05` and the board's `INTK-004` is upstream `INTK-027`.
  Mitigation: step 2 records this in `research`; the join table is in `HZN-001`'s
  `board-conventions.md`. [[FEAT-023]] (plan handle `DSK-05-23`) owns upstream INTK-004's label
  half; this screen owns its Operations half.
- **PLAT-034 (recorded trap): App Insights quota can hide failures.** Mitigation: pilot evidence is
  the desktop diagnostics bundle from [[FND-036]] (plan handle `DSK-02-11`), not a telemetry query.
- **`GET /admin/health` is new**, not a rename of `/health/ready` — `src/Pegasus.Web/Health/`
  holds one check today. Owners: [[FEAT-027]] for the intake/integration payload and plan 10 for the
  admin health endpoint. If neither has landed, the panel ships with the dependencies that do exist
  and says so, rather than showing an empty panel that reads as "all healthy".

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
