# Files — FEAT-011

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today — `ls src` returns only `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`; `ls tests` only
`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`)* | Triage DTOs: list item, detail (including the origin receipt's evidence images, the reply evidence and the response candidates), finding payloads, and **one request record per command**. The dispatcher's eleven-parameter signature partitions cleanly — five commands need only the shared mutation fields — so one union record with eleven nullable fields would be the wrong shape. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]], plan handle `DSK-02-05`)* | `TriageListViewModel` (on the [[DUI-007]] data-table pattern, state as a dropdown filter, newest first) and `TriageDetailViewModel` with one command object per enumerated action — no dispatcher string anywhere — plus the detail XAML to `docs/desktop/06-ui-design/screen-specs.md:287-296` and its `Triage.Header.<Field>` / `Triage.Actions.<Action>` / new `Triage.Evidence.*` AutomationIds. |
| `src/Pegasus.Core/Triage/` | **Only** for a precondition that lives in the page model and is business logic, moved in with a characterization test written first. A second implementation is a stop condition (`docs/engineering.md` § One Core owner). |
| `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs` | Re-pointed at a moved rule and **nothing else** — the Guardrails forbid modifying the Razor triage pages beyond that. |
| `src/Pegasus.Web/` — the `/api/v1` triage group only | Only where [[GWY-013]] (plan handle `DSK-03-13`) left a gap this slice must close to consume its own contract. Behind `Features:DesktopGateway` ([[GWY-002]], plan handle `DSK-03-02`). |
| `tests/Pegasus.Core.Tests/Triage/` | The action-matrix characterization: which action is legal from which `TriageState`, which require a reason, which require a finding payload. Written **before** any rule moves. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`)* | The seven-case matrix from [[TEST-002]] (plan handle `DSK-08-02`) applied to **every** enumerated action. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[TEST-004]], plan handle `DSK-08-04`)* | `CanExecute` per state, reason-required commands, finding and supersede payload validation, response link/unlink candidate selection, and the evidence section being **absent** — not errored — for an actor without `PerformCasework`. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Rows `PAR-23` and `PAR-24` — both `not inventoried` today, both recording test evidence as "to locate". `PAR-24`'s "dispatches 13 commands" text is settled by this ticket's open question. |
| `docs/desktop/06-ui-design/screen-specs.md` § `Triage detail` (`:287-296`) | Add an evidence-images section and a `Triage.Evidence.*` AutomationId. The section list today names evidence, reply evidence, findings, responses and the linked case — and **no gallery** (upstream INTK-034). |
| `docs/frd/frd-03-triage.md` | **Only** if the carried-forward upstream INTK-034 question is answered yes: record the Triage evidence surface as required behaviour. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by [[DUI-013]], plan handle `DSK-06-13`)* | Triage section, citing FRD-03. The file does not exist today. |
| `docs/capabilities.md` | `DSK` rows for the triage queue and actions. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:85-97` | `OnPostActionAsync`'s eleven-parameter signature — `actionName`, `expectedVersion`, `operationKey`, `reason`, `roadworthiness`, `assessment`, `supersedesFindingId`, `responseCandidate`, `sentEvidenceId`, `caseId`. This is the whole parameter surface the twelve routes must cover between them; no route needs all of it. |
| `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:107-112` | `TriageMutationRequest(id, expectedVersion, actor, operationKey, reason)` is built **once** and reused unchanged by `unassign`, `await_information`, `complete`, `cancel` and `reopen`. Five of the twelve therefore need no bespoke request record. |
| `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:114-210` | The twelve `case` labels in source order, and the `default` at `:208-210` that throws "The requested Triage action is not supported." Note `link_response` parses a `responseCandidate` string into a poll-outcome id **and** a sent-evidence id (`:156-170`) — the desktop must send the pair, not the string. |
| `src/Pegasus.Web/Pages/Triage/Details.cshtml:56` | The link out to `/Intake/Details/{Origin.ReceiptId}` — today the engineer's **only** route to the client's damage photographs. That single line is the defect upstream INTK-034 records. |
| `src/Pegasus.Web/Mcp/TriageMcpTools.cs:37,66,81,98-143` | Thirteen tool declarations: three reads and **ten** mutations. The two dispatcher labels with no MCP tool are `assign` and `unassign` — which is the measured explanation for the ten-versus-twelve gap. |
| `src/Pegasus.Core/Triage/TriageContracts.cs:288-294` | `ITriageQueries` has `ListAsync` and `GetAsync` **only**. `GetByOriginReceiptAsync` does not exist in the fork; it arrives with upstream INTK-033 (board [[INTK-007]]) and its resolution is [[GWY-013]] step 8's. |
| `src/Pegasus.Core/Triage/TriageContracts.cs:79-84,138` | `CreateTriageFromIntakeRequest` takes a **normalized** registration; `ICreateTriageFromIntake` is the interface. Nothing in this slice calls either — creation is the Worker's path. |
| `src/Pegasus.Core/Triage/TriageLifecycle.cs` (561 lines) | The lifecycle the action matrix must be characterized against. `TriageLifecycleRules.ValidateCreate` is the one judge of a registration; the state preconditions for the twelve actions live in or beside this file. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:10` | `StaffAccessRight.PerformCasework`. A reader without it sees the evidence section **absent**, not an error — `screen-specs.md`'s "sections only when populated" rule. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` | The twelve named routes with the parenthetical "verify the full set", and the note that `assign` becomes Engineer selection per upstream INTK-019. It also pins `TriageVersionConflictException` → 409. |
| `docs/desktop/05-implementation-and-migration/README.md:119-123` | The plan's own statement of the count discrepancy and its instruction that the remaining commands are "enumerated during S11 research, not [assumed]". |
| `docs/desktop/06-ui-design/screen-specs.md:287-296` | The Triage detail spec: one container, identity, sections only when populated, named actions in the action bar/overflow each through `ReasonDialog` where a reason is required, **never a generic Close**, and the two AutomationId families — with **no gallery** listed. |
| `docs/engineering.md` § Capability organization | `Triage` is a reserved business word and keeps its settled meaning in every operator string. |
| `tests/Pegasus.IntegrationTests/QdosTriageReplayIntegrationTests.cs` | The existing replay evidence — read it before writing the replay case of the seven-case matrix, so the desktop's idempotency facts match the shape already proven. |
| `docs/desktop/08-testing/test-uat-stack.md` | The local Test/UAT stack the operator UAT script (step 13) runs on. L-02 forbids an Azure test environment. |

