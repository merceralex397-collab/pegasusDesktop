# Endpoint map: `/api/v1` derived from the Razor surface

One row per endpoint the desktop needs, derived from the 53 page models and
their handlers under `src/Pegasus.Web/Pages/` (inventory of 2026-08-23).
"Replaces" names the page model and handler whose behaviour the endpoint
carries; the endpoint calls the **same Core use case or port** the handler
calls today, so there is one business implementation. Where the exact
use-case type name was not confirmed during inventory, the owning Core
folder is given; the implementing ticket records the precise type.

Conventions (from [README.md](README.md) § 3):

- Base path `/api/v1`; bearer token from area 04; every request carries
  `X-Pegasus-Client-Version` and `X-Correlation-Id`.
- Commands are explicit verbs (`POST …/hold`), never a generic action
  endpoint. Every command body carries `operationKey`; case-scoped commands
  also carry `expectedVersion` and, where Core requires it,
  `editLeaseToken` — the **Concurrency token** column says which.
- Reads return `version` and a weak `ETag`; lists default to newest first.
- **Auth right** is the `StaffAccessRight` checked by the endpoint filter;
  `PerformCasework` implies `AccessStaffApplication`.
- **Idempotent?** `yes (key)` = replay of the same `operationKey` returns
  the same result; `GET` endpoints are naturally idempotent.
- **Phase** is the proposal §24 phase in which the desktop slice needs it.

## Session, compatibility, diagnostics

| Area | Method + route | Replaces (page/handler) | Core use case/port | Auth right | Idempotent? | Concurrency token | Returns | Phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Session | `POST /connect/token` (grant `password` / `refresh_token`, client `pegasus-desktop`) | `Account/SignIn` `OnPostAsync` (cookie sign-in) | Identity `CheckPasswordSignInAsync` + `StaffActorFactory` (area 04) | anonymous (rate-limited) | n/a | — | access + refresh token | 2 |
| Session | `POST /session/logout` | `Account/SignOut` `OnPostAsync` | OpenIddict revocation (area 04) | AccessStaffApplication | yes | — | 204 | 2 |
| Session | `POST /session/password-change` | `Account/PasswordChange` `OnPostAsync` | `src/Pegasus.Core/Identity/` password change use case | AccessStaffApplication | yes (key) | — | 204, refresh tokens revoked | 2 |
| Session | `GET /session/me` | `StaffPageModel` claims resolution | `StaffActorFactory.TryCreate` | AccessStaffApplication | GET | — | actor id, roles, rights, must-change-password flag | 2 |
| Compatibility | `GET /client-compatibility` | — (new, §9.1) | admin setting (area 04) | anonymous | GET | — | minimum/current version, channel, maintenance, TTL | 2 |
| Diagnostics | `GET /diagnostics/version` (existing) | `Program.cs:954` | — | anonymous | GET | — | version, sourceSha | 2 |
| Health | `GET /health/live`, `GET /health/ready` (existing) | `Program.cs:939-950` | `DatabaseReadinessHealthCheck` | anonymous | GET | — | status | 2 |
| Health | `GET /admin/health` | — (new, §18.3) | aggregation over existing checks + worker last-cycle, provider state | ManageWorkflowConfiguration | GET | — | dependency states, minimum client version, feed state | 8 |

## Dashboard and rail counts

| Area | Method + route | Replaces (page/handler) | Core use case/port | Auth right | Idempotent? | Concurrency token | Returns | Phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Dashboard | `GET /dashboard` | `Index.cshtml.cs` `OnGetAsync` (43 lines) | `IDashboardQueries` (`src/Pegasus.Core/Operations/`) | AccessStaffApplication | GET | ETag | assigned/new/overdue/recent/integration-failure counts and lists | 3 |
| Dashboard | `GET /dashboard/rail-counts` | `Presentation/RailCountsPageFilter.cs` | `IDashboardQueries.GetCaseStageCountsAsync` | AccessStaffApplication | GET | ETag | counts per rail entry (only figures already queried; absent = nothing) | 3 |

## Cases

