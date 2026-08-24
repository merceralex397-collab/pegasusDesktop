# Plan — FND-034: Wire the theme resource dictionaries (Light/Dark/HighContrast) into `App.xaml` and ban hard-coded colours

**Diff estimate: ~11 files, ~520 lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the `files`
document, file by file, measured 2026-08-24:
`Styles/Tokens.Colors.xaml` ~200 (24 key rows × three theme dictionaries);
`Styles/Tokens.Typography.xaml` ~70 (eight styles);
`Styles/Tokens.Spacing.xaml` ~25 (nine steps plus seven layout values);
`Styles/Tokens.Shape.xaml` ~12; `Styles/Tokens.Focus.xaml` ~15;
`Styles/Icons.Lucide.xaml` ~8 (an empty dictionary reserving its slot — [[DUI-003]], plan handle
`DSK-06-03`, fills it); `Styles/Controls.*.xaml` ~8 per file reserving slots;
`Styles/Pegasus.Theme.xaml` ~20 (the merge list);
`src/Pegasus.Desktop/App.xaml` ~+8;
`tests/Pegasus.ArchitectureTests/StyleLiteralTests.cs` ~85.
Nothing under `src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `src/Pegasus.Web` or
`src/Pegasus.Worker` is touched, and neither `docs/design/README.md` nor
`docs/desktop/06-ui-design/tokens-and-theme.md` is edited.

## Approach

**The deliverable that only this ticket can produce is the guard, not the palette.** [[DUI-001]]
(plan handle `DSK-06-01`) fills the token values; what this ticket contributes is one directory, one
merge point, and one executable check that turns "no hex literal in any view" from a review habit
into a failing test. Everything else here is transcription, and transcription is only safe because
the guard makes a drifted copy visible.

Three choices follow from that framing, and the third is the one that matters most:

1. **Transcribe, never re-derive.** Every value comes verbatim from
   `docs/desktop/06-ui-design/tokens-and-theme.md` § Colour tokens / § Typography / § Spacing /
   § Shape, which is itself derived from `docs/design/README.md`. That file's § Change rule is
   explicit: "Tokens here are derived, not owned … The desktop never carries a second token source."
   A value that differs from the table is a transcription error, not a judgement call.
2. **Reserve the two unfilled slots rather than inventing their contents.** `Icons.Lucide.xaml` and
   the `Controls.*.xaml` set have named owners with tickets. This is the *opposite* of the dormant
   scaffolding `docs/engineering.md` § Abstractions (`:113`) forbids: their callers exist and are
   scheduled, and the load order is exactly what those tickets need to merge into.
3. **Put the scanner in `tests/Pegasus.ArchitectureTests`, not in the desktop test project.** The
   ticket body permits either — "or an architecture fact in `tests/Pegasus.ArchitectureTests` if the
   check is pure text" — and the check *is* pure text: it globs `*.xaml` and applies regexes,
   touching no WinUI type. The reason to choose it is that `.github/workflows/ci.yml`'s `unit` lane
   (`:136`) already runs that project unfiltered **on every PR today**, while no lane runs a desktop
   test project until [[FND-040]] (plan handle `DSK-02-15`) adds one. A guard that starts firing
   immediately is worth more than one that waits, and the project already imports
   `System.Text.RegularExpressions` (`DependencyDirectionTests.cs:2`) and exposes
   `FindRepositoryRoot()` (`:509`), so the fact adds no dependency.

The rejected alternative for (3) is `tests/Pegasus.Desktop.ViewModelTests`. It reads more naturally —
a desktop rule in the desktop test project — but that project does not exist yet ([[FND-038]], plan
handle `DSK-02-13`, creates it), and even once it does, nothing runs it in CI until [[FND-040]]. The
guard would be green and unenforced for the whole of Phase 1, which is precisely the window in which
every area 05 and area 06 ticket writes its first XAML.

## Governing docs

This ticket's `refs` array is **not** empty — it carries `docs/frd/frd-12-operator-experience.md`,
which genuinely binds. `get_doc_gates FND-034` also reports `docs_todo: true`, so the conversion ADRs
below are still to be authored.

**Meets — `docs/frd/frd-12-operator-experience.md` (the ticket's `ref`):**

| FRD-12 requirement | Where it says so | Met by |
| --- | --- | --- |
| "keyboard, pointer, screen-reader, 200% zoom, **forced-colour**, and reduced-motion support" | § Operator experience `:24-25` | Steps 4 and 5 — the `HighContrast` theme dictionary with every entry mapped to a `SystemColor*` resource is exactly how forced-colour support is delivered; step 10's high-contrast screenshot is its evidence |
| "exact state labels mapped to Core decisions" and "loading, empty, current, stale, unavailable, partial, failed, validation, conflict, and access-denied states" | § Operator experience `:21-23` | Steps 4 and 7 — the pending/review/success/danger brush families are the token layer those state treatments bind to, so a state can be rendered as **text plus colour** rather than colour alone |
| "One semantic action or state has one consistent icon across Pegasus; no decorative or generated replacement icon is used" | `:28` | Step 3 — `Icons.Lucide.xaml` is a single reserved slot filled from one checksum-pinned source by [[DUI-003]]; this ticket creates no second icon path |

> **New ADR** — this ticket's values and rules are governed by `docs/design/README.md`, which is an
> existing binding authority rather than a conversion ADR, so no new ADR is claimed for the palette
> itself. The conversion ADR nearest to it is **ADR-0104** (online-required; bounded local cache),
> which is why the theme ships **inside the package** rather than being fetched — there is no
> runtime token service and none is wanted. ADR-0104 has two claimants — [[FND-005]] (plan handle
> `DSK-00-05`) and [[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s plan for the ownership
> reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/06-ui-design/tokens-and-theme.md`; if either lands differently this plan is revised
> before implementation.