## Ripple effects

- **OpenAPI and the generated client.** Triage DTOs in `src/Pegasus.Contracts`
  change `openapi/pegasus-v1.json` and the generated client that [[GWY-013]] and
  the contract tests bind to.
- **`tests/Pegasus.IntegrationTests`** — `TriageQueuesWebTests.cs`,
  `QdosTriageIntegrationTests.cs`, `QdosTriageCaseAssociationIntegrationTests.cs`
  and `QdosTriageReplayIntegrationTests.cs` must stay green if a precondition
  moves into `src/Pegasus.Core/Triage/` and `Details.cshtml.cs` is re-pointed.
- **[[FEAT-009]] (plan handle `DSK-05-09`)** owns the streaming download service
  the triage source download reuses. A copy here would be a second implementation.
- **[[FEAT-016]] (plan handle `DSK-05-16`)** owns the one gallery and viewer and
  names Triage in its adopter step. Whatever this slice ships for the evidence
  section is replaced or bound by that ticket; a second renderer here is a stop
  condition.
- **`docs/desktop/06-ui-design/screen-specs.md` § `Triage detail` is this
  ticket's block to edit.** [[FEAT-016]] owns the § `§13.7 Documents and evidence`
  viewer-contract block and [[GWY-007]] (plan handle `DSK-03-07`) owns the
  case-workspace `:230-231` line — one block, one owner.
- **`docs/frd/frd-03-triage.md` changes only if the operator answers yes.**
  Recording a surface the operator did not ask for would put unrequired behaviour
  into an FRD.
- **`docs/frd/frd-13-desktop-operator-experience.md` does not exist yet** — it is
  authored by [[DUI-013]]; contribute the triage section there if it has not
  landed.

## Out of scope

- **The Razor triage pages beyond re-pointing a moved rule.** They stay
  deployable until `PAR-23` and `PAR-24` reach `UAT passed`.
- **A second image gallery, viewer or thumbnail cache.** [[FEAT-016]] owns that
  control; this slice binds to it or defines the seam and leaves the rendering to
  it.
- **A second retention of the origin receipt's images under the Triage.** The
  photographs are read from the origin receipt's retained assets over the existing
  byte endpoints; a second custody record duplicates custody and is a stop
  condition.
- **A second streaming download implementation.** [[FEAT-009]]'s service is
  reused.
- **`ITriageQueries.GetByOriginReceiptAsync`.** It does not exist in the fork
  (`TriageContracts.cs:288-294`); it arrives with upstream INTK-033 (board
  [[INTK-007]]) and its resolution is [[GWY-013]] step 8's, after [[FND-023]]
  (plan handle `DSK-01-10`)'s sync.
- **Triage creation from intake.** `CreateTriageIfQualifyingAsync`
  (`src/Pegasus.Core/Intake/DurableIntake.cs:893`) is the Worker's path and is
  untouched.
- **Any Azure write.**
