# Plan — FEAT-008: S8 Concurrency UX (conflict, lease lost, replay)

**Diff estimate: ~15 files, ~2,100 lines.**

Derived from the files document: 2 `Pegasus.Contracts` files (~220 lines — the
discriminated problem set keyed by type, four shapes with genuinely different
payloads, plus the replay marker on the success shape); 1 `/api/v1`
problem-details mapping file in `src/Pegasus.Web` (~180 — three added payload
fields, one wholly new `CaseOperationConflictException` branch, and the holder
resolution through `IDescribeCaseEditAuthorityHolder`); 1
`Pegasus.Desktop.Infrastructure` file for `ConflictRecoveryService` (~260 —
re-query, editorial-projection comparison, reapply plan, the retry rule); 3
desktop files — `ConflictRecoveryView` XAML (~240), its view model (~300, the
largest: five states and three actions), and the lease-lost read-only transition
wired into [[FEAT-005]]'s (plan handle `DSK-05-05`) editor (~90); 4 test files —
contract (~260), view-model (~340, eleven facts), UI script (~90), and the
two-user fixture support (~80); ~2 regenerated Kiota files (~160, generated); 3
documentation edits. **`src/Pegasus.Core` is untouched** — the `research` document
found every rule already Core-owned, and `CaseEditAuthority.cs` is explicitly
[[GWY-008]]'s (plan handle `DSK-03-08`) under its named conditional exception — so
no characterization move is budgeted and none is permitted.

## Approach

Make the **gateway** say precisely what happened, then let one desktop control
render it. The gateway's problem-details mapping is extended so each of the four
concurrency outcomes arrives as a distinct typed problem carrying what the desktop
cannot invent — `currentVersion` on `version-conflict`, a **named** holder on
`lease-conflict` — and so a replayed command is explicitly marked on its success
response. `ConflictRecoveryService` then re-queries, compares and produces a
reapply plan; `ConflictRecoveryView` is the single control every editor reuses.

