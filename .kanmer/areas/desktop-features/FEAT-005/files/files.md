# Files — FEAT-005

Surface area of `DSK-05-05 · S5 Case edit with lease, version and completeness`.
Paths that do not exist at `HEAD` `bbd1c549` are marked with the ticket that
creates them; every other path was confirmed with `ls` or `wc -l`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | The claim/renew/release request and response DTOs, the save request (eighteen fields mirroring `CaseEditableData`), the completeness request (four booleans) and the completeness projection carrying **both** `Values` and `Evaluation`. The lease response must carry `expiresAtUtc` — the desktop cannot read `EditLeaseDuration` from Infrastructure. |
| `src/Pegasus.Web/` — the `/api/v1` cases **command** group only *(group by [[GWY-002]] (plan handle `DSK-03-02`); routes by [[GWY-008]] (plan handle `DSK-03-08`))* | `POST /cases/{id}/lease/claim|renew|release`, `PUT /cases/{id}`, `POST /cases/{id}/confirm-completeness`, each calling the same Core use case the Razor handler calls. Refusals must translate to typed 409 problems carrying the current version and, for a lease conflict, the holder's **display name**. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | `CaseEditSession`: claim on entering edit, renew on a timer inside the window derived from `ExpiresAtUtc`, release on exit, `LeaseLost` event on a failed renew. Holds the token **in memory only** — never on disk, never in a log. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | Edit state added to `CaseWorkspaceViewModel` from [[FEAT-003]] (plan handle `DSK-05-03`): dirty indicator, deliberate `SaveCommand` bound to `Ctrl+S`, navigation guard, immediate field validation against `CaseDataPolicy`, completeness command. |
| `src/Pegasus.Core/Cases/` | **Only** if a completeness precondition is found to live in the page model rather than in Core, and then only after a characterization test in `tests/Pegasus.Core.Tests`. The `research` document found none — `CaseDataPolicy.ValidateCompleteness` and `CaseCompletenessPolicy.Evaluate` are already Core-owned — so the expected diff here is **zero**. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | Dirty state, navigation guard, save disabled without a lease or while offline, operation-key reuse, lease-lost handling, 409 mapped to the conflict state with the current version captured. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Claim / renew / release replay, expiry, release by a non-holder, and 409-with-current-version. |
| `tests/Pegasus.IntegrationTests/` | The **two-user** test against LocalDB, driving the gateway directly. This project already exists; new tests must land in exactly one shard (`scripts/Invoke-TestShard.ps1 -VerifyPartition`, per [[TEST-003]] (plan handle `DSK-08-03`)). |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]] (plan handle `DSK-08-06`))* | `ui-tests.ps1 -Script case-edit`: edit, save and conflict-message assertions. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Row `PAR-08` (`:53`) — **edit handlers**; the read path was [[FEAT-003]]. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(skeleton by [[FND-008]] (plan handle `DSK-00-08`))* | Edit and edit-mode section. |
| `docs/capabilities.md` | One `DSK` row for case edit. |

## Context files

