# Post-implementation report — DUI-005

## Implementation

- Added `Pegasus.Desktop.Presentation.OperatorText` as the desktop presentation boundary.
- Business vocabulary delegates to the single shared `Pegasus.Contracts.Vocabulary.OperatorVocabulary` owner.
- Added the missing explicit `FeeNote` entry to that shared owner after independent inspection.
- Added Europe/London formatting with UTC fallback labelling, invariant count formatting, and one-decimal MB formatting.
- Changed the scaffold counter's XAML-facing property from `int` to formatted `string`.
- Added reflection and theory coverage for mapped enums, raw values, identifier entry, Target/reference identifiers, date/time, numeric formatting, and banned operator wording.
- Added only the required direct project references and updated the test lock file.
- Updated `docs/desktop/06-ui-design/README.md` and `docs/desktop/05-implementation-and-migration/reuse-map.md`.

## Validation

- `dotnet restore ./Pegasus.slnx --locked-mode`: passed; all projects up to date.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false --nologo`: passed; 0 warnings, 0 errors.
- `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`: passed; Debug x64 WinUI build, 0 warnings, 0 errors.
- `dotnet test tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-restore --logger "console;verbosity=minimal"`: passed; 63 passed, 0 failed, 0 skipped, including after the FeeNote fix.
- `rg -n "\\.ToString\\(\\)|ToLocalTime\\(\\)|DateTime\\.Now" src/Pegasus.Desktop --glob "!**/obj/**"`: only three diagnostic serialization calls in `Logging/DiagnosticsLoggerProvider.cs`; no display-path matches in ViewModels/Presentation.
- `git diff --stat origin/dev -- src/Pegasus.Worker`: empty.
- Independent `codex review --commit 2ddb878b`: no actionable regressions identified. The follow-up FeeNote fix is a one-line explicit shared-map correction and is included in the pushed PR head `ec6c080e`.

## Acceptance disposition

- Shared vocabulary, raw-value guards, date/time rules, unmapped enum-set guards, banned-word checks, and current-surface identifier/Target checks are implemented and validated.
- The current desktop scaffold contains no identifier input or Target/reference column, so no product picker screen was invented. The guard test fails if a future surface exposes a typed identifier or raw aggregate identifier.
- No clean-machine UI/install evidence is claimed; that is outside this Tier 1 static/build/architecture ticket.

## Review handoff

PR #54 is ready for exact-head CI and independent review of the final head `ec6c080e`. Merge and Kanmer proof remain outstanding.

## Exact-head CI and review completion — 2026-08-30

- PR #54 exact head: `ec6c080e65bd25e853fce85b581afb625394b87f`.
- Repository-check run `33290083605` is green after rerunning SQL shard 3 job `99200136610` (rerun job id `99202262331`), which had timed out once at the configured 20-minute limit without an assertion failure. The rerun passed, as did SQL shards 1 and 2, SQL coverage, unit, browser, documentation, changes, reference-data, and local-development-scripts; infrastructure was skipped by path rules.
- Independent review of the final head found no actionable defect. Merge and merged-main proof remain outstanding.
