FND-014 raw handler enumeration at HEAD ecb9b7b40c802b5ea800a69a7a46a0875269737a (136 declaration lines):

```text
src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs:53:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs:59:    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:44:    public IActionResult OnGet()
src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:54:    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Account/SignOut.cshtml.cs:10:    public IActionResult OnGet() => RedirectToPage("/Index");
src/Pegasus.Web/Pages/Account/SignOut.cshtml.cs:12:    public async Task<IActionResult> OnPostAsync()
src/Pegasus.Web/Pages/Administration/Access/Index.cshtml.cs:26:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Access/Index.cshtml.cs:37:    public async Task<IActionResult> OnPostReviewAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Accounts/Edit.cshtml.cs:22:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Administration/Accounts/Edit.cshtml.cs:34:    public async Task<IActionResult> OnPostDisableAsync(
src/Pegasus.Web/Pages/Administration/Accounts/Index.cshtml.cs:32:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Accounts/Index.cshtml.cs:43:    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Automation/Activity.cshtml.cs:23:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Administration/Automation/Index.cshtml.cs:45:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Automation/Index.cshtml.cs:57:    public async Task<IActionResult> OnPostSetEnabledAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Automation/Index.cshtml.cs:95:    public async Task<IActionResult> OnPostSetSendToAiEnabledAsync(
src/Pegasus.Web/Pages/Administration/Automation/Index.cshtml.cs:128:    public async Task<IActionResult> OnPostUpdateConnectorAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Automation/Index.cshtml.cs:168:    public async Task<IActionResult> OnPostRotateChannelTokenAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Automation/Index.cshtml.cs:207:    public async Task<IActionResult> OnPostClearChannelTokenAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Configuration.cshtml.cs:40:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Configuration.cshtml.cs:52:    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Index.cshtml.cs:22:    public IActionResult OnGet()
src/Pegasus.Web/Pages/Administration/MailCategories.cshtml.cs:24:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/MailCategories.cshtml.cs:32:    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Mailboxes.cshtml.cs:45:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Mailboxes.cshtml.cs:58:    public async Task<IActionResult> OnPostUpdateAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Mailboxes.cshtml.cs:167:    public async Task<IActionResult> OnPostResolveFoldersAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Organizations/Edit.cshtml.cs:33:    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Organizations/Edit.cshtml.cs:45:    public async Task<IActionResult> OnPostUpdateAsync(
src/Pegasus.Web/Pages/Administration/Organizations/Index.cshtml.cs:34:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Organizations/Index.cshtml.cs:46:    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Principals/Create.cshtml.cs:32:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Principals/Create.cshtml.cs:43:    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Principals/Index.cshtml.cs:18:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Principals/Replace.cshtml.cs:38:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Administration/Principals/Replace.cshtml.cs:58:    public async Task<IActionResult> OnPostReplaceAsync(
src/Pegasus.Web/Pages/Administration/Roles/Index.cshtml.cs:48:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Administration/Roles/Index.cshtml.cs:59:    public async Task<IActionResult> OnPostAssignAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:184:    public async Task<IActionResult> OnPostSaveDamageAsync(
src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:246:    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:277:    public async Task<IActionResult> OnPostGenerateReportDraftAsync(
src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:330:    public async Task<IActionResult> OnPostImportEstimateAsync(
src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:476:    public async Task<IActionResult> OnPostAcceptSpecificationAsync(
src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:583:    public async Task<IActionResult> OnPostSendAsync(
src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:628:    public async Task<IActionResult> OnPostReconcileAsync(
src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs:23:    public Task<IActionResult> OnPostRecordReportApprovalAsync(
src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs:52:    public Task<IActionResult> OnPostCloseAsync(
src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs:69:    public Task<IActionResult> OnPostReopenAsync(
src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs:106:    public Task<IActionResult> OnPostArchiveAsync(
src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:210:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:266:    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken = default)
src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:28:    public async Task<IActionResult> OnPostRetryCustodyAsync(
src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:74:    public async Task<IActionResult> OnPostUploadDocumentAsync(
src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:138:    public Task<IActionResult> OnPostRemoveDocumentAsync(
src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:162:    public Task<IActionResult> OnPostConfirmThirdPartyVehicleEvidenceAsync(
src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:186:    public async Task<IActionResult> OnPostCreateRequestUploadLinkAsync(
src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:237:    public Task<IActionResult> OnPostRevokeRequestUploadLinkAsync(
src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:110:    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:156:    public async Task<IActionResult> OnPostClaimLeaseAsync(
src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:203:    public async Task<IActionResult> OnPostRenewLeaseAsync(
src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:250:    public async Task<IActionResult> OnPostReleaseLeaseAsync(
src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:293:    public Task<IActionResult> OnPostConfirmCompletenessAsync(
src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:324:    public Task<IActionResult> OnPostSaveAsync(
src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs:16:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Cases/Documents/Export.cshtml.cs:18:    public async Task<IActionResult> OnPostAsync(
src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs:21:    public async Task<IActionResult> OnPostAsync(
src/Pegasus.Web/Pages/Cases/Index.cshtml.cs:71:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:33:    public async Task<IActionResult> OnPostAddNoteAsync(
src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:61:    public Task<IActionResult> OnPostCreateTaskAsync(
src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:89:    public Task<IActionResult> OnPostAssignTaskAsync(
src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:117:    public Task<IActionResult> OnPostCompleteTaskAsync(
src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:143:    public Task<IActionResult> OnPostCancelTaskAsync(
src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:169:    public Task<IActionResult> OnPostRecordManualChaseAsync(
src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:201:    public Task<IActionResult> OnPostLinkReportEvidenceAsync(
src/Pegasus.Web/Pages/Cases/Tasks.cshtml.cs:225:    public Task<IActionResult> OnPostUnlinkReportEvidenceAsync(
src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs:24:    public Task<IActionResult> OnPostRequestVehicleLookupAsync(
src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs:46:    public Task<IActionResult> OnPostAcceptVehicleSuggestionAsync(
src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs:87:    public async Task<IActionResult> OnPostGenerateEvaHandoffAsync(
src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs:26:    public Task<IActionResult> OnPostHoldAsync(
src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs:42:    public Task<IActionResult> OnPostReleaseHoldAsync(
src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs:64:    public Task<IActionResult> OnPostReturnToReviewAsync(
src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs:98:    public Task<IActionResult> OnPostAssignEngineerAsync(
src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs:133:    public Task<IActionResult> OnPostStartWorkAsync(
src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs:156:    public Task<IActionResult> OnPostRecordEngineerFindingAsync(
src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs:180:    public async Task<IActionResult> OnPostCreateLinkedReplacementAsync(
src/Pegasus.Web/Pages/Connect/Authorize.cshtml.cs:46:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Connect/Authorize.cshtml.cs:80:    public async Task<IActionResult> OnPostAcceptAsync(
src/Pegasus.Web/Pages/Connect/Authorize.cshtml.cs:130:    public async Task<IActionResult> OnPostDenyAsync(
src/Pegasus.Web/Pages/Error.cshtml.cs:29:    public void OnGet()
src/Pegasus.Web/Pages/ImageIntake/Details.cshtml.cs:26:    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
src/Pegasus.Web/Pages/ImageIntake/Details.cshtml.cs:48:    public async Task<IActionResult> OnPostCloseAsync(
src/Pegasus.Web/Pages/ImageIntake/Index.cshtml.cs:27:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Index.cshtml.cs:27:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Intake/Asset.cshtml.cs:20:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:95:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:111:    public async Task<IActionResult> OnPostRetryAllocationAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:157:    public async Task<IActionResult> OnPostBlockAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:178:    public async Task<IActionResult> OnPostReevaluateAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:192:    public async Task<IActionResult> OnPostCorrectDraftAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:240:    public async Task<IActionResult> OnPostClaimCaseLeaseAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:274:    public async Task<IActionResult> OnPostLinkCaseAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:310:    public async Task<IActionResult> OnPostReverseCaseLinkAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:513:    public async Task<IActionResult> OnPostRegisterImageIntakeAsync(
src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:535:    public async Task<IActionResult> OnPostDismissSuggestionAsync(
src/Pegasus.Web/Pages/Intake/Image.cshtml.cs:20:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Intake/Source.cshtml.cs:11:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:69:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:158:    public async Task<IActionResult> OnGetPreviewAsync(
src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:157:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:199:    public async Task<IActionResult> OnPostPrepareLinkCaseAsync(
src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:260:    public async Task<IActionResult> OnPostPrepareUnlinkCaseAsync(
src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:318:    public async Task<IActionResult> OnPostLinkCaseAsync(
src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:383:    public async Task<IActionResult> OnPostUnlinkCaseAsync(
src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:448:    public async Task<IActionResult> OnPostCorrectClassificationAsync(
src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:511:    public async Task<IActionResult> OnPostMoveToRecommendedFolderAsync(
src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:57:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:71:    public async Task<IActionResult> OnPostRetryExternalAsync(
src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:112:    public async Task<IActionResult> OnPostRevokeLinkAsync(
src/Pegasus.Web/Pages/Search/Index.cshtml.cs:27:    public IActionResult OnGet() =>
src/Pegasus.Web/Pages/StatusCode.cshtml.cs:38:    public void OnGet(int code)
src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:56:    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:85:    public async Task<IActionResult> OnPostActionAsync(
src/Pegasus.Web/Pages/Triage/Index.cshtml.cs:199:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Unidentified/Details.cshtml.cs:88:    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Unidentified/Details.cshtml.cs:93:    public async Task<IActionResult> OnPostResolveAsync(Guid id, CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Unidentified/Index.cshtml.cs:17:    public IActionResult OnGet() =>
src/Pegasus.Web/Pages/Upload.cshtml.cs:43:    public void OnGet()
src/Pegasus.Web/Pages/Upload.cshtml.cs:48:    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/UploadConfirmationPageModel.cs:21:    public async Task<IActionResult> OnGetCaseSearchAsync(
src/Pegasus.Web/Pages/UploadConfirmationPageModel.cs:47:    public async Task<IActionResult> OnPostAttachAsync(
src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs:61:    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken) =>
src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs:64:    public async Task<IActionResult> OnPostRegisterGroupAsync(
src/Pegasus.Web/Pages/UploadGroupStatus.cshtml.cs:130:    public async Task<IActionResult> OnPostAttachGroupAsync(
src/Pegasus.Web/Pages/UploadStatus.cshtml.cs:56:    public async Task<IActionResult> OnGetAsync(
src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs:31:    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
src/Pegasus.Web/Pages/Uploads/Request.cshtml.cs:52:    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)

```

