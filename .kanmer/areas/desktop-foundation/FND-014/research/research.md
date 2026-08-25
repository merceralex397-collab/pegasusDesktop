# Research — FND-014: Re-derive the page-model inventory and confirm the parity-matrix skeleton

## Question

Does the 46-row skeleton in `docs/desktop/01-inventory-and-parity/parity-matrix.md` match the Razor page-model and handler surface at the current fork head, and are its path, base-class, command-set, and non-Razor entries sufficient for the dependent parity tickets?

## Baseline and scope

- Baseline: `git rev-parse HEAD` and `git log -1 --format='%H%n%cI'` both returned `ecb9b7b40c802b5ea800a69a7a46a0875269737a`; commit time `2026-08-24T19:11:49+01:00`.
- Scope stayed within the two authorized repository documents: `docs/desktop/01-inventory-and-parity/parity-matrix.md` and `docs/desktop/01-inventory-and-parity/README.md`. No source, tests, runtime, Azure, or status-column changes were made.
- The raw 136-line handler declaration output is in FND-014 scratch `notes.md`, produced by the exact command required by the ticket. Of these, 134 declarations are in the 53 `*.cshtml.cs` page-model files and two are shared inherited declarations in `UploadConfirmationPageModel.cs`; the latter are represented once in PAR-29 and explicitly inherited by PAR-30.

## Findings

### Counts and difference lists

- `git ls-files 'src/Pegasus.Web/**/*.cshtml.cs'` returned **53** page models.
- `git ls-files 'src/Pegasus.Web/**/*.cshtml'` returned **76** views.
- `git grep -n "public .*On\\(Get\\|Post\\)[A-Za-z]*" -- 'src/Pegasus.Web/Pages'` returned **136 declaration lines**: 134 are in the 53 `*.cshtml.cs` page-model files and two are shared inherited declarations in `UploadConfirmationPageModel.cs`. The per-file reconciliation for the 53 page-model files is complete, and the two shared declarations are represented once in PAR-29 with PAR-30's inheritance called out explicitly.
- Difference list (a), page models with no matrix row: **empty** after correcting the five missing `Administration/` prefixes.
- Difference list (b), matrix page-model paths that do not exist: **empty** after the same corrections.
- Difference list (c), handlers present in code but absent from a row, or listed by a row but absent from code: **empty** after representing the two shared `UploadConfirmationPageModel` handlers once in PAR-29 and noting their inheritance in PAR-30. `PAR-04` and the two shell pages intentionally have no handler list; `PAR-43` is a web-shell grouping.
- The pathspec check is material: `src/Pegasus.Web/Pages/Cases/**/*.cshtml.cs` returns **4**, while `Pages/Cases/*.cshtml.cs` returns **12**; the Administration equivalents return **11** and **15**. The correction was handed to [[FND-016]] in its Kanmer scratch.

### Corrections made

- Added an `Inventoried at` column and stamped all 46 rows with `ecb9b7b40c802b5ea800a69a7a46a0875269737a`.
- Corrected `PAR-37`, `PAR-39`, `PAR-40`, and `PAR-41` entries so every secondary Administration page has its full `Administration/` prefix. The five affected files are Accounts/Edit, Automation/Activity, Organizations/Edit, Principals/Create, and Principals/Replace.
- Corrected `PAR-43` so the `Error` and `StatusCode` page models each record their `OnGet` handler; the independent audit had caught this omission.
- Corrected `PAR-24`: `Triage/Details.OnPostActionAsync` has 12 named cases, not 13. The named set is `assign`, `unassign`, `await_information`, `record_finding`, `supersede_finding`, `link_response`, `unlink_response`, `complete`, `cancel`, `reopen`, `link_case`, and `unlink_case`. The `default` branch throws for unsupported input and is not a command. The matrix now records the expanded set and `× 12`.
- Corrected the area-plan citation from `Pages/Shared/StaffPageModel` to `Pages/StaffPageModel.cs` and corrected the area-plan risk note from a 13-command Triage dispatch to 12 named commands plus a rejecting default branch.
- Resolved U-10 by extending the same authorized README edit to correct the objectively measured `CaseMutationPageModel` count from **7** to **8**. The eight derivers are Closure, Custody, Details, Documents/Export, Eva/Download, Tasks, Vehicle, and Workflow. This is a documentation correction, not a product decision or runtime change.

### Base-class map

The exact `git grep -n` map is recorded in the scratch evidence. Reconciled counts:

