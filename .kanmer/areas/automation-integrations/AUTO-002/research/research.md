# Research — AUTO-008: durable intake latency

## Question

Where can latency enter the current durable intake path, what is already observable, and what must be measured before changing the architecture?

## Findings

- Submission first retains staging bytes and writes a Pending work item through `ReceiveIntake`; successful Web upload returns after durability, before processing (`src/Pegasus.Core/Intake/DurableIntake.cs`, `src/Pegasus.Web/Pages/Upload.cshtml.cs`).
- Pending work is not enqueued inline. `PendingWorkDispatchFunction` runs on `PendingWorkDispatchSchedule`; the checked-in local example is `*/15 * * * * *`, so schedule alignment can contribute 0–15 seconds before queue delivery (`src/Pegasus.Worker/IntakeFunctions.cs`, `src/Pegasus.Worker/local.settings.example.json`).
- Queue delivery then calls `ProcessQueuedIntake`, which reads and hashes staging bytes, stores durable bytes, parses/extracts/classifies, persists receipt/evaluation, deletes staging, associates or allocates a Case, performs image automation, and synchronizes Unidentified work. These sequential boundaries need separate timing before any is blamed (`src/Pegasus.Core/Intake/DurableIntake.cs`).
- `ProcessIntake` already emits an Activity tag `intake.duration_ms`, but it measures only the inner retained-intake evaluation, not receipt-to-dispatch wait or the post-evaluation allocation/automation tail (`src/Pegasus.Core/Intake/ProcessIntake.cs`).
- Retry delays are deliberately 30 seconds, 2 minutes, 10 minutes, 30 minutes, and 2 hours for transient failure; retry latency must not be mixed with healthy-path performance.
- Existing integration tests can invoke `ProcessQueuedIntake` immediately and therefore do not represent timer/queue latency. No checked-in benchmark or percentile evidence was found.
- No older TypeScript intake runtime exists in the tracked repository. The only TypeScript files are design-system assets, so a performance comparison needs an approved predecessor source rather than recollection.
- The operator estimates ordinary processing under five seconds. That is a hypothesis until representative median/p95/worst-case observations separate queue wait and compute time.

## Implications

Do not redesign the durable boundary or expose processing states from static inspection. Instrument timestamps for durable acceptance, dispatch claim/enqueue, processing claim, evaluation completion, and terminal post-processing; then run representative local/integration fixtures repeatedly. Report healthy path separately from retries. If dispatch wait dominates, prefer the smallest safe dispatch improvement that preserves the SQL outbox and recovery semantics; any code change becomes a new implementation ticket.

## Open questions

The measurement method is resolved. Production or predecessor comparison needs separately available evidence.

## Azure architecture refresh — 2026-08-21

### Verified live facts
- The live Worker setting `PendingWorkDispatchSchedule` is `*/15 * * * * *`; this is not merely a checked-in local default. It can contribute 0–15 seconds before pending SQL outbox work is enqueued.
- The current path already has Azure SQL outbox state, Azure Queue Storage, a queue-triggered Azure Function, Application Insights, and Log Analytics.
- Microsoft documents Queue Storage triggers as using exponential idle polling, with a configurable `maxPollingInterval`; queue-trigger concurrency is batch-based and scales with the Function host: https://learn.microsoft.com/azure/azure-functions/functions-bindings-storage-queue-trigger

### Design consequence
First measure receipt-to-dispatch, dispatch-to-queue-trigger, processing, and allocation separately using existing timestamps and Application Insights. If healthy latency is dominated by the 15-second timer, compare the smallest safe options in order: shorten the existing schedule; improve the existing dispatcher wake-up while retaining SQL reconciliation; only then consider a different messaging service. Do not add Service Bus, Event Grid, another Function, or bypass the SQL outbox without measured evidence. Queue trigger polling may add its own variable delay and must be measured rather than attributed to processing code.

## 2026-08-25 live prerequisite and local-stack evidence

The local prerequisite path was repaired without changing repository source, tracked configuration, or cloud state.

- \`pwsh ./scripts/Invoke-Doctor.ps1 -Profile Offline\` initially failed only because SDK 10.0.302, Azurite, and the generated Playwright launcher were absent. \`dotnet restore tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj\`/Debug build and pinned Chromium installation repaired the latter two prerequisites. \`winget install --exact --id Microsoft.DotNet.SDK.10 --version 10.0.302 --scope user ...\` returned \`No applicable installer found\`; the official user-scoped \`dotnet-install.ps1 -Version 10.0.302 -InstallDir C:\Users\PC\AppData\Local\dotnet-sdk-10.0.302 -NoPath\` succeeded. Rerunning the doctor with that SDK first on PATH passed all checks: \`Pegasus Doctor Offline passed. This result grants no external-operation approval.\`

- \`pwsh ./scripts/Initialize-LocalDevelopment.ps1\` succeeded on retry with the task SDK first on PATH and \`MSBUILDDISABLENODEREUSE=1\`. Evidence: restore/build succeeded; LocalDB instance \`MSSQLLocalDB\` was started; the final doctor passed; the run was initialized at \`artifacts/local-development\`.

- \`pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start\` did not reach Web readiness. The owned run manifest \`artifacts/local-development/4fcffe7639144a1993a52542c9509d67/run-manifest.json\` records \`state: Failed\`, \`failure.code: START_FAILED\`, and \`Exception calling "GetFullPath" with "1" argument(s): "The path is empty. (Parameter 'path')"\` at \`scripts/Invoke-LocalDevelopment.ps1:1482\`. The preceding initialization log proves the database/identity initialization completed; the failure occurs in launcher bookkeeping before a web process is recorded. The manifest has no web/worker process, and no measurement was fabricated from this failed run.

- The run-owned \`artifacts/local-development\` output is ignored local evidence; \`git status --short --branch\` remained clean.

Deployed schedule read-only evidence: \`az functionapp config appsettings list --name <worker-app> --resource-group rg-pegasus-prod\` returned the safe \`Intake__Schedule\` value \`*/5 * * * * *\`, matching tracked \`src/Pegasus.Worker/local.settings.example.json\` line 7. No app setting write was attempted. The local runtime start defect is a blocker for the required real acceptance-to-dispatch timing sample; next action is a repository-owned fix or an approved workaround for the launcher path handling, followed by a fresh owned local run and real healthy/retry measurements.

## 2026-08-25 queue-trigger interpretation

Primary Microsoft Learn evidence was fetched from \`https://learn.microsoft.com/azure/azure-functions/functions-bindings-storage-queue-trigger#polling-algorithm\`. It states that the queue trigger uses random exponential backoff: approximately 100 ms after a message is found, about 200 ms after an empty poll, increasing to \`maxPollingInterval\`; local development defaults to 2 seconds. It also states queue-trigger retry visibility is controlled by \`visibilityTimeout\`, and messages are retried up to five times before the poison queue by default. This is a runtime interpretation aid, not a substitute for live measurements.

Tracked \`src/Pegasus.Worker/host.json\` sets \`batchSize=4\`, \`newBatchThreshold=2\`, \`visibilityTimeout=00:05:00\`, \`maxDequeueCount=5\`, \`messageEncoding=none\`, and \`maxPollingInterval=00:00:02\`. Worker code identifies the durable intake queue as \`intake-work\` and the external queue as \`external-work\`. These values are recorded for interpreting the eventual sample; they do not establish latency without a running local queue-triggered path.
