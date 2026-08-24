# Research — GWY-003: staff bearer actor resolution and the per-group `StaffAccessRight` filter

## Question

What exactly does the claims → actor → right path have to do on `/api/v1`, given that `StaffAuthorization`
already fixes what each of the twelve rights means, and what does that fixed meaning imply for the
"positive and negative test for every right" the ticket body requires?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted:
`grep -c '^| PAR-' …` returns **46** — and every row is "keyed by the Razor page model and handler
group that implements it today" (`parity-matrix.md:3-5`). An endpoint filter is a boundary mechanism,
not an operator capability; each capability that *uses* it keeps its own row. (`PAR-04`,
`Account/AccessDenied.cshtml.cs`, is the closest thing, and `endpoint-map.md` § Stays web-only says
that page is "replaced by the `not-authorized` problem type" — this ticket is what produces it.)

The closest existing repository mechanisms — what does this job today:

- **For staff, `src/Pegasus.Web/Pages/StaffPageModel.cs:11-16`.** The whole claims → actor seam is
  five lines:
  `StaffActorFactory.TryCreate(User.FindFirstValue(ClaimTypes.NameIdentifier), User.FindAll(ClaimTypes.Role).Select(claim => claim.Value), out actor)`.
  It is a `protected` method on an abstract `PageModel` base, so the API cannot reuse the *member* —
  but it can and must reuse the *factory call*, which is the only thing in it.
- **For Automation, `src/Pegasus.Web/Mcp/AutomationActorResolver.cs:26-72`.** The fail-closed
  precedent, and a far richer one: it refuses an unauthenticated principal, refuses a disabled client
  registration, refuses a missing scope, and writes an attributable `SecurityEvent` on every material
  denial through `DenyAsync` (`:74-90`). Its XML summary (`:13-19`) states the rule: resolve "before
  any tool touches a use case", failing closed on each condition.
- **For the rights themselves, `src/Pegasus.Core/Identity/StaffAuthorization.cs`.**
  `IsAuthorized(ActionActor, StaffAccessRight)` at `:29-58` is a single fail-closed `switch` with a
  `_ => false` arm, and `Require` at `:60-67` throws `StaffAuthorizationException` (`:69-78`, carrying
  the refused `Permission`). Core is already the one policy engine; this ticket adds a fail-fast
  boundary in front of it, never a second one.

## Findings

### Facts

Read from the repository at fork `main`, 2026-08-24.

- **`StaffAccessRight` has exactly twelve values**, `src/Pegasus.Core/Identity/StaffAuthorization.cs:7-21`:
  `AccessStaffApplication`, `PerformCasework`, `ManageStaffAccounts`, `ReviewStaffAccess`,
  `AssignStaffRoles`, `ManageOrganizationsAndPrincipals`, `ManageWorkflowConfiguration`,
  `ManageApprovedMailboxes`, `ManageApprovedOutlookCategories`, `ManageAutomationClients`,
  `ExecuteSystemWork`, `SubmitRequestUpload`. The enum's own summary (`:3-6`) says it "Names the
  application boundary being authorised. Business-state preconditions remain owned by their feature
  use cases" — the *Two policy engines* rule, already written into Core.
- **The twelve rights are not twelve *role* checks; they are four distinct shapes.**
  `StaffAuthorization.IsAuthorized` (`:29-58`), measured:
  1. `AccessStaffApplication` → `actor.Kind == ActorKind.Staff`. **Any** staff role passes.
  2. `PerformCasework` → `actor.Kind is ActorKind.Staff or ActorKind.Automation`. Any staff role
     passes, **and so does an Automation actor** (ADR-0011, stated in the code comment at `:38-41`).
  3. Eight management rights (`ManageStaffAccounts`, `ReviewStaffAccess`, `AssignStaffRoles`,
     `ManageOrganizationsAndPrincipals`, `ManageWorkflowConfiguration`, `ManageApprovedMailboxes`,
     `ManageApprovedOutlookCategories`, `ManageAutomationClients`) →
     `actor.Kind == ActorKind.Staff && actor.IsInRole(StaffRole.Administrator)`.
  4. `ExecuteSystemWork` → `actor.Kind == ActorKind.SystemWorker`;
     `SubmitRequestUpload` → `actor.Kind == ActorKind.RequestLink`.
  Then `_ => false`.
