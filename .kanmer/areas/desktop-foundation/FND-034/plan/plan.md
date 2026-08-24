# Plan — FND-034: Wire the theme resource dictionaries (Light/Dark/HighContrast) into `App.xaml` and ban hard-coded colours

**Diff estimate: ~11 files, ~430 lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the files
document: `Tokens.Colors.xaml` ~150 (24 keys × three theme dictionaries);
`Tokens.Typography.xaml` ~60 (eight styles); `Tokens.Spacing.xaml` ~24;
`Tokens.Shape.xaml` ~14; `Tokens.Focus.xaml` ~18; `Icons.Lucide.xaml` ~6 (empty dictionary, position
reserved); `Controls.*.xaml` ~12 across the set (empty dictionaries, positions reserved);
`Pegasus.Theme.xaml` ~20 (the ordered merge); `App.xaml` +6; the guard test ~85; plus a
`ProjectReference`/glob adjustment if the guard lands in `tests/Pegasus.ArchitectureTests`. The three
theme screenshots are proof artefacts, not diff.

## Approach

Create the directory, the load order and the merge **once**, and make the literal ban an executable
fact rather than a review habit. The rejected alternative is enforcing "no hex literal in a view" by
the `winui-code-review` checklist alone: it is already in that checklist
(`.codex/skills/winui-code-review/references/quality-rules.md`), and it will still be run at step 12
— but a checklist is a human pass over a diff, and this rule has to hold across every future area-05
slice written by an agent that never read this ticket. `docs/engineering.md` § Lessons ("Guards
encoded defects as allowed divergence; never watched to fail → A guard that has never fired is
deleted") is why step 9 proves the scanner **red first** against a planted literal before proving it
green.

One decision this plan takes beyond the body: **the guard lands in `tests/Pegasus.ArchitectureTests`,
not in `tests/Pegasus.Desktop.ViewModelTests`.** The body permits either ("or an architecture fact in
`tests/Pegasus.ArchitectureTests` if the check is pure text") and the check *is* pure text — a regex
over `src/Pegasus.Desktop/**/*.xaml`. That project targets `net10.0`, already imports
`System.Text.RegularExpressions` (`DependencyDirectionTests.cs:2`), already has `FindRepositoryRoot()`
(`:509`), and already runs whole and unfiltered on every PR in the CI `unit` lane
(`.github/workflows/ci.yml:136-148`). Putting it there means the guard fires from the first PR after
this one; putting it in the desktop test project would mean it fires only once [[FND-040]] (plan
handle `DSK-02-15`) adds the desktop lane. The fact keeps the exact name
`StylesAreTheOnlySourceOfColourAndType` either way, because the body requires "that exact id, so one
literal name exists on both sides of the split with [[DUI-001]]".

## Governing docs

`refs` carries one entry, `docs/frd/frd-12-operator-experience.md`, and `get_doc_gates FND-034`
reports `docs_todo: true`.

| Governing doc | How this plan meets it |
| --- | --- |
| `docs/frd/frd-12-operator-experience.md` (`refs`) | **Meets.** The operator experience requires states that are perceivable without colour and an interface usable in forced-colours mode. Steps 4–5 give every key a `HighContrast` value mapped to a system colour so Windows governs; step 7 gives the 3 px focus ring that makes keyboard position visible; steps 10–11 are the demonstration. |

> **New ADR** — this ticket introduces no architectural decision of its own. It implements
> `docs/design/README.md` § Tokens, which is a **binding repository authority** (`AGENTS.md` §
> Simplicity rails), through the WinUI mapping in `docs/desktop/06-ui-design/tokens-and-theme.md`.
> The programme ADR nearest to it is ADR-0100 (native WinUI 3 client in the fork), authored by
> [[FND-026]] (plan handle `DSK-02-01`) and also claimed by [[FND-005]] (plan handle `DSK-00-05`) —
> see [[FND-026]]'s plan for the ownership reconciliation. If ADR-0100 lands differently this plan is
> revised before implementation.

Because `refs` carries only one entry, these are the other authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/design/README.md` § Tokens § Colour | The Light values verbatim; green reserved for confirmed completion; the excluded marketing tokens | Step 4 |
| `docs/design/README.md:260-268` | Radius `2`, borders `1`, focus ring `3px rgba(219,8,22,.38)`, "There is no second approved radius" | Step 7 |
| `docs/design/README.md:270-277` | Spacing steps 4/8/12/14/18/24/32/40/64; gutters 24 | Step 7 |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Files and load order | The eight entries, their order, and the single `App.xaml` merge after `XamlControlsResources` | Steps 3, 8 |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Colour tokens | The 24-key table with Light, Dark and HighContrast columns, transcribed verbatim | Steps 4, 5 |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Typography | Eight styles `BasedOn` built-ins, tabular numerals, no raw `FontSize` | Step 6 |
| `docs/desktop/06-ui-design/tokens-and-theme.md` § Change rule | Tokens are derived, not owned; a change starts in the authority | § Risks, step 11 |
| `.codex/skills/winui-design/SKILL.md:143` | `Light`, `Dark` **and** `HighContrast` explicitly — never `Default` | Step 4 |
| Proposal § 14.10, § 14.9 | A theme system; keyboard and accessibility | Steps 3–11 |
| `AGENTS.md` § Simplicity rails | `docs/design/README.md` is the binding design authority for every UI change; one list per concept | § Approach; steps 2, 8, 9 |
| `docs/engineering.md` § Lessons ("a guard that has never fired is deleted") | The scanner must be proven to fail | Step 9 |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 7 | Semantic labels, text-plus-colour states, a **manual** high-contrast review that automation does not replace | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md` plus `references/theme-accessibility.md` and
  `references/brushes-and-icons.md` — all three verified present) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md`, with
  `references/quality-rules.md`), win-dev-skills v0.5.0 `f1028dd5`.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search` for `ResourceDictionary.ThemeDictionaries`,
  `XamlControlsResources`, high-contrast system colour resources).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve steps: same order, same ownership, same paths.

