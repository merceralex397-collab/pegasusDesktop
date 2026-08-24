# Checklist — FND-044: Native login screen and the session-failure matrix

One box per plan step, in plan order. Tick a box only when the thing it names is true in the
worktree.

- [ ] **Orient.** Read the area 04 *Session failure matrix* and `docs/desktop/06-ui-design/screen-specs.md:83-113` in full; confirm [[FND-043]]'s `SessionFailure` exists (`ls src/Pegasus.Desktop/Session`), and stop if it does not.
- [ ] **Take the ticket.** `get_doc_gates FND-044`, `take_ticket FND-044`, branch `task/desktop-login-screen` created from `origin/dev`.
- [ ] **Load `winui-design` and search before writing XAML.** Run `winui-search.exe` for the sign-in form controls, and read the skill's *XAML landmines* (`SKILL.md:82-138`) and *Theming rules* (`:140`) sections — `x:Bind` defaults to `OneTime`, which is why a state change would otherwise not repaint.
- [ ] **Add `Session/SignInPage.xaml` + `.xaml.cs` + `SignInViewModel.cs`** under a `Session/` capability folder: navless frame, logo plus **company** name, User name and Password (label + control only), primary Sign in, and no "forgot password".
- [ ] **Set the four sign-in AutomationIds exactly**: `SignIn.UserName`, `SignIn.Password`, `SignIn.Submit`, `SignIn.Problem`.
- [ ] **Implement the four inline states** — idle; signing in (submit disabled, thin **indeterminate** progress); invalid credentials; rate limited — as view-model state bound to an `InfoBar`, with **no `ContentDialog` for any routine state**.
- [ ] **Give every state a non-colour cue** (icon plus distinct text), so no state is conveyed by severity colour alone.
- [ ] **Reuse the three settled strings word for word** from `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:27`, `:31` and `:73-74`, having read the comment at `:21-23` first; add no sentence explaining what the system did.
- [ ] **After an invalid attempt, return focus to User name and clear the password box.**
- [ ] **Map `RateLimited`**: "Try again in a minute", submit disabled until the `Retry-After` seconds elapse **and then actually re-enabled**.
- [ ] **Map `AccountDisabled`**: the disabled-access sentence with no retry loop.
- [ ] **Map `PasswordChangeRequired`**: navigate to a Change password routing target carrying `Password.Current`, `Password.New`, `Password.Confirm`, `Password.Save`, with minimum length shown only as a validation outcome and never as hint text.
- [ ] **Map `Unreachable`**: a connectivity sentence that is never an invalid-credentials message.
- [ ] **Add the Update required / Blocked page**: full-window, rail-less, title "Update required", current and minimum versions rendered as values, primary "Update now", secondary "Sign out"; AutomationIds `Update.Required.Now`, `Update.Required.SignOut`, `Blocked.Reason`.
- [ ] **Make the Blocked variant drop "Update now"** and show the operator sentence plus "Sign out" only.
- [ ] **Wire "Update now"** to [[FND-045]]'s update entry point — or, if that ticket has not landed, to a command that logs and disables itself, recorded in the plan.
- [ ] **Apply the keyboard rules**: `Tab` order User name → Password → Sign in; `Enter` submits from either field; the error message is **associated with** the fields; the focus visual is the 3px `PegasusFocusBrush` ring with forced colours falling back to the system focus visual.
- [ ] **Write one view-model test per matrix row** plus idle and signing-in, asserting state, **message identity** (not a substring) and submit-enabled, with no `DispatcherQueue` and no new fake beyond [[FND-038]]'s.
- [ ] **Build and launch asynchronously** with `.codex/skills/winui-dev-workflow/BuildAndRun.ps1`, and record the printed app PID.
- [ ] **Contribute seven `Test-UI` cases to the one harness** `tests/Pegasus.Desktop.UITests/ui-tests.ps1`, using `winapp ui wait-for` / `get-value` and **never `Start-Sleep`**; if the file does not exist, create it with exactly `param([Parameter(Mandatory)][int]$AppPid)` and the `Test-UI` helper, and record that [[TEST-006]] owns the skeleton.
- [ ] **Capture seven `winapp ui screenshot` states** — idle, signing in, invalid credentials, rate limited, account disabled, update required, disconnected — into `artifacts/ui-tests/`.
- [ ] **Run the `winui-ui-testing` accessibility audit** over the sign-in and update-required windows; treat any interactive control without an `AutomationId` as a defect and fix it.
- [ ] **Do and record the manual pass** `docs/design/README.md:1300` requires: keyboard-only sign-in, screen-reader announcement, focus/error behaviour, forced colours, reduced motion, 200% zoom.
- [ ] **Leave FRD-13 to [[FND-008]]** — do not create `docs/frd/frd-13-desktop-operator-experience.md`; record the dependency in the plan, and touch `screen-specs.md` only as a reasoned correction.
- [ ] **Run the simplification pass** over this branch's own diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] **Verification / proof.** Run `dotnet test tests/Pegasus.Desktop.ViewModelTests`, `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid>`, `winapp ui inspect -a <pid> --interactive`, `grep -rn '#FF\|Color="#' src/Pegasus.Desktop/Session` (no matches), `dotnet test tests/Pegasus.ArchitectureTests` and `dotnet build Pegasus.slnx -c Release` (`0 Warning(s)`); attach the seven screenshots and the recorded manual pass as `visual` and `command-log` proof; state in it whether [[FND-045]] had landed, whether this ticket created the `ui-tests.ps1` skeleton, and that FRD-13 does not exist yet. Open the PR into `dev`.

## Progress notes
