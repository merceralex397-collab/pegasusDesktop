# Files — TEST-007 UI critical-path scripts

| File or area | Intended work | Risk / reuse |
| --- | --- | --- |
| tests/Pegasus.Desktop.UITests/critical-paths/* | Implement the ticket-scoped test, script or configuration change. | Reuse existing fixtures, test conventions and local-stack evidence. |
| Shared UI harness and route/test data setup | Implement the ticket-scoped test, script or configuration change. | Reuse existing fixtures, test conventions and local-stack evidence. |
| AutomationIds from screen specs | Implement the ticket-scoped test, script or configuration change. | Reuse existing fixtures, test conventions and local-stack evidence. |

## Context files

Read docs/desktop/08-testing/README.md, docs/desktop/08-testing/test-uat-stack.md, docs/runbook.md test guidance, .github/workflows/ci.yml where relevant, and the ticket body. Load pegasus-desktop first, then run-tests and the routed test/UI/packaging skill.

## Out of scope

No Azure test resource, production deployment, provider credential, test-only business-rule implementation, or unrelated CI refactor.
