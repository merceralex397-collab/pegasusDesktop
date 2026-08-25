---
name: pegasus-desktop
description: "Routing entry point for every Pegasus native-desktop conversion task (WinUI 3 client, Pegasus.Web gateway endpoints, MSIX/App Installer release, tests, Azure disposition). Load it FIRST on any desktop-conversion ticket: it states the locked decisions that override upstream skill guidance, the dependency boundaries, the evidence format, and which pinned dotnet/win-dev/azure skill, MCP tool, and subagent to load next. Do not use for ordinary Pegasus web/worker work that has no desktop-conversion ticket."
---

# Pegasus desktop conversion — project skill

Planning set: `docs/desktop/README.md` (index, decisions, routing legend).
Skill routing: `docs/desktop/12-agent-tooling/skill-routing.md`.
Subagents: `docs/desktop/12-agent-tooling/subagents.md` and `.codex/agents/`.
Authority order for conflicts: `docs/operator-notes.md` > PRD > FRD >
`docs/capabilities.md` > ADR > current-state docs > working rules > this skill
> upstream skills. Upstream skill guidance that assumes a web app, Microsoft
account login, cross-platform runtime, public distribution, or enterprise
scale does not apply unless a Pegasus decision adopts it.

## Locked decisions (override upstream guidance)

- Native WinUI 3 (Windows App SDK 2.x stable) on Windows 11 x64 only; no
  WebView/WebView2 shell and no web UI hosted in the app. The single
  permitted WebView2 use is the isolated, never-visible HTML→PDF report
  renderer (ADR-0108).
- L-01 The cloud gateway is `Pegasus.Web` evolved in place: versioned
  `/api/v1` Minimal API route groups plus a staff token flow beside the Razor
  Pages, same Container App, no new deployment unit.
- Existing Pegasus credentials and roles; no Microsoft-account login; the
  desktop never holds a database, Graph, Box, DVLA/DVSA, or Azure secret.
- Online-required, not offline-first; no local replica database.
- Forced updates: signed MSIX via App Installer (2021 schema,
  `UpdateBlocksActivation`) plus a gateway minimum-client-version gate that
  fails closed.
- Modular monolith: one `Pegasus.Core` business-policy owner; no
  microservices, message bus, CQRS, event sourcing, SignalR, Redis, or APIM.
- L-02 Test/UAT is a local production-mimicking stack (Azurite, local
  gateway and Worker, replay adapters); ADR-0014 stands (local + production
  only); the production pilot ring is the only real-Azure validation.
- L-03 Report rendering moves to the desktop (WebView2 HTML→PDF over the
  shared Scriban templates); the gateway renderer stays until parity passes.
- L-04 Specialist Codex subagents exist as `.codex/agents/*.toml`; every ticket
  names its subagent, skills and MCP tools.
- L-05 The Kanmer board is seeded by the implementing agent from the ticket
  tables in these plans; the open upstream board is triaged in area 01.
- Azure: reads are free; every write is ⚠, conditional on exact-target
  approval or on D-002 (signing) / D-003 (feed hosting); nothing is
  deprovisioned before cutover, observed use, and rollback approval.
- Decided: D-001 (2026-08-23) — the fork is the single release source for
  gateway and desktop from the first production gateway change; upstream is
  merged in one final time, then frozen. D-003 (2026-08-23) — the update
  feed is an in-house UNC file share served over SMB (constraint C-01: the
  repositories become private on completion, so no GitHub-hosted anonymous
  feed can survive); no Azure resource hosts the feed, and update checks
  need the office network or VPN. D-002 (2026-08-23) — packages are signed
  with a **self-managed certificate** held in-house: the public `.cer` is
  trusted per workstation in `Cert:\LocalMachine\TrustedPeople` (never
  `Trusted Root`), the `.pfx` never leaves the signing host and is never a
  GitHub secret, the certificate subject equals the manifest `Publisher`
  exactly and is fixed before the first package, signatures are always
  timestamped, and trust reaches a machine before any package signed with
  it. **No decisions remain open**; the whole distribution path touches no
  Azure resource.

## Dependency boundaries

- `Pegasus.Desktop` and `Pegasus.Desktop.Infrastructure` reference
  `Pegasus.Core` and `Pegasus.Contracts` only; never
  `Pegasus.Infrastructure`, EF Core, Azure SDKs, Box, or Graph SDKs.
- `Pegasus.Web` and `Pegasus.Worker` translate transport; they carry no
  business policy (a second implementation is a stop condition).
- New tables need runtime-role GRANT migrations; new top-level projects need
  the accepted ADR (ADR-0100 block for the desktop conversion).

## UI and accessibility conventions

`docs/design/README.md` binds every screen: a field is a label and a
control; no how-it-works copy; only populated sections render; filters are
dropdowns, newest first; banned words; status vocabulary; no colour-only
state; tokens via `ThemeResource`; a unique `AutomationProperties.AutomationId`
on every interactive control; keyboard completion of every workflow; 200%
scale and high contrast verified.

## Invocation protocol (every ticket)

1. Read this skill, then the owning area plan under `docs/desktop/` and the
   Kanmer ticket folder (`get_doc_gates` before every move).
2. Load the exact upstream skills named in `skill-routing.md` from the pinned
   revision (`eng/skills/skills.lock.json`); never fetch a moving `main`.
3. Summarise only the applicable guidance; name any upstream guidance that a
   Pegasus decision overrides.
4. Implement the smallest vertical slice; keep `main` releasable (feature
   gates, expand/contract).
5. Run the skill-prescribed verification plus the repository profiles
   (`dotnet restore --locked-mode`, Release build, focused `dotnet test`).
6. Record the evidence below; an independent `pegasus-desktop-reviewer`
   reviews before merge.

## Evidence format (Appendix C)

Skills consulted (path, source repo, commit); applicable guidance; project
decisions taking precedence (ADRs, L-/D- ids); repository evidence
(`file:line`, data model, existing fixture); implementation (projects
changed, new dependencies, desktop/cloud placement); verification (commands,
unit, contract, UI, accessibility, performance, packaging/update); deviations
(none, or reason and approval).

## Next skill to load

WinUI: `winui-dev-workflow`, `winui-design`, then `winui-code-review`,
`winui-ui-testing`, `winui-packaging`. Gateway: `dotnet-webapi`,
`minimal-api-file-upload`, `optimizing-ef-core-queries`. Tests: `run-tests`,
`code-testing-agent`, `test-gap-analysis`. Build: `directory-build-organization`,
`convert-to-cpm`, `binlog-failure-analysis`. Azure (read-only):
`azure-resource-lookup`, `azure-cost`, `azure-diagnostics`. Release:
`pegasus-release`, `winui-packaging`. Docs: Microsoft Learn MCP
(`microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search`).