| Area | Method + route | Replaces (page/handler) | Core use case/port | Auth right | Idempotent? | Concurrency token | Returns | Phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Cases | `GET /cases?page&pageSize&sort&stage&assignee&principal&q` | `Cases/Index` `OnGetAsync` (261 lines), `Search/Index` redirect | `ICaseQueryStore` search/list | PerformCasework | GET | ETag | paged list, newest first | 3 |
| Cases | `GET /cases/{id}` | `Cases/Details` `OnGetAsync` (header, overview, parties, dates, next action) | `ICaseDataQueries`, `ICaseWorkflowQueries` | PerformCasework | GET | ETag + `version` | case header + overview section | 3 |
| Cases | `GET /cases/{id}/assessment`, `/documents`, `/communications`, `/tasks`, `/reports`, `/history` | sections of `Cases/Details`, `Cases/Assessment/Index`, `Cases/Tasks` partials | `ICaseAssessmentStore` reads, `IDocumentContentStore` listing, `IRetainedMailQueries` (case association), `ICaseTaskQueries`, `IEvaHandoffQueries`, action-history reads | PerformCasework | GET | ETag per section | section payloads, loaded lazily | 3 (history), 4–7 (others) |
| Cases | `POST /cases` | `Cases/Create` `OnPostCreateAsync` (689 lines) | `src/Pegasus.Core/Cases/` create use case via `IAllocateIntake`/acceptance path | PerformCasework | yes (key) | `operationKey` | 201 + case id + version | 4 |
| Cases | `POST /cases/{id}/lease/claim`, `…/renew`, `…/release` | `Cases/Details` `OnPostClaimLeaseAsync`, `OnPostRenewLeaseAsync`, `OnPostReleaseLeaseAsync` | `IAcquireCaseEditLease`, `IRenewCaseEditLease`, `IReleaseCaseEditLease` (`CaseCommandContracts.cs:77-91`) | PerformCasework | yes (key; replay returns same token/expiry) | `expectedVersion`, `operationKey` (`editLeaseToken` for renew/release) | lease token, expiry, holder | 4 |
| Cases | `PUT /cases/{id}` (save details) | `Cases/Details` `OnPostSaveAsync` | `ICaseDataStore` save use case | PerformCasework | yes (key) | `expectedVersion`, `editLeaseToken`, `operationKey` | new version | 4 |
| Cases | `POST /cases/{id}/confirm-completeness` | `Cases/Details` `OnPostConfirmCompletenessAsync` | completeness command (`src/Pegasus.Core/Cases/`) | PerformCasework | yes (key) | `expectedVersion`, `editLeaseToken`, `operationKey` | new version | 4 |
| Cases | `POST /cases/{id}/hold`, `/release-hold`, `/return-to-review`, `/assign-engineer`, `/start-work`, `/record-engineer-finding`, `/linked-replacement` | `Cases/Workflow` seven `OnPost*Async` handlers (227 lines) | `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs`, `CaseCommandSeams.cs` commands | PerformCasework (engineer finding: Engineer role) | yes (key) | `CaseMutationRequest` fields | new version (+ replacement id) | 4 |
| Cases | `POST /cases/{id}/report-approval`, `/close`, `/reopen`, `/archive` | `Cases/Closure` four handlers (121 lines) | lifecycle commands (`Lifecycle/`) | PerformCasework | yes (key) | `CaseMutationRequest` fields; reopen requires `reason` | new version | 4 |
| Tasks | `POST /cases/{id}/notes` | `Cases/Tasks` `OnPostAddNoteAsync` | notes command (`src/Pegasus.Core/Cases/`) | PerformCasework | yes (key) | `CaseMutationRequest` fields | note id, version | 4 |
| Tasks | `POST /cases/{id}/tasks`, `POST /cases/{id}/tasks/{taskId}/assign`, `/complete`, `/cancel` | `Cases/Tasks` `OnPostCreateTaskAsync`, `OnPostAssignTaskAsync`, `OnPostCompleteTaskAsync`, `OnPostCancelTaskAsync` | `src/Pegasus.Core/Tasks/` commands (`CaseTaskVersionConflictException`) | PerformCasework | yes (key) | task `expectedVersion`, `operationKey` | task, version | 4 |
| Tasks | `POST /cases/{id}/chases/manual` | `Cases/Tasks` `OnPostRecordManualChaseAsync` | `Tasks/` manual chase command | PerformCasework | yes (key) | `CaseMutationRequest` fields | version | 5 |
| Tasks | `POST /cases/{id}/report-evidence/link`, `/unlink` | `Cases/Tasks` `OnPostLinkReportEvidenceAsync`, `OnPostUnlinkReportEvidenceAsync` | `Workflow/` report-evidence link commands | PerformCasework | yes (key) | `CaseMutationRequest` fields | version | 5 |
| Custody | `POST /cases/{id}/custody/retry` | `Cases/Custody` `OnPostRetryCustodyAsync` | human-only custody retry use case (`Custody/`) | PerformCasework | yes (key) | `CaseMutationRequest` fields | version, work item id | 6 |
| Custody | `POST /cases/{id}/documents/upload-session` → `PUT /upload-sessions/{sid}` → `POST /upload-sessions/{sid}/complete` | `Cases/Custody` `OnPostUploadDocumentAsync` (`IFormFile`) | `ICaseCustody` / document add use case; limits from `IntakeEnvelopeLimits` | PerformCasework | complete: yes (key) | `CaseMutationRequest` fields on complete | document id, version | 6 |
| Custody | `DELETE /cases/{id}/documents/{docId}` (soft, reasoned) | `Cases/Custody` `OnPostRemoveDocumentAsync` | document remove command | PerformCasework | yes (key) | `CaseMutationRequest` fields + `reason` | version | 6 |
| Custody | `POST /cases/{id}/third-party-vehicle-evidence/confirm` | `Cases/Custody` `OnPostConfirmThirdPartyVehicleEvidenceAsync` | evidence confirmation command | PerformCasework | yes (key) | `CaseMutationRequest` fields | version | 6 |
| Custody | `POST /cases/{id}/request-upload-links`, `DELETE /cases/{id}/request-upload-links/{linkId}` | `Cases/Custody` `OnPostCreateRequestUploadLinkAsync`, `OnPostRevokeRequestUploadLinkAsync` | `RequestUploadPolicy` (`Documents/RequestUploadPolicy.cs`) create/revoke | PerformCasework | yes (key) | `operationKey` | link id, expiry | 6 |
| Documents | `GET /cases/{id}/documents/{docId}/content` | `Cases/Documents/Download` `OnGetAsync` (112 lines) | `IDocumentContentStore` | PerformCasework | GET | ETag, range | bytes, no-sniff, safe filename | 6 |
| Documents | `POST /cases/{id}/documents/export` | `Cases/Documents/Export` `OnPostAsync` (160 lines) | export use case (CASE-019 proof) | PerformCasework | yes (key) | `operationKey` | archive bytes (async job id if long) | 6 |
| Vehicle | `POST /cases/{id}/vehicle/lookups` | `Cases/Vehicle` `OnPostRequestVehicleLookupAsync` | `src/Pegasus.Core/Vehicle/` lookup request (durable request row; Worker executes) | PerformCasework | yes (key) | `CaseMutationRequest` fields | request id, status | 6 |
| Vehicle | `POST /cases/{id}/vehicle/suggestions/{sid}/accept` | `Cases/Vehicle` `OnPostAcceptVehicleSuggestionAsync` | vehicle suggestion acceptance | PerformCasework | yes (key) | `CaseMutationRequest` fields | version | 6 |
| Vehicle | `GET /cases/{id}/vehicle` | `Cases/Details` vehicle section | `IVehicleEvidenceQueries` | PerformCasework | GET | ETag + `version` | confirmed values, lookup status/observations, provider provenance, source age and typed failure | 6 |
| EVA | `POST /cases/{id}/eva-handoff`, `GET /cases/{id}/eva-handoff/{revision}/bundle` | `Cases/Vehicle` `OnPostGenerateEvaHandoffAsync`; `Cases/Eva/Download` `OnPostAsync` | `src/Pegasus.Core/Eva/` handoff generation, `IEvaHandoffQueries`, reasoned download | PerformCasework | generate: yes (key); download: GET | `CaseMutationRequest` fields | revision id; bundle bytes | 6 |
| Assessment | `GET /cases/{id}/assessment` | `Cases/Assessment/Index` `OnGetAsync` (740 lines) | `ICaseAssessmentStore` reads, `AssessmentPolicy` | PerformCasework | GET | ETag + `version` | assessment model, readiness summary | 7 |
| Assessment | `POST /cases/{id}/assessment/damage` | `OnPostSaveDamageAsync` | `src/Pegasus.Core/Assessment/` save command | PerformCasework | yes (key) | `CaseMutationRequest` fields | version | 7 |
| Assessment | `POST /cases/{id}/assessment/estimate-import` (upload session) | `OnPostImportEstimateAsync` (`IFormFile`) | `IEstimateDocumentParser` (`AudatexEstimatePdfParser`) via Core import | PerformCasework | yes (key) | `CaseMutationRequest` fields | imported lines, version | 7 |
| Assessment | `POST /cases/{id}/assessment/specification/accept` | `OnPostAcceptSpecificationAsync` | repair specification acceptance | Engineer | yes (key) | `CaseMutationRequest` fields | version | 7 |
| Assessment | `POST /cases/{id}/reports/draft` | `OnPostGenerateReportDraftAsync` | `GenerateCaseAssessmentReportDraft` → `IAssessmentReportRenderer` (gateway-side until L-03 parity; then the desktop renders and `POST /cases/{id}/reports` registers the final PDF) | PerformCasework | yes (key) | `CaseMutationRequest` fields | report bytes or report id + ETag | 7 |
| Assessment | `POST /cases/{id}/reports` (register final), `GET /cases/{id}/reports/{rid}/content` | — (new for L-03; today the web keeps the rendered draft server-side) | report registration + `IDocumentContentStore` | PerformCasework | yes (key) | `CaseMutationRequest` fields | report id, version; bytes | 7 |
| Assessment | `POST /cases/{id}/assessment/send`, `/reconcile` | `OnPostSendAsync`, `OnPostReconcileAsync` | send/reconcile commands (`Assessment/`, `Workflow/`) | PerformCasework | yes (key) | `CaseMutationRequest` fields | version, send status | 7 |

