# Research — FND-044: what the native login screen must say, show and never do

## Question

What exactly must the native WinUI 3 sign-in screen render for each of the seven
session-failure conditions, which operator words are already settled and must be reused
rather than rewritten, and where does this ticket's UI surface stop and another ticket's
begin?

## Current behaviour

**The parity matrix covers this, and the rows are `PAR-01`, `PAR-03` and `PAR-04`.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds 46 rows
(`grep -c '^| PAR-' …` → `46`), each keyed to a page model under `src/Pegasus.Web/Pages/**`.

- **`PAR-01` (§13.1 Access and session, FRD-04)** — `Account/SignIn.cshtml.cs` (106 lines).
  Its desktop-target column reads "Login screen (area 04): username/password, connectivity
  and update-required states", which is precisely this ticket.
- **`PAR-03`** — `Account/PasswordChange.cshtml.cs` (189 lines). Desktop target:
  "Password-change dialog; forced-change state routed before shell (proposal §8.4 'password
  reset required')". This ticket **routes to** that screen and adds its four AutomationIds;
  the change-password behaviour itself is [[FEAT-021]]'s (plan handle `DSK-05-21`).
- **`PAR-04`** — `Account/AccessDenied.cshtml.cs` (7 lines), "Static". Desktop target: the
  "denied" problem state in the area 06 UI state contract.

What the Razor page does today, read directly, is the source of the copy this screen must
reuse:

