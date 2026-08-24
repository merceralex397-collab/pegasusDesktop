---
id: TEST-017
type: ticket
title: >-
  DSK-08-17 · Build the Test/UAT stack lifecycle: `TestStack` mode in
  `Invoke-LocalDevelopment.ps1`, doctor prerequisites, local feed and
  `Publish-Feed`
status: backlog
area: testing
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-08
  - phase-2
  - tier-6
  - tier-12
  - needs-operator
groups:
  - EPIC-009
  - HZN-003
links: []
refs:
  - docs/adr/0014-local-to-production-deployment.md
docs_todo: true
archived: false
created: '2026-08-24T07:55:26.147Z'
updated: '2026-08-24T07:55:26.147Z'
---

## What

Extend `scripts/Invoke-LocalDevelopment.ps1` with a `TestStack` mode that brings up the whole local production-mimicking environment — gateway, Worker, Azurite, LocalDB, local update feed — with `Start`, `Status`, `Smoke`, `Reset`, `Stop` and a new `Publish-Feed` verb, and extend `scripts/Invoke-Doctor.ps1` with the desktop prerequisites.

## Why

Locked decision L-02 replaces the proposal's "Test/UAT with production-like Azure dependencies" with a local stack, and ADR-0014 (local development and production only) stands. Everything downstream in this epic assumes that stack exists: the UI suite, the packaging suite, the performance scripts and the fourteen UAT scenarios all name it as their environment. Without one script that brings it up on a clean Windows 11 machine, each of those tickets invents its own setup and the evidence stops being comparable between runs.

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-17`
- Plan detail: `docs/desktop/08-testing/test-uat-stack.md` — the whole document: § Components (the exact configuration per component), § "Why `DevelopmentOffline` and not a new profile", § "Machine prerequisites", § Lifecycle (the verb table), § "What the stack proves and what it does not", § Data, § "Known gaps"
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 21.3 Environments (the deviation this ticket implements)
- Repository evidence:
  - `scripts/Invoke-LocalDevelopment.ps1:1-30` — `[ValidateSet('Start','Status','Smoke','Stop','Reset')]$Action`, `-RunId`, `-FailureMode None|AfterWeb|StoragePressure`, `-StoragePressureMegabytes`, `-StartupTimeoutSeconds`; the artifact root `artifacts/local-development`, the Azurite program path `node_modules/azurite/dist/src/azurite.js`, the development storage account name and key
  - `scripts/Invoke-Doctor.ps1:1-12` — `[ValidateSet('Offline','Cloud')]$Profile`, the `Add-Check` helper with its `-Advisory` switch and its rule: never report Passed for something that is not true
  - `src/Pegasus.Worker/local.settings.example.json` — the exact Worker values to copy: `AzureWebJobsStorage=UseDevelopmentStorage=true`, `Runtime__Profile=DevelopmentOffline`, `ConnectionStrings__Pegasus=Server=(localdb)\MSSQLLocalDB;Database=PegasusDevelopment;...`, the five schedules, the approved inbox and sent settings
  - `src/Pegasus.Web/Program.cs:101-122` — `Runtime:Profile` accepts only `DevelopmentOffline` (Development environment) or `Production`; `Features:LocalIntake` requires `DevelopmentOffline`
  - `package.json` — Azurite 3.36.0 is the only devDependency
  - `scripts/Initialize-LocalDevelopment.ps1` — locked restore, Playwright Chromium, dev certificates, LocalDB
- Binding decisions:
  - L-02 — the stack is local and there is no Azure test environment; ADR-0014 is not superseded.
  - D-003 — the local feed is a file share or folder share, the same SMB mechanism as production, never an HTTP substitute.
  - D-002 — the development certificate lives in `Cert:\LocalMachine\TrustedPeople`, the same store the production certificate uses.
  - **Deviation (runtime profile)**: no `TestStack` runtime profile is added. `Runtime:Profile` keeps exactly two values; the stack runs under `DevelopmentOffline` with `Features:LocalIntake` and `Features:LocalDocumentCustody`.
- Depends on: `DSK-02-07` — channel-selected desktop configuration, so a package can be built for the `teststack` channel. `DSK-04-06` — the `/api/v1/client-compatibility` endpoint `Status` probes. `DSK-04-12` — the `.appinstaller` template and local feed this mode hosts.

## Routing

- **Subagent**: `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `run-tests` (`dotnet/skills` `98f84851`, plugin `dotnet-test`) → `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`) for the feed and certificate steps
- **MCP**: Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`) for the MIME types and `Content-Length` an App Installer feed host must serve; Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/test-uat-stack.md` in full and `scripts/Invoke-LocalDevelopment.ps1` in full — 1,583 lines that already own the process lifecycle, the run manifest, ownership timestamps and failure injection. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Load `pegasus-desktop`, then `run-tests`. Add a `-Mode` parameter to `Invoke-LocalDevelopment.ps1` with `[ValidateSet('Development','TestStack')]` defaulting to `Development`, so every existing invocation behaves exactly as before. Extend the mode, do not fork the script — a sibling script would duplicate the manifest, ownership and failure-injection logic.
3. Implement `Start -Mode TestStack`: start Azurite from `node_modules/azurite/dist/src/azurite.js`, ensure LocalDB and migrate with `dotnet run --project src/Pegasus.Web -- --migrate-development`, start the gateway with `Runtime:Profile=DevelopmentOffline`, `Features:LocalIntake=true`, `Features:LocalDocumentCustody=true`, `Features:DesktopGateway=true`, start the Worker under Functions Core Tools with `local.settings.json` copied from `src/Pegasus.Worker/local.settings.example.json`, start the local feed host, seed if the database is empty, and print the gateway URL and the feed `.appinstaller` link.
4. Implement `Status -Mode TestStack`: report `/health/live`, `/health/ready`, `GET /api/v1/client-compatibility`, the Azurite ports, which Worker functions are enabled, and that the feed is reachable **with the correct MIME types** — `application/appinstaller` and `application/msix` — and a `Content-Length`. Confirm the required MIME behaviour with `microsoft_docs_fetch` on <https://learn.microsoft.com/windows/msix/msix-troubleshooting-guide> and cite it in the script comment.
5. Implement `Smoke -Mode TestStack`: obtain a token from `/connect/token` with the seeded staff account, list cases, open one, and check the report-generation dependencies are present. Fail with a named repair line, following the `Get-RequiredApplication` pattern already in the script.
6. Implement `Reset -Mode TestStack`: drop and recreate the database, clear the Azurite data and the artifact root, reseed, and optionally uninstall the desktop package. It is destructive by design; require an explicit confirmation switch and say so in the help.
7. Implement `Stop -Mode TestStack`: stop every process the mode started, using the existing run-manifest ownership so it never kills a process another worktree owns.
8. Implement the new `Publish-Feed` verb: copy a freshly packaged `.msix` and its `.appinstaller` for the `teststack` channel into the feed folder, bumping the `.appinstaller` `Version` (it must increase on every publish, including a rollback publish) and leaving `Uri` equal to the served path. This verb is what [[DSK-08-10]] uses to simulate mandatory updates and rollbacks.
9. Extend `scripts/Invoke-Doctor.ps1 -Profile Offline` with the desktop prerequisites from `test-uat-stack.md` § "Machine prerequisites": PowerShell 7, .NET SDK 10.0.302, Node, Functions Core Tools v4, SQL Server Express LocalDB, the WebView2 Evergreen runtime, `winapp` CLI ≥ 0.3, `AxeWindowsCLI`, and a development certificate in `Cert:\LocalMachine\TrustedPeople`. Use `Add-Check` with a real `Repair` line for each, and `-Advisory` only where the requirement genuinely does not apply — never report Passed for something that is not true.
10. Build the seed dataset from `reference/` material and the existing integration builders under `tests/Pegasus.IntegrationTests/DocumentExtraction/`: plausible VRMs and references, irregular counts, Europe/London dates. Never `corpus/`, never operational email, never a real provider payload.
11. **Operator step**: walk the whole thing through on a clean, dedicated Windows 11 machine — run `Invoke-Doctor.ps1 -Profile Offline`, fix what it names, then `Start`, `Status`, `Smoke`, install the desktop package from the feed, `Publish-Feed` a newer version, take the update, then `Reset` and `Stop`. Hand back the transcript. That transcript is the ticket's proof.
12. Document the mode in `docs/runbook.md` beside the existing local-development section, and add the "what the stack does not prove" list from `test-uat-stack.md` so nobody reads a local pass as release evidence.
13. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] `-Mode TestStack` brings up gateway, Worker, Azurite, database and feed from one command, and existing `Development` behaviour is unchanged.
- [ ] `Status` reports every component including feed reachability with the correct MIME types and `Content-Length`.
- [ ] `Publish-Feed` bumps the `.appinstaller` version and leaves `Uri` equal to the served path.
- [ ] `Invoke-Doctor.ps1 -Profile Offline` reports every desktop prerequisite with a real repair line.
- [ ] No third `Runtime:Profile` value is introduced.
- [ ] A clean-machine walkthrough is recorded end to end.