The programme-level authorities that also bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 14.10 Theme system | A theme system exists, covering the supported modes | Steps 3–8 |
| Proposal § 14.9 Keyboard and accessibility | Visible focus everywhere | Step 7 (`Tokens.Focus.xaml`, 3 px `PegasusFocusBrush` ring) |
| `docs/design/README.md` (binding design authority, `AGENTS.md` § Simplicity rails) | Approved tokens only; a reviewed divergence must be recorded explicitly | Step 4 — every value transcribed; § Out of scope records each refusal |
| `docs/design/README.md` § Shape, borders and focus (`:258-268`) | Radius `2px`, borders `1px`, focus ring `3px rgba(219,8,22,.38)`; "**There is no second approved radius**" | Step 7 — the 6px/5px in `site.css`/`.design-sync`/`.stitch` is a flagged discrepancy and is **not** adopted |
| `docs/design/README.md` § Spacing and layout (`:270-277`) | Steps `4, 8, 12, 14, 18, 24, 32, 40, 64px`; "Primary gutters are 24px" | Step 7 |
| `docs/design/README.md` § Tokens § Colour | Green is reserved for **confirmed completion** and never means progress, availability or generic positivity; the excluded marketing tokens are absolute | Step 4, and § Out of scope |
| `docs/design/README.md` § Change and verification rule (`:982`) | A value change starts in the authority | This ticket edits neither the authority nor the mapping; a contrast finding becomes an open question instead |
| `tokens-and-theme.md` § Files and load order | The eight-entry set; `Pegasus.Theme.xaml` merged **after** `XamlControlsResources`, referenced once | Steps 3, 8 |
| `tokens-and-theme.md` § Colour tokens | 24 keys × Light / Dark / HighContrast; HighContrast maps to the eight named `SystemColor*` resources | Steps 4, 5 |
| `tokens-and-theme.md` § Change rule (`:197`) | "Tokens here are derived, not owned … The desktop never carries a second token source" | § Approach choice 1 |
| `.codex/skills/winui-design/SKILL.md:143` | "Custom theme dictionaries cover `Light`, `Dark`, **and** `HighContrast` explicitly — never `Default`" | Step 4 |
| `.codex/skills/winui-design/SKILL.md:146` | Never set `HighContrastAdjustment="None"` unless every brush is system-aware | § Out of scope |
| `docs/engineering.md` § Lessons from the predecessor (`:217`) | A guard that has never fired is indistinguishable from one that does not work | Step 9's prove-red-then-green ordering |
| `docs/engineering.md` § Plan sizing (`:201`) | Diff estimate first, from a measured inventory | The estimate above |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 7 | "Automated axe results do not replace manual keyboard or assistive-technology review" | § Verification V3 and its honesty clause |
| **L-04** (locked) | Every ticket names its subagent, skills and MCP tools | § Routing below |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md` plus `references/theme-accessibility.md` and
  `references/brushes-and-icons.md`) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md`), win-dev-skills v0.5.0 `f1028dd5`.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search` for `ResourceDictionary.ThemeDictionaries`,
  `XamlControlsResources`, high-contrast system colour resources).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve implementation steps: same order, same ownership, same file
paths, adding the *how* the body leaves out.

1. **Orient.** Read `docs/desktop/06-ui-design/tokens-and-theme.md` **in full** — it is the WinUI
   mapping and every value below is transcribed from it — plus `docs/design/README.md` § Tokens
   (§ Colour, § Shape borders and focus `:258-268`, § Spacing and layout `:270-277`). Read this
   ticket's `research` and `files` documents. Then `get_doc_gates FND-034` and `take_ticket` on branch
   `task/desktop-theme` from `origin/dev`.
2. **Record which [[DUI-001]] case applies — the split itself is settled and is not re-opened.**
   Check whether [[DUI-001]] (plan handle `DSK-06-01`) has landed and write the answer into this plan
   under a dated heading. **If it has**: keep every value it wrote, change no existing key, and this
   ticket contributes only the file set, the merge and the guard. **If it has not**: create the
   dictionaries with the keys and values of `tokens-and-theme.md` § Colour tokens transcribed
   verbatim. Either way, produce **one** copy of the palette. This ticket owns
   `src/Pegasus.Desktop/Styles/`, its file set and load order, the `App.xaml` merge (step 8) and the
   `StylesAreTheOnlySourceOfColourAndType` guard test (step 9); [[DUI-001]] fills values in place and
   creates no second file, merge or scanner.
3. **Create the eight entries** under `src/Pegasus.Desktop/Styles/` with exactly the names and order
   in `tokens-and-theme.md` § Files and load order: `Tokens.Colors.xaml`, `Tokens.Typography.xaml`,
   `Tokens.Spacing.xaml`, `Tokens.Shape.xaml`, `Tokens.Focus.xaml`, `Icons.Lucide.xaml`, the
   `Controls.*.xaml` set, and `Pegasus.Theme.xaml` merging them in that order. `Icons.Lucide.xaml`
   and the `Controls.*.xaml` files are created **empty, reserving their slot**: [[DUI-003]] supplies
   the sixteen glyph geometries from the checksum-pinned sprite, and [[DUI-006]] (plan handle
   `DSK-06-06`), [[DUI-008]] (`DSK-06-08`), [[DUI-009]] (`DSK-06-09`) and [[DUI-010]] (`DSK-06-10`)
   supply the control styles. Do not invent their contents; their callers are named, dated tickets,
   so a reserved slot is not dormant scaffolding.
4. **`Tokens.Colors.xaml`.** Declare `ResourceDictionary.ThemeDictionaries` with keys `Light`, `Dark`
   **and** `HighContrast` — and **never `Default`** (`.codex/skills/winui-design/SKILL.md:143`).
   `Default` is the trap: it silently works in light mode and is the shape most WinUI samples show,
   and getting it wrong fails at **runtime**, not at compile time. Transcribe all 24 key rows from
   `tokens-and-theme.md` § Colour tokens verbatim; do not paraphrase and do not re-derive a value from
   `docs/design/README.md` yourself. Use `{StaticResource}` inside the theme dictionaries and
   `{ThemeResource}` at usage sites, with `SystemColor*` staying `{ThemeResource}`
   (`SKILL.md:142`).
5. **Map every HighContrast entry to a system colour resource** — `SystemColorWindowColor`,
   `SystemColorWindowTextColor`, `SystemColorHighlightColor`, `SystemColorHighlightTextColor`,
   `SystemColorButtonFaceColor`, `SystemColorButtonTextColor`, `SystemColorGrayTextColor`,
   `SystemColorHotlightColor` — exactly as the table's HighContrast column specifies, so
   forced-colours mode governs. This is the step that satisfies FRD-12 `:24-25`'s forced-colour
   requirement, and a half-applied mapping is worse than none.
6. **`Tokens.Typography.xaml`.** The eight styles, each `BasedOn` its named built-in WinUI text style
   (`TitleTextBlockStyle`, `SubtitleTextBlockStyle`, `BodyStrongTextBlockStyle`,
   `BodyTextBlockStyle`, `CaptionTextBlockStyle`), with `Typography.NumeralAlignment="Tabular"` on
   the numeric styles so counts, dates, references and amounts align in columns. **No raw `FontSize`
   may appear in any view** — that is what step 9's scanner enforces. Note that
   `PegasusSectionTextStyle` is recorded in the mapping as "15/700 (**assumption**: 14 acceptable;
   confirm in review)"; transcribe the WinUI value as written and leave the assumption to the review.
7. **`Tokens.Spacing.xaml`, `Tokens.Shape.xaml`, `Tokens.Focus.xaml`.** Spacing:
   `PegasusSpace1`…`PegasusSpace9` as `x:Double` 4, 8, 12, 14, 18, 24, 32, 40, 64 and `PegasusGutter`
   24, plus `PegasusTableRowHeight` 32, `PegasusFactRowHeight` 28, `PegasusPanelPadding`,
   `PegasusContentMaxWidth` 1280, `PegasusRailWidth` 236, `PegasusMinimumTargetSize` 44,
   `PegasusMinimumWindowWidth` 1280. Shape: `ControlCornerRadius` and `OverlayCornerRadius` = `2`
   and `PegasusBorderThickness` = `1` — `docs/design/README.md:268` says "There is no second approved
   radius", so the 6px/5px in `site.css` / `.design-sync/conventions.md` / `.stitch/DESIGN.md` is a
   flagged discrepancy and is **not** adopted. Focus: override the focus visual to the 3 px
   `PegasusFocusBrush` ring, with `FocusVisualSecondaryBrush` → `PegasusPanelBrush`.
8. **Merge in `src/Pegasus.Desktop/App.xaml`**: `XamlControlsResources` **first**, then
   `Pegasus.Theme.xaml`, so the project's overrides win. Reference `Pegasus.Theme.xaml` **exactly
   once** in the whole application. This merge is owned here; [[DUI-001]] verifies it rather than
   adding a second one.
9. **Make the rule executable.** Add the fact `StylesAreTheOnlySourceOfColourAndType` — that exact
   name, so one literal identifier exists on both sides of the split with [[DUI-001]] — in
   `tests/Pegasus.ArchitectureTests/StyleLiteralTests.cs`. It scans `src/Pegasus.Desktop/**/*.xaml`
   **excluding `Styles/`** and fails on: a hex colour literal (`#` followed by 3, 4, 6 or 8 hex
   digits), a raw `FontSize=` attribute, and a numeric `CornerRadius=`. Reuse `FindRepositoryRoot()`
   (`DependencyDirectionTests.cs:509`) and `System.Text.RegularExpressions` (already imported at
   `:2`). **Prove it red first with a temporary planted literal, then remove the literal and prove it
   green** — `docs/engineering.md` § Lessons: a guard that has never fired is indistinguishable from
   one that does not work. There is **one** scanner in the repository and this is it.
