# Files — FEAT-020: S20 Operations and integration health

Surveyed at `bbd1c549` (2026-08-24). Paths marked *(created by …)* do not exist yet.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/OperationsViewModel` *(owned by [[FEAT-030]] (plan handle `DSK-07-04`))* | This slice **adds members** — the retry and revoke commands, the two lists and the health panel — and changes no existing member. If [[FEAT-030]] has not landed, create the type with exactly the members its step 3 pins (`ObservableObject`, `[RelayCommand]`, no UI type in the view model) and record in the plan which case applied. A second view model for this screen is a stop condition. |
| `src/Pegasus.Desktop/OperationsPage.xaml` *(owned by [[FEAT-030]])* | Same rule: extended, not replaced. Two lists on [[DUI-007]]'s data-table pattern (plan handle `DSK-06-07`), plus an integration-health panel showing each dependency's state **as text** and its last-cycle time in Europe/London through the shared vocabulary map. |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | The operations snapshot DTO and the health DTO. The health payload names each dependency, its state and its last-cycle time — and carries no connection string, endpoint credential, token or internal host name. |
| `src/Pegasus.Web/` — the `/api/v1` operations group | `GET /api/v1/operations` (snapshot with `ETag`), `POST /api/v1/operations/external-work/{wid}/retry`, `POST /api/v1/operations/upload-links/{lid}/revoke` from [[GWY-013]] (plan handle `DSK-03-13`), plus consumption of `GET /api/v1/admin/health`. Behind `Features:DesktopGateway`. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Snapshot 200 with `ETag`, 401, 403; retry success and retry of an **ineligible** item refused with a problem; revoke success and replay returning the same result; a fact that the health payload contains no secret-shaped value; and the step-3 case-link fact in whichever of its two forms the decision takes. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | List loading, eligibility-driven command enablement, retry and revoke outcomes, health-state rendering including an unavailable dependency, and the freshness rule. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]] (plan handle `DSK-08-06`))* | The `operations` script: keyboard traversal and the retry command, with the `axe-windows` report attached. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | The operations rows. |
| `docs/current-architecture.md:291` | **Only if** step 3's decision is to leave the email-operations row unlinked: correct the sentence so it describes what those surfaces actually join. If the decision is to carry the link, the sentence becomes true and is left as it stands, with the evidence recorded. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by area 00)* | A **sub-heading** for the retry and revoke command behaviour inside the Operations screen section [[FEAT-030]] creates — not a second screen section. |
| `docs/capabilities.md` | `DSK` rows for operations and integration health. |

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:41-45` | `LoadedAtUtc` is set **only after the query returns**, with the reason written in the comment: "so a failed load never claims to be fresh (FRD-12)". The desktop must carry that rule — a failed refresh does not leave a stale timestamp on screen. |
| `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:71-110` | The retry contract in practice: `RetryExternalWork(workItemId, expectedAttemptCount, actor, operationKey)`, `result.IsReplay` distinguishing replay from first execution, and three distinct failure translations — `StaffAuthorizationException` → forbid, `ArgumentException` → invalid, `InvalidOperationException` → "the external work failure changed before retry". Those are three different operator outcomes, not one error. |
| `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:112-119` | Revoke's parameters — `requestId, caseId, expectedVersion, expectedCaseVersion, reason, operationKey` — and that the call is bracketed by a case edit lease acquire/release. The desktop command carries the same six values. |
| `src/Pegasus.Core/Operations/RequestOperations.cs:32-56` | `RequestOperationProjection` — what the Operations page really renders. Note `CaseId` is **non-nullable `Guid`** and `CaseReference` a `string`: these rows already join a real case. `CanRetry` (`:50`) and `CanRevoke` (`:51`) are computed server-side, which is why the client must never infer eligibility. |
| `src/Pegasus.Core/Operations/EmailOperations.cs:20-46` | `EmailOperationProjection` — the other projection, and the one at issue. `CaseId` is `Guid?`, `IntakeId` is present at `:26`, and `CanRetry => RetryMailboxId is not null && RetryExpectedDueAtUtc is not null` at `:45`. The `SourceLength` remark at `:34-42` explains why a refused message's size is shown at all. |
| `src/Pegasus.Core/Operations/EmailOperations.cs:62` and `src/Pegasus.Infrastructure/DependencyInjection.cs:240` | The fact that reframes step 3: `GetEmailOperations` is declared and registered, and **nothing calls it**. `grep -rn "GetEmailOperations" src/ --include=*.cs` returns only these two hits. The desktop Operations screen would be the first surface to render that projection — so the case-link decision is taken *before* the row is shown, not as a repair. |
| `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs:159` | The literal `CaseId: null` on the received-intake `EmailOperationProjection` row. Six other `CaseId: null` literals exist in the file (`:144`, `:175`, `:192`, `:221`, `:511`, `:543`) — `:159` is the one upstream INTK-004 is about. |
| `docs/current-architecture.md:291` | The claim: "Operations, retained Mail, Upload, MCP, and retry surfaces join the current allocation state and actual Case link." One of this sentence and `EfOperationsStore.cs:159` is wrong for the email-operations surface, and step 3 decides which. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` | The three operations routes, their `PerformCasework` right, the `ETag` on the snapshot and the `operationKey` + `reason` on both commands. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Session, compatibility, diagnostics` | `GET /admin/health` — **new**, right `ManageWorkflowConfiguration`, returning "dependency states, minimum client version, feed state". Not a rename of `/health/ready`; `src/Pegasus.Web/Health/` holds one check today. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:1652` | End-to-end business scenario **13**: "An integration failure is visible and recoverable." Plan 08 references scenarios 1–14 (`docs/desktop/08-testing/README.md:18`, `:229`, `:265`) but does not enumerate them; [[TEST-016]] (plan handle `DSK-08-16`) authors the scripts. |
| `docs/desktop/10-security-observability-performance/README.md` | What the health surface may disclose (proposal §18.3) and the performance budgets. Read before writing the health DTO. |
| `docs/design/README.md:412-445` | Banned operator words and the four hard rules; and the standing prohibition on colour-only state, which the health panel is the most likely screen to breach. |
| `HZN-001` group document `board-conventions.md` § `Upstream ids versus board ids` | The join table. Neither upstream `PLAT-023` nor upstream `INTK-004` has a fork ticket; the board's `PLAT-023` is `DSK-11-05` and the board's `INTK-004` is upstream `INTK-027`. Getting this wrong is how the board deletes real work. |

## Ripple effects

- **`openapi/pegasus-v1.json` and the generated client** — three operations routes plus the health
  read; regenerated in this change.
- **[[FEAT-030]] (plan handle `DSK-07-04`)** owns the view model and page this slice extends; the
  two must not be in flight against the same file simultaneously.
- **[[GWY-013]] (plan handle `DSK-03-13`)** owns the projection: if step 3's decision is to carry
  the case link, the `EfOperationsStore` change is that ticket's, not this one's.
- **`docs/current-architecture.md`** — if step 3 decides the other way, this slice edits `:291`.
  That file is a current-state document in the authority order
  (`docs/desktop/00-governance-and-workflow/README.md:131-134`), so the edit is a correction of
  record, not a preference.
- **[[FEAT-001]] (plan handle `DSK-05-01`)** shows the dashboard failure counts this screen drills
  into; a change to what counts as a failure changes both.
- **[[GWY-023]] (plan handle `DSK-04-06`)** supplies the minimum client version and feed state the
  panel displays.
- **[[TEST-016]] (plan handle `DSK-08-16`)** authors scenario 13 as a UAT script; this screen is
  where the scenario is observed.
- **`docs/capabilities.md`, `frd-13`, the parity matrix** — updated in the same slice.

## Out of scope

- `src/Pegasus.Worker` — untouched. The Worker executes the retried work; this screen schedules it.
- `src/Pegasus.Web/Health/DatabaseReadinessHealthCheck.cs` — not modified beyond an extension
  agreed with plan 10.
- `src/Pegasus.Infrastructure/Persistence/EfOperationsStore.cs` — the projection change, if step 3
  decides on one, belongs to [[GWY-013]].
- Creating a second `OperationsViewModel` or `OperationsPage.xaml` — a stop condition;
  [[FEAT-030]] owns both.
- Application Insights queries and Azure resource state — read-only inputs owned by plan 10 and
  plan 11. Recorded trap PLAT-034: App Insights quota can hide failures, so the pilot evidence is
  the desktop diagnostics bundle rather than a telemetry query.
- Azure: no write.
