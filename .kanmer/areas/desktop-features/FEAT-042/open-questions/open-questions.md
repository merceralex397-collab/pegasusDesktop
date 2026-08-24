# Open questions

**This document exists because the ticket body ordered it.** FEAT-042's Guardrails read: "**Open
question (operator), to be resolved and recorded in this ticket's `open-questions` document before
step 3 is implemented**". The body is settled and outranks the author, so the question below is
recorded as an unticked item rather than reasoned away.

The unticked item holds `leave-preparing`, `enter-review` and `enter-done` shut. That is the
intended behaviour and the reason the body required it. It does **not** gate `leave-backlog` — the
ticket can be groomed and sequenced while the question stands.

---

- [ ] **Is desktop report generation automatic, or an operator-initiated command?**

  **The conflict.** **Upstream DOCS-001 (board [[DOCS-001]])** records report generation as
  **automatic** — "detects a complete, accepted assessment, invokes the integrated renderer".
  `docs/desktop/06-ui-design/screen-specs.md:379-386` and this ticket make Generate an
  **operator-initiated** command with the AutomationId `Case.Reports.Generate`, "progress in status
  bar; cancel".

  **What the operator has already said, so this is not a fresh question.** Board [[DOCS-001]]'s own
  `open-questions` document states: "The operator has selected automatic generation when all
  required assessment details are accepted, immutable version/hash/custody, idempotent replay,
  append-only correction versions, **human approval before issue**, and no separate renderer
  runtime." That selection was made in the Razor/upstream context, before L-03 moved rendering to
  the desktop.

  **What is genuinely undecided.** Whether that selection carries across to the desktop, where the
  renderer no longer lives on an always-on host. Automatic generation on the desktop would mean a
  workstation renders when it happens to be open, which is a different thing from a server
  rendering on detection. The phrase "human approval before issue" is compatible with more than one
  reading of what "automatic" governs — **but the body forbids resolving it that way**: "do not
  invent a hybrid, and do not implement automatic generation on the strength of the upstream wording
  alone." So the observation is offered as evidence, not taken as the answer.

  **Why it must be answered before step 3 is implemented.** Step 3 places the readiness gate. If
  generation is automatic, "complete and accepted" is a *trigger* the gateway evaluates and
  something must own the detection and the schedule; if it is operator-initiated, "complete and
  accepted" is a *refusal reason* returned to a command the operator issued. Those are different
  endpoint contracts, different `NotReady` semantics and different desktop surfaces. Building one
  and converting later means rewriting the gate this ticket exists to install.

  **What the answer must say, to be usable:** (a) automatic or operator-initiated, for the desktop
  specifically; (b) if automatic, which host evaluates the trigger — the gateway, the Worker, or an
  open desktop — since L-03 placed only the *rendering* on the client; (c) whether upstream
  DOCS-001's selection is hereby confirmed as unchanged for the desktop or superseded for it, so
  that board ticket's plan is written to the same answer.

  **Answered by:** the operator. Record the decision inline above, tick the box, and cite it in the
  `plan`'s step 3 under a dated heading. If the answer changes the endpoint contract, revise the
  `plan` and `files` documents before implementation rather than absorbing it in code.

---

## Parked (explicitly deferred)

Nothing is parked for this ticket. The items below are recorded here only so a reader does not
mistake them for open questions — each is a **scope boundary owned by a named ticket**, and each is
already listed in the `plan`'s *Risks / open questions* section.

- The append-only issued-version-to-Sent-evidence ledger belongs to **upstream TICK-208 (board
  [[DOCS-003]])**, already imported. Board `DOCS-003` is upstream TICK-208 — **not** upstream
  `DOCS-003`, which is an unrelated post-alpha RPT-04 activation gate with no fork ticket at all.
- The parity flag's name and flip authority belong to [[FEAT-040]] (plan handle `DSK-07-14`) step 10
  and [[FEAT-038]] (plan handle `DSK-07-12`) step 9, whichever lands first.
- Whether the readiness refusal needs a new `urn:pegasus:problem:*` slug is
  [[GWY-001]] (plan handle `DSK-03-01`)'s list to change; this ticket records the choice, it does not
  invent a URN.
- The golden-file parity sign-off that permits switching the gateway renderer off is [[FEAT-041]]
  (plan handle `DSK-07-15`)'s results table.

## Defaults taken rather than asked

Recorded per the authoring contract, so a reviewer can see they were decisions.

- **Report record shape** — the existing approval row plus an ordinary case document version, not a
  new table (assumption `A-07-16-1`). Taken as the default because Core already models finality that
  way; the `plan`'s step 2 verifies it and instructs a stop-and-re-plan if it fails, rather than
  quietly adding a table and its `Grant*` migration.
- **Readiness refusal problem type** — mapped onto an existing slug from the list at
  `docs/desktop/03-gateway-api-and-data/README.md:167` in preference to requesting a new one. The
  `plan`'s step 3 records which.
