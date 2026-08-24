# Open questions — FEAT-011

Two questions, both opened because the **ticket body instructs it**, not because
the author chose to. Step 2: "note the discrepancy under the ticket's open
questions, and get it resolved before leaving Preparing — do not assume a
number." Guardrails: "Confirm the surface is wanted and record the FRD-03 answer
before the section ships; an unticked open question blocks the Kanmer move,
which is correct here."

An unticked `- [ ]` above the `## Parked` heading blocks `leave-preparing`,
`enter-review` and `enter-done` (verified with `get_doc_gates`). It does **not**
block `leave-backlog`. Blocking is the intended behaviour on this ticket.

- [ ] **How many triage actions are there — twelve, thirteen, or ten?** It
      matters because proposal §10.2 forbids a generic action endpoint, so "how
      many actions" is the same question as "how many `/api/v1` routes", and
      [[GWY-013]] (plan handle `DSK-03-13`) cannot be verified against a number
      nobody has agreed. **The measured evidence, from this ticket's `research`:**
      `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:114-210` has **twelve**
      `case` labels — `assign`, `unassign`, `await_information`, `record_finding`,
      `supersede_finding`, `link_response`, `unlink_response`, `complete`,
      `cancel`, `reopen`, `link_case`, `unlink_case` — with a `default` at
      `:208-210` that throws. `src/Pegasus.Web/Mcp/TriageMcpTools.cs` declares
      thirteen tools of which **ten** are mutations (`:98`–`:143`) and three are
      reads (`:37`, `:66`, `:81`); the two labels with no MCP tool are exactly
      `assign` and `unassign`, which accounts for the ten-versus-twelve gap.
      `docs/desktop/05-implementation-and-migration/README.md:119-123` and
      `docs/desktop/01-inventory-and-parity/parity-matrix.md` `PAR-24` both say
      **thirteen**, and `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
      lists twelve routes with the note "verify the full set".
      **Who can answer:** the plan owner for area 05, with [[GWY-013]]'s author.
      **What unblocks it:** an agreed count recorded here and carried into
      `PAR-24` at plan step 14 and into [[GWY-013]]'s route list.
      **Recommended answer:** twelve — the dispatcher is the executable authority,
      the MCP set is twelve minus the assignment pair, and no thirteenth label
      exists in the source read on 2026-08-24. If that is accepted, `PAR-24`'s
      "dispatches 13 commands" and the plan text's "thirteen" are corrected to
      twelve.

- [ ] **Is a Triage evidence surface wanted at all, and should
      `docs/frd/frd-03-triage.md` record it as required behaviour?** Carried
      forward from upstream INTK-034, which records this as an operator question
      and asks the FRD-03 half explicitly. **Why it matters:** both QDOS Triage
      templates attach the client's damage photographs and assessing them is the
      whole point of a Triage, yet today the engineer reaches them only by
      navigating out to the originating e-mail — `src/Pegasus.Web/Pages/Triage/Details.cshtml:56`
      links to `/Intake/Details/{Origin.ReceiptId}` and
      `docs/desktop/06-ui-design/screen-specs.md:287-296` lists evidence, reply
      evidence, findings, responses and the linked case but **no gallery**.
      **What is not in question:** surfacing the receipt's existing assets
      duplicates no custody (they are read over the existing byte endpoints);
      retaining the images a second time under the Triage would, and is a stop
      condition either way. **Who can answer:** the operator.
      **What unblocks it:** a yes/no on the surface, plus — if yes — confirmation
      that FRD-03 records it as required behaviour. Plan step 10 does not ship
      until this is answered, and `docs/frd/frd-03-triage.md` is written only on a
      yes.
      **Recommended answer:** yes, with FRD-03 recording it — the read-only
      surface costs no custody, and the alternative is shipping a desktop Triage
      that still sends the engineer to an e-mail to see the damage.

## Parked (explicitly deferred)

Nothing below this heading is counted by the gate.

- [ ] **Whether `ITriageQueries.GetByOriginReceiptAsync` should be added by this
      slice.** Safe to defer: it does not exist in the fork today
      (`src/Pegasus.Core/Triage/TriageContracts.cs:288-294` carries only
      `ListAsync` and `GetAsync`), it arrives with upstream INTK-033 (board
      [[INTK-007]]), and resolving it after [[FND-023]] (plan handle `DSK-01-10`)'s
      sync is [[GWY-013]] step 8's named work. It is a scope boundary with an
      owner, not a question for this ticket. **Reopens if:** [[GWY-013]] declines
      the ownership, or [[INTK-007]] is dropped — in which case the Triage detail
      has no supported route to its origin receipt id and open question 2's
      implementation is blocked on a different ticket.

## Trivial defaults taken rather than asked

- The request records partition on the evidence
  (`Details.cshtml.cs:107-112` reuses one `TriageMutationRequest` across five
  actions), so five commands share that shape and seven carry their own. No union
  record with eleven nullable fields, and no question raised about it.
- `link_response` sends the poll-outcome id and sent-evidence id **pair** parsed
  at `Details.cshtml.cs:156-170`, not the raw `responseCandidate` string. Taken as
  a default because the page model already does the parse and the desktop must not
  become a second parser.
