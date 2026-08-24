# Checklist — FND-034

One box per plan step, in plan order. Each is independently tickable: it names the file, value or
command whose completion makes the box true.

- [ ] Read `docs/desktop/06-ui-design/tokens-and-theme.md` in full and `docs/design/README.md` § Tokens (`:258-277`); run `get_doc_gates FND-034`; `take_ticket` on branch `task/desktop-theme` from `origin/dev`.
- [ ] Check whether [[DUI-001]] (plan handle `DSK-06-01`) has landed and **record which case applied in the plan**: values preserved and no existing key changed, or dictionaries created with the `tokens-and-theme.md` § Colour tokens table transcribed verbatim.
- [ ] Create the eight entries under `src/Pegasus.Desktop/Styles/` in load order: `Tokens.Colors.xaml`, `Tokens.Typography.xaml`, `Tokens.Spacing.xaml`, `Tokens.Shape.xaml`, `Tokens.Focus.xaml`, `Icons.Lucide.xaml`, the `Controls.*.xaml` set, and `Pegasus.Theme.xaml` merging them in that order.
- [ ] Confirm `Icons.Lucide.xaml` and the `Controls.*.xaml` set were created as **empty** dictionaries reserving their load-order position, with their contents left to [[DUI-003]] (plan handle `DSK-06-03`), [[DUI-006]] (plan handle `DSK-06-06`), [[DUI-008]] (plan handle `DSK-06-08`), [[DUI-009]] (plan handle `DSK-06-09`) and [[DUI-010]] (plan handle `DSK-06-10`).
- [ ] Declare `ResourceDictionary.ThemeDictionaries` in `Tokens.Colors.xaml` with keys `Light`, `Dark` and `HighContrast` — and confirm there is **no** `Default` key (`.codex/skills/winui-design/SKILL.md:143`).
- [ ] Transcribe every key and value from the `tokens-and-theme.md` § Colour tokens table verbatim into the Light and Dark dictionaries; confirm no value was paraphrased or re-derived.
- [ ] Map every HighContrast entry to one of the eight named system colour resources exactly as the table's HighContrast column specifies; confirm `HighContrastAdjustment="None"` was **not** set anywhere.
- [ ] Write `Tokens.Typography.xaml` with the eight styles, each `BasedOn` its named built-in WinUI text style, and `Typography.NumeralAlignment="Tabular"` on the numeric styles; transcribe the `PegasusSectionTextStyle` assumption as written rather than resolving it.
- [ ] Write `Tokens.Spacing.xaml` with `PegasusSpace1`…`PegasusSpace9` as `x:Double` 4, 8, 12, 14, 18, 24, 32, 40, 64 and `PegasusGutter` = 24.
- [ ] Write `Tokens.Shape.xaml` with `ControlCornerRadius` and `OverlayCornerRadius` at `2` and border thickness `1`; confirm no second radius value was introduced.
- [ ] Write `Tokens.Focus.xaml` overriding the focus visual to the `PegasusFocusBrush` 3 px ring.
- [ ] Merge in `src/Pegasus.Desktop/App.xaml`: `XamlControlsResources` first, then `Pegasus.Theme.xaml`; confirm `Pegasus.Theme.xaml` is referenced exactly once in the whole application.
- [ ] Add the fact named exactly `StylesAreTheOnlySourceOfColourAndType` to `tests/Pegasus.ArchitectureTests`, scanning `src/Pegasus.Desktop/**/*.xaml` excluding `Styles/` and failing on a hex colour literal (`#` + 3/4/6/8 hex digits), a raw `FontSize=` attribute or a numeric `CornerRadius=`; reuse `FindRepositoryRoot()` (`DependencyDirectionTests.cs:509`).
- [ ] **Prove the guard red first** with a temporary planted literal, capture the failure output for the proof, then remove the literal and prove it green.
- [ ] Run the app under Light, under Dark, and with Windows high contrast enabled, capturing one screenshot per theme of the shell from [[FND-033]] (plan handle `DSK-02-08`).
- [ ] Run the contrast check on every foreground/background pair (4.5:1 body text, 3:1 large text and UI boundaries) in Light and Dark; record the results.
- [ ] If any pair failed, create the `open-questions` document with one unticked box per failing pair naming the two keys and the measured ratio — and confirm **no Light value was adjusted** to make a pair pass.
- [ ] Run the `winui-code-review` theming checklist over the new XAML.
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`): `dotnet test --filter "FullyQualifiedName~StylesAreTheOnlySourceOfColourAndType"` (green, with the earlier red output attached); the three theme screenshots; `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun` (exit 0, zero warnings); confirmation that `Tokens.Colors.xaml` has no `Default` theme-dictionary key; `grep -rn 'Pegasus.Theme.xaml' src/Pegasus.Desktop/` (exactly one reference, in `App.xaml`); `ls src/Pegasus.Desktop/Styles/` (the eight load-order entries and no others); and the recorded [[DUI-001]] landing case. Capture every output as tier-7 evidence.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
