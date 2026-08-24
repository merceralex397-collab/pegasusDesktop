# Research — DUI-015 Accessibility automation lane

## Question

How can the UI test lane continuously reject interactive controls without AutomationIds or accessibility-critical defects while retaining mandatory human review?

## Verified findings

- The area exit gate requires 100% interactive-control AutomationId coverage and no critical AxeWindowsCLI findings.
- The UI testing skill defines app-element filtering needed to avoid window chrome and OS dialogs.
- Automated results are additive: ten recorded human reviews remain separately required.

## Implication

Use a narrow shared WinUI 3 control or test-lane extension. The desktop remains native, online-required and gateway-backed; no WebView shell, direct database/provider access, Azure write, or second policy owner is justified.

## Dependencies

TEST-006 supplies the harness; TEST-012 determines CI feasibility; TEST-013 owns workflow lanes; DUI-016 retains human-review ownership.