## Intake (received items), uploads, image intake

| Area | Method + route | Replaces (page/handler) | Core use case/port | Auth right | Idempotent? | Concurrency token | Returns | Phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Intake | `GET /received?page&pageSize&queue&state` | `Operations/Index` queue lists, `UploadStatus` | `ListIntake` (`Intake/IntakeQueryUseCases.cs:5`), `IIntakeReceiptQueries` | PerformCasework | GET | ETag | paged receipts | 5 |
| Intake | `GET /received/{id}` | `Intake/Details` `OnGetAsync` (613 lines) | `GetIntake` (`IntakeQueryUseCases.cs:43`) | PerformCasework | GET | ETag + `version` | receipt, evidence, suggestions, drafts, OCR-required state | 5 |
| Intake | `POST /received/{id}/retry-allocation`, `/block`, `/reevaluate`, `/correct-draft`, `/dismiss-suggestion`, `/register-image-intake` | `Intake/Details` `OnPostRetryAllocationAsync`, `OnPostBlockAsync`, `OnPostReevaluateAsync`, `OnPostCorrectDraftAsync`, `OnPostDismissSuggestionAsync`, `OnPostRegisterImageIntakeAsync` | named Core intake commands (`Intake/DurableIntake.cs`, `Intake/IntakeAllocation.cs:208` `AllocateIntake`, `ImageIntake/` registration) | PerformCasework | yes (key) | receipt `expectedVersion`, `operationKey`, `reason` where Core requires | version | 5 |
| Intake | `POST /received/{id}/case-lease/claim`, `POST /received/{id}/link-case`, `POST /received/{id}/reverse-case-link` | `Intake/Details` `OnPostClaimCaseLeaseAsync`, `OnPostLinkCaseAsync`, `OnPostReverseCaseLinkAsync` | `IAcquireCaseEditLease`, `ILinkIntake` (`DurableIntake.cs:1109`), `IReverseIntakeLink` | PerformCasework | yes (key) | receipt version + case `expectedVersion` + `editLeaseToken` | version(s) | 5 |
| Intake | `GET /received/{id}/source`, `GET /received/{id}/assets/{aid}`, `GET /received/{id}/images/{iid}` | `Intake/Source`, `Intake/Asset`, `Intake/Image` `OnGetAsync` | `DownloadIntakeSource` (`Intake/DownloadIntakeSource.cs`), asset/image reads | PerformCasework | GET | ETag, range | bytes, no-sniff, safe filename, SHA-256 validated | 5 |
| Uploads | `POST /uploads/upload-session` → `PUT` bytes → `POST …/complete` | `Upload.cshtml.cs` `OnPostAsync` (`IFormFile`, 10 MiB, one file) | `ReceiveIntake` staging + Worker dispatch | PerformCasework | complete: yes (key = receipt token) | `operationKey` | receipt id + status URL | 5 |
| Uploads | `GET /uploads/{receiptId}/status` | `UploadStatus` `OnGetAsync` | `EfQueuedIntakeStatusQueries` via Core port | PerformCasework | GET | ETag | Received/Processing/Complete/Failed | 5 |
| Uploads | `GET /uploads/groups/{gid}`, `POST /uploads/groups`, `POST /uploads/groups/{gid}/attach` | `UploadGroupStatus` `OnGetAsync`, `OnPostRegisterGroupAsync`, `OnPostAttachGroupAsync` | grouped upload use cases (`Intake/`) | PerformCasework | yes (key) | `operationKey` | group status | 5 |
| Uploads (external) | `GET/POST /Uploads/{token}` | `Uploads/Request` — **stays a Razor page** (anonymous request-link actor, antiforgery, PRG) | `GetRequestUpload`, `UploadToRequest` | RequestLink actor | — | — | web only | — |
| Image intake | `GET /image-intake?page`, `GET /image-intake/{id}`, `POST /image-intake/{id}/close` | `ImageIntake/Index` `OnGetAsync`, `ImageIntake/Details` `OnGetAsync`, `OnPostCloseAsync`; `VehicleImages` routes | `IImageIntakeQueries`, `IImageIntakeStore` lifecycle (`ImageIntake/`) | PerformCasework | close: yes (key) | `expectedVersion`, `operationKey` | list / detail / version | 5 |

