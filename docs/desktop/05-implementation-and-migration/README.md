# 05 · Implementation and migration — reuse map and vertical slices

This area is the implementation route from the current Razor Pages web
application to the native desktop client: what is reused, what is
extracted, what is replaced, what is cut, and the ordered vertical slices
(Phases 3–8 of the proposal) that carry every current capability across
with parity evidence. Supporting files:

- [reuse-map.md](reuse-map.md) — component-by-component REUSE / EXTRACT /
  REPLACE / CUT decisions with the evidence behind each.
- [vertical-slices.md](vertical-slices.md) — the 22 slices S1–S22 in the
  ticket-ready shape of proposal §25.

## 1. Purpose and proposal coverage

| Proposal section | What this area does with it |
| --- | --- |
| §3.1 What "core" means | Maps Domain/Application/Desktop/Gateway/Worker/Adapters onto the existing four projects; keeps `Pegasus.Core` as the single business-policy owner |
| §4.1 Placement decisions | Restated per slice (desktop vs. cloud, with the six-question test) |
| §5.3 Dependency direction | Enforced by the reuse map and the architecture tests listed in area 02 |
| §6.1 Fork controls | New native projects rather than page translation; characterization tests before moving logic; small vertical slices; ADR-recorded deviations |
| §13 Current and desired functionality | Every §13.1–13.10 group is owned by at least one slice; §13.11 is explicitly out of scope |
| §22.1 Characterization before refactoring | The characterization gap list in §3 and a per-slice verification line |
| §24 Phases 3–8 | Phase exit gates restated in §4; slices assigned to phases |
| §25 Ticket structure | Each slice is written in the twelve-section shape; the work breakdown carries one ticket per slice |

Out of scope here: the foundation (area 02), the gateway conventions (area
03), authentication/updates/startup (area 04), UI tokens and screen specs
(area 06), integration adapters (area 07), test strategy (area 08).

## 2. Evidence base

### Facts

Verified by read-only inspection of the fork at `main` `191ddf33`
(2026-08-23); line numbers are from that revision.

- `Pegasus.Core` is already the transport-neutral Domain+Application layer
  the proposal asks for: 107 `.cs` files, 227 public port interfaces, zero
  package dependencies (`src/Pegasus.Core/Pegasus.Core.csproj`). Folder
  sizes: `Intake/` 32, `Workflow/` 8, `Identity/` 8, `Cases/` 8,
  `ImageIntake/` 7, `Tasks/` 5, `Assessment/` 5, `Vehicle/` 4, `Triage/` 4,
  `Operations/` 4, `ReferenceData/` 3, `Actors/` 3, `Reports/` 2,
  `Lifecycle/` 2, `Eva/` 2, `Documents/` 2, `Custody/` 2, `AiWork/` 2,
  `Address/` 2.
