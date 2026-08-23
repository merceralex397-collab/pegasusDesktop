# Skill routing — work type to pinned skill, MCP tool, and subagent

This resolves proposal §20.4's routing labels to the exact skill names and
paths in the pinned upstream revisions recorded in
[`skills.lock.draft.json`](skills.lock.draft.json). Every ticket's `Routing`
block copies the relevant row. Load the project skill
(`.agents/skills/project/pegasus-desktop/SKILL.md`) before any row below.

Pinned sources (2026-08-23): `dotnet/skills` `98f84851` (2026-08-21),
`microsoft/win-dev-skills` `f1028dd5` (v0.5.0, 2026-07-22),
`microsoft/azure-skills` `1a03acfb` (2026-08-21). Paths are relative to each
repository; vendored copies live under `.agents/skills/vendor/<family>/<name>/`
once ticket DSK-12-02 lands (today the WinUI skills sit under
`.codex/skills/`).

## Work type routing (proposal §20.4 resolved)

| Work type | .NET skills (`dotnet/skills` path) | Windows skills (`win-dev-skills` path) | Azure skills | MCP tools | Subagent |
| --- | --- | --- | --- | --- | --- |
| Domain/application logic in `Pegasus.Core` | `run-tests`, `code-testing-agent`, `test-gap-analysis` (`plugins/dotnet-test/skills/...`) | none | none | Kanmer | `pegasus-gateway-dev` (server) or `winui-dev` (view-model side), tests by `pegasus-test-engineer` |
| Gateway endpoint / contract | `dotnet-webapi` (`plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`), `minimal-api-file-upload` | none | none | Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`), Kanmer | `pegasus-gateway-dev` |
| Persistence change / migration | `optimizing-ef-core-queries` (`plugins/dotnet-data/...`), `run-tests` | none | none | Microsoft Learn | `pegasus-gateway-dev`; grant check `scripts/Test-MigrationGrants.ps1` |
| WinUI page or control | `run-tests` (view-model tests) | `winui-dev-workflow`, `winui-design`, `winui-code-review` (`plugins/winui/skills/<name>/SKILL.md`) | none | Microsoft Learn (control APIs), Kanmer | `winui-dev`; review `pegasus-desktop-reviewer` |
| App lifecycle / windowing / single instance | none | `winui-dev-workflow`, `winui-design` | none | Microsoft Learn (`AppInstance`, `AppWindow`) | `winui-dev` |
| Packaging / update / signing / feed | `authoring-github-workflows` (`.agents/skills/authoring-github-workflows/SKILL.md`), `directory-build-organization`, `binlog-failure-analysis` | `winui-packaging` | `azure-storage` (read-only semantics), `azure-validate` (what-if only when a write is approved) | Microsoft Learn (App Installer schema, Artifact Signing), Azure MCP read-only `storage` | `pegasus-release-packager`; Azure facts from `pegasus-azure-auditor` |
| Authentication / token storage | `dotnet-webapi` (token endpoint), `run-tests` | `winui-design` (dialog/focus rules) | none | Microsoft Learn (`PasswordVault`, `ProtectedData`, OpenIddict docs site) | `pegasus-gateway-dev` (server), `winui-dev` (client) |
| Performance work | `analyzing-dotnet-performance`, `dotnet-trace-collect`, `dump-collect` (`plugins/dotnet-diag/...`) | `winui-code-review` (performance checklist) | none | none | `pegasus-ui-verifier` (measure), `winui-dev` (fix) |
| Accessibility review | none | `winui-ui-testing` (AutomationId audit), `winui-code-review` | none | none | `pegasus-ui-verifier`, `pegasus-desktop-reviewer` |
| CI / build change | `authoring-github-workflows`, `directory-build-organization`, `convert-to-cpm` (`plugins/dotnet-nuget/...`), `binlog-failure-analysis`, `setup-local-sdk` | `winui-packaging` (CI sample) | none | none | `pegasus-release-packager` |
| Test authoring / grading | `run-tests`, `code-testing-agent`, `scaffold-dotnet-test-project`, `test-gap-analysis`, `assertion-quality` | `winui-ui-testing` | none | Kanmer | `pegasus-test-engineer`, `pegasus-ui-verifier` |
| Azure inventory / cost / health (read-only) | none | none | `azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost`, `azure-diagnostics`, `azure-compliance` (`skills/<name>/SKILL.md`) | Azure MCP read tools (`group_resource_list`, `storage`, `keyvault`, `monitor`, `applicationinsights`, `sql`, `containerapps`, `functionapp`, `pricing`, `advisor`) | `pegasus-azure-auditor` |
| Observability / telemetry | `configuring-opentelemetry-dotnet` (reference only) | none | `appinsights-instrumentation` | Azure MCP read-only `applicationinsights`, `monitor` | `pegasus-gateway-dev`, `pegasus-azure-auditor` |
| Repository inventory / parity research | none | none | none | Kanmer (`get_item`, `search_items`) | `pegasus-parity-researcher` |
| Ticket pipeline | Kanmer skills `kanmer-tickets`, `kanmer-research`, `kanmer-plan`, `kanmer-execute`, `kanmer-review`, `kanmer-verify`, `kanmer-closeout`, `kanmer-docs`, `kanmer-setup` (`.grok/skills/<name>/SKILL.md`) | — | — | Kanmer MCP (`get_status`, `list_board`, `list_items`, `get_doc_gates`, `create_item`, `take_ticket`, `set_ticket_doc`, `move_item`, `link_doc`, `append_scratch`) | the owning agent of the ticket |
| Gateway release (Container App, Worker) | — | — | — | — | `pegasus-release-packager` using the existing `pegasus-release` skill (`.agents/skills/pegasus-release/SKILL.md`) |
| Documentation lookups | `microsoft-docs`, `microsoft-code-reference` (Microsoft Learn plugin) | — | — | `microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search` | any agent; verification by `pegasus-desktop-reviewer` |

## Routing index by area plan

| Area plan | Primary subagents | Skills loaded first | MCP |
| --- | --- | --- | --- |
| 00 Governance and workflow | `pegasus-parity-researcher`, `pegasus-desktop-reviewer` | `kanmer-setup`, `kanmer-tickets`, `kanmer-docs` | Kanmer |
| 01 Inventory and parity | `pegasus-parity-researcher`, `pegasus-azure-auditor` | `azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost`, `kanmer-research`, `test-gap-analysis` | Kanmer, Azure (read), Microsoft Learn |
| 02 Architecture and foundation | `winui-dev`, `pegasus-test-engineer`, `pegasus-desktop-reviewer`, `pegasus-release-packager` | `winui-setup`, `winui-dev-workflow`, `winui-design`, `directory-build-organization`, `convert-to-cpm`, `scaffold-dotnet-test-project` | Microsoft Learn, Kanmer |
| 03 Gateway API and data | `pegasus-gateway-dev`, `pegasus-test-engineer`, `pegasus-desktop-reviewer` | `dotnet-webapi`, `minimal-api-file-upload`, `optimizing-ef-core-queries`, `code-testing-agent`, `run-tests`, `microsoft-code-reference` | Microsoft Learn, Kanmer |
| 04 Auth, session, update, startup | `pegasus-gateway-dev`, `winui-dev`, `pegasus-test-engineer`, `pegasus-desktop-reviewer`, `pegasus-release-packager` | `dotnet-webapi`, `microsoft-code-reference`, `winui-dev-workflow`, `winui-packaging` | Microsoft Learn, Kanmer |
| 05 Implementation and migration | `winui-dev`, `pegasus-gateway-dev`, `pegasus-test-engineer`, `pegasus-desktop-reviewer` | `winui-dev-workflow`, `winui-design`, `winui-code-review`, `dotnet-webapi`, `code-testing-agent`, `run-tests`, `test-gap-analysis` | Microsoft Learn, Kanmer |
| 06 UI design | `winui-dev`, `pegasus-ui-verifier`, `pegasus-desktop-reviewer` | `winui-design` (+`winui-search.exe`), `winui-code-review`, `winui-ui-testing` | Microsoft Learn, Kanmer |
| 07 Integrations | `pegasus-gateway-dev`, `winui-dev`, `pegasus-desktop-reviewer`, `pegasus-azure-auditor` | `dotnet-webapi`, `microsoft-code-reference`, `winui-dev-workflow` | Microsoft Learn, Kanmer, Azure (read) |
| 08 Testing | `pegasus-test-engineer`, `pegasus-ui-verifier`, `pegasus-release-packager` | `run-tests`, `code-testing-agent`, `scaffold-dotnet-test-project`, `test-gap-analysis`, `assertion-quality`, `winui-ui-testing`, `analyzing-dotnet-performance`, `dotnet-trace-collect` | Microsoft Learn, Kanmer |
| 09 Release, update, distribution | `pegasus-release-packager`, `pegasus-azure-auditor`, `pegasus-test-engineer` | `winui-packaging`, `pegasus-release`, `authoring-github-workflows`, `directory-build-organization`, `binlog-failure-analysis` | Microsoft Learn, Azure (read), Kanmer |
| 10 Security, observability, performance | `pegasus-desktop-reviewer`, `pegasus-ui-verifier`, `pegasus-azure-auditor`, `pegasus-test-engineer` | `winui-code-review`, `analyzing-dotnet-performance`, `dotnet-trace-collect`, `dump-collect`, `azure-diagnostics`, `appinsights-instrumentation` | Microsoft Learn, Azure (read), Kanmer |
| 11 Azure disposition | `pegasus-azure-auditor`, `pegasus-release-packager` | `azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost`, `azure-compliance`, `azure-diagnostics` | Azure (read), Microsoft Learn, Kanmer |
| 12 Agent tooling | `pegasus-desktop-reviewer` | `create-custom-agent` (reference only), `kanmer-setup` | Kanmer |

## Not applicable to this conversion (do not load)

| Skill / agent | Reason |
| --- | --- |
| `entra-app-registration`, `entra-agent-id` | No Microsoft-account or Entra login; Pegasus credentials stay (proposal §8). |
| `azure-deploy`, `azure-prepare`, `azure-app-onboard`, `azure-app-onboard-prereq`, `azure-cloud-migrate`, `azure-enterprise-infra-planner`, `python-appservice-deploy` | The conversion adds no Azure deployment unit; the gateway is released by the existing `pegasus-release` procedure; Azure writes are approval-gated. |
| `azure-kubernetes`, `airunway-aks-setup`, `azure-aigateway`, `microsoft-foundry`, `azure-ai`, `azure-messaging`, `azure-kusto` | No AKS, API Management, Foundry, Service Bus, or Kusto in the estate; adding them fails the cloud-justification test. |
| `azure-upgrade`, `azure-reliability`, `azure-quotas` | Only if a later, approved Azure change needs them. |
| `dotnet-maui`, `dotnet-blazor`, `dotnet-template-engine`, `dotnet-test-migration`, `dotnet11`, `dotnet-ai`, `dotnet-advanced` plugins | Cross-platform UI and other scenarios excluded by proposal §2.2; xunit stays; .NET 10 LTS is the target. |
| `winui-wpf-migration` | No WPF source to migrate (the WinForms evaluator under `scripts/email-eval-desktop/` stays as is, ADR-0016); tables are reference only. |
| `winui-session-report` | User-invoked only; carries a privacy warning before any report is shared. |
| `dotnet-aot-compat` | Native AOT/trimming deferred until startup is profiled (proposal §7.1). |
| `configuring-opentelemetry-dotnet` | App Insights SDK remains the telemetry path; no collector fleet (proposal §18.2). |
| `create-custom-agent` | VS Code `.agent.md` format; Codex agents are TOML under `.codex/agents/`. |