## Mail workspace

| Area | Method + route | Replaces (page/handler) | Core use case/port | Auth right | Idempotent? | Concurrency token | Returns | Phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Mail | `GET /mail?mailbox&folder&page&pageSize&q&queue&destination&classification` | `Mail/Index` `OnGetAsync` (428 lines) | `ListRetainedMail`, `GetRetainedMailFreshness` (`Intake/RetainedMail.cs`) | PerformCasework | GET | ETag | retained messages newest first, freshness; `pageSize` ≤ 100; destination and detailed classification are mutually exclusive | 5 |
| Mail | `POST /mail/refresh` | `Mail/Index` manual refresh | freshness refresh use case | PerformCasework | yes (coalesced) | — | freshness | 5 |
| Mail | `GET /mail/deleted?mailbox&search&page&pageSize` | `Mail/Index` Deleted Items view | `SearchDeletedMail` (resolved Deleted Items read, cap 100) (`Intake/DeletedMailSearch.cs`) | PerformCasework | GET | ETag | deleted-mail page with `state` and `isTruncated`; no retention or backfill | 5 |
| Mail | `GET /mail/{id}/preview` | `Mail/Index` `OnGetPreviewAsync` (`JsonResult`, `:176`) | retained body preview (`MailBodyPresentation`) | PerformCasework | GET | ETag | inert text preview | 5 |
| Mail | `GET /mail/{id}` | `Mail/Message` `OnGetAsync` (1,025 lines) | `GetRetainedMail` (thread, attachments, classification, queue, outcome, association, move result, suggested move) | PerformCasework | GET | ETag + versions | message detail, whole folder recommendation (`folderType`, policy key/version, reason, mailbox version, `canMove`), suggested/latest move | 5 |
| Mail | `POST /mail/{id}/link-case/prepare`, `POST /mail/{id}/link-case`, `POST /mail/{id}/unlink-case/prepare`, `POST /mail/{id}/unlink-case` | `Mail/Message` `OnPostPrepareLinkCaseAsync`, `OnPostLinkCaseAsync`, `OnPostPrepareUnlinkCaseAsync`, `OnPostUnlinkCaseAsync` | case search/detail queries + `IAcquireCaseEditLease` + `ILinkIntake` / `IReverseIntakeLink` | PerformCasework | yes (key) | message/receipt versions, case `expectedVersion` + `editLeaseToken` | versions; unlink warns "Unlinking this email cancels case <ref>" | 5 |
| Mail | `POST /mail/{id}/classification` | `Mail/Message` `OnPostCorrectClassificationAsync` | classification correction command | PerformCasework | version-based; `operationKey` is the audit correlation | classification version | version and classification history | 5 |
| Mail | `POST /mail/{id}/move-to-recommended-folder` | `Mail/Message` `OnPostMoveToRecommendedFolderAsync` | folder-move command (provider port; absent when provider unavailable) | PerformCasework | yes (key) | classification/recommendation/mailbox versions, `operationKey`, `reason` | move record | 5 |

