# Research — DUI-002 Developer gallery/debug page

## Question

How can one non-production page make every approved token and shared control state reviewable without becoming an operator-facing screen?

## Verified findings

- The ticket body fixes the gallery's sections and makes it the review surface for Light, Dark and forced-colours states.
- It consumes the resource keys from DUI-001 and must expose resolved resources instead of duplicating colour or typography literals.
- The design authority permits labels for keys and values but prohibits explanatory operator copy.

## Implications

Implement the smallest shared WinUI slice stated in the ticket body. Reuse the existing project, generated gateway client and authority documents; do not create a WebView shell, direct data access, a second vocabulary/resource list, or an Azure dependency. The implementation agent is `winui-dev`; independent review is by `pegasus-desktop-reviewer`.

## Dependency / decision handling

DUI-001 must supply the resource keys; a design-owner review of the Dark palette is recorded against the theme work, not invented in this ticket.
