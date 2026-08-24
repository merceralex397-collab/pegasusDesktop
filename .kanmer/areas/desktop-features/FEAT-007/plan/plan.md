# Plan — FEAT-007: S7 Parties and reference data (organizations, principals)

**Diff estimate: ~16 files, ~1,900 lines.**

Derived from the files document: 3 `Pegasus.Contracts` DTO files (~260 lines —
five request and four response shapes, two of them carrying a `version`);
1 `/api/v1` administration-endpoint file (~240, six routes plus the reference
read and the two 409 translations) — **consumed from [[GWY-015]] (plan handle
`DSK-03-15`) where it already exists, authored here only for a gap it leaves**;
1 `Pegasus.Desktop.Infrastructure` client file (~140, seven calls plus the two
named page-size constants); 3 desktop files — `OrganizationsViewModel` (~200),
`OrganizationDetailViewModel` (~330, the largest single file: organization form,
principal rows, two commands), the reference-data view (~110); 4 test files —
contract (~300), view-model (~280), UI script (~80), and the two negative facts
folded into the first two; ~2 regenerated Kiota files (~150, generated); 4
documentation edits. **`src/Pegasus.Core` is expected to be untouched** — the
`research` document found every rule already Core-owned and no principal update
path to move — so no characterization move is budgeted. Step 3a is the one
conditional exception, and it is budgeted at zero.

## Approach

Build **one** destination and put the principal work inside it. `OrganizationsViewModel`
is the only Administration entry point for parties; `OrganizationDetailViewModel`
hosts the organization form, that organization's principal rows, and the create
and replace commands. The rejected alternative was the obvious one — convert the
five Razor page models one-for-one into two native destinations, Organizations and
Principals — and it is rejected on evidence rather than taste:
`src/Pegasus.Web/Pages/Administration/Principals/Index.cshtml.cs` is 31 lines that
call `IListOrganizations` and inject no principal service at all, so a Principals
destination would be a second rendering of the organization list, which is exactly
the fragmentation upstream PLAT-028's 2026-08-21 operator decision removed.
Building it is a stop condition, not a style preference.

The second rejected alternative was validating only server-side and rendering the
400s. `OrganizationAdministrationPolicy`'s four bounds
(`src/Pegasus.Core/Cases/OrganizationAdministration.cs:270-275`) are constants in
`Pegasus.Core`, which the reuse-map boundary note
(`docs/desktop/05-implementation-and-migration/reuse-map.md:44-50`) expressly
permits the desktop to reference, so running them locally costs one project
reference and gives the operator an answer before a round trip — and the store
re-checks them inside the transaction anyway.

The third decision is a *negative* one and it shapes the tests: a principal's
immutability is not implemented, it is **structural**. Core exposes
`ICreatePrincipal` and `IReplacePrincipal` and nothing else
(`src/Pegasus.Core/Cases/CaseContracts.cs:345-366`) — no update, no delete. The
correct evidence is therefore the absence of a route and the absence of a command,
not a client-side guard, which would be a second implementation of a rule Core
enforces by omission.

## Governing docs

### Meets — the ticket's `refs`

`refs`: `docs/frd/frd-04-parties-accounts-and-access.md`.

