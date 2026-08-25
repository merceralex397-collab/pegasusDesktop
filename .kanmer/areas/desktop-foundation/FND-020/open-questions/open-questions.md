# Open questions — FND-020

All ten gated questions are resolved for this spike on 2026-08-25. Each answer is
written in `docs/desktop/01-inventory-and-parity/flow-records.md` or
`docs/open-decisions.md`.

- [x] **U-1 · Q4.1** — Box SDK named transfer primitives were inspected and the
      current guarded gateway path plus the default direct-transfer boundary
      were recorded.
- [x] **U-2 · Q4.2** — Core and EF document metadata fields were enumerated.
- [x] **U-3 · Q4.3** — PLAT-041’s backlog state and export ordering constraint
      were recorded in `docs/open-decisions.md`.
- [x] **U-4** — Record 5’s adapter, durable-request, and Worker/secret evidence
      was re-run and written back.
- [x] **U-5 · Q5.1** — No evidence found; default no for direct native
      provider calls.
- [x] **U-6 · Q5.3** — Web handler → Core request → durable store/queue →
      Worker adapter trace was recorded.
- [x] **U-7 · Q6.1** — TICK-206 remains unresolved; six-template scope and
      current callers were recorded in `docs/open-decisions.md`.
- [x] **U-8 · Q6.3** — No ten-workstation evidence is available; the required
      operator observation was recorded in `docs/open-decisions.md`.
- [x] **U-9 · Q6.2/Q6.4** — Official Microsoft and PDFsharp references with
      fetch dates were recorded; fidelity remains a Phase 7 measurement.
- [x] **U-10** — Records 4–6 were written back; the six-template count,
      pinned Playwright/base-image versions, and documentation validations
      were recorded.

## Parked (explicitly deferred)

Everything below this heading is out of the gate and remains owned elsewhere.

- [ ] The duplicate Microsoft.Playwright literal in the integration-test
      project remains [[FND-027]] scope.
- [ ] A future carry-over refresh may restate the already recorded upstream
      decisions; no answer in this ticket depends on that refresh.
