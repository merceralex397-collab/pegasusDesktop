# Open questions — FND-042 (plan handle `DSK-04-01`)

## Resolved

- [x] **Which ticket authors `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`?**

  **Answered 2026-08-24 by the operator: [[FND-005]] owns ADR-0105.** It is the sole authoring ticket for that path. [[FND-042]] may review the resulting ADR against the Phase-2 token/session and minimum-version-gate requirements and extend that one file only when its own scoped work genuinely requires it; it must never create a second ADR-0105 file.

  This replaces the former “first claimant to start authors it” tie-break. The current execution shape for this ticket is ADR-0102 authoring plus ADR-0105 review/extension only after FND-005's canonical file exists.

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
