# Plan — TEST-001 API contract-test project

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Scaffold tests/Pegasus.Api.ContractTests with xUnit 2.9.3 and WebApplicationFactory so api-v1 contracts are exercised against the gateway composition.

## Steps

1. Inspect existing test platform, global.json and package-management conventions before adding the project.
2. Create the minimal xUnit/WebApplicationFactory project and add it to the solution.
3. Reuse gateway fixture/auth test helpers where they exist; do not reproduce Core business policy.
4. Run locked restore, Release build and the focused project test.

## Verification

- The project appears once in Pegasus.slnx and builds with warnings-as-errors.
- Focused API contract test command passes using the detected test runner.
- Fixture does not require live Azure, Box or Graph.

## Risks

The project is a contract boundary; keep request setup and expected results shared with endpoint tests rather than creating a second policy engine.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