## Verification

- [ ] `pwsh ./scripts/Invoke-Doctor.ps1 -Profile Offline` — expected: exit 0 on a prepared machine; on an unprepared one, each missing prerequisite named with its repair.
- [ ] `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start -Mode TestStack` then `-Action Status -Mode TestStack` — expected: every component healthy, the feed URL and `.appinstaller` link printed.
- [ ] `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Smoke -Mode TestStack` — expected: token obtained, cases listed, one case opened, exit 0.
- [ ] `pwsh ./scripts/Test-PegasusPlatform.ps1` — expected: exit 0 (the LocalDB lifecycle classifier tests still pass; this is a CI job).
- [ ] `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start` with no `-Mode` — expected: identical behaviour to before this change.

## Evidence tier

Tiers 6 and 12 — Functions/Azurite caller, and Integrated workflow. It obliges the actual timer and queue triggers against Azurite, real Blob staging and retry behaviour, and an end-to-end path from source receipt through Core, SQL and the Worker to a persisted operator view; Flex Consumption scaling and Key Vault references stay out of reach under L-02.

## Documentation changes

- `docs/runbook.md` — the `TestStack` mode, its verbs, the prerequisites and the Windows-only note.
- `docs/operations.md` — the stack as the UAT surface, and what it does not prove.
- `docs/desktop/08-testing/test-uat-stack.md` — mark the lifecycle as implemented and correct anything the implementation had to change.
- `docs/capabilities.md` — a `DSK` row for "Test/UAT stack" with the canonical owner named.

## Guardrails

- **Azure**: no write, and no Azure resource of any kind. Asking for an Azure test resource is out of bounds under L-02 and ADR-0014.
- **Scope boundary**: may edit `scripts/Invoke-LocalDevelopment.ps1`, `scripts/Invoke-Doctor.ps1`, the seed fixtures and the documentation named above. Must not add a `TestStack` value to `Runtime:Profile`, must not create a sibling lifecycle script, and must not change the composition root in `src/Pegasus.Web/Program.cs`.
- **Traps**: the existing `Development` mode must behave identically after this change — it is used by every developer and by CI-adjacent scripts. `Invoke-Doctor.ps1` must never report Passed for something untrue; use `-Advisory` honestly. The local feed proves App Installer mechanics but not the production host's configuration or the production certificate — record that gap rather than hiding it. Never fabricate domain data; `corpus/` is never copied. `Reset` is destructive and must be explicit.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