10. **Sweep the three themes visually.** Run the app under Light, under Dark, and with Windows high
    contrast enabled, capturing **one screenshot per theme** of the shell from [[FND-033]] (plan
    handle `DSK-02-08`). Attach all three. This is the only step that exercises all three code
    paths: a missing or misnamed theme dictionary throws at **runtime on theme switch**, which a
    single-theme screenshot would never reveal (A-FND034-1).
11. **Run the contrast check.** Every foreground/background pair must reach **4.5:1 for body text**
    and **3:1 for large text and UI boundaries**, in Light and in Dark. Record any failing pair as an
    **open question** on this ticket for the design authority — not as a silent edit, and **never**
    by adjusting a Light value: `tokens-and-theme.md` § Contrast says the Dark values "are starting
    points to be adjusted by that review, not authority", while the Light column *is* the authority's.
    The obligation is carried in this ticket's `open-questions` document, parked with its reason and
    with the escalation rule the implementer discharges here.
12. **Review, simplify, open the PR.** Run the `winui-code-review` theming checklist
    (`.codex/skills/winui-code-review/references/quality-rules.md`), then
    `dotnet build ./Pegasus.slnx --configuration Release` for the authoritative zero-warning gate.
    Run the simplification pass over this branch's own diff, record it under a dated
    `## Simplification pass` heading in this document, and open the PR into `dev`.

