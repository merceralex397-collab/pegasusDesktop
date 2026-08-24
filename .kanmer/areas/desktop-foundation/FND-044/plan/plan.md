# Plan — FND-044: Native login screen and the session-failure matrix

**Diff estimate: ~11 files, ~780 lines.** Derived from the files document file by file:
`Session/SignInPage.xaml` ~90 and `.xaml.cs` ~25; `Session/SignInViewModel.cs` ~150 (eight
states, the seven-value switch, the `Retry-After` countdown that re-enables submit);
`Session/UpdateRequiredPage.xaml` ~70 + `.xaml.cs` ~20 + its view model ~60 (two variants —
Update required and Blocked — sharing one page); `Session/ChangePasswordPage.xaml` ~55 +
`.xaml.cs` ~15 as a routing target carrying its four AutomationIds; navigation/DI
registration +~12; `tests/Pegasus.Desktop.ViewModelTests/Session/SignInViewModelTests.cs`
~190 (seven matrix rows plus idle and signing-in, ~20 lines each); and
`tests/Pegasus.Desktop.UITests/ui-tests.ps1` +~90 for seven `Test-UI` cases (plus ~40 more if
this ticket also has to create the pinned skeleton). `docs/engineering.md:201` § Plan sizing
requires the estimate first.

## Approach

**Build one sign-in page with four inline states and route the other three conditions to
their own full-window screens, driving everything from [[FND-043]]'s seven-value
`SessionFailure` enum and rendering messages in an `InfoBar` rather than a dialog.** The
alternative rejected is **one screen with a `ContentDialog` per failure**: it is the reflex,
and the plan row forbids it in as many words — "no modal for routine states (InfoBar)" —
because a modal steals focus, breaks the keyboard journey
`docs/desktop/06-ui-design/keyboard-and-accessibility.md:1-7` requires, and turns a routine
rate-limit into an interruption. The second alternative rejected is **writing fresh operator
copy**: three strings are already settled in `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs`
(`:27`, `:31`, `:73-74`) and the file's own comment at `:21-23` records why they are worded as
they are; a friendlier rewrite would make desktop and web disagree while the Razor page is
still live. The third alternative, **porting `_LayoutAuth.cshtml`**, is ruled out by
`screen-specs.md:87-88`, which specifies a navless WinUI frame with the **company** name —
not the product — and by the ticket's own trap list.

The state that decides whether this ticket succeeded is **`Unreachable`**. A server-unreachable
condition and a wrong password both look like "you cannot sign in", and the spec calls the
distinction out twice (`screen-specs.md:93-94` and the area 04 matrix row). It gets its own
enum value, its own message, its own view-model test and its own UI-test case — not a shared
"could not sign in" branch.

## Governing docs

The ticket's `refs` list is **not** empty — it carries
`docs/frd/frd-12-operator-experience.md` — and its frontmatter also carries `docs_todo: true`,
so both halves of this section apply.

**Meets** — for the one entry in `refs`:

| FRD-12 requirement | Where it says so | Met by |
| --- | --- | --- |
| "loading, empty, current, stale, unavailable, partial, failed, validation, conflict, and access-denied states" | `docs/frd/frd-12-operator-experience.md:22-23` | Steps 4 and 6. This screen owes the subset that exists on a login surface: loading (signing in), validation (empty fields), failed (invalid credentials), unavailable (unreachable), and access-denied (account disabled). Each is a distinct view-model state with its own message, not a shared failure branch. |
| "keyboard, pointer, screen-reader, 200% zoom, forced-colour, and reduced-motion support" | `:24-25` | Steps 8 and 12, and the recorded manual pass. `keyboard-and-accessibility.md:39-42` supplies the forced-colours rule: the system focus visual replaces `PegasusFocusBrush` rather than the brush being redefined. |
| "exact state labels mapped to Core decisions" | `:21` | Step 6. Each rendered state maps to exactly one `SessionFailure` value, which maps to exactly one gateway signal in the area 04 matrix — no label is invented at the UI layer. |
| "Every actionable search result is a full-row keyboard-focusable link or button with visible action affordance" and the row/label rules | `:27` | Not applicable on this screen and stated so rather than silently skipped: the login surface has no list, no rows and no search. |
| UI behaviour is owned by `docs/design/README.md` (FRD-12 header line) | `:2` | Steps 2 and 5. `docs/design/README.md:424` ("stop explaining pages") governs the copy; `:774-792` governs the accessibility behaviours; `:1300-1302` governs what must be recorded. |