| What | Where | Exact value |
| --- | --- | --- |
| Username field label and required message | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:25-27` | `[Display(Name = "Username")]`, `[Required(ErrorMessage = "Enter your username.")]`, `StringLength(256)` |
| Password field label and required message | `:29-31` | `[Display(Name = "Password")]`, `[Required(ErrorMessage = "Enter your password.")]`, `StringLength(256)` |
| Why those messages are explicit | `:21-23` (the file's own comment) | the framework default names the bind property — "The UserName field is required." — "which is a C# identifier, not a word the operator has ever seen on this screen" |
| Invalid-credential sentence | `:73-74` | "The username or password is incorrect. If your access has changed, contact an administrator." |
| Rate limit | `:13` | `[EnableRateLimiting("StaffSignIn")]`, 10 attempts per client per minute (`src/Pegasus.Core/Actors/StaffSessionPolicy.cs:12`) |
| Forced password change | `:83-85` | `MustChangePassword` → `RedirectToPage("/Account/PasswordChange")` |

## Findings

- **The screen spec is binding and already names every control.**
  `docs/desktop/06-ui-design/screen-specs.md:85-97` § *Sign in* fixes the frame ("navless
  frame with the logo and the company name (not the product; `_LayoutAuth` convention)"), the
  fields ("User name, Password (label + control only)"), the primary action, the explicit
  absence of "forgot password" ("not a current capability"), the eight states, and the four
  AutomationIds `SignIn.UserName`, `SignIn.Password`, `SignIn.Submit`, `SignIn.Problem`.
  - The state list at `:89-95` is the same seven matrix rows plus `idle`, and it already
    resolves each one: rate limited → problem `sign_in_rate_limited` → "Try again in a
    minute"; server unreachable → "connectivity sentence, **not** an invalid-credentials
    message"; client unsupported → the Update required screen.
- **The Update required / Blocked screen is specified separately and in the same file.**
  `screen-specs.md:99-106`: full-window, no rail, title "Update required", "the current and
  minimum versions as values", one primary "Update now" (opens the App Installer update) and
  a secondary "Sign out"; the **Blocked** variant — account disabled or compatibility
  fail-closed — shows the operator sentence and "Sign out" only. AutomationIds
  `Update.Required.Now`, `Update.Required.SignOut`, `Blocked.Reason`.
- **The Change password screen is also specified, and this ticket only routes to it.**
  `screen-specs.md:108-113`: Current / New / Confirm plus Save, with the rule that "minimum
  length is shown only as a validation outcome, never as hint text", and AutomationIds
  `Password.Current`, `Password.New`, `Password.Confirm`, `Password.Save`.
- **The keyboard baseline is a separate document with its own numbers.**
  `docs/desktop/06-ui-design/keyboard-and-accessibility.md:1-7` states the three
  non-negotiables — every critical workflow completable by keyboard alone, every state
  perceivable without colour, every control reachable by UI Automation — and `:39-42` fixes
  the focus visual as "3px Collision-red ring (`PegasusFocusBrush`, authority `:264`)" with
  forced-colours mode using the system focus visual. `:24` gives the global `Tab` order
  (title bar → rail → page header → content → status bar), which the navless login frame
  reduces to User name → Password → Sign in.
- **"Stop explaining pages" is an operator direction, not a style preference.**
  `docs/design/README.md:424` records it verbatim, dated 2026-08-20. It is why the
  invalid-credential sentence is one sentence with an action ("contact an administrator") and
  no description of what the system did.
- **The accessibility obligations are enumerated, not implied.**
  `docs/design/README.md:774-792` lists them, and the four that bite on this screen are
  "labelled navigation", "associated field errors and error summaries", "visible focus" and
  "non-colour state cues". `:1300` adds the recording obligation: "keyboard, screen-reader,
  focus/error, forced-colours, reduced-motion, 1280+ desktop, constrained desktop and
  200%-zoom inspection **must be recorded**".
- **The UI-test harness has an owner and a pinned signature, and this ticket is a
  contributor.** The ticket body fixes it: `tests/Pegasus.Desktop.UITests/ui-tests.ps1`
  carries `param([Parameter(Mandatory)][int]$AppPid)` — **never `$Pid`, which is read-only in
  PowerShell** — and a `Test-UI` helper that counts pass/fail. [[TEST-006]] (plan handle
  `DSK-08-06`) owns the skeleton. `ls tests/` returns exactly `Pegasus.ArchitectureTests`,
  `Pegasus.Core.Tests` and `Pegasus.IntegrationTests` (2026-08-24), so neither the folder nor
  the file exists yet; if [[TEST-006]] has not landed, this ticket creates the file **with
  exactly that signature and helper** so the two cannot fork.
- **There is a real ordering hazard on "Update now".** The update entry point is
  [[FND-045]]'s (plan handle `DSK-04-09`) startup orchestrator. The body already resolves it:
  if [[FND-045]] has not landed, bind the command to something that logs and disables itself,
  and say so in the plan. That is a recorded degradation, not a placeholder to forget.
- **Nothing in this ticket touches the gateway, and the Razor page stays live.** The
  Guardrails say the web sign-in page stays until cutover, so `src/Pegasus.Web/Pages/Account/`
  is read-only evidence here — the source of the copy, not a file to edit.
- **The seven failure values are [[FND-043]]'s closed enum.** `AccessTokenExpired`,
  `RefreshRevoked`, `AccountDisabled`, `PasswordChangeRequired`, `ClientUnsupported`
  (carrying `minimumVersion`), `Unreachable`, `RateLimited` (carrying `Retry-After`). This
  screen switches on them and adds none.

### Facts

| Fact | Source |
| --- | --- |
| Sign-in field labels and the two required-field messages | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:25-31` |
| The comment explaining why those messages are explicit | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:21-23` |
| The invalid-credential sentence, word for word | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:73-74` |
| `[EnableRateLimiting("StaffSignIn")]` on the page | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:13` |
| 10 sign-in attempts per client per minute | `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:12` |
| `MustChangePassword` redirects to `/Account/PasswordChange` | `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:83-85` |
| Sign-in screen spec: frame, fields, states, four AutomationIds, no "forgot password" | `docs/desktop/06-ui-design/screen-specs.md:85-97` |
| Update required / Blocked spec and its three AutomationIds | `docs/desktop/06-ui-design/screen-specs.md:99-106` |
| Change password spec and its four AutomationIds | `docs/desktop/06-ui-design/screen-specs.md:108-113` |
| Keyboard/accessibility baseline: keyboard-only, non-colour, UI-Automation-reachable | `docs/desktop/06-ui-design/keyboard-and-accessibility.md:1-7` |
| Focus visual is a 3px `PegasusFocusBrush` ring; forced colours uses the system visual | `docs/desktop/06-ui-design/keyboard-and-accessibility.md:39-42` |
| "stop explaining pages", operator direction 2026-08-20 | `docs/design/README.md:424` |
| Accessibility required behaviours (labelled navigation, associated field errors, visible focus, non-colour state cues) | `docs/design/README.md:774-792` |
| Inspection evidence must be **recorded**, and automated results do not prove acceptance | `docs/design/README.md:1300-1302` |
| FRD-12 requires the full state vocabulary and keyboard/screen-reader/200%/forced-colour/reduced-motion support | `docs/frd/frd-12-operator-experience.md:20-25` |
| Parity rows `PAR-01`, `PAR-03`, `PAR-04` | `docs/desktop/01-inventory-and-parity/parity-matrix.md:46,48,49`; `grep -c '^| PAR-'` → 46 |
| `tests/` holds three projects; `Pegasus.Desktop.UITests` does not exist yet | `ls tests/`, 2026-08-24 |
| `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended` | `Directory.Build.props:6-7` |

### Assumptions

- **A-04-08-1 — [[FND-034]]'s (plan handle `DSK-02-09`) theme dictionaries expose
  `PegasusFocusBrush` and the InfoBar severity brushes by resource key.** Confirmed by:
  building the page and running the `grep` for colour literals in the verification list.
  *If wrong*, the missing keys are a defect in [[FND-034]]'s ticket, not a licence to write a
  colour literal here.
- **A-04-08-2 — a WinUI `InfoBar` bound to a view-model property is announced by a screen
  reader when its `IsOpen` flips.** Confirmed by: `microsoft_docs_search` for `InfoBar`
  semantics plus the manual screen-reader pass in step 12. *If wrong*, the fallback is an
  explicit live-region announcement — `docs/design/README.md:789` asks for "restrained live
  announcements", so an always-announcing region is not the answer either.
- **A-04-08-3 — `winapp ui wait-for` / `get-value` can read an `InfoBar`'s message text by the
  AutomationId on its parent.** Confirmed by: the first `ui-tests.ps1` run. *If wrong*, put
  the AutomationId on the text element itself and record the change against
  `screen-specs.md:96-97`, which names `SignIn.Problem` without fixing which element carries
  it.
- **A-04-08-4 — [[TEST-006]] (plan handle `DSK-08-06`) has not yet created
  `tests/Pegasus.Desktop.UITests/ui-tests.ps1`.** Measured true on 2026-08-24 by `ls tests/`.
  *If it has landed by implementation time*, contribute cases to the existing file and change
  neither its signature nor its helper.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3 (`:169-176`), answered. The
responsibility being placed is **presenting the sign-in and blocked-state experience to the
operator, and deciding which of the seven failure states is shown**.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | A login screen is one operator at one workstation. The shared state behind it — the account, its roles, its enabled flag, the minimum client version — is the gateway's and is untouched here; this screen renders what [[FND-043]]'s `SessionFailure` reports. |
| Unattended execution — must it run with every desktop closed? | **No** | It exists only when a person is looking at it. Nothing on this screen runs without a session. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | The screen holds a password only in the `PasswordBox` for the duration of one attempt, clears it after an invalid attempt (`screen-specs.md:90-91`), and persists nothing. There is deliberately no "remember me" (proposal § 8.2; the ticket's Guardrails). |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing calls in, and nothing calls out to a browser: plan 04 § 3 decision 1 keeps the login native with no authorization-code round trip, so no redirect URI exists. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes — and it lands on the existing `Pegasus.Web` gateway under L-01, not in Azure.** | The screen must never be the thing that decides access. `docs/frd/frd-04-parties-accounts-and-access.md:25` puts authorization in Core and at every caller boundary, failing closed; the disabled-account and client-unsupported states shown here are **reports** of a gateway decision made by [[GWY-021]] (plan handle `DSK-04-04`) and [[GWY-023]] (`DSK-04-06`). A screen that hid a control would change nothing about what the gateway allows — which is the point. |
| Measured operational advantage — measured evidence central is materially better? | **No** | The opposite is the whole conversion thesis: a native screen removes the browser round trip proposal § 8.1 rules out. No measurement supports rendering login server-side. |

One "yes", naming the **existing gateway** under L-01. Nothing lands in Azure. The only stack
this ticket runs against is the local Test/UAT one (L-02).

## Implications

1. **Copy is reused, not written.** Three strings are already settled and must match word for
   word: "Enter your username.", "Enter your password." and the invalid-credential sentence.
   Writing a friendlier version is a defect under `docs/design/README.md:424`.
2. **Eight states, one screen, no dialogs.** Idle, signing in, invalid credentials and rate
   limited are inline states on the sign-in page; password-change-required, client-unsupported
   and blocked are **routed** screens; unreachable is an inline connectivity sentence. The
   plan row's "no modal for routine states (InfoBar)" is the rule.
3. **The AutomationIds are a contract with two other tickets.** [[TEST-006]]'s harness and
   [[DUI-015]]'s (plan handle `DSK-06-15`) AutomationId coverage audit both read them, so the
   eleven ids in the body must be spelled exactly as `screen-specs.md:96-97`, `:105-106` and
   `:113` give them.
4. **The most likely defect is the one the spec calls out twice**: showing a transport failure
   as a credential failure. Both look like "you cannot sign in", and only a named test
   distinguishes them.
5. **Tier 7 is not satisfied by automation.** `docs/design/README.md:1300-1302` requires the
   keyboard, screen-reader, focus/error, forced-colours, reduced-motion and 200%-zoom
   inspections to be **recorded**, and says automated results do not prove acceptance. Steps
   11–12 must produce artefacts, not a green log line.
6. **Two dependencies degrade gracefully and both must be stated.** If [[FND-045]] has not
   landed, "Update now" logs and disables itself; if [[TEST-006]] has not landed, this ticket
   creates the harness skeleton to the pinned signature. Neither is a silent workaround.

## Open questions

None. Every undecided item is owned by a **named sibling ticket** — the `SessionFailure`
values by [[FND-043]] (plan handle `DSK-04-07`), the update entry point by [[FND-045]]
(`DSK-04-09`), the UI-test harness skeleton by [[TEST-006]] (`DSK-08-06`), the theme
dictionaries by [[FND-034]] (`DSK-02-09`), the change-password behaviour by [[FEAT-021]]
(`DSK-05-21`), and FRD-13 by [[FND-008]] (`DSK-00-08`) — which makes each a scope boundary
recorded in the plan's *Risks / open questions* section. The four assumptions above are
settled by a build, a documentation query or one `ls`. No `open-questions` document is
created.