## Triage, Unidentified, Operations

| Area | Method + route | Replaces (page/handler) | Core use case/port | Auth right | Idempotent? | Concurrency token | Returns | Phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Triage | `GET /triage?page&state`, `GET /triage/{id}`, `GET /triage/{id}/source` | `Triage/Index` `OnGetAsync` (449 lines), `Triage/Details` `OnGetAsync` (496 lines) | `ITriageQueries`, triage source download (`Triage/`) | PerformCasework | GET | ETag + `version` | list / detail / bytes | 5 |
| Triage | `POST /triage/{id}/assign`, `/unassign`, `/await-information`, `/findings`, `/findings/{fid}/supersede`, `/responses/link`, `/responses/unlink`, `/complete`, `/cancel`, `/reopen`, `/case-link`, `/case-unlink` | `Triage/Details` `OnPostActionAsync` dispatcher cases `assign`, `unassign`, `await_information`, `record_finding`, `supersede_finding`, `link_response`, `unlink_response`, `complete`, `cancel`, `reopen`, `link_case`, `unlink_case` (`Pages/Triage/Details.cshtml.cs:116-204`; verify the full set) | triage lifecycle commands (`Triage/TriageLifecycle.cs`), same as MCP `pegasus_triage_*` tools | PerformCasework (assign → Engineer selection per INTK-019) | yes (key) | triage `expectedVersion`, `operationKey`, `reason` where required | version | 5 |
| Unidentified | `GET /unidentified?page`, `GET /unidentified/{id}`, `GET /unidentified/{id}/members/{mid}/source`, `POST /unidentified/{id}/resolve` | `Unidentified/Index` `OnGet`, `Unidentified/Details` `OnGetAsync`, `OnPostResolveAsync` | `IUnidentifiedStore` queries/commands (`Intake/Unidentified/`, key ≤ 200) | PerformCasework | resolve: yes (key) | `expectedVersion`, `operationKey`, `reason` | list / detail / bytes / version | 5 |
| Operations | `GET /operations` | `Operations/Index` `OnGetAsync` (236 lines) | Operations projection (`Operations/`): retryable external work, active upload links | PerformCasework | GET | ETag | snapshot | 3 |
| Operations | `POST /operations/external-work/{wid}/retry`, `POST /operations/upload-links/{lid}/revoke` | `Operations/Index` `OnPostRetryExternalAsync`, `OnPostRevokeLinkAsync` | `IExternalWorkStore` retry, request-link revoke | PerformCasework | yes (key) | `operationKey`, `reason` | status | 3 |

