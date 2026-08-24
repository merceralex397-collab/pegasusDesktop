# Plan — TEST-017 Test/UAT stack lifecycle

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Build TestStack lifecycle support in Invoke-LocalDevelopment.ps1 with doctor prerequisites, local feed and Publish-Feed.

## Steps

1. Read local-stack definition and existing DevelopmentOffline profile; do not add a new runtime profile.
2. Add bounded TestStack orchestration for local gateway/worker, Azurite, LocalDB/replay and development feed.
3. Add doctor checks with actionable prerequisites and safe local cleanup.
4. Exercise start, stop, feed publish and a representative test path.

## Verification

- TestStack uses DevelopmentOffline plus documented feature flags.
- Doctor reports missing prerequisite precisely.
- Lifecycle does not contact production resources or require Azure.

## Risks

LocalDB is Windows-only and scripts must avoid impacting mailbox/Box locations.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