| Base class | Raw matches | Actual page models | Evidence |
| --- | ---: | ---: | --- |
| `StaffPageModel` | 21 | **18** | subtract the Administration, CaseMutation, and UploadConfirmation base declarations |
| `AdministrationPageModel` | 16 | **16** | all are page models, including external `Connect/Authorize` |
| `CaseMutationPageModel` | 8 | **8** | all are case page models |
| `UploadConfirmationPageModel` | 2 | **2** | UploadStatus and UploadGroupStatus |
| none of the four | — | **9** | explicitly listed in the skeleton below |

`Connect/Authorize` still is the external OpenIddict consent page and remains a deliberate `legacy path retained` row; its Administration base inheritance is recorded, not changed.

### HTTP surface and MCP projection

- `Program.cs` has `MapHealthChecks` at lines 939 and 945 and `MapGet("/diagnostics/version")` at 954.
- `AutomationMcpExtensions.cs` has OpenIddict endpoint URI setup at lines 39–40, token `MapPost` at 134, and `MapMcp` at 137. Literal paths are `/connect/token`, `/authorize`, and `/mcp`.
- The broader `MapGet|MapPost|MapPut|MapDelete|AddControllers` search found only the known health/version and token registrations; no unrepresented HTTP surface.
- MCP enumeration returned **35** distinct `pegasus_*` tools. The raw `McpServerTool` count of 42 includes seven tool-type attributes, so it does not indicate 42 tools. `PAR-46` remains the covering row.
- History command `git log -p --reverse 191ddf33..HEAD -- src/Pegasus.Web/Pages/Triage/Details.cshtml.cs` returned no commits. There is no evidence of a removed thirteenth command; the old 13 count was a skeleton miscount.

### Multi-command handler check

The listed multi-handler files were read and searched for dispatch constructs. They expose discrete named Razor handlers, each delegating to one named Core operation. Outcome/result switches in Assessment, Intake, Mail, Operations, and Mailboxes are result-label or state handling, not action-name command dispatch. `Triage/Details.OnPostActionAsync` is the only action-name dispatcher and is now expanded in `PAR-24`.

## Reconciled skeleton

The table is the complete 46-row handoff to [[FND-015]], [[FND-016]], [[FND-017]], and [[FND-018]]. `none` means the page model does not derive from one of the four shared base classes; non-Razor rows are labelled explicitly. Every row is stamped at the same fork SHA.