## Administration and audit

| Area | Method + route | Replaces (page/handler) | Core use case/port | Auth right | Idempotent? | Concurrency token | Returns | Phase |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Admin | `GET /admin/configuration`, `PUT /admin/configuration` | `Administration/Configuration` `OnGetAsync`, `OnPostAsync` | `ICaseWorkflowConfiguration` reads/writes (`Workflow/`) | ManageWorkflowConfiguration | yes (key) | configuration version, `operationKey` | configuration | 8 |
| Admin | `GET /admin/mail-categories`, `PUT /admin/mail-categories` | `Administration/MailCategories` `OnGetAsync`, `OnPostSaveAsync` | approved Outlook categories (`Identity/` approved categories) | ManageApprovedOutlookCategories | yes (key) | version, `operationKey` | categories | 8 |
| Admin | `GET /admin/mailboxes`, `PUT /admin/mailboxes/{id}`, `POST /admin/mailboxes/{id}/resolve-folders` | `Administration/Mailboxes` `OnGetAsync`, `OnPostUpdateAsync`, `OnPostResolveFoldersAsync` (362 lines) | approved mailbox estate (`Identity/`, ADR-0022/0024; resolver is Web-only Graph read) | ManageApprovedMailboxes | yes (key) | mailbox version, `operationKey` | mailboxes / resolved folders | 8 |
| Admin | `GET /admin/access-review`, `POST /admin/access-review` | `Administration/Access/Index` `OnGetAsync`, `OnPostReviewAsync` | staff access review (`Identity/`) | ReviewStaffAccess | yes (key) | `operationKey` | review record | 8 |
| Admin | `GET /admin/accounts`, `POST /admin/accounts`, `GET /admin/accounts/{id}`, `POST /admin/accounts/{id}/disable` | `Administration/Accounts/Index` `OnGetAsync`, `OnPostCreateAsync`; `Accounts/Edit` `OnGetAsync`, `OnPostDisableAsync` | `IStaffAccountQueries`, staff-account administration (`Identity/StaffAccountAdministration.cs`) | ManageStaffAccounts | yes (key) | `operationKey`; disable requires `reason` | account; disabled → tokens revoked | 8 |
| Admin | `GET /admin/roles`, `POST /admin/roles/assign` | `Administration/Roles/Index` `OnGetAsync`, `OnPostAssignAsync` | role assignment (`Identity/`) | AssignStaffRoles | yes (key) | `operationKey` | roles | 8 |
| Admin | `GET /admin/automation`, `POST /admin/automation/enabled`, `POST /admin/automation/send-to-ai-enabled`, `PUT /admin/automation/connector`, `POST /admin/automation/channel-token/rotate`, `POST /admin/automation/channel-token/clear`, `GET /admin/automation/activity` | `Administration/Automation/Index` five handlers (260 lines), `Automation/Activity` `OnGetAsync` | `AutomationClientRegistry`, Send-to-AI settings, automation activity (`Identity/`, `AiWork/`) | ManageAutomationClients | yes (key) | `operationKey` | settings / activity | 8 |
| Admin | `GET /admin/organizations`, `POST /admin/organizations`, `GET /admin/organizations/{id}`, `PUT /admin/organizations/{id}` | `Administration/Organizations/Index` `OnGetAsync`, `OnPostCreateAsync`; `Organizations/Edit` `OnGetAsync`, `OnPostUpdateAsync` | organization administration (`Cases/OrganizationAdministration.cs`, key ≤ 100) | ManageOrganizationsAndPrincipals | yes (key) | organization version, `operationKey` | organization | 8 |
| Admin | `GET /admin/principals`, `POST /admin/principals`, `POST /admin/principals/{id}/replace` | `Administration/Principals/Index`, `Principals/Create` `OnPostCreateAsync`, `Principals/Replace` `OnPostReplaceAsync` | principal administration (`Cases/`), provider inspection mode (ADR-0018) | ManageOrganizationsAndPrincipals | yes (key) | `operationKey`, `reason` | principal | 8 |
| Audit | `GET /audit?actor&case&from&to&page` | history partials, `Automation/Activity` | action-history / security-event read ports (`Identity/IdentityContracts.cs:98-137`) | ManageStaffAccounts (full) / PerformCasework (own case history) | GET | ETag | paged history | 3 (case history), 8 (search) |
| Reference data | `GET /reference/providers`, `/principals`, `/engineers`, `/mailboxes` (lookups) | dropdown sources across Create/Details/Triage pages | `IProviderReferenceCatalog`, principal/organization queries, staff lists | PerformCasework | GET | ETag (short cache) | small reference lists | 3–4 |

