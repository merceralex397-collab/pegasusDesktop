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

## Reviewer remediation checkpoint — 2026-08-29

Reviewer findings on 5729b454 were addressed in 2aa753d3. HighContrast now uses dynamic SystemColor...Color resources, focus thickness is the required Thickness type, and the source guard requires ThemeResource for authored color references. Post-fix ViewModel tests are 10/10, ArchitectureTests are 121/121, and the Release build is 0 warnings/0 errors. The new exact head awaits CI and independent re-review. The contrast-value conflict, missing dated design-owner decisions, and absent Dark/HighContrast gallery evidence remain honestly recorded blockers.

## Independent exact-head review — 2026-08-29

Archimedes reviewed exact SHA `2aa753d33ff73ba957e1ac1d3a808a312d8f0258` and returned **BLOCKED**. The local locked restore, Release build (0 warnings/0 errors), ViewModel tests (10/10), ArchitectureTests (121/121), packaged launch/UI inspection, and static token audit passed. Exact-head CI run `33259607789` subsequently completed green, including all three SQL shards and coverage.

Two code findings remain and are being remediated: the HighContrast muted-text brush uses the disabled-content `SystemColorGrayTextColor`, and the authored-XAML source guard does not inspect colour-bearing Setter/property-element values. Three product/authority decisions also remain open: Dark palette confirmation, dated 2px-radius supersession confirmation, and resolution of the documented contrast conflict. Dark/HighContrast gallery screenshots are explicitly parked for DSK-06-02 and are not claimed as evidence for this ticket.

## Code remediation — 2026-08-29

Commit `79f25d7c` remediates both code blockers from the independent review: HighContrast muted text now uses `SystemColorWindowTextColor`, and the authored-XAML guard covers colour-bearing Setter and property-element values with negative probes. Local ViewModel tests passed 12/12, the Release solution build passed with 0 warnings/0 errors, ArchitectureTests passed 121/121, and `git diff --check` passed. The branch is pushed; exact-head CI and a fresh independent review are pending. Product/authority decisions remain open and block merge/closeout.

## Exact-head review and CI

Archimedes independently reviewed exact commit 79f25d7ce4d490520747657db1895ce8df75aec0 and found no remaining code blockers. GitHub Actions run 33260338165 is tied to that exact SHA and completed successfully across documentation, local-development-scripts, changes, reference-data, unit, SQL shards 1-3, browser, and aggregate SQL coverage; infrastructure was skipped as designed.

DUI-001 remains blocked by the three open product questions recorded in open-questions.md: Dark-palette approval, the dated 2px-radius supersession, and the Light/Dark contrast-threshold conflict. The gallery screenshots/manual visual pass are explicitly deferred to DSK-06-02 and are not merge blockers.
