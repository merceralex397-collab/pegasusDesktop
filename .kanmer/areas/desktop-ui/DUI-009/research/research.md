# Research — DUI-009 ReasonDialog

## Question

How can every reasoned or destructive desktop action use one accessible ContentDialog that names its consequence and cannot confirm accidentally?

## Verified findings

- The authority fixes title, record identity, approved consequence text, labelled reason, verb-labelled confirmation/Cancel, focus containment and focus return.
- The web ReasonDialog is implementation evidence; it is not permission to carry over unapproved explanatory copy.
- ContentDialog is reserved for decisions requiring interruption; non-blocking feedback remains page-local.

## Implication

Use a narrow shared WinUI 3 control or test-lane extension. The desktop remains native, online-required and gateway-backed; no WebView shell, direct database/provider access, Azure write, or second policy owner is justified.

## Dependencies

DUI-001 supplies theme/focus resources and DSK-02-08 owns dialog-service registration.
