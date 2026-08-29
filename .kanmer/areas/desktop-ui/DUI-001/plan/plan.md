# Plan — DUI-001 Theme resource dictionaries

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can the desktop consume the authority tokens once, in all three themes, without creating a second palette or bypassing High Contrast? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Read DSK-02-09's delivered file set and the token tables; reuse its merge order and guard test rather than creating new infrastructure.
2. Transcribe each approved token into the existing Light, Dark and HighContrast dictionaries, using system colours only for HighContrast.
3. Fill typography, spacing, shape and focus resources; keep the approved 2px radii and 3px focus treatment.
4. Build and run the shell, exercise all three themes, and add the guard-test and screenshot evidence.

## Verification

- `dotnet build --configuration Release` is warning-free.
- The existing `StylesAreTheOnlySourceOfColourAndType` test passes.
- Light, Dark and Contrast screenshots show legible panels and visible focus.

## Risks and dependencies

DSK-02-09 owns the Styles set, App.xaml merge and guard. The two design-owner confirmations named in the ticket body remain external review inputs; do not silently choose a different palette or radius.

The implementation worktree must record its simplification pass and independent desktop review before merge.

## Implementation checkpoint — 2026-08-29

- DSK-02-09's Styles set, App.xaml merge, and named guard were absent on `origin/dev` (`66aa3eba`), so this ticket supplied exactly the six planned files and the single `StylesAreTheOnlySourceOfColourAndTypeTests` guard. No parallel Styles tree or second scanner was created.
- `src/Pegasus.Desktop/App.xaml` now merges `Pegasus.Theme.xaml` exactly once after `XamlControlsResources`; `Pegasus.Theme.xaml` preserves the planned five-token-file order. The existing scaffold's two `FontSize` literals were removed because the guard contract applies to all non-Styles XAML.
- Commit: `e8caad76` pushed to `origin/task/desktop-theme-resources`.
- The 30 colour keys are present in Light, Dark and HighContrast. HighContrast uses only system brush redirects; there is no Default dictionary, system-brush opacity, or HighContrastAdjustment=None. Shape/focus/spacing/typography resources match the token plan; the one planned raw FontSize remains only in the reference typography style where the WinUI mapping explicitly differs from the built-in base style.

## Verification checkpoint — 2026-08-29

- `dotnet restore .\Pegasus.slnx --locked-mode` — passed.
- `pwsh .\.codex\skills\winui-dev-workflow\BuildAndRun.ps1 .\src\Pegasus.Desktop\Pegasus.Desktop.csproj -SkipRun` — passed; 0 warnings, 0 errors.
- Same BuildAndRun command with `-Detach` — passed; the packaged app launched as `Pegasus.Desktop`, PID 39192. `winapp ui inspect -a 39192 --interactive --json` found the live window and controls. The process was stopped after capture.
- `dotnet test .\tests\Pegasus.Desktop.ViewModelTests\Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~StylesAreTheOnlySourceOfColourAndType` — passed 1/1 after the guard fix.
- Negative probe: temporarily reintroduced `FontSize="14"` in `MainPage.xaml`; the same guard failed naming `src/Pegasus.Desktop/MainPage.xaml: raw FontSize attribute`; the change was reverted and the guard passed 1/1.
- `dotnet build .\Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal` — passed; 0 warnings, 0 errors.
- `dotnet test .\tests\Pegasus.Desktop.ViewModelTests\Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build --no-restore` — passed 7/7.
- `dotnet test .\tests\Pegasus.ArchitectureTests\Pegasus.ArchitectureTests.csproj --configuration Release --no-build --no-restore` — passed 121/121.
- Static token audit — 30 keys in each theme and equal key sets; App merge count 1; no forbidden non-Styles XAML literals.
- A Light screenshot was captured at `artifacts/ui/06-01-light.png`. The current scaffold has no runtime theme switch and is not yet the delivered shell/gallery, so Dark and HighContrast screenshot evidence is intentionally not claimed here. The screenshot demonstrates a real packaged launch only; it does not substitute for the later shell/gallery visual pass.

## Simplification pass — 2026-08-29

- Reused the existing App resource pipeline, test project, and scaffold; added no new package or abstraction.
- Kept the Styles set to the six names owned by this ticket and used the guard only because DSK-02-09 had not landed.
- Replaced the tempting HighContrast translucent custom brush with the existing system highlight brush because the HC rule forbids opacity on system brushes.
- Excluded generated `bin/obj` XAML from the source guard so build output is not mistaken for authored source; the guard still scans every authored desktop XAML file outside `Styles/`.
- No further behavior-preserving simplification identified.

The Dark palette remains the documented starting assumption and the authority's 2px radius is adopted; no new design value was invented. Review must confirm the design-authority checklist and carry the screenshot limitation honestly.