**New documents this ticket is written to**, because `docs_todo: true`:

> **New FRD** — FRD-13 "Desktop operator experience" (login, session restore, blocked states,
> the update-required screen), authored by [[FND-008]] (plan handle `DSK-00-08`).
> `ls docs/frd/` returns FRD-01…FRD-12 only (2026-08-24), so **this ticket does not create it**
> — it records the dependency and writes the screens to the area 06 specification instead.
> **New ADR** — ADR-0100 (native WinUI 3 desktop client inside this fork, which authorises
> `src/Pegasus.Desktop`), authored by [[FND-026]] (plan handle `DSK-02-01`); [[FND-005]]
> (plan handle `DSK-00-05`) also claims it — see [[FND-026]]'s plan for the ownership
> reconciliation.
> **New ADR** — ADR-0102 (existing Pegasus credentials with a desktop token session, which is
> why this screen is a Pegasus login and not a Microsoft one), authored by [[FND-042]] (plan
> handle `DSK-04-01`); [[FND-006]] (plan handle `DSK-00-06`) also claims it — see
> [[FND-042]]'s plan for the ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/06-ui-design/screen-specs.md` § 13.1 and
> `docs/desktop/04-auth-session-update-and-startup/README.md` § 3; if either ADR or FRD-13
> lands differently this plan is revised before implementation.

The programme-level authorities that also bind:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 8.1 User experience | The login stays a Pegasus login — username and password, same account store, **no** Microsoft-account prompt, no dependency on the Windows identity | Steps 3–5; verified by the "no browser launched" acceptance criterion |
| Proposal § 8.4 Session failure handling | Distinguish the failure kinds rather than reporting everything as bad credentials | Steps 4 and 6 |
| Proposal § 14.8 Notifications and errors | Routine states are inline, not modal | Step 4 |
| Plan 04 § 3 *Session failure matrix* | Seven rows, each with its gateway signal and required desktop behaviour | Steps 4 and 6, one view-model test and one UI-test case per row |
| Plan 06 `screen-specs.md:85-97` | Sign-in frame, fields, absence of "forgot password", eight states, four AutomationIds | Step 3 |
| Plan 06 `screen-specs.md:99-106` | Update required / Blocked: full-window, versions as values, "Update now" + "Sign out"; Blocked drops "Update now" | Step 7 |
| Plan 06 `screen-specs.md:108-113` | Change password: four AutomationIds; minimum length only as a validation outcome, never hint text | Step 6's routing target |
| Plan 06 `keyboard-and-accessibility.md:1-7` | Keyboard-only completion; perceivable without colour; reachable by UI Automation | Steps 8 and 12 |
| Plan 06 `keyboard-and-accessibility.md:39-42` | 3px `PegasusFocusBrush` focus ring; forced colours uses the **system** focus visual | Step 8 |
| `docs/design/README.md:424` | Operator direction, 2026-08-20: "stop explaining pages" | Step 5 |
| `docs/design/README.md:774-792` | Labelled navigation, associated field errors and error summaries, visible focus, **non-colour state cues** | Steps 5 and 8 |
| `docs/design/README.md:1300-1302` | Inspection evidence must be recorded; generated or synthetic material cannot prove acceptance | Steps 11–12 and the Verification section |
| **L-04** | Every ticket names its subagent, skills and MCP tools | The Routing block below |
| `docs/engineering.md:76` tier 7 | Authenticated workflows through the real UI; automated axe does not replace the manual keyboard and assistive-technology review | Verification |
| `docs/engineering.md:106-111` | No `Common`/`Helpers`/`Utilities`/`Services` folder | Step 3's `Session/` folder |
| `Directory.Build.props:6-7` | `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended` | Step 3 |
| `AGENTS.md` § Repository task workflow steps 4–5 | Simplification pass; review by an agent that did not implement | Step 13; Routing |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`, with `winui-search.exe` for control lookup) →
  `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-ui-testing`
  (`.codex/skills/winui-ui-testing/SKILL.md`). All four vendored from
  `microsoft/win-dev-skills` v0.5.0 `f1028dd5` and verified present 2026-08-24.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `InfoBar` and
  `PasswordBox` semantics.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-044` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's thirteen implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient and take.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 3
   *Session failure matrix* and `docs/desktop/06-ui-design/screen-specs.md` § 13.1
   (`:83-113`) in full — the AutomationIds and copy rules there are **binding, not
   suggestions**. Confirm the prerequisite exists: `ls src/Pegasus.Desktop/Session` for
   [[FND-043]]'s (plan handle `DSK-04-07`) `SessionFailure`; if it is missing, stop. Call
   `get_doc_gates FND-044`, then `take_ticket FND-044`, and branch
   `task/desktop-login-screen` from `origin/dev`.
