# Checklist — FND-007

## 2026-08-25 correction — documented invisible host

Microsoft Learn documents `HWND_MESSAGE` as the valid parent for an invisible `CoreWebView2Controller` on Windows 8 and later; the WebView will never become visible. The fixed design is `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`. This supersedes every earlier collapsed-XAML/hidden-HWND host-selection instruction below. Phase 7 validates packaged-app initialisation, PDF output and no-window behaviour; it does not select a host. This user-directed correction also adds `docs/desktop/00-governance-and-workflow/README.md` and `docs/desktop/07-integrations/README.md` to FND-007's docs-only scope.


One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")` as you go; append progress notes below rather
than rewriting.

- [x] Read the plan row, `docs/desktop/00-governance-and-workflow/README.md` § 3's ADR-0108 line, `docs/desktop/07-integrations/README.md:227-231` and `:251-259`, and the two renderer ADRs `docs/adr/0025-*.md` and `docs/adr/0028-*.md`
- [x] Call `get_doc_gates FND-007`, confirm `leave-backlog: [governing-doc]` is satisfied by `docs_todo`, then `take_ticket` with a real branch and worktree cut from `fnd-005-foundation-adrs` at `d22c39dd` because `origin/dev` is absent
- [x] Run `ls docs/adr/010*` **before writing** and record the output (expected: `No such file or directory`; a file appearing means someone else wrote this ADR)
- [x] `microsoft_docs_search` for `CoreWebView2.PrintToPdfStreamAsync`, and again for `CoreWebView2Controller` hosting on a window handle; `microsoft_docs_fetch` the WebView2 print how-to
- [x] Create `docs/adr/0108-desktop-webview2-report-rendering.md` with the eight-key frontmatter, `status: proposed`, `supersedes: []`, `superseded_by: []`, and reviewer-normalised `related_frd: [frd-11]`
- [x] Record the reviewer-driven normalisation to `related_frd: [frd-11]`, matching ADR-0025, ADR-0026, and ADR-0028; commit `d3762780` contains the one-token correction
- [x] Use the heading set `## Status · ## Context · ## Decision · ## Consequences · ## Reversal condition · ## Options considered · ## Links`, Status first, following `docs/adr/0029-*.md:11-20`
- [x] In `## Status`, state `proposed` and what would change it, in the form `docs/adr/0028-*.md:13-16` uses
- [x] In `## Context`, state L-03
- [x] In `## Context`, quote proposal § 23.2 `:1715` ("…requires an ADR and must not host Pegasus UI") and § 2.1 `:1351` ("no WebView shell") **verbatim**, and argue why the exception does not swallow the rule
- [x] In `## Context`, record the retained gateway renderer of ADR-0025/ADR-0028 and what central rendering costs the Web image (`docs/adr/0028-*.md:22-27`)
- [x] In `## Context`, fill **every cell** of the six-question cloud-justification table for report rendering, transcribed from this ticket's `research` document and re-verified; answer "measured operational advantage" as **no, and not yet measured**
- [x] In `## Decision`, record what is decided now: `Pegasus.Desktop.Infrastructure` behind the **existing** `IAssessmentReportRenderer` port (`src/Pegasus.Core/Reports/AssessmentReportRendering.cs:284`), the same governed Scriban templates embedded by [[FEAT-039]], isolated/non-UI/never-visible/single-flight, and the runtime-missing named failure with gateway fallback
- [x] In `## Decision`, state that the desktop **produces the document bytes but never registers the report** — registration, custody and audit stay behind the gateway ([[FEAT-042]])
- [x] In `## Decision`, record the documented `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)` host and make [[FEAT-040]] responsible only for packaged-app validation
- [x] In `## Consequences`, state the retention gate with its owner: the gateway renderer stays until [[FEAT-041]]'s golden-file parity passes on approved fixtures, after which no required report may depend on the web renderer unless a superseding ADR says so
- [x] In `## Consequences`, state that parity is tolerant and not pixel-equal (WebView2 Chromium self-updates; Playwright is pinned to 1.61.0), and that acceptance proves architecture only
- [x] Write `## Reversal condition`: WebView2 runtime absent or unmaintainable across the fleet; packaged-app failure of the documented `HWND_MESSAGE` controller; or unclosable golden-file divergence
- [x] Confirm **no** row was added to `docs/adr/README.md`, and that `AGENTS.md` was not edited
- [x] Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both exit 0, with the Microsoft Learn URLs and their fetch date **outside** any fenced block
- [x] Confirm `git diff --stat` shows 1 file changed and **0 deletions**, and that `git diff --stat -- docs/adr/0025-*.md docs/adr/0028-*.md docs/adr/README.md AGENTS.md` is empty
- [ ] Open the PR against `dev` with `gh pr create --base dev` and merge on the independent review from `pegasus-desktop-reviewer`, whose named judgement is that the renderer is not a web shell
- [ ] After [[FEAT-040]] and [[FEAT-041]] are done, verify [[FEAT-038]]'s frontmatter-only acceptance PR against the reversal condition and the § 23.2 statement — making **no** edit to ADR-0108 from this ticket
- [ ] Verification run: record the proof as the two PR references plus the golden-file test output cited from [[FEAT-041]], together with the command table from the plan's `## Verification` section