The exact command was `git grep -n "public .*On\\(Get\\|Post\\)[A-Za-z]*" -- 'src/Pegasus.Web/Pages'`.

Research and matrix reconciliation completed on branch fnd-014-parity-inventory at HEAD ecb9b7b40c802b5ea800a69a7a46a0875269737a. Counts: 53 page models, 76 views, 136 handler declaration lines; difference lists A/B/C empty after full Administration prefixes were restored in PAR-37/39/40/41. Base classes: 18 Staff page models, 16 Administration, 8 CaseMutation, 2 UploadConfirmation, 9 deriving from none. PAR-24 now records 12 named Triage commands; git log -p --reverse 191ddf33..HEAD for Triage Details returned no commits. Non-Razor search found only known Program.cs health/version, MCP token, and MapMcp registrations; MCP tool enumeration is 35 distinct pegasus_* names (42 McpServerTool matches includes seven type attributes). U-11 was handed to FND-016 scratch; U-10 was resolved by correcting the README count from 7 to 8 in this same authorized documentation change. Status cells remained 21 inventoried, 23 not inventoried, 2 legacy path retained. Validation: pwsh ./scripts/Test-DocumentationLinks.ps1 passed, 226 files checked; git diff --check passed. Markdown placement will be rerun after commit.

Independent read-only parity audit confirmed: at HEAD, before working-tree fixes the five missing Administration prefixes were exactly Accounts/Edit, Automation/Activity, Organizations/Edit, Principals/Create, Principals/Replace; after fixes, page-model paths reconcile 53/53 with no nonexistent citations. It also independently caught PAR-43 omitting OnGet for Error and StatusCode; those handlers are now recorded. Triage file blobs at 191ddf33 and HEAD are identical, with the same 12 named cases plus default, and no commits in the range.

