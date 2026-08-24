# Research — DUI-012 Page header and manual refresh

## Question

How can each screen state what it shows and refresh the exact query safely, while preserving last-good data and avoiding a lede?

## Verified findings

- The authority fixes a one-title/no-lede header and manual refresh that reruns the same filter, reports state and keeps last-good data.
- The web FreshnessBanner describes supported query states and Europe/London or UTC labelling; the desktop consumes the semantic model.
- Refresh is telemetry/query work, never a business command.

## Implication

Use a narrow shared WinUI 3 control or test-lane extension. The desktop remains native, online-required and gateway-backed; no WebView shell, direct database/provider access, Azure write, or second policy owner is justified.

## Dependencies

DUI-001 provides text/brush resources; actual data queries remain in gateway contracts.
