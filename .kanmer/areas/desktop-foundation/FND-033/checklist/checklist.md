# Checklist — FND-033

One box per plan step, in plan order. Each names the file, control or command whose completion makes
it true, so it can be ticked independently and honestly.

- [ ] Read `docs/desktop/06-ui-design/screen-specs.md:41-81` § Shell in full, plus `:27-39` (absent-vs-disabled and the AutomationId convention) and `docs/design/README.md` § No explanatory copy and page economy (`:422`).
- [ ] Read `src/Pegasus.Web/Pages/Shared/_Layout.cshtml:56-114` for the route inventory only, noting `:6` ("a permanently inert item says the product is broken"), then close it — this is a `NavigationView`, not a port.
- [ ] Confirm [[DUI-004]] (plan handle `DSK-06-04`) has not been taken; apply the ownership split recorded in the plan's Approach; record the same split in [[DUI-004]]'s plan document. If it is already taken and started, stop and reconcile with its holder rather than building a second shell.
- [ ] Run `get_doc_gates FND-033` and `take_ticket` on branch `task/desktop-shell` from `origin/dev`.
- [ ] Load `winui-design` and run `winui-search.exe` for the `NavigationView` API surface **and its selection-indicator template parts** before writing any XAML.
- [ ] Write `src/Pegasus.Desktop/Shell/ShellPage.xaml` with a `NavigationView` at `PaneDisplayMode="Left"`, `OpenPaneLength="236"`, `IsPaneToggleButtonVisible="False"`, and seven `NavigationViewItem`s in order: Dashboard, Inbox, Upload, Queues, Cases, Operations, Administration.
- [ ] Bind `Administration` and `Inbox` visibility to `ShellViewModel` properties so they are **absent** when not applicable — never hard-coded visible and never rendered disabled.
- [ ] Restyle the `NavigationView` selection indicator to a weight change **plus** a 2 px Collision-red left marker, with every colour and size from a `{ThemeResource}` key and glyphs from [[DUI-003]] (plan handle `DSK-06-03`)'s `PathIcon` set.
- [ ] Run `microsoft_docs_search` for `AppWindow TitleBar` drag-region semantics, then build the title bar: logo, environment badge (non-production only, read from [[FND-032]] (plan handle `DSK-02-07`)'s channel option through one view-model property), connection glyph **plus word**, version and channel, user menu (Change password, Sign out, Diagnostics).
- [ ] Build the status bar: connection state, last sync in **Europe/London**, background transfer summary opening the transfer pane, and update availability. Use the exact string "Disconnected — reconnecting".
- [ ] Declare rail counts as `int?` on `ShellViewModel` so absent and zero are different values, and bind the count element to be **absent** when null.
- [ ] Create `src/Pegasus.Desktop/Services/INavigationService.cs` and `IDialogService.cs` with implementations, register both in `Hosting/PegasusHost.cs`, and route every rail item through the navigation service — no other navigation mechanism anywhere.
- [ ] Set a unique `AutomationProperties.AutomationId` on every interactive control, using exactly `Shell.Rail.<Route>`, `Shell.Title.Environment`, `Shell.Title.User`, `Shell.Status.Connection`, `Shell.Status.Update`, and the `Dialog.Reason.*` names for the dialog service.
- [ ] Wire the shell keyboard subset: `Alt+D/I/U/Q/C/O/A`, `Ctrl+K` → Cases search, `F5` → refresh; verify tab order reaches every rail item and the user menu. Record any access-key collision and raise it with [[DUI-014]] (plan handle `DSK-06-14`) rather than substituting a letter.
- [ ] Implement the five shell states (authenticated; unauthenticated; update-required/blocked full-window with no rail; disabled account; stale role) as view-model states with placeholder content — implement no authentication.
- [ ] Write the view-model tests in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle `DSK-02-13`): rail visibility administrator vs non-administrator; badge hidden in `production`; connection text connected/disconnected; navigation to each of the seven routes; and a `null` count rendering nothing, **distinguishable in the test from a count of `0`**. Sequence [[FND-038]] first if it has not landed and record the sequencing.
- [ ] Run `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj` async; navigate every rail item, press each access key, drag the window by its custom title bar, and capture screenshots.
- [ ] Record in the proof whether the keyboard evidence is a manual pass or [[TEST-006]] (plan handle `DSK-08-06`)'s `winapp ui` shell smoke batch, naming [[TEST-006]] as the automation follow-up if manual.
- [ ] Run the `winui-code-review` checklist over the new XAML: theming, no raw `FontSize`, no hex literals, AutomationIds present.
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`, evidence tier 7): `dotnet build ./Pegasus.slnx --configuration Release` (exit 0, `0 Warning(s)` — the authoritative gate); `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`; the recorded manual keyboard and navigation pass with screenshots of at least the authenticated and blocked states; the `winui-code-review` output; and `grep -rniE '#[0-9a-f]{6}|FontSize="[0-9]' src/Pegasus.Desktop/Shell/` returning **no matches**. Write the honesty clauses into the proof: manual versus automated keyboard evidence; `BuildAndRun.ps1` green ≠ `dotnet build` green; no CI job builds the desktop until [[FND-040]] (plan handle `DSK-02-15`) lands; and which shell states were shown to a human rather than only implemented.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
