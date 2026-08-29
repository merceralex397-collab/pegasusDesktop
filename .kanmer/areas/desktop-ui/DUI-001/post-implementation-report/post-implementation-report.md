# Post-implementation report — DUI-001

## Delivered

- Added the six planned resource dictionaries under `src/Pegasus.Desktop/Styles/`: colours, typography, spacing, shape, focus, and the ordered `Pegasus.Theme.xaml` merge.
- Added the single `StylesAreTheOnlySourceOfColourAndTypeTests` guard because the owning DSK-02-09 guard was absent on the branch.
- Added the one permitted caller cleanup: removed two pre-existing raw `FontSize` attributes from `MainPage.xaml` so the guard passes over authored desktop XAML.
- Merged `Pegasus.Theme.xaml` exactly once after `XamlControlsResources`.
- No new dependency, deployment unit, Azure write, upstream sync, or second palette owner was introduced.

## Validation

- BuildAndRun `-SkipRun`: passed with 0 warnings and 0 errors.
- BuildAndRun `-Detach`: packaged app launched as `Pegasus.Desktop` (PID 39192); live UI inspection succeeded.
- Release solution build: passed with 0 warnings and 0 errors.
- ViewModel tests: 7/7 passed.
- Styles guard: 1/1 passed; negative raw-`FontSize` probe failed as intended before reversion.
- Architecture tests: 121/121 passed.
- Static audit: all 30 colour keys are present in all three theme dictionaries with equal key sets; no Default dictionary, forbidden High Contrast opacity, or authored non-Styles colour/font-size/numeric-corner-radius literal remains.
- Light screenshot: `artifacts/ui/06-01-light.png`.

## Evidence boundary

The current scaffold has no runtime theme switch and is not yet the delivered shell/gallery. Therefore Dark and HighContrast screenshots and the full manual contrast/focus review are not claimed. This is recorded as an evidence limitation, not treated as a pass. The documented Dark starting palette and authority-approved 2px radius were used without inventing new values.

## Review remediation — 2026-08-29

The independent review findings on the initial head were corrected in 5729b454. The canonical PegasusFocusVisualThickness token is present, focus aliases are theme-dictionary scoped, and the guard now rejects named colour attributes and composite numeric corner radii. Post-fix ViewModel tests passed 9/9, the Release solution build passed with 0 warnings/0 errors, and ArchitectureTests passed 121/121. A fresh packaged launch (PID 115896) was UI-inspected successfully and produced artifacts/ui/06-01-light-after-review.png; the process was stopped after capture. PR #41 now awaits exact-head CI and re-review. Dark/HighContrast screenshot and manual contrast evidence remains unclaimed because the current scaffold has no runtime theme switch or delivered shell/gallery.