## Stays web-only (not projected)

| Page | Reason |
| --- | --- |
| `Pages/Uploads/Request.cshtml.cs` | Anonymous external audience (request-link actor), antiforgery + PRG; not a desktop surface (proposal §13.11 boundary) |
| `Pages/Connect/Authorize.cshtml.cs` | OpenIddict consent for external MCP connectors (ADR-0027); the desktop uses the password grant, not consent |
| `Pages/Error.cshtml.cs`, `Pages/StatusCode.cshtml.cs` | Web status pages; the desktop renders problem details natively |
| `Pages/Account/AccessDenied.cshtml.cs` | Replaced by the `not-authorized` problem type |
| Razor partials, `wwwroot/css/site.css`, `wwwroot/js/site.js` | Presentation; retired after cutover (area 05 cut list) |

## Coverage check

Every page model in the 2026-08-23 inventory appears above either as a
replaced handler or in "stays web-only": Account (4), Cases (12 incl.
Assessment, Documents, Eva), Intake (4), Mail (2), Triage (2), Unidentified
(2), ImageIntake (2), Operations (1), Upload/UploadStatus/UploadGroupStatus/
Uploads-Request (4), Administration (16), Index, Search, Connect/Authorize,
Error, StatusCode (5) = 53. The parity matrix in
[../01-inventory-and-parity/parity-matrix.md](../01-inventory-and-parity/parity-matrix.md)
is the authority for status per row; this map is the API column of that
matrix.
