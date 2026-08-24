# Checklist — TEST-002 Authorization and failure-path template

- [ ] Inventory the existing api-v1 endpoint groups and their current auth/problem mapping.
- [ ] Build a small parameterized template covering unauthenticated, wrong-role, invalid request, not-found/conflict and known provider failure responses.
- [ ] Use endpoint fixtures that invoke the real Core use cases rather than recreate rules in test assertions.
- [ ] Apply the template to one representative command from each group and document the extension rule.
- [ ] Verify: Each selected command has authorization and problem-details assertions.
- [ ] Verify: No test encodes a duplicate business policy.
- [ ] Verify: Focused filtered contract tests pass with the detected runner syntax.
- [ ] Record exact test command/output, simplification pass and independent review.