Read these before writing code. Each is here for one specific trap.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs:414-426` (`CaseLifecycleRules.ValidateMutation`) | **The five things every case mutation must present**, in one place: case id and non-negative version (`:418` → `:583-594`); an actor with `PerformCasework` (`:419` → `:596-606`); an operation key ≤ **100** characters (`:599`); a **`Reason`, required, ≤ 500 characters** (`:420`); an `EditLeaseToken` of **exactly 64** characters (`:421-425`). The reason requirement is the fact most likely to be missed: **there is no reason-free save.** |
| `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:1-66` | The refusal order as business policy. `LeaseTokenLength = 64` (`:18`); `IsHeld` (`:24-25`); `RequireVersion` (`:27-33`); `RequireLease` (`:38-66`) — expired/absent/unreadable → `CaseEditLeaseExpiredException`, wrong holder or wrong token → `CaseEditLeaseConflictException`. The class summary (`:5-11`): "there is no takeover, force, or bypass." |
| `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:68-92` | `CaseEditAuthorityHolder` (`:75-81`) and `IDescribeCaseEditAuthorityHolder` (`:83-90`). A holder is disclosed **by name**, never by identifier, and the Automation Actor is disclosed as itself (`:79-80`, ADR-0011). |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:323-336` (`ILeaseCaseForEdit`) | The exact replay contract: an exact claim or renewal replay returns the same token and expiry, and an exact release replay returns success, **before** state/version/ownership/expiry preconditions are evaluated; a key reused with different material fails with `CaseOperationConflictException`; actor authorization always precedes replay recovery. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:118-157` | `CaseEditLease(CaseId, Token, Holder, Version, ExpiresAtUtc)` (`:118-123`) — **`ExpiresAtUtc` is what the renew timer must read** — and the four refusal exceptions, with `CaseVersionConflictException.ActualVersion` at `:129`. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:338-342` (`ICaseWorkflowStore`) | "Each operation is one atomic transaction: optimistic-version and lease checks, case/due-work change, exact evidence link where supplied, idempotency, and permanent action history either all commit or all fail." That is why none of it can move to the client. |
| `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs:20`, `:173`, `:254` | `EditLeaseDuration = TimeSpan.FromMinutes(5)`. **In Infrastructure**, which the desktop must never reference. Read it to know the real window; drive the timer from `ExpiresAtUtc` instead. |
| `src/Pegasus.Core/Cases/CaseDataOperations.cs:97-205` (`CaseDataPolicy`) | The rules the desktop can and should run locally. `ValidateCompleteness` (`:105-119`) — confirming while incomplete is refused. `Normalize` (`:121-161`) — mileage ≥ 0, defined inspection mode, no `DateOnly.MinValue`, whitespace collapse plus eleven length caps, registration compacted to 20 upper-case alphanumerics (`:191-205`). `ValidateInspection` (`:163-190`) — address and mode saved **together**; `ImageBasedAssessment` needs the exact `Ext18InspectionAddressPolicy` sentinel; that sentinel cannot be a physical address. |
| `src/Pegasus.Core/Cases/CaseDataOperations.cs:15-31`, `:59-94` | `ConfirmCompleteness` reads the current `ICaseWorkflowConfiguration` and evaluates four switches; a confirmation can be **accepted and still not satisfy the policy**. `CaseCompletenessProjection` (`src/Pegasus.Core/Cases/CaseDataContracts.cs:105-107`) carries both halves, and both must reach the operator. |
| `src/Pegasus.Core/Cases/CaseDataContracts.cs:125-143` | `CaseEditableData` — the eighteen editable fields, in order, matching `OnPostSaveAsync`'s parameters exactly. |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:156-382` | The five handlers. Read them for the **operator sentences**, which are settled copy worth reusing verbatim: "Edit mode is active until …" (`:180`), "Edit mode was renewed until …" (`:222`), "Edit mode was left safely." (`:264`), and the three refusals at `:196-197`, `:240-241`, `:280`. Note `lease` never appears in any of them — it is a banned word. |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:383-423` (`RestoreLeaseState`) | The state machine the desktop replaces, and the one genuinely interesting state in it: `CanRecoverLease` (`:421-422`) — the server says this actor holds edit mode but this client lost the token. Across a desktop restart that state is real, and [[FEAT-008]] (plan handle `DSK-05-08`) renders it. |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:303-314` | `IsLeaseLoss` = expired **or** conflict; `RequiresReacquisition` adds `CaseVersionConflictException`. The remarks (`:306-312`) state the business rule: the rejected editor "must reload and reacquire rather than merge or force the save", and clearing client state "does not release the server-owned authority". The **rule** travels; the TempData clearing does not. |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:38-88`, `:195-244` | Everything not to reproduce: the 8 000 / 2 000-character budgets, the 41-name allow-list, and the retention routine itself. |
| `src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:28-32` | The bound on "editing requires edit mode": a note takes **no** lease and **no** expected version, deliberately (CASE-017), so it never contends with an engineer editing the same case. That command is [[FEAT-006]]'s (plan handle `DSK-05-06`). |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs` (77 lines) | Twelve rights (`:8-20`), fail-closed matrix (`:33-56`). `PerformCasework` admits Staff **or** Automation (`:39-41`) — which is why the Automation Actor can hold edit mode at all. |
| `docs/design/README.md:412-420` | The banned-word list. **`lease` is on it.** Operator copy uses "edit mode". `:417-420` says CI does not enforce this; the reviewer is the only gate. |
| `docs/desktop/06-ui-design/screen-specs.md:191-197` | The lease/conflict contract for the workspace: "`Enter edit mode` acquires the lease; header shows holder and expiry; renew and `Leave editing`; lease loss or stale version disables every mutation, preserves proposed values in memory for comparison and never overwrites the newer record; reload/compare/reacquire are the only recovery actions; **no forced takeover**." |
| `docs/desktop/06-ui-design/screen-specs.md:217-224` | Dirty state as a header chip; `Ctrl+S` saves; navigation away warns through a `ReasonDialog`-shaped confirmation; validation attaches to the section it concerns; the state list; `Ctrl+1..8` / `Ctrl+S` / `Esc` / `Ctrl+W`. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:54-56` | The three rows: lease claim/renew/release ("replay returns same token/expiry"), `PUT /cases/{id}`, and `POST /cases/{id}/confirm-completeness`, each with its concurrency-token column. |
| `docs/frd/frd-01-case-identity-and-lifecycle.md:82-88` | The FRD this ticket's `refs` names, and the authority for every rule above: one server-owned expiring lease; other staff read-only and able to see the holder; the rejected editor keeps proposed values and must reload and reacquire; **no Administrator bypass, forced takeover, collaborative merge or bulk case mutation**; web and MCP callers use the same guard. |
| `tests/Pegasus.IntegrationTests/CaseEditModeWebTests.cs` (126 lines) | The closest existing oracle for edit-mode entry and exit. |
| `tests/Pegasus.IntegrationTests/ConcurrencyTokenPersistenceTests.cs` (271 lines) | The persistence-level concurrency oracle — tier 4 evidence already in the repository. |
| `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs` (2,194 lines) | The largest workflow oracle; must stay green. |
| `tests/Pegasus.Core.Tests/Workflow/CaseEditAuthorityTests.cs` | The Core-level oracle for the refusal order. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` | The shared `WebApplicationFactory<Program>`; `Features:DesktopGateway` must be enabled explicitly. |
| `docs/desktop/08-testing/test-uat-stack.md:22` | The Test/UAT configuration for the two-user run. |

