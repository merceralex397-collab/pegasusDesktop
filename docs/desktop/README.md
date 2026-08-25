# Pegasus native desktop conversion — plan set

This folder turns the
[Pegasus Native Windows Desktop Conversion proposal](Pegasus_Native_Desktop_Design_Proposal.md)
into area plans that an implementing agent can execute one folder at a time.
The proposal is the design target; these plans are the evidence-backed,
ticket-sized route to it. They are **programme planning**, not authority:
a durable technical decision still becomes an ADR under `docs/adr/`,
required behaviour an FRD under `docs/frd/`, and scope a PRD — see
[`AGENTS.md` § New Markdown placement](../../AGENTS.md#new-markdown-placement).

Planning baseline: fork `merceralex397-collab/pegasusDesktop`, branch `main`
at `191ddf33`, 2026-08-23. Upstream `collisionengineers/pegasus` `main` was
32 commits ahead at `7d6a948a` on that date
(see [01 · upstream carry-over](01-inventory-and-parity/upstream-kanmer-carryover.md)).

## Reading order

| # | Area | Folder | Read when |
| --- | --- | --- | --- |
| 00 | Governance, branching, Kanmer, ADR/FRD list | [00-governance-and-workflow](00-governance-and-workflow/README.md) | Before any ticket; it defines the rules the others assume |
| 01 | Phase 0 inventory: parity matrix, flow records, Azure register, upstream board carry-over | [01-inventory-and-parity](01-inventory-and-parity/README.md) | First work package |
| 02 | Phase 1 solution foundation: projects, build props, shell, single instance, diagnostics | [02-architecture-and-foundation](02-architecture-and-foundation/README.md) | Second work package |
| 03 | Gateway API evolved inside `Pegasus.Web`: endpoints, OpenAPI, generated client, concurrency, audit | [03-gateway-api-and-data](03-gateway-api-and-data/README.md) | With 04 and every slice |
| 04 | Phase 2 auth/session, compatibility gate, forced update, startup and first run | [04-auth-session-update-and-startup](04-auth-session-update-and-startup/README.md) | Before the first slice ships |
| 05 | Reuse/extract/cut map and the vertical slices (Phases 3–8) | [05-implementation-and-migration](05-implementation-and-migration/README.md) | Every feature ticket |
| 06 | UI design: tokens, shell, screen specs, keyboard, accessibility | [06-ui-design](06-ui-design/README.md) | Every UI ticket |
| 07 | Integrations: Graph, Box, DVLA/DVSA, mail, reports (WebView2), OCR | [07-integrations](07-integrations/README.md) | Phases 5–7 |
| 08 | Testing strategy, Test/UAT local stack, CI lanes | [08-testing](08-testing/README.md) | Every ticket's verification section |
| 09 | Release, update, distribution, signing/hosting decision matrix, runbooks, first install | [09-release-update-and-distribution](09-release-update-and-distribution/README.md) | Phase 2 onward; every release |
| 10 | Security, observability, performance | [10-security-observability-performance](10-security-observability-performance/README.md) | Phase 8 hardening and every release candidate |
| 11 | Azure disposition: register, conditional writes, deprovision checklist | [11-azure-disposition](11-azure-disposition/README.md) | Any Azure touch |
| 12 | Agent tooling: skills, lockfile, subagents, MCP, invocation protocol | [12-agent-tooling](12-agent-tooling/README.md) | Before the first agent session |

## Locked decisions and open decisions

Decisions the operator confirmed on 2026-08-23 while this plan set was written.
They bind the plans until a recorded decision changes them.

| ID | Decision | Status | Owner plan |
| --- | --- | --- | --- |
| L-01 | Gateway is `Pegasus.Web` evolved in place (versioned `/api/v1` route groups and a staff token flow beside Razor Pages, same Container App); no new deployment unit | Locked | 03 |
| L-02 | Test/UAT is a local production-mimicking stack (local gateway and Worker processes, Azurite, LocalDB/SQL container, replay adapters); no Azure test environment; ADR-0014 stands; production pilot ring for real-Azure validation | Locked | 08, 09 |
| L-03 | Report rendering moves to the desktop through an isolated, non-UI WebView2 HTML→PDF path; the gateway renderer is retained only until golden-file parity passes; needs ADR-0108 (reserved desktop ADR block ADR-0100…0110, see 00) | Locked | 07 |
| L-04 | Specialist Codex subagents exist as `.codex/agents/*.toml`; every ticket names its subagent, skills, and MCP tools | Locked | 12 |
| L-05 | Kanmer board is seeded by the implementing agent from the ticket tables in these plans; the open upstream board is triaged in 01 | Locked | 00, 01 |
| D-001 | Release source of truth after Phase 2 | Recorded in ADR-0100 § Consequences and `docs/operations.md` § Release source of truth; the freeze date and mechanism remain pending agreement with the upstream owners, and sync continues until recorded | 00 |
| D-002 | Production code signing | **Decided 2026-08-23: option C — a self-managed certificate**, kept in-house and trusted per workstation (`LocalMachine\TrustedPeople`). With D-003 this makes the whole distribution path free of Azure resources and recurring cost; the price is a trust rollout and a rehearsed renewal (runbooks R5, R7) | 09 |
| D-002 · consequence | The distribution decisions are recorded. The desktop distribution path (sign in-house → copy to the UNC share → App Installer over SMB) touches no Azure resource at all | Recorded | 09, 11 |
| D-003 | Update-feed hosting | **Decided 2026-08-23: UNC file share** on an always-on in-house Windows host, served to App Installer over SMB. Driven by constraint C-01 below; costs nothing and needs **no Azure write** | 09 |

### Constraints recorded after planning began

| ID | Constraint | Consequences |
| --- | --- | --- |
| C-01 (2026-08-23) | **The repositories become private once the conversion is complete.** They are public today only because GitHub gives free CI minutes to public repositories. | (a) The update feed must not depend on anonymous HTTPS from GitHub — GitHub Releases and GitHub Pages are ruled out permanently, which decided D-003 in favour of a UNC share (SMB carries Windows authentication, so a private feed works). (b) GitHub Actions minutes stop being free: private-repository Windows runners bill at a 2× multiplier against a monthly allowance, and this repository runs most jobs on `windows-latest` with desktop packaging and UI lanes still to be added — see [08 · CI cost](08-testing/README.md#7-risks-and-traps) and ticket DSK-08-19. |

Azure rule throughout: reads are free; every write is marked ⚠ in the plans,
is conditional on a decision above or on exact-target approval
([runbook approval matrix](../runbook.md#live-operation-approval-matrix)), and
is mirrored in [11-azure-disposition](11-azure-disposition/README.md).
Nothing is deprovisioned before cutover, observed use, and rollback approval.

## Area plan template

Every `README.md` in this folder set uses the same eight sections so a ticket
can be cut from any of them without reading the whole set.

1. **Purpose and proposal coverage** — what the area delivers and which
   proposal sections (§) it implements.
2. **Evidence base** — repository paths with line references, official
   documentation with fetch date, and a *facts vs. assumptions* split
   ([engineering § plan sizing](../engineering.md#plan-sizing)).
3. **Decisions and assumptions** — locked/open decisions it depends on,
   explicit deviations from the proposal, ⚠ Azure writes.
4. **Target state and exit gate** — what "done" looks like and what proves it.
5. **Work breakdown** — ticket-sized rows: ID · profile · dependencies ·
   acceptance · verification · evidence tier
   ([engineering § evidence tiers](../engineering.md#required-evidence-tiers))
   · **routing** (subagent · skills · MCP). Mirrors proposal §25.
6. **Routing table** — the exact skills, MCP tools, and subagents for the
   area, with the pinned source of each skill.
7. **Risks and traps** — including the repository's recorded traps.
8. **Documentation changes** — ADR/FRD/PRD, `docs/capabilities.md`,
   `docs/operations.md`, `docs/current-architecture.md`, `docs/index.md`.

Ticket IDs in these plans are planning handles of the form `DSK-<area>-<nn>`
(for example `DSK-02-03`). When a ticket is created on the Kanmer board it
receives the board's own ID from its area prefix; the plan handle goes into
the ticket's `refs`/body so the two stay joined.

## Routing legend

Agents, skills, and MCP servers referenced by the plans. Exact names matter:
the implementing agent loads a skill by the name below, from the pinned
revision recorded in [12 · skill routing](12-agent-tooling/skill-routing.md).

| Kind | Names used in the plans | Source |
| --- | --- | --- |
| Codex subagents | `winui-dev` (existing), `pegasus-gateway-dev`, `pegasus-parity-researcher`, `pegasus-test-engineer`, `pegasus-desktop-reviewer`, `pegasus-release-packager`, `pegasus-azure-auditor`, `pegasus-ui-verifier` | `.codex/agents/*.toml`; specs in [12 · subagents](12-agent-tooling/subagents.md) |
| WinUI skills | `winui-setup`, `winui-dev-workflow`, `winui-design`, `winui-code-review`, `winui-ui-testing`, `winui-packaging`, `winui-wpf-migration`, `winui-session-report` | `microsoft/win-dev-skills` v0.5.0 (`f1028dd5`), vendored under `.codex/skills/` today |
| .NET skills | `dotnet-webapi`, `minimal-api-file-upload`, `configuring-opentelemetry-dotnet`, `optimizing-ef-core-queries`, `run-tests`, `code-testing-agent`, `scaffold-dotnet-test-project`, `test-gap-analysis`, `assertion-quality`, `analyzing-dotnet-performance`, `dotnet-trace-collect`, `dump-collect`, `directory-build-organization`, `convert-to-cpm`, `binlog-failure-analysis`, `dotnet-aot-compat`, `setup-local-sdk`, `authoring-github-workflows` | `dotnet/skills` `98f84851` (plugins `dotnet-aspnetcore`, `dotnet-data`, `dotnet-test`, `dotnet-diag`, `dotnet-msbuild`, `dotnet-nuget`, `dotnet-upgrade`, `dotnet`, and `.agents/skills/`) |
| Azure skills | `azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost`, `azure-diagnostics`, `azure-compliance`, `azure-validate`, `azure-storage`, `appinsights-instrumentation` | `microsoft/azure-skills` `1a03acfb` |
| Microsoft Docs skills | `microsoft-docs`, `microsoft-code-reference` | Microsoft Learn plugin |
| Kanmer skills | `kanmer-setup`, `kanmer-tickets`, `kanmer-research`, `kanmer-plan`, `kanmer-execute`, `kanmer-review`, `kanmer-verify`, `kanmer-closeout`, `kanmer-docs`, `kanmer-groom`, `kanmer-report`, `kanmer-auto` | `.grok/skills/` (Kanmer 0.1.0) |
| Project skill | `pegasus-desktop` (routing entry point) and `pegasus-release` (existing gateway release) | `.agents/skills/project/pegasus-desktop/SKILL.md`, `.agents/skills/pegasus-release/SKILL.md` |
| MCP servers | Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search`); Kanmer (`get_status`, `list_board`, `list_items`, `get_doc_gates`, `create_item`, `take_ticket`, `set_ticket_doc`, `move_item`, …); Azure MCP (read-only tools such as `group_resource_list`, `storage`, `keyvault`, `monitor`, `applicationinsights`, `sql`, `containerapps`, `functionapp`, `pricing`) | `.codex/config.toml`, `.mcp.json`, azure-skills MCP |

## Proposal coverage

Every proposal section is owned by at least one area.

| Proposal section | Owning area(s) |
| --- | --- |
| §1 Executive decision, §2 Authority and scope, §3 Reconciliation, §6 Repository strategy | 00 |
| §4 Cloud-justification test and placement decisions | 00 (test), 11 (register), 03/07 (per capability) |
| §5 Target system architecture, §5.4 solution structure | 02, 03 |
| §7 Desktop technology baseline, §7.3 single instance | 02 |
| §8 Authentication and authorization, §9 Forced updates and compatibility | 04 (and 09 for the package side) |
| §10 API and data architecture | 03 |
| §11 Local state and offline behaviour | 02 (cache/diagnostics), 04 (connectivity), 05 (drafts) |
| §12 Integration design | 07 |
| §13 Current and desired functionality | 01 (inventory), 05 (slices) |
| §14 Native WinUI 3 experience | 06 |
| §15 Performance design, §16 Reliability, §17 Security, §18 Observability | 10 (with 02 for diagnostics bundle) |
| §19 Azure service disposition | 11 |
| §20 Integrating the skill repositories | 12 |
| §21 Build, CI and release | 09 (and 08 for CI test lanes) |
| §22 Testing strategy, §23 Verification and parity | 08, 01 |
| §24 Implementation sequence | 00 (phase map), 01–09 (phase content) |
| §25 Ticket structure, §26 Documentation set | 00 |
| §27 Acceptance criteria, §28 Optimality, §29 Next actions | 00 |
| Appendix A ADR template | 00 |
| Appendix B Cloud dependency record | 11 |
| Appendix C Agent implementation evidence | 12 |
| Appendix D Research basis | this index (baseline) and 12 (pins) |

## Status

| Area | Plan state | Notes |
| --- | --- | --- |
| 00–12 | Drafted 2026-08-23 | Awaiting first ticket creation on the fork's Kanmer board (see 00) |
