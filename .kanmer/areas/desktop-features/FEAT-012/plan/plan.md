# Plan — FEAT-012: S12 Unidentified and vehicle images

**Diff estimate: ~21 files, ~2,200 lines.**

Derived from the `files` document, not asserted. `src/Pegasus.Contracts` — 4
files, ~300 lines (queue row tolerating a missing origin receipt, two details,
resolve bound to [[GWY-013]]'s widened shape, close, VRM suggestion, candidate
case); `src/Pegasus.Desktop` four view models plus four XAML files and two rail
entries — 9 files, ~880 lines (the four page models they replace total 373 lines,
but the promote control, paging and the reason dialogs are new);
`/api/v1` gap-closing in `src/Pegasus.Web` — 1 file, ~60 lines;
`tests/Pegasus.Api.ContractTests` — 2 files, ~470 lines (both queues' seven
endpoints plus the count-exclusion assertion plus the promote path's five cases);
`tests/Pegasus.Desktop.ViewModelTests` — 2 files, ~290 lines;
`tests/Pegasus.ArchitectureTests` — 1 file, ~60 lines (no second streaming
service, no second normaliser or validator); documentation — 4 files, ~140 lines.

## Approach

Build both queues as thin clients over [[GWY-013]] (plan handle `DSK-03-13`)'s
contracts, and put **nothing** about vehicle registrations in the desktop — not a
normaliser, not a format check, not a Triage call, not an origin-receipt lookup.
The alternative considered and rejected was validating the typed registration
client-side for a faster refusal: `src/Pegasus.Core/ImageIntake/ImageIntakeLifecycle.cs:169-173`
states in its own summary that `NormalizeRegistrationInput` is "the one owner" of
that transformation, and `TriageLifecycleRules.ValidateCreate` is the one judge of
validity, so a client-side check would be a second owner of both — a stop
condition under `docs/engineering.md` § One Core owner, and one that would drift
the moment the rule changed. The desktop therefore sends the typed registration,
renders the outcome and renders the refusal. Likewise the queue's "counts exclude
receipts that produced a case" rule is not re-implemented: it is
`item.State == Open` in `src/Pegasus.Infrastructure/Persistence/EfUnidentifiedStore.cs:250,259`,
so the desktop asks the endpoint and a contract test proves the endpoint kept it.

## Governing docs

The ticket carries `refs: ["docs/frd/frd-02-intake-and-source-identity.md"]` and
`docs_todo: true` (confirmed in `get_doc_gates FEAT-012`, which reports
`governing-doc` satisfied at `leave-backlog`).

**Meets — `docs/frd/frd-02-intake-and-source-identity.md`.** Steps 3 and 10 pin
that the queue membership rule, the resolve and close preconditions and the
source-identity fields are identical through `/api/v1` and through the Razor
pages; step 6 keeps the promote path's source identity intact by opening the
Triage from the **originating receipt** rather than creating new material. The
FRD is not modified by this ticket.

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the gateway;
> no long-lived provider secret in the package), authored by [[FND-005]]. Same
> condition — the authority for member source bytes being brokered.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review`:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §13.4, §13.5 | Both queues workable natively so nothing sits unresolved after the Phase 5 intake cutover | Steps 5, 7, 9 |
| Proposal §10.2 | Explicit reasoned commands, never a generic action endpoint | Steps 5, 6, 7 |
| `docs/operator-notes.md:42` | Keep it Unidentified "until a vehicle registration is known, then open the Triage" | Step 6 |
| `docs/design/README.md:535-546` | Settled vocabulary, exact and case-sensitive: `Unidentified`, `Vehicle images`, `Image reference` | Steps 1, 7, 11 |
| `docs/design/README.md` § No explanatory copy | VRM suggestions and candidate cases shown as data, with no explanation of how they were derived | Step 7 |
| `docs/desktop/06-ui-design/screen-specs.md:298-307` | Both details' section lists, "sections only when populated", the three AutomationIds | Steps 5, 7 |
| `docs/engineering.md` § One Core owner | One registration normaliser, one validator, one Triage-creation call, one streaming service | Steps 6, 8 and the architecture test at step 10 |
| L-01 | The gateway owns the commands and the audit | Steps 3, 6 |
| L-02 | Verification on the local Test/UAT stack | Steps 10–11 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 12 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `run-tests` → `winui-code-review` at
  review
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

These refine the ticket body's twelve implementation steps in the same order and
with the same ownership.

1. **Orient and take.** Read the plan row `DSK-05-12`,
   `docs/desktop/05-implementation-and-migration/vertical-slices.md:434-458`,
   `docs/desktop/06-ui-design/screen-specs.md:298-307` and the status vocabulary
   at `docs/design/README.md:535-546` — `Unidentified` is the settled word, never
   "Needs sorting". Call `get_doc_gates FEAT-012`, then `take_ticket` with branch
   `task/dsk-05-12-unidentified-vehicle-images` and worktree
   `../pegasus-worktrees/dsk-05-12-unidentified-vehicle-images` from `origin/dev`.
2. **Read the four page models in full and record.** Append to `research`, for
   each: the query it lists, the exclusion rule, the resolve and close command
   parameters (`expectedVersion`, `operationKey` **≤ 200**, `reason`), and the VRM
   suggestion and candidate-case fields. Note two things the naive reading misses:
   `src/Pegasus.Web/Pages/Unidentified/Index.cshtml.cs` is a **redirect**, not a
   list — the real queue is the fifth tab of
   `src/Pegasus.Web/Pages/Triage/Index.cshtml.cs:249-274` — and the exclusion rule
   is `item.State == Open` in
   `src/Pegasus.Infrastructure/Persistence/EfUnidentifiedStore.cs:250,259`, not a
   join against case links. **Record the SHA read.**
3. **Confirm the endpoints.** From [[GWY-013]]:
   `GET /api/v1/unidentified?page`, `GET /api/v1/unidentified/{id}`,
   `GET /api/v1/unidentified/{id}/members/{mid}/source`,
   `POST /api/v1/unidentified/{id}/resolve`; and `GET /api/v1/image-intake?page`,
   `GET /api/v1/image-intake/{id}`, `POST /api/v1/image-intake/{id}/close`.
   Verify the list counts apply the same `Open`-state exclusion the Razor queue
   applies, and that the count and the rows come from one query rather than two
   that can disagree.
4. **Contracts.** Add the DTOs to `src/Pegasus.Contracts` *(created by
   [[FND-029]], plan handle `DSK-02-04`)*, including the VRM suggestion with
   **confidence-free** presentation fields and the candidate case list with
   reference and status. The queue-row DTO must tolerate a **missing** origin
   receipt — `EfUnidentifiedStore.cs:252-262` left-joins because the origin can be
   a submission group. The resolve request DTO is [[GWY-013]]'s shape, restated in
   step 6 — bind to it, do not redesign it.
5. **Unidentified view models.** Implement `UnidentifiedListViewModel` and
   `UnidentifiedDetailViewModel` in `src/Pegasus.Desktop` *(created by
   [[FND-030]], plan handle `DSK-02-05`)* using the [[DUI-007]] (plan handle
   `DSK-06-07`) data-table pattern; resolve is an explicit reasoned command using
   the [[DUI-009]] (plan handle `DSK-06-09`) dialog contract.
6. **The promote control (upstream INTK-035 — absorbed here and in [[GWY-013]];
   there is no fork ticket for it).** Check first that [[GWY-013]] has landed the
   resolve contract, and restate its shape from that ticket's step 7 before
   writing a line of client code. The shape on
   `POST /api/v1/unidentified/{id}/resolve` is: `expectedVersion`, `operationKey`
   (≤ 200), `reason`, a **required** `targetKind`, `targetId`, an optional
   `targetReference`, and one new **optional** `registration`. `registration` is
   accepted **only** with `targetKind = Triage` — sent with any other kind it is a
   validation failure — and when it is present `targetId` is **absent**, because
   the endpoint derives it from the Triage it opens. An ordinary resolve sends no
   `registration` and is unchanged. Behind it, [[GWY-013]] normalises through
   `ImageIntakeLifecycle.NormalizeRegistrationInput`
   (`src/Pegasus.Core/ImageIntake/ImageIntakeLifecycle.cs:174`), opens the Triage
   from the **originating receipt** through `ICreateTriageFromIntake`
   (`src/Pegasus.Core/Triage/TriageContracts.cs:138`) with
   `TriageLifecycleRules.ValidateCreate` as the one judge, reuses an existing
   Triage on that receipt rather than creating a second, and then calls the
   existing `ResolveUnidentified` so the resolution is recorded with
   `UnidentifiedResolutionTargetKind.Triage`. **This ticket writes no registration
   normaliser, no format check, no Triage-creation call and no origin-receipt
   lookup** — it sends the typed registration, renders the outcome and renders the
   refusal. `ITriageQueries.GetByOriginReceiptAsync` does not exist in the fork
   today (`TriageContracts.cs:288-294` carries only `ListAsync` and `GetAsync`)
   and arrives with upstream INTK-033 (board [[INTK-007]]); [[GWY-013]] step 8
   owns resolving that after [[FND-023]] (plan handle `DSK-01-10`)'s sync. Record
   in `research` which case applied, and if the contract is not on the generated
   client, **stop and raise it on [[GWY-013]]** rather than building around it.
7. **Vehicle images view models.** Implement `VehicleImagesListViewModel` and
   `VehicleImagesDetailViewModel` the same way; close is an explicit reasoned
   command. Show VRM suggestions and candidate cases **as data**, without
   explanatory copy about how they were derived. Note the list is not paged in the
   Razor page (`src/Pegasus.Web/Pages/ImageIntake/Index.cshtml.cs:44-73`) but the
   endpoint map adds `?page` — do not assume the whole set arrives. Render every
   state through [[FEAT-023]] (plan handle `DSK-05-23`)'s label list, following the
   precedent `ImageIntake/Index.cshtml.cs:76-84` sets in its own comment.
8. **Reuse the streaming service.** Member source access uses [[FEAT-009]] (plan
   handle `DSK-05-09`)'s streaming download service — one implementation, not a
   copy.
9. **Shell rail.** Add both queues under Queues in the route order from
   `screen-specs.md` § `Shell`, with counts sourced from the rail-counts endpoint.
   An **absent** count renders nothing — not a zero.
10. **Contract tests.** In `tests/Pegasus.Api.ContractTests` *(created by
    [[TEST-001]], plan handle `DSK-08-01`)*: for both list, detail, resolve, close
    and source endpoints — success, 401, 403, 409 stale version, replay of the same
    `operationKey`, reason required; a list assertion proving receipts that
    produced a case are excluded from the counts; and for the promote path, five
    facts — a supplied registration opens exactly **one** Triage from the
    originating receipt and closes the Unidentified item; an invalid registration
    is refused and opens nothing; a receipt that already has a Triage does not gain
    a second; a `registration` with a non-`Triage` `targetKind` is a validation
    failure; a resolve with no `registration` is unchanged. These mirror
    [[GWY-013]] step 12's facts against the **generated client** — if one fails
    here but passes there, the client binding is wrong, not the endpoint. Enable
    `Features:DesktopGateway` explicitly.
11. **View-model tests.** In `tests/Pegasus.Desktop.ViewModelTests` *(created by
    [[TEST-004]], plan handle `DSK-08-04`)*: list paging, resolve and close
    requiring a reason, the promote command's `CanExecute` and its refusal path
    when the gateway rejects the registration, conflict handling through the shared
    [[FEAT-008]] (plan handle `DSK-05-08`) pattern, and correct vocabulary on every
    state.
12. **Documentation, simplification pass, PR.** Update `parity-matrix.md` rows
    `PAR-25` and `PAR-26`; add the promote control and its AutomationId to
    `screen-specs.md:298-307` — **this ticket's block, and not the
    `endpoint-map.md` resolve row, which is [[GWY-013]]'s**; add the section to
    `docs/frd/frd-13-desktop-operator-experience.md` (created by [[DUI-013]], plan
    handle `DSK-06-13` — contribute the content there if it has not landed); add
    the `DSK` rows to `docs/capabilities.md`. Run the simplification pass over this
    branch's diff, record it under a dated `## Simplification pass` heading below,
    then open the PR into `dev`.