- [x] Apply the 2026-08-25 user-directed correction: update ADR-0108, the Phase 0/7 source plans, and dependent ticket documents from host selection to the documented `HWND_MESSAGE` controller.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)

2026-08-24 — Implemented the planned Phase 0 ADR as the only staged repository file: `docs/adr/0108-desktop-webview2-report-rendering.md` (134 insertions, zero deletions), `status: proposed`. Microsoft Learn search then fetch established the `PrintToPdfStreamAsync` and `CoreWebView2Controller`/HWND facts; all fetched URLs and the date are in `## Links`. `Test-DocumentationLinks.ps1`, `Test-TestMarkdownPlacement.ps1`, and `Test-MarkdownPlacement.ps1 -Base d22c39dd -Head HEAD` passed after staging.

Paused before the PR/reviewer/acceptance steps: this clone has `origin/main` and `origin/task/desktop-plan-segmentation` only — no `origin/dev` — and this scoped run does not push or open a PR. FND-007 intentionally remains Implementing on `fnd-007-webview2-adr`. `docs_todo` remains true and no `link_doc` is added because the proposed ADR is not on `origin/dev`. Reviewer-directed follow-up `d3762780` resolved the form discrepancy to `[frd-11]`; the remaining pause is the missing-`origin/dev` PR path, later acceptance, and proof.

2026-08-24 — Reviewer-directed follow-up resolved the frontmatter-form discrepancy: ADR-0108 now uses `related_frd: [frd-11]`, matching the house form in ADR-0025/0026/0028. Commit `d3762780` changes no other source file and no ADR index row. `git diff --check`, documentation-link, placement-regression, and committed-range placement checks passed. The remote/no-`origin/dev` PR blocker remains unchanged.

2026-08-24 — Branch published successfully as `origin/fnd-007-webview2-adr` at `d376278098e7731738195a6773d7318c3b382e72` after a repository-local credential-helper override. The PR/review checkbox remains open solely because `origin/dev` does not exist; no PR target was substituted.


2026-08-25 — User-directed correction: Microsoft Learn explicitly documents `HWND_MESSAGE` as the valid invisible parent for `CoreWebView2Controller` on Windows 8+. ADR-0108 and the Phase 0/7 source plans now fix that host; Phase 7 retains only packaged-app/PDF validation and parity.

2026-08-25 — User-directed Microsoft Learn correction committed as `f328076d`: ADR-0108 and the Phase 0/7 plans now use `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`; Phase 7 validates packaged integration rather than selecting a host. Documentation links, placement regression, committed-range placement and `git diff --check` passed. The commit is local only; no push was requested.