2. **Load the design skill and search before writing XAML.** `pegasus-desktop`, then
   `winui-design`. Follow that skill's *Search samples before writing XAML* section
   (`SKILL.md:8`) and run `winui-search.exe` for the sign-in form controls; then read its
   *XAML landmines* (`:82-138` — `x:Bind` defaults to `OneTime`, `TextBox` two-way needs
   `UpdateSourceTrigger=PropertyChanged`, `Converter={x:Null}` crashes at runtime) and
   *Theming rules* (`:140`) sections. Use only theme resources from [[FND-034]] (plan handle
   `DSK-02-09`) — **no colour literal anywhere**. The `x:Bind` default in particular is why a
   state change would otherwise not repaint the `InfoBar`.
3. **Add the sign-in page and view model** under `src/Pegasus.Desktop/Session/` — a capability
   folder, never `Common`/`Helpers`/`Utilities`/`Services` (`docs/engineering.md:106-111`).
   Navless frame, logo plus the **company** name (not the product — `_LayoutAuth` convention,
   `screen-specs.md:87-88`), fields **User name** and **Password** with label plus control
   only, primary **Sign in**, and **no** "forgot password" (`:89`, "not a current
   capability"). Set `AutomationProperties.AutomationId` to exactly `SignIn.UserName`,
   `SignIn.Password`, `SignIn.Submit`, `SignIn.Problem`. Done when it builds clean under
   `TreatWarningsAsErrors=true`.
4. **Implement the inline states as view-model state, never dialogs**: idle; signing in
   (submit disabled, thin **indeterminate** progress — there is no percentage to show);
   invalid credentials; rate limited. Messages go in an `InfoBar` bound to `SignIn.Problem`.
   The plan row is explicit that routine states get an `InfoBar` and never a modal, and
   `docs/design/README.md:789` asks for "non-colour state cues" — so the `InfoBar` carries an
   icon and distinct text, not merely a different severity colour.
5. **Reuse the gateway's operator copy verbatim** so desktop and web say the same thing while
   both are live: the invalid-credential sentence from
   `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:73-74` ("The username or password is
   incorrect. If your access has changed, contact an administrator.") and "Enter your
   username." / "Enter your password." from `:27` and `:31`. Read the comment at `:21-23`
   before rewording anything — it records why those messages are explicit. After an invalid
   attempt, **return focus to the User name field and clear the password box**
   (`screen-specs.md:90-91`). Do not add a sentence explaining what the system did:
   `docs/design/README.md:424`, operator direction 2026-08-20, "stop explaining pages".
6. **Map the remaining matrix rows.** `RateLimited` → "Try again in a minute" with submit
   disabled until the `Retry-After` seconds elapse (the limit is 10 attempts per client per
   minute — `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:12` — so the wait is seconds, and
   the countdown must actually re-enable the button rather than requiring a restart).
   `AccountDisabled` → the disabled-access sentence with **no retry loop**.
   `PasswordChangeRequired` → navigate to the Change password screen, which this ticket
   creates as a routing target carrying `Password.Current`, `Password.New`,
   `Password.Confirm`, `Password.Save` — its behaviour is [[FEAT-021]]'s (plan handle
   `DSK-05-21`), and its "minimum length only as a validation outcome, never hint text" rule
   (`screen-specs.md:111-112`) applies even to the stub. `ClientUnsupported` → the Update
   required screen. `Unreachable` → a connectivity sentence, **never** an invalid-credentials
   message.
7. **Add the Update required / Blocked screen** as a full-window, rail-less page
   (`screen-specs.md:99-106`): title "Update required", the current and minimum versions
   rendered **as values** (a labelled value, not a sentence), one primary "Update now" and a
   secondary "Sign out". The **Blocked** variant — account disabled, or the compatibility
   fail-closed state — shows the operator sentence and "Sign out" **only**, with
   `Blocked.Reason` on the message; it does not offer "Update now", because there is nothing
   to update. Wire "Update now" to the update entry point [[FND-045]] (plan handle
   `DSK-04-09`) exposes; **if that ticket has not landed, bind it to a command that logs and
   disables itself, and record that here** — a button that appears to work and does nothing is
   worse than a disabled one.
8. **Apply the keyboard and focus rules**
   (`docs/desktop/06-ui-design/keyboard-and-accessibility.md`). `Tab` order User name →
   Password → Sign in; `Enter` submits from either field; every interactive control has an
   `AutomationId`; the error message is **associated with the fields**, not merely placed near
   them (`docs/design/README.md:786`, "associated field errors and error summaries"), and no
   state is conveyed by colour alone. The focus visual is the 3px `PegasusFocusBrush` ring,
   with forced-colours mode falling back to the **system** focus visual (`:39-42`) rather than
   redefining the brush.
9. **Write the view-model tests** in `tests/Pegasus.Desktop.ViewModelTests` — one per matrix
   row plus idle and signing-in — asserting the resulting state, the **message identity** (not
   a substring match that would pass on the wrong sentence) and whether submit is enabled.
   They run without a `DispatcherQueue` (plan 02 § 4), and they reuse [[FND-038]]'s (plan
   handle `DSK-02-13`) fakes rather than adding new ones.
