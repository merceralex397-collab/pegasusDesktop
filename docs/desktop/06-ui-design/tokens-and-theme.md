# Tokens and theme — WinUI resource plan derived from the design authority

Every value below is taken from [`docs/design/README.md`](../../design/README.md)
§ Tokens (`:182-293`), § Icons (`:334-376`) and § Logo (`:297`), with the
neutral extras from `.stitch/DESIGN.md` where the authority is silent. The
authority is the source; this file is the WinUI mapping. A change to a value
starts in the authority (its change and verification rule, `:982`), never
here. No hex literal may appear in a view; every colour is a `{ThemeResource}`
from the dictionaries planned here (`winui-code-review` theming checklist).

## Files and load order

```text
src/Pegasus.Desktop/Styles/
├─ Tokens.Colors.xaml        # ResourceDictionary.ThemeDictionaries: Light, Dark, HighContrast
├─ Tokens.Typography.xaml    # text styles based on built-in WinUI styles
├─ Tokens.Spacing.xaml       # x:Double spacing steps, row heights, gutters
├─ Tokens.Shape.xaml         # ControlCornerRadius, OverlayCornerRadius, border thickness
├─ Tokens.Focus.xaml         # focus visual overrides
├─ Icons.Lucide.xaml         # PathIcon geometries for the sixteen registered glyphs
├─ Controls.*.xaml           # StatusChip, ReasonDialog, ProblemInfoBar, DataTable header, field
└─ Pegasus.Theme.xaml        # merges the above in order; referenced once from App.xaml
```

`App.xaml` merges `Pegasus.Theme.xaml` after the WinUI `XamlControlsResources`
so overrides win. Theme dictionaries cover `Light`, `Dark` **and**
`HighContrast` explicitly — never `Default` (`winui-design` skill `:143`).

## Colour tokens

Keys are named by purpose, not hue (authority `:160-165`; `winui-design`
theming rules). Light values are the authority's. Dark values are an
**assumption** (the authority is light-only): the same semantic roles on the
inverse band palette the authority already uses for record heads
(`#1b1e23` band, `#ededee` text). HighContrast maps every key to a system
colour so forced-colours mode governs (`SystemColorWindowColor`,
`SystemColorWindowTextColor`, `SystemColorHighlightColor`,
`SystemColorHighlightTextColor`, `SystemColorButtonFaceColor`,
`SystemColorButtonTextColor`, `SystemColorGrayTextColor`,
`SystemColorHotlightColor`).

