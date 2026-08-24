# Plan — TEST-002 Authorization and failure-path template

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Establish a reusable authorization and failure-path contract template for every api-v1 command.

## Steps

1. Inventory the existing api-v1 endpoint groups and their current auth/problem mapping.
2. Build a small parameterized template covering unauthenticated, wrong-role, invalid request, not-found/conflict and known provider failure responses.
3. Use endpoint fixtures that invoke the real Core use cases rather than recreate rules in test assertions.
4. Apply the template to one representative command from each group and document the extension rule.

## Verification

- Each selected command has authorization and problem-details assertions.
- No test encodes a duplicate business policy.
- Focused filtered contract tests pass with the detected runner syntax.

## Risks

Keep only one fixture taxonomy and preserve the gateway/Core boundary.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
