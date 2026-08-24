# Research — DUI-010 ProblemInfoBar

## Question

How can gateway ProblemDetails become a concise, copy-safe operator sentence plus a Reference value without raw problem codes or banned words?

## Verified findings

- The gateway owns problem type and correlation data; the desktop maps the known contract to a per-page InfoBar.
- The authority resolves the forbidden phrase correlation identifier to the visible label Reference.
- The ticket requires build-failing tests for banned terminology and raw-code leakage.

## Implication

Use a narrow shared WinUI 3 control or test-lane extension. The desktop remains native, online-required and gateway-backed; no WebView shell, direct database/provider access, Azure write, or second policy owner is justified.

## Dependencies

DSK-03-02 owns the gateway mapping and correlation value; this ticket consumes it.