The rejected alternative was inferring the outcome client-side from status codes
and message text. It fails on all four counts: `CaseEditLeaseConflictException`
carries no holder (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:135-142`),
`CaseOperationConflictException` carries no version (`:152-158`), a replay is a
plain 200 indistinguishable from a fresh success (`:322-334`), and the message
strings are deliberately content-safe. The desktop cannot invent a version or a
name it was not given, which is why step 3 changes the gateway rather than parsing
prose.

The second rejected alternative was reproducing the web's shape — one "lease loss"
condition covering both lease exceptions, as `CaseMutationPageModel.IsLeaseLoss`
does (`:292-294`). Core checks **expiry before ownership**
(`CaseEditAuthority.cs:39-65`), so an expired lease never reaches the ownership
comparison and has no holder; collapsing the two would show "another member of
staff holds this case" to an operator whose own edit mode merely timed out. The
gateway catalogue already keeps them apart
(`docs/desktop/03-gateway-api-and-data/README.md:167`) and so does the design
authority's Case state row (`docs/design/README.md:772`).

**One reading is load-bearing and is recorded here so it cannot drift.** The body's
"Keep mine" action is a **re-populate-after-reacquire**, not a merge or a force:
after a successful reload *and* reacquire, the operator's proposed values are
placed back into the fresh editor and the save that follows is an **ordinary** save
carrying the **new** `expectedVersion` and the **new** lease token. The body itself
says the service "**never resends the original body unchanged**" and that the
operator "reapplies deliberately". Any implementation that writes the operator's
values over the newer record violates FRD-01 `:86`,
`docs/design/README.md:722` and `screen-specs.md:193-197` at once — and is a **stop
condition**, not a design choice. See `research` § `A-05-08-2`.

## Governing docs

### Meets — the ticket's `refs`

`refs`: `docs/frd/frd-01-case-identity-and-lifecycle.md`.

| Requirement | Where | Met by |
| --- | --- | --- |
| "Other authorised staff remain read-only and **can see the holder** and recovery state." | `frd-01:84` | Steps 3 and 8 (the gateway resolves the holder through `IDescribeCaseEditAuthorityHolder`; the lease-conflict state names them) |
| "Every save, transition, assignment, association, evidence change, and other staff mutation presents both the lease token and the Case version loaded by that editor." | `frd-01:84` | Step 5 (a reapply carries the **new** version and the **new** token, never the stale pair) |
| "Core refuses a missing, expired, wrong-holder, or stale-version mutation **without overwriting newer work**." | `frd-01:86` | Steps 3, 6 and 12 (four distinct typed problems; no control that writes over the newer record; the two-user run proves nothing is lost) |
| "The rejected editor **keeps proposed values for comparison** and must reload and reacquire rather than **merge or force the save**." | `frd-01:86` | Steps 5 and 6 (proposed values live in the view model and drive the compare pane; Reload / Keep mine / Cancel, with Keep mine defined as re-populate-after-reacquire) |
| "There is no Administrator bypass, forced takeover, **collaborative merge**, bulk case mutation…" | `frd-01:86` | The Out-of-scope boundary — no such control is built, and building one is a stop condition |
| "Web and MCP Automation Actor callers use the same guard." | `frd-01:88` | Step 3 (the `/api/v1` mapping translates the **same** Core exceptions the MCP map translates; the guard is untouched) |
| "A deliberate recovery or material denial/failure is attributable permanent history; routine renewal, expiry, heartbeat, polling, and adapter mechanics remain telemetry." | `frd-01:88` | Steps 5 and 8 (the desktop writes no history; a re-query raises no business event, and the recovery action's history comes from the ordinary command it ends in) |
| "Entering edit mode acquires the case's one server-owned expiring lease." (context for the lease-lost path) | `frd-01:84` | Step 8 (re-claim re-queries first; it never silently re-acquires) |

### New ADR

The ticket carries `docs_todo: true` (confirmed in `get_doc_gates FEAT-008`, which
also shows `governing-doc` `satisfied: true` at `leave-backlog`).

> **New ADR** — ADR-0103 (gateway = evolved `Pegasus.Web`; never direct database
> access from a workstation) and ADR-0104 (online-required, bounded local cache
> only), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and to L-01 in
> `docs/desktop/README.md` § Locked decisions; if either ADR lands differently this
> plan is revised before implementation. **ADR-0104 bounds step 5: the comparison
> is against a fresh server read, never a cached copy, and proposed values are
> in-memory session state rather than replicated data.**

ADR-0100 has more than one interested party through the no-split deviation
recorded in `docs/desktop/05-implementation-and-migration/README.md` § 3; it is
authored by [[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s plan for the
ownership reconciliation.

**ADR-0011 is an existing ADR, not a new one**, and this plan does not author it —
it is cited because `DescribeCaseEditAuthorityHolder`'s docstring
(`src/Pegasus.Core/Workflow/CaseEditAuthority.cs:68-74`) names it as the reason the
Automation Actor is disclosed as itself. Step 8 meets that rule by using the Core
use case rather than reimplementing the naming.

### Programme-level authorities that bind today

`refs` carries one FRD and the ticket is otherwise governed by programme-level
authority. Each row names the step that satisfies it.

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-01 (`docs/desktop/README.md` § Locked decisions) | The gateway returns typed problems; the desktop implements the recovery experience | Steps 3 and 5–6 |
| L-02 (same) | The two-user scenario runs on the local Test/UAT stack — no Azure test environment | Step 12 |
| L-04 (same) | Routing named on the ticket | § Routing below |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | Thirteen stable `urn:pegasus:problem:<slug>` types; the concurrency four are `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`; body never carries payload dumps; `correlationId` always present | Steps 3–4 and 10 |
| `docs/desktop/03-gateway-api-and-data/README.md:166` | Conflicts → 409 carrying `currentVersion`; **`If-Match` is not the concurrency mechanism** | Steps 3–4 |
| `AGENTS.md` § Product invariants | Never overwrite newer work; duplicate business implementation is a stop condition | Step 6 and the Out-of-scope boundary |
| `docs/engineering.md` § One Core owner | One policy owner per rule — the refusal order stays in `CaseEditAuthority` | Step 3 (mapping only; Core untouched) |
| `docs/engineering.md` § Plan sizing | Diff estimate first, derived from the files document | First line |
| `docs/engineering.md` § Required evidence tiers | Tier 12 obliges the end-to-end two-user run through Core and SQL with safe replay; "registration or mock-only paths do not satisfy this tier" | Step 12 |
| `docs/frd/frd-01-case-identity-and-lifecycle.md:86` | Reload and reacquire; no merge, no force, no takeover | Steps 6 and 8 |
| `docs/design/README.md:722` | Reload/compare/reacquire are the only recovery interactions; no collaborative merge control | Step 6 |
| `docs/design/README.md:769`, `:772` | The Mutations state row in full; "lease held/expired/lost/stale" as four states | Steps 6, 8 and 9 |
| `docs/design/README.md:622` | Returning "never silently discards or replaces the operator's proposed values" | Step 5 |
| `docs/design/README.md:412-420` | `lease`, `caller` and `correlation identifier` are banned from operator copy — and the ban is a **review rule, not a CI check** | Step 6 ("edit mode"; the InfoBar's copyable field is labelled "Reference") |
| `docs/desktop/06-ui-design/screen-specs.md:193-197` | Proposed values preserved **in memory**; no forced takeover | Steps 5–6 |
| `docs/desktop/06-ui-design/screen-specs.md:417-427` | The cross-cutting state contract this pattern must render distinctly | Steps 6, 8, 9 |
| Plan 05 § 7 | Do not reproduce web mechanics (TempData, PRG); `/api/v1` gated off returns 404 and tests enable `Features:DesktopGateway` explicitly | Steps 5 and 10 |
| Proposal §10.4, §16.1, §14.8 | Detected concurrency; uncertain outcomes resolved deterministically; notifications and errors | Steps 3, 7 and 9 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (problem types);
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`;
  `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `dotnet-webapi` (dotnet/skills
  `98f84851`) → `run-tests` → `winui-code-review` at review.
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

These refine the ticket body's thirteen implementation steps — same order, same
ownership, same paths — adding the *how* the body leaves out.

1. **Orient and take the ticket.** Read `vertical-slices.md` § S8 and § Common to
   every slice, `screen-specs.md:193-197` and `:417-427`,
   `docs/frd/frd-01-case-identity-and-lifecycle.md:82-88`, and
   `docs/design/README.md:622`, `:722`, `:769-772`. Call `get_doc_gates FEAT-008`,
   then `take_ticket` with branch `task/dsk-05-08-concurrency-ux` and worktree
   `../pegasus-worktrees/dsk-05-08-concurrency-ux` from `origin/dev`.
2. **Enumerate the outcomes in `research` and record the SHA read.** There are
   **four** Core exceptions, not three:
   `CaseVersionConflictException` (`CaseWorkflowContracts.cs:125-133`, carries both
   versions), `CaseEditLeaseConflictException` (`:135-142`, **no holder**),
   `CaseEditLeaseExpiredException` (`:144-150`, a different meaning), and
   `CaseOperationConflictException` (`:152-158`, **no version**). Plus **replay**,
   which is a success (`:322-334`). Read
   `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` in full and note its class
   docstring (`:7-16`): the MCP boundary deliberately lets **no holder material
   cross**. The gateway boundary must widen that, because FRD-01 `:84` entitles
   authorised staff to see the holder — do not port `AutomationMcpErrors`
   faithfully and assume it is right.
3. **Confirm and complete the `/api/v1` mapping with [[GWY-002]] (plan handle
   `DSK-03-02`).** Read the delivered mapping and `openapi/pegasus-v1.json`, then
   check three things and add what is missing:
   - `version-conflict` carries `currentVersion` (`README.md:166` requires it).
   - `lease-conflict` carries a **named holder**, resolved through
     `IDescribeCaseEditAuthorityHolder`
     (`src/Pegasus.Core/Workflow/CaseEditAuthority.cs:83-127`) — **not**
     `ActorDisplayNames` directly, because that use case is the half carrying the
     ADR-0011 rule that the Automation Actor is disclosed as itself and never as
     staff. It needs only `PerformCasework` (`:108`), so an authorised editor may
     see it. Carry `isAutomation` alongside the name so the view model never has to
     guess. **The lease token is never on the wire in a problem.**
   - `lease-expired` carries the case version and **no holder** — Core never
     establishes one on that path (`CaseEditAuthority.cs:51-58`). Do not invent one.
   - **Add an explicit `CaseOperationConflictException` branch.** It derives from
     `InvalidOperationException` and today falls into the generic pass-through
     (`AutomationMcpErrors.cs:54-60`), whose message interpolates a raw case id.
     Emit `operation-conflict` carrying the operation key and no interpolated
     sentence.
   - **Confirm the replay marker exists on the success path.** A replay is a 200
     returning the original outcome and is otherwise indistinguishable
     (`CaseWorkflowContracts.cs:322-334`); `ICaseWorkflowQueries.HasOperationAsync`
     (`:320`) is the read behind it. If the marker is absent, step 9 is
     unimplementable — raise it on [[GWY-002]] and [[GWY-008]].
   The ticket's scope boundary permits the problem-details mapping file and nothing
   else in the gateway.
4. **Add the problem DTOs to `src/Pegasus.Contracts` as a discriminated set keyed
   by problem type**, matching the catalogue slugs at `README.md:167` exactly, so a
   view model matches on `type` and never parses prose. Four shapes with genuinely
   different payloads — do not flatten them into one bag with nullable fields, which
   would let a view model read a version that is not there. `correlationId` is
   present on all four.
5. **Implement `ConflictRecoveryService` in `src/Pegasus.Desktop.Infrastructure`.**
   Given a failed command and its typed problem, it re-queries the affected record,
   produces a field-level comparison of the operator's proposed values against the
   current server values, and returns a reapply plan the operator confirms. Three
   constraints:
   - **It never resends the original body unchanged** (the body's own words).
   - It compares the **editorial projection only** — never `version`, `id`,
     `operationKey` or the lease token. The precedent is `RetainableFormFields`
     (`CaseMutationPageModel.cs:41-91`, 43 fields): "Identifiers, versions, keys,
     tokens, and the fields that only route a command are never retained, so the
     comparison shows editorial work and never an identifier." **The selection rule
     travels; the cookie storage does not.**
   - It does **not** reproduce the TempData machinery — no 8000/2000 budget, no
     chunking, no drop/shorten flags (`CaseMutationPageModel.cs:31-39`). Proposed
     values live in the view model, which is exactly what
     `screen-specs.md:195` requires ("preserves proposed values **in memory** for
     comparison"). Discarding them instead would breach
     `docs/design/README.md:622`: returning "never silently discards or replaces
     the operator's proposed values". The mechanism is banned; the behaviour is
     required.
6. **Implement the reusable `ConflictRecoveryView` in `src/Pegasus.Desktop`.** An
   `InfoBar` from [[DUI-010]] (plan handle `DSK-06-10`) carrying the operator
   sentence; a compare pane listing **only differing fields**, both columns rendered
   in the same vocabulary (the precedent is `DisplayValue`,
   `Details.cshtml.cs:526-534` — "so the two columns compare rather than reading
   'true' beside 'Yes'"; dates go through the shared Europe/London vocabulary); and
   explicit **Reload, Keep mine, Cancel**. A `ContentDialog` only where the decision
   genuinely interrupts.
   - **Keep mine is a re-populate-after-reacquire.** It reloads, reacquires, places
     the proposed values back into the fresh editor, and the save that follows is an
     **ordinary** save carrying the new `expectedVersion` and the new lease token.
     It never writes over the newer record. Building it any other way violates
     FRD-01 `:86`, `docs/design/README.md:722` and `screen-specs.md:193-197` and is
     a **stop condition**.
   - Operator copy uses "edit mode", reusing the register the web already ships
     (`Details.cshtml.cs:178`, `:197`, `:244`, `:268`). `lease`, `caller` and
     `correlation identifier` are banned (`docs/design/README.md:412-420`) and
     **nothing in CI catches them** (`:416-420`) — the reviewer is the check. The
     InfoBar's copyable field is labelled "Reference".
7. **Define the retry rule in code and in the FRD section.** An idempotent command
   may be retried with the **same** `operationKey`; a non-idempotent command is
   never retried without the operator deciding to issue a **fresh** key; an
   uncertain outcome after a timeout is resolved by **re-querying**, never by
   resending. Write it into
   `docs/frd/frd-13-desktop-operator-experience.md` as a requirement, not a code
   comment — step 13 lands the text.
8. **Implement the lease-lost path.** The editor becomes read-only immediately, the
   current holder is named, and the operator is offered re-claim **which re-queries
   first** — never a silent re-acquire.
   - **Check first that [[GWY-008]]'s (plan handle `DSK-03-08`) two cross-actor
     lease facts (its step 12) and its acceptance criterion "A competing claim never
     replaces an unexpired lease holder, in either actor direction" have landed and
     pass.** They are restated in the ticket body's Source of truth and are the only
     evidence on the board that the exclusion upstream KANMER-005 reports is closed.
   - **If they pass**, build to that behaviour: a rejected claim leaves the existing
     holder in place, so this screen shows *the holder is unchanged and the operator
     did not take the lease*, naming an Automation Actor holder as itself through
     `CaseEditAuthorityHolder` (`CaseEditAuthority.cs:75-81`) in settled operator
     vocabulary.
   - **If either fails, stop and raise it on [[GWY-008]].** Do not model a takeover,
     do not add a client-side guard, do not claim parity around it.
   - Distinguish the two lease paths, which the web does not: `lease-expired` says
     the case is available to re-enter and names **no** holder; `lease-conflict`
     names the holder. Also render the `RequiresReacquisition` truth
     (`CaseMutationPageModel.cs:296-304`): after a **version** conflict the operator
     still holds the server-side authority — only the client's copy of the state was
     cleared — so the screen must not imply the lease was taken away.
9. **Implement the replayed path.** When the gateway reports a replay, show the
   original outcome and do **not** present it as a new success. This is the state
   the design authority calls "idempotent/replayed result"
   (`docs/design/README.md:769`).
10. **Contract tests in `tests/Pegasus.Api.ContractTests`** for each of the four
    problem types: shape, status code, and the presence of `currentVersion` or the
    named holder. Plus the replay fact on the success path. Assert the negative too:
    **no lease token appears in any problem body**. Enable `Features:DesktopGateway`
    explicitly or every route returns 404 (plan 05 § 7).
11. **View-model tests in `tests/Pegasus.Desktop.ViewModelTests`**: each problem
    type mapped to its state; the comparison producing **only** differing fields and
    containing no identifier, version, key or token row; refusal to retry a
    non-idempotent command without a fresh key; re-query on timeout; the
    lease-conflict state naming an Automation Actor holder correctly **without
    substituting the operator's own identity**; the `lease-expired` state naming no
    holder; and the `A-05-08-2` guard — **a reapply never carries the stale version
    or the old lease token**.
12. **Operator step — the scripted two-user UAT run** on the local Test/UAT stack
    (`docs/desktop/08-testing/test-uat-stack.md:22`, `Features:DesktopGateway=true`,
    LocalDB). Two operators edit the same case; the second is told about the
    conflict, compares, reapplies deliberately, and **no value is lost**. Capture the
    operator's sign-off text and date in the ticket proof, plus screenshots of each
    of the four states. This is tier 12 and
    `docs/engineering.md` § Required evidence tiers is explicit that "registration or
    mock-only paths do not satisfy this tier" — it runs through Core and SQL or it
    does not count. Keep it consistent with [[TEST-016]]'s (plan handle `DSK-08-16`)
    **scenario 11**, which is this run written as a release-critical UAT script.
13. **Documentation and close.** Add the conflict-and-recovery section to
    `docs/frd/frd-13-desktop-operator-experience.md`, including step 7's retry rule
    as a requirement. Add a **note** to
    `docs/desktop/01-inventory-and-parity/parity-matrix.md` recording the shared
    recovery pattern — **not a row**; this behaviour is cross-cutting over
    `PAR-08`–`PAR-12` and has no page model of its own. Add the `DSK` row to
    `docs/capabilities.md`. Run the simplification pass over the branch diff, record
    it under a dated `## Simplification pass` heading here, then open the PR into
    `dev`.

## Verification

Evidence tier from the ticket body: **Tier 5 — Web/API/MCP caller**, **Tier 7 —
Browser/accessibility**, **Tier 12 — Integrated workflow**. Tier 5 obliges
observable evidence that each problem type is produced by the real endpoint with
correct exception translation; tier 7 obliges the two-session editing,
error-behaviour and accessible validation-summary evidence; tier 12 obliges the
end-to-end two-user run through Core and SQL with safe replay, not a mocked
failure.

| Command | Expected | Becomes evidence |
| --- | --- | --- |
| `dotnet build ./Pegasus.slnx -c Release --no-restore` | Clean, with `TreatWarningsAsErrors=true` on the new projects | Command log |
| `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` | The four problem-type facts pass with the required payload fields; the replay fact passes; no lease token appears in any problem body | Test output (tier 5) |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` | State mapping, comparison, retry-rule, re-query, Automation-holder naming and the reapply guard pass | Test output |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayCaseCommandTests"` | **[[GWY-008]]'s two cross-actor lease facts pass.** This is a **blocker check, not this ticket's evidence** — the pattern is built to their outcome. A failure here is raised on [[GWY-008]] under its named Core scope exception, never worked around here | Test output (dependency gate) |
| `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script concurrency` | The scripted two-user conflict passes against the gateway fixture **without sleeps** | UI artefacts (tier 7) |
| Two-user UAT run on the Test/UAT stack | Named operator sign-off with date, plus screenshots of all four states; no value lost | UAT record (tier 12) |

The command log, the tier-7 artefacts and the tier-12 UAT record together become
`proof`, written by the last checklist box. **No Azure resource is touched.**

## Risks / open questions

- **The replay marker may not exist on the gateway success shape.** Nothing on the
  wire distinguishes a replayed 200 from a fresh one
  (`CaseWorkflowContracts.cs:322-334`), so step 9 depends entirely on [[GWY-002]]
  (plan handle `DSK-03-02`) and [[GWY-008]] (plan handle `DSK-03-08`) emitting it.
  *Mitigation*: step 3 checks for it before any view-model work, and raises it there
  rather than attempting client-side detection, which is impossible. **This is the
  single hardest dependency in the ticket.**
- **[[GWY-008]]'s two cross-actor lease facts may fail.** Then the exclusion
  upstream KANMER-005 reports is live and unfixed. *Mitigation*: the body's
  instruction is unambiguous and step 8 repeats it — **stop and raise it on
  [[GWY-008]]** under its named conditional Core scope exception. Not a client-side
  guard, not a modelled takeover, not a parity claim. A scope boundary naming that
  ticket, not a question this plan answers.
- **The `lease-conflict` holder may arrive unnamed.** `AutomationMcpErrors.cs:7-16`
  deliberately keeps holder material off the MCP boundary, and an implementer
  porting it faithfully will reproduce that. *Mitigation*: step 3 names
  `IDescribeCaseEditAuthorityHolder` explicitly and step 11 asserts the naming,
  including the Automation case. FRD-01 `:84` is the entitlement and
  `DescribeCaseEditAuthorityHolder` needs only `PerformCasework`, so there is no
  authorization obstacle.
- **"Keep mine" can be built as a force.** The phrase invites it. *Mitigation*: the
  Approach and step 6 define it as re-populate-after-reacquire and label any other
  reading a **stop condition**; step 11 asserts a reapply never carries the stale
  version or the old token; and `pegasus-desktop-reviewer` reviews it independently.
- **The "do not reproduce retained proposed values" trap can be read as
  "discard them".** That would be a data-loss defect. *Mitigation*: step 5 states
  both halves in one place — the cookie mechanism is banned, the in-memory
  preservation is required (`screen-specs.md:195`, `docs/design/README.md:622`).
- **A fifth exception arrives from a sibling.** `CaseTaskVersionConflictException`
  (`src/Pegasus.Core/Tasks/CaseTaskContracts.cs:21-31`) is task-scoped and reaches
  this pattern through [[FEAT-006]] (plan handle `DSK-05-06`). *Mitigation*: shape
  the version-conflict state so its subject is a parameter, not hard-coded to the
  case. The endpoints for it are FEAT-006's and [[GWY-009]]'s (plan handle
  `DSK-03-09`), not this ticket's.
- **Banned words have no automated check.** `docs/design/README.md:416-420` says so
  plainly. *Mitigation*: named in the reviewer's brief; `lease` is the one at risk
  because the domain word is everywhere in the code.
- **No open question is opened.** The ticket body does not instruct one, and the
  `research` document found nothing genuinely unsettled — the two candidates are a
  sibling's scope boundary (`A-05-08-4`) and a reading recorded as a stop condition
  (`A-05-08-2`). **No `open-questions` document is created.**

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
