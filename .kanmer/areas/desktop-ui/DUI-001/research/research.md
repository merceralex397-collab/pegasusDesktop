# Research — DUI-001 Theme resource dictionaries

## Question

How can the desktop consume the authority tokens once, in all three themes, without creating a second palette or bypassing High Contrast?

## Verified findings

- `tokens-and-theme.md` is the source for the resource keys, load order and Light/Dark/HighContrast dictionaries; the ticket body assigns values, not a new style architecture.
- `docs/design/README.md` fixes colour, typography, spacing, shape and focus values; `src/Pegasus.Web/wwwroot/css/site.css` is evidence only, not a second source of truth.
- The WinUI guidance requires `{ThemeResource}` at use sites, explicit Light/Dark/HighContrast dictionaries, and no `HighContrastAdjustment="None"`.

## Implications

Implement the smallest shared WinUI slice stated in the ticket body. Reuse the existing project, generated gateway client and authority documents; do not create a WebView shell, direct data access, a second vocabulary/resource list, or an Azure dependency. The implementation agent is `winui-dev`; independent review is by `pegasus-desktop-reviewer`.

## Dependency / decision handling

DSK-02-09 owns the Styles set, App.xaml merge and guard. The two design-owner confirmations named in the ticket body remain external review inputs; do not silently choose a different palette or radius.