1. **Orient.** Read `docs/desktop/06-ui-design/tokens-and-theme.md` **in full** and
   `docs/design/README.md` § Tokens (`:258-277`). Then `get_doc_gates FND-034` and `take_ticket` on
   branch `task/desktop-theme` from `origin/dev`.
2. **The split with [[DUI-001]] (plan handle `DSK-06-01`) is settled; do not re-open it.** This ticket
   owns `src/Pegasus.Desktop/Styles/`, its file set and load order, the `App.xaml` merge (step 8) and
   the `StylesAreTheOnlySourceOfColourAndType` guard test (step 9). [[DUI-001]] owns the token
   *values* and fills them into these dictionaries **in place**: no new file, no second merge, no
   second guard test. Check whether [[DUI-001]] has landed and **record which case applied in this
   document**: if it has, keep its values and change no existing key; if it has not, create the
   dictionaries with the keys and values of `tokens-and-theme.md` § Colour tokens transcribed
   verbatim. Two copies of the palette is the failure this ticket exists to avoid.
3. **Create the eight entries** under `src/Pegasus.Desktop/Styles/` in the load order of
   `tokens-and-theme.md` § Files and load order: `Tokens.Colors.xaml`, `Tokens.Typography.xaml`,
   `Tokens.Spacing.xaml`, `Tokens.Shape.xaml`, `Tokens.Focus.xaml`, `Icons.Lucide.xaml`, the
   `Controls.*.xaml` set, and `Pegasus.Theme.xaml` merging them in that order. The counts reconcile as
   the body states: the five `Tokens.*` files plus `Pegasus.Theme.xaml` are the six [[DUI-001]] fills
   in place; `Icons.Lucide.xaml` and the `Controls.*.xaml` set are the remaining two entries, whose
   contents come from [[DUI-003]] (plan handle `DSK-06-03`) for the icons and from [[DUI-006]] (plan
   handle `DSK-06-06`), [[DUI-008]] (plan handle `DSK-06-08`), [[DUI-009]] (plan handle `DSK-06-09`)
   and [[DUI-010]] (plan handle `DSK-06-10`) for the control styles, merged in when they land. Create
   them as empty dictionaries so the load order is reserved; do not invent their contents.
