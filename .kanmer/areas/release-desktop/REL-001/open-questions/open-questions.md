# Open questions — REL-001 (plan handle `DSK-09-01`): ADR-0105 ownership

**Why this document exists.** The ticket body instructs it. Its § Guardrails paragraph
"Ownership overlap — ADR-0105 has three claimants" ends: *"if another ticket has already
authored it, this ticket becomes a review of that ADR against § 3 of the area 09 plan plus
whatever area 09 still owes, and the change of shape is recorded in `open-questions/`"* — and
the same paragraph says which ticket authors ADR-0105 is *"an **ownership question for the
operator to settle before Phase 2**, not something the first agent to start decides
silently"*. The body outranks the author, and that applies here.

The earlier plan declined to open this, reasoning that an unticked item "would block every
stage move". That is false. An unticked `- [ ]` line above `## Parked` blocks exactly three
boundaries — `leave-preparing`, `enter-review` and `enter-done` — and never `leave-backlog`.
For profile `chore` the board declares two of those three (`get_doc_gates` with no id:
`chore` → `leave-preparing: [plan, questions-resolved]`, `enter-done: [proof,
questions-resolved]`). So these boxes stop this ticket **leaving Preparing** — which is
exactly the point, because the thing they guard is an operator answer that must arrive
before an agent starts writing the ADR.

Both boxes are answerable without implementing anything: one is an operator question, the
other is two read-only checks.

## Unresolved

- [ ] **Which of the three claimants authors `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`?**
      The operator settles this before Phase 2. The three claimants are
      [[REL-001]] (plan handle `DSK-09-01`, this ticket, phase-0),
      [[FND-005]] (plan handle `DSK-00-05`, "Author ADR-0100, ADR-0101, ADR-0103, ADR-0104,
      ADR-0105 and ADR-0110 in the reserved block", phase-0) and
      [[FND-042]] (plan handle `DSK-04-01`, "Author ADR-0102 … and ADR-0105", phase-2).
      Verified on the board 2026-08-24: all three are in `backlog`, none is taken, and none
      has authored anything.

      The body's tie-break — *the first of the three to be worked authors the file, and the
      other two verify it covers their content and extend it in place, never a second file
      for the same number* — governs **execution**. It does not answer this question, because
      the body says in the same breath that ownership is not for the first agent to start to
      decide silently. Tick this box when the operator's answer is recorded **verbatim with
      its date** in the plan document, and say there whether it confirmed the tie-break or
      named a different author.

      If the answer names another ticket, this one becomes the review-and-extend shape of the
      body's rule; record that in the box below rather than proceeding as author.

- [ ] **Has ADR-0105 already been authored, and does this ticket therefore change shape?**
      Body: *"Before step 3, check the board with Kanmer `search_items` for `ADR-0105`"*.
      Run both checks and record the outcome and its date here:

      - `mcp__kanmer__search_items ADR-0105`
      - `ls docs/adr/0105*`

      Measured 2026-08-24: `ls docs/adr/0105*` returns *No such file or directory*, and
      `search_items` returns the three claimants above plus the related
      [[GWY-023]] (plan handle `DSK-04-06`, the gateway-side version gate) — so **outcome (a)
      applies today: no file, nobody has authored it.** Re-run rather than trusting that
      line; the whole reason the check exists is that two agents can work area 00 and area 09
      in parallel.

      The three outcomes and what each means:
      - **(a) no file, no other ticket has authored it** → this ticket authors it. Record
        outcome (a) with its date and tick.
      - **(b) the file exists** → this ticket becomes a **review** of that ADR against § 3 of
        the area 09 plan plus whatever area 09 still owes, extending it in place. **This is
        the change of shape the body requires be recorded here** — write what the existing
        ADR covers, what area 09 still owes, and what this ticket will add. Never create a
        second file for the same number.
      - **(c) another claimant is in `implementing` on it** → stop and coordinate rather than
        race. Leave this box unticked; that is the correct state while a race is live.

## Parked (explicitly deferred)

- **Whether ADR-0105 should have been ADR-0030, the next free number.** Not open: settled by
  the operator on 2026-08-23, who confirmed the reserved block ADR-0100–ADR-0110 for the
  conversion precisely so a one-way sync from the still-active upstream ADR sequence cannot
  collide. Recorded at `AGENTS.md:80-88`. Plan step 3 re-reads that sentence before using
  0105; it is a confirmation, not a question.

- **FRD-13.** Not open, and not this ticket's: `docs/frd/README.md` lists FRD-01…FRD-12 and
  [[FND-008]] (plan handle `DSK-00-08`) authors FRD-13. A decision a named sibling ticket
  owns is a scope boundary, not an open question. Plan step 8 writes the forward pointer as
  prose with no relative link, so `scripts/Test-DocumentationLinks.ps1` stays green.

- **D-002 (self-managed certificate) and D-003 (in-house UNC share).** Not open and not to be
  re-opened — both decided by the operator on 2026-08-23. The ADR records them; it does not
  re-evaluate them.
