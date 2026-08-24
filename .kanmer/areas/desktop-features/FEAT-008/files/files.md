# Files — FEAT-008

Surface area of `DSK-05-08 · S8 Concurrency UX (conflict, lease lost, replay)`.
Paths that do not exist at `HEAD` `bbd1c549` are marked with the ticket that
creates them; every other path was confirmed with `ls`, `wc -l` or `grep`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`); problem-type conventions by [[GWY-001]] (plan handle `DSK-03-01`))* | The concurrency problem DTOs as a **discriminated set keyed by problem type**, so a view model matches on `type` and never parses prose. Four types from the catalogue at `docs/desktop/03-gateway-api-and-data/README.md:167`: `version-conflict` (carries `currentVersion`), `lease-conflict` (carries the **named holder** and an `isAutomation` flag), `lease-expired` (carries the case version; **no holder** — Core never establishes one on this path), `operation-conflict` (carries the operation key; **no version** — the exception has none). Plus the replay marker on the **success** shape, because a replay is a 200, not a problem. |
| `src/Pegasus.Web/` — the `/api/v1` **problem-details mapping only** *(mapping by [[GWY-002]] (plan handle `DSK-03-02`))* | Add the fields the desktop cannot invent: `currentVersion` on `version-conflict`, the holder resolved through `IDescribeCaseEditAuthorityHolder` on `lease-conflict`, and an **explicit branch for `CaseOperationConflictException`**, which today falls into the generic pass-through (`src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:54-60`) and leaks a raw case id in its message. The ticket's scope boundary permits exactly this file and no other gateway change. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | `ConflictRecoveryService`: given a failed command and its typed problem, re-query the affected record, produce a field-level comparison of the operator's proposed values against the current server values, and return a reapply plan the operator confirms. **It never resends the original body unchanged**, and it compares the *editorial* projection only — never `version`, `id`, `operationKey` or the lease token. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | The reusable `ConflictRecoveryView`: an `InfoBar` from [[DUI-010]] (plan handle `DSK-06-10`) carrying the operator sentence, a compare pane listing **only differing fields**, and explicit Reload / Keep mine / Cancel. A `ContentDialog` only where the decision genuinely interrupts. Plus the lease-lost read-only transition on the editor from [[FEAT-005]] (plan handle `DSK-05-05`). |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | One fact per problem type: shape, status code, and the presence of `currentVersion` or the named holder. Plus the replay fact on the success path. `Features:DesktopGateway` enabled explicitly. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | Each problem type mapped to its state; the comparison producing only differing fields; refusal to retry a non-idempotent command without a fresh key; re-query on timeout; the lease-conflict state naming an Automation Actor holder correctly **without substituting the operator's own identity**; and the `A-05-08-2` guard — a reapply never carries the stale version or the old lease token. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]] (plan handle `DSK-08-06`))* | `ui-tests.ps1 -Script concurrency`: the scripted two-user conflict against the gateway fixture, **without sleeps**. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(skeleton by [[FND-008]] (plan handle `DSK-00-08`))* | New conflict-and-recovery section, including the retry rule from step 7 written as a requirement rather than a comment. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | A **note**, not a row — this behaviour is cross-cutting over the case rows (`PAR-08`–`PAR-12`) and has no page model of its own. |
| `docs/capabilities.md` | `DSK` row for concurrency recovery. |

## Context files

Read these before writing code. Each is here for one specific trap.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:125-158` | The **four** concurrency exceptions and their asymmetric payloads: `CaseVersionConflictException` carries both versions (`:125-133`); `CaseEditLeaseConflictException` (`:135-142`) and `CaseEditLeaseExpiredException` (`:144-150`) carry the case version and **no holder**; `CaseOperationConflictException` (`:152-158`) carries the operation key and **no version at all**. The desktop can only render what these carry, plus whatever the gateway re-reads. |
| `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:39-65` (`RequireLease`) | **The refusal order, which is business policy** (class docstring, `:5-11`). Missing token, passed expiry, unreadable hash or blank holder ⇒ `CaseEditLeaseExpiredException`; only *then* is ownership compared ⇒ `CaseEditLeaseConflictException`. So an expired lease that someone else happens to hold reports **expired**, and there is no holder to name on that path. Collapsing the two — as the web does — produces a message that is wrong, not merely vague. |
| `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:68-127` (`CaseEditAuthorityHolder`, `IDescribeCaseEditAuthorityHolder`) | **Where the holder's name comes from, and the ADR-0011 rule that governs it.** "The Automation Actor is disclosed as itself… it is never described as a member of staff" (`:68-74`). Resolution (`:103-126`): needs only `PerformCasework`; a non-`Guid` holder subject id **is** the Automation Actor; `Guid.Empty` and an unresolvable account are both `Unnamed`; otherwise the account's `UserName`. Use this, not `ActorDisplayNames` directly — this is the half that carries the automation rule. |
| `src/Pegasus.Core/Actors/ActorDisplayNames.cs:5-11`, `:50-68` | The general resolver and its constants (`UnknownStaff`, `SystemWorker`, `Automation`, `RequestLink`, `:14-17`). Its docstring states the underlying rule: business records keep only kind and subject, so "every read model that shows 'who did this' resolves through here rather than printing the subject id (a raw GUID for a staff actor)". **A GUID must never reach the conflict message.** |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:7-16` (class docstring) | **The trap that will fool a careful implementer.** "The three edit-guard refusals name which guard refused and the current case version… **no token or other holder material crosses the boundary with them.**" This is the file the gateway problem types are ported from, and it deliberately withholds the holder. The desktop boundary must widen that — FRD-01 `:84` entitles authorised staff to see the holder — so a faithful port is the *wrong* answer here. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:22-67` | The existing translation: four explicit branches (`:29-52`), then a generic `ArgumentException or InvalidOperationException or InvalidDataException` pass-through (`:54-60`). **`CaseOperationConflictException` derives from `InvalidOperationException` and lands in the pass-through**, whose message interpolates a raw case id. That is the gap step 3 closes. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:322-334` (`ILeaseCaseForEdit` docstring) | **A replay is a success, not an exception.** "An exact claim or renewal replay returns the same opaque token and expiry… **before** mutable-state, version, ownership, or expiry preconditions are evaluated. Reusing an operation key with different request material fails with `CaseOperationConflictException`. Actor authorization **always** precedes replay recovery." So step 9 needs a gateway-side marker; nothing on the wire distinguishes a replay otherwise. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:320` (`ICaseWorkflowQueries.HasOperationAsync`) | The read that can answer "was this operation already applied?" — the mechanism behind the replay marker. |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:292-304` | `IsLeaseLoss` collapses expired **and** conflict into one condition — **do not copy that**. `RequiresReacquisition` adds version conflict, with the sentence that matters most for step 8: "Clearing this page's lease state does not release the server-owned authority, so **a holder who did nothing wrong keeps it** and simply re-enters edit mode deliberately rather than saving over newer work." |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:41-91` (`RetainableFormFields`) | The 43-field allow-list and its rule: "**Identifiers, versions, keys, tokens, and the fields that only route a command are never retained**, so the comparison shows editorial work and never an identifier." The storage mechanism does not travel; **the selection rule does**. Seven of the 43 are also in `BooleanFormFields` (`:93-106`). |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:31-39`, `:110-174` | The 8000/2000 cookie budgets — a TempData artefact the desktop does **not** need — and the two shared refusal sentences (`:121`, `:139`) that conflate four outcomes into one. Those sentences are what this ticket replaces. |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:73-77` | "**There is no control that applies, merges, or forces them: the only way forward is to enter edit mode again and retype.**" Read this beside the ticket's "Keep mine" action and the `research` document's `A-05-08-2`: Keep mine re-populates the editor **after** a successful reload and reacquire, and the save that follows is ordinary, carrying the new version and the new token. Building it to resend over the newer record is a **stop condition**. |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:526-534` (`DisplayValue`) | Both columns render in the same vocabulary "so the two columns compare rather than reading 'true' beside 'Yes'". The desktop has richer types and more ways to break this — dates especially, which go through the shared Europe/London vocabulary. |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:178`, `:197`, `:244`, `:268`, `:287` | The settled operator sentences for edit mode, already shipped. `lease` is banned, "edit mode" is the word, and these five are the precedent — reuse their register rather than inventing new phrasing. |
| `src/Pegasus.Core/Tasks/CaseTaskContracts.cs:21-31` | `CaseTaskVersionConflictException(TaskId, ExpectedVersion, ActualVersion)` — a **fifth** exception, task-scoped, arriving from [[FEAT-006]] (plan handle `DSK-05-06`). The pattern must accept a version conflict whose subject is not the case; the endpoints for it are FEAT-006's, not this ticket's. |
| `docs/desktop/03-gateway-api-and-data/README.md:167` | The **thirteen stable `urn:pegasus:problem:<slug>` type URIs**, including all four concurrency slugs and — deliberately — no `replayed`. "Body never carries payload dumps; `correlationId` always present." This is the catalogue the DTOs key on. |
| `docs/desktop/03-gateway-api-and-data/README.md:166` | "Conflicts → `409` problem carrying `currentVersion`"; reads return `version` **and** a weak `ETag`; **`If-Match` is not the concurrency mechanism** (Core's semantics are per aggregate and lease-aware). A recovery flow built on `If-Match` would be wrong for this codebase. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:54` | The lease routes: "yes (key; **replay returns same token/expiry**)" and a response carrying "lease token, expiry, **holder**". The holder is already contracted on the lease response; the conflict problem needs the same treatment. |
| `docs/frd/frd-01-case-identity-and-lifecycle.md:84-88` | The requirement in its own words: staff "**can see the holder** and recovery state" (`:84`); "The rejected editor keeps proposed values for comparison and must reload and reacquire rather than **merge or force the save**. There is no Administrator bypass, forced takeover, **collaborative merge**…" (`:86`); "Web and MCP Automation Actor callers use the same guard" (`:88`). |
| `docs/design/README.md:722` | "reload/compare, and reacquire are the only recovery interactions: lease loss or a stale version disables every mutation, preserves proposed values for comparison, and never overwrites the newer Case. There is no forced Administrator takeover, bulk Case edit, direct external edit, or **collaborative merge control**." |
| `docs/design/README.md:769`, `:772` | The Mutations state row this pattern must satisfy in full, and the Case row listing "**lease held/expired/lost/stale**" — four lease states, which is why the two lease problem types stay separate. |
| `docs/design/README.md:412-420` | The banned words, including `lease`, `caller` and `correlation identifier`. And the honesty clause at `:416-420`: this is "a review rule, not an automated check — **nothing in CI enforces it today**, and claiming otherwise would be the kind of false assurance the evidence discipline above exists to prevent." |
| `docs/design/README.md:622` | "returning **never silently discards or replaces** the operator's proposed values" — the other half of the trap. The mechanism is banned; the behaviour is required. |
| `docs/desktop/06-ui-design/screen-specs.md:193-197` | The lease/conflict bullet: "preserves proposed values **in memory** for comparison and never overwrites the newer record; reload/compare/reacquire are the only recovery actions; no forced takeover". "In memory" is the sanctioned mechanism. |
| `docs/desktop/06-ui-design/screen-specs.md:417-427` | The cross-cutting state contract table, whose Mutations row (`:422`) enumerates every state this pattern renders, and the empty-state rule beneath it. |
| `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs` (2,194 lines) | The lifecycle and concurrency persistence oracle. Must stay green; not modified. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` | The shared `WebApplicationFactory<Program>`; `Features:DesktopGateway` must be enabled explicitly or every `/api/v1` route returns 404 (plan 05 § 7). |
| `docs/desktop/08-testing/test-uat-stack.md:22` | The Test/UAT gateway configuration for the two-user operator run, including `Features:DesktopGateway=true` and the LocalDB database. |

## Ripple effects

- **Generated client and OpenAPI snapshot.** Four problem schemas plus the replay
  marker on success shapes. [[GWY-005]] (plan handle `DSK-03-05`) commits the Kiota
  output with a CI no-op check; [[TEST-001]] (plan handle `DSK-08-01`) fails the
  snapshot test on an undeclared change to `openapi/pegasus-v1.json`. A problem
  schema change ripples into **every** command endpoint's documented responses.
- **[[GWY-002]] (plan handle `DSK-03-02`) owns the mapping this ticket extends.**
  Its plan-03 row acceptance is "exception → problem mapping tested for each Core
  exception" (`docs/desktop/03-gateway-api-and-data/README.md:215`). Step 3 adds
  fields there; anything larger is raised on that ticket.
- **[[GWY-008]] (plan handle `DSK-03-08`) is the blocking dependency and the owner
  of upstream KANMER-005.** Its step 12's two cross-actor lease facts and its
  acceptance criterion are the **only** evidence on the board that the exclusion is
  closed. If either fails, this ticket **stops and raises it there** —
  `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` and
  `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs` are GWY-008's
  under its named conditional Core exception, and this ticket reads them and changes
  neither.
- **[[FEAT-005]] (plan handle `DSK-05-05`) produces the states this renders.** Its
  `CaseEditSession` raises `LeaseLost` on a failed renew; this ticket turns that
  into the read-only transition and the named-holder surface. The two must agree on
  when the client-held token is cleared — including after a **version** conflict,
  per `RequiresReacquisition`.
- **Every later editing slice consumes this pattern.** [[FEAT-006]] routes nineteen
  command refusals through it (including the task-scoped fifth exception);
  [[FEAT-017]] and [[FEAT-018]] follow. A bespoke conflict message anywhere else is
  a second pattern and a review failure.
- **[[DUI-010]] (plan handle `DSK-06-10`) supplies the InfoBar.** Its "copyable
  Reference" is the correlation id, which `README.md:167` says is always present on
  a problem — and `correlation identifier` is a banned operator word, so the label
  must be "Reference".
- **`FEAT-008` blocks `FEAT-022`, `FEAT-024`, `FEAT-025` and [[TEST-016]] (plan
  handle `DSK-08-16`).** TEST-016's **scenario 11** is this ticket's two-user run
  written as a UAT script — "the expected result is the `409` problem surfaced as an
  operator sentence with a reload or compare path, and nothing silently
  overwritten". Keep the two consistent; scenario 11 is release critical path.
- **[[FEAT-024]] (plan handle `DSK-05-24`) retires `CaseMutationPageModel` for
  desktop paths** and asserts the desktop has no TempData/PRG equivalent. This
  ticket must not create one.
- **Existing web tests must stay green.** Nothing here touches
  `CaseMutationPageModel.cs` or any Razor page, so `CaseWorkflowPersistenceTests`,
  `CaseDetailsWebTests` and the MCP tests must pass unchanged.
- **Documentation link check.** `scripts/Test-DocumentationLinks.ps1` runs over
  repository documentation, so a broken relative link in the new FRD-13 section
  fails CI.

## Out of scope

Recorded so the reviewer sees each was a decision, not an oversight.

- **`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` and every Razor page.**
  The ticket's scope boundary forbids modifying them. They stay live until their
  parity rows reach cut-over; the cut is [[FEAT-026]] (plan handle `DSK-05-26`).
- **`src/Pegasus.Core/Workflow/CaseEditAuthority.cs` and
  `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs`.** They are
  **[[GWY-008]]'s** under its named conditional exception. This ticket reads them
  and changes neither.
- **Upstream KANMER-005 is not asserted here.** [[GWY-008]] owns it; its step 12's
  two cross-actor facts and its acceptance criterion are the evidence, and this
  ticket renders their outcome. **Upstream KANMER-005 has no fork ticket at all**,
  so it must never be written as a board wiki-link (`HZN-001` group document
  `board-conventions.md` § "Upstream ids versus board ids").
- **No modelled takeover, and no client-side lease guard.** "There is no takeover,
  force, or bypass" (`CaseEditAuthority.cs:5-11`); no forced Administrator takeover
  (`docs/design/README.md:722`). If [[GWY-008]]'s facts fail, the correct action is
  to block — never a workaround and never a parity claim over an unproved exclusion.
- **No collaborative merge control of any kind.** Forbidden by FRD-01 `:86` and
  `docs/design/README.md:722`. "Keep mine" is a re-populate-after-reacquire action
  whose save is ordinary; anything that writes over the newer record is a stop
  condition.
- **No cookie TempData equivalent** — no 8000/2000 budget, no chunking, no
  drop/shorten flags. Proposed values live in the view model
  (`screen-specs.md:195`). [[FEAT-024]] asserts this.
- **No blind retry of a non-idempotent command**, and no automatic retry after a
  timeout. An uncertain outcome is resolved by re-query.
- **The lease session itself** — claim, renew, release, the timer and the dirty
  state — is [[FEAT-005]]'s (plan handle `DSK-05-05`).
- **The nineteen command endpoints and the task-scoped conflict** are
  [[FEAT-006]]'s (plan handle `DSK-05-06`) and [[GWY-009]]'s (plan handle
  `DSK-03-09`).
- **No new `PAR` row.** Concurrency is cross-cutting over `PAR-08`–`PAR-12`; the
  matrix gets a note. Inventing a row would claim a page-model oracle that does not
  exist.
- **No Azure write.** Enabling `Features:DesktopGateway` in production is
  [[PLAT-024]] (plan handle `DSK-11-06`).