- Every mutation already travels in a remote-API shape: `CaseMutationRequest`
  (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182`) carries
  `CaseId`, `ExpectedVersion`, `ActionActor`, `OperationKey`, `Reason`,
  `EditLeaseToken`; replay semantics are documented on `ILeaseCaseForEdit`
  (`CaseWorkflowContracts.cs:322-334`); version conflicts throw
  `CaseVersionConflictException` (`CaseWorkflowContracts.cs:125`); lease
  tokens are 64 hex chars (`src/Pegasus.Core/Workflow/CaseEditAuthority.cs`);
  lease commands are `IAcquireCaseEditLease` / `IRenewCaseEditLease` /
  `IReleaseCaseEditLease` (`src/Pegasus.Core/Workflow/CaseCommandContracts.cs:77-91`).
- Actor and authorization are transport-neutral: `ActorKind`
  (`src/Pegasus.Core/Identity/IdentityContracts.cs:22`), `ActionActor`
  (`:30`), `StaffAccessRight` with twelve rights and a fail-closed switch
  (`src/Pegasus.Core/Identity/StaffAuthorization.cs`),
  `StaffActorFactory.TryCreate` (`src/Pegasus.Core/Actors/StaffActorFactory.cs`).
- `Pegasus.Web` holds 53 Razor page models (~10,800 LOC) plus 76 `.cshtml`;
  the largest are `Pages/Mail/Message.cshtml.cs` 1,025,
  `Pages/Cases/Assessment/Index.cshtml.cs` 740, `Pages/Cases/Create.cshtml.cs`
  689, `Pages/Cases/Details.cshtml.cs` 654, `Pages/Intake/Details.cshtml.cs`
  613, `Pages/Triage/Details.cshtml.cs` 496, `Pages/Triage/Index.cshtml.cs`
  449, `Pages/Mail/Index.cshtml.cs` 428, `Pages/Administration/Mailboxes.cshtml.cs`
  362, `Pages/Cases/CaseMutationPageModel.cs` 339.
- 50 of the 53 page models import no `Pegasus.Infrastructure` type; the
  three `Pages/Account/*` models reference `PegasusIdentityUser`.
- Web-only state machinery that must not be carried over: PRG with 65
  `RedirectToPage` calls across 27 page models; `TempData` in 29 page models;
  `CaseMutationPageModel` retains proposed values in cookie TempData with
  budgets `MaximumRetainedProposedCharacters = 8000` and
  `MaximumRetainedProposedValueCharacters = 2000` and a
  `RetainableFormFields` allow-list of about thirty names
  (`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:36-80`).
- `src/Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs` is the single
  code→operator-vocabulary map consumed by the gateway and desktop. The
  Core-typed `src/Pegasus.Web/Presentation/OperatorLabels.cs` adapter preserves
  the 24 `.cshtml` consumers. `Presentation/RailCountsPageFilter.cs` (51 lines) is an
  `IAsyncPageFilter` writing `ViewData["RailCounts"]`.
- The MCP layer (`src/Pegasus.Web/Mcp/`, 14 files, ~3,200 LOC, 35 tools) is
  the only existing machine-readable projection of Core; it touches
  `IHttpContextAccessor` only in `AutomationActorResolver.cs:29`, and
  `AutomationMcpErrors.cs` (154 lines) already maps Core exceptions to
  transport errors.
- Report rendering is `Pegasus.Core.Reports.IAssessmentReportRenderer` →
  `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`
  (326 lines; Scriban templates embedded from
  `docs/design/assets/report-renderer/templates/`, Playwright Chromium
  `PdfAsync`, PDFsharp post-processing; singleton; ADR-0025, ADR-0028).
- `Pegasus.Worker` has nine functions (`IntakeFunctions.cs`,
  `MailboxFunctions.cs`, `EmailEvidenceFunctions.cs`,
  `Functions/ExternalWorkFunctions.cs`) and translates triggers into Core use
  cases only (`docs/current-architecture.md` § Worker callers).
- Test estate: xunit 2.9.3 throughout; `tests/Pegasus.Core.Tests` 69 files,
  494 facts, 72 theories; `tests/Pegasus.IntegrationTests` 136 files, 716
  facts (59 files use `WebApplicationFactory<Program>`; shared factory at
  `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26`); browser lane
  `tests/Pegasus.IntegrationTests/Browser/` 9 files, 20 facts including
  `AccessibilityTests.cs` (Deque.AxeCore.Playwright);
  `tests/Pegasus.ArchitectureTests` 11 files, 62 facts (custom reflection,
  `DependencyDirectionTests.cs` 520 lines).
- Design authority: `docs/design/README.md` four hard rules (lines 422–445),
  banned operator words (412–420), approved necessary-copy list (400–409),
  status vocabulary, route order Dashboard → Inbox → Upload → Queues → Cases
  → Operations → Administration; `AGENTS.md § Simplicity rails` makes
  "operator-facing explanation is a defect" a merge rule.
- Upstream board (`collisionengineers/pegasus@kanmer-board`, 2026-08-23) has
  109 open tickets; the UI-redesign and defect tickets absorbed by slices are
  listed per slice in [vertical-slices.md](vertical-slices.md) and triaged in
  [01 · upstream carry-over](../01-inventory-and-parity/upstream-kanmer-carryover.md).

### Assumptions

- Area 03 will publish the gateway endpoint names in its `endpoint-map.md`;
  the slices reference route *groups* (`/api/v1/cases`, `/api/v1/received`,
  …) and area 03 is authoritative for exact paths.
- Area 06 will publish one screen spec per slice; the slices reference the
  spec by slice ID.
- The thirteen triage commands dispatched by
  `Pages/Triage/Details.cshtml.cs:OnPostActionAsync` include the ten named
  by the MCP tool set (await-information, record-finding, supersede-finding,
  response-link, response-unlink, complete, cancel, reopen, case-link,
  case-unlink); the remaining three are enumerated during S11 research, not
  assumed.
- Upstream fixes continue to land during the conversion; the one-way sync in
  area 00 keeps the gateway current, and each slice re-checks its page model
  against the synced revision before implementation.

## 3. Decisions and assumptions

- **Locked**: L-01 (gateway evolves inside `Pegasus.Web`), L-03 (local
  WebView2 report rendering, detail in area 07), the local Test/UAT stack
  (area 08), the subagent set (area 12).
- **Deviation: `Pegasus.Core` is not split into `Pegasus.Domain` and
  `Pegasus.Application`** (proposal §5.4). The repository's "one Core owner"
  invariant (`AGENTS.md § Product invariants`, `docs/engineering.md`
  § One Core owner) and the fact that Core already has zero package
  dependencies and transport-neutral actors make the split pure ceremony.
  Recorded in ADR-0100; revisit only if a desktop-only application-layer
  concern appears that Core must not carry.
- **Deviation: slices replace pages, they do not translate them.** Each
  slice is specified from the business capability, the Core use cases and
  the design authority, with the Razor page model used as behavioural
  evidence only (proposal §6.1 control 1).
- **Extract `OperatorLabels` to a shared assembly.** The operator vocabulary
  (24 consumers) becomes one list used by web and desktop alike
  (one-list-per-concept rule, `AGENTS.md § Simplicity rails`). Its settled home
  is `Pegasus.Contracts`; GWY-016 owns the extraction and leaves only the
  Core-typed Web adapter.
- **Web stays live until cutover.** Razor pages remain deployable and
  feature-gated `/api/v1` groups ship beside them (area 03); no page is
  removed before its slice reaches `UAT passed` in the parity matrix.
- **Characterization before moving any rule.** Core policies are already
  covered by `tests/Pegasus.Core.Tests` (494 facts); the page-model level
  behaviours listed below are deliberately *not* preserved (they are web
  mechanics, not business behaviour):
  - TempData-retained proposed values and the `RetainableFormFields`
    allow-list;
  - PRG redirects and `TempData["CaseDetailsStatus"]` style status passing
    (upstream CASE-001 asks to show-or-drop it — dropped for desktop);
  - antiforgery tokens (replaced by bearer tokens, area 04);
  - the `IAsyncPageFilter` rail-count injection (becomes an endpoint).
  Characterization gaps to close before the slice that moves them: case
  completeness confirmation rules (S5), create-screen draft-to-case mapping
  (S4), intake draft correction and link/unlink integrity checks (S9, S10),
  the triage action matrix (S11), assessment save/import/reconcile rules
  (S17), report projection fixtures (S18), administration validation rules
  (S19). Where a rule lives in a page model rather than Core, the slice first
  moves it into Core with a test (stop condition: duplicate business
  implementation).
- **Placement**: every slice answers the six-question cloud test in
  [vertical-slices.md](vertical-slices.md); the default is desktop for
  interaction and validation, gateway for authoritative writes, audit,
  secrets and shared data.
- **No Azure writes in this area.** Slices consume the gateway; the only
  ⚠ items (compatibility setting, feed, signing) are owned by areas 04, 09
  and 11.

## 4. Target state and exit gate

Target state: every §13.1–13.10 capability has a native screen backed by a
gateway endpoint and a Core use case, with parity evidence in
`docs/desktop/01-inventory-and-parity/parity-matrix.md` at `UAT passed` or
better, and the Razor page it replaces marked `cut over`. Phase exit gates
(proposal §24) are the programme gates; the slice exit gates are in
[vertical-slices.md](vertical-slices.md).

| Phase | Slices | Exit gate (proposal §24) |
| --- | --- | --- |
| 3 | S1–S3 | Native workflow uses real test data through the gateway; paging/filtering/performance budgets pass; accessibility and keyboard baseline passes; parallel comparison with web results matches |
| 4 | S4–S8 | Two-user conflict test passes; all critical case rules unit-tested; no silent overwrite; UAT approves the primary case workflow |
| 5 | S9–S13 | Intake arrives while the desktop is closed; duplicate and failure paths pass; no desktop holds Graph credentials; full source-to-case traceability |
| 6 | S14–S16 | Large and failed transfers recover safely; provider secrets absent from the package; provider rate/error handling passes; document parity approved |
| 7 | S17–S18 | Approved fixtures match expected values/content; no required report depends on the web renderer unless explicitly retained; final document and audit correct; performance target passes on baseline hardware |
| 8 | S19–S22 | Full automated suite passes; accessibility critical issues resolved; security review has no unresolved high-risk item; production-like package tested |

Proof for each slice: the ticket's proof document (Kanmer `proof/`) carries
the commands run, test output, screenshots, and the parity-matrix row update.

## 5. Work breakdown

One ticket per slice plus four cross-cutting tickets. Dependencies name the
slice they build on and the area tickets they need (`03-*` endpoint groups,
`04-*` session, `06-*` screen specs, `02-*` foundation). Tier = evidence
tier from `docs/engineering.md § Required evidence tiers`. Profile is the
Kanmer profile on the fork board (`feature` unless stated).

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-05-01 | S1 Dashboard and work queue | feature | DSK-02 foundation; DSK-04 session; DSK-03 dashboard group; DSK-06 S1 spec | Dashboard answers the five §14.3 questions from live gateway data; rail counts match web for the same data | VM tests; contract tests; `winapp ui` smoke; parity comparison table | 5, 7, 12 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · pegasus-desktop-reviewer · skills winui-dev-workflow, winui-design, dotnet-webapi, code-testing-agent, run-tests · MCP Microsoft Learn, Kanmer |
| DSK-05-02 | S2 Case list and search | feature | DSK-05-01; DSK-03 cases group | Server-paged, sorted, filtered list; `Ctrl+K` search; column chooser; keyboard open | VM tests; paging contract tests; perf budget (first page ≤ 1 s); `winapp ui` | 5, 7, 10 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills winui-design, dotnet-webapi, optimizing-ef-core-queries · MCP Microsoft Learn, Kanmer |
| DSK-05-03 | S3 Case detail read-only and history | feature | DSK-05-02; DSK-03 case sections + audit | Case header + Overview/History sections lazy-loaded; audit rows match web | VM tests; contract tests; parity comparison | 5, 7, 12 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills winui-design, dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-04 | S4 Case create | feature | DSK-05-03; DSK-03 create endpoint; characterization of create mapping | Create from instruction draft or blank; provenance beside each field; allocation rules identical to web | Core characterization tests; contract tests; VM tests; UAT script | 2, 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills winui-design, dotnet-webapi, code-testing-agent · MCP Microsoft Learn, Kanmer |
| DSK-05-05 | S5 Case edit with lease, version and completeness | feature | DSK-05-03; DSK-03 lease/save endpoints | Deliberate save; dirty state; lease claim/renew/release; confirm completeness; no silent overwrite | Two-user conflict test; VM tests; contract tests; UAT | 4, 5, 7, 12 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · pegasus-desktop-reviewer · skills winui-design, dotnet-webapi, test-gap-analysis · MCP Microsoft Learn, Kanmer |
| DSK-05-06 | S6 Workflow, closure and tasks commands | feature | DSK-05-05 | Every Workflow/Closure/Tasks command available as an explicit, audited action with confirmation per design authority | Contract tests per command incl. authorization failures; VM tests | 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi, code-testing-agent · MCP Microsoft Learn, Kanmer |
| DSK-05-07 | S7 Parties and reference data (organizations, principals) | feature | DSK-05-03; DSK-03 administration group | Admin-only CRUD of organizations/principals incl. principal replace; immutable principal after allocation respected | Contract tests; VM tests; authorization tests | 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-08 | S8 Concurrency UX (conflict, lease lost, replay) | feature | DSK-05-05 | 409/lease-lost/replayed outcomes render reload-compare-reapply; operation keys reused on retry | Contract tests for conflict; VM tests; UAT two-user script | 5, 7, 12 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · pegasus-desktop-reviewer · skills winui-design, dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-09 | S9 Received items (intake detail, actions, bytes) | feature | DSK-05-05; DSK-03 received group | Retry/block/reevaluate/correct-draft/link/reverse-link/register-image/dismiss; source/asset/image bytes stream | Characterization tests; contract tests; VM tests; corpus-driven integration (tier 8) | 2, 5, 7, 8 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi, minimal-api-file-upload, code-testing-agent · MCP Microsoft Learn, Kanmer |
| DSK-05-10 | S10 Mail workspace (list, message, link/unlink, classify, move) | feature | DSK-05-09; DSK-03 mail group | List scoped by mailbox/folder with freshness; message detail; link/unlink with confirmations; classification correction; recommended move | Contract tests; VM tests; `winapp ui` dialogs; parity vs `MailWorkspaceWebTests.cs` | 5, 7, 12 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · pegasus-desktop-reviewer · skills winui-design, dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-11 | S11 Triage list, detail and actions | feature | DSK-05-09; DSK-03 triage group | All thirteen actions explicit and audited; source download | Characterization of the action matrix; contract tests; VM tests | 2, 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi, code-testing-agent · MCP Microsoft Learn, Kanmer |
| DSK-05-12 | S12 Unidentified and vehicle images | feature | DSK-05-09 | Unidentified list/detail/resolve; vehicle-images list/detail/close; VRM suggestions shown | Contract tests; VM tests | 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-13 | S13 Uploads (manual, status, groups) | feature | DSK-05-09; DSK-03 uploads group | Drag/drop or picker upload within limits; status and group status; 10 MiB limit enforced client and server | Contract tests incl. limit; VM tests; `winapp ui` file picker | 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills minimal-api-file-upload, winui-ui-testing · MCP Microsoft Learn, Kanmer |
| DSK-05-14 | S14 Documents and custody (Box browser, transfer queue, preview) | feature | DSK-05-05; DSK-07 Box design | Folder/file list, upload queue with progress/cancel/retry, preview, export, remove, retry custody, request-link create/revoke | Contract tests; VM tests; large/failed transfer tests; parity | 5, 7, 10 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · pegasus-desktop-reviewer · skills winui-design, dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-15 | S15 Vehicle lookup and EVA handoff | feature | DSK-05-05; DSK-07 DVLA design | Request lookup, accept suggestion, provider error states distinct; EVA bundle generate/download | Contract tests; VM tests; replay-adapter integration | 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-16 | S16 Images and gallery | feature | DSK-05-14 | Reusable gallery, progressive thumbnails, evidence tab reads document records | VM tests; perf budget (thumbnails never block navigation); `winapp ui` | 7, 10 | winui-dev · pegasus-test-engineer · pegasus-ui-verifier · skills winui-design, analyzing-dotnet-performance · MCP Microsoft Learn, Kanmer |
| DSK-05-17 | S17 Assessment workbench | feature | DSK-05-05; DSK-03 assessment group | Save damage, import estimate, accept specification, reconcile; mileage prefill from lookup evidence | Characterization tests on assessment rules; contract tests; VM tests | 2, 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi, code-testing-agent · MCP Microsoft Learn, Kanmer |
| DSK-05-18 | S18 Report generation, preview, finalise, send | feature | DSK-05-17; DSK-07 WebView2 renderer; ADR-0108 | Local render via isolated WebView2; preview; finalise registers canonical copy through the gateway; send with idempotency | Golden-file tests; contract tests; VM tests; perf on baseline hardware | 2, 5, 7, 10 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · pegasus-desktop-reviewer · skills winui-design, dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-19 | S19 Administration | feature | DSK-05-07; DSK-03 administration group | Configuration, mail categories, mailboxes, access review, accounts, roles, automation, activity — admin-only, audited | Authorization tests per endpoint; VM tests; UAT | 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-20 | S20 Operations and integration health | feature | DSK-05-01; DSK-03 operations group | Retryable external work, active links, integration health, retry/revoke | Contract tests; VM tests | 5, 7 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-21 | S21 Password change and account lifecycle | feature | DSK-04 session | Change password; must-change-password routing; disabled account explained | Contract tests; VM tests; security tests | 5, 9 | winui-dev · pegasus-gateway-dev · pegasus-test-engineer · skills dotnet-webapi · MCP Microsoft Learn, Kanmer |
| DSK-05-22 | S22 Hardening sweep | chore | DSK-05-01…21 | All slices pass accessibility, performance and security baselines; parity matrix complete | axe-windows scan; `winapp ui` suite; perf report; security checklist | 7, 9, 10 | pegasus-ui-verifier · pegasus-desktop-reviewer · pegasus-test-engineer · skills winui-ui-testing, winui-code-review, analyzing-dotnet-performance · MCP Microsoft Learn, Kanmer |
| DSK-05-23 | Extract `OperatorLabels` to the shared assembly (covered by GWY-016) | chore | DSK-02 Contracts project | One vocabulary list used by web and desktop; 24 `.cshtml` consumers retain their signatures; sanctioned intake wording follows the design authority | Base/post characterization; existing web tests green; architecture ownership guard | 1, 2, 5 | pegasus-gateway-dev · pegasus-desktop-reviewer · skills dotnet-webapi, run-tests · MCP Kanmer |
| DSK-05-24 | Retire `CaseMutationPageModel` state machine for desktop paths | chore | DSK-05-05, DSK-05-08 | Desktop edit state is in-memory VM state plus server lease; no TempData equivalents introduced | Architecture test: desktop has no TempData/PRG; VM tests | 1, 7 | winui-dev · pegasus-desktop-reviewer · skills winui-code-review · MCP Kanmer |
| DSK-05-25 | Parity evidence per slice (matrix maintenance) | chore | each slice | Every slice's row reaches `automated verification passed` then `UAT passed` with linked proof | Matrix diff reviewed per PR | 12 | pegasus-parity-researcher · pegasus-desktop-reviewer · skills kanmer-verify · MCP Kanmer |
| DSK-05-26 | Cut-list execution after cutover | chore | Phase 10 approval | Razor pages, partials, `site.css`, `site.js`, browser lane removed per reuse-map cut list; web-only routes kept | Build green; architecture tests; release notes | 1, 5 | pegasus-gateway-dev · pegasus-desktop-reviewer · skills run-tests · MCP Kanmer |

## 6. Routing table

| Need | Subagent | Skills (pinned source) | MCP |
| --- | --- | --- | --- |
| Native screens, view models, navigation | `winui-dev` | `winui-dev-workflow`, `winui-design`, `winui-code-review` (win-dev-skills v0.5.0 `f1028dd5`) | Microsoft Learn, Kanmer |
| Gateway endpoints and contracts for a slice | `pegasus-gateway-dev` | `dotnet-webapi`, `minimal-api-file-upload`, `optimizing-ef-core-queries`, `microsoft-code-reference` (dotnet/skills `98f84851`; Microsoft Learn plugin) | Microsoft Learn, Kanmer |
| Tests: characterization, VM, contract, gap analysis | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `test-gap-analysis`, `assertion-quality` (dotnet/skills) | Microsoft Learn |
| Independent review of boundaries, XAML, a11y | `pegasus-desktop-reviewer` | `winui-code-review`, `winui-design` | Microsoft Learn |
| UI automation, accessibility and performance evidence | `pegasus-ui-verifier` | `winui-ui-testing`, `analyzing-dotnet-performance` | — |
| Parity matrix rows and page-model evidence | `pegasus-parity-researcher` | `kanmer-research`, `kanmer-verify` | Kanmer |
| Ticket pipeline | (any) | `kanmer-tickets`, `kanmer-plan`, `kanmer-execute`, `kanmer-review` | Kanmer (`get_doc_gates`, `take_ticket`, `set_ticket_doc`, `move_item`) |

Every slice ticket records the skills consulted with their commit SHAs in
its post-implementation report (proposal Appendix C; template in area 00).

## 7. Risks and traps

- **Scope creep from §13.11.** AI assistants, WhatsApp, Audatex/Tractable,
  EVA replacement, provider APIs, MI reporting are *not* parity; they remain
  upstream backlog (carry-over category `unchanged-backlog`). A slice that
  needs one of them stops and raises a ticket.
- **Parity drift.** Upstream keeps fixing the web app (32 commits ahead on
  2026-08-23). Each slice re-reads its page model after the latest sync and
  records the revision it characterized.
- **The two giants.** `Pages/Mail/Message.cshtml.cs` (1,025) and
  `Pages/Cases/Assessment/Index.cshtml.cs` (740) are split into sub-slices
  (S10a list/preview, S10b message/link-unlink, S10c classify/move; S17a
  damage, S17b estimate import/accept, S17c reconcile) and never landed as
  one PR.
- **Page-model logic that is really business logic.** If a rule is found
  only in a page model, it is moved into Core with a test before the slice
  consumes it; a second implementation is a stop condition.
- **Design authority is a merge rule.** No field hints, no how-it-works
  copy, only populated sections, filters are dropdowns, newest first; banned
  words (`intake`, `lease`, `artifact`, `projection`, …) never reach the UI;
  every state renders through the shared vocabulary list.
- **`TreatWarningsAsErrors=true`** and `AnalysisLevel=latest-recommended`
  apply to the new projects; WinUI analyzers from `winui-dev-workflow` add
  `WUI*` warnings — fix, do not suppress wholesale.
- **Do not reproduce web mechanics.** TempData budgets, PRG, antiforgery,
  `IAsyncPageFilter` injection, `ViewData` are web-only; desktop slices hold
  state in view models and rely on server leases/versions.
- **Feature gates.** `/api/v1` groups are behind `Features:DesktopGateway`;
  an endpoint that is "registered" but gated off returns 404 — integration
  tests must enable the gate explicitly.
- **Binary endpoints and limits.** Source/asset/image/document bytes and the
  10 MiB single-file limit (`IntakeEnvelopeLimits`) are enforced server-side;
  the desktop must stream, not buffer.
- **Recorded repository traps** (release skill): a new table needs runtime
  role GRANT migrations (PLAT-035); App Insights quota hides failures
  (PLAT-034) — desktop diagnostics bundles are the evidence during pilots.

## 8. Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row status per
  slice (designed → implemented → automated verification passed → UAT passed
  → cut over → legacy path retired).
- `docs/adr/0100-…md` (native client, records the no-split deviation),
  `0108` (WebView2 rendering, consumed by S18); no new ADR per slice.
- `docs/frd/frd-13-desktop-operator-experience.md` — desktop behaviour per
  slice where it deliberately differs from the web (area 00 owns the FRD
  skeleton; slices add sections).
- `docs/capabilities.md` — `DSK-nn` rows per slice, canonical owner FRD-13.
- `docs/current-architecture.md` — implementation map rows for
  `Pegasus.Desktop*` and the shared vocabulary assembly once merged;
  `docs/operations.md` unchanged until a release ships a slice.
- `docs/index.md` already links this plan set.
