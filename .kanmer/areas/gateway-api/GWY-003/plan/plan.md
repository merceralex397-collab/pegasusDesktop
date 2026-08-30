# Plan — GWY-003: staff bearer actor resolution and the per-group `StaffAccessRight` endpoint filter

**Diff estimate: ~6 files, ~430 lines.**

`docs/engineering.md` § Plan sizing requires the estimate first. Derived from the files document,
file by file:

| File | Change | Lines |
| --- | --- | --- |
| `src/Pegasus.Web/Api/StaffActorAccessor.cs` | new — one `StaffActorFactory.TryCreate` call, the non-staff/Automation-audience refusal, and the `ISecurityEventWriter` denial; modelled on `AutomationActorResolver.RequireAsync` (`:26-72`) and `DenyAsync` (`:74-90`), minus the registry and scope checks | ~70 |
| `src/Pegasus.Web/Api/RequireStaffRightFilter.cs` | new — the `IEndpointFilter`, the `RouteGroupBuilder.RequireStaffRight(StaffAccessRight)` extension, and the fail-fast-boundary XML doc | ~60 |
| `src/Pegasus.Web/Api/DesktopGateway.cs` (created by [[GWY-002]] (plan handle `DSK-03-02`)) | one added `const` — the `HttpContext.Items` key for the resolved actor | ~+4 |
| `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs` (created by [[GWY-002]]) | register the accessor (and `IHttpContextAccessor` if absent) inside `AddPegasusDesktopGateway`; no signature change | ~+5 |
| `src/Pegasus.Web/Api/DesktopGatewayProblems.cs` (created by [[GWY-002]]) | two added mappings — `account-disabled`, `password-change-required`; no existing branch reordered | ~+12 |
| `tests/Pegasus.IntegrationTests/DesktopGatewayAuthorizationTests.cs` | new — the twenty-seven facts plus the test-endpoint and principal harness; 78 files in this project already follow the `[Trait("Category","SqlServer")]` shape | ~280 |

The 280-line test estimate is the dominant term and is a real one: twelve rights across four
authorization shapes is a `TheoryData`-driven matrix (~120 lines of data plus two theory bodies), the
three extra facts are ~60 lines, and the harness that maps a right-carrying endpoint inside the group
and signs the matrix's principals in is ~100.

## Approach

Put the whole decision in **one** filter that resolves the actor once, refuses non-staff principals
before consulting any right, and then asks Core — `StaffAuthorization.IsAuthorized` — for the answer.
The rejected alternative was an `IAuthorizationRequirement`/policy per right, which is the more
conventional ASP.NET Core shape: it was rejected because a policy cannot hand the resolved
`ActionActor` to the endpoint handler, so every handler would re-resolve the principal, and the
endpoint map's Conventions section already names an "endpoint filter" as the mechanism. The cost is
that authorization sits slightly outside the framework's authorization pipeline; the mitigation is
that it delegates to Core rather than deciding anything, and that the twenty-seven facts observe it
over the real route.

Two choices inside that shape are load-bearing and argued from measurement:

- **Refuse the Automation audience before checking the right, not after.**
  `StaffAuthorization.IsAuthorized` admits `ActorKind.Automation` for `PerformCasework`
  (`src/Pegasus.Core/Identity/StaffAuthorization.cs:38-41`, ADR-0011), and `PerformCasework` is the
  right on nearly every row of `endpoint-map.md`. An Automation principal that reached `/api/v1` would
  therefore pass most of the surface. The audience/kind refusal is what stops it, and it is why the
  ticket's Traps say "reject it on audience/kind, not on rights".