| Requirement | Where | Met by |
| --- | --- | --- |
| "`Administrator` … staff account creation/disable/access review/role assignment; **principals and successor cutover**; workflow configuration…" | `frd-04:19` | Steps 5–7 (the screens exist only for an administrator, and successor cutover is the explicit replace command) |
| "`Engineer` … **Excluded**: accounts, roles, access review, **principals, successor cutover**, workflow configuration…" | `frd-04:20` | Steps 7 and 8 (the rail entry is absent for a non-administrator, and the gateway still returns 403 to a forged call) |
| "`User` … **Excluded**: … principals, successor cutover …" | `frd-04:21` | Steps 7 and 8 (same two halves) |
| "Authorization is enforced in Core use cases **and at every caller boundary**. It fails closed without revealing case or source data." | `frd-04:25` | Step 8 (403 facts for a casework-only staff session *and* for the Automation Actor; the problem body names no organization or principal content) |
| "**Immutable principal/reference** … rules apply **regardless of administrative privilege**." | `frd-04:25` | Steps 6 and 8 (no edit command exists on a principal row; the contract test asserts no `PUT …/principals/{id}` route exists and that a replace cannot reuse the predecessor's code) |
| "Permanent business history records every business mutation … with … trusted staff or automated actor, caller, time, policy/version, structured before/after values, outcome, and reason where applicable." | `frd-04:29` | Step 3 (the endpoints call the same Core use cases the Razor handlers call, so the existing history write is inherited unchanged — this ticket adds no second history path) |
| "A history write is part of the mutable business transaction; a failed write cannot leave an unrecorded successful mutation." | `frd-04:29` | Step 3 (the desktop never writes history; it calls the command and the store's transaction covers both) |
| "Routine views, searches, refreshes … remain content-safe telemetry." | `frd-04:31` | Step 5 (a list refresh raises no business event) |

### New ADR

The ticket carries `docs_todo: true` (confirmed in `get_doc_gates FEAT-007`, which
also shows `governing-doc` `satisfied: true` at `leave-backlog`).

> **New ADR** — ADR-0103 (gateway = evolved `Pegasus.Web`; never direct database
> access from a workstation), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and to L-01 in
> `docs/desktop/README.md` § Locked decisions; if the ADR lands differently this
> plan is revised before implementation.

ADR-0100 has more than one interested party through the no-split deviation
recorded in `docs/desktop/05-implementation-and-migration/README.md` § 3; it is
authored by [[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s plan for the
ownership reconciliation.

**No ADR is claimed for the deferred credential controls.** ADR-0107 keeps
provider credentials behind the gateway, and that is where the deferred half would
land — but it is not built here, so this plan meets nothing of it.

### Programme-level authorities that bind today

`refs` carries one FRD and the ticket is otherwise governed by programme-level
authority. Each row names the step that satisfies it.

| Authority | Requirement | Met by |
| --- | --- | --- |
| Upstream PLAT-028, operator decision 2026-08-21 | Archive upstream PLAT-024 and consolidate Organizations and Principals into one destination | Step 5, and the Out-of-scope boundary — a second destination is a stop condition |
| Upstream PLAT-028, second operator decision | Provider-key controls belong on the Principal *within* the consolidated Organization detail | The Out-of-scope boundary — deferred, with its destination recorded so it is not re-litigated |
| L-01 (`docs/desktop/README.md` § Locked decisions) | The gateway owns authorization and audit | Steps 3 and 8 |
| L-02 (same) | Verification on the local Test/UAT stack; no Azure test environment | Steps 8–10 |
| L-04 (same) | Routing named on the ticket | § Routing below |
| `AGENTS.md` § Product invariants | Principal and reference immutable after allocation; neither reference reused; duplicate business implementation is a stop condition | Steps 6 and 8 (negative facts), and the Approach's third decision |
| `docs/engineering.md` § One Core owner | One policy owner per rule | Step 3a (conditional; the expected count is zero) |
| `docs/engineering.md` § Plan sizing | Diff estimate first, derived from the files document | First line |
| `docs/engineering.md` § Required evidence tiers | Tier 5 obliges route-level evidence of the authorization boundary, idempotency and exception translation; tier 7 obliges keyboard, focus, semantic-label and validation-summary evidence from a real run | Steps 8 and 10 |
| `docs/design/README.md:412-420` | Banned operator words; a review rule, **not** a CI check | Steps 5–6 (`artifact` and `aggregate` are the two within reach on this screen) |
| `docs/design/README.md:424-440` | A field is a label and a control; no how-it-works copy; consequence copy only from the closed necessary-copy list | Step 6 |
| `docs/design/README.md:459` | Administration permits no credential/cloud/release operation, no bulk predecessor import, no permanent deletion | The Out-of-scope boundary |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:129-130` | Administration commands at `ManageOrganizationsAndPrincipals`, idempotent by key | Steps 3 and 8 |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:132` | `GET /reference/providers` at **`PerformCasework`**, ETag with a short cache | Steps 3 and 9 |
| `docs/desktop/05-implementation-and-migration/reuse-map.md:44-50` | The desktop may reference `Pegasus.Core` for deterministic validation, never `Pegasus.Infrastructure` or an SDK | Step 5 |
| Plan 05 § 7 | `/api/v1` gated off returns 404; tests enable `Features:DesktopGateway` explicitly | Step 8 |
| Proposal §13.6 | Party and reference-data maintenance according to permissions | The whole ticket |
| Proposal §13.11 | Scope creep is a stop condition | The Out-of-scope boundary (upstream `TICK-034`, credential controls, organization addresses) |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`,
  `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) →
  `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) →
  `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `run-tests`
  (dotnet/skills `98f84851`) → `winui-code-review` at review.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
  (call `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary).
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's eleven implementation steps — same order, same
ownership, same paths — adding the *how* the body leaves out. Step 3a is an
addition the `research` document forced, and it is conditional and budgeted at
zero.

1. **Orient and take the ticket.** Read the plan row
   (`docs/desktop/05-implementation-and-migration/README.md:216`),
   `vertical-slices.md` § S7, `screen-specs.md:332-341` and
   `docs/frd/frd-04-parties-accounts-and-access.md:19-25` (the staff role access
   matrix). Call `get_doc_gates FEAT-007`, then `take_ticket` with branch
   `task/dsk-05-07-parties` and worktree
   `../pegasus-worktrees/dsk-05-07-parties` from `origin/dev`.
2. **Re-read the five page models at the current SHA and record it.** They are
   `Organizations/Index.cshtml.cs` (126), `Organizations/Edit.cshtml.cs` (146),
   `Principals/Index.cshtml.cs` (31), `Principals/Create.cshtml.cs` (137),
   `Principals/Replace.cshtml.cs` (199). The `research` document was written at
   `bbd1c549`; upstream keeps fixing the web app, so record the SHA actually read
   (plan 05 § 7, "Parity drift"). Confirm the four bounds are still 300 / 20 /
   **100** / 500 at `OrganizationAdministration.cs:270-275`, that
   `PlanPrincipalReplacement` (`:341-388`) still preserves `SequenceLineageId` and
   still calls `RequireUniquePrincipalCode`, and that no `IUpdatePrincipal` has
   appeared.
3. **Confirm the delivered endpoints against [[GWY-015]] (plan handle
   `DSK-03-15`).** Read the route group and the committed
   `openapi/pegasus-v1.json` snapshot, not the plan text: `GET/POST
   /api/v1/admin/organizations`, `GET/PUT /api/v1/admin/organizations/{id}`,
   `GET/POST /api/v1/admin/principals`, `POST
   /api/v1/admin/principals/{id}/replace`, and `GET /api/v1/reference/providers`.
   Check four things specifically — every mutation gated on
   `ManageOrganizationsAndPrincipals`; every mutation carrying `operationKey`
   bounded at **100**; replace additionally carrying `reason`; and the reference
   read gated on **`PerformCasework`**, not the administrator right (endpoint map
   `:132` — it is a dropdown source shared with Create/Details/Triage, and moving
   it to the administrator right breaks those callers). The principal routes are
   consumed **from Organization detail**; the consolidation is a destination
   decision and changes no endpoint. Any gap is raised on [[GWY-015]], not
   patched here.
   - **3a (conditional).** `IProviderReferenceCatalog` has exactly one method,
     `FindCandidatesByDomainSuffixAsync`
     (`src/Pegasus.Core/ReferenceData/ReferenceDataContracts.cs:37-42`) — it
     cannot list. If [[GWY-015]] satisfies `GET /reference/providers` with a
     gateway-side projection, nothing is owed. If it needs a new Core port, that
     port is a Core change and gets a characterization test **before** the slice
     consumes it (`docs/desktop/05-implementation-and-migration/README.md` § 7).
     Expected outcome: nothing owed.
4. **Add the DTOs to `src/Pegasus.Contracts`.** Five requests, four responses.
   Keep the organization `version` and the principal `version` as **separate**
   fields on the wire — they are different versions and flattening them loses
   principal-level concurrency. Do not introduce a shared administration request
   bag; each command names the fields it needs. Mirror the four bounds as
   validation attributes so the same numbers are enforced on both sides.
5. **Build the consolidated experience in `src/Pegasus.Desktop`.**
   `OrganizationsViewModel` is the **single** Administration entry point for
   parties, on the [[DUI-007]] (plan handle `DSK-06-07`) data-table pattern, page
   size **25**. `OrganizationDetailViewModel` owns the organization form (name,
   the two role checkboxes, reason on update), that organization's principal rows,
   and the two principal commands, on the [[DUI-008]] (plan handle `DSK-06-08`)
   form pattern. There is **no** Principals view model, rail entry, card or route
   — building one is a stop condition. Surface
   `ActivePrincipalsRequireWorkProvider` when the operator clears Work Provider on
   an organization that has an active principal; a checkbox pair that swallows
   that refusal looks broken. The organization pickers inside the two principal
   commands use page size **100** and reproduce the top-up from
   `Principals/Create.cshtml.cs:90-119` — if the selected organization is not in
   the returned page, fetch it and prepend it. Render reference data read-only,
   with no edit affordance of any kind. Regenerate the `operationKey` when the
   operator edits the form after a refusal, and reuse it when resending an
   unchanged request — the four page models all do this
   (`Index.cshtml.cs:87`, `Edit.cshtml.cs:88`, `Create.cshtml.cs:87`,
   `Replace.cshtml.cs:114`).
6. **Implement principal replacement as its own explicit command inside
   Organization detail**, through the [[DUI-009]] (plan handle `DSK-06-09`)
   `ReasonDialog`. Never an inline field edit; a principal row carries **no** edit
   command at all, because Core exposes no update path
   (`src/Pegasus.Core/Cases/CaseContracts.cs:345-366`). Default
   `SuccessorOrganizationId` to the predecessor's organization, matching
   `Replace.cshtml.cs:163` — losing that default turns the common case into a
   search. The consequence sentence comes from the closed necessary-copy list at
   `docs/design/README.md:400-409`; do not write a fresh one. The "never reuses a
   reference" rule is **not** implemented client-side — it needs
   `codeAlreadyExists`, which only the store can answer — so it is asserted in the
   contract test at step 8.
7. **Apply role awareness from [[FND-046]] (plan handle `DSK-04-10`).** The
   Administration rail entry and both screens are absent for a non-administrator,
   consuming that ticket's mechanism and adding no second visibility rule. Hiding
   is usability only; step 8 proves the gateway still refuses a forged call.
8. **Contract tests in `tests/Pegasus.Api.ContractTests`.** Per endpoint: 200 for
   an administrator; **403 `not-authorized` for a `PerformCasework`-only staff
   session and separately for the Automation Actor** (`StaffAuthorization.cs:45-53`
   denies it by construction, so this is a real second case, not a duplicate); 401
   without a token; **409 on a stale version — Edit and Replace only**, since
   neither create carries a version; and replay of the same `operationKey`
   returning the same result. Then the two negative facts: **no `PUT
   /api/v1/admin/principals/{id}` route exists**, and a replace that reuses the
   predecessor's code is refused (`DuplicatePrincipalCode`). Mirror the shape of
   `OrganizationAdministrationWebTests.DirectOrganizationAndPrincipalRoutesDenyNonAdministratorSession`
   (`:141`). Enable `Features:DesktopGateway` explicitly or every route returns
   404. Record the two exemptions from the [[TEST-002]] (plan handle `DSK-08-02`)
   seven-case matrix rather than silently skipping them: the creates have no
   stale-version case, and the reference read's 403 case is a different actor.
9. **View-model tests in `tests/Pegasus.Desktop.ViewModelTests`.** List paging;
   create validation against all four bounds, including the **100**-character
   operation key; edit dirty state; replace refusing an empty reason; the
   successor organization defaulting to the predecessor's; the operation-key
   regeneration rule; and the two structural facts — **the principal row exposes
   no edit command**, and **no navigation target named Principals exists**. The
   second is what keeps the consolidation from quietly regressing later.
10. **`winapp ui` script and accessibility scan.** Add
    `tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script parties` covering
    create-organization and replace-principal **by keyboard, both reached from
    Organization detail**, and run the `axe-windows` scan over the list and the
    detail. Attach both artefacts to the ticket proof.
11. **Documentation and close.** Update `parity-matrix.md` `PAR-40` (`:85`) and
    `PAR-41` (`:86`) — and note that PAR-41's native-screen cell moves from
    "Principals admin" to Organization detail, which is a consolidation record, not
    a status change. Rewrite `screen-specs.md:332-341`: record the consolidation in
    place of the "Organizations list/edit … and Principals create/replace"
    pairing, re-host the `Admin.Principals.Create` and `Admin.Principals.Replace`
    AutomationIds on Organization detail, add the decision the carry-over line at
    `:340-341` omits, and correct "addresses, contacts" — `Organization`
    (`src/Pegasus.Core/Cases/CaseContracts.cs:13-17`) carries neither. Add the
    parties and reference-data section to
    `docs/frd/frd-13-desktop-operator-experience.md` and `DSK` rows to
    `docs/capabilities.md`. Run the simplification pass over the branch diff,
    record it under a dated `## Simplification pass` heading here, then open the PR
    into `dev`.

## Verification

Evidence tier from the ticket body: **Tier 5 — Web/API/MCP caller** and **Tier 7 —
Browser/accessibility**. Tier 5 obliges route-level evidence that each
administration endpoint reaches Core with the right authorization boundary,
idempotency and exception translation; tier 7 obliges keyboard, focus,
semantic-label and validation-summary evidence from a real run of both screens.

| Command | Expected | Becomes evidence |
| --- | --- | --- |
| `dotnet build ./Pegasus.slnx -c Release --no-restore` | Clean, with `TreatWarningsAsErrors=true` on the new projects | Command log |
| `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` | 200/401/403/409/replay pass per endpoint; the two negative facts pass; the Automation-Actor 403 is a distinct passing case | Test output (tier 5) |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` | List, create, edit, replace, key-regeneration and the two structural facts pass | Test output |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~OrganizationAdministration"` | `OrganizationAdministrationWebTests` (2 facts) and `OrganizationAdministrationPersistenceTests` pass **unchanged** — nothing here touches the Razor pages | Test output (regression guard) |
| `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script parties` | Keyboard create and replace pass **from Organization detail**; `axe-windows` reports zero critical issues | UI artefacts + axe report (tier 7) |

The tier-7 artefacts and the command log together become `proof`, written by the
last checklist box. Both screens run against the local Test/UAT stack
(`docs/desktop/08-testing/test-uat-stack.md:22`) with
`Features:DesktopGateway=true`; **no Azure resource is touched.**

## Risks / open questions

- **[[GWY-015]] (plan handle `DSK-03-15`) sits a phase later than this ticket.**
  It is `HZN-009` / `phase-8`; `FEAT-007` is `HZN-005` / `phase-4`. The plan-05
  dependency row (`README.md:216`) records "DSK-03 administration group" as a
  dependency, so the dependency is agreed and only the horizons disagree.
  *Mitigation*: step 3 reads the delivered routes before any binding is written;
  if they are absent, this ticket blocks on [[GWY-015]] and the sequencing is
  raised with whoever schedules the board. It is a scope boundary naming that
  ticket, not a question this plan can answer.
- **A route shape may differ from the ticket body's list.** *Mitigation*: step 3
  reads `openapi/pegasus-v1.json`, not the plan text, and any gap is raised on
  [[GWY-015]]. The scope boundary permits this ticket to touch only the
  organizations and principals routes it consumes.
- **`GET /reference/providers` may need a new Core port.** The catalogue answers
  one question by domain suffix and cannot list. *Mitigation*: step 3a is
  conditional and, if it fires, requires a characterization test before the slice
  consumes the new port. Budgeted at zero because the endpoint-map row
  (`:132`) describes a dropdown source, which a gateway-side projection satisfies.
- **The reference-data section has no oracle.** No web page renders provider
  reference data today and no `PAR` row covers it, so its acceptance is "renders
  read-only, no edit affordance" and nothing more. *Mitigation*: the plan promises
  no parity comparison for it, and the documentation step adds no parity row that
  would imply one.
- **The consolidation can regress silently.** A later ticket adding an
  Administration card could reintroduce a Principals destination without noticing
  the decision. *Mitigation*: step 9's structural fact — no navigation target named
  Principals exists — fails the build if it does, and the Out-of-scope entry
  records it as a stop condition.
- **Operation-key length.** 100 here against 200 for intake commands. *Mitigation*:
  step 4 mirrors the bound as a validation attribute from the Core constant rather
  than a literal, and step 9 asserts it.
- **Deferred provider-credential controls.** Owned by upstream `TICK-058` and
  upstream `TICK-061` under `HZN-002`, with their destination already decided by
  upstream PLAT-028's second operator decision. A **scope boundary naming those
  tickets, not an open question** — and the ticket's Guardrails forbid building a
  partial credential surface in the meantime.
- **No open question is opened.** The ticket body does not instruct one, and the
  `research` document found nothing genuinely unsettled. **No `open-questions`
  document is created.**

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