## Ripple effects

- **Generated client and OpenAPI snapshot.** [[GWY-005]] (plan handle
  `DSK-03-05`) commits Kiota output with a CI no-op check; [[TEST-001]] (plan
  handle `DSK-08-01`) fails the snapshot test on an undeclared change. Five new
  command shapes land in both.
- **Test sharding.** New `tests/Pegasus.IntegrationTests` facts must appear in
  exactly one shard; `scripts/Invoke-TestShard.ps1 -VerifyPartition` is the
  check, owned by [[TEST-003]] (plan handle `DSK-08-03`).
- **[[FEAT-003]]'s view model changes shape.** Edit state is added to
  `CaseWorkspaceViewModel`; every later slice that hangs a tab there inherits
  the change.
- **[[FEAT-008]] (plan handle `DSK-05-08`) consumes the states this slice
  raises.** The full reload-compare-reapply pattern is designed there; this slice
  must make version conflict, lease lost and lease-taken-by-a-named-holder
  unambiguous and never silently overwrite. A conflict UX invented here would be
  a second pattern.
- **[[FEAT-024]] (plan handle `DSK-05-24`) retires `CaseMutationPageModel` for
  desktop paths** and adds the architecture test that the desktop has no
  `TempData`/PRG equivalent. Anything this slice introduces that resembles one
  fails that test later.
- **Existing web tests must stay green.** Nothing here touches
  `Details.cshtml.cs` or `CaseMutationPageModel.cs`, so `CaseDetailsWebTests.cs`,
  `CaseEditModeWebTests.cs`, `CaseWorkflowPersistenceTests.cs` and
  `ConcurrencyTokenPersistenceTests.cs` must pass unchanged.
- **Downstream tickets.** `FEAT-005` blocks `FEAT-006`, `FEAT-008`, `FEAT-009`,
  `FEAT-014`, `FEAT-015`, `FEAT-017`, `FEAT-022`, `FEAT-024`, `FEAT-025`,
  `TEST-007` and `PLAT-017` — the widest block set in the area.
- **Documentation link check.** `scripts/Test-DocumentationLinks.ps1` runs over
  repository documentation, so a broken relative link in the new FRD-13 section
  fails CI.

## Out of scope

Recorded so the reviewer sees each was a decision.

- **`CaseMutationPageModel.cs` stays untouched.** Its retirement for desktop
  paths is [[FEAT-024]] (plan handle `DSK-05-24`); the web keeps it until
  cutover (reuse-map, `Pegasus.Web` table).
- **`Pages/Cases/Details.cshtml.cs` is not modified.** It stays live until
  `PAR-08` reaches `cut over`.
- **No TempData-retained proposed values, no PRG, no antiforgery** in the desktop
  path.
- **The full conflict-and-recovery UX is not built here.** This slice makes the
  three failure states unambiguous; reload-compare-reapply is [[FEAT-008]] (plan
  handle `DSK-05-08`).
- **No workflow, closure or task commands.** Those nineteen commands are
  [[FEAT-006]] (plan handle `DSK-05-06`).
- **No forced takeover, no merge, no bypass, no bulk edit.** Forbidden by
  `docs/frd/frd-01-case-identity-and-lifecycle.md:84-86` and by
  `CaseEditAuthority.cs:5-11`; building one is a stop condition.
- **The lease token never appears in the UI or in a log**, and is never
  persisted to disk.
- **No Azure write.** Enabling `Features:DesktopGateway` in production is
  [[PLAT-024]] (plan handle `DSK-11-06`).
