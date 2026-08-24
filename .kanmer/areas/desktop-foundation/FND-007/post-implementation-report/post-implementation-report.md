# Post-implementation report — FND-007

## Summary

Added the proposed ADR-0108 record in commit `39c704dc` on `fnd-007-webview2-adr`. It records the isolated, never-visible WebView2 HTML-to-PDF exception; keeps the gateway renderer until local Test/UAT golden-file parity; and deliberately leaves the off-screen host unresolved for the Phase 7 spike. This is not a merged, accepted, or implemented renderer. The ticket remains **Implementing** because this clone has no `origin/dev` for the required PR path and this run must not push.

## Changes

| File | Change | Why |
| --- | --- | --- |
| `docs/adr/0108-desktop-webview2-report-rendering.md` | Added, 134 lines, `status: proposed` | Records L-03's narrow WebView2 exception before a renderer implementation exists, preserves the gateway fallback/parity gate, and gives the Phase 7 spike a named host-choice slot. |

No renderer code, tests, index row, accepted ADR, or Azure resource changed.

## Governing docs

FND-007 has no existing `refs` and remains `docs_todo: true`; the new ADR is the governing decision it authors. It follows the proposal's section 2.1 no-WebView-shell rule and section 23.2 exception, L-02/L-03, ADR-0025's shared-template boundary, ADR-0028's existing gateway renderer, and FRD-11's gateway-owned report authority.

The ADR deliberately does **not** link into `docs/adr/README.md`: that index represents accepted current architecture, while ADR-0108 is proposed. It also does not link the ticket path or clear `docs_todo`, because the ADR is not on `origin/dev`.

Microsoft Learn search followed by fetch verified and cited, with fetch date 2026-08-24:

- `CoreWebView2.PrintToPdfStreamAsync` provides PDF data asynchronously and allows only one printing operation per WebView.
- `CoreWebView2Controller` exposes the parent-window and visibility hosting mechanics; `CoreWebView2ControllerWindowReference` can be constructed from an HWND.

## Risks / follow-ups

- [[FEAT-040]] (`DSK-07-14`) must prove the off-screen host: collapsed WinUI `WebView2` with a XAML root versus `CoreWebView2Controller` on a hidden HWND. ADR-0108 names neither as selected.
- [[FEAT-041]] (`DSK-07-15`) owns approved-fixture golden-file parity. Runtime absence, unclosable parity divergence, or neither host working off-screen triggers the stated reversal condition.
- [[FEAT-038]] (`DSK-07-12`) owns the later acceptance change and accepted-index row after the evidence exists.
- The ticket body requires `related_frd: [FRD-11]`, while existing ADRs use lowercase file stems. The implemented value follows the settled body; an independent reviewer should decide whether a normalisation change is needed.
- `git branch -r` shows only `origin/main` and `origin/task/desktop-plan-segmentation`; `origin/dev` is absent. Therefore no push, PR, or move to Review was attempted.

## Verification hand-off

Completed on the branch after staging the new ADR:

- `git diff --cached --check` — passed.
- `pwsh -NoProfile -File scripts/Test-DocumentationLinks.ps1` — passed; 233 files checked.
- `pwsh -NoProfile -File scripts/Test-TestMarkdownPlacement.ps1` — passed.
- `pwsh -NoProfile -File scripts/Test-MarkdownPlacement.ps1 -Base d22c39dd -Head HEAD` — passed.
- The commit contains exactly `docs/adr/0108-desktop-webview2-report-rendering.md`, with 134 insertions and no deletions.

Before any PR or later acceptance, confirm the ADR remains `status: proposed`, `supersedes: []`, `superseded_by: []`, has no `docs/adr/README.md` row, retains the Microsoft Learn URLs/fetch date, and still contains the non-UI, gateway-parity, and reversal clauses. On merged main, `kanmer-verify` must write proof only after the Phase 7 acceptance evidence exists.
