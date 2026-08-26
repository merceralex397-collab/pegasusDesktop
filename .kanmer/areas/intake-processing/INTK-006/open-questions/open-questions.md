# Open questions — INTK-006

- [ ] Choose the operator-visible outcome for a standalone audit whose attached report does not yield exactly one Repairable/Total Loss result, and provide the exact wording to record. The implementation must remain fail-closed and must not allocate an immutable `a.` or `ap.` reference from a guess. Choose one:
  1. Named blocked state: retain the receipt and report, create no case, and show wording that tells the operator the report outcome could not be read.
  2. Named needs-attention state: retain all readable facts and the missing outcome, create no case, and show wording that directs staff to resolve the report.
  3. Staff-confirmation route: retain the receipt and require staff to confirm Repairable or Total Loss before allocation, with wording that makes the confirmation responsibility explicit.
  The chosen state, wording, and date must be recorded in research and the carry-over register before implementation.
- [ ] Confirm the local dependency boundary. The current repository instruction forbids upstream synchronization; this ticket may use the already imported INTK-005 research as read-only provenance and the current `origin/dev` code, but must not wait for or perform a future upstream sync. Confirm that this local fork is the implementation source.

## Parked (explicitly deferred)

None.
