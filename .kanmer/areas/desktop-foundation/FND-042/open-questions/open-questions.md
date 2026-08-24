# Open questions — FND-042 (plan handle `DSK-04-01`)

One question, opened because **the ticket body instructs it**: step 3 and the Guardrails both
say *"Which ticket authors it is an **ownership question for the operator to settle before
Phase 2** — record the answer in the plan document before writing; **do not decide it
silently by starting first**."* The body outranks the author, and an unticked box is the only
mechanism that actually prevents "starting first".

An unticked `- [ ]` line above `## Parked` blocks exactly three boundaries for this ticket —
`leave-preparing`, `enter-review` and `enter-done`. It does **not** gate `leave-backlog`.
That is the intended behaviour here: this ticket is labelled `phase-2`, so "settle before
Phase 2" means "settle before this ticket is implemented".

- [ ] **Operator: which of the three claimant tickets authors
  `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`?**

  The three claimants are [[FND-042]] (this ticket, plan handle `DSK-04-01`), [[FND-005]]
  (plan handle `DSK-00-05`) and [[REL-001]] (plan handle `DSK-09-01`). All three state the
  same two reconciled points, so **only the assignment is open**:

  1. **One filename** — `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`, the
     only ADR-0105 path the plan set itself names
     (`docs/desktop/04-auth-session-update-and-startup/README.md:297`).
  2. **One rule** — whichever ticket is worked first authors the file; the other two verify
     that it covers their content and **extend it in place**; none of them ever creates a
     second file for the same number.

  What is needed to close this: one line from the operator naming the authoring ticket.
  Write the answer beside this box, tick it, and repeat it under a dated note in this
  ticket's `plan` document (the body's step 3 requires the plan to carry it).

  Why it is not taken as a default: the body's Guardrails explicitly forbid deciding it by
  starting first, and the three tickets sit in different areas (`desktop-foundation`,
  `desktop-foundation`, `release-desktop`) with different reviewers, so "first worked" is not
  predictable from the board.

  Measured state, 2026-08-24: `ls docs/adr/010*` returns **nothing** and
  `grep -n '0102\|0105' docs/adr/README.md` returns nothing — no claimant has authored it
  yet, so the question is still live rather than already answered by events. Re-run both
  checks when the operator answers.

## Parked (explicitly deferred)

- **Who authors `docs/adr/0102-existing-pegasus-credentials-token-session.md`.** [[FND-006]]
  (plan handle `DSK-00-06`) is the other claimant. Deferred rather than asked, because the
  ticket body does **not** call this one an operator question — it is covered by the same
  one-filename / extend-in-place rule, and step 3's `ls docs/adr/010*` settles it at
  implementation time without anyone deciding anything. It is recorded as a scope boundary in
  the plan's *Risks / open questions* section.
- **The `AGENTS.md` § ADR conventions index-shape sentence** (`:114-117`) describing a
  five-column index that `docs/adr/README.md:18-19` contradicts. Deferred: correcting it is
  [[FND-005]]'s ticket, and this ticket's Guardrails forbid editing `AGENTS.md`. The trivial
  default taken here instead of asking: **the real file wins**, and this ticket writes
  three-cell rows.
- **Whether the ADR index rows cite FRD-13.** FRD-13 is authored by [[FND-008]] (plan handle
  `DSK-00-08`) and may not exist when this ticket runs. Trivial default taken rather than
  asking: `ls docs/frd/` at step 10 and write `—` in the `Related FRD` cell if it is absent,
  recording which applied. A later ticket can fill the cell; a broken link cannot be left.
