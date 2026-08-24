# Plan — TEST-018 Golden-file report parity lane

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Run area-07 fixtures through the stack and compare desktop WebView2 output to gateway renderer within documented tolerances.

## Steps

1. Confirm the governed fixture set and the permitted comparison tolerances before writing tests.
2. Run gateway and isolated desktop renderer against the same inputs.
3. Compare structural/text/render outputs with documented tolerance; never silently rebaseline.
4. Record mismatches, environment/runtime version and disposition.

## Verification

- Fixture provenance is immutable and comparison threshold documented.
- A deliberate output mismatch fails the lane.
- Gateway renderer remains until parity evidence passes.

## Risks

WebView2 runtime drift is recorded as environment evidence; no pixel-only or silent-baseline approach.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