| Key (`SolidColorBrush` unless noted) | Role (authority) | Light | Dark (assumption) | HighContrast |
| --- | --- | --- | --- | --- |
| `PegasusAccentBrush` | Collision red: primary action, active navigation marker, urgent emphasis (`:163`) | `#DB0816` | `#FF5A63` (lifted for contrast on dark) | `SystemColorHighlightColor` |
| `PegasusAccentPressedBrush` | Pressed/dark red | `#8F1422` | `#DB0816` | `SystemColorHighlightColor` |
| `PegasusAccentTintBrush` | Red tint (selected row, hover wash) | `rgba(219,8,22,.07)` → `#12DB0816` | `#33FF5A63` | `SystemColorHighlightColor` at opacity 0.2 |
| `PegasusNavigationBrush` | Warm charcoal navigation/rail | `#2C2A27` | `#1B1E23` | `SystemColorButtonFaceColor` |
| `PegasusNavigationTextBrush` | Text on the rail | `#FFFFFF` | `#EDEDEE` | `SystemColorButtonTextColor` |
| `PegasusInkBrush` | Near-black ink, primary text | `#16191D` | `#EDEDEE` | `SystemColorWindowTextColor` |
| `PegasusMutedTextBrush` | Muted/secondary text | `#6B6B6B` | `#A7A9AD` | `SystemColorGrayTextColor` |
| `PegasusGroundBrush` | Light-neutral application ground | `#F5F4F2` | `#121417` | `SystemColorWindowColor` |
| `PegasusSurfaceBrush` | Warm paper surface (`.stitch` `surface`) | `#F7F6F4` | `#16191D` | `SystemColorWindowColor` |
| `PegasusPanelBrush` | White panels/cards/tables | `#FFFFFF` | `#1B1E23` | `SystemColorWindowColor` |
| `PegasusBorderBrush` | 1px hairline border | `#E6E4E1` | `#2F333A` | `SystemColorWindowTextColor` |
| `PegasusBorderStrongBrush` | Strong hairline (`.stitch` `#d8d5d1`) | `#D8D5D1` | `#3A3F47` | `SystemColorWindowTextColor` |
| `PegasusRecordBandBrush` | Inverse record head band | `#1B1E23` | `#0E1013` | `SystemColorButtonFaceColor` |
| `PegasusRecordBandTextBrush` / `PegasusRecordBandMutedBrush` | Text on the band | `#EDEDEE` / `#A7A9AD` | same | `SystemColorButtonTextColor` |
| `PegasusSuccessBrush` | Confirmed completion only (`:165`, `:200`) | `#16833B` | `#3DBD66` | `SystemColorWindowTextColor` |
| `PegasusSuccessContainerBrush` | Success container | `#E8F3EC` | `#10301B` | `SystemColorWindowColor` |
| `PegasusPendingForegroundBrush` / `PegasusPendingBackgroundBrush` / `PegasusPendingBorderBrush` | Incomplete/pending amber | `#7A3E00` / `#FFF4D6` / `#A15C00` | `#FFD27A` / `#3A2A06` / `#A15C00` | window text / window / window text |
| `PegasusReviewForegroundBrush` / `PegasusReviewBackgroundBrush` / `PegasusReviewBorderBrush` | Review navy | `#143A5E` / `#EAF1F8` / `#365F87` | `#9EC5EA` / `#0F1F2E` / `#365F87` | window text / window / window text |
| `PegasusPrimaryContainerBrush` | Red-tinted container (`.stitch` `#fceeef`) | `#FCEEEF` | `#2A1214` | `SystemColorWindowColor` |
| `PegasusPlateBackgroundBrush` / `PegasusPlateForegroundBrush` / `PegasusPlateBorderBrush` | VRM plate (`.stitch`): yellow plate, ink text | `#FCD116` / `#16191D` / `#D9B012` | same (plate is a real-world artefact) | window / window text / window text |
| `PegasusReferencePlateBackgroundBrush` | White plate variant for internal references (Case/PO, Image reference) | `#FFFFFF` with `PegasusBorderStrongBrush` | `#1B1E23` | `SystemColorWindowColor` |
| `PegasusFocusBrush` | Keyboard focus ring `3px rgba(219,8,22,.38)` (`:264`) | `#61DB0816` | `#80FF5A63` | `SystemColorHighlightColor` |
| `PegasusDangerBrush` | Error text/icon (error summary, failed state) — reuses Collision red sparingly | `#DB0816` | `#FF5A63` | `SystemColorWindowTextColor` |

Rules carried from the authority:

- Green never means progress, availability or generic positivity (`:200`).
- Collision red is sparse: primary actions, the active-route marker, visible
  focus and urgent emphasis (`:163`). One primary (accent) button per view
  region.
- Excluded: WhatsApp green, large display scales, CTA shadows, gradients,
  neon/glow, purple/blue "AI" aesthetics, pure black `#000000`, cool slate
  greys (`:201`, `.stitch` § banned).
- The one reviewed divergence (`Send to Claude` control, `:206-237`) is a web
  control; the desktop has no equivalent in the conversion scope.

Contrast: every foreground/background pair above must pass 4.5:1 for body
text and 3:1 for large text and UI boundaries in Light and Dark; the contrast
review is one of the ten recorded reviews. The Dark values are starting
points to be adjusted by that review, not authority.

## Typography

Authority: system UI stack, body 13.5–14px (`:239-256`, `:176`); `.stitch`
scale metric 28/700, page title 20/700, section heading 15/700, sub 14/650,
body 13.5/400 at 20px line height, body-small 13, caption 12.5, eyebrow
11/700 uppercase 0.08em, plate/reference `ui-monospace` 13/700 0.04em;
"nothing larger than 28px exists"; tabular numerals everywhere for counts,
metrics, dates and references.

WinUI mapping uses only built-in text styles (no raw `FontSize` in views —
`winui-code-review`); Segoe UI Variable is the system UI face. Sizes below
are the WinUI type ramp; the authority sizes they stand in for are shown.

