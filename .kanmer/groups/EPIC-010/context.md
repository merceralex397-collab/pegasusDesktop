# EPIC-010 · Area 09 — release, update and distribution

Read this once before working any `DSK-09-*` ticket. It carries what binds the
whole epic; the ticket carries the work.

## What this epic delivers

The desktop package's whole life: build properties and versioning, the MSIX,
the `.appinstaller` update contract and its validator, the CI packaging lanes,
the production signing certificate and its per-workstation trust, the update
feed, and the runbooks R1–R10 that publish, enforce, roll back, renew, block
and onboard. Proposal § 9 accepts mandatory updates and places more logic in the
client on the strength of them — so this path is critical infrastructure, not
packaging chrome. Out of scope: the gateway's own deployment (the existing
`pegasus-release` skill, referenced not rewritten) and the Microsoft Store.

## Proposal coverage

§ 9.1 (package half of the two-layer enforcement; the gateway half is area 04),
§ 9.2 (startup sequence as seen from the package), § 9.3 (operational controls),
§ 21.1–21.3 (build properties, CI stages, environments), § 24 Phase 2 (pilot
feed), Phase 9 (pilot ring, parallel run, update and rollback exercised),
Phase 10 (mandatory production release), § 26 (the operations documents),
§ 29 item 7 (the signed development MSIX and mandatory-update flow).

## Decisions that bind every ticket here

- **D-002 (2026-08-23, decided)** — production signing is a **self-managed
  certificate**, held in-house, trusted per workstation in
  `LocalMachine\TrustedPeople`, **never** `Trusted Root`. Subject equals the
  manifest `Publisher` exactly and never changes. ~3-year validity. Timestamping
  is mandatory. The `.pfx` never leaves the signing host and is never a GitHub
  secret. **Write for this route only — it is not a comparison.**
- **D-003 (2026-08-23, decided)** — the update feed is a **UNC file share** on an
  always-on in-house Windows host, served to App Installer over **SMB**. MIME
  types, `Content-Length` and HTTP byte ranges do not apply; share ACLs and a
  permanently stable UNC path replace them. **Write for this route only.**
- **C-01** — the repositories become private on completion. GitHub Releases and
  GitHub Pages are ruled out **permanently** and must not be re-proposed;
  private Windows runner minutes bill at 2×, so CI time is a live cost.
- **L-02** — Test/UAT is the local production-mimicking stack; ADR-0014 stands.
  Asking for an Azure test resource is out of bounds. Every runbook rehearses on
  the stack first, then on the pilot ring.
- **Consequence** — the distribution path (sign in-house → copy to the share →
  App Installer over SMB) touches **no Azure resource at all**. This area's
  earlier ⚠ Azure writes are **withdrawn**; area 11 mirrors that.
- **Assumption in force**: package identity `CollisionEngineers.Pegasus`, one
  identity, two channels (`pilot`, `prod`); ten Windows 11 x64 workstations with
  the WebView2 runtime present.
- **Deviation on record**: the proposal fixes no version scheme; this area
  chooses `1.<minor>.<build>.0` (build = CI run number, revision always `0`) for
  monotonicity and rollback simplicity. A separate pilot package identity was
  considered and rejected — ring change is a reinstall.
- **Withdrawn, never revive**: `DSK-09-07` (Azure Artifact Signing spike) and
  `DSK-09-09` (OV certificate spike). Never cite them as a dependency.

## Exit gate and what proves it

Obsolete package blocked and updates (Phase 2); update and rollback exercised on
the pilot ring with the support runbook **proven, not written** (Phase 9);
mandatory production release shipped with no user needing the legacy web UI
(Phase 10); install, mandatory update and rollback all proven (§ 27 item 13);
unsupported versions cannot proceed (§ 27 item 4). Proof lives in ticket proof
documents (`command-log`, `test-output`, `visual`) and in the `docs/operations.md`
desktop release row, refreshed in the same task as each release.

## Routing for this area

- Subagents: `pegasus-release-packager` (owns nearly every row),
  `pegasus-azure-auditor` (read-only inventory records), `pegasus-test-engineer`
  (packaging and update tests), `pegasus-desktop-reviewer` (independent review —
  an agent that did not implement). All at `.codex/agents/<name>.toml`.
- Skills, project skill first:
  `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`);
  `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`, vendored from
  `microsoft/win-dev-skills` v0.5.0 `f1028dd5`);
  `pegasus-release` (`.agents/skills/pegasus-release/SKILL.md`);
  `authoring-github-workflows`, `directory-build-organization`,
  `binlog-failure-analysis` (`dotnet/skills` `98f84851`);
  `winui-ui-testing` for update-screen assertions; Kanmer skills under
  `.grok/skills/`.
- MCP: Kanmer; Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`,
  `microsoft_code_sample_search`); Azure MCP **read-only** only.
- Do **not** load: `azure-deploy`, `azure-prepare`, `entra-app-registration`,
  `winui-wpf-migration`, `dotnet-aot-compat` — see the do-not-load table in
  `docs/desktop/12-agent-tooling/skill-routing.md`.

## Traps (area plan § 7)

2017/2 `.appinstaller` schema silently ignores `ShowPrompt`,
`UpdateBlocksActivation` and `HoursBetweenUpdateChecks` — 2021 only.
`ms-appinstaller:` protocol is disabled by default since December 2023 — publish
the file path. The package check **fails open** when the feed is unreachable;
only the gateway gate closes that door. Certificate expiry without timestamping
invalidates the signature path for new installs. Publisher/certificate mismatch
breaks packaging (`0x8007000B`); missing trust breaks install (`0x800B0109`).
SmartScreen warns on a new hash — reputation, not a signature failure. The CI
runner has no production certificate: signing only in the protected tag job or
on the release terminal. The Linux publish of Web and Worker must stay green —
desktop projects stay out of the Linux solution filter. Release docs drift:
`docs/operations.md:295` contradicts its own table and `CHANGELOG.md` stopped at
2026-08-03 — treat neither as current. App Insights' 0.1 GB/day cap (PLAT-034)
can hide update telemetry for most of a day. Group Policy/CSP overrides App
Installer settings on managed devices. `Package.Current.CheckUpdateAvailabilityAsync`
fails with access denied — use `PackageManager.FindPackageForUser`.

## Read before starting any ticket in this epic

1. `docs/desktop/09-release-update-and-distribution/README.md`
2. `docs/desktop/09-release-update-and-distribution/appinstaller-template.md`
3. `docs/desktop/09-release-update-and-distribution/runbooks.md`
4. `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md`
5. `docs/desktop/README.md` (decisions, routing legend)
6. `docs/desktop/00-governance-and-workflow/README.md` (board shape, ADR block, phase map)
7. `docs/desktop/12-agent-tooling/skill-routing.md` (exact names, pinned revisions, do-not-load)
8. `docs/desktop/08-testing/test-uat-stack.md` (where every rehearsal happens)
9. `AGENTS.md` (task workflow, simplification pass, safety rails, Markdown placement)
10. `docs/runbook.md` § Live-operation approval matrix; `docs/engineering.md` § Required evidence tiers
