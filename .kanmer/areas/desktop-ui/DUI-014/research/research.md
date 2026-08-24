# Research — DUI-014 Keyboard map and access keys

## Question

How can the whole authority keyboard map be implemented once, discoverable and script-verifiable across shell and slices?

## Verified findings

- The authoritative keyboard map fixes Ctrl+K Cases-search behaviour, core commands, rail access keys, F6 cycling, visible focus and AutomationIds.
- Keyboard completion is a tier-7 acceptance requirement, not a convenience enhancement.
- The shell is a prerequisite and TEST-007 consumes the resulting scripted journeys.

## Implication

Use a narrow shared WinUI 3 control or test-lane extension. The desktop remains native, online-required and gateway-backed; no WebView shell, direct database/provider access, Azure write, or second policy owner is justified.

## Dependencies

DUI-004 supplies the shell; TEST-007 is the downstream end-to-end consumer.