Simplification pass: 2026-08-25 — n/a — docs-only. The branch changes only the parity matrix and its area README; no code, abstraction, dependency, or architecture was introduced. No behaviour-preserving simplification finding applies.

Committed repository changes as 83e945c9 (`docs: reconcile desktop parity inventory`). Post-commit validation: `pwsh ./scripts/Test-DocumentationLinks.ps1` passed (226 files); `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` passed; `git diff --check origin/dev...HEAD` passed.

Independent review preparation caught and the implementation fixed one Markdown table defect: after adding the `Inventoried at` column, the separator row had 10 cells for an 11-column header. Added the missing separator cell and amended the commit to e5da09b3. Revalidation passed: header/separator both 11 columns; `pwsh ./scripts/Test-DocumentationLinks.ps1` (226 files), `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD`, and `git diff --check origin/dev...HEAD` all exit 0. The prior reviewer process was stopped because its target commit was amended; a fresh independent review targets e5da09b3.

Review correction (2026-08-25): the independent reviewer identified that the 136 raw declarations include 134 declarations in the 53 `*.cshtml.cs` page-model files plus two shared declarations in `UploadConfirmationPageModel.cs`. Updated the authorized parity matrix and research handoff to represent `OnGetCaseSearchAsync` and `OnPostAttachAsync` once in PAR-29, with explicit inheritance called out in PAR-30. Amended commit is `f6572913`. Validation after correction: `git diff --check origin/dev...HEAD` exit 0; `pwsh ./scripts/Test-DocumentationLinks.ps1` exit 0, 226 files; `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` exit 0; page models 53, views 76, raw handler declarations 136.