- **Consequence, and it is the most important finding in this document: two of the twelve rights have
  no reachable "authorized success" on `/api/v1`.** `StaffActorFactory.TryCreate`
  (`src/Pegasus.Core/Actors/StaffActorFactory.cs:8-38`) ends in `actor = ActionActor.Staff(staffId, roles);`
  — it can only ever produce `ActorKind.Staff`. A staff bearer token therefore can **never** satisfy
  `ExecuteSystemWork` (needs `SystemWorker`) or `SubmitRequestUpload` (needs `RequestLink`). The
  body's "one authorized-success fact and one wrong-role-refused fact" per right must mean, for those
  two, that the *positive* fact is a **permanent refusal** fact: no staff actor of any role is ever
  authorized. Writing them any other way would require either widening `StaffActorFactory` (forbidden
  by the Guardrails — `src/Pegasus.Core/Identity/**` is out of scope) or fabricating a passing test.
- **Symmetrically, two rights have no reachable "wrong role" through role choice.**
  `AccessStaffApplication` and `PerformCasework` are satisfied by every staff role, so their negative
  case is only reachable with a non-`Staff` actor kind — which step 4 rejects earlier in the pipeline.
  Their negative facts are therefore the anonymous and Automation-audience cases, not a
  "staff user without the right" case.
- **`StaffActorFactory.TryCreate` fails closed on three distinct inputs.** `:15-18` returns `false`
  when the subject id is not a non-empty `Guid`; `:21-29` returns `false` when **any** role name fails
  `Enum.TryParse<StaffRole>(roleName, ignoreCase: false, …)` or `Enum.IsDefined` — note
  `ignoreCase: false`, so `"administrator"` is rejected; `:31-34` returns `false` when the role set is
  empty. A staff token with no role claim resolves to no actor at all.
- **`StaffRole` has three values** — `Administrator`, `Engineer`, `User`
  (`src/Pegasus.Core/Identity/IdentityContracts.cs:5-9`), with `StaffRoleNames.All` at `:18-19`.
  `ActorKind` has four — `Staff`, `SystemWorker`, `RequestLink`, `Automation` (`:22-28`; the body
  cites `:22-30`, measured `:22-28`).
- **The audience to reject is `pegasus-automation-mcp`**, `src/Pegasus.Web/Mcp/AutomationMcp.cs:24`
  (the body cites `:31`; measured `:24`). The Automation scheme is `PegasusAutomationMcp` (`:13`) and
  its endpoint policy `AutomationMcpEndpoint` (`:22`). **This rejection is not optional cleanliness:**
  because `PerformCasework` admits `ActorKind.Automation` by design, an Automation principal that
  reached `/api/v1` would pass every casework filter in the endpoint map. Rejecting on audience/kind
  before the right check is the only thing that stops it — exactly as the ticket's Traps say.
- **The security-event port is already the right shape.**
  `src/Pegasus.Core/Identity/IdentityContracts.cs:139-142` —
  `ISecurityEventWriter.AppendAsync(SecurityEvent, CancellationToken)`; the record at `:116-123` is
  `(Guid Id, SecurityEventType Type, SecurityEventOutcome Outcome, string SubjectId, DateTimeOffset OccurredAtUtc, string CorrelationId, string? ReasonCode = null)`.
  `SecurityEventType` (`:98-107`) offers `SignIn`, `PasswordChanged`, `Token`, `Client`, `RateLimited`,
  `SecurityStampChanged`, `SecurityConfigurationChanged`; `SecurityEventOutcome` (`:109-114`) offers
  `Succeeded`, `Denied`, `Failed`. `AutomationActorResolver.DenyAsync` (`:74-90`) builds one with
  `Guid.NewGuid()`, the type, `Denied`, the subject, `timeProvider.GetUtcNow()`,
  `httpContext.TraceIdentifier` and a snake_case reason code (`automation_token_rejected`,
  `automation_client_disabled`, `automation_scope_denied`). The API's reason codes should follow that
  vocabulary.
- **`SecurityEvent.CorrelationId` is a real join point with [[GWY-002]] (plan handle `DSK-03-02`).**
  The MCP resolver fills it from `httpContext.TraceIdentifier`; on `/api/v1` the correlation id is
  accepted-or-generated by `CorrelationIdEndpointFilter` and stored for the problem body. A denial
  record that used `TraceIdentifier` while the problem body carried a different `correlationId` would
  make the audit trail unjoinable to the operator-visible failure. They must be the same value.