| PAR | Page model / entry point | Handlers or command set | Base class | Inventoried at |
| --- | --- | --- | --- | --- |
| PAR-01 | Account/SignIn.cshtml.cs | OnGet<br>OnPostAsync | Account/SignIn.cshtml.cs: none | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-02 | Account/SignOut.cshtml.cs | OnGet<br>OnPostAsync | Account/SignOut.cshtml.cs: none | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-03 | Account/PasswordChange.cshtml.cs | OnGetAsync<br>OnPostAsync | Account/PasswordChange.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-04 | Account/AccessDenied.cshtml.cs | — | Account/AccessDenied.cshtml.cs: none | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-05 | Index.cshtml.cs | OnGetAsync | Index.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-06 | Search/Index.cshtml.cs | OnGet | Search/Index.cshtml.cs: none | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-07 | Cases/Index.cshtml.cs | OnGetAsync | Cases/Index.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-08 | Cases/Details.cshtml.cs | OnGetAsync<br>OnPostClaimLeaseAsync<br>OnPostRenewLeaseAsync<br>OnPostReleaseLeaseAsync<br>OnPostConfirmCompletenessAsync<br>OnPostSaveAsync | Cases/Details.cshtml.cs: CaseMutationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-09 | Cases/Create.cshtml.cs | OnGetAsync<br>OnPostCreateAsync | Cases/Create.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-10 | Cases/Workflow.cshtml.cs | OnPostHoldAsync<br>OnPostReleaseHoldAsync<br>OnPostReturnToReviewAsync<br>OnPostAssignEngineerAsync<br>OnPostStartWorkAsync<br>OnPostRecordEngineerFindingAsync<br>OnPostCreateLinkedReplacementAsync | Cases/Workflow.cshtml.cs: CaseMutationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-11 | Cases/Tasks.cshtml.cs | OnPostAddNoteAsync<br>OnPostCreateTaskAsync<br>OnPostAssignTaskAsync<br>OnPostCompleteTaskAsync<br>OnPostCancelTaskAsync<br>OnPostRecordManualChaseAsync<br>OnPostLinkReportEvidenceAsync<br>OnPostUnlinkReportEvidenceAsync | Cases/Tasks.cshtml.cs: CaseMutationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-12 | Cases/Closure.cshtml.cs | OnPostRecordReportApprovalAsync<br>OnPostCloseAsync<br>OnPostReopenAsync<br>OnPostArchiveAsync | Cases/Closure.cshtml.cs: CaseMutationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-13 | Cases/Custody.cshtml.cs | OnPostRetryCustodyAsync<br>OnPostUploadDocumentAsync<br>OnPostRemoveDocumentAsync<br>OnPostConfirmThirdPartyVehicleEvidenceAsync<br>OnPostCreateRequestUploadLinkAsync<br>OnPostRevokeRequestUploadLinkAsync | Cases/Custody.cshtml.cs: CaseMutationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-14 | Cases/Vehicle.cshtml.cs | OnPostRequestVehicleLookupAsync<br>OnPostAcceptVehicleSuggestionAsync<br>OnPostGenerateEvaHandoffAsync | Cases/Vehicle.cshtml.cs: CaseMutationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-15 | Cases/Assessment/Index.cshtml.cs | OnGetAsync<br>OnPostSaveDamageAsync<br>OnPostGenerateReportDraftAsync<br>OnPostImportEstimateAsync<br>OnPostAcceptSpecificationAsync<br>OnPostSendAsync<br>OnPostReconcileAsync | Cases/Assessment/Index.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-16 | Cases/Documents/Download.cshtml.cs | OnGetAsync | Cases/Documents/Download.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-17 | Cases/Documents/Export.cshtml.cs | OnPostAsync | Cases/Documents/Export.cshtml.cs: CaseMutationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-18 | Cases/Eva/Download.cshtml.cs | OnPostAsync | Cases/Eva/Download.cshtml.cs: CaseMutationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-19 | Intake/Details.cshtml.cs | OnGetAsync<br>OnPostRetryAllocationAsync<br>OnPostBlockAsync<br>OnPostReevaluateAsync<br>OnPostCorrectDraftAsync<br>OnPostClaimCaseLeaseAsync<br>OnPostLinkCaseAsync<br>OnPostReverseCaseLinkAsync<br>OnPostRegisterImageIntakeAsync<br>OnPostDismissSuggestionAsync | Intake/Details.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-20 | Intake/Asset.cshtml.cs<br>Intake/Image.cshtml.cs<br>Intake/Source.cshtml.cs | OnGetAsync | Intake/Asset.cshtml.cs: StaffPageModel<br>Intake/Image.cshtml.cs: StaffPageModel<br>Intake/Source.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-21 | Mail/Index.cshtml.cs | OnGetAsync<br>OnGetPreviewAsync | Mail/Index.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-22 | Mail/Message.cshtml.cs | OnGetAsync<br>OnPostPrepareLinkCaseAsync<br>OnPostPrepareUnlinkCaseAsync<br>OnPostLinkCaseAsync<br>OnPostUnlinkCaseAsync<br>OnPostCorrectClassificationAsync<br>OnPostMoveToRecommendedFolderAsync | Mail/Message.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-23 | Triage/Index.cshtml.cs | OnGetAsync | Triage/Index.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-24 | Triage/Details.cshtml.cs | OnGetAsync<br>OnPostActionAsync | Triage/Details.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-25 | Unidentified/Index.cshtml.cs<br>Unidentified/Details.cshtml.cs | OnGet<br>OnGetAsync<br>OnPostResolveAsync | Unidentified/Index.cshtml.cs: none<br>Unidentified/Details.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-26 | ImageIntake/Index.cshtml.cs<br>ImageIntake/Details.cshtml.cs | OnGetAsync<br>OnPostCloseAsync | ImageIntake/Index.cshtml.cs: none<br>ImageIntake/Details.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-27 | Operations/Index.cshtml.cs | OnGetAsync<br>OnPostRetryExternalAsync<br>OnPostRevokeLinkAsync | Operations/Index.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-28 | Upload.cshtml.cs | OnGet<br>OnPostAsync | Upload.cshtml.cs: StaffPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-29 | UploadStatus.cshtml.cs; shared UploadConfirmationPageModel.cs (also inherited by PAR-30) | OnGetAsync; OnGetCaseSearchAsync; OnPostAttachAsync | UploadStatus.cshtml.cs: UploadConfirmationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-30 | UploadGroupStatus.cshtml.cs (inherits the shared handlers listed in PAR-29) | OnGetAsync<br>OnPostRegisterGroupAsync<br>OnPostAttachGroupAsync | UploadGroupStatus.cshtml.cs: UploadConfirmationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-31 | Uploads/Request.cshtml.cs | OnGetAsync<br>OnPostAsync | Uploads/Request.cshtml.cs: none | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-32 | Administration/Index.cshtml.cs | OnGet | Administration/Index.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-33 | Administration/Configuration.cshtml.cs | OnGetAsync<br>OnPostAsync | Administration/Configuration.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-34 | Administration/MailCategories.cshtml.cs | OnGetAsync<br>OnPostSaveAsync | Administration/MailCategories.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-35 | Administration/Mailboxes.cshtml.cs | OnGetAsync<br>OnPostUpdateAsync<br>OnPostResolveFoldersAsync | Administration/Mailboxes.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-36 | Administration/Access/Index.cshtml.cs | OnGetAsync<br>OnPostReviewAsync | Administration/Access/Index.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-37 | Administration/Accounts/Index.cshtml.cs<br>Administration/Accounts/Edit.cshtml.cs | OnGetAsync<br>OnPostCreateAsync<br>OnPostDisableAsync | Administration/Accounts/Index.cshtml.cs: AdministrationPageModel<br>Administration/Accounts/Edit.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-38 | Administration/Roles/Index.cshtml.cs | OnGetAsync<br>OnPostAssignAsync | Administration/Roles/Index.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-39 | Administration/Automation/Index.cshtml.cs<br>Administration/Automation/Activity.cshtml.cs | OnGetAsync<br>OnPostSetEnabledAsync<br>OnPostSetSendToAiEnabledAsync<br>OnPostUpdateConnectorAsync<br>OnPostRotateChannelTokenAsync<br>OnPostClearChannelTokenAsync | Administration/Automation/Index.cshtml.cs: AdministrationPageModel<br>Administration/Automation/Activity.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-40 | Administration/Organizations/Index.cshtml.cs<br>Administration/Organizations/Edit.cshtml.cs | OnGetAsync<br>OnPostCreateAsync<br>OnPostUpdateAsync | Administration/Organizations/Index.cshtml.cs: AdministrationPageModel<br>Administration/Organizations/Edit.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-41 | Administration/Principals/Index.cshtml.cs<br>Administration/Principals/Create.cshtml.cs<br>Administration/Principals/Replace.cshtml.cs | OnGetAsync<br>OnPostCreateAsync<br>OnPostReplaceAsync | Administration/Principals/Index.cshtml.cs: AdministrationPageModel<br>Administration/Principals/Create.cshtml.cs: AdministrationPageModel<br>Administration/Principals/Replace.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-42 | Connect/Authorize.cshtml.cs | OnGetAsync<br>OnPostAcceptAsync<br>OnPostDenyAsync | Connect/Authorize.cshtml.cs: AdministrationPageModel | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-43 | Error.cshtml.cs<br>StatusCode.cshtml.cs | OnGet<br>OnGet | Error.cshtml.cs: none<br>StatusCode.cshtml.cs: none | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-44 | cross-cutting policy | - | none | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-45 | Program.cs HTTP routes | MapHealthChecks/MapGet | none | ecb9b7b40c802b5ea800a69a7a46a0875269737a |
| PAR-46 | Mcp/*McpTools.cs | 35 pegasus_* tools | none | ecb9b7b40c802b5ea800a69a7a46a0875269737a |

## Status preservation

Before and after the edit, the matrix contains 21 `inventoried` rows, 23 `not inventoried` rows, and 2 `legacy path retained` rows. No Status cell changed, and no row was advanced by this ticket.

## Verification evidence

- `git diff --check origin/dev...HEAD`: passed.
- `pwsh ./scripts/Test-DocumentationLinks.ps1`: passed (226 files checked).
- `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD`: passed.
- The code enumerations, HTTP-surface search, Triage history check, and raw handler output are recorded in FND-014 scratch.

## Open-question dispositions

U-1 through U-9 are answered above; U-10 is resolved by the documented count correction; U-11 is handed to [[FND-016]] scratch. The parked Connect inheritance, UAT ownership, and matrix-location decisions remain explicitly deferred to their named owners.

## Implications

Dependent parity tickets can use this table as a current static skeleton, but the SHA is a baseline, not runtime proof. Row status remains owned by [[FND-015]] through [[FND-018]], and later upstream synchronization requires re-verification by [[FND-023]] / [[FND-051]].
