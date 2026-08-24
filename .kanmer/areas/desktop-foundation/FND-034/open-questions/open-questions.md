# Open questions — FND-034

This document exists because the ticket body instructs it **twice**, and a body instruction is
binding:

- Step 11 — "Record any pair that fails as an open question for the design authority rather than
  silently adjusting the authority's Light values."
- § Documentation changes — "`docs/desktop/06-ui-design/tokens-and-theme.md` — record contrast-review
  outcomes for the Dark column as an open question in the ticket, not as a silent edit."

**Nothing is blocking today.** Every entry below is parked, because each one's answer is produced by
step 11 of this ticket's own work. An unticked `- [ ]` item above the `## Parked` heading blocks
`leave-preparing`, `enter-review` and `enter-done` — so making the contrast review blocking now would
stop the ticket ever reaching the step that measures it. That is the wrong boundary, not a reason to
avoid blocking; the escalation rule in Q1 puts the block at the boundary where it belongs.

## Parked (explicitly deferred)

### Q1 — Does the Dark colour column meet the contrast thresholds?

**Question.** Does every foreground/background pair in the `Dark` theme dictionary reach **4.5:1 for
body text** and **3:1 for large text and UI boundaries**?

**Why it is a real question and not an assumption dressed up as one.**
`docs/desktop/06-ui-design/tokens-and-theme.md` § Colour tokens says in its own preamble that
"Light values are the authority's. Dark values are an **assumption** (the authority is light-only)",
and its § Contrast paragraph closes it: the Dark values "are starting points to be adjusted by that
review, **not authority**." The mapping document itself declares this unresolved.

**Why it is parked rather than blocking.** The measurement is step 11 of this ticket. No pair can be
recorded before the dictionaries exist and are rendered, so there is no answer to gate on at
`leave-preparing`, and gating there would prevent the work that produces the answer.

**Who answers it.** The implementing agent measures; the **design authority**
(`docs/design/README.md`, through its change and verification rule at `:982`) decides any value
change. `tokens-and-theme.md` § Change rule routes it: a proposed change "is raised against
`docs/design/README.md` … reviewed on the gallery page in Light/Dark/HighContrast at 100% and 200%,
and only then applied to `Styles/`." The gallery page is [[DUI-002]] (plan handle `DSK-06-02`).

**Escalation rule — the implementer discharges this, and it is the point of the entry.**
If step 11 finds any pair below threshold, **move it out of `## Parked`** and write it above this
heading as an unticked item, in this form:

> `- [ ] Contrast: <ForegroundKey> on <BackgroundKey> in Dark measures <n.n>:1, below the <4.5|3>:1
>   threshold. Raised against docs/design/README.md § Change and verification rule (:982). Awaiting
>   the design authority's replacement Dark value.`

It then correctly blocks `enter-review` and `enter-done` until the authority answers — which is the
boundary that matters, because a theme with an unreadable pair must not be reviewed as finished or
closed. It does not block `leave-preparing`, and it never blocks `leave-backlog` (no
`open-questions` item ever does).

**What is refused while it is open.** Adjusting a **Light** value to make a pair pass. The Light
column is the authority's; only the Dark column is the assumption that may move. Silently editing
`tokens-and-theme.md` is refused for the same reason.

**Default taken in the meantime.** Transcribe the Dark values exactly as
`tokens-and-theme.md` § Colour tokens records them and proceed. That is the mapping document's own
instruction ("starting points"), so it is a default, not a guess.

### Q2 — Is `PegasusSectionTextStyle` at 14 acceptable where the authority targets 15/700?

**Question.** `tokens-and-theme.md` § Typography records `PegasusSectionTextStyle`
(`BasedOn BodyStrongTextBlockStyle`, 14 / SemiBold) against an authority target of "15/700", with the
parenthetical "(**assumption**: 14 acceptable; confirm in review)".

**Why it is parked.** It is flagged in the mapping as an assumption to confirm in the same review
that answers Q1, and `tokens-and-theme.md` § Change rule names "the section-heading size" alongside
the Dark values as the two things that must be raised against the authority rather than decided
downstream.

**Who answers it.** The design authority, through the same route as Q1.

**Default taken.** Transcribe 14 / SemiBold as the mapping records it, and change nothing. Recorded
here so the reviewer sees it was a transcription of a flagged assumption rather than an unnoticed
deviation from the authority's 15/700.

### Q3 — The 2px-versus-6px radius discrepancy (noted for visibility; settled for this ticket)

**Not a question this ticket may answer, and not one it needs answered.**
`docs/design/README.md:268` settles it for the desktop: "`site.css` now uses the approved 2px radius
throughout. **There is no second approved radius.**" `tokens-and-theme.md` § Shape records that the
6px/5px appearing in `.design-sync/conventions.md` and `.stitch/DESIGN.md` is "a discrepancy flagged
to the design owner; **not adopted**".

**Default taken.** `ControlCornerRadius` and `OverlayCornerRadius` = `2`, per the authority. The
discrepancy is already with the design owner and is logged here only so a reviewer comparing the
dictionaries against `.stitch/DESIGN.md` does not read the difference as an error in this ticket.

---

## Not open — recorded so they are not re-opened

- **The split with [[DUI-001]] (plan handle `DSK-06-01`).** Settled in this ticket's body, in detail:
  this ticket owns `src/Pegasus.Desktop/Styles/`, its file set and load order, the `App.xaml` merge
  and the `StylesAreTheOnlySourceOfColourAndType` guard test; [[DUI-001]] fills the token values in
  place and creates no second file, merge or scanner. The body says "do not re-open it", and this
  document does not.
- **The contents of `Icons.Lucide.xaml` and the `Controls.*.xaml` set.** Owned by [[DUI-003]] (plan
  handle `DSK-06-03`), [[DUI-006]] (`DSK-06-06`), [[DUI-008]] (`DSK-06-08`), [[DUI-009]]
  (`DSK-06-09`) and [[DUI-010]] (`DSK-06-10`). Scope boundaries with named owners, recorded in the
  plan's Risks section — not questions.
- **Operator decisions D-002, D-003 and D-004, and the Send to AI (AI-09) recorded exclusion.**
  Settled by the operator on 2026-08-24 and untouched by this ticket.