- **The existing rate-limit rejection already writes security events with a path-derived reason
  code.** `src/Pegasus.Web/Program.cs:275-296`: `OnRejected` sets `Retry-After: 60` and chooses
  `sign_in_rate_limited`, `automation_rate_limited` or `authentication_rate_limited` by path.
  `/api/v1` currently falls into `authentication_rate_limited`. Adding a per-user limiter for `/api/v1`
  writes is area 03 § 3's intent but is **not** this ticket's — the Traps say "do not add a second
  limiter mechanism".
- **`AutomationActorResolver` is `internal sealed`** (`:20`) and is constructed with
  `IHttpContextAccessor`, `AutomationClientRegistry`, `ISecurityEventWriter`, `TimeProvider`. The new
  accessor should be the same visibility and take `IHttpContextAccessor`, `ISecurityEventWriter` and
  `TimeProvider` — the registry has no analogue because the desktop client's enabled/stamp check is
  [[GWY-021]]'s (plan handle `DSK-04-04`), per the ticket body's step 8.
- **`StaffAuthorizationException` carries the refused `Permission`** (`StaffAuthorization.cs:69-78`),
  which is what lets the 403 problem name the right without the client guessing. [[GWY-002]] maps that
  exception to `not-authorized`; this ticket's filter can either throw it (and let that mapping fire)
  or return the problem result directly. Throwing keeps one translation point.
- **`grep -rn "StaffActorFactory.TryCreate" src/Pegasus.Web` returns exactly one call site today** —
  `src/Pegasus.Web/Pages/StaffPageModel.cs:12`. The ticket's own verification expects **two** after
  this change; that is the executable form of "no second parser exists".

### Assumptions

- **A-GWY003-1 — [[GWY-021]] (plan handle `DSK-04-04`) has landed and populates
  `ClaimTypes.NameIdentifier` and every `ClaimTypes.Role` on the `/api/v1` principal.** *Confirms it*:
  the ticket body's own step 2, plus a smoke fact asserting a bearer-authenticated request reaches the
  filter with a resolvable actor. *If wrong*: **stop and record the blocker** — the body says so, and
  inventing a second token pipeline is the failure that instruction exists to prevent.
