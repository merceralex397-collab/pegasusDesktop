# EPIC-011 — Area 10 · Security, observability and performance

Cross-cutting hardening for the native desktop conversion: the threat model and the controls
that answer it, how the system is observed and supported, and the performance budgets with the
measurement method and release regression report. Tickets `DSK-10-01`…`DSK-10-18` (board ids
`PLAT-001`…`PLAT-018`). Board note: plan 00 § Kanmer board shape assigns **no** board area to
plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`) alongside plan 11.

## Proposal coverage

§15 Performance design (§15.1 budgets adopted verbatim, §15.2 practices → review checklist,
§15.3 profiling and the release regression report) · §16 Reliability and error handling
(§16.1 operation model, §16.2 provider resilience, §16.3 crash recovery) · §17 Security and
privacy (§17.1 controls, §17.2 explicit non-goals, §17.3 threat focus) · §18 Observability and
support (§18.1 desktop diagnostics, §18.2 central telemetry, §18.3 health) · §22.2 Security and
Performance test lists · §11.1 local cache list and §11.3 connectivity (security side) ·
§24 Phase 8 hardening gate. Packaging and signing controls are plan 09; token storage and the
compatibility gate are plan 04.

## What binds every ticket in this epic

- **ADR-0109** (to be authored): desktop diagnostics bundle **plus the existing Application
  Insights**. No new telemetry fleet, no collector. `configuring-opentelemetry-dotnet` is on
  the do-not-load table — the estate keeps the App Insights SDK and switching exporters is out
  of scope. This is a recorded deviation from a generic reading of §18.2, not an oversight.
- **ADR-0102 / ADR-0103 / ADR-0104 / ADR-0105** (to be authored): access token in memory,
  refresh token DPAPI-protected, gateway not direct database access, bounded local cache only,
  signed MSIX with a minimum-version gate. Every ticket here sets `docs_todo: true` because
  those ADRs do not exist yet; that is the honest leave-backlog answer.
- **L-01** the gateway is `Pegasus.Web` evolved in place · **L-02** Test/UAT is a **local**
  production-mimicking stack (ADR-0014 stands — asking for an Azure test resource is out of
  bounds) · **L-04** every ticket names its subagent, skills and MCP tools.
- **D-002** production signing is a self-managed certificate; the private key is a first-class
  asset of this plan (restricted ACL on the signing host, never a GitHub secret, loss or
  compromise is an incident with runbook R5). **D-003** the update feed is an in-house UNC share
  over SMB — feed and manifest controls are SMB ACLs and signature validation, never public
  HTTPS. **C-01** the repositories become private and private Windows runner minutes bill at 2×,
  so no ticket here may add a new Windows CI job; steps go inside jobs that already exist.
- **Azure**: reads are free; the only ⚠ writes in this area are the Log Analytics daily-cap
  change and the optional third alert rule, both owned by `DSK-10-16`, both conditional on
  exact-target approval (`docs/runbook.md` § Live-operation approval matrix) and mirrored in
  `docs/desktop/11-azure-disposition/README.md` § conditional writes. Nothing is deprovisioned.
- **Assumption on record**: the lowest-spec office workstation is not yet known — `DSK-10-10`
  records it, and until it does **no budget is pass/fail**. No desktop App Insights SDK is
  assumed; server telemetry plus on-demand bundles, revisited only on pilot evidence.

## Exit gate and what proves it

Proposal §24 Phase 8: full automated suite passes; accessibility critical issues resolved
(plans 06/08); security review has no unresolved high-risk item; a production-like package is
tested on the local stack and the pilot ring; a performance regression report is attached to
the release. Proof for this epic: the threat register with every row carrying a control and a
test; the secret scan and dependency gate failing on planted material; a diagnostics bundle
that reproduces a real failure with no secrets in it; and a regression report whose ten §15.1
budget rows each carry a measured number on the recorded baseline machine.

## Routing for this area

| Work type | Subagent (`.codex/agents/<name>.toml`) | Skills (exact name · pinned source) | MCP |
| --- | --- | --- | --- |
| Security review, checklists, register | `pegasus-desktop-reviewer` (read-only) | `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-code-review` (`.codex/skills/`, win-dev-skills v0.5.0 `f1028dd5`) | Kanmer; Microsoft Learn |
| Security and authorization tests | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `test-gap-analysis`, `assertion-quality` (dotnet/skills `98f84851`, plugin `dotnet-test`) | Kanmer |
| Package/CI controls (secret scan, SBOM, vulnerabilities) | `pegasus-release-packager` | `winui-packaging` (win-dev-skills `f1028dd5`), `authoring-github-workflows`, `directory-build-organization` (dotnet/skills `98f84851`) | Microsoft Learn |
| Desktop diagnostics, file hygiene, reliability model | `winui-dev` | `winui-dev-workflow`, `winui-code-review`, `winui-design` (win-dev-skills `f1028dd5`) | Microsoft Learn (`ProtectedData`, file ACL APIs, `AppInstance`) |
| Baseline, profiling, regression report | `pegasus-ui-verifier` | `analyzing-dotnet-performance`, `dotnet-trace-collect`, `dump-collect` (dotnet/skills `98f84851`, plugin `dotnet-diag`); `winui-ui-testing` for scripted runs | — |
| Telemetry dimensions and health surface | `pegasus-gateway-dev` | `dotnet-webapi` (dotnet/skills `98f84851`), `appinsights-instrumentation` (azure-skills `1a03acfb`) | Microsoft Learn |
| Telemetry verification, quota and alert decisions | `pegasus-azure-auditor` | `azure-diagnostics`, `azure-cost`, `azure-validate` (what-if only, and only when a write is approved) — azure-skills `1a03acfb` | Azure MCP **read-only** `monitor`, `applicationinsights`, `group_resource_list`, `pricing` |

Do not load: `configuring-opentelemetry-dotnet`, `entra-*`, `winui-wpf-migration`,
`winui-session-report`, `azure-deploy`/`azure-prepare` — see
`docs/desktop/12-agent-tooling/skill-routing.md` § "Not applicable to this conversion".
Every ticket runs its Kanmer pipeline with `get_doc_gates <id>` before each move; one gated
boundary per move; `board.yml` is never the authority.

## Traps recorded for this area (plan § 7)

1. **App Insights blind window** — the Log Analytics workspace runs a 0.1 GB/day cap resetting
   03:00Z that the estate exhausts within hours, so a working-hour telemetry check returns empty
   and the two alert rules cannot fire (PLAT-034). The diagnostics bundle is the primary support
   tool; verify telemetry only inside an uncapped window.
2. **Runtime-role grants missing on new tables** — shipped three times; tests run full-privilege
   so the suite is green while the estate refuses the write (PLAT-035).
3. **Plaintext verification account** reaching go-live; its password is in git history and is
   permanently disclosed.
4. **Secrets leaking through desktop logs or the bundle** — redaction by default, unit-tested,
   and the bundle itself is scanned.
5. **"Remember me" becoming a stored password** — only the refresh token is stored.
6. **Budgets judged on a fast developer machine**; measurements outside release builds.
7. **Memory growth** from image/document views and duplicate event subscriptions.
8. **Crash handling that swallows exceptions and continues** in a corrupted state.
9. **Alert or quota changes made casually** — ⚠ items need exact-target approval and a what-if.
10. **Scope creep** into obfuscation, anti-tamper and licensing — §17.2 non-goals are refused
    by reference in the threat register.

## Read these before starting any ticket in this epic

- `docs/desktop/10-security-observability-performance/README.md` (the area plan, all 8 sections)
- `docs/desktop/README.md` § Locked decisions and open decisions, § Routing legend
- `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape, § Ticket template
- `docs/desktop/12-agent-tooling/skill-routing.md` (exact names, pins, do-not-load table)
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` §§ 15–18, § 22.2, § 24 Phase 8
- `AGENTS.md` (ADR conventions and the reserved ADR-0100…0110 block, Simplicity and Safety
  rails, Repository task workflow steps 4 and 5) and `docs/engineering.md` § Evidence and
  § Required evidence tiers (tiers 1, 3, 7, 9, 10, 11 are all used here)
- `docs/runbook.md` § Live-operation approval matrix and § Corpus safety and evaluation
- `docs/current-architecture.md:160-183` (PLAT-034 and PLAT-035 in the operator's own words)
- `docs/operations.md:363-369` (the quota) and `:768-775` (the verification account)
- `docs/design/README.md` before writing any operator-facing copy or state label
