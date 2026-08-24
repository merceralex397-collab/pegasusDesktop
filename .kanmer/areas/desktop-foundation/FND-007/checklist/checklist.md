# Checklist — FND-007

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")` as you go; append progress notes below rather
than rewriting.

- [ ] Read the plan row, `docs/desktop/00-governance-and-workflow/README.md` § 3's ADR-0108 line, `docs/desktop/07-integrations/README.md:227-231` and `:251-259`, and the two renderer ADRs `docs/adr/0025-*.md` and `docs/adr/0028-*.md`
- [ ] Call `get_doc_gates FND-007`, confirm `leave-backlog: [governing-doc]` is satisfied by `docs_todo`, then `take_ticket` with a real branch and worktree cut from `origin/dev`
- [ ] Run `ls docs/adr/010*` **before writing** and record the output (expected: `No such file or directory`; a file appearing means someone else wrote this ADR)
- [ ] `microsoft_docs_search` for `CoreWebView2.PrintToPdfStreamAsync`, and again for `CoreWebView2Controller` hosting on a window handle; `microsoft_docs_fetch` the WebView2 print how-to
- [ ] Create `docs/adr/0108-desktop-webview2-report-rendering.md` with the eight-key frontmatter, `status: proposed`, `supersedes: []`, `superseded_by: []`, `related_frd: [FRD-11]`
- [ ] Raise the `related_frd` form with `pegasus-desktop-reviewer` in the PR — the body says `[FRD-11]`, all 28 existing ADRs use `[frd-11]` — and record their answer rather than switching it silently
- [ ] Use the heading set `## Status · ## Context · ## Decision · ## Consequences · ## Reversal condition · ## Options considered · ## Links`, Status first, following `docs/adr/0029-*.md:11-20`
- [ ] In `## Status`, state `proposed` and what would change it, in the form `docs/adr/0028-*.md:13-16` uses
- [ ] In `## Context`, state L-03
- [ ] In `## Context`, quote proposal § 23.2 `:1715` ("…requires an ADR and must not host Pegasus UI") and § 2.1 `:1351` ("no WebView shell") **verbatim**, and argue why the exception does not swallow the rule
- [ ] In `## Context`, record the retained gateway renderer of ADR-0025/ADR-0028 and what central rendering costs the Web image (`docs/adr/0028-*.md:22-27`)
- [ ] In `## Context`, fill **every cell** of the six-question cloud-justification table for report rendering, transcribed from this ticket's `research` document and re-verified; answer "measured operational advantage" as **no, and not yet measured**
- [ ] In `## Decision`, record what is decided now: `Pegasus.Desktop.Infrastructure` behind the **existing** `IAssessmentReportRenderer` port (`src/Pegasus.Core/Reports/AssessmentReportRendering.cs:284`), the same governed Scriban templates embedded by [[FEAT-039]], isolated/non-UI/never-visible/single-flight, and the runtime-missing named failure with gateway fallback
- [ ] In `## Decision`, state that the desktop **produces the document bytes but never registers the report** — registration, custody and audit stay behind the gateway ([[FEAT-042]])
- [ ] In `## Decision`, write the off-screen host as an explicitly **named blank** a later editor can fill without touching anything around it, citing [[FEAT-040]]'s spike
- [ ] In `## Consequences`, state the retention gate with its owner: the gateway renderer stays until [[FEAT-041]]'s golden-file parity passes on approved fixtures, after which no required report may depend on the web renderer unless a superseding ADR says so
- [ ] In `## Consequences`, state that parity is tolerant and not pixel-equal (WebView2 Chromium self-updates; Playwright is pinned to 1.61.0), and that acceptance proves architecture only
- [ ] Write `## Reversal condition`: WebView2 runtime absent or unmaintainable across the fleet; unclosable golden-file divergence; neither off-screen host initialising without a visible window
- [ ] Confirm **no** row was added to `docs/adr/README.md`, and that `AGENTS.md` was not edited
- [ ] Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both exit 0, with the Microsoft Learn URLs and their fetch date **outside** any fenced block
- [ ] Confirm `git diff --stat` shows 1 file changed and **0 deletions**, and that `git diff --stat -- docs/adr/0025-*.md docs/adr/0028-*.md docs/adr/README.md AGENTS.md` is empty
- [ ] Open the PR against `dev` with `gh pr create --base dev` and merge on the independent review from `pegasus-desktop-reviewer`, whose named judgement is that the renderer is not a web shell
- [ ] After [[FEAT-040]] and [[FEAT-041]] are done, verify [[FEAT-038]]'s frontmatter-only acceptance PR against the reversal condition and the § 23.2 statement — making **no** edit to ADR-0108 from this ticket
- [ ] Verification run: record the proof as the two PR references plus the golden-file test output cited from [[FEAT-041]], together with the command table from the plan's `## Verification` section

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