- **A-GWY003-2 — [[GWY-021]] signals the disabled-account and must-change-password states in a form
  this filter can map, rather than expecting this ticket to query Identity.** The body's step 8 says
  "do not implement the identity check here; only map it". *Confirms it*: read
  `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 row `DSK-04-04` and [[GWY-021]]'s
  documents before writing step 8. *If wrong*: the mapping is a no-op stub and must be recorded as
  deferred to [[GWY-021]], never resolved by adding a `UserManager` lookup inside an endpoint filter.
- **A-GWY003-3 — the `pegasus-desktop` bearer token carries an audience distinguishable from
  `pegasus-automation-mcp`.** Both flow through the same OpenIddict server in one process (L-01).
  *Confirms it*: the Automation-audience refusal fact in step 9. *If wrong*: the rejection must key on
  `ActorKind`/scheme instead of audience, and the fact must still exist — a shared audience would be a
  security defect to report, not to work around silently.
- **A-GWY003-4 — an `IEndpointFilter` is the right level for this, rather than an authorization
  policy.** The endpoint map's Conventions section names an "endpoint filter" explicitly, and a filter
  can stash the resolved actor for the handler (step 7) where a policy cannot. *Confirms it*: the
  twenty-seven facts passing. *If wrong*: an `IAuthorizationRequirement` per right is the fallback,
  but it would need a parallel mechanism to hand the actor to handlers, which is why the filter is
  preferred.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes — on the existing evolved `Pegasus.Web` gateway.** | Rights derive from role assignments held in the one Identity store every operator shares; a role revoked for one user must take effect for that user's next request from any workstation. `StaffAuthorization` (`StaffAuthorization.cs:29-58`) is the single Core boundary "shared by Web, Worker and later authenticated transports" (its own summary at `:23-26`). L-01 fixes the host as `Pegasus.Web` evolved in place — no new deployment unit, no new Azure resource. |
| Unattended execution — must it run with every desktop closed? | **No** | The filter runs per request. Nothing here executes without a caller. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No, for this ticket.** | This ticket consumes an already-authenticated principal; the token issuance, signing keys and Data Protection ring belong to [[GWY-019]] (plan handle `DSK-04-02`) and [[GWY-021]]. No secret is composed here. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing external calls `/api/v1`; the desktop is the only client. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and this ticket is that enforcement, on the existing gateway.** | Proposal § 10.1: the API "enforces permissions even if a workstation is misconfigured". A desktop build cannot be trusted to withhold an administration screen; the server refuses. The audit half is the same answer: material denials become `SecurityEvent` rows through `ISecurityEventWriter` (`IdentityContracts.cs:139-142`), the same permanent record the MCP ingress writes, which no client can produce or suppress. ADR-0102 (existing credentials and token session) is authored by area 04 and cited here. No Azure write. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | No measurement exists or is claimed. The placement follows from the two rows above and from proposal § 8.3, not from a benchmark. |

**Conclusion.** Four "no" and two "yes"; both "yes" answers land on the **already-running
`Pegasus.Web` Container App** under L-01. Nothing new is placed anywhere, and no Azure resource is
touched.

## Implications

1. **The twelve-right test matrix is not twelve copies of one shape.** Its four shapes are: two
   any-staff-role rights (`AccessStaffApplication`, `PerformCasework`), eight Administrator-gated
   rights, and two rights **no staff actor can ever hold** (`ExecuteSystemWork`, `SubmitRequestUpload`).
   The plan must say per right what "positive" and "negative" mean, or the implementer will write two
   tests that cannot both pass and then "fix" it by widening the actor factory.
2. **`PerformCasework` admitting `ActorKind.Automation` makes step 4 load-bearing.** Rejecting the
   Automation audience is not tidiness — without it, an Automation token passes the filter on every
   casework endpoint in `endpoint-map.md`, which is most of the surface. The rejection must key on
   audience/kind **before** the right check, and it must be its own fact.
3. **There is exactly one claims → actor implementation, and a `grep` proves it.** Call
   `StaffActorFactory.TryCreate` with `ClaimTypes.NameIdentifier` and every `ClaimTypes.Role`, exactly
   as `StaffPageModel.cs:12-15`. Do not re-derive the `Guid.TryParse`, `Enum.TryParse(ignoreCase: false)`
   and empty-role-set rules — they live at `StaffActorFactory.cs:15-34` and re-deriving them is how
   the two paths drift.
4. **The denial record must carry the request's correlation id, not `TraceIdentifier`.** `SecurityEvent`
   has a `CorrelationId` member; [[GWY-002]]'s filter accepts or generates the value the problem body
   reports. Using two different values makes the audit unjoinable to the operator's failure. The MCP
   precedent uses `TraceIdentifier` only because MCP has no correlation header.
5. **Throwing `StaffAuthorizationException` beats constructing a 403 in the filter.** [[GWY-002]]
   already maps that exception to `not-authorized` with the message discipline of
   `AutomationMcpErrors.cs:7-15`; throwing keeps one translation point and keeps the filter free of
   response construction. The exception also carries the refused `Permission`.
6. **The filter must stay a fail-fast boundary.** Core still calls `StaffAuthorization.Require` inside
   every use case (for example `src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:16`); the filter is a
   cheap early refusal, not the authority. An XML doc saying so is step 6's whole content, and any
   business precondition added to the filter is the *Two policy engines* defect.
7. **Rate limiting is already partly wired and must not be duplicated.** `Program.cs:275-296` already
   writes a `RateLimited` security event on rejection and picks a reason code by path; `/api/v1` falls
   into `authentication_rate_limited` today. A per-user write limiter for `/api/v1` is area 03 § 3's
   stated intent but belongs to a later ticket — adding a second limiter mechanism here is a Trap.

## Open questions

- None that must be answered before implementation. The right semantics are fixed in Core and measured
  above; the two dependencies ([[GWY-002]] for the group and problem mapping, [[GWY-021]] for the
  bearer scheme and the enabled/stamp signal) are named in the ticket body with an explicit stop
  condition, which makes them sequencing dependencies rather than unanswered questions. The four
  assumptions each name the command inside this ticket's own steps that settles them.
