# Research — DUI-008 Form section and field pattern

## Question

How can reusable forms satisfy the authority's label/control-only rule while exposing required state and validation accessibly?

## Verified findings

- The authority prohibits hint, optional and format-guidance prose; a field is label plus control with associated validation.
- WinUI `AutomationProperties.LabeledBy` associates a visible label with an input; Microsoft Learn confirms this preferred WinUI pattern.
- TextBox two-way input must use `UpdateSourceTrigger=PropertyChanged` for keyboard-driven validation.

## Implications

Implement the smallest shared WinUI slice stated in the ticket body. Reuse the existing project, generated gateway client and authority documents; do not create a WebView shell, direct data access, a second vocabulary/resource list, or an Azure dependency. The implementation agent is `winui-dev`; independent review is by `pegasus-desktop-reviewer`.

## Dependency / decision handling

DUI-001 tokens are consumed; DUI-010 provides page-level problem treatment, not a replacement for field validation.