10. **Build, launch, and contribute UI-test cases to the one harness.** Run
    `.codex/skills/winui-dev-workflow/BuildAndRun.ps1` **asynchronously** — it prints the app
    PID, which the next step needs. Load `winui-ui-testing` and add the seven session-failure
    cases to `tests/Pegasus.Desktop.UITests/ui-tests.ps1`. **Do not author a second harness.**
    That file's contract is [[TEST-006]]'s (plan handle `DSK-08-06`): the signature is
    `param([Parameter(Mandatory)][int]$AppPid)` — never `$Pid`, which is read-only in
    PowerShell and would make the script unrunnable — and the pass/fail counter is its
    `Test-UI` helper. Drive each case against a stubbed gateway and assert with
    `winapp ui wait-for` / `get-value`, **never `Start-Sleep`** (the skill's *Key Gotchas*
    section at `SKILL.md:359`). `ls tests/` returns three projects today (2026-08-24), so if
    [[TEST-006]] has not landed, create the file from the `winui-ui-testing` script template
    with exactly that signature and that helper so the two cannot fork, and record here that
    [[TEST-006]] takes ownership of the skeleton when it lands.
11. **Capture a `winapp ui screenshot` for each of the seven states** — idle, signing in,
    invalid credentials, rate limited, account disabled, update required, disconnected — into
    `artifacts/ui-tests/` (already ignored by `.gitignore:20-21`, so they are proof
    attachments, not repository files) and attach them to the ticket proof.
12. **Run the accessibility audit and the manual pass.** Run the accessibility-audit section
    of `winui-ui-testing` over the sign-in and update-required windows and record the result:
    **a control without an `AutomationId` is a defect, not a warning.** Then do the manual
    pass `docs/design/README.md:1300` requires and record it — keyboard, screen-reader,
    focus/error, forced-colours, reduced-motion and 200%-zoom inspection — because `:1301`
    says automated results and generated material cannot prove acceptance.