## Verification

Evidence tiers from the body: **5** (Web/API/MCP caller), **7**
(Browser/accessibility).

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — both queues' list/detail/command facts pass, including the count-exclusion
  assertion and the promote path's opens-one-Triage, invalid-registration,
  already-has-a-Triage, wrong-target-kind and unchanged-ordinary-resolve cases
  (tier 5: authorization, idempotency and exception translation per route).
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — paging, reason-required, promote-command and conflict facts pass.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`
  — no second streaming implementation and no second registration normaliser or
  validator is introduced; dependency-direction facts stay green.
- Tier 7: keyboard, focus, semantic-label and text-plus-colour evidence from a
  real run of **both** screens, captured with the [[TEST-006]] (plan handle
  `DSK-08-06`) harness and the `axe-windows` scan from [[TEST-009]] (plan handle
  `DSK-08-09`).

## Risks / open questions

- **The resolve contract may not have landed.** [[GWY-013]] owns it. Step 6 is a
  hard gate: if `registration` is not on the generated client, stop and raise it
  there. Building a client-side workaround is a stop condition.
  Answered by: [[GWY-013]].
- **The Triage-reuse rule depends on a lookup that does not exist yet.**
  `ITriageQueries.GetByOriginReceiptAsync` is absent from the fork
  (`src/Pegasus.Core/Triage/TriageContracts.cs:288-294`) and arrives with upstream
  INTK-033 (board [[INTK-007]]). Resolving it after [[FND-023]]'s sync is
  [[GWY-013]] step 8's. Scope boundary with a named owner, not an open question.
- **Two different operation-key bounds.** 200 here
  (`src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:398`), 100 in
  administration. Mitigation: the bound is read from the area's own contract, never
  from a shared client constant.
- **A queue row can have no origin receipt.** The left join at
  `EfUnidentifiedStore.cs:252-262` allows a submission-group origin. Mitigation:
  the DTO and view model tolerate missing file name, subject and sender, and a
  view-model test covers it.
- **Count-versus-rows disagreement.** The web gets consistency from one query
  (`src/Pegasus.Web/Pages/Triage/Index.cshtml.cs:263-274`). Mitigation: the desktop
  renders the count the endpoint returns and never computes its own; step 3
  verifies the endpoint kept the property.
- **Vocabulary drift.** `Unidentified`, `Vehicle images` and `Image reference` are
  exact and case-sensitive; "Needs sorting" is retired
  (`docs/design/README.md:535-546`). Mitigation: every state renders through
  [[FEAT-023]]'s list and a view-model test asserts the strings.
- **ONNX on the desktop.** Out of scope — `src/Pegasus.Infrastructure/Vision/`
  stays server-side (ADR-0019) and the question is the [[FEAT-044]] (plan handle
  `DSK-07-18`) spike.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
