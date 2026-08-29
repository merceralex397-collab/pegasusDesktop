# Checklist — DUI-001 Theme resource dictionaries

- [ ] Read DSK-02-09's delivered file set and the token tables; reuse its merge order and guard test rather than creating new infrastructure.
- [ ] Transcribe each approved token into the existing Light, Dark and HighContrast dictionaries, using system colours only for HighContrast.
- [ ] Fill typography, spacing, shape and focus resources; keep the approved 2px radii and 3px focus treatment.
- [ ] Build and run the shell, exercise all three themes, and add the guard-test and screenshot evidence.
- [ ] Verify: `dotnet build --configuration Release` is warning-free.
- [ ] Verify: The existing `StylesAreTheOnlySourceOfColourAndType` test passes.
- [ ] Verify: Light, Dark and Contrast screenshots show legible panels and visible focus.
- [ ] Record the simplification pass and independent review in the plan before merge.

## Implementation progress — 2026-08-29

- [x] Read DSK-02-09's delivered-file state and token tables; DSK-02-09 artifacts were absent on `origin/dev`, so the permitted six-file set and single guard were supplied.
- [x] Transcribe the 30 colour keys into explicit Light, Dark and HighContrast dictionaries with system brush redirects in HighContrast.
- [x] Fill typography, spacing, shape and focus resources; retain the approved 2px radii and 3px focus treatment.
- [x] Build the desktop, launch it through `BuildAndRun.ps1`, and inspect the live packaged window.
- [x] Verify the guard, including a negative temporary raw-`FontSize` probe; focused guard 1/1, ViewModel suite 7/7, ArchitectureTests 121/121.
- [x] Verify the Release solution build with 0 warnings/errors and the one App.xaml theme merge.
- [ ] Verify three-theme visual screenshots: Light capture exists at `artifacts/ui/06-01-light.png`; Dark/HighContrast cannot be honestly captured against the current scaffold because it has no theme switch or delivered shell/gallery. This remains a visual-evidence limitation for review.
- [x] Simplification pass recorded in the plan.

## Review remediation — 2026-08-29

- [x] Add the documented PegasusFocusVisualThickness token and retain the platform thickness alias.
- [x] Place focus brush aliases in explicit Light/Dark/HighContrast theme dictionaries.
- [x] Extend the source guard to named colour attributes and composite numeric corner radii; negative probes cover both.
- [x] Rebuild and rerun validation after remediation: ViewModel tests 9/9, ArchitectureTests 121/121, Release build 0 warnings/0 errors.
- [x] Relaunch and inspect the packaged app after remediation; Light screenshot captured at artifacts/ui/06-01-light-after-review.png.
- [ ] Obtain exact-head CI and independent re-review for 5729b454; Dark/HighContrast visual evidence remains an explicit limitation of the current scaffold, not a claimed pass.

## Reviewer remediation checkpoint — 2026-08-29

- [x] Map HighContrast resources to dynamic SystemColor...Color values via SolidColorBrush.Color.
- [x] Define PegasusFocusVisualThickness as Thickness and retain the platform alias.
- [x] Reject non-ThemeResource color references in the authored-source guard and cover StaticResource with a negative probe.
- [x] Revalidate: ViewModel tests 10/10, ArchitectureTests 121/121, Release build 0 warnings/0 errors, packaged launch/UI inspection.
- [ ] Resolve the three open design/contrast questions and obtain a fresh exact-head independent review; PR head is now 2aa753d3.
- [ ] Capture the later shell/gallery Dark and HighContrast evidence under DSK-06-02; this is explicitly parked and not claimed for DUI-001.
