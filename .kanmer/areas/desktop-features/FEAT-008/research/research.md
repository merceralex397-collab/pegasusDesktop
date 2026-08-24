# Research — FEAT-008: S8 Concurrency UX (conflict, lease lost, replay)

## Question

Which Core outcomes must the one shared conflict-and-recovery pattern render, what
does the existing transport map already disclose about each, and where does the
holder's name — including an Automation Actor holder — actually come from?

Read at `HEAD` `bbd1c549` (`git rev-parse --short HEAD`).

## Current behaviour

The web surfaces concurrency through `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs`
(**339 lines**) and the retained-proposed-values panel on
`src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` (**654 lines**). Every refusal
reaches the operator as one of two generic sentences —

> "The case action was not applied because the case changed, edit mode was lost, or
> the action is not permitted." (`CaseMutationPageModel.cs:121`)

> "The document could not be retained because the case changed, edit mode was lost,
> or custody is unavailable." (`Custody.cshtml.cs:132`, the transport variant)

— which **conflate four distinct Core outcomes into one message**. That conflation
is the thing this ticket exists to replace; it is not a defect in the web, which
has no way to branch a PRG redirect four ways, but it is not parity-worthy either.

Parity rows: this ticket has **no `PAR` row of its own**, and none should be
invented. The matrix holds **46** rows
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`),
each keyed to a page model under `src/Pegasus.Web/Pages/**`; concurrency is a
cross-cutting behaviour of the case rows (`PAR-08`–`PAR-12`), not a page. The
ticket's Documentation-changes block is right to ask for a **note** recording the
shared recovery pattern rather than a row. The closest existing mechanism is
`CaseMutationPageModel`'s `ExecuteCommandAsync` / `IsLeaseLoss` /
`RequiresReacquisition` triple (`:110-174`, `:292-304`), and that is what the
findings below characterize.

## Findings

### Facts

Each verified by reading the repository at `bbd1c549`.

- **Core raises four concurrency exceptions, not three, and their payloads
  differ.** All in `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs`:
  - `CaseVersionConflictException(caseId, expectedVersion, actualVersion)` at
    `:125-133` — carries **both** versions.
  - `CaseEditLeaseConflictException(caseId, caseVersion)` at `:135-142` — carries
    the case version and **no holder**.
  - `CaseEditLeaseExpiredException(caseId, caseVersion)` at `:144-150` — the same
    shape, a **different** meaning.
  - `CaseOperationConflictException(caseId, operationKey)` at `:152-158` — carries
    the operation key and **no version at all**.
  - So the desktop cannot render "the current version" for an operation conflict:
    that exception does not carry one. Either the gateway re-reads, or the state
    honestly says the version is unknown until reload.
- **Expiry is checked before ownership, so an expired lease held by someone else
  reports *expired*, not *conflict*.** `CaseEditAuthority.RequireLease`
  (`src/Pegasus.Core/Workflow/CaseEditAuthority.cs:39-65`) throws
  `CaseEditLeaseExpiredException` when the token is missing, the expiry has passed
  (`IsHeld`, `:24-25`), the retained hash is unreadable, or the retained holder is
  blank; only then does it compare holder and token and throw
  `CaseEditLeaseConflictException` (`:60-65`). The refusal order is stated as
  business policy in the class docstring (`:5-11`).
  - A recovery screen that treats "lease lost" as one condition will name a holder
    that Core never established.
- **`CaseEditAuthority.LeaseTokenLength = 64`** (`:14-18`), retained in a column of
  that exact width so a longer presented value can never round-trip.
- **The existing transport map deliberately withholds the holder — and this is the
  single most important finding for step 8.**
  `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` (**154 lines**) says so in its class
  docstring (`:7-16`): "The three edit-guard refusals name which guard refused and
  the current case version, so the Automation Actor can reload and reacquire rather
  than retry blindly; **no token or other holder material crosses the boundary with
  them.**" Its `CaseEditLeaseConflictException` branch (`:39-45`) says only "case
  edit authority is held by another actor".
  - The ticket requires the desktop's `lease-conflict` problem to **name** the
    holder. That is a deliberate widening of what the MCP map discloses, not a port
    of it — and it is legitimate, because the audience differs: the MCP boundary
    serves an Automation client, the gateway boundary serves an authorised staff
    editor whom FRD-01 explicitly entitles to see the holder (see the next fact).
    Say this out loud in the plan; an implementer who ports `AutomationMcpErrors`
    faithfully will produce an anonymous message and think it correct.
- **There is already a Core use case for naming the holder, and it carries the
  Automation rule.** `IDescribeCaseEditAuthorityHolder` and
  `DescribeCaseEditAuthorityHolder` (`CaseEditAuthority.cs:83-127`) return
  `CaseEditAuthorityHolder(string? DisplayName, bool IsAutomation = false)`
  (`:75-81`), with statics `.Unnamed` and `.Automation`.
  - Its docstring (`:68-74`): "A resolved account is named; an unresolvable one is
    described without an identifier, because the retained holder is a subject
    identifier and **an identifier is never operator-facing**. The Automation Actor
    is disclosed as itself: **ADR-0011 requires it to stay attributable without
    impersonating staff**, so it is never described as a member of staff."
  - The resolution rules (`:103-126`): `StaffAuthorization.Require(actor,
    PerformCasework)` — casework permission is enough, no administrator right
    needed; a holder subject id that is **not** a `Guid` is the Automation Actor
    (`:112-115`); `Guid.Empty` is `Unnamed`; an account that no longer resolves is
    `Unnamed`; otherwise the account's `UserName`.
  - **This, not `ActorDisplayNames` directly, is the mechanism step 2 is reaching
    for.** `src/Pegasus.Core/Actors/ActorDisplayNames.cs` is the general
    actor-to-name resolver (`Resolve`, `:50-68`, returning the constants
    `UnknownStaff`/`SystemWorker`/`Automation`/`RequestLink` at `:14-17`), and
    `DescribeCaseEditAuthorityHolder` is the case-lease-specific one layered on the
    same staff-account read. The ticket body's own Source-of-truth line already
    cites `CaseEditAuthority.cs:75-81` for exactly this, so the two halves of the
    body agree; the plan routes through the specific one because it is the half
    that carries the ADR-0011 rule.
- **The gateway problem catalogue already names four concurrency types, and
  "replayed" is not one of them.**
  `docs/desktop/03-gateway-api-and-data/README.md:167` fixes thirteen stable
  `urn:pegasus:problem:<slug>` type URIs, of which the concurrency four are
  **`version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`**.
  There is no `replayed` slug — correctly, because a replay is a **success**.
  - This reconciles the ticket cleanly. Step 2's list ("version conflict, lease
    conflict, operation conflict, and replayed") plus step 8's lease-lost path and
    step 9's replayed path resolve to: **four problem types** — the catalogue's
    four — **plus one success marker**. Step 10's "each of the four problem types"
    is therefore satisfiable and unambiguous once `:167` is read.
- **A replay succeeds; it does not throw.** The `ILeaseCaseForEdit` docstring
  (`CaseWorkflowContracts.cs:322-334`): "An exact claim or renewal replay returns
  the **same opaque token and expiry**, and an exact release replay returns
  success, **before** mutable-state, version, ownership, or expiry preconditions
  are evaluated. Reusing an operation key with **different request material** fails
  with `CaseOperationConflictException`. Actor authorization **always** precedes
  replay recovery."
  - So a replayed command returns 200 with the original outcome and is otherwise
    **indistinguishable from a new success** on the wire. Step 9 is only
    implementable if the gateway marks it explicitly. Nothing in Core does that for
    it.
  - `ICaseWorkflowQueries.HasOperationAsync(caseId, operationKey, ct)`
    (`CaseWorkflowContracts.cs:320`) is the read that can answer it.
  - The endpoint map agrees on the lease routes: "yes (key; **replay returns same
    token/expiry**)" and a response carrying "lease token, expiry, **holder**"
    (`docs/desktop/03-gateway-api-and-data/endpoint-map.md:54`).
- **`CaseOperationConflictException` is the one the existing map does not
  translate.** `AutomationMcpErrors.ExecuteAsync` (`:22-67`) matches
  `StaffAuthorizationException`, `CaseEditLeaseExpiredException`,
  `CaseEditLeaseConflictException` and `CaseVersionConflictException` explicitly,
  then falls through to a generic `ArgumentException or InvalidOperationException
  or InvalidDataException` branch (`:54-60`) that passes `exception.Message`
  through unchanged. `CaseOperationConflictException` derives from
  `InvalidOperationException`, so it lands there — and its message is "Operation
  '{operationKey}' was already applied to case '{caseId}' with different inputs.",
  which puts a raw case id on the boundary.
  - The gateway mapping must add an explicit branch. This is a real gap, and it is
    exactly what the ticket's step 3 anticipates: "Add the missing fields to the
    gateway mapping where they are absent."
- **The web collapses expired and conflict into one condition, deliberately.**
  `CaseMutationPageModel.IsLeaseLoss` (`:292-294`) — `exception is
  CaseEditLeaseExpiredException or CaseEditLeaseConflictException`. The desktop
  must **not** copy that: `lease-expired` and `lease-conflict` are separate problem
  types with different operator meanings, and the design authority's Case state row
  lists "lease held/expired/lost/stale" as four distinct states
  (`docs/design/README.md:775`).
- **A stale version also drops the client's held lease, and the reason is
  written down.** `RequiresReacquisition` (`:296-304`) is lease loss **or**
  `CaseVersionConflictException`, with the remark: "a stale version, because the
  requirement makes the rejected editor 'reload and reacquire rather than merge or
  force the save'. **Clearing this page's lease state does not release the
  server-owned authority, so a holder who did nothing wrong keeps it** and simply
  re-enters edit mode deliberately rather than saving over newer work."
  - The second sentence is the one the recovery screen must render honestly: after
    a version conflict the operator still holds the server-side authority; only the
    client's copy of the state was cleared.
- **The comparison must show editorial work and never an identifier, and the
  allow-list is explicit.** `RetainableFormFields`
  (`CaseMutationPageModel.cs:41-91`) is a `FrozenSet` of **43** field names, of
  which **7** are also in `BooleanFormFields` (`:93-106`). Its docstring (`:41-45`):
  "The values an operator types or chooses as case content. **Identifiers,
  versions, keys, tokens, and the fields that only route a command are never
  retained**, so the comparison shows editorial work and never an identifier."
  - The desktop's field-level comparison must honour the same discipline. It is the
    *selection rule* that travels, not the storage mechanism.
- **The comparison renders both columns in the same vocabulary.**
  `Details.cshtml.cs:526-534` (`DisplayValue`): "Renders a proposed checkbox value
  in the same words as the current one, so the two columns compare rather than
  reading 'true' beside 'Yes'."
- **The web offers no apply, merge or force — and says so in a docstring.**
  `Details.cshtml.cs:73-77`: "The values a refused editor submitted, held for
  comparison against the values the case now holds. **There is no control that
  applies, merges, or forces them: the only way forward is to enter edit mode again
  and retype.**"
  - The constraint behind this is authority-level, not an implementation choice:
    FRD-01 (`docs/frd/frd-01-case-identity-and-lifecycle.md:86`) — "The rejected
    editor keeps proposed values for comparison and must reload and reacquire
    rather than **merge or force the save**. There is no Administrator bypass,
    forced takeover, **collaborative merge**…"; `docs/design/README.md:721` —
    "reload/compare, and reacquire are the only recovery interactions… never
    overwrites the newer Case. There is no forced Administrator takeover… or
    collaborative merge control"; `screen-specs.md:193-197` — the same, adding
    "preserves proposed values **in memory** for comparison".
  - See `A-05-08-2` for how the ticket's "Keep mine" action sits inside this.
- **The budgets are a cookie artefact and do not travel.**
  `MaximumRetainedProposedCharacters = 8000` and
  `MaximumRetainedProposedValueCharacters = 2000`
  (`CaseMutationPageModel.cs:31-39`) exist because "Cookie TempData chunks across
  cookies". `Details.cshtml.cs:80-82` exposes `ProposedValuesWereDropped` and
  `ProposedValuesWereShortened` so "nothing is trimmed or discarded quietly". The
  desktop holds proposed values in a view model and needs neither the budget nor the
  two flags — a genuine simplification, and one worth stating so it is not mistaken
  for lost behaviour.
- **"Edit mode" is the settled operator word for the lease, and it already ships.**
  `lease` is banned from operator copy (`docs/design/README.md:412-420`), together
  with `caller` and `correlation identifier`. The web already writes "Edit mode is
  active until …" (`Details.cshtml.cs:178`), "Edit mode could not be entered because
  the case changed or is being edited by another member of staff." (`:197`), "Edit
  mode could not be renewed. Reload the case and enter edit mode again." (`:244`),
  "Edit mode was left safely." (`:268`).
  - The ban is a **review rule, not a CI check** — `docs/design/README.md:416-420`
    says so explicitly: "nothing in CI enforces it today, and claiming otherwise
    would be the kind of false assurance the evidence discipline above exists to
    prevent."
- **The state contract this pattern must satisfy is already enumerated.**
  `docs/design/README.md:769` (Mutations row) and its restatement at
  `docs/desktop/06-ui-design/screen-specs.md:422`: "Validation; confirmation;
  success; denied; **stale version; lease lost**; dependency unavailable;
  **idempotent/replayed result; conflict and recovery**."
- **upstream KANMER-005 has no fork ticket, and [[GWY-008]] (plan handle
  `DSK-03-08`) is its single owner.** `search_items` for `DSK-03-08` returns
  `GWY-008 · DSK-03-08 · Case command endpoints: create, save, lease, completeness,
  workflow and closure`, whose plan-03 row acceptance already reads "Each command:
  success, unauthorized, version conflict 409, lease conflict, replay returns same
  result, validation problem"
  (`docs/desktop/03-gateway-api-and-data/README.md:221`). Nothing on the fork board
  carries the upstream id, so it must never be written as a board wiki-link.

### Assumptions

Labelled per `docs/engineering.md` § plan sizing.

- **`A-05-08-1` — [[GWY-002]] (plan handle `DSK-03-02`) emits the four
  concurrency problem types with the payload fields this pattern needs.** Its
  plan-03 row acceptance is "exception → problem mapping tested for **each Core
  exception**" (`docs/desktop/03-gateway-api-and-data/README.md:215`), and the
  catalogue at `:167` names the four slugs.
  - *Confirmed by*: step 3 — reading the delivered mapping and
    `openapi/pegasus-v1.json`, and checking three specific payloads: `currentVersion`
    on `version-conflict`, a **named holder** on `lease-conflict`, and an explicit
    replay marker on the success path.
  - *Breaks if wrong*: the desktop cannot invent a version or a name it was not
    given. Step 3 adds the missing fields to the gateway mapping, which the
    ticket's scope boundary expressly permits ("the `/api/v1` problem-details
    mapping in `src/Pegasus.Web`").
- **`A-05-08-2` — "Keep mine" is a re-populate-after-reacquire action, not a
  merge or a force.** The ticket body's step 6 names Reload, **Keep mine** and
  Cancel; its § What says "reload, compare and **deliberately reapply**"; its step
  5 says the service "returns a reapply plan the operator confirms" and "**never
  resends the original body unchanged**".
  - *Reading*: after a successful reload **and reacquire**, the operator's proposed
    values are re-populated into the fresh editor as a starting point, and the save
    that follows is an **ordinary** save carrying the **new** `expectedVersion` and
    the **new** lease token. That is not a merge and not a force — the write passes
    the normal `CaseEditAuthority` guard at the current version — and it is the only
    reading compatible with FRD-01 `:86`, `docs/design/README.md:721` and
    `screen-specs.md:193-197`. It improves on the web only by sparing the retype
    that `Details.cshtml.cs:76` describes, which is a TempData limitation rather
    than a rule.
  - *Confirmed by*: the view-model fact in step 11 asserting that a reapply never
    carries the stale version or the old token, plus reviewer sign-off from
    `pegasus-desktop-reviewer`.
  - *Breaks if wrong*: if "Keep mine" were built to resend the operator's values
    over the newer record it would violate three authorities at once and the product
    invariant behind them. **That is a stop condition**, and the plan states it as
    one rather than leaving the phrase open to a data-losing reading.
- **`A-05-08-3` — the gateway can mark a replay on the success response.**
  `ICaseWorkflowQueries.HasOperationAsync` (`CaseWorkflowContracts.cs:320`) exists,
  and the lease routes already promise "replay returns same token/expiry"
  (`endpoint-map.md:54`).
  - *Confirmed by*: step 3's reading of the delivered response shape.
  - *Breaks if wrong*: step 9 is unimplementable and a replayed command is reported
    as a fresh success — precisely the defect the acceptance criterion forbids.
    Raised on [[GWY-002]] and [[GWY-008]], not worked around client-side.
- **`A-05-08-4` — [[GWY-008]]'s (plan handle `DSK-03-08`) two cross-actor lease
  facts land and pass.** They are restated verbatim in the ticket body's Source of
  truth, and its acceptance criterion is "A competing claim never replaces an
  unexpired lease holder, in either actor direction…".
  - *Confirmed by*: step 8's check, and the
    `--filter "FullyQualifiedName~DesktopGatewayCaseCommandTests"` run in
    Verification.
  - *Breaks if wrong*: the exclusion upstream KANMER-005 reports is live and
    unfixed. The body's instruction is unambiguous and this plan repeats it: **stop
    and raise it on [[GWY-008]]**. Do not model a takeover, do not add a
    client-side guard, do not claim parity around it.
- **`A-05-08-5` — [[DUI-010]] (plan handle `DSK-06-10`) supplies an `InfoBar`
  presentation that can host a compare pane beside it.** The control is "per-page
  InfoBar with operator sentence and copyable Reference".
  - *Confirmed by*: reading the delivered control in step 6.
  - *Breaks if wrong*: the compare pane becomes a sibling region rather than a
    child of the InfoBar. A layout change, not a scope change.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md` § 3,
answered for the conflict-and-recovery responsibility. This ticket genuinely
splits, and the split is the answer rather than a hedge: the **typed problems** are
a gateway responsibility, the **recovery experience** is a desktop one.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | The whole pattern exists because two actors contend for one case. `CaseEditAuthority` (`src/Pegasus.Core/Workflow/CaseEditAuthority.cs:5-11`) is "the single owner of the decision every staff case mutation is guarded by", and the holder disclosed by `DescribeCaseEditAuthorityHolder` is another user's identity. The authoritative half lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **No** | Nothing here runs unattended. A lease expires by **server time without a sweeper** — `CaseEditAuthority.IsHeld` (`:19-25`): "An abandoned lease expires without a sweeper, so every projection and guard asks this one question." There is no background job to place anywhere, and `src/Pegasus.Worker` is untouched. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | The only secret-shaped material is the 64-character edit-lease token (`CaseEditAuthority.cs:14-18`). It is short-lived, per-case and per-holder, held in memory only by [[FEAT-005]] (plan handle `DSK-05-05`), and never written to disk or a log. It is not a long-lived credential. Note that the recovery screen must never display it — the holder is named through `CaseEditAuthorityHolder`, never through the token. |
| Public callback — must an external service call a stable public endpoint? | **No** | No external party participates in a case-edit conflict. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | The refusal order is business policy and lives in Core (`CaseEditAuthority.cs:5-11`, `:39-65`); replay recovery happens **after** actor authorization (`CaseWorkflowContracts.cs:322-334`); and "there is no takeover, force, or bypass" is enforced server-side, not by a screen. FRD-01 `:88` — "Web and MCP Automation Actor callers use the same guard." Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **n/a** | No measurement exists either way and none is needed. Questions 1 and 5 place the authoritative half; the *presentation* half is placed by §4.1's default — interaction and rendering run on the desktop — not by this question. |

**Placement:** the gateway returns the four typed problems and the replay marker,
resolves the holder through `IDescribeCaseEditAuthorityHolder`, and enforces the
guard; the desktop re-queries, compares and offers reload / reapply / cancel, and
enforces nothing. Two "yes" answers, both naming the gateway, and one presentation
responsibility on the desktop by §4.1 default. **No Azure resource is involved and
no Azure write occurs.**

## Implications

- **Five outcomes, four problem types, one success marker.** The catalogue at
  `docs/desktop/03-gateway-api-and-data/README.md:167` already separates
  `lease-conflict` from `lease-expired`, and Core raises both. A pattern built on
  the web's `IsLeaseLoss` collapse (`CaseMutationPageModel.cs:292-294`) would show
  "another member of staff holds this case" to an operator whose own lease merely
  timed out — a message that is not just imprecise but wrong.
- **The lease-lost path of step 8 is `lease-expired`, not `lease-conflict`.**
  Expiry is checked first (`CaseEditAuthority.cs:51-58`), so an expired lease never
  reaches the ownership comparison and there is no holder to name. The
  holder-naming requirement in the acceptance criterion belongs to
  `lease-conflict`; `lease-expired` says the case is available to re-enter.
- **The gateway must widen what the MCP map discloses, and must be told to.**
  `AutomationMcpErrors.cs:7-16` deliberately keeps holder material off its
  boundary. FRD-01 `:84` entitles staff to more — "Other authorised staff remain
  read-only and **can see the holder** and recovery state" — and
  `DescribeCaseEditAuthorityHolder` requires only `PerformCasework` (`:108`), so
  the entitlement and the mechanism already agree. An implementer porting
  `AutomationMcpErrors` faithfully will produce an anonymous message and believe it
  correct; the plan must name this explicitly.
- **`CaseOperationConflictException` needs a new mapping branch and a redacted
  message.** It currently falls into the generic pass-through
  (`AutomationMcpErrors.cs:54-60`) and its message embeds a raw case id. The
  `operation-conflict` problem should carry the *fact* of the reuse, not the
  interpolated sentence.
- **"Replayed" cannot be detected client-side.** A replay is a 200 carrying the
  original outcome. If the gateway does not mark it, step 9 has nothing to render
  and the acceptance criterion "a replayed command shows the original outcome and
  is not reported as a new success" is unmeetable. This is the single hardest
  dependency in the ticket and it belongs to [[GWY-002]] and [[GWY-008]].
- **The mechanism is banned; the behaviour is required.** The ticket's trap says
  "do not reproduce retained proposed values", and it means the **cookie TempData
  machinery** — the 8000/2000 budgets, the chunking, the drop/shorten flags. It
  does **not** mean discarding the operator's typing: `screen-specs.md:195` requires
  the pattern to "preserve proposed values **in memory** for comparison", and
  `docs/design/README.md:624` requires that returning "never silently discards or
  replaces the operator's proposed values". Read carelessly, that trap becomes a
  data-loss defect. The plan states both halves in one sentence.
- **The comparison's field-selection rule travels even though its storage does
  not.** `RetainableFormFields` (43 fields, `CaseMutationPageModel.cs:41-91`) exists
  to keep identifiers, versions, keys and tokens out of the comparison. The desktop
  compares view-model editorial state against a fresh server read, so it gets the
  same discipline for free — provided it compares the *editorial* projection and
  not the whole DTO, which would put `version` and `id` rows in the pane.
- **Both columns must render in the same vocabulary.** `DisplayValue`
  (`Details.cshtml.cs:526-534`) exists because "true" beside "Yes" is not a
  comparison. The desktop has richer types and can get this wrong more ways —
  dates in particular, which must go through the shared Europe/London vocabulary
  (`vertical-slices.md` § Common to every slice).
- **The pattern is consumed before it is finished.** [[FEAT-006]] (plan handle
  `DSK-05-06`) already routes nineteen command refusals through it, including
  `CaseTaskVersionConflictException` — a **fifth** exception type from
  `src/Pegasus.Core/Tasks/CaseTaskContracts.cs:21-31` carrying a task-level version.
  That is task-scoped and belongs to [[FEAT-006]]'s endpoints, but the pattern must
  be shaped to take a version conflict whose subject is not the case.
- **This ticket asserts nothing about cross-actor lease exclusion.** The body is
  emphatic and repeats itself for a reason: [[GWY-008]] owns upstream KANMER-005's
  two facts, this ticket renders their outcome. The correct failure mode is to
  block, not to build a client-side guard — and the plan's Verification records the
  `DesktopGatewayCaseCommandTests` filter as a *blocker check*, not as this
  ticket's own evidence.

## Open questions

None that block the plan. `A-05-08-1`, `A-05-08-3` and `A-05-08-5` are settled by
step 3's and step 6's reading of the delivered gateway mapping and control set, and
each has a named consequence in the plan's *Risks / open questions* section.
`A-05-08-2` is a reading of the ticket body reconciled against three authorities
and recorded as a stop condition rather than a question — the body says "Keep
mine" and the body outranks this document; what the plan adds is the constraint
that keeps that action lawful. `A-05-08-4` is [[GWY-008]]'s to answer, and the body
already prescribes exactly what to do in both branches; that is a scope boundary
naming a sibling ticket, not an open question.

**No `open-questions` document is created:** the ticket body does not instruct one,
and nothing here is genuinely unsettled.