## Verification

Evidence tier **7 — Browser/accessibility** (`docs/engineering.md` § Required evidence tiers, `:72`),
applied to the desktop, as the ticket body states: semantic labels, text-plus-colour states and a
**manual** high-contrast review. That tier's own sentence governs the proof: *"Automated axe results
do not replace manual keyboard or assistive-technology review."*

The `proof` document is produced from these five outputs.

- **V1.** `dotnet build ./Pegasus.slnx --configuration Release` — expected exit 0 and
  `0 Warning(s)`. The authoritative gate: it is what `.github/actions/dotnet-build/action.yml:22-27`
  runs and, unlike `BuildAndRun.ps1`, it sees the repository-root `Directory.Build.props`.
- **V2.** `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --filter "FullyQualifiedName~StylesAreTheOnlySourceOfColourAndType"`
  — expected `1 passed`. **Paste both runs**: the red run with the planted literal (showing which
  literal it caught and in which file) and the green run after its removal. A green-only result is
  not evidence the guard works.
- **V3.** The **three-theme manual sweep**: one screenshot each of the [[FND-033]] shell in Light, in
  Dark, and with Windows high contrast enabled. Expected: no unreadable pair, no missing element,
  forced colours honoured throughout rather than half-applied. State plainly that this is a manual
  review — tier 7 requires one and no automated check substitutes.
