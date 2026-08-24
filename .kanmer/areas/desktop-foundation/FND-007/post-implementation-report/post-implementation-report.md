# Post-implementation report — FND-007

## 2026-08-25 correction — documented invisible host

Microsoft Learn documents `HWND_MESSAGE` as the valid parent for an invisible `CoreWebView2Controller` on Windows 8 and later; the WebView will never become visible. The fixed design is `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`. This supersedes every earlier collapsed-XAML/hidden-HWND host-selection instruction below. Phase 7 validates packaged-app initialisation, PDF output and no-window behaviour; it does not select a host. This user-directed correction also adds `docs/desktop/00-governance-and-workflow/README.md` and `docs/desktop/07-integrations/README.md` to FND-007's docs-only scope.


## Summary

Added the proposed ADR-0108 record in commit `39c704dc` on `fnd-007-webview2-adr`. It records the isolated, never-visible WebView2 HTML-to-PDF exception; keeps the gateway renderer until local Test/UAT golden-file parity; and names the documented `HWND_MESSAGE` controller as the invisible host; Phase 7 validates packaged-app integration rather than selecting a host. This is not a merged, accepted, or implemented renderer. The ticket remains **Implementing** because this clone has no `origin/dev` for the required PR path and this run must not push.

## Changes

| File | Change | Why |
| --- | --- | --- |
| `docs/adr/0108-desktop-webview2-report-rendering.md` | Added, 134 lines, `status: proposed` | Records L-03's narrow WebView2 exception before a renderer implementation exists, preserves the gateway fallback/parity gate, and records the fixed documented host with a packaged-controller validation gate. |

No renderer code, tests, index row, accepted ADR, or Azure resource changed.

## Governing docs

FND-007 has no existing `refs` and remains `docs_todo: true`; the new ADR is the governing decision it authors. It follows the proposal's section 2.1 no-WebView-shell rule and section 23.2 exception, L-02/L-03, ADR-0025's shared-template boundary, ADR-0028's existing gateway renderer, and FRD-11's gateway-owned report authority.

The ADR deliberately does **not** link into `docs/adr/README.md`: that index represents accepted current architecture, while ADR-0108 is proposed. It also does not link the ticket path or clear `docs_todo`, because the ADR is not on `origin/dev`.

Microsoft Learn search followed by fetch verified and cited, with fetch date 2026-08-24:

- `CoreWebView2.PrintToPdfStreamAsync` provides PDF data asynchronously and allows only one printing operation per WebView.
- `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)` is the documented invisible-host route on Windows 8+; the WebView will never become visible.

## Risks / follow-ups

- [[FEAT-040]] (`DSK-07-14`) validates the fixed `HWND_MESSAGE` controller from the packaged app: initialization, PDF output, runtime version and no-window evidence.
- [[FEAT-041]] (`DSK-07-15`) owns approved-fixture golden-file parity. Runtime absence, packaged-controller failure, or unclosable parity divergence triggers the stated reversal condition.
- [[FEAT-038]] (`DSK-07-12`) owns the later acceptance change and accepted-index row after the evidence exists.
- The ticket body initially required `related_frd: [FRD-11]`, while existing ADRs use lowercase file stems. Reviewer-directed follow-up `d3762780` normalised ADR-0108 to `[frd-11]`, matching ADR-0025, ADR-0026, and ADR-0028.
- `git branch -r` shows only `origin/main` and `origin/task/desktop-plan-segmentation`; `origin/dev` is absent. Therefore no push, PR, or move to Review was attempted.

## Verification hand-off

Completed on the branch after staging the new ADR:

- `git diff --cached --check` — passed.
- `pwsh -NoProfile -File scripts/Test-DocumentationLinks.ps1` — passed; 233 files checked.
- `pwsh -NoProfile -File scripts/Test-TestMarkdownPlacement.ps1` — passed.
- `pwsh -NoProfile -File scripts/Test-MarkdownPlacement.ps1 -Base d22c39dd -Head HEAD` — passed.
- The commit contains exactly `docs/adr/0108-desktop-webview2-report-rendering.md`, with 134 insertions and no deletions.

Before any PR or later acceptance, confirm the ADR remains `status: proposed`, `supersedes: []`, `superseded_by: []`, has no `docs/adr/README.md` row, retains the Microsoft Learn URLs/fetch date, and still contains the non-UI, gateway-parity, and reversal clauses. On merged main, `kanmer-verify` must write proof only after the Phase 7 acceptance evidence exists.

## Reviewer-driven normalization

The reviewer-directed follow-up normalised ADR-0108's `related_frd` value from `[FRD-11]` to `[frd-11]`, matching ADR-0025, ADR-0026, and ADR-0028. Commit `d3762780` changes that frontmatter token only; it does not alter status, body, index, scope, or the proposed/acceptance boundary.

`git diff --check 39c704dc..HEAD`, `Test-DocumentationLinks.ps1` (233 files), `Test-TestMarkdownPlacement.ps1`, and `Test-MarkdownPlacement.ps1 -Base 39c704dc -Head HEAD` all passed. The `origin/dev` delivery blocker remains unchanged; no push, PR, or stage move occurred.

## Delivery update — 2026-08-24

The branch is now published: `origin/fnd-007-webview2-adr` at `d376278098e7731738195a6773d7318c3b382e72`, tracking the local branch. The repository-local credential configuration now pins `merceralex397-collab` and overrides the global GitHub CLI credential helper with Git Credential Manager; no global GitHub account was changed. `origin/dev` is still absent, so no PR or stage move was made.


## User-directed correction — 2026-08-25

Microsoft Learn's documented `HWND_MESSAGE` host invalidated the former two-host spike. This branch now also updates `docs/desktop/07-integrations/README.md` and `docs/desktop/00-governance-and-workflow/README.md`, and the related Kanmer artifacts, to make Phase 7 a packaged-controller validation rather than a host-selection exercise.


### Correction validation

- `pwsh -NoProfile -File scripts/Test-DocumentationLinks.ps1` — passed; 233 files checked.
- `pwsh -NoProfile -File scripts/Test-TestMarkdownPlacement.ps1` — passed.
- `git diff --check` — passed.

2026-08-25 — User-directed Microsoft Learn correction committed as `f328076d`: ADR-0108 and the Phase 0/7 plans now use `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`; Phase 7 validates packaged integration rather than selecting a host. Documentation links, placement regression, committed-range placement and `git diff --check` passed. The commit is local only; no push was requested.
