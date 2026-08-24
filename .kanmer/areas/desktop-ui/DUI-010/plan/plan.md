# Plan — DUI-010 ProblemInfoBar

## Governing documents

This ticket remains docs_todo: true; use the authority and area plan now, then link the appropriate FRD-13 section once authored. Do not create a competing PRD, FRD or ADR from this ticket.

## Chosen approach

How can gateway ProblemDetails become a concise, copy-safe operator sentence plus a Reference value without raw problem codes or banned words?

## Steps

1. Inspect the API problem-details mapping and authority copy lists.
2. Create the narrow problem-presentation model and InfoBar style using one sentence plus expandable/copyable Reference.
3. Add guard tests for known mappings, banned words and raw-code leakage.
4. Render representative retry, unavailable, denied and validation cases in a test host.

## Verification

- View-model tests fail for a banned term or raw problem code.
- UIA exposes a copyable Reference only when supplied by the gateway.
- InfoBar state remains screen-local and never claims an external action succeeded.

## Risks and dependencies

DSK-03-02 owns the gateway mapping and correlation value; this ticket consumes it.

The implementation must record its simplification pass and independent pegasus-desktop-reviewer assessment before merge.
