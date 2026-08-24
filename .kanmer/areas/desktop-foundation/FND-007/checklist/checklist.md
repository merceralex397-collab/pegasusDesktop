# Checklist — FND-007

One box per plan step, in plan order. Every box is independently tickable.

- [ ] Read the plan row, § 3's ADR-0108 line, `docs/desktop/07-integrations/README.md:227-230` and `:255`, and `docs/adr/0025-*.md` and `docs/adr/0028-*.md`; call `get_doc_gates FND-007` and `take_ticket`
- [ ] Run `ls docs/adr/0108*` **before writing anything** and record its output (expected on 2026-08-24: no such file); if it exists, record that [[FEAT-038]] created it and that this ticket edits in place
- [ ] `microsoft_docs_search` / `microsoft_docs_fetch` for `CoreWebView2.PrintToPdfStreamAsync` and for `CoreWebView2Controller` hosting on a window handle; record both URLs **with the fetch date**
- [ ] Create `docs/adr/0108-desktop-webview2-report-rendering.md` with the `AGENTS.md:94-108` frontmatter: `status: proposed`, `supersedes: []`, `superseded_by: []`, `related_frd: [frd-11]` as a lowercase stem
- [ ] Give it the heading set `## Status · ## Context · ## Decision · ## Consequences · ## Reversal condition · ## Links`, following `docs/adr/0029-*.md:11-20`
- [ ] Write `## Status`: "Proposed", the ADR-0028-style relation sentence (refines/relates ADR-0025 and ADR-0028, supersedes neither), and the explicit statement that a `proposed` ADR is not settled authority and the gateway renderer remains the path in use
- [ ] Write `## Context` covering the L-03 statement, why proposal § 2.1 (`:60`) is not breached, and the retained gateway renderer of ADR-0025/ADR-0028
- [ ] Put the six-question cloud-justification table in `## Context` with a real answer and a real citation in every row — no blank cells
- [ ] Answer the "measured operational advantage" row **no**, citing the measured costs: Chromium startup on first render, single-flight `SemaphoreSlim(1,1)`, and the cpu 1.0 / 2 Gi Container App sizing for in-process Chromium
- [ ] Write `## Decision`'s settled half: rendering moves to `Pegasus.Desktop.Infrastructure` behind `IAssessmentReportRenderer`, shared Scriban templates, isolated non-UI single-flight WebView2 that is never visible and never hosts Pegasus UI
- [ ] Write `## Decision`'s deferred half: the off-screen host (collapsed WinUI `WebView2` control versus `CoreWebView2Controller` on a hidden HWND) is recorded here once [[FEAT-040]] proves it — named as a parameter with its owner, not omitted
- [ ] State in `## Decision` that report readiness, accepted inputs, immutable identity and hash, correction and approval remain governed by FRD-11 and `Pegasus.Core`
- [ ] Write `## Consequences` with the retention rule as a checkable gate: the gateway renderer stays until [[FEAT-041]]'s golden-file parity tests pass on approved fixtures, and afterwards no required report may depend on the web renderer unless amended by a superseding ADR
- [ ] Write `## Reversal condition` naming the WebView2 runtime absence/version case (Q6.3) and the unclosable golden-file divergence case (Q6.2)
- [ ] Confirm **no** ADR-0108 row was added to `docs/adr/README.md` at this merge
- [ ] Confirm every forward reference to a not-yet-existing document is prose, not a relative link
- [ ] Confirm `git diff --stat` shows no change to `docs/adr/0025-*.md`, `docs/adr/0028-*.md` or `AGENTS.md`
- [ ] Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both exit 0
- [ ] Open the PR against `dev` and take the independent review from `pegasus-desktop-reviewer`, whose specific judgement is that the described renderer is not a web shell under proposal § 23.2
- [ ] Record the simplification pass under a dated `## Simplification pass` heading in `plan` (`n/a — docs-only`)
- [ ] After [[FEAT-040]] and [[FEAT-041]] are done, verify [[FEAT-038]]'s frontmatter-only acceptance PR against the reversal condition and the § 23.2 statement — and make no edit to ADR-0108 in this ticket
- [ ] Verification run — `ls docs/adr/0108*`, `grep -n '^status:'`, `grep -n '^related_frd:'`, `grep -n '0108' docs/adr/README.md`, the Learn-citation grep, and the two gate scripts — all as the plan's Verification table states; **this box produces `proof`**, which is the two PR references plus [[FEAT-041]]'s golden-file output cited, not re-run

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
