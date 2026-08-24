# Plan — TEST-016 End-to-end UAT scenarios

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Create the fourteen business UAT scripts, each mapped to local Test/UAT or the production pilot ring with pass/fail recording.

## Steps

1. Extract scenarios 1–14 from the programme plan and classify each as local-stack or pilot-ring proof.
2. Write concise operator scripts with precondition, action, expected result and evidence field.
3. Identify non-local proof explicitly rather than pretending local stack covers it.
4. Run a representative local scenario to validate the template.

## Verification

- All fourteen scenarios have ownership/environment/evidence fields.
- Pilot-only claims are clearly marked.
- No scenario relies on fabricated domain input.

## Risks

This is UAT evidence, not a second functional specification; FRDs remain behaviour authority.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
