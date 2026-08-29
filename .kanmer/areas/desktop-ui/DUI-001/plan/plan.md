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
