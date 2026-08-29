# Post-implementation report — DUI-010

## Result

Implemented through remediation commit `681f6f16e66ba9e96e20e3cf6d1ede63f7344db4` on branch `task/desktop-problem-infobar`, following the initial implementation `16d40759eb0a2fda1d12e45fbc184cc9267f778a` and review remediation `a98ad1fe0ff21e0c8709925e09ec046c1f05e26c`.

The change adds one reusable native `ProblemInfoBar` control and one `ProblemPresentation` mapping table. It consumes the existing `Pegasus.Contracts.ProblemDetails.PegasusProblem` and `PegasusProblemTypes` contract without changing gateway code. The control renders a page-local Polite `InfoBar`, maps every current gateway type to an exact severity and operator sentence, exposes an optional `Reference`, and copies only that value through `DataPackage.SetText`. Ordinary `MainPage` no longer displays synthetic failure states.

## Remediation

The final remediation separates the Problem and AutomationIdPrefix callbacks so ID changes cannot hide an active problem, closes/reopens only for a new ProblemPresentation, binds accessibility names to the centralized Reference label, removes the unused startup sample layout, and extends the static XAML operator-string guard to single- and double-quoted attributes and gateway problem-type values.

## Validation

Commands run in the ticket worktree:

- `dotnet restore .\\Pegasus.slnx --locked-mode` — passed.
- `dotnet build .\\src\\Pegasus.Desktop\\Pegasus.Desktop.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false` — passed with 0 warnings and 0 errors.
- `dotnet test .\\tests\\Pegasus.Desktop.ViewModelTests\\Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-restore --logger 'console;verbosity=minimal'` — passed, 22/22.
- `git diff --check` — passed.

## Acceptance status

The mapping, exact severity assertions, centralized operator-string guard, Reference-only copy behaviour, page-local InfoBar structure, Polite setting, and no-toast/no-modal shape are implemented and locally evidenced.

The repository does not contain `tests/Pegasus.Desktop.UITests/problem-tests.ps1`. Therefore the prescribed scripted UI test, replacement-announcement runtime capture, 200% scale check, keyboard walkthrough, and full Dark/High Contrast screenshot/manual sweep are not claimed. The earlier direct UIA clipboard run belongs to the superseded three-sample startup page and is retained only as historical evidence, not current acceptance. [[DUI-002]]/[[TEST-006]] own the missing gallery/harness path.

No Azure, cloud, upstream, corpus, or deployment state was changed. Fresh independent review of commit `681f6f16` is pending before any PR is opened.
