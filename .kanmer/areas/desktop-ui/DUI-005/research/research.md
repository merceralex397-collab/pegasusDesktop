# Research — DUI-005 Shared operator vocabulary consumption

## Question

How can every desktop-facing state, time, size and identifier be presented through one shared label map?

## Verified findings

- The web's OperatorLabels map is the existing business-facing vocabulary owner; the ticket requires the desktop to consume its relocation, not fork it.
- The ticket body identifies raw enum/GUID/hash/version/byte-count display as a testable failure and requires Europe/London time formatting.
- Identifier entry must use named pickers rather than raw aggregate keys.

## Implications

Implement the smallest shared WinUI slice stated in the ticket body. Reuse the existing project, generated gateway client and authority documents; do not create a WebView shell, direct data access, a second vocabulary/resource list, or an Azure dependency. The implementation agent is `winui-dev`; independent review is by `pegasus-desktop-reviewer`.

## Dependency / decision handling

FEAT-023's unresolved ownership split must remain unresolved here; this plan consumes its eventual decision rather than duplicating the question.