- **V4.** The contrast measurements for every foreground/background pair in Light and Dark, as a
  table with the measured ratio against the 4.5:1 / 3:1 thresholds. Any pair below threshold is
  recorded in `open-questions` for the design authority; **no Light value is adjusted**.
- **V5.** `grep -rniE '#[0-9a-f]{3,8}\b|FontSize="[0-9]|CornerRadius="[0-9]' src/Pegasus.Desktop --include=*.xaml | grep -v '/Styles/'`
  — expected **no matches**. This is the shell-level check the fact automates, and running it by hand
  once confirms the fact's regex and the grep agree.

**Honesty clauses for the proof.**

- Say which [[DUI-001]] case applied (landed, so values kept; or not landed, so values transcribed),
  and confirm exactly one copy of the palette exists.
- Say plainly that the theme evidence is a **manual** sweep and name what was *not* exercised — for
  example, whether 200 % zoom was checked, which belongs to [[DUI-002]] (plan handle `DSK-06-02`)'s
  gallery review rather than here.
- A green `BuildAndRun.ps1` is **not** the same claim as a green `dotnet build`: the script injects a
  project-level `Directory.Build.props` (`.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172`,
  its existence test at `:152` against the project directory only) that shadows the root one and
  drops `TreatWarningsAsErrors`. V1 is authoritative.
- Note which of the eight load-order entries are **reserved but empty** (`Icons.Lucide.xaml`, the
  `Controls.*.xaml` set) and name their owning tickets, so the reviewer does not read an empty
  dictionary as an omission.

## Risks / open questions

- **Risk — A-FND034-1: a missing or misnamed theme dictionary fails at runtime, not at compile
  time.** A `Default` key instead of the three explicit ones silently works in light mode and throws
  on theme switch. *Mitigation*: step 4's explicit rule, and step 10's three-theme sweep, which is
  the only thing that exercises all three paths. A single-theme screenshot is not evidence.