| Desktop style key (derived from) | Use | WinUI size/weight | Authority target |
| --- | --- | --- | --- |
| `PegasusMetricTextStyle` (based on `TitleTextBlockStyle`) | Dashboard metric values | 28 / SemiBold | 28/700 metric-numeral |
| `PegasusPageTitleTextStyle` (`SubtitleTextBlockStyle`) | Screen title (one per screen, no lede) | 20 / SemiBold | 20/700 |
| `PegasusSectionTextStyle` (`BodyStrongTextBlockStyle`) | Section/tab headings | 14 / SemiBold | 15/700 (assumption: 14 acceptable; confirm in review) |
| `PegasusBodyTextStyle` (`BodyTextBlockStyle`) | Body, table cells, fields | 14 / Normal, line height 20 | 13.5–14/400 |
| `PegasusBodyStrongTextStyle` (`BodyStrongTextBlockStyle`) | Queue/metric labels, stronger weight allowed (`:250`) | 14 / SemiBold | 14/650 |
| `PegasusCaptionTextStyle` (`CaptionTextBlockStyle`) | Captions, freshness time, helper rows that are data (never hints) | 12 / Normal | 12.5–13 |
| `PegasusEyebrowTextStyle` (`CaptionTextBlockStyle` + `CharacterSpacing=80`, uppercase via text) | Compact uppercase eyebrows (`:249`) | 12 / SemiBold | 11/700 uppercase |
| `PegasusReferenceTextStyle` (`BodyStrongTextBlockStyle` with `FontFamily="Cascadia Mono, Consolas"`) | VRM plates, Case/PO, Image reference | 13 / SemiBold | `ui-monospace` 13/700 |

Tabular numerals: set `Typography.NumeralAlignment="Tabular"` on the
numeric styles (counts, dates, references, amounts) so columns align.
Tw Cen MT and Futura are never UI fonts and no brand-font bundle is loaded
(`:253-254`); the report renderer's fonts are a report concern
([07 · integrations](../07-integrations/README.md)).

## Spacing, density and layout

| Key | Value | Authority |
| --- | --- | --- |
| `PegasusSpace1` … `PegasusSpace9` (`x:Double`) | 4, 8, 12, 14, 18, 24, 32, 40, 64 | approved steps (`:271-275`) |
| `PegasusGutter` | 24 | primary gutters (`:277`) |
| `PegasusTableRowHeight` | 32 | 32px table rows (`:176`) |
| `PegasusFactRowHeight` | 28 | `.stitch` fact rows |
| `PegasusPanelPadding` | 12–16 (use 16 default, 12 dense) | `:176` |
| `PegasusContentMaxWidth` | 1280 | content region capped at 1280 and centred beside the rail (`:90-96`) |
| `PegasusRailWidth` | 236 | authenticated shell rail (`:65-68`) |
| `PegasusMinimumTargetSize` | 44 (pointer target), while keeping 32px visual rows via padding/hit-test | practical 44px targets (`:796`) |
| `PegasusMinimumWindowWidth` | 1280 (dense multi-pane); 1024–1279 reorders secondary content | `:279` |

Use only the steps a screen actually exercises. Section gaps 24 (max 40).
Above-the-fold contract at 1280×800 for single-record screens (`:176`).

## Shape, borders, focus, depth

| Resource | Value | Authority |
| --- | --- | --- |
| `ControlCornerRadius`, `OverlayCornerRadius` | `2` | primary radius 2px; "There is no second approved radius" (`:262-267`). The 6px/5px recorded in `site.css`/`.design-sync/conventions.md`/`.stitch/DESIGN.md` is a discrepancy flagged to the design owner; not adopted. |
| `PegasusBorderThickness` | `1` | 1px borders |
| `PegasusFocusVisualThickness` | `3` with `PegasusFocusBrush` | keyboard focus ring 3px rgba(219,8,22,.38) |
| `FocusVisualPrimaryBrush` / `FocusVisualSecondaryBrush` overrides | primary = `PegasusFocusBrush`, secondary = `PegasusPanelBrush` | visible focus everywhere |
| Depth | border-first; `ThemeShadow` only for flyouts (32) and dialogs (128) per WinUI guidance; no CTA shadows | rare soft shadows (`:266`); `winui-design` elevation guidance |
| Backdrop | none (solid `PegasusGroundBrush`); optional Mica behind title bar/rail only if contrast review passes | border-led, not decorative (`:162`); proposal §14.1 deviation recorded in the README |

## Icons, marks and logo

