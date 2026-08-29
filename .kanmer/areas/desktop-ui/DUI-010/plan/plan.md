# Plan — DUI-010 ProblemInfoBar

## Governing documents

This ticket remains docs_todo: true; use the authority and area plan now, then link the appropriate FRD-13 section once authored. Do not create a competing PRD, FRD or ADR from this ticket.

## Chosen approach

How can gateway ProblemDetails become a concise, copy-safe operator sentence plus a Reference value without raw problem codes or banned words?

## Steps

1. Inspect the API problem-details mapping and authority copy lists.
2. Create the narrow problem-presentation model and InfoBar style using one sentence plus expandable/copyable Reference.
3. Add guard tests for known mappings, banned words and raw-code leakage.
4. Render representative retry, unavailable, denied and validation cases in a test host.

## Verification

- View-model tests fail for a banned term or raw problem code.
- UIA exposes a copyable Reference only when supplied by the gateway.
- InfoBar state remains screen-local and never claims an external action succeeded.

## Risks and dependencies

DSK-03-02 owns the gateway mapping and correlation value; this ticket consumes it.

The implementation must record its simplification pass and independent pegasus-desktop-reviewer assessment before merge.

## Implementation evidence — 2026-08-29

- Consumed the existing `Pegasus.Contracts.ProblemDetails.PegasusProblem` and `PegasusProblemTypes` contract; all 13 current public problem-type constants are mapped. No gateway source or contract was changed.
- Added one `ProblemPresentation` table and one native `ProblemInfoBar` control. The reusable control accepts a page-supplied problem and AutomationId prefix; ordinary `MainPage` does not instantiate synthetic problem states. The control exposes only the mapped sentence and optional Reference value. Copy uses `DataPackage.SetText` with that value only.
- Reused the existing `tests/Pegasus.Desktop.ViewModelTests` project and reflected over the contract constants for the mapping theory. Added unmapped, banned-word, and raw-code guards; no second test project or UI test file was created because the UI test scaffold is not present.

## Simplification pass — 2026-08-29

- Reuse: retained the existing `PegasusProblem`/`PegasusProblemTypes` contract, CommunityToolkit ViewModel, native `InfoBar`, and current ViewModelTests scaffold.
- Simplification: used one private mapping dictionary and one transport-neutral presentation record; no service, interface, converter, notification centre, toast, modal, or fallback sentence was added.
- Scope: the ordinary `MainPage` has no synthetic problem gallery; [[DUI-002]] owns the non-production gallery. Earlier direct `winapp ui` checks are historical evidence for the superseded sample page, not current full acceptance. No unrelated files, gateway code, corpus, cloud state, or upstream state were touched.
- Disposition: no behaviour-preserving simplification findings remain. The Desktop and ViewModelTests lock entries are required by the new `Pegasus.Contracts` project reference so `--locked-mode` remains valid.

Independent `pegasus-desktop-reviewer` assessment remains a pre-merge gate and was not represented as complete by the implementer.

## Independent review findings and remediation — 2026-08-29

Locke reviewed exact commit `16d40759eb0a2fda1d12e45fbc184cc9267f778a` and correctly identified merge blockers:

- synthetic validation/unavailable/not-found states were rendered on ordinary startup;
- replacing one open problem did not force a fresh Polite announcement;
- the test asserted only that severity was an enum value, not the required value per gateway type;
- the banned-word guard covered only the presentation table, not static desktop XAML operator attributes;
- the checklist overstated the four sample states and independent review.

Disposition: remove all synthetic ProblemInfoBar instances and sample problems from `MainPage`/its view model until [[DUI-002]] owns a non-production gallery; keep the reusable control and centralized mapping. Add dispatcher-mediated close/reopen behavior for every replacement ProblemPresentation. Add an explicit expected severity matrix for all 13 gateway types and scan static desktop XAML `Text`, `Header`, `Content`, `AutomationProperties.Name`, and `ToolTip` values in the guard test. Correct the checklist so the unavailable gallery evidence and independent review are not marked complete. No UI harness or theme evidence is fabricated; [[TEST-006]]/[[DUI-002]] remain prerequisites for that evidence.

The remediation build passed with zero warnings/errors; focused ProblemPresentationTests passed 16/16. A fresh independent review is required before merge.


## Final remediation commit — 2026-08-29

Commit `681f6f16e66ba9e96e20e3cf6d1ede63f7344db4` separates the Problem and AutomationIdPrefix dependency-property callbacks, reopens the InfoBar only for a new ProblemPresentation, binds both Reference accessibility names to the centralized label, removes the unused startup layout/sample namespace, and makes the static XAML guard accept both quote styles. It also asserts that gateway problem-type values do not appear in operator strings. Locked restore, Release desktop build (0 warnings/errors), full ViewModelTests (22/22), and `git diff --check` passed.

The runtime announcement sequence, Dark/High Contrast/200% scale sweep, keyboard walkthrough, and scripted UI test remain unclaimed because `tests/Pegasus.Desktop.UITests/problem-tests.ps1` is not present; these are explicitly deferred to [[TEST-006]]/[[DUI-002]]. A fresh independent review of this exact commit is pending.
