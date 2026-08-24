# EPIC-003 — Area 02 · Architecture and foundation (Phase 1)

Read once before working any FND ticket labelled `plan-02`. It carries what every
ticket in this epic inherits; each ticket carries only its own work.

## What this epic delivers

The solution shape every later area builds inside: central package management and
lock files, a server-only build entry point so Linux workstations keep working, the
shared `src/Pegasus.Contracts` DTO project, the WinUI 3 client `src/Pegasus.Desktop`
and its `src/Pegasus.Desktop.Infrastructure`, the generic-host composition, the
shell and theme dictionaries, single-instance lifecycle, the crash path and
diagnostics bundle, the desktop test project, the architecture facts that enforce
the new boundaries, a development-signed MSIX, and the `desktop-build` CI lane.
Tickets DSK-02-01 … DSK-02-16 (board ids FND-026 … FND-041).

## Proposal coverage

§5.2 deployment units, §5.3 desktop layers, §5.4 solution structure; §7.1 runtime,
§7.2 composition, §7.3 single instance; §11.1 what may be cached locally; §16.3
crash recovery; §18.1 desktop diagnostics; §21.1 build properties; §24 Phase 1.
Out of scope here: endpoints and contracts behaviour (area 03), token flow and the
startup gate (area 04), visual token values (area 06), packaging channels, feed and
production signing (area 09).

## Decisions, assumptions and deviations binding every ticket

- L-01 the gateway is `Pegasus.Web` evolved in place — no new deployment unit.
- L-02 Test/UAT is a local production-mimicking stack; ADR-0014 stands. A ticket
  that asks for an Azure test resource is out of bounds.
- L-03 the only permitted WebView2 use is the isolated non-UI HTML→PDF renderer
  under ADR-0108, which does not exist yet — until it does, WebView2 is banned.
- L-04 every ticket names its subagent, skills and MCP tools.
- D-002 the production certificate subject equals the manifest `Publisher` exactly
  and is trusted per workstation in `LocalMachine\TrustedPeople`; the `.pfx` never
  becomes a GitHub secret. D-003 the feed is an in-house UNC share.
- C-01 the repositories become private, so `windows-latest` minutes bill at 2×.
- ADR block is ADR-0100…ADR-0110, never "next free number".
- **Deviation 1**: `Pegasus.Core` stays one project (Domain + Application), against
  proposal §5.4 — one Core owner of business policy (`AGENTS.md` § Product invariants).
- **Deviation 2 (additive)**: the server projects stay Linux-publishable through a
  server-only build entry point; Windows CI builds the full `Pegasus.slnx`.
- Assumptions to prove, never assume: A1 Windows App SDK 2.x compiles on SDK
  10.0.302; A3 `windows-latest` can restore the Windows SDK build tools and run
  `winapp` (DSK-02-15 answers it with a real run link).
- No Azure write arises anywhere in this epic.

## Exit gate and what proves it

The seven rows of `docs/desktop/02-architecture-and-foundation/README.md` § 4:
dev-signed MSIX launches on a clean Windows 11 machine; no WebView/web dependency
in the package; foundation tests pass; install/uninstall leaves only intended user
state; architecture facts go red on a forbidden reference; a second launch activates
the first window; the diagnostics bundle exports the documented manifest. DSK-02-16
collects the evidence and is reviewed by an agent that implemented none of it.

## Routing for this area

| Work | Subagent | Skills (pinned source) | MCP |
| --- | --- | --- | --- |
| Scaffold, host, shell, services, single instance, diagnostics | `winui-dev` | `winui-setup`, `winui-dev-workflow` (`BuildAndRun.ps1`), `winui-design` (`winui-search.exe`), `winui-code-review` — `.codex/skills/`, win-dev-skills v0.5.0 `f1028dd5` | Microsoft Learn `microsoft_docs_search`, `microsoft_code_sample_search` |
| Contracts project | `pegasus-gateway-dev` | `dotnet-webapi`, `microsoft-code-reference` — dotnet/skills `98f84851` | Microsoft Learn |
| CPM, build entry point, dev MSIX, CI lane | `pegasus-release-packager` | `convert-to-cpm`, `directory-build-organization`, `binlog-failure-analysis`, `authoring-github-workflows` (dotnet/skills `98f84851`), `winui-packaging` | Microsoft Learn |
| Tests and architecture facts | `pegasus-test-engineer` | `scaffold-dotnet-test-project`, `code-testing-agent`, `run-tests` — dotnet/skills `98f84851` | — |
| Review and exit gate | `pegasus-desktop-reviewer` (read-only) | `winui-code-review`, `winui-design` | Kanmer, Microsoft Learn |

Load `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) first, always.
Never load `winui-wpf-migration`, `winui-session-report`, `dotnet-aot-compat`,
`configuring-opentelemetry-dotnet`, or any `azure-deploy`/`azure-prepare` family skill
(`docs/desktop/12-agent-tooling/skill-routing.md` § Not applicable).
Kanmer pipeline: `get_doc_gates` before every move; one gated boundary per move.

## Traps (plan § 7)

XAML compiler silence below Windows App SDK 2.1.3 (`MSB3073` naming no `.xaml`);
`TreatWarningsAsErrors=true` + `AnalysisLevel=latest-recommended` apply to the new
projects — handle generated code, never relax the policy; a Windows TFM in
`Pegasus.slnx` breaks Linux restore; desktop lock files are RID/TFM specific, so CI
must restore with the matching RID; `BuildAndRun.ps1` skips its props injection when
the repo-root `Directory.Build.props` exists, so reference
`Microsoft.WindowsAppSDK.Analyzers` explicitly; package identity is permanent;
self-contained MSIX size must be measured; the shell is a `NavigationView`, not a
port of `_Layout.cshtml`; do not copy Core policy or `OperatorLabels` into the desktop.

## Read before starting any ticket in this epic

`docs/desktop/02-architecture-and-foundation/README.md` (whole file) ·
`docs/desktop/README.md` § Locked decisions and § Routing legend ·
`docs/desktop/00-governance-and-workflow/README.md` § 3 and § 7 ·
`docs/desktop/12-agent-tooling/skill-routing.md` ·
`AGENTS.md` §§ ADR conventions, New Markdown placement, Simplicity rails, Product
invariants, Repository task workflow · `docs/engineering.md` § Required evidence
tiers and § Simplicity · `docs/runbook.md` § Supported platform and § Locked
restore, build, and test · `docs/design/README.md` (any UI ticket) ·
`Directory.Build.props`, `Pegasus.slnx`, `global.json`,
`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`,
`.github/workflows/ci.yml`, `.github/actions/dotnet-build/action.yml`.
