---
id: TEST-010
type: ticket
title: >-
  DSK-08-10 · `eng/packaging/Test-Package.ps1`: install, upgrade, mandatory
  update, blocked client, signature failure, interrupted update, rollback,
  uninstall, no-admin, cert trust
status: backlog
area: testing
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-08
  - phase-2
  - tier-11
  - needs-operator
groups:
  - EPIC-009
  - HZN-003
links: []
blocks:
  - REL-005
docs_todo: true
archived: false
created: '2026-08-24T07:51:10.280Z'
updated: '2026-08-24T08:51:12.787Z'
---

## What

Write `eng/packaging/Test-Package.ps1`, the scripted packaging and update suite that drives the local feed through every install and update scenario — clean install, upgrade from each supported previous version, mandatory update, blocked obsolete client, signature failure, interrupted update, rollback, uninstall and reinstall, no-administrator install, and certificate-trust deployment — asserting the expected `Get-AppxPackage` state after each.

## Why

Proposal §22.2 ("Packaging and update tests") lists exactly these ten scenarios, and §9 makes the two-layer enforcement (App Installer plus the gateway minimum-version gate) a hard requirement: an unsupported client must not be able to proceed. The distribution route is now fully decided — sign in-house with a self-managed certificate (D-002) and serve over SMB from a UNC share (D-003) — which means the local stack can rehearse the real mechanism rather than an HTTP substitute, and must additionally cover the two failure modes that route introduces: installing when the certificate is *not* trusted, and a renewal where trust is pushed before the newly signed package. Runs on the Test/UAT stack from [[DSK-08-17]] against the `.appinstaller` template from [[DSK-09-03]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-10`
- Plan detail: `docs/desktop/08-testing/README.md` § 3 (both distribution decisions settled; the packaging suite additionally covers the untrusted certificate — expect `0x800B0109` — and a certificate-renewal rollout where trust is pushed before the newly signed package) and `docs/desktop/08-testing/test-uat-stack.md` § "Tickets to build it" and § "Machine prerequisites"
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "Packaging and update tests", § 9.1 two-layer enforcement, § 21.2 stage 11
- Repository evidence:
  - `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § "Template (2021 schema)" — `OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="true"`, `<AutomaticBackgroundTask />`, `<ForceUpdateFromAnyVersion>true</ForceUpdateFromAnyVersion>`; `Uri` must equal the exact served path; the `.appinstaller` `Version` must increase on every publish including a rollback; `Publisher` must match the signing certificate subject
  - `.codex/skills/winui-packaging/SKILL.md` — `winapp cert generate --manifest`, `winapp cert install ./devcert.pfx` (admin), `winapp package <dir> --cert ./devcert.pfx --self-contained`, and the troubleshooting rows for publisher mismatch and untrusted certificate
  - `docs/desktop/08-testing/test-uat-stack.md` § Lifecycle — the `Publish-Feed` verb used to simulate mandatory updates and rollbacks
  - `.gitignore` — `artifacts/` is ignored; transcripts are filed in the ticket, not the tree
- Binding decisions:
  - D-002 — production signing is a self-managed certificate trusted per workstation in `LocalMachine\TrustedPeople`; the suite runs with a development certificate in the same store and the same mechanism.
  - D-003 — the feed is a UNC file share served over SMB; the local feed is a file share or folder share, never an HTTP substitute.
  - L-02 — everything is local; the production feed and certificate are proved only on the pilot ring.
- Depends on: `DSK-08-17` — the Test/UAT stack and its `Publish-Feed` verb. `DSK-09-03` — the `.appinstaller` templates and the `eng/packaging/Test-AppInstaller.ps1` validator this suite sits beside.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`)
- **MCP**: Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`) for App Installer update semantics; Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-10` and § 3, `docs/desktop/08-testing/test-uat-stack.md`, and `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` in full. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Load `pegasus-desktop`, then `winui-packaging`. Verify the App Installer behaviours you are about to assert with `microsoft_docs_fetch` on <https://learn.microsoft.com/windows/msix/app-installer/update-settings> and <https://learn.microsoft.com/windows/msix/app-installer/auto-update-and-repair--overview>; quote what you confirm in the ticket research document rather than assuming.
3. **Operator step**: on the dedicated Windows 11 Test/UAT workstation, produce a development certificate with `winapp cert generate --manifest .` so the subject matches the package manifest `Publisher` exactly, and trust it with `winapp cert install ./devcert.pfx` from an elevated session (`Cert:\LocalMachine\TrustedPeople`). Hand back the certificate thumbprint and subject. Keep a copy of the certificate **untrusted** for the signature-failure scenario.
4. Create `eng/packaging/Test-Package.ps1` with parameters `-FeedPath`, `-PackagePath`, `-FromVersion`, `-ToVersion`, `-Scenario` (default `All`) and `-TranscriptPath` (default `artifacts/packaging/`). Every scenario writes a `Get-AppxPackage CollisionEngineers.Pegasus` transcript before and after itself — that pair is the evidence, per `test-uat-stack.md` § "Evidence capture".
5. Implement scenario `CleanInstall`: no package present, open the channel `.appinstaller` from the feed, assert `Get-AppxPackage` reports the expected `Version` and `PackageFullName`, and assert the install required no administrator elevation.
6. Implement scenario `UpgradeFrom`: install `-FromVersion`, publish `-ToVersion` with `Publish-Feed`, relaunch, and assert the version after update. Parameterise over each supported previous version rather than only the immediately preceding one — "each supported previous version" is the proposal's wording.
7. Implement scenario `MandatoryUpdate`: with `UpdateBlocksActivation="true"` and `ShowPrompt="true"` in the published `.appinstaller`, assert that the old version cannot be activated until the update is taken, then that activation succeeds afterwards.
8. Implement scenario `BlockedClient`: set the gateway minimum client version above the installed package ([[DSK-04-06]]) and assert the desktop shows the update-required state and performs no `/api/v1` command — this is the gateway half of the two-layer enforcement and must be proved independently of App Installer.
9. Implement scenario `SignatureFailure`: publish a package signed with the untrusted certificate and assert the install fails with `0x800B0109` (a certificate chain processed but terminated in a root certificate the trust provider does not trust). Then implement `CertificateTrustRollout`: import the certificate into `LocalMachine\TrustedPeople` first, publish the same package, and assert the install now succeeds — the rehearsal of the D-002 renewal order.
10. Implement scenario `InterruptedUpdate`: begin an update and cut the feed off mid-transfer (rename or unshare the feed folder), assert the installed version is unchanged and the app still launches, then restore the feed and assert the update completes.
11. Implement scenarios `Rollback` (publish a lower `MainPackage Version` with an increased `.appinstaller` `Version` and `ForceUpdateFromAnyVersion` true; assert the workstation moves down), `UninstallReinstall` (assert intended user settings survive and nothing else does) and `NoAdmin` (run the whole clean install as a standard user and assert no elevation prompt).
12. Make the script exit non-zero on the first failed assertion, and write a per-scenario summary table (scenario, expected, observed, pass/fail) to the transcript path.
13. **Operator step**: run `-Scenario All` on the workstation once end to end, and hand back the transcript directory. File it as ticket proof (`command-log`); nothing goes into the repository tree.
14. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] All ten proposal scenarios plus the certificate-trust rollout are scripted with explicit expected `Get-AppxPackage` state.
- [ ] The untrusted-certificate install fails with `0x800B0109`, and the same package installs after the certificate is trusted.
- [ ] The blocked-client scenario proves the gateway gate independently of App Installer.
- [ ] The interrupted update leaves the previous version installed and launchable.
- [ ] Rollback moves a workstation to a lower package version through an increased `.appinstaller` version.
- [ ] Every scenario writes a before/after `Get-AppxPackage` transcript.

## Verification

- [ ] `pwsh ./eng/packaging/Test-Package.ps1 -Scenario All -FeedPath <local feed> -TranscriptPath ./artifacts/packaging` — expected: exit 0 and a summary table with every scenario `PASS`.
- [ ] `Get-AppxPackage CollisionEngineers.Pegasus` after the rollback scenario — expected: `Version` equal to the rolled-back version.
- [ ] `pwsh ./eng/packaging/Test-AppInstaller.ps1 <published .appinstaller>` — expected: exit 0 (the published file the suite used is valid).

## Evidence tier

Tier 11 — Migration/recovery. It obliges every supported prior version to be exercised, the update to be idempotent and interruptible without loss, and the previous artefact to remain usable; `efbundle` against Azure SQL and point-in-time restore stay pilot-ring checks.

## Documentation changes

- `docs/runbook.md` — add the packaging suite command to the desktop test section.
- `docs/operations.md` § Evidence profiles — register the `Packaging` trait and what it proves.
- `docs/desktop/08-testing/README.md` § 4 — mark the packaging/update row as scripted.

## Guardrails

- **Azure**: no write. The feed is a local file share; no Azure storage is involved at any point.
- **Scope boundary**: may create `eng/packaging/Test-Package.ps1` and its transcripts under `artifacts/`. Must not modify the `.appinstaller` templates (owned by [[DSK-09-03]]), must not touch the production feed or the production certificate, and must not publish anything to a real UNC share outside the Test/UAT stack.
- **Traps**: the package under test is installed and uninstalled repeatedly — dedicated workstation only, never a machine holding a pilot install. `Publisher` must match the certificate subject exactly or every install fails for the wrong reason. The `.appinstaller` `Version` must increase on every publish, including the rollback publish. The private key of any production certificate never leaves the signing host and is never a GitHub secret.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
