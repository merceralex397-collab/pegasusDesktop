# Plan — FEAT-026: Cut-list execution after cutover

**Diff estimate: ~144 files, ~25,400 lines (~25,100 deleted, ~300 added).** Derived from the
measured inventory below: 48 page models (10,472 lines), 63 `.cshtml` (8,511), 3 base/support `.cs`
under `Pages/` (500), up to 6 `Presentation/` view models (up to 772), `site.css` + `site.js`
trimmed rather than deleted (~2,900 of 3,257), and the 9-file browser lane (1,943) **only if
item 4's precondition holds**. Against that, ~300 lines are added or edited across `Program.cs`,
two layouts, one `.csproj`, `Directory.Build.props`, one new architecture-test file and six
documentation files. This is a **deletion** ticket: the added lines are the retained-route facts
and the documentation of record.

**Chore inventory** — profile `chore` owes no `research` or `files` document, so the measured
surface area is stated here (`docs/engineering.md` § plan sizing: the estimate comes from a real
inventory, not an assertion). All measurements at `bbd1c549` (`git rev-parse --short HEAD`,
2026-08-24), with the command that produced each.

| Path | Measured today | Disposition |
| --- | --- | --- |
| `src/Pegasus.Web/Pages/**/*.cshtml.cs` | **53 files, 11,008 lines** (`find … -name "*.cshtml.cs" \| wc -l`; `find … -exec cat {} \; \| wc -l`). *(The ticket body says "~10,800 LOC"; the measured figure at this revision is 11,008.)* | **48 deleted** (10,472 lines); 5 KEEP (536 lines — see below). |
| `src/Pegasus.Web/Pages/**/*.cshtml` | **76 files, 8,941 lines**. 53 are page views; the other **23** are 15 `Shared/` partials, 4 `Cases/Shared/` case partials, 2 `_ViewStart.cshtml`, 1 `_ViewImports.cshtml`, and `Cases/Assessment/Suggestions.cshtml` (a view with no page model). | **63 deleted** (8,511 lines); 5 KEEP views (170) and 8 retained infrastructure files (260). |
| KEEP page models | `Uploads/Request.cshtml.cs` **222**, `Connect/Authorize.cshtml.cs` **177**, `Error.cshtml.cs` **41**, `StatusCode.cshtml.cs` **89**, `Account/AccessDenied.cshtml.cs` **7** — 536 lines (`wc -l`) | Survive. All five exist at this revision. |
| `src/Pegasus.Web/Pages/Shared/` | **15 partials**: `_ErrorSummary`, `_FreshnessBanner`, `_ImageGallery`, `_InstructionDraftFields`, `_Layout` (135), `_LayoutAuth` (28), `_LayoutExternal` (37), `_LucideSprite` (20), `_MetricCard`, `_PageHeader` (24), `_Provenance`, `_ProvenancePanel`, `_ReasonDialog`, `_StatusChip`, `_UploadOutcome` | **10 deleted; 5 retained** — see the KEEP closure below. |
| `src/Pegasus.Web/Pages/Cases/Shared/` | **4 case partials**: `_CaseDocuments`, `_CaseHistory`, `_CaseSummary`, `_CaseWorkflow` | All 4 deleted. |
| `src/Pegasus.Web/wwwroot/css/site.css`, `wwwroot/js/site.js` | **2,471** and **786** lines (`wc -l`) | **Trimmed, not deleted** — all three layouts reference both (see step 6). |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` | **339 lines**; `public abstract partial class CaseMutationPageModel(ILogger logger) : StaffPageModel` at `:18` | Deleted (cut-list item 3). [[FEAT-024]] (plan handle `DSK-05-24`) already stopped the desktop depending on it. |
| Other non-`.cshtml.cs` `.cs` under `Pages/` | 5 files: `Administration/AdministrationPageModel.cs` **7**, `Cases/CaseMutationPageModel.cs` **339**, `EditModeDisplay.cs` **79**, `StaffPageModel.cs` **18**, `UploadConfirmationPageModel.cs` **82** | **3 deleted** (`CaseMutationPageModel`, `EditModeDisplay`, `UploadConfirmationPageModel` = 500 lines); **2 retained** — `AdministrationPageModel` and `StaffPageModel` are load-bearing for a KEEP page (see below). |
| `src/Pegasus.Web/Presentation/` | 8 files, **1,559 lines**: `GalleryImage.cs` 4, `InstructionDraftFieldsView.cs` 64, `MailBodyPresentation.cs` 43, `MailClassificationSelection.cs` 102, `OperatorLabels.cs` 685, `RailCountsPageFilter.cs` 51, `UploadCaseDecision.cs` 306, `UploadOutcome.cs` 304 | Mixed — see the per-file consumer table below. |
| `src/Pegasus.Web/Program.cs` | `RailCountsPageFilter` registered at **`:261`** through `AddRazorPages().AddMvcOptions(…)`, with the reason at `:255-260`; `IUploadOutcomeQueries`→`UploadOutcomeQueries` at **`:608-609`**; `IUploadCaseDecision`→`UploadCaseDecision` at **`:613-614`**; `AddPegasusReportRendering()` at **`:574`** | Registrations for deleted types removed in the same change. `:574` is **out of scope** (item 5). |
| `tests/Pegasus.IntegrationTests/Browser/` | **9 files, 1,943 lines**; **20 `[Fact]` + 1 `[Theory]`** (`AccessibilityTests.cs:47`) — the reuse map's "20 facts" is the `[Fact]` count exactly. Largest: `OperatorJourneyTests.cs` 612, `BrowserTestSupport.cs` 209, `UploadDropzoneBrowserTests.cs` 207 | Deleted **only if** item 4's precondition holds. |
| The Playwright pin | `Directory.Build.props:17` `<PlaywrightVersion>1.61.0</PlaywrightVersion>`, consumed by **two** projects: `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:26` (`Microsoft.Playwright` `PackageReference`) and `src/Pegasus.Web/Pegasus.Web.csproj:28` (`ContainerBaseImage` = `mcr.microsoft.com/playwright/dotnet:v$(PlaywrightVersion)-noble`). The comment at `Pegasus.Web.csproj:20-27` records ADR-0028 / DELIV-012: "the renderer runs in process inside this Web container". | Removed **only if** item 4's precondition holds — and removing the property while `Pegasus.Infrastructure` still references `Microsoft.Playwright` breaks the build. |
| `tests/Pegasus.ArchitectureTests/` | 11 `.cs` files, **62 `[Fact]`**; `DependencyDirectionTests.cs` **520 lines** is the reflection style to extend; `MainBranchHistoryGuardTests.cs` already guards the promotion rule | Gains the retained-route facts (step 10). |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | **46 `PAR-` rows** (`grep -c '^| PAR-'`). Status distribution today: **23 `not inventoried`, 21 `inventoried`, 2 `legacy path retained`** (`PAR-31` at `:76`, `PAR-42` at `:87`) — **0 rows at `designed` or beyond**. Ladder at `:13-23`: `cut over` `:21`, `legacy path retired` `:22`, `legacy path retained` `:23`. | Advanced to `legacy path retired` for every removed row (step 12). |
| `scripts/Test-MainBranchHistory.ps1` | Present; parameters `-Before`, `-Head`, `-ReleaseBranch`, `-RepositoryPath` | Guards the `dev` → `main` promotion at step 13. |

### The KEEP closure — measured, and larger than the five pages

Deleting the five KEEP page models' *neighbours* is not enough: each retained page pulls a
transitive closure of layouts, partials, base classes and assets. Measured:

| Retained page | Layout it actually renders in | Why |
| --- | --- | --- |
| `Pages/Uploads/Request.cshtml` | `Shared/_LayoutExternal` | `Pages/Uploads/_ViewStart.cshtml:6` sets it, with the reason in the comment above: the public upload link "must not render the staff shell". |
| `Pages/Connect/Authorize.cshtml` | **`Shared/_Layout` — the staff shell** | It sets **no** `Layout` and has no local `_ViewStart`, so it falls through to `Pages/_ViewStart.cshtml:2` (`Layout = "_Layout"`). Cut-list item 2 deletes the shell layouts; deleting `_Layout` breaks the OpenIddict consent page. |
| `Pages/Error.cshtml:5`, `Pages/StatusCode.cshtml:5`, `Pages/Account/AccessDenied.cshtml:5` | `Shared/_LayoutAuth` | Each sets it explicitly. |

| Retained non-page file | Pulled in by | Measured reference |
| --- | --- | --- |
| `Shared/_LayoutAuth.cshtml` (28) | Error, StatusCode, AccessDenied | `:5` of each page |
| `Shared/_LayoutExternal.cshtml` (37) | Uploads/Request | `Pages/Uploads/_ViewStart.cshtml:6` |
| `Shared/_Layout.cshtml` (135) | Connect/Authorize | `Pages/_ViewStart.cshtml:2`, by fall-through |
| `Shared/_LucideSprite.cshtml` (20) | **all three layouts** | `_LayoutAuth:19`, `_LayoutExternal:18`, `_Layout:38` |
| `Shared/_PageHeader.cshtml` (24) | Connect/Authorize | `Connect/Authorize.cshtml:8` (`<partial name="Shared/_PageHeader" />`) |
| `wwwroot/css/site.css`, `wwwroot/js/site.js` | **all three layouts** | `_LayoutAuth:16`/`:25`, `_LayoutExternal:15`/`:34`, `_Layout:35`/`:132` |
| `Pages/StaffPageModel.cs` (18) | Connect/Authorize, via its base | provides `TryGetActor`, `NewOperationKey` |
| `Pages/Administration/AdministrationPageModel.cs` (7) | **Connect/Authorize directly** | `Connect/Authorize.cshtml.cs:24` — `public sealed class AuthorizeModel : AdministrationPageModel`. Deleting the `Administration/` folder wholesale takes this file and breaks the retained consent page. |
| `Pages/_ViewStart.cshtml` (3), `Pages/Uploads/_ViewStart.cshtml` (7), `Pages/_ViewImports.cshtml` (6) | Razor infrastructure | required while any Razor page remains |

### `Presentation/` — per-file consumers, measured

`grep -rln "<type>" src/Pegasus.Web --include=*.cs --include=*.cshtml`, excluding the file itself:

| File | Lines | Consumers | Disposition |
| --- | --- | --- | --- |
| `GalleryImage.cs` | 4 | `Cases/Details.cshtml`, `ImageIntake/Details.cshtml`, `Shared/_ImageGallery.cshtml` — all deleted | Delete |
| `InstructionDraftFieldsView.cs` | 64 | `Cases/Create.cshtml.cs`, `Intake/Details.cshtml.cs`, `Shared/_InstructionDraftFields.cshtml` — all deleted | Delete |
| `MailBodyPresentation.cs` | 43 | `Mail/Message.cshtml` — deleted | Delete |
| `MailClassificationSelection.cs` | 102 | `Mcp/MailMcpTools.cs`, `Mail/Index.cshtml.cs`, `Mail/Message.cshtml.cs` | **RETAIN** — `Mcp/` is in the never-cut list; this file survives because the retained MCP tool surface uses it. |
| `UploadCaseDecision.cs` | 306 | `Mail/Message.cshtml.cs`, `UploadConfirmationPageModel.cs`, `UploadGroupStatus.cshtml.cs`, `UploadStatus.cshtml.cs`, **`Program.cs:613-614`** | Delete with its DI registration — after confirming no `/api/v1` or `Mcp/` consumer took a dependency on it. |
| `UploadOutcome.cs` | 304 | `Shared/_UploadOutcome.cshtml`, `UploadGroupStatus.cshtml{,.cs}`, `UploadStatus.cshtml{,.cs}`, **`Program.cs:608-609`** | Same. |
| `RailCountsPageFilter.cs` | 51 | `Program.cs:261` (global page filter) | Delete with its registration (cut-list item 3 names it). |
| `OperatorLabels.cs` | 685 | 24 `.cshtml` + 16 `.cs` today | **Not in this list.** It moved to the shared assembly in [[FEAT-023]] (plan handle `DSK-05-23`) and stays. |

## Approach

Treat the cut list as a **gated, closure-aware deletion**, not a folder removal. Three gates come
first and none of them is this agent's to grant: the operator's Phase 10 approval (step 2), every
replaceable parity row reading `cut over` (step 3), and — for item 4 only — the Playwright
renderer's retirement under ADR-0108 golden-file parity. Then delete by *manifest*, computed from
the measured KEEP closure above rather than from folder names, removing each type's DI registration
in the same change (`docs/engineering.md` § One Core owner: migrate or delete the replaced code,
registrations, tests and documentation in the same slice). Finally make the retention enforceable:
architecture facts that assert the five retained routes still exist, so a later change cannot delete
`Uploads/Request` or `Connect/Authorize` silently.

Rejected: **deleting `src/Pegasus.Web/Pages/` wholesale and restoring what the build complains
about.** The measurements above show why it fails quietly rather than loudly — `Connect/Authorize`
derives from `AdministrationPageModel` and renders in `_Layout`, and all three layouts reference
`site.css`, `site.js` and `_LucideSprite`. A compile error would catch the base class; nothing in
the build catches a missing layout or a missing stylesheet, so the consent page would ship broken
and be discovered by an external MCP connector. Also rejected: **removing the Playwright pin on
this ticket's schedule.** `$(PlaywrightVersion)` is consumed by `Pegasus.Infrastructure`'s package
reference as well as `Pegasus.Web`'s container base image; removing it while the in-process renderer
still exists breaks the build and, worse, would strip Chromium from the production image that
renders reports.

## Governing docs

The ticket's `refs` is **empty** (`get_doc_gates FEAT-026` reports `refs: null`) and
`docs_todo: true`. The New-ADR paragraph alone would give `kanmer-review` nothing to check against
the diff, so the authorities that bind today are tabled below it.

> **New ADR** — ADR-0108 (report rendering in the desktop through an isolated, non-UI WebView2
> HTML→PDF path; the gateway renderer retained until golden-file parity), authored by
> [[FND-007]] (plan handle `DSK-00-07`); see [[FND-007]]'s plan for the ownership reconciliation —
> [[FEAT-038]] (plan handle `DSK-07-12`) is titled "Author ADR-0108" as well, so this ADR has more
> than one claimant and this plan does not assert a single author.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:162`); if the ADR lands
> differently this plan is revised before implementation. ADR-0108 is what gates cut-list item 4:
> until it is accepted and golden-file parity signs off, the browser lane and the Playwright pin
> stay. ADR-0100 (native WinUI 3 client converted inside this fork) and ADR-0103 (gateway =
> `Pegasus.Web` evolved in place, which is why the host survives this ticket) are authored by
> [[FND-005]] (plan handle `DSK-00-05`). ADR-0028 (the in-process renderer and its Container App
> CPU/memory uplift) already exists and is **not** re-authored here — its reversal is item 5's, and
> item 5 belongs to [[PLAT-026]] (plan handle `DSK-11-08`).

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 24 Phase 10 | Code and infrastructure dependencies are removed only after the mandatory production desktop release, a monitored business cycle and explicit approval | Step 2 |
| Proposal § 19.2 Deprovisioning method after cutover | Nothing is deprovisioned before cutover, observed use and rollback approval | Step 2, Step 9 |
| `reuse-map.md` § `Cut list after cutover (Phase 10 only)` | The five numbered items, removed "only after the parity matrix shows every row at `cut over`, the rollback window has expired and the operator has approved" | Steps 3–9 |
| `reuse-map.md` § `Never cut before parity` | `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Worker`, migrations; Identity, OpenIddict, MCP ingress, rate limiting, health endpoints; any page whose row is not `cut over`; the web-only KEEP rows; Azure resources | Step 4's KEEP manifest, Step 10's facts, § Verification's `git diff --stat` check |
| `endpoint-map.md` § `Stays web-only (not projected)` | Five retained pages with a stated reason each | Step 4 |
| `docs/engineering.md` § One Core owner | Migrate or delete the replaced code, **registrations**, tests and documentation in the same slice | Steps 5, 7 |
| `docs/engineering.md` § Required evidence tiers (1, 5) | Tier 1 obliges compiling the approved projects and enforcing dependency direction and one policy owner after the removals; tier 5 obliges observable evidence that the retained routes and `/api/v1` still reach Core with authentication and validation intact | Steps 10–11, § Verification |
| L-01 | The gateway host stays; only the Razor staff surface goes | § Approach; Step 4's KEEP manifest |
| L-03 | The gateway renderer is retired only once ADR-0108 golden-file parity is signed off | Step 8's precondition |
| D-001 | The fork is the single release source by this point | Step 13's promotion |
| L-04 | Routing named on the ticket | § Routing |
| `AGENTS.md` § Repository task workflow (`:305-310`) | A `dev` → `main` release is an exact-SHA, non-force promotion needing an explicit `MERGE AUTH GRANTED` immediately before the `main` update; a GitHub merge is not a promotion | Step 13 |
| `scripts/Test-MainBranchHistory.ps1` | A push to `main` whose history is not contained in `dev` fails | Step 13 |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | A bare `<PREFIX>-<nnn>` is a fork board id; an upstream id is written `upstream <ID>` | Step 9's hand-off to [[PLAT-026]] |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (performs the
  removals in `Pegasus.Web`); `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml` (independent review that nothing retained was
  removed)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`,
  `plugins/dotnet-test/skills/run-tests/SKILL.md`) → `pegasus-release`
  (`.agents/skills/pegasus-release/SKILL.md`) for the release notes
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` →
  `kanmer-closeout` (call `get_doc_gates <id>` before every move; `chore` needs `plan` and
  `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refining the ticket body's thirteen steps — same order, same ownership, same file paths. Body step
numbers in brackets.

1. **[body 1] Orient and take.** Read the plan row, `reuse-map.md` § `Cut list after cutover
   (Phase 10 only)` (`:117-135`) and § `Never cut before parity` (`:136`ff), and the endpoint map's
   § `Stays web-only (not projected)` table (`endpoint-map.md:134`ff). Call `get_doc_gates
   FEAT-026`, then `take_ticket` with branch `task/dsk-05-26-cut-list` and worktree
   `../pegasus-worktrees/dsk-05-26-cut-list` from `origin/dev`.
2. **[body 2] Operator step — the three Phase 10 preconditions, recorded before anything is
   deleted.** (a) The mandatory production desktop release has shipped; (b) at least one complete
   business cycle has been monitored; (c) the operator has given explicit cutover approval **with a
   date**. Paste the approval text verbatim into the ticket proof. **Without all three the ticket
   stays in Preparing** — this is a gate, not a formality, and it is the operator's to grant, never
   an agent's to infer from a green build.
3. **[body 3] Verify the parity matrix.** From [[FEAT-025]] (plan handle `DSK-05-25`): every
   replaceable row reads `cut over` (ladder `parity-matrix.md:21` — "Desktop is the path in use; web
   path disabled in the Test/UAT stack") and the deliberate exceptions read `legacy path retained`
   (`:23`). The starting state measured at `bbd1c549` is **23 `not inventoried`, 21 `inventoried`,
   2 `legacy path retained`, 0 at `designed` or beyond** across 46 rows — so at the time of writing
   *no* row authorises a deletion. A row still at `UAT passed` or below **blocks removal of the page
   it names**: list any such row and stop rather than removing it. Confirm with [[FEAT-025]] that
   the `cut over` reading is the one its maintenance rule enforces.
4. **[body 4] Build the removal manifest in this plan**, one line per file, grouped by cut-list
   item, each with the matrix row that authorises it. Compute it from the **KEEP closure measured
   above**, not from folder names. The KEEP set that must survive, in full:
   - **Pages** — `Pages/Uploads/Request.cshtml{,.cs}`, `Pages/Connect/Authorize.cshtml{,.cs}`,
     `Pages/Error.cshtml{,.cs}`, `Pages/StatusCode.cshtml{,.cs}`,
     `Pages/Account/AccessDenied.cshtml{,.cs}`.
     *(Note the discrepancy and take the body's reading: `reuse-map.md:124-125` names only four KEEP
     pages, omitting `Account/AccessDenied`; the endpoint map's web-only table and this ticket's
     body both name five. Five is the KEEP set; record the reuse-map omission as a documentation
     correction at step 12.)*
   - **Base classes** — `Pages/StaffPageModel.cs` and `Pages/Administration/AdministrationPageModel.cs`,
     because `Connect/Authorize.cshtml.cs:24` derives from the latter.
   - **Razor infrastructure** — `Pages/_ViewStart.cshtml`, `Pages/Uploads/_ViewStart.cshtml`,
     `Pages/_ViewImports.cshtml`.
   - **Layouts and partials** — `Shared/_LayoutAuth`, `Shared/_LayoutExternal`, `Shared/_LucideSprite`,
     `Shared/_PageHeader`, and `Shared/_Layout` unless step 6 re-points `Connect/Authorize`.
   - **Elsewhere in `Pegasus.Web`** — the whole `Mcp/` folder, `Authentication/`, rate limiting,
     `Health/`, `/diagnostics/version`, and `Presentation/MailClassificationSelection.cs` (used by
     `Mcp/MailMcpTools.cs`).
   - **Outside `Pegasus.Web`** — `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Worker`,
     migrations, Identity, OpenIddict.
5. **[body 5] Cut-list item 1 — the staff Razor surface.** Delete the **48** non-KEEP page models
   and their `.cshtml`, `Cases/Assessment/Suggestions.cshtml`, the **4** case partials in
   `Pages/Cases/Shared/`, and the **10** non-retained `Shared/` partials (`_ErrorSummary`,
   `_FreshnessBanner`, `_ImageGallery`, `_InstructionDraftFields`, `_MetricCard`, `_Provenance`,
   `_ProvenancePanel`, `_ReasonDialog`, `_StatusChip`, `_UploadOutcome`). Delete
   `Pages/EditModeDisplay.cs` (79) and `Pages/UploadConfirmationPageModel.cs` (82) with them.
   **Remove every now-unused DI registration in `src/Pegasus.Web/Program.cs` in the same change** —
   a registration for a deleted type is a defect. Known ones: the `RailCountsPageFilter` global
   filter at `:261` (with its comment at `:255-260`), `IUploadOutcomeQueries` at `:608-609` and
   `IUploadCaseDecision` at `:613-614`. `AddRazorPages()` itself stays: five Razor pages remain.
6. **[body 6] Cut-list item 2 — assets and layouts, reduced rather than removed.** The body says
   "keeping whatever the retained pages still need", and the measurement shows that is most of it:
   all three layouts reference `~/css/site.css`, `~/js/site.js` and `Shared/_LucideSprite`. So:
   - Keep `_LayoutAuth` (28) and `_LayoutExternal` (37), and keep `_LucideSprite` (20) and
     `_PageHeader` (24) which they and `Connect/Authorize.cshtml:8` pull in.
   - Decide `_Layout` (135) explicitly: **either** keep it for `Connect/Authorize`, **or** give
     `Connect/Authorize.cshtml` an explicit `Layout` and change `Pages/_ViewStart.cshtml:2` so no
     page silently inherits a deleted shell. Record which was chosen — a silent fall-through to a
     deleted layout is a runtime failure the build will not catch.
   - **Trim** `site.css` (2,471) and `site.js` (786) to what the retained layouts and five pages
     actually use rather than deleting them; deleting them leaves every retained page unstyled.
   - Verify by **loading each of the five KEEP pages** after the removal, as the body requires.
7. **[body 7] Cut-list item 3 — the mutation state machine and the view models.** Delete
   `Pages/Cases/CaseMutationPageModel.cs` (339) and `Presentation/RailCountsPageFilter.cs` (51)
   with its `Program.cs:261` registration. For `Presentation/*View.cs`: **the glob matches exactly
   one file** at this revision — `InstructionDraftFieldsView.cs` (64) — so apply the body's real
   rule, "no longer referenced", to the whole folder using the consumer table above. Delete
   `GalleryImage.cs` (4), `InstructionDraftFieldsView.cs` (64) and `MailBodyPresentation.cs` (43);
   delete `UploadCaseDecision.cs` (306) and `UploadOutcome.cs` (304) **after confirming** no
   `/api/v1` group or `Mcp/` tool took a dependency on them. **Retain
   `MailClassificationSelection.cs` (102)** — `Mcp/MailMcpTools.cs` uses it. `OperatorLabels.cs`
   is **not** in this list: it moved to the shared assembly in [[FEAT-023]] and stays.
8. **[body 8] Cut-list item 4 — the browser lane and the Playwright pin, both conditional.**
   Delete `tests/Pegasus.IntegrationTests/Browser/` (9 files, 1,943 lines, 20 `[Fact]` +
   1 `[Theory]`) and remove the pin from `src/Pegasus.Web/Pegasus.Web.csproj:20-28` and
   `Directory.Build.props:10-17` **only if** the Playwright renderer has also been retired under
   ADR-0108 golden-file parity ([[FEAT-018]], plan handle `DSK-05-18`). Note the coupling the body
   does not spell out: `$(PlaywrightVersion)` is consumed by
   `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:26` as well, so the property cannot be
   removed while the in-process renderer exists — and `Pegasus.Web.csproj:28` is the **production**
   container base image, not a test-only setting. **If the renderer is still retained, keep both and
   record why in this plan and in the proof.**
9. **[body 9] Cut-list item 5 is out of scope — hand it over, do not act.** The
   `AddPegasusReportRendering()` registration (`src/Pegasus.Web/Program.cs:574`, defined at
   `src/Pegasus.Infrastructure/DependencyInjection.cs:446`) and the Container App CPU/memory uplift
   reversal (ADR-0028) are an ⚠ Azure setting change owned by [[PLAT-026]] (plan handle
   `DSK-11-08`, "Post-cutover deprovision checklist, prepared and not executed"). Raise or update
   that ticket. **No Azure write on this branch.**
10. **[body 10] Make the retention enforceable.** Extend `tests/Pegasus.ArchitectureTests` (62
    facts today, reflection style of `DependencyDirectionTests.cs`, 520 lines) with facts asserting
    that the five retained web-only routes still exist — so a future change cannot delete
    `Uploads/Request` or `Connect/Authorize` silently. Assert the **routes**, and additionally
    assert that `AuthorizeModel`'s base type resolves, since that is the dependency a folder-level
    deletion breaks. Each fact fails with a message naming what is missing and pointing at this
    ticket.
11. **[body 11] Build and test with the deletions in place.** Every remaining test must pass
    **without an assertion being edited to accommodate a removal**. An edited assertion means a
    behaviour changed, not a file disappeared — stop and investigate. Web tests that exercised a
    deleted page are deleted with it; that is a removal, not an edit, and the two must be
    distinguishable in the diff.
12. **[body 12] Documentation of record.** Update `docs/current-architecture.md` (remove the retired
    Razor surface from the implementation map), `docs/boundaries.md` (the web front end's code-side
    removal is executed), `docs/operations.md` and the release notes through the `pegasus-release`
    skill, mark the executed items in `docs/desktop/05-implementation-and-migration/reuse-map.md`
    — including the step-4 correction that the KEEP set is **five** pages, not four — and advance
    the removed rows in `docs/desktop/01-inventory-and-parity/parity-matrix.md` to
    `legacy path retired` (`:22`). `PAR-31` (`:76`) and `PAR-42` (`:87`) stay
    `legacy path retained` and must **never** read `cut over`.
13. **[body 13] Simplify, PR, promote.** Run the simplification pass over the branch diff and record
    it under a dated `## Simplification pass` heading. Open the PR into `dev`. Promotion to `main`
    is an **exact-SHA, non-force** promotion requiring the operator's literal `MERGE AUTH GRANTED`
    immediately before the `main` update (`AGENTS.md:305-310`) — a GitHub merge is not a promotion,
    and `scripts/Test-MainBranchHistory.ps1` fails a push whose history is not contained in `dev`.

## Verification

Evidence tiers from the body: **1** (Static/build/architecture) and **5** (Web/API/MCP caller).

- `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — succeeds with
  `TreatWarningsAsErrors=true` (`Directory.Build.props:8`) and no unresolved reference to a deleted
  type.
- `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"` — the
  full suite passes; the browser lane's 20 facts are absent **only if** item 4 applied, and present
  and green otherwise.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`
  — the new retained-route facts pass and the existing 62 facts stay green.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` — passes after the documentation updates.
- `git diff --stat` — **no file under `src/Pegasus.Core/`, `src/Pegasus.Infrastructure/`,
  `src/Pegasus.Worker/`, `src/Pegasus.Web/Mcp/`, `src/Pegasus.Web/Authentication/` or
  `src/Pegasus.Web/Health/`**, and no Azure artefact touched.
- **Manual check recorded in the proof** — each of the five retained pages loads correctly on the
  deployed gateway after promotion: the request-link upload page in `_LayoutExternal`, the MCP
  consent page in whichever layout step 6 chose, and the error, status-code and access-denied pages
  in `_LayoutAuth`. This is the tier-5 obligation and the only check that catches a missing layout
  or a trimmed-away stylesheet.

Evidence that becomes `proof`: the operator's Phase 10 approval text and date (step 2), the parity
rows authorising each deletion (step 3), the removal manifest, the build and three test outputs, the
`git diff --stat`, the link-check output, and the five-page load record.

## Risks / open questions

- **A folder-level deletion breaks the retained OpenIddict consent page, and the build will only
  half-catch it.** `Connect/Authorize.cshtml.cs:24` derives from
  `Pages/Administration/AdministrationPageModel.cs`, which sits inside the folder item 1 deletes;
  and the page renders in `_Layout` by fall-through from `Pages/_ViewStart.cshtml:2`, which nothing
  in the build validates. Mitigation: step 4's KEEP closure names both, step 6 forces an explicit
  layout decision, and step 10's fact asserts the base type resolves.
- **Deleting `site.css`, `site.js` and `_LucideSprite` literally would leave every retained page
  unstyled.** All three layouts reference all three. Mitigation: step 6 trims rather than deletes,
  and the tier-5 manual check is the only thing that proves it.
- **The Playwright pin is production infrastructure, not a test setting.**
  `Pegasus.Web.csproj:28` sets the container base image and `Pegasus.Infrastructure.csproj:26`
  consumes the same property. Mitigation: step 8's precondition; the owner of the renderer's
  retirement is [[FEAT-018]] (plan handle `DSK-05-18`) under ADR-0108, whose authorship is
  reconciled in [[FND-007]]'s plan. A scope boundary, not an open question.
- **Cut-list item 5 is an Azure change and is not this ticket's.** Owner: [[PLAT-026]] (plan handle
  `DSK-11-08`). Mitigation: step 9 hands it over and § Verification's `git diff --stat` proves
  nothing Azure-adjacent moved. Note the namespace: board `PLAT-026` is the seeded `DSK-11-08`
  ticket, not upstream `PLAT-026`; the join table is in the `HZN-001` group document
  `board-conventions.md`.
- **The Phase 10 approval is an operator gate with no ticket.** Mitigation: step 2 records all
  three preconditions with the approval text and date, and the ticket stays in Preparing without
  them. It is not an open question — it is a decision the operator takes at the gate, and inventing
  an `open-questions` entry for it would block the ticket on something no agent can answer.
- **No parity row authorises a deletion today.** Measured: 0 of 46 rows at `designed` or beyond.
  Mitigation: step 3 is a hard precondition; this ticket cannot begin until [[FEAT-025]] has driven
  the rows to `cut over`. Running it early would delete pages whose desktop replacement is not in
  use.
- **`UploadOutcome` and `UploadCaseDecision` have a `Program.cs` consumer as well as page-model
  consumers.** Mitigation: step 7 confirms no `/api/v1` or `Mcp/` dependency before deleting them;
  if one exists they are retained like `MailClassificationSelection.cs` and the manifest records
  why.
- **The reuse map's KEEP list is one page short of the endpoint map's.** `reuse-map.md:124-125`
  omits `Account/AccessDenied`. Mitigation: step 4 takes the body's five-page reading and step 12
  corrects the reuse map. Not a body error — the body and the endpoint map agree.
- **The matrix ladder's `implemented` reads "Native code exists on a branch"
  (`parity-matrix.md:18`), while [[FEAT-025]]'s maintenance rule requires a merge.** This ticket
  gates on `cut over` (`:21`), which is unambiguous, so the difference does not affect it —
  but step 3 confirms the reading with [[FEAT-025]] rather than assuming it.
- **An assertion edited to make a deletion pass hides a behaviour change.** Mitigation: step 11
  states the rule and the reviewer diffs deleted test files against edited ones — they must be
  distinguishable.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
