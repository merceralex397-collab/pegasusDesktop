# Checklist — FND-034

One box per plan step, in plan order. Each names the file, key or command whose completion makes it
true, so it can be ticked independently and honestly.

- [ ] Read `docs/desktop/06-ui-design/tokens-and-theme.md` in full, plus `docs/design/README.md` § Tokens § Colour, § Shape borders and focus (`:258-268`) and § Spacing and layout (`:270-277`); read this ticket's `research` and `files` documents.
- [ ] Run `get_doc_gates FND-034` and `take_ticket` on branch `task/desktop-theme` from `origin/dev`.
- [ ] Check whether [[DUI-001]] (plan handle `DSK-06-01`) has landed and record which case applied under a dated heading in the plan: if landed, keep its values and change no existing key; if not, transcribe `tokens-and-theme.md` § Colour tokens verbatim. Confirm exactly **one** copy of the palette exists.
- [ ] Create the eight load-order entries under `src/Pegasus.Desktop/Styles/`: `Tokens.Colors.xaml`, `Tokens.Typography.xaml`, `Tokens.Spacing.xaml`, `Tokens.Shape.xaml`, `Tokens.Focus.xaml`, `Icons.Lucide.xaml`, the `Controls.*.xaml` set, and `Pegasus.Theme.xaml` merging them in that order.
- [ ] Create `Icons.Lucide.xaml` and the `Controls.*.xaml` files **empty, reserving their slots** — [[DUI-003]] (plan handle `DSK-06-03`) supplies the glyphs and [[DUI-006]] (`DSK-06-06`), [[DUI-008]] (`DSK-06-08`), [[DUI-009]] (`DSK-06-09`), [[DUI-010]] (`DSK-06-10`) the control styles. Invent no contents.
- [ ] In `Tokens.Colors.xaml` declare `ResourceDictionary.ThemeDictionaries` with keys `Light`, `Dark` and `HighContrast` and **no `Default`** (`.codex/skills/winui-design/SKILL.md:143`), transcribing all 24 key rows verbatim.
- [ ] Map **every** HighContrast entry to its `SystemColor*` resource exactly as the table's HighContrast column specifies (`SystemColorWindowColor`, `SystemColorWindowTextColor`, `SystemColorHighlightColor`, `SystemColorHighlightTextColor`, `SystemColorButtonFaceColor`, `SystemColorButtonTextColor`, `SystemColorGrayTextColor`, `SystemColorHotlightColor`).
- [ ] Write `Tokens.Typography.xaml` with the eight styles, each `BasedOn` its named built-in WinUI text style, and `Typography.NumeralAlignment="Tabular"` on the numeric styles.
- [ ] Write `Tokens.Spacing.xaml`: `PegasusSpace1`…`PegasusSpace9` = 4, 8, 12, 14, 18, 24, 32, 40, 64; `PegasusGutter` = 24; plus `PegasusTableRowHeight` 32, `PegasusFactRowHeight` 28, `PegasusPanelPadding`, `PegasusContentMaxWidth` 1280, `PegasusRailWidth` 236, `PegasusMinimumTargetSize` 44, `PegasusMinimumWindowWidth` 1280.
- [ ] Write `Tokens.Shape.xaml`: `ControlCornerRadius` and `OverlayCornerRadius` = `2`, `PegasusBorderThickness` = `1`. Do **not** adopt the 6px/5px from `site.css` / `.design-sync/conventions.md` / `.stitch/DESIGN.md` — `docs/design/README.md:268` says there is no second approved radius.
- [ ] Write `Tokens.Focus.xaml`: focus visual overridden to the 3 px `PegasusFocusBrush` ring, `FocusVisualSecondaryBrush` → `PegasusPanelBrush`.
- [ ] Edit `src/Pegasus.Desktop/App.xaml` to merge `XamlControlsResources` **first**, then `Pegasus.Theme.xaml`, referenced exactly once in the whole application.
- [ ] Add the fact `StylesAreTheOnlySourceOfColourAndType` (that exact name) in `tests/Pegasus.ArchitectureTests/StyleLiteralTests.cs`, scanning `src/Pegasus.Desktop/**/*.xaml` excluding `Styles/` and failing on a hex colour literal, a raw `FontSize=` and a numeric `CornerRadius=`, reusing `FindRepositoryRoot()` (`DependencyDirectionTests.cs:509`).
- [ ] Prove the guard **red** with a temporary planted literal, capture that run, then remove the literal and prove it green. Confirm it is the only such scanner in the repository.
- [ ] Run the app under Light and capture a screenshot of the [[FND-033]] (plan handle `DSK-02-08`) shell.
- [ ] Run the app under Dark and capture a screenshot of the same shell.
- [ ] Enable Windows high contrast, run the app, and capture a screenshot — confirming forced colours are honoured throughout rather than half-applied.
- [ ] Measure every foreground/background pair in Light and Dark against 4.5:1 (body text) and 3:1 (large text and UI boundaries), and record the table.
- [ ] For any pair below threshold: convert the parked contrast entry in `open-questions` into an unticked `- [ ]` item above `## Parked`, naming the pair and its measured ratio, for the design authority. Adjust the **Dark** value or wait for the answer — **never** adjust a Light value and never silently edit `tokens-and-theme.md`.
- [ ] Run the `winui-code-review` theming checklist (`.codex/skills/winui-code-review/references/quality-rules.md`) over the new XAML.
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`, evidence tier 7): `dotnet build ./Pegasus.slnx --configuration Release` (exit 0, `0 Warning(s)` — the authoritative gate); `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --filter "FullyQualifiedName~StylesAreTheOnlySourceOfColourAndType"` with **both** the red and green runs pasted; the three theme screenshots; the contrast table; and `grep -rniE '#[0-9a-f]{3,8}\b|FontSize="[0-9]|CornerRadius="[0-9]' src/Pegasus.Desktop --include=*.xaml | grep -v '/Styles/'` returning no matches. Write the honesty clauses into the proof: which [[DUI-001]] case applied and that one palette copy exists; that the theme evidence is a **manual** sweep and what was not exercised (200 % zoom belongs to [[DUI-002]], plan handle `DSK-06-02`); that `BuildAndRun.ps1` green ≠ `dotnet build` green; and which load-order entries are reserved-but-empty with their owning tickets.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