- **Risk — A-FND034-2: forced colours half-applied.** If some HighContrast entries map to
  `SystemColor*` and some do not, high-contrast mode produces a mixture that is worse than no support.
  *Mitigation*: step 5 maps **every** entry; V3's high-contrast screenshot is the check; and
  `HighContrastAdjustment="None"` is refused outright (`.codex/skills/winui-design/SKILL.md:146`).
- **Risk — A-FND034-3: the literal scanner's regex is wrong in either direction.** A false positive
  blocks legitimate work; a false negative leaves a guard that has never fired, which
  `docs/engineering.md` § Lessons says to delete. *Mitigation*: step 9's prove-red-then-green ordering
  and V2's requirement to paste **both** runs. Take particular care that `#` inside a comment or a
  `{Binding}` path does not match.
- **Risk — A-FND034-4: the Dark column may fail the contrast thresholds.**
  `tokens-and-theme.md` § Contrast states both the requirement and that the Dark values "are starting
  points to be adjusted by that review, **not authority**". *Mitigation*: step 11 measures; a failing
  pair becomes an **open question** for the design authority, recorded in this ticket's
  `open-questions` document, with the escalation rule written there. **A Light value is never adjusted
  to make a pair pass** — that would be an edit to the authority made from a downstream ticket.
- **Risk — two copies of the palette.** [[DUI-001]] and this ticket both touch these dictionaries.
  *Mitigation*: the split is settled in the ticket body and step 2 records which case applied. One
  directory, one merge, one guard test — the Guardrails name duplication as the failure mode.
- **Risk — the guard starts failing other areas' PRs the moment it lands.** Placing it in
  `tests/Pegasus.ArchitectureTests` means the CI `unit` lane (`.github/workflows/ci.yml:136`) runs it
  on every PR from every area immediately. That is the intent, not a side effect. *Mitigation*: say
  so in the PR description so nobody is surprised by a red lane on unrelated work.
- **Risk — a `{ThemeResource}` key [[FND-033]] references does not exist here.** A missing key is a
  runtime XAML failure. *Mitigation*: step 10 uses the shell itself as the visual subject, so a shell
  that launches in all three themes is the evidence that every key it references resolves.
- **Sequencing, recorded not resolved — [[FND-030]] and [[FND-033]] must both have landed.**
  [[FND-030]] (plan handle `DSK-02-05`) creates `App.xaml`; [[FND-033]] supplies the shell that step
  10 screenshots. The plan's dependency arrow names only [[FND-033]].
- **Scope boundary, not an open question — the token values, the glyphs, the control styles and the
  gallery page.** [[DUI-001]], [[DUI-003]], [[DUI-006]]/[[DUI-008]]/[[DUI-009]]/[[DUI-010]] and
  [[DUI-002]] respectively.
- **Scope boundary, not an open question — the 2px-versus-6px radius discrepancy.**
  `docs/design/README.md:268` settles it for this ticket ("There is no second approved radius") and
  `tokens-and-theme.md` records that the divergence in `site.css` / `.design-sync/conventions.md` /
  `.stitch/DESIGN.md` is already flagged to the design owner. It is noted in `open-questions` under
  `## Parked` for visibility, not raised again here.
- **An `open-questions` document IS created on this ticket**, because the body instructs it twice —
  step 11 ("Record any pair that fails as an open question for the design authority") and
  § Documentation changes ("record contrast-review outcomes for the Dark column as an open question
  in the ticket, not as a silent edit"). It is created with the contrast review **parked**, because
  the answer is produced by step 11 of this ticket's own work and blocking `leave-preparing` would
  stop the ticket ever reaching the step that measures it. The escalation rule is written into that
  document: if step 11 finds a failing pair, the implementer converts it to an unticked `- [ ]` item
  above `## Parked`, which then correctly blocks `enter-review` and `enter-done` until the design
  authority answers. No settled operator decision (D-002, D-003, D-004, the Send-to-AI exclusion) is
  reopened, and the split with [[DUI-001]] is not re-opened either.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
