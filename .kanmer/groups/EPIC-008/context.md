# EPIC-008 — Area 07 · Integrations (Graph, Box, DVLA/DVSA, mail, reports, OCR)

Read once before working any `DSK-07-*` ticket. It carries what binds every ticket in the
epic; the per-ticket detail is in the ticket body.

## What this epic delivers

The external-system seams of the desktop conversion, decided per integration: what the
desktop does locally, what the gateway (`Pegasus.Web` evolved in place) brokers, and what
stays in the unattended Worker. Nineteen tickets across three phases — Phase 5 intake and
communications (`DSK-07-01`…`04`, `11`, `19`), Phase 6 documents, Box and vehicle services
(`05`…`10`, `18`), Phase 7 reports (`12`…`17`).

## Proposal coverage

`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` §4.1 placement rows (Graph intake,
Box browsing, DVLA/DVSA lookup, report generation, scheduled work, file preview, OCR);
§12.1–12.6 in full; §13.4, §13.5, §13.7, §13.8, §13.9 (integration halves only — screens are
area 06, slices area 05); §16.2 external provider resilience; §24 Phases 5, 6, 7.

## Decisions that bind every ticket here

- **L-01** — the gateway is `Pegasus.Web` evolved in place. Every endpoint is an `/api/v1`
  route group behind `Features:DesktopGateway`. No new deployment unit.
- **L-02** — Test/UAT is the local production-mimicking stack (ADR-0014 stands). Asking for
  an Azure test resource is out of bounds.
- **L-03 / ADR-0108** — report rendering moves to an isolated, non-UI WebView2 HTML→PDF path;
  the gateway renderer stays registered until `DSK-07-15`'s golden-file parity passes.
- **ADR-0106** — Graph intake stays central: no desktop poller, no desktop Graph credential,
  no change-notification callback without a new accepted decision.
- **ADR-0107** — Box and DVLA/DVSA credentials stay behind the gateway. **A step that puts a
  provider secret, token, reusable URL or provider object id in the desktop package, a
  response body or a log is a defect** — refuse it and say so in the ticket.
- **C-01** — the repositories go private; private-repo Windows runner minutes bill at 2×, so
  reuse existing CI lanes rather than adding new ones.
- **Azure**: this area needs **no** write. Reads (Key Vault names, storage, App Insights) are
  free. A secret rotation or role change would be an exact-target write and is not planned
  here (`docs/runbook.md` § Live-operation approval matrix; mirror in plan 11).

## Deviations and open assumptions recorded in the plan

- Box direct transfer is **off** by default; bytes stream through the gateway until
  `DSK-07-07` proves a short-lived, file-scoped downscoped token and a follow-up ticket
  enables it.
- WebView2 chosen over native PDF layout because the Scriban templates, `report.css`, logo
  and signatures already exist and are governed under `docs/design/assets/report-renderer/`.
- ONNX VRM recognition stays server-side until `DSK-07-18` recommends otherwise; **no engine
  move without an accepted ADR**.
- DVLA/DVSA terms are assumed to forbid a direct native call; `DSK-07-10` records the check.

## Exit gate and what proves it

Phase 5 — intake arrives while every desktop is closed, duplicate and failure paths pass, no
desktop holds Graph credentials, full source-to-case traceability. Proof: Worker integration
tests (`MailboxIntakeIntegrationTests.cs`), gateway contract tests, package secret scan, UAT.
Phase 6 — large and failed transfers recover safely, provider secrets absent from the
package, provider rate/error handling passes, document parity approved. Proof: transfer-queue
tests with injected failures, MSIX secret scan, DVLA/DVSA replay tests, parity-matrix rows.
Phase 7 — approved fixtures match, no required report depends on the web renderer unless
explicitly retained, final document and audit correct, performance target met. Proof:
golden-file suite (`DSK-07-15`), report upload audit test, area 10 performance report.

## Routing for this area

| Work type | Subagent (`.codex/agents/<name>.toml`) | Skills (pinned source) | MCP |
| --- | --- | --- | --- |
| Gateway endpoints | `pegasus-gateway-dev` | `dotnet-webapi`, `minimal-api-file-upload` (dotnet/skills `98f84851`, plugin `dotnet-aspnetcore`); `microsoft-code-reference` (Learn plugin) | Microsoft Learn `microsoft_docs_search` / `microsoft_code_sample_search`; Kanmer |
| Desktop surfaces | `winui-dev`, verified by `pegasus-ui-verifier` | `winui-dev-workflow`, `winui-design`, `winui-ui-testing` (win-dev-skills v0.5.0 `f1028dd5`, today under `.codex/skills/`) | Microsoft Learn; Kanmer |
| WebView2 renderer | `winui-dev`, then `pegasus-desktop-reviewer` | `winui-dev-workflow`, `microsoft-code-reference`, `winui-code-review` (`WUI4xxx` interop rules) | Microsoft Learn `microsoft_docs_fetch` on the WebView2 print how-to and `CoreWebView2` reference |
| Tests and golden files | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `assertion-quality`, `test-gap-analysis` (dotnet/skills, plugin `dotnet-test`) | — |
| Secret / resource checks | `pegasus-azure-auditor` (read-only) | `azure-resource-lookup`, `azure-storage` (azure-skills `1a03acfb`) | Azure MCP read-only `keyvault` (names only), `storage`, `group_resource_list` |
| Carry-over and ADRs | `pegasus-parity-researcher`, `pegasus-desktop-reviewer` | `kanmer-tickets`, `kanmer-docs` (`.grok/skills/`) | Kanmer `create_item`, `link_doc` |

Load `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) before any row.
**Do not load** here: `azure-messaging` (no Service Bus/Event Hubs exist and
`docs/operations.md:87` forbids adding them), `entra-app-registration` (no Microsoft login),
`azure-ai` (no cloud AI introduced), plus everything in `skill-routing.md` § Not applicable.

## Traps (plan § 7) that apply across the epic

WebView2 off-screen hosting must be proven, not assumed; one print operation per WebView at a
time, so serialise renders; a missing WebView2 runtime needs a named failure and the gateway
fallback; golden files drift because WebView2 self-updates while Playwright is pinned —
compare with tolerances, never pixels, and never re-baseline silently; a second copy of the
Scriban/CSS set breaks the one-list rule; **custody retry is human-only**
(`docs/current-architecture.md:571`) — automating it is forbidden; poison counts and `unknown`
outcomes must never be collapsed into a friendly status; Graph credential drift is fixed by
the upstream sync (PLAT-039) before Phase 6 work starts; any new table needs a runtime-role
`Grant*` migration checked by `scripts/Test-MigrationGrants.ps1`; scope creep into
MAIL-12/13/17/19, EXT-xx and AI-xx is out of conversion scope (proposal §13.11).

## Read before starting any ticket in this epic

- `docs/desktop/07-integrations/README.md` (the area plan, all eight sections)
- `docs/desktop/README.md` § Locked decisions, § Routing legend
- `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR block, cloud-justification
  table), § Kanmer board shape, § 7 traps
- `docs/desktop/03-gateway-api-and-data/README.md` § 3 (conventions, problem-details
  catalogue) and `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
- `docs/desktop/06-ui-design/screen-specs.md` and `keyboard-and-accessibility.md` (UI tickets)
- `docs/desktop/12-agent-tooling/skill-routing.md` (exact names, pins, do-not-load table)
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` (before any report or
  Box work)
- `AGENTS.md` (task workflow, simplicity and safety rails), `docs/engineering.md`
  § Required evidence tiers, `docs/design/README.md` (operator copy authority)