## Review remediation checkpoint — 2026-08-29

- Independent review blocked the first PR head on three concrete issues: the documented token name was absent, focus aliases were outside theme dictionaries, and the source guard missed named colour/composite corner-radius literals.
- Commit 5729b454 resolves those findings: Tokens.Focus.xaml now defines canonical PegasusFocusVisualThickness and aliases the platform key to it inside the explicit Light/Dark/HighContrast theme dictionaries; the guard recognizes named colour attributes and composite numeric CornerRadius values and has two negative probes.
- Post-fix validation: ViewModel suite 9/9; Release solution build 0 warnings/0 errors; ArchitectureTests 121/121; git diff --check passed; static audit still reports 30 equal keys per theme, one App merge, and no forbidden authored literals.
- Post-fix BuildAndRun.ps1 -Detach launched the packaged app as Pegasus.Desktop (PID 115896); winapp ui inspect --interactive succeeded and screenshot artifacts/ui/06-01-light-after-review.png was captured. PID 115896 was stopped after capture.
- PR #41 is now at exact head 5729b454, pushed to origin/task/desktop-theme-resources; CI and independent re-review remain pending. Dark/HighContrast screenshot evidence remains explicitly deferred because the current scaffold has no runtime theme switch or delivered shell/gallery; no Tier-7 pass is claimed for those states.

## Reviewer remediation checkpoint — 2026-08-29

- Microsoft Learn verification confirmed that FrameworkElement.FocusVisualPrimaryThickness is a Thickness and that High Contrast system resources use SystemColor[name]Color keys. Commit 2aa753d3 changes PegasusFocusVisualThickness to Thickness and maps each HighContrast SolidColorBrush through Color={ThemeResource SystemColor...Color}.
- The guard now permits only ThemeResource color references in authored non-Styles XAML and has a negative StaticResource probe. Focused ViewModel tests pass 10/10; ArchitectureTests pass 121/121; Release solution build passes with 0 warnings/0 errors; the packaged app launches and is UI-inspectable after these changes.
- New PR head: 2aa753d33ff73ba957e1ac1d3a808a312d8f0258, pushed to origin/task/desktop-theme-resources. The earlier CI run was green for 5729b454 and must be rerun at this new head.
- Remaining blockers are not code guesses: the authoritative Light/Dark token values conflict with the stated contrast thresholds, the required dated design-owner confirmations are not attached, and the current scaffold cannot produce the required Dark/HighContrast gallery screenshots. These are recorded in open-questions; the screenshot work is parked for DSK-06-02, while the decisions remain unchecked and block closeout.

## Review remediation checkpoint — 2026-08-29 (Archimedes exact-head review)

- Independent `pegasus-desktop-reviewer` review of exact SHA `2aa753d33ff73ba957e1ac1d3a808a312d8f0258` returned **BLOCKED**.
- Exact-head CI run `33259607789` is now green: documentation, changes, local-development-scripts, reference-data, unit, browser, SQL shards 1/2/3, and SQL coverage passed; infrastructure was correctly skipped.
- Code findings to remediate before merge: map HighContrast `PegasusMutedTextBrush` away from `SystemColorGrayTextColor` (reserved for disabled content), and extend the authored-XAML guard to inspect colour-bearing `Setter` values and property-element values.
- Product/authority blockers remain: obtain dated design-owner confirmation of the Dark palette; obtain dated confirmation that the authority's 2px radius supersedes historical 5px/6px values; resolve the documented Light/Dark contrast conflict by corrected tokens or a named approved exception.
- The reviewer accepts the Dark/HighContrast gallery screenshot limitation as explicitly parked for DSK-06-02; it is not a merge blocker, but Tier-7 evidence is not claimed for this ticket.
- Next: apply the two code fixes, run focused/full validation, push a new exact head, and obtain a fresh independent review. Do not merge or close while the three open questions remain unchecked.

## Code remediation checkpoint — 2026-08-29

- Applied Archimedes' two code fixes at commit `79f25d7c` on `origin/task/desktop-theme-resources`.
- HighContrast `PegasusMutedTextBrush` now maps to dynamic `SystemColorWindowTextColor`; it no longer uses the disabled-content `SystemColorGrayTextColor`.
- `StylesAreTheOnlySourceOfColourAndTypeTests` now scans colour-bearing `Setter` attributes and direct colour property elements; negative probes cover both named literals. ThemeResource-only handling remains centralized in `FormatColourViolation`.
- Local validation after the fix: ViewModel tests 12/12, Release solution build 0 warnings/0 errors, ArchitectureTests 121/121, and `git diff --check` passed.
- New exact-head CI and fresh independent review are pending. The three design-owner questions remain unchecked and block merge/closeout.
