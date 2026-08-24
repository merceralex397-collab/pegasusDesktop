# Plan — TEST-008 Document, vehicle and report UI scripts

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Script document upload, vehicle lookup, report preview and report finalise on the native stack.

## Steps

1. Confirm the gateway/replay contract and allowed local fixtures for each provider-related path.
2. Implement UIA scripts for upload, vehicle request/accept, preview and finalise.
3. Assert explicit provider/failure/provenance states and custody confirmation.
4. Capture render/output evidence without fabricating domain documents.

## Verification

- Scripts run only against replay/local Test/UAT dependencies.
- Provider timeout/failure remains distinguishable from success.
- Report finalisation checks gateway custody/audit outcome.

## Risks

No live provider credentials, provider direct calls or fabricated corpus artifacts.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