13. **Documentation, simplification pass, PR.** Do **not** create
    `docs/frd/frd-13-desktop-operator-experience.md`: `ls docs/frd/` returns FRD-01…FRD-12
    (2026-08-24) and FRD-13 is [[FND-008]]'s (plan handle `DSK-00-08`) — record the dependency
    here instead. Touch `docs/desktop/06-ui-design/screen-specs.md` only if an implemented
    state genuinely differs from the spec, and then as a correction carrying its reason. Run
    the simplification pass over this branch's own diff, record it under a dated
    `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 7 — Browser/accessibility** (`docs/engineering.md:76`).
The tier obliges authenticated-workflow evidence driven through the real UI: keyboard
traversal, focus and error behaviour, semantic labels, and text-plus-colour states — and it
states in its own words that "automated axe results do not replace manual keyboard or
assistive-technology review". `docs/design/README.md:1300-1302` adds the recording
obligation and rules out generated or synthetic material as acceptance evidence. Proof types:
`visual` (seven state screenshots) and `command-log` (test output and the audit).

| Command / observation | Expected evidence |
| --- | --- |
| `dotnet test tests/Pegasus.Desktop.ViewModelTests` | `Passed!` with one green test per session-failure matrix row plus idle and signing-in, zero skipped |
| `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid>` | every assertion reports PASS and seven state screenshots are written under `artifacts/ui-tests/` |
| `winapp ui inspect -a <pid> --interactive` | the tree lists all four sign-in AutomationIds and **no interactive control without an AutomationId** |
| `grep -rn '#FF\|Color="#' src/Pegasus.Desktop/Session` | no matches — every brush comes from [[FND-034]]'s theme dictionaries |
| `grep -rn 'ContentDialog' src/Pegasus.Desktop/Session` | no match for a routine state; the plan row forbids a modal for one |
| Diff of the three reused strings against `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:27,31,73-74` | word-for-word identical |
| `dotnet test tests/Pegasus.ArchitectureTests` | `Passed!` — [[FND-037]]'s `DesktopXamlContainsNoWebView` stays green over the three new XAML files |
| `dotnet build Pegasus.slnx -c Release` on Windows | `Build succeeded` with `0 Warning(s)` |
| Manual pass, recorded | keyboard-only sign-in; screen-reader announcement of the `InfoBar`; focus returned to User name and password cleared after an invalid attempt; forced-colours and 200% zoom screenshots |
| Observations stated rather than inferred | whether [[FND-045]] had landed, and so whether "Update now" is wired or disabled; whether this ticket created the `ui-tests.ps1` skeleton or contributed to [[TEST-006]]'s; that FRD-13 does not exist yet |

## Risks / open questions

- **Risk — a transport failure shown as a bad credential.** The single most likely
  operator-visible defect: both states read as "you cannot sign in". Mitigation: `Unreachable`
  is a distinct `SessionFailure` value with its own message, its own view-model test and its
  own UI-test case; `screen-specs.md:93-94` and the area 04 matrix both say it explicitly.
- **Risk — the rate-limit countdown never re-enables submit.** A disabled button that stays
  disabled until restart is indistinguishable from a hang. Mitigation: step 6 requires the
  `Retry-After` countdown to re-enable the control, and step 9's test asserts submit-enabled
  before and after.
- **Risk — `x:Bind` defaults to `OneTime`, so the state never repaints.**
  `.codex/skills/winui-design/SKILL.md:84-92` names this landmine first. Mitigation: step 2
  requires reading that section before writing XAML, and the UI tests would catch a frozen
  `InfoBar`.
- **Risk — a second UI-test harness.** Mitigation: step 10 pins the signature
  (`param([Parameter(Mandatory)][int]$AppPid)`, never `$Pid`) and the `Test-UI` helper, and
  records that [[TEST-006]] (plan handle `DSK-08-06`) owns the skeleton whenever it lands.
- **Risk — colour-only state cues.** An `InfoBar` that differs between states only by severity
  colour fails `docs/design/README.md:789`. Mitigation: step 4 requires an icon and distinct
  text, and step 12's forced-colours screenshot is where it would show.
- **Risk — "Update now" that appears to work and does nothing.** Mitigation: step 7 disables
  the command and logs when [[FND-045]] has not landed, and requires that state to be recorded
  rather than left to be discovered.
- **Scope boundary, not an open question — the session client.** [[FND-043]] (plan handle
  `DSK-04-07`) owns `ISessionClient` and the seven `SessionFailure` values; this ticket
  renders them and adds none.
- **Scope boundary, not an open question — the startup orchestrator and the update entry
  point.** [[FND-045]] (plan handle `DSK-04-09`).
- **Scope boundary, not an open question — change-password behaviour.** [[FEAT-021]] (plan
  handle `DSK-05-21`) owns it; this ticket adds the route and the four AutomationIds.
- **Scope boundary, not an open question — FRD-13.** [[FND-008]] (plan handle `DSK-00-08`)
  authors it; `ls docs/frd/` confirms FRD-12 is the highest today.
- **Open questions**: none. The four research assumptions are settled by a build, one
  documentation query, the first UI-test run, or one `ls`. No `open-questions` document is
  created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds XAML,
C# and a PowerShell test file, so `n/a — docs-only` does not apply._
