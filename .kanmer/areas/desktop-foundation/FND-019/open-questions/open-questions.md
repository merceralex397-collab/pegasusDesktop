# Open questions — FND-019

These boxes are the gate. For profile `spike` an unticked `- [ ]` line **above** the
`## Parked` heading blocks `enter-done` — and only `enter-done`; it never gates
`leave-backlog`. Verified with `get_doc_gates` (no id): `spike` resolves to
`enter-done: [research, questions-resolved]` and nothing else.

Every box below corresponds to a `NOT YET CAPTURED` block in the `research`
document. Tick a box only when the answer is written into
`docs/desktop/01-inventory-and-parity/flow-records.md` (or moved to
`docs/open-decisions.md`) **and** recorded on the box itself.

- [x] **U-1 · `Q1.1` — do the OpenIddict EF tables carry the grants refresh-token
      rotation needs?** Matters because a missing `GRANT` ships green locally and
      fails only in production (the upstream `PLAT-035` class, carried here by
      [[PLAT-018]] (plan handle `DSK-10-18`); grants have shipped wrong three times).
      Answered by the implementer from the migrations. Unblocked by: the table list,
      the grant status of each with `path:line`, and a yes/no. Recommended answer:
      the tables exist (research F-7); expect the gap to be in the grants, not the
      schema.
- [x] **U-2 · `Q1.2` — which claims must the token carry, and how often must
      `IsEnabled` be re-checked?** Matters because guessing produces a desktop that
      signs an operator out mid-case or keeps a disabled account alive. The claim set
      is a fact (read `StaffActorFactory.TryCreate`); the interval is a **decision for
      area 04** and this ticket proposes, it does not choose. Recommended: state the
      claim set as `sub` plus one `role` claim per `StaffRole` name spelled exactly as
      the enum member (research A-01-1), and offer the interval options with the
      operator-visible cost of each.
- [x] **U-3 · `Q1.3` — how is `MustChangePassword` surfaced to a token client?**
      Matters because inventing a problem type or a claim name here would bind area
      04 to something nobody decided. Answered by: reading
      `src/Pegasus.Web/Program.cs:875-899`. Recommended: the code does not settle it —
      add one named line to `docs/open-decisions.md` and stop, as the ticket's step 5
      instructs.
- [x] **U-4 · `Q1.4` — does `DevelopmentOfflineAuthenticationHandler` get a token
      equivalent for the local Test/UAT stack?** Matters because the Test/UAT stack is
      local by decision (L-02, ADR-0014) and an answer that asks for an Azure test
      resource is out of bounds. Answered by the implementer from
      `src/Pegasus.Web/Program.cs` and `docs/runbook.md` § Offline development
      profile.
- [x] **U-5 · Microsoft Learn citations for the token and client-storage facts.**
      Matters because `AGENTS.md` and this programme forbid answering an API question
      from memory. Unblocked by: a Microsoft Learn URL **and a fetch date** beside each
      of the OpenIddict refresh-rotation, `PasswordVault` and `ProtectedData` answers.
- [x] **U-6 · `Q2.1` — does any desktop feature need a new table in Phases 0–4?**
      The plan's expected answer is none. Unblocked by: the list of desktop-held state
      (proposal §11.1) with where each item lives instead, then the yes/no.
- [x] **U-7 · `Q2.2` — how is the desktop OpenIddict client seeded, and which runtime
      role needs which grant on the token tables?** Answered from
      `src/Pegasus.Web/Mcp/AutomationMcp.cs` (how the Automation client is seeded
      today) and `scripts/Test-MigrationGrants.ps1` (the grant rule).
- [x] **U-8 · `Q2.3` — does upstream `PLAT-035`'s build-time grant check land before
      the first gateway schema change?** Answered from the carry-over triage in
      `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`; the fork
      ticket that carries it is [[PLAT-018]] (plan handle `DSK-10-18`). Record the
      ordering constraint either way.
- [x] **U-9 · the actual migration count at the head this ticket runs on.** Matters
      because the acceptance criterion says the number must come from a re-run, not
      from the plan. Use the corrected command in research F-2 — the two published
      commands return 104 and 103, and the real count is 64 at
      `bbd1c549`. State the value observed and the first and last migration ids.
- [x] **U-10 · `Q3.1` — which per-mailbox "last successful cycle" fields already
      exist?** Answered from `src/Pegasus.Core/Operations/` and the approved-mailbox
      model, with `path:line` per field, split into "exists" and "needs adding".
- [x] **U-11 · `Q3.2` — do the Web runtime role's grants already cover the gateway's
      new retained-mail read endpoints?** Matters for the same reason as U-1: a local
      full-privilege run proves nothing about deployed permissions. Unblocked by: the
      granting migration `path:line` per table. The record's "they should — same Web
      role" is an assumption, not an answer.
- [x] **U-12 · `Q3.3` — ADR-0024 stable-mailbox-identity timing relative to the
      Phase 5 Inbox slice.** Answered from
      `docs/adr/0024-stable-approved-mailbox-identity-and-explicit-baseline.md` and
      the carry-over triage. Record the consequence of each option.
- [x] **U-13 · read-only Azure confirmation of the nine `AzureWebJobs.*.Disabled`
      setting names.** **Operator/session step** — the session must be authenticated
      with a reader-level role before the Azure MCP `functionapp` show of
      `pegasus-prod-worker-252ow37gij` can run. Names only; no value read, no other
      Azure tool called, zero writes. A function reported disabled is the designed
      fail-closed state (`infra/modules/platform.bicep:36`), not a fault.
- [x] **U-14 · records 1, 2 and 3 written back and closed.** Every `Q` heading in
      `flow-records.md` reads `Answered <date>: …` or
      `Moved to docs/open-decisions.md <date>`, `pwsh ./scripts/Test-DocumentationLinks.ps1`
      and `pwsh ./scripts/Test-MarkdownPlacement.ps1` both exit 0, and Phase 0
      exit-gate item 3 is satisfied.

## Parked (explicitly deferred)

Everything below this heading is **not** counted by the gate. It is safe to defer
because the ticket is fully executable without it.

- [ ] The ticket's own Verification block carries two commands that match nothing in
      this tree: `git grep -n "Function(\"" src/Pegasus.Worker` (the source uses
      `[Function(nameof(X))]`) and
      `git ls-files src/Pegasus.Infrastructure/Persistence/Migrations | grep -c "\.cs$"`
      (returns 104, not 64). Corrected commands and the real values are recorded in
      the `research` document under F-2 and F-3, so no answer is lost. Correcting the
      ticket text and record 2's own verification block is owned by [[FND-052]]
      (board grooming — unrunnable verification commands). Reopened if [[FND-052]]
      declines the scope.
- [ ] Whether the flow-record corrections should also refresh the "inventoried at"
      SHA on `PAR-01`…`PAR-04`, `PAR-21`, `PAR-22`, `PAR-27` and `PAR-35`. Deferred
      because [[FND-023]] (plan handle `DSK-01-10`) owns re-stamping parity rows after
      the first upstream sync, and doing it twice would create a stale second stamp.
      Reopened if this ticket's corrections change a cited `path:line` in one of those
      rows.
