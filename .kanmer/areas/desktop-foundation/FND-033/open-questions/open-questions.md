# Open questions — FND-033

Opened because this ticket's `## Documentation changes` binds the author to it: *"`docs/desktop/06-ui-design/screen-specs.md`
— no change; it is the source. **Record any spec ambiguity as an open question in the ticket, not as
an edit.**"* One ambiguity was found. It is written as an unticked item because it must be answered
before step 5 writes the badge.

**What an unticked item here actually blocks**: `leave-preparing`, `enter-review` and `enter-done`.
It does **not** block `leave-backlog`. Verified with `get_doc_gates` — `questions-resolved` appears in
those three boundary lists for profile `feature` and in no other.

---

- [ ] **Which channel drives which environment-badge label?** The badge is `Shell.Title.Environment`,
      written at plan step 5.

  **The ambiguity, stated exactly.**
  `docs/desktop/06-ui-design/screen-specs.md` § Shell says the title bar carries an "environment badge
  (non-production only: **"Pilot"**, **"Test/UAT"**, **"Development"**)" — three labels for the
  non-production case.
  `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 7 defines exactly three
  channels — **`pilot`**, **`production`**, **`local`** — selected at package time by the
  `PegasusChannel` MSBuild property, and
  `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 item 8 confirms the package carries
  only "the gateway base URL, feed URL, and channel name per channel".
  So `pilot` → "Pilot" and `production` → badge hidden are unambiguous, but **two labels ("Test/UAT"
  and "Development") compete for the single remaining channel `local`, and one of them has no channel
  at all.**

  **Why it is not a detail.** This ticket's own § Why states the consequence: "the environment badge
  is what stops an operator doing pilot work believing they are in production". The badge's text is
  operator-facing copy, and `docs/design/README.md` is the binding design authority for operator copy
  (`AGENTS.md` § Simplicity rails); the authority order in
  `docs/desktop/00-governance-and-workflow/README.md` § 3 puts `design/README.md` **above** these
  plans. So the resolution is the design authority's to give, not this plan's to assume.

  **The default this plan would otherwise take, if the answer is "just pick one".**
  `local` → **"Test/UAT"**, and "Development" corresponds to no channel and is therefore never
  rendered. Reasoning: L-02 fixes the `local` channel at the local **Test/UAT** stack, and "Test/UAT"
  is the name the plan set uses for that stack throughout. Taking this default without confirmation
  would silently retire one of the three labels the screen spec names, which is why it is asked rather
  than assumed.

  **Who answers it**: the design authority owner, through
  `docs/design/README.md`'s change and verification rule. One of:
  1. confirm `local` → "Test/UAT" and record that "Development" is retired from the badge; or
  2. confirm `local` → "Development" and record that "Test/UAT" is retired; or
  3. add a fourth channel so all three labels are reachable — which is a change to plan 02 § 3
     decision 7 and plan 04 § 3 item 8, not a change this ticket may make, and would need
     [[FND-032]] (plan handle `DSK-02-07`) to revise its configuration set.

  **What unblocks**: plan step 5 (write the badge), the view-model test "environment badge hidden in
  the production channel and shown otherwise", and [[DUI-004]] (plan handle `DSK-06-04`) step 7, which
  renders the same badge.

## Parked (explicitly deferred)

Nothing is parked. The two other overlaps that could have been written here are **scope boundaries
with named owners**, not open questions, and they are recorded in the plan's *Risks / open questions*
section instead:

- The shell file path — `src/Pegasus.Desktop/Shell/ShellPage.xaml` (this ticket) versus
  `src/Pegasus.Desktop/Views/ShellPage.xaml` ([[DUI-004]] step 3). Settled in the plan: this ticket
  creates the file, so `Shell/` is the path, and [[DUI-004]] — whose own § Source of truth casts it as
  dressing "the shell scaffold … this ticket dresses" — dresses it there.
- The keyboard map — this ticket wires only the `screen-specs.md` § Shell subset; [[DUI-014]] (plan
  handle `DSK-06-14`) owns the full map in
  `docs/desktop/06-ui-design/keyboard-and-accessibility.md`. A subset, not a conflict.

Also **not** opened, because it resolved on inspection: the seven-route rail order. The abbreviated
restatement at `docs/design/README.md:474-475` omits Operations, but the canonical list at `:30-38`
includes it as route 6 ("settled by the operator on 2026-08-04"), `:1089-1091` reconciles the two, and
`src/Pegasus.Web/Pages/Operations/Index.cshtml` exists. The reconciliation is recorded in this
ticket's `research` document so the next reader does not re-open it.