- **Refuse by throwing `StaffAuthorizationException`, not by building a 403.** [[GWY-002]] already
  maps that exception to the `not-authorized` problem with the message discipline of
  `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:7-15`, and the exception carries the refused
  `Permission` (`StaffAuthorization.cs:69-78`). Constructing a response inside the filter would create
  a second translation point for the same refusal.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates GWY-003` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0102 (existing Pegasus credentials and identity store; the desktop session is a
> short-lived access token plus a rotated refresh token) governs this ticket and is **authored by area
> 04**, not here — the ticket body's *Documentation changes* section says so, naming `DSK-04-01`.
> ADR-0101 (local-execution / cloud-authority split) and ADR-0103 (gateway, never direct database
> access from workstations) are authored by [[FND-005]] (plan handle `DSK-00-05`); ADR-0100 is claimed
> by both [[FND-005]] and [[FND-026]] (plan handle `DSK-02-01`) — see [[FND-026]]'s plan for the
> ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table); if any of them lands
> differently this plan is revised before implementation.

Because `refs` is empty, these are the authorities that actually bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 8.3 | Authorization is enforced on the server | Steps 3–6 |
| Proposal § 10.1 | The API "enforces permissions even if a workstation is misconfigured" | Steps 4, 5 and the twenty-seven facts of steps 9–10 |
| Plan 03 § 3 *Projection style* (`README.md:162`) | Thin argument-mappers; no business rule in Web; MCP and API stay two ingresses over one Core | Step 6's XML doc; § Risks records the trap |
| Plan 03 § 3 *Problem details* (`:167`) | `not-authorized` is one of the thirteen stable slugs; `correlationId` always present | Step 5 throws into [[GWY-002]]'s single mapping |
| Plan 03 § 7 *Two policy engines* | "any rule that appears in an endpoint filter is a defect" | Step 6 |
| Plan 03 § 7 *Coexistence* | The cookie scheme defaults (`__Host-Pegasus`, `SameSite=Strict`) must not change when bearer is present | § Out of scope in the files document; no `Program.cs` authentication edit |
| Plan 03 § 7 *Rate limiting* | Reuse the existing limiter policies; do not add a second mechanism | Nothing added; `Program.cs:275-296` untouched |
| `endpoint-map.md` § Conventions | "**Auth right** is the `StaffAccessRight` checked by the endpoint filter" | Step 5's `RequireStaffRight` extension |
| ADR-0011 (existing) | The Automation Actor holds `PerformCasework` only | Step 4's audience/kind refusal, which is what keeps that grant from reaching the desktop surface |
| L-01 (locked) | One process, one Identity store; the API adds a scheme, not a deployment unit | No new project, no new host |
| L-04 (locked) | Every ticket names its subagent, skills and MCP tools | § Routing below |
| `AGENTS.md` § Product invariants | One business implementation; Web composes, Core decides | Step 3 calls `StaffActorFactory`; step 5 calls `StaffAuthorization` |
| `AGENTS.md` § Repository task workflow step 4 | A simplification pass over the branch diff before the PR | Step 12 |
| `docs/engineering.md` § Required evidence tiers, tiers 5 and 9 | An observable role matrix over the real route **plus** the denial-before-use audit record — not a unit test of the filter class | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`,
  plugin `dotnet-aspnetcore`) → `code-testing-agent` (dotnet/skills `98f84851`, plugin `dotnet-test`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve steps: same order, same ownership, same file paths.

1. **Orient.** Read `docs/desktop/03-gateway-api-and-data/README.md` § 3 and § 7,
   `endpoint-map.md` § Conventions, and
   `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 row `DSK-04-04` so the seam matches
   what [[GWY-021]] (plan handle `DSK-04-04`) registers. Read `StaffAuthorization.cs` whole — the
   twelve rights are four shapes and the test matrix depends on which. Call `get_doc_gates GWY-003`
   and `take_ticket` on branch `task/gateway-staff-authorization` from `origin/dev`.
2. **Confirm the dependency, or stop.** A bearer authentication scheme for `/api/v1` must exist and
   populate `ClaimTypes.NameIdentifier` and every `ClaimTypes.Role`. Confirm by reading [[GWY-021]]'s
   documents and by a smoke request through the test host. If it has not landed, **stop and record the
   blocker in this document**; do not invent a second token pipeline. Also confirm [[GWY-002]] has
   landed — `src/Pegasus.Web/Api/DesktopGateway.cs`, `DesktopGatewayExtensions.cs` and
   `DesktopGatewayProblems.cs` must exist, because steps 5–8 extend them.
3. **`src/Pegasus.Web/Api/StaffActorAccessor.cs`.** An `internal sealed` scoped service taking
   `IHttpContextAccessor`, `ISecurityEventWriter` and `TimeProvider` — the same constructor shape as
   `AutomationActorResolver` (`:20-24`) minus the registry. It calls
   `StaffActorFactory.TryCreate(principal.FindFirstValue(ClaimTypes.NameIdentifier), principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value), out actor)`,
   exactly as `src/Pegasus.Web/Pages/StaffPageModel.cs:12-15` does. **Do not** re-derive the parsing
   rules: `StaffActorFactory.cs:15-34` already fixes that the subject must be a non-empty `Guid`, that
   every role must parse with `ignoreCase: false` and be `Enum.IsDefined`, and that an empty role set
   fails. Copying those checks is how the two paths drift.
4. **Refuse non-staff principals before any right is consulted.** In the accessor: if the principal is
   unauthenticated, or carries the `AutomationMcp.Audience` (`"pegasus-automation-mcp"`,
   `src/Pegasus.Web/Mcp/AutomationMcp.cs:24` — use the constant, never a literal), or resolves to an
   `ActorKind` other than `Staff`, refuse with the 403 `not-authorized` problem. Write the denial
   through the existing `ISecurityEventWriter`, building the `SecurityEvent` the way
   `AutomationActorResolver.DenyAsync` (`:74-90`) does — `Guid.NewGuid()`, `SecurityEventType.Token`,
   `SecurityEventOutcome.Denied`, the subject (or `"anonymous"`), `timeProvider.GetUtcNow()`, a
   snake_case reason code in that file's vocabulary (`desktop_token_rejected`,
   `desktop_actor_not_staff`) — **but take the correlation id from [[GWY-002]]'s correlation filter,
   not from `HttpContext.TraceIdentifier`**. `SecurityEvent.CorrelationId`
   (`src/Pegasus.Core/Identity/IdentityContracts.cs:116-123`) is what joins the audit row to the
   `correlationId` the operator sees in the problem body; two different values make the trail
   unjoinable. Reuse the writer; a second audit path is the defect this step forbids.
5. **`src/Pegasus.Web/Api/RequireStaffRightFilter.cs`.** An `IEndpointFilter` carrying one
   `StaffAccessRight`: resolve through the accessor, then call
   `Pegasus.Core.Identity.StaffAuthorization.IsAuthorized(actor, right)`. On refusal **throw
   `StaffAuthorizationException(right)`** so [[GWY-002]]'s handler produces the single 403
   `not-authorized` problem — one translation point, and the exception already carries the refused
   `Permission` (`StaffAuthorization.cs:69-78`). Expose
   `RouteGroupBuilder RequireStaffRight(this RouteGroupBuilder group, StaffAccessRight right)` so each
   later group declares its right in one line; keep it to that single argument, because
   [[GWY-006]], [[GWY-007]], [[GWY-010]], [[GWY-012]], [[GWY-013]], [[GWY-015]] and [[PLAT-005]] all
   attach to this signature.
6. **State the boundary in XML documentation.** The filter is a fail-fast boundary, not a policy
   engine: every Core use case still calls `StaffAuthorization.Require` itself (for example
   `src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:16`). Quote the enum's own summary
   (`StaffAuthorization.cs:3-6`) — "Business-state preconditions remain owned by their feature use
   cases and are evaluated after this actor boundary succeeds." Never widen the filter with a business
   precondition; that is the *Two policy engines* trap in area 03 § 7.
7. **Hand the actor to the handler without re-resolving.** Register the accessor in
   `AddPegasusDesktopGateway` (`DesktopGatewayExtensions.cs`), and have the filter stash the resolved
   `ActionActor` in `HttpContext.Items` under a new `const` on `DesktopGateway` — the fixed-names class
   collects every such name in one place, as `AutomationMcp.cs` does. Change no existing member of
   either file, and do not alter `MapPegasusDesktopGateway`'s return type; thirteen downstream tickets
   depend on it returning the `RouteGroupBuilder`.
8. **Map, do not implement, the two identity states.** Extend `DesktopGatewayProblems.cs` with
   `account-disabled` and `password-change-required` mappings over whatever [[GWY-021]] signals. Do
   not query `UserManager` from an endpoint filter, and do not reorder the existing exception branches:
   all four case exceptions derive from `InvalidOperationException`, so the order [[GWY-002]] set is
   load-bearing. If [[GWY-021]]'s signal is not yet defined, record the mapping as deferred to
   [[GWY-021]] in this document rather than inventing a lookup.
9. **`tests/Pegasus.IntegrationTests/DesktopGatewayAuthorizationTests.cs` — the twelve-right matrix,
   written to the four measured shapes.** `[Trait("Category", "SqlServer")]`. Map one test endpoint
   inside the group per right, carrying `RequireStaffRight(right)`. Then:
   - **`AccessStaffApplication`, `PerformCasework`** — positive: a staff principal in **any** role
     succeeds. Negative: not a different staff role — every staff role passes
     (`StaffAuthorization.cs:35`, `:38-41`) — so the reachable negative is the Automation-audience
     principal, asserted here and again in step 9's dedicated fact.
   - **The eight management rights** (`ManageStaffAccounts`, `ReviewStaffAccess`, `AssignStaffRoles`,
     `ManageOrganizationsAndPrincipals`, `ManageWorkflowConfiguration`, `ManageApprovedMailboxes`,
     `ManageApprovedOutlookCategories`, `ManageAutomationClients`) — positive: a staff
     `Administrator`. Negative: a staff `Engineer` or `User` (`:43-52`).
   - **`ExecuteSystemWork`, `SubmitRequestUpload`** — **no staff actor of any role can ever be
     authorized**, because `StaffActorFactory.TryCreate` always returns `ActionActor.Staff(...)`
     (`StaffActorFactory.cs:36`) while these rights require `SystemWorker` and `RequestLink`
     (`StaffAuthorization.cs:54-55`). Their *positive* fact is therefore a **permanent-refusal** fact,
     named as such (`SystemWorkRightIsUnreachableForEveryStaffRole`), asserted across all three roles.
     **Do not** widen `StaffActorFactory` to make a conventional green test:
     `src/Pegasus.Core/Identity/**` is outside the Guardrails and the refusal is the correct
     behaviour.
   Then the three further facts: a disabled account is refused; an Automation-audience token is
   refused; an anonymous request receives **401** (note `SetFallbackPolicy(RequireAuthenticatedUser())`
   at `Program.cs:517-520` challenges the *cookie* scheme by default, so this fact genuinely tests that
   [[GWY-021]] attached bearer to the group rather than being a formality).
10. **Assert the audit side effect, not only the status code.** In at least the disabled-account and
    Automation-token facts, read back through the same `ISecurityEventWriter` port the MCP tests use
    and assert a `Denied` `SecurityEvent` exists with the expected reason code **and with the same
    correlation id the response carried**. That last clause is what makes step 4's correlation choice
    observable rather than a convention.
11. **Run.** `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayAuthorizationTests"`.
    Done means **27** facts pass, none skipped: 12 × 2 (with the two permanent-refusal positives above)
    plus 3. Then `grep -rn "StaffActorFactory.TryCreate" src/Pegasus.Web` — exactly two call sites.
12. **Simplify and close.** Run the simplification pass over the branch diff (`AGENTS.md` step 4) and
    record findings and dispositions under the dated `## Simplification pass` heading below. Open the
    PR into `dev`.

## Verification

Evidence tier **5 — Web/API/MCP caller** and tier **9 — Security/observability**
(`docs/engineering.md` § Required evidence tiers), as the ticket body states: an **observable role
matrix over the real route plus the denial-before-use audit record**, not a unit test of the filter
class.

The `proof` document is produced from these command logs:

1. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayAuthorizationTests"`
   — expected: 27 facts pass, **none skipped**. A skipped fact is a failure here; the two
   permanent-refusal facts must run and pass, not be `Skip`ped as "not applicable".
2. `grep -rn "StaffActorFactory.TryCreate" src/Pegasus.Web` — expected: exactly two call sites,
   `Pages/StaffPageModel.cs` and `Api/StaffActorAccessor.cs`.
3. `dotnet build Pegasus.slnx -c Release` — expected: `Build succeeded`, `0 Warning(s)`.
4. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release` (whole
   project) — expected: green, proving the cookie path and the MCP surface are unchanged.
5. Additionally, and not in the body:
   `grep -rn '"pegasus-automation-mcp"' src/Pegasus.Web --include=*.cs` — expected: exactly one match,
   the `const` at `Mcp/AutomationMcp.cs:24`. A literal in the new accessor is the duplication the
   fixed-names class exists to prevent.
6. Additionally: the audit assertion from step 10 appears in the test output — the denial
   `SecurityEvent` carries the same correlation id as the response, which is the only executable proof
   of step 4's correlation choice.

## Risks / open questions

- **Risk — the twelve-right matrix is written as twelve copies of one shape.** Two rights have no
  reachable staff success and two have no reachable role-based refusal. *Mitigation*: step 9 states
  the four shapes explicitly and names the permanent-refusal facts, so an implementer who hits a red
  test knows the test is right and the instinct to widen `StaffActorFactory` is wrong.
- **Risk — an Automation token reaches the casework surface.** `StaffAuthorization` grants
  `PerformCasework` to `ActorKind.Automation` by design (ADR-0011), and that right guards most of
  `endpoint-map.md`. *Mitigation*: step 4 refuses on audience/kind **before** any right is consulted,
  and step 9 gives it a dedicated fact.
- **Risk — a second claims → actor parser appears.** *Mitigation*: step 3 calls the Core factory and
  the `grep` in § Verification is the gate.
- **Risk — the audit trail cannot be joined to the failure the operator saw.** *Mitigation*: step 4
  takes the correlation id from [[GWY-002]]'s filter and step 10 asserts the two match.
- **Risk — the filter grows a business rule.** *Mitigation*: step 6's XML doc quotes Core's own
  summary; area 03 § 7 names it a defect; review checks it.
- **Risk — the anonymous fact passes for the wrong reason.** With
  `SetFallbackPolicy(RequireAuthenticatedUser())` (`Program.cs:517-520`) an endpoint with no bearer
  scheme attached challenges the cookie scheme and 302s. *Mitigation*: step 9 asserts **401**
  specifically, which fails if the bearer scheme is not attached to the group.
- **Scope boundary, not an open question — the bearer scheme and the enabled/security-stamp check.**
  [[GWY-021]] (plan handle `DSK-04-04`) owns them, and [[GWY-019]] (plan handle `DSK-04-02`) owns the
  OpenIddict `pegasus-desktop` client. Step 2 stops rather than improvising.
- **Scope boundary, not an open question — the identity lookups behind `account-disabled` and
  `password-change-required`.** [[GWY-021]] again; step 8 maps a signal only, and records deferral if
  the signal is not yet defined.
- **Scope boundary, not an open question — a per-user rate limiter for `/api/v1` writes.** Area 03 § 3
  states the intent; `Program.cs:275-296` is the one existing mechanism and a second is a Trap. Not
  this ticket.
- **Body imprecision worth recording, not a contradiction.** The body cites
  `src/Pegasus.Web/Mcp/AutomationMcp.cs:31` for the audience constant; measured, it is `:24`. It cites
  `IdentityContracts.cs:22-30` for `ActorKind`; measured, the enum is `:22-28`. It cites
  `AutomationActorResolver.cs:76-90` for the denial writer; measured, `DenyAsync` begins at `:74`.
  None of these changes an instruction, and every referenced construct exists as described.
- **No open question is opened on this ticket.** Everything unknown is settled by a command inside the
  ticket's own steps or by a named sibling ticket with an explicit stop condition.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._

## Dependency reconciliation — 2026-08-30

GWY-021 is now merged to main as 0e7fa423. Its implementation already contains DesktopActorResolver under src/Pegasus.Web/Desktop, which owns the per-request bearer/session checks and currently also calls StaffActorFactory.TryCreate before the group reaches an endpoint. That landed shape is authoritative and creates one necessary seam adjustment for this ticket: do not add a second claims parser in Api.

Implementation will extract the single claims-to-actor call and non-staff denial handling into the planned Api/StaffActorAccessor, have DesktopActorResolver delegate to that accessor after its GWY-021 account/session checks, and store the actor under the planned DesktopGateway item key. RequireStaffRightFilter will consume that stored actor through the accessor. This preserves GWY-021’s account boundary, gives downstream groups the planned reusable filter, and leaves exactly two StaffActorFactory.TryCreate call sites: StaffPageModel and StaffActorAccessor.

GWY-021 also already supplied the account-disabled and password-change-required problem mappings in DesktopGatewayProblems.cs; no duplicate mapping or reorder is needed. The existing VehicleAuthorizationEndpointFilter remains a legacy local filter for the already-landed vehicle/mail slices and is not broadened here. The ticket’s original folder and call-site wording is therefore interpreted through the landed dependency while preserving its acceptance intent: one Core factory owner for bearer claims and one reusable per-right transport boundary.
