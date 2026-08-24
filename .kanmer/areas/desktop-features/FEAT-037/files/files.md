# Files — FEAT-037

Surveyed before planning. Paths that do not exist yet are marked with the ticket that creates them;
everything else was confirmed with `ls` / `grep` on 2026-08-24.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/Operations/OutboundOperationState.cs` *(new; project created by [[FND-029]] (plan handle `DSK-02-04`), conventions by [[GWY-001]] (plan handle `DSK-03-01`))* | The single outbound state vocabulary — `draft`, `queued`, `sent`, `failed`, `unknown` — plus the documented map from `EmailOperationState`. A second copy anywhere else is the duplication `AGENTS.md` § Simplicity rails calls a stop condition. Breakage risk: renaming a wire value later is a contract break caught by the OpenAPI snapshot. |
| `src/Pegasus.Contracts/Cases/CaseCommunicationsResponse.cs` *(new)* | The communications payload: per entry the direction, the five-value state, discovery / link / sent times, the correlating actor, and the linked e-mail's canonical classification (destination + category). Must **not** carry `PolicyKey` or `PolicyVersion`. |
| `src/Pegasus.Contracts/Cases/AssessmentSendRequest.cs` / `…Response.cs` *(new)* | `expectedVersion`, `editLeaseToken`, `operationKey` in; state, provider message identifier where known, and the named reconcile path out. |
| `src/Pegasus.Web/Api/V1/Cases/*` *(the `/api/v1` case route group created by [[GWY-002]] (plan handle `DSK-03-02`))* | `POST /api/v1/cases/{caseId}/assessment/send` over the existing send use case, and `GET /api/v1/cases/{caseId}/communications`. Authorisation is the per-group `StaffAccessRight` filter from [[GWY-003]] (plan handle `DSK-03-03`). |
| `src/Pegasus.Infrastructure/Persistence/` (the `IRetainedMailQueries` adapter) | Widen the case-scoped projection to carry `MailOperationalDestination` and `MailCategory?` for each linked message. Breakage risk: this is the only file where a new table could sneak in — it must be a join against the existing classification row, not a new one. |
| `tests/Pegasus.Api.ContractTests/Cases/OutboundCommandTests.cs` *(new; project created by [[TEST-001]] (plan handle `DSK-08-01`))* | The nine contract facts of body step 10. |
| `tests/Pegasus.IntegrationTests/OutboundSendEvidenceTests.cs` *(new)* | The `unknown` → poll → `sent` fact of body step 11, following `tests/Pegasus.IntegrationTests/SentEvidencePollPersistenceTests.cs`. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | The communications row (`:52`) gains the classification field; the send row (`:79`) gains the returned state. |
| `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` | The outbound command seam clause — behaviour, not mechanism. |
| `docs/desktop/06-ui-design/screen-specs.md` § `§13.8 Communications` (`:362-369`) | Record that the payload carries each linked e-mail's canonical classification, so [[DUI-013]] (plan handle `DSK-06-13`) can carry it into FRD-13. |

## Context files

What an implementer must read before touching anything above.

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:583-660` | The exact shape of the existing send and reconcile handlers — the required fields, and that the *operation key is validated before anything else happens* (`IsOperationKeyValid` at `:738`, called at `:598` and `:644`). The `/api/v1` endpoint must reproduce that ordering, not invent its own. `:644` also shows reconcile refusing an empty `requestId` in the same guard. |
| `src/Pegasus.Core/Operations/EmailOperations.cs:12-18` | That Core has **four** states (`Pending`, `Succeeded`, `Failed`, `Unknown`), not five: `draft` is client-only and has no Core counterpart. Anyone who writes a five-to-five map is wrong. `EmailOperationProjection`'s `CanRetry` (derived from `RetryMailboxId` + `RetryExpectedDueAtUtc`) also shows the house style — derive, don't store, a boolean the read model can compute. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:82-100` | The `ApprovedMailboxReportSentEvidence` record and, at `:82-84`, the sentence that makes step 8 non-negotiable: "A caller cannot substitute a draft, manual assertion, queue result, prepared text, or a report file for this evidence." Sixteen members, all discovered by the Worker — a client has nothing to offer here. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:62-79` | `ReportApprovalEvidence`'s own summary — "It does not claim the report was sent" — and `ReportApprovalSubmission`'s note that the *boundary*, not the caller, assigns actor and time. This is why approved and sent are two columns, and why [[FEAT-042]] (plan handle `DSK-07-16`) depends on this seam rather than restating it. |
| `src/Pegasus.Core/Intake/Classification/MailOperationalDestinationPolicy.cs:7-22` | `Queries` is a *destination*, and `Unidentified` is "an abstention, never a category". Also that `MailOperationalDestinationResult` bundles `PolicyKey` and `PolicyVersion` right beside the two fields that may ship — so the projection is a deliberate narrowing, and copying the record wholesale is the PLAT-015 breach. |
| `src/Pegasus.Core/Intake/RetainedMail.cs:366-381` | `IRetainedMailQueries`'s four methods, none of which projects classification. This is the gap: the case-scoped read has to be widened, and this interface is where the widening is visible. |
| `src/Pegasus.Core/Intake/Classification/MailOperationalDestinationPolicy.cs:26+` (`MailOperationalDestinationQuery`) | The precedent that the persistence adapter *translates* destination facts against the classification row and "does not own another classification-to-destination table" — the strongest evidence that the join in `A-07-11-2` is available without new schema. |
| `docs/current-architecture.md:86-90` | The repository-wide `terminal` / `transient` / `unknown` rule: terminal stops retries, unknown **remains** unknown, metrics count effects not attempts. `unknown` collapsing into `sent` is the defect this rule exists to prevent. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:16,22,52,79` | The conventions (`operationKey` on every command; replay returns the original result) plus the two reserved rows this ticket fills in. `:52` already names `IRetainedMailQueries` as the communications source — the widening is expected, not a surprise. |
| `docs/desktop/06-ui-design/screen-specs.md:362-369` | The four chips, the separate discovery/link/sent times, and the two AutomationIds `Case.Communications.Table` / `Case.Communications.Send`. It has **no** classification wording yet — proof that the documentation change in this ticket is real work, not a restatement. |
| `docs/frd/frd-08-email-mailbox-and-background-processing.md:120-135` | The classification rows whose destination is `Queries`. The FRD, not this ticket, decides what counts as a Query. |
| `src/Pegasus.Worker/EmailEvidenceFunctions.cs:16,53` | `SentEvidencePollFunction` and `DueWorkSweepFunction` — the unattended producers of the only evidence that proves a send. Nothing here may be called, changed or triggered by the desktop; `git diff --stat origin/dev -- src/Pegasus.Worker` must be empty. |
| `tests/Pegasus.IntegrationTests/SentEvidencePollPersistenceTests.cs` | The pattern for step 11's integration test: how the poll is driven in-test and how the resulting evidence is asserted. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:18-70` | The house translation of Core refusals into content-safe errors — lease expired, lease conflict, version conflict each naming the current case version, generic collapse for anything unexpected. The `/api/v1` problem mapping is ported from this by [[GWY-002]]; this ticket must produce the same refusal codes rather than new ones. |
| `docs/desktop/07-integrations/README.md` § 7 ("Scope creep into MAIL-12/13/17/19") | The named trap: only the outbound command seam is built. |

## Ripple effects

- **`openapi/pegasus-v1.json`** — the committed snapshot owned by [[GWY-004]] (plan handle
  `DSK-03-04`). Two new routes and three new schemas change it, and the snapshot test fails until
  it is regenerated and reviewed. The snapshot is reviewed evidence, not a build artefact.
- **The generated client** — [[GWY-005]] (plan handle `DSK-03-05`) regenerates the Kiota client
  from that snapshot and its committed output has a CI no-op check; a contract change that is not
  regenerated fails that check.
- **`tests/Pegasus.Api.ContractTests`** — nine new facts (body step 10).
- **`tests/Pegasus.IntegrationTests`** — one new fact (body step 11); existing sent-evidence tests
  must stay green, which is the guard that the projection widening did not disturb the poll.
- **Desktop consumers** — [[FEAT-003]] (plan handle `DSK-05-03`) renders the Communications tab
  from this read, and [[FEAT-042]] (plan handle `DSK-07-16`) consumes the seam for report finalise
  and send. Both read the contract; neither redefines it.
- **[[FEAT-045]] (plan handle `DSK-07-19`)** — the provider error taxonomy applies to this
  endpoint's refusals. If FEAT-045 lands first, use its slugs; if this lands first, FEAT-045 maps
  these refusals into the catalogue. Either order works; two vocabularies do not.
- **`docs/desktop/06-ui-design/screen-specs.md`** — the § 13.8 block gains the classification
  sentence, which [[DUI-013]] then carries into FRD-13.
- **No migration.** If the implementer finds themselves writing one, `A-07-11-2` has failed and
  `scripts/Test-MigrationGrants.ps1` (the PLAT-035 runtime-role `Grant*` check) is the CI job that
  will say so — stop and raise it rather than adding the table.

## Out of scope

Recorded here so the reviewer sees each as a decision, per the ticket's Guardrails:

- **`src/Pegasus.Worker`** — untouched; the verification asserts an empty diff for that path.
- **`src/Pegasus.Infrastructure/Email/`** — untouched; no outbound provider client is added
  anywhere. ADR-0106 keeps the mail service credential central.
- **Compose, mailbox mutation, idempotent report send as a new capability, and automatic
  chasers** — upstream MAIL-12/13/17/19, open upstream capabilities and out of conversion scope
  under proposal § 13.11.
- **Query lifecycle** — creating, replying to and resolving a query stay with **upstream CASE-002**
  (not imported; and note that board [[CASE-002]] is upstream CASE-022, public upload links — a
  different ticket entirely). This ticket carries the *classification*, which is what upstream
  CASE-009 (also not imported) actually needed.
- **The classification policy itself** — `src/Pegasus.Core/Intake/Classification/` is read and
  projected, never changed.
- **`PolicyKey` and `PolicyVersion` on the wire** — deliberately dropped at the boundary.
- **Any new table for idempotency bookkeeping** — the existing operation-key mechanics are reused.