4. **`Tokens.Colors.xaml`** — declare `ResourceDictionary.ThemeDictionaries` with keys `Light`, `Dark`
   **and** `HighContrast`, and **never** `Default` (`.codex/skills/winui-design/SKILL.md:143`). Copy
   every key and value from the colour-token table verbatim; do not paraphrase or re-derive a value.
   Name resources by purpose, not hue — the table already does (`PegasusAccentBrush`,
   `PegasusDangerBrush`), and the skill states it as a theming rule.
5. **HighContrast maps to system colours** — every entry to one of the eight named resources
   (`SystemColorWindowColor`, `SystemColorWindowTextColor`, `SystemColorHighlightColor`,
   `SystemColorHighlightTextColor`, `SystemColorButtonFaceColor`, `SystemColorButtonTextColor`,
   `SystemColorGrayTextColor`, `SystemColorHotlightColor`), exactly as the table's HighContrast column
   specifies, so **forced-colours mode governs**. Do not set `HighContrastAdjustment="None"` —
   `winui-design` § Theming rules forbids it "unless your app already supplies system-aware brushes
   throughout".
6. **`Tokens.Typography.xaml`** — the eight styles from the typography table, each `BasedOn` its named
   built-in WinUI text style, with `Typography.NumeralAlignment="Tabular"` on the numeric styles. No
   raw `FontSize` may appear in any view. Note while transcribing that
   `PegasusSectionTextStyle` carries a recorded assumption in its own row ("15/700 — assumption: 14
   acceptable; confirm in review"); transcribe it as written and do not silently resolve it.
7. **`Tokens.Spacing.xaml`, `Tokens.Shape.xaml`, `Tokens.Focus.xaml`** — spacing
   `PegasusSpace1`…`PegasusSpace9` as `x:Double` 4, 8, 12, 14, 18, 24, 32, 40, 64 and
   `PegasusGutter` = 24; `ControlCornerRadius` and `OverlayCornerRadius` at `2` with border thickness
   `1` (`docs/design/README.md:268` — "There is no second approved radius"; the 6px/5px recorded
   elsewhere is a flagged discrepancy, not adopted); the focus visual overridden to the
   `PegasusFocusBrush` 3 px ring.
8. **Merge in `src/Pegasus.Desktop/App.xaml`**: `XamlControlsResources` **first**, then
   `Pegasus.Theme.xaml`, so the project's overrides win. Reference `Pegasus.Theme.xaml` **exactly
   once** in the whole application. This merge is owned here; [[DUI-001]] verifies it rather than
   adding a second one.
9. **The guard — `StylesAreTheOnlySourceOfColourAndType`**, that exact name. Land it as an
   architecture fact in `tests/Pegasus.ArchitectureTests` (the check is pure text; see § Approach for
   why that home beats the desktop test project). It scans `src/Pegasus.Desktop/**/*.xaml`
   **excluding `Styles/`** and fails on: a hex colour literal (`#` followed by 3, 4, 6 or 8 hex
   digits), a raw `FontSize=` attribute, and a numeric `CornerRadius=` literal. Reuse
   `FindRepositoryRoot()` (`DependencyDirectionTests.cs:509`) for the path;
   `System.Text.RegularExpressions` is already imported at `:2`. **Prove it red first** with a
   temporary fixture string, then green — a guard that has never fired is deleted
   (`docs/engineering.md` § Lessons). There is one scanner in the repository and it is this one.
10. **Verify the three themes visually.** Run the app under Light, under Dark, and with Windows high
    contrast enabled, capturing one screenshot per theme of the shell from [[FND-033]] (plan handle
    `DSK-02-08`). Attach all three to the proof. This is the only step that exercises all three
    `ThemeDictionaries` code paths — a missing key is a runtime resource failure, not a compile error.
11. **Run the contrast check**: every foreground/background pair must reach 4.5:1 for body text and
    3:1 for large text and UI boundaries in Light and Dark. **Record any failing pair as an open
    question for the design authority** — this ticket's § Documentation changes requires it — and do
    **not** silently adjust a Light value: the Dark column is explicitly an assumption to be adjusted
    by review, the Light column is authority. If a pair fails, create the `open-questions` document
    then, with one unticked box per failing pair naming the two keys and the measured ratio.
12. **Review and close.** Run the `winui-code-review` theming checklist over the new XAML, then the
    simplification pass recorded under a dated heading below, then open the PR into `dev`.

## Verification

Evidence tier **7 — Browser/accessibility** (`docs/engineering.md` § Required evidence tiers, `:72`),
applied to the desktop: semantic labels, text-plus-colour states and a **manual** high-contrast
review. `:74` states that automated results "do not replace manual keyboard or assistive-technology
review".

The `proof` document is produced from these:

1. `dotnet test --filter "FullyQualifiedName~StylesAreTheOnlySourceOfColourAndType"` over the project
   the guard landed in — expected: passes. **And** the recorded red run: paste the failure output from
   the temporary planted literal before it was removed. A green-only result does not prove the guard
   works.
2. The manual theme sweep with three screenshots (Light, Dark, high contrast) of the shell —
   expected: no unreadable pair, no missing element, forced colours honoured.
3. `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`
   — expected: exit 0, zero warnings.
4. Additionally, and not in the body — three checks that make acceptance criteria executable:
   - `grep -c 'Default' src/Pegasus.Desktop/Styles/Tokens.Colors.xaml` as a `ThemeDictionaries` key
     — expected: **no** `Default` key (inspect the match; the word may legitimately appear elsewhere).
   - `grep -rn 'Pegasus.Theme.xaml' src/Pegasus.Desktop/` — expected: exactly **one** reference, in
     `App.xaml`.
   - `ls src/Pegasus.Desktop/Styles/` — expected: the eight entries of the load order and no others.
5. The recorded answer to step 2: whether [[DUI-001]] had landed, and therefore whether values were
   transcribed or preserved.
6. The contrast-check results per pair, and — if any pair failed — the `open-questions` document that
   records it.

## Risks / open questions

- **Risk — a second palette.** The single failure mode this ticket exists to prevent. *Mitigation*:
  step 2's explicit ownership statement and the recorded check of whether [[DUI-001]] landed. **This
  ticket is the single owner of `src/Pegasus.Desktop/Styles/`, its file set and load order, the
  `App.xaml` merge and the `StylesAreTheOnlySourceOfColourAndType` guard test**; [[DUI-001]] fills the
  token values in place and creates no second file, merge or scanner.
- **Risk — a `Default` theme dictionary.** It is what most WinUI samples show and it silently works in
  light mode. *Mitigation*: `SKILL.md:143`, the acceptance criterion, and § Verification item 4.
- **Risk — the scanner is a false-negative.** A regex that never matches is a guard that has never
  fired, which `docs/engineering.md` § Lessons deletes. *Mitigation*: step 9 proves it red **first**,
  and the proof carries the red output as well as the green.
- **Risk — the scanner is a false-positive.** A `#` in a comment or a binding path could trip it.
  *Mitigation*: anchor the regex to `#` followed by exactly 3, 4, 6 or 8 hex digits and run it against
  the real tree in the same step.
- **Open question that may be created at step 11, and the body requires it.** If a foreground/
  background pair fails 4.5:1 (body text) or 3:1 (large text and UI boundaries), record it as an open
  question for the design authority rather than adjusting a value. No box is pre-opened here because
  the check has not run and an empty box would block the ticket for a question nobody has asked; the
  obligation sits with the implementer at step 11. Do **not** resolve such a failure by editing a
  Light value — that is an edit to the authority made from a downstream ticket.
- **Scope boundary, not an open question — `Icons.Lucide.xaml` and `Controls.*.xaml` contents.**
  [[DUI-003]], [[DUI-006]], [[DUI-008]], [[DUI-009]] and [[DUI-010]]. Their positions are reserved;
  their contents are not authored here.
- **Scope boundary, not an open question — the gallery page and the 100 %/200 % token review.**
  [[DUI-002]] (plan handle `DSK-06-02`), per `tokens-and-theme.md` § Change rule.
- **Scope boundary, not an open question — the shell's layout.** [[FND-033]]. This ticket restyles
  nothing structural and introduces no token the authority does not define.
- **Traps restated because they are absolute**: green never means progress, availability or generic
  positivity; Collision red is sparse (primary actions, active-route marker, focus, urgent emphasis)
  with one primary button per view region; and the banned list — WhatsApp green, large display scales,
  CTA shadows, gradients, neon/glow, purple/blue "AI" aesthetics, pure black `#000000`, cool slate
  greys — admits no exception. No brand-font bundle is loaded; Tw Cen MT and Futura are never UI
  fonts.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