- Lucide is the only icon system (`:334-345`): 24×24 viewBox, 2px stroke,
  round caps/joins, rendered 16–24px, `currentColor`. The sixteen registered
  glyphs (`search`, `user`, `refresh-cw`, `clock`, `calendar`,
  `check-circle`, `alert-triangle`, `alert-circle`, `info`, `file-text`,
  `filter`, `shield`, `chevron-right`, `arrow-right`, `upload`, `lock`) are
  converted from the SHA-256-pinned sprite
  (`src/Pegasus.Web/wwwroot/images/lucide-sprite.svg`,
  `C81F0677…22BF1`) into `PathIcon` geometries in `Icons.Lucide.xaml`; a
  unit test records each glyph's source hash beside its key. Stroke rendering:
  `PathIcon` fills geometry, so convert the stroked path to an outlined path
  (Lucide provides outlined variants) or use a `Path` with `Stroke` and
  `StrokeThickness=2` inside a 24×24 `Viewbox` — DSK-06-03 picks the method
  that preserves the glyph shape and records it.
- A new icon needs a new registered glyph in the authority first (`:370`);
  no Segoe Fluent Icons substitution for an unregistered need.
- Decorative icons carry no accessible name (`AutomationProperties.AccessibilityView="Raw"`);
  an icon that conveys meaning carries `AutomationProperties.Name`.
- The fourteen commissioned raster marks (adopted 2026-08-17) are decorative
  surface art (30–112px) shipped as assets with empty accessible names; ten
  are in use (`pegasus-lockup`, `accounts`, `roles`, `access`,
  `organisations`, `principals`, `configuration`, `mailboxes`, `automation`,
  `checkmark`), four supplied and unplaced.
- Logo: `docs/design/brand/logos/logo_no_margin.png` (SHA-256 `E7247BE4…63A2`);
  never redrawn, recoloured or extracted from a screenshot; the desktop copy
  is verified by checksum in a unit test and used in the title bar/sign-in
  frame and the package assets (`Package.appxmanifest` visual assets are
  derived from it through a recorded mapping, see
  [09 · release](../09-release-update-and-distribution/README.md)).

## Control styles (shared)

| Control/pattern | Key points |
| --- | --- |
| Buttons | `AccentButtonStyle` = `PegasusAccentBrush` (one per region); default buttons = panel + 1px border; destructive verbs are default style inside a `ReasonDialog`, never red buttons outside it |
| `StatusChip` | tone (`Neutral`, `Pending`, `Review`, `Success`, `Blocked`) → background/foreground/border brushes above + Lucide glyph + text label; never colour alone |
| Table header | `PegasusBodyStrongTextStyle`, sort glyph, `AutomationProperties.HeaderStatus`-equivalent via `ItemsView`/`ListViewHeaderItem` patterns; 32px rows |
| Field | label above control; required marker styling on the label (`*` glyph with accessible "required" name) — never "Required." prose; validation text under the control associated by `AutomationProperties.DescribedBy` |
| `InfoBar` | severity Informational/Warning/Error mapped from problem type; message is the operator sentence; an expandable *Reference* row carries the correlation id as copyable text |
| `ContentDialog` | `ReasonDialog` template: title = named requirement; body = identity (name, count) and the approved consequence sentence when one exists; `TextBox` reason; primary = verb; secondary = Cancel; `DefaultButton=Close` for destructive actions so Enter does not destroy |
| Progress | thin indeterminate `ProgressBar` at the top of the content region or in the status bar; no ring spinners; honours `UISettings.AnimationsEnabled` with a static "Working" text equivalent |

## Gallery/debug page

`Pegasus.Desktop/Views/Developer/GalleryPage.xaml`, reachable only in
non-production channels (settings → Developer), renders: every brush swatch
with its key and hex per theme; every text style with sample text; spacing
scale; all button states; `StatusChip` per tone; a field in valid/invalid
states; a `ReasonDialog` launcher; an `InfoBar` per severity; the sixteen
glyphs; the logo; a table sample with sort/filter. It is the artifact for the
theme review screenshots (DSK-06-02) and the first place a token change is
seen.

## Change rule

Tokens here are derived, not owned. A proposed change (including the Dark
values and the section-heading size) is raised against
`docs/design/README.md` through its change and verification rule (`:982`),
reviewed on the gallery page in Light/Dark/HighContrast at 100% and 200%,
and only then applied to `Styles/`. The desktop never carries a second token
source; `site.css` remains the web's implementation of the same authority.
