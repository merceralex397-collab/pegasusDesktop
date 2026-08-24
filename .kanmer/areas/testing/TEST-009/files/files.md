# Files — TEST-009 Accessibility lane

| File or area | Intended work | Risk / reuse |
| --- | --- | --- |
| tests/Pegasus.Desktop.UITests accessibility scripts | Implement the ticket-scoped test, script or configuration change. | Reuse existing fixtures, test conventions and local-stack evidence. |
| AxeWindowsCLI report capture | Implement the ticket-scoped test, script or configuration change. | Reuse existing fixtures, test conventions and local-stack evidence. |
| artifacts/a11y evidence convention | Implement the ticket-scoped test, script or configuration change. | Reuse existing fixtures, test conventions and local-stack evidence. |

## Context files

Read docs/desktop/08-testing/README.md, docs/desktop/08-testing/test-uat-stack.md, docs/runbook.md test guidance, .github/workflows/ci.yml where relevant, and the ticket body. Load pegasus-desktop first, then run-tests and the routed test/UI/packaging skill.

## Out of scope

No Azure test resource, production deployment, provider credential, test-only business-rule implementation, or unrelated CI refactor.
