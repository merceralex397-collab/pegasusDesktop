# Checklist — FND-033

One box per plan step, in plan order. Each is independently tickable: it names the file, value or
command whose completion makes the box true.

- [ ] Read `docs/desktop/06-ui-design/screen-specs.md` § Shell in full, plus `docs/design/README.md:30-46`, `:169-173`, `:489-491`, `:586` and `:764-772`; run `get_doc_gates FND-033`; `take_ticket` on branch `task/desktop-shell` from `origin/dev`.
- [ ] Record in the plan the settled file path (`src/Pegasus.Desktop/Shell/ShellPage.xaml`, this ticket creating it; [[DUI-004]] (plan handle `DSK-06-04`) dresses it there) and the keyboard boundary against [[DUI-014]] (plan handle `DSK-06-14`) — both before any XAML is written.
- [ ] Answer the environment-badge open question (`open-questions`) and tick its box; without it, step 5's badge text cannot be written.
- [ ] Load `winui-design` and confirm the current `NavigationView` API and selection-indicator template parts with `.codex/skills/winui-design/winui-search.exe`; record that the left rail is the authority's shape, not a reflexive choice.
- [ ] Create `src/Pegasus.Desktop/Shell/ShellPage.xaml` with `PaneDisplayMode="Left"`, `OpenPaneLength="236"`, `IsPaneToggleButtonVisible="False"`, `IsSettingsVisible="False"`, and the seven rail items in order: Dashboard, Inbox, Upload, Queues, Cases, Operations, Administration.
- [ ] Bind `Administration` and `Inbox` visibility to view-model state so each is **absent**, never disabled, when not applicable (`docs/design/README.md:172`, `:586`).
- [ ] Restyle the selection indicator to a weight change **plus** a 2 px Collision-red left marker using `{ThemeResource}` keys from [[FND-034]] (plan handle `DSK-02-09`); confirm the accessible selection state survives.
- [ ] Build the title bar: logo slot, environment badge (`Shell.Title.Environment`, non-production only, reading the channel option from [[FND-032]] (plan handle `DSK-02-07`)), connection glyph plus word, version and channel, and the user menu (`Shell.Title.User`) with Change password, Sign out, Diagnostics — with a working drag region confirmed against `AppWindow TitleBar` documentation.
- [ ] Build the status bar: `Shell.Status.Connection`, last sync rendered Europe/London through the shared operator-label map (never `ToLocalTime()`), background-transfer summary opening the transfer pane, and `Shell.Status.Update`.
- [ ] Confirm the view-model shape allows a rail count to be **absent** (not `0`, not a placeholder) until the query returns; leave the count binding and its test to [[DUI-004]] step 6.
- [ ] Create `src/Pegasus.Desktop/Services/INavigationService.cs` and `IDialogService.cs` with implementations, register both in `Hosting/PegasusHost.cs`, and route every rail item through the navigation service; confirm no second navigation or prompt mechanism exists.
- [ ] Set a unique `AutomationProperties.AutomationId` on every interactive control, using the eleven spec names: the seven `Shell.Rail.<Route>` ids plus `Shell.Title.Environment`, `Shell.Title.User`, `Shell.Status.Connection`, `Shell.Status.Update`.
- [ ] Wire the shell keyboard subset only: `Alt+D/I/U/Q/C/O/A`, `Ctrl+K` → Cases search, `F5` refresh; verify tab order reaches every rail item and the user menu; confirm nothing from [[DUI-014]]'s wider map was implemented here.
- [ ] Implement the six shell states as view-model states with placeholder content: authenticated; unauthenticated; update-required and blocked (full-window, rail genuinely removed); disabled account; stale role — plus the disconnected status-bar text with saves disabled and content still visible. Implement no authentication.
- [ ] Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests`: rail visibility administrator vs non-administrator; badge hidden in the production channel and shown otherwise; status-bar connection text connected and disconnected; navigation service routing to each of the seven routes.
- [ ] Run `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj` asynchronously, navigate every rail item, and capture screenshots including one full-window state showing the rail removed.
- [ ] Run [[TEST-006]] (plan handle `DSK-08-06`)'s `winapp ui` shell smoke batch if it exists; otherwise record in the proof that the evidence is a manual pass and name [[TEST-006]] as the automation follow-up.
- [ ] Perform and record the manual keyboard pass (every access key, `Ctrl+K`, `F5`, full tab traversal, visible focus at every stop) — tier 7 requires it separately from any automation.
- [ ] Run the `winui-code-review` checklist over the new XAML: theming, no raw `FontSize`, no hex literals, AutomationIds present.
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`): `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` (tests named individually); `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun` (exit 0, zero warnings); `grep -rnE '#[0-9A-Fa-f]{3,8}\b' src/Pegasus.Desktop/Shell/ src/Pegasus.Desktop/Services/` (no matches); `grep -c 'AutomationProperties.AutomationId' src/Pegasus.Desktop/Shell/*.xaml` (at least the eleven spec ids); plus the screenshots and the recorded manual keyboard pass. Capture every output as tier-7 evidence.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
