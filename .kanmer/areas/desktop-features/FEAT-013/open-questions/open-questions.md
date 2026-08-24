# Open questions — FEAT-013

One question, opened because the **ticket body instructs it**. Step 2: "Record
the real limits with evidence in `research`, raise the discrepancy under the
ticket's open questions and get it resolved before leaving Preparing — do not
implement to the plan prose over the code."

An unticked `- [ ]` above the `## Parked` heading blocks `leave-preparing`,
`enter-review` and `enter-done` (verified with `get_doc_gates`). It does **not**
block `leave-backlog`. Blocking is the intended behaviour here.

- [ ] **Is a staff Upload one file, or a batch of up to twenty — and which does
      the desktop implement?** It matters because the answer sets the queue's
      shape, the client-side check, three contract-test boundaries and the screen
      spec's wording, and because implementing to the prose would ship a desktop
      that refuses submissions the web accepts today.
      **The measured evidence, from this ticket's `research`:** the code accepts a
      **batch**. `src/Pegasus.Core/Intake/IntakeContracts.cs:41` declares
      `MaximumBatchFileCount = 20` with the comment "The most files one staff
      Upload submission may select as a single group"; `:13` bounds each file at
      `MaximumContentLength = 10 * 1024 * 1024`; `:49-50` bounds the request at
      `MaximumBatchContentLength = (20 × 10 MiB) + MultipartOverhead`; `:56` sets
      `MultipartOverhead = 64 * 1024`.
      `src/Pegasus.Web/Pages/Upload.cshtml.cs:38` binds `IFormFile[] Upload`;
      `:67-73` refuses more than twenty files with "You selected {n} files. Submit
      20 or fewer at a time."; `:74-89` refuses each empty file and each file over
      the per-file cap. `src/Pegasus.Web/Program.cs:525-530` bounds the multipart
      body to `MaximumBatchContentLength` with the comment "Bounded for a whole
      Upload batch, not one file". `src/Pegasus.Web/Pages/Upload.cshtml:36` sets
      `multiple` on the input.
      **What says otherwise:**
      `docs/desktop/05-implementation-and-migration/vertical-slices.md:461-462`
      ("upload one file (≤ 10 MiB; …)") and
      `docs/desktop/06-ui-design/screen-specs.md:311` ("Drop target plus file
      picker (one file ≤ 10 MiB …)"). Both are plan prose written before the
      batch existed.
      **Who can answer:** the plan owner for area 05.
      **What unblocks it:** an agreed answer recorded here, carried into the
      `screen-specs.md:309-317` Upload block at plan step 13 and into the three
      contract-test boundaries at step 10.
      **Recommended answer:** the **code** — a batch of up to twenty files, each
      ≤ 10 MiB, with the request bounded by `MaximumBatchContentLength`. The
      ticket's own Traps say "the code wins and the discrepancy is recorded, not
      silently resolved", and its acceptance criteria already say "within the
      limits actually enforced by `IntakeEnvelopeLimits`". Accepting this corrects
      the prose in `vertical-slices.md` and `screen-specs.md`; the
      `vertical-slices.md` correction belongs to whoever owns that block, and the
      `screen-specs.md` Upload block is this ticket's.

## Parked (explicitly deferred)

Nothing below this heading is counted by the gate.

- [ ] **The operator-facing word for the retry-scheduled waiting state.** Safe to
      defer past `leave-preparing` because the ticket already fixes the method:
      plan step 8 takes the word from the settled operator vocabulary in
      `docs/design/README.md` rather than inventing one, and reconciles it with
      `docs/frd/frd-02-intake-and-source-identity.md`. The wire value is not in
      question — it stays `retry_scheduled`, spelled as
      `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs:722` already
      persists it, and it belongs to [[GWY-011]] (plan handle `DSK-03-11`).
      **Reopens if:** no settled word in `docs/design/README.md` covers a receipt
      that is safe, retained and waiting for a scheduled retry. Coining one and
      writing it into an FRD would be worse than asking, so at that point it
      becomes an operator question rather than a default.

## Trivial defaults taken rather than asked

- **Poll-interval clamp bounds**: minimum 2 s, maximum 60 s, target = the time
  remaining to `dueAtUtc` bounded into that range, with a null `dueAtUtc` falling
  back to the minimum. Taken as a default and recorded in the plan's step 8
  because the ticket requires the interval to be "derived … (clamped — record the
  bounds in `plan`)" without naming the bounds. Two seconds preserves today's
  responsiveness for an item about to run; sixty seconds keeps a two-hour wait
  from costing 3,600 requests.
- **The desktop surfaces only the three upload bounds** — per-file, batch count
  and batch envelope — and never `MaximumMailboxContentLength` (750 MiB,
  `src/Pegasus.Core/Intake/IntakeContracts.cs:34`), which is a received-message
  bound. Taken as a default because that file's own remark records the incident
  caused by applying one bound where the other belonged.
- **INTK-001's `document.hidden` half is recorded as moot** rather than
  reimplemented as a window-visibility rule. Taken as a default because the ticket
  says so in its own words and the desktop has no background tab.
