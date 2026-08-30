# Plan — FND-039: Dev-certificate MSIX build and the install/uninstall packaging script

**Diff estimate: ~3 files, ~185 lines.**

## Measured file-and-line inventory

Profile `chore` owes no `research` and no `files` document, so this plan carries the
surface-area burden alone (`.grok/skills/kanmer-plan/assets/plan-template.md`'s
"written FROM research and files" precondition does not apply). Every row below was measured
against the fork working tree on 2026-08-24 with `wc -l`, `cat -n` and `grep -n`; the diff
estimate is the sum, not an assertion.

| Path | Measured now | Change | Lines |
| --- | --- | --- | --- |
| `tests/Pegasus.Packaging.Tests/Test-InstallUninstall.ps1` | does not exist — `ls tests/` returns exactly `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests` | **New.** Parameterised strict-mode script: remove stale package, install, assert identity, launch, terminate, uninstall, assert clean state, write a result log | ~170 |
| `.gitignore` | 77 lines; the "Local secrets and environment state" block is `:4-13` (`.env`, `.env.*`, `!.env.example`, `secrets.json`, `appsettings.*.local.json`, `/.azure/*`, `!/.azure/deployment-plan.md`, `**/local.settings.json`, `*.user`). **`grep -n 'pfx\|\.cer$' .gitignore` returns nothing — no certificate pattern exists today** | Add `*.pfx`, `*.cer`, `devcert.*` after `:13`, inside the existing secrets block rather than as a fourth comment heading | +3 |
| `docs/runbook.md` § Supported platform (`:19`) | The section runs `:19-38`; the release-operations paragraph is `:30-34` and the CI-runner paragraph `:36-38`. **`grep -n 'winapp\|Developer Mode\|MSIX' docs/runbook.md` finds no desktop prerequisite anywhere** | Insert one new paragraph after `:34`: the `winapp` CLI prerequisite, Developer Mode, and the development-certificate trust step for a Test/UAT machine | +12 |

Not touched, and each is a deliberate exclusion recorded under *Risks* below:
`.github/workflows/ci.yml` (owned by [[REL-005]], plan handle `DSK-09-05`), any `.appinstaller`
template or feed ([[REL-003]], plan handle `DSK-09-03`; [[REL-008]], plan handle `DSK-09-10`),
`src/Pegasus.Desktop/Package.appxmanifest` (owned by [[FND-030]], plan handle `DSK-02-05`),
and `Directory.Build.props` (the product `Version` `0.1.0-alpha.1` at `:9` is the gateway's
identity, not the desktop's — plan 09 § 3 "Versioning").

## Approach

**Drive the whole packaging path through the vendored `winapp` CLI exactly as
`.codex/skills/winui-packaging/SKILL.md` prescribes, and put every assertion about the result
in one strict-mode PowerShell script rather than in prose an operator has to re-read.** The
alternative rejected is **hand-rolling the package with `MakeAppx.exe` and `signtool.exe`**:
it is the classic route, but nothing in this repository mentions `signtool` or MSIX today
(plan 09 § 2 Facts), the skill is pinned at win-dev-skills v0.5.0 `f1028dd5`, and its
`winapp cert generate --manifest` is the one mechanism that prevents the "Publisher mismatch"
failure the skill's troubleshooting table lists first (`SKILL.md:109`). Choosing the two-tool
route would mean owning the Publisher match by hand at exactly the point where getting it
wrong is permanent. The second alternative rejected is **an xunit-hosted packaging test**
matching the other three test projects: `Add-AppxPackage` / `Get-AppxPackage` are PowerShell
cmdlets on a machine, not in-process assertions, and area 08 already intends
`eng/packaging/Test-Package.ps1` ([[TEST-010]], plan handle `DSK-08-10`) as the PowerShell
harness this script's scenarios are later absorbed into — so a C# shim here would be a second
harness to delete.

The scripted assertions are the substance, not ceremony. The failure this ticket exists to
catch is silent: `Get-AppxPackage <name>` returns **nothing** for a package that was never
installed, so a script that pipes it into `Remove-AppxPackage` and exits 0 has proved
nothing at all. Every assertion therefore names the expected value and prints the actual one.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(confirmed by `get_doc_gates FND-039`). No existing PRD, FRD or ADR is claimed to be met.

> **New ADR** — ADR-0105 (MSIX packaging and distribution, and the minimum-version gate),
> authored by [[REL-001]] (plan handle `DSK-09-01`); [[FND-005]] (plan handle `DSK-00-05`)
> and [[FND-042]] (plan handle `DSK-04-01`) also claim ADR-0105 — see [[REL-001]]'s plan for
> the ownership reconciliation.
> A second new ADR this plan sits under: **ADR-0100** (native WinUI 3 desktop client inside
> this fork, which authorises the packaged project being signed here), authored by
> [[FND-026]] (plan handle `DSK-02-01`); [[FND-005]] also claims ADR-0100 — see [[FND-026]]'s
> plan for the ownership reconciliation.
> This plan is written to the decisions as recorded in `docs/desktop/README.md`
> § Locked decisions (D-002, D-003, C-01) and
> `docs/desktop/09-release-update-and-distribution/README.md` § 3; if either ADR lands
> differently this plan is revised before implementation.

Because `refs` is empty, the programme-level authorities that bind today, each with the step
that satisfies it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| **D-002** (`docs/desktop/README.md` § Locked decisions) | Production signing is a self-managed certificate trusted per workstation in `LocalMachine\TrustedPeople`, **never** Trusted Root; the certificate subject equals the manifest `Publisher` exactly; the production `.pfx` never leaves the signing host and is never a GitHub secret | Steps 3, 4 and 5 — the development/production trust-store difference is recorded, and the production rollout is explicitly [[REL-007]]'s (plan handle `DSK-09-08`) |
| **D-003** | The feed is a UNC share over SMB | Not exercised here: this ticket installs locally, and step 12 keeps the feed out of scope |
| **C-01** | The repositories become private | Step 4 — no certificate or key ever enters the repository or GitHub secrets |
| Proposal § 7.1 Runtime | Self-contained signed MSIX | Step 6 (`--self-contained`) |
| Proposal § 21.2 CI stages steps 8–11 | Package, sign, verify, publish are distinct stages | Step 12 — this ticket owns package and sign locally; publish is [[REL-008]] and the CI lane is [[REL-005]] |
| Proposal § 24 Phase 1 | A development MSIX exists before Phase 2 wires the real feed | Steps 2–6 |
| Plan 02 § 3 decision 3 | Packaged single-project MSIX, `WindowsAppSDKSelfContained=true`, `SelfContained=true`, x64 | Steps 2 and 6 |
| Plan 02 § 4 exit-gate row "Clean Windows 11 machine launches the native shell from a dev-signed MSIX" | Install log plus a screenshot of the shell (tier 7) | Step 9 |
| Plan 02 § 4 exit-gate row "Install/uninstall works and leaves only intended user settings" | `%LOCALAPPDATA%\Packages\<pfn>` removed, DPAPI store removed | Steps 7 and 9 |
| Plan 02 § 7 trap "Package identity churn" | `Identity.Name` and `Identity.Publisher` are permanent, and D-002's certificate subject must equal the Publisher | Steps 3 and 10 |
| Plan 02 § 7 trap "Self-contained size" | Acceptable for ten users but it must be measured and recorded | Step 6 |
| Plan 09 § 2 Facts | The vendored skill is the pinned procedure: `cert generate --manifest`, `cert install` (admin), `package --cert` preferred over separate `sign`, `--self-contained`, `--timestamp` critical for production | Steps 2–6 |
| Plan 09 § 3 "Signing" | Signing happens only in a protected tag job or on the authorised release terminal; **never on a PR build** — PR builds use the dev certificate | Step 12's boundary to [[REL-005]] |
| Plan 09 § 7 trap "Publisher mismatch" | Certificate/manifest mismatch breaks packaging | Step 3 |
| Plan 09 § 7 trap "Certificate expiry without timestamping" | Timestamp every production signature | Step 6 records that the development package omits `--timestamp` and why that is a difference, not an oversight |
| Plan 09 § 7 trap "SmartScreen" | First-download warning is not a signing failure | Step 9's operator report |
| `docs/engineering.md:76` § Required evidence tiers, tier 11 | Install, previous-artifact compatibility and clean removal demonstrated, not asserted | Verification |
| `docs/engineering.md:201` § Plan sizing | A plan states its diff estimate first, from a real number | The first line and the inventory above |
| `AGENTS.md` § Simplicity rails (one list per concept) | The `winapp`/Developer Mode prerequisite is stated **once** in `docs/runbook.md`; [[FND-028]], [[FND-030]] and [[FND-048]] cite it | Step 11 |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 12 |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing, reviewer `pegasus-desktop-reviewer` |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-packaging`
  (`.codex/skills/winui-packaging/SKILL.md`, 121 lines, win-dev-skills v0.5.0 `f1028dd5`,
  vendored and verified present 2026-08-24) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, with `BuildAndRun.ps1` beside it) for the
  Release build.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for
  `Add-AppxPackage` / `Get-AppxPackage` semantics and single-project MSIX).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates FND-039` before
  every move; a move crosses at most one gated boundary. Note that `chore` owes `plan` at
  `leave-preparing` and `proof` at `enter-done`, and no `research`, `files` or `checklist`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps in the same order, with the same
ownership and the same file paths. Each names a measured current value.

1. **Orient and take.** Read `.codex/skills/winui-packaging/SKILL.md` in full — it is 121
   lines and the whole procedure is in it: the five-step workflow at `:22-52`, the key rules
   at `:54-63`, the troubleshooting table at `:105-116`. Read plan 09 § 2 Facts, § 3
   "Signing" and § 7, and plan 02 § 3 decision 3 and § 7. Confirm the prerequisite exists —
   `ls src/Pegasus.Desktop/Package.appxmanifest`; if it is missing, stop: this ticket is
   blocked behind [[FND-030]] (plan handle `DSK-02-05`). Then `get_doc_gates FND-039`,
   `take_ticket FND-039`, and branch `task/desktop-dev-msix` from `origin/dev`.
2. **Build Release without launching.**
   `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj /p:Configuration=Release -SkipRun`.
   The script honours both flags — `BuildAndRun.ps1:26` declares `[switch]$SkipRun` and
   `:93-102` detects an explicit `/p:Configuration=` and does not override it. **Record the
   build-output directory the script reports**; step 6 takes that directory as its positional
   argument, and guessing it is the most common way this step fails.
3. **Generate the development certificate against the manifest.**
   `winapp cert generate --manifest src/Pegasus.Desktop --if-exists skip`. `--manifest`
   auto-matches `Identity.Publisher` (`SKILL.md:29-31`, and `:55` states the rule), which is
   what prevents the first entry in the troubleshooting table (`:109`). Then **record both
   strings in this plan under a dated note**: the manifest `Publisher` value and the
   certificate subject, character for character. They must be identical, and under D-002 the
   production certificate's subject must equal the same string ~three years from now. Note
   also that the default PFX password is `password` (`SKILL.md:58`) — acceptable for a
   development certificate that never leaves the workstation, and explicitly not the pattern
   for production.
4. **Close the certificate leak before generating anything else is committed.** `.gitignore`
   is 77 lines and `grep -n 'pfx' .gitignore` currently returns **nothing**, so add `*.pfx`,
   `*.cer` and `devcert.*` immediately after `:13` (`*.user`), inside the existing
   "Local secrets and environment state" block that begins at `:4` — a new comment heading
   would be a fourth list of one concept. Then confirm `git status --porcelain` never lists
   the certificate. Under D-002 a production `.pfx` is not a repository file and not a GitHub
   secret at all; this pattern exists so the development one cannot become one by accident.
5. **Operator step — trust the development certificate.** `winapp cert install ./devcert.pfx`
   requires an elevated terminal (`SKILL.md:35-37`, and `:57` and `:112` both say so). The
   operator runs it and hands back
   `Get-ChildItem Cert:\LocalMachine\Root | Where-Object Subject -eq '<publisher>'` showing
   the thumbprint and store. **Record explicitly in this plan** that the skill's `cert
   install` places the certificate in the machine **Trusted Root** store (`SKILL.md:37`,
   "Adds cert to machine Trusted Root store"), that this is acceptable for the **development**
   certificate on development and Test/UAT machines only, and that the **production**
   certificate goes to `Cert:\LocalMachine\TrustedPeople` and never to Trusted Root
   (D-002; plan 09 § 3 "Signing" at `:215-216`) — [[REL-007]] (plan handle `DSK-09-08`) owns
   that rollout and [[REL-006]] (plan handle `DSK-09-06`) owns the development-certificate
   pipeline for Test/UAT machines.
6. **Package and sign.** `winapp package <build-output-dir> --cert ./devcert.pfx
   --self-contained` (`SKILL.md:16` and `:39-43`). Expected: a `.msix` containing
   `resources.pri` and a reported successful signature. **Record the produced package size in
   bytes** — [[REL-002]]'s (plan handle `DSK-09-02`) `desktop-release-manifest.json` needs the
   number and plan 02 § 7 requires it measured rather than estimated. Record deliberately
   that `--timestamp` is **omitted** here: `SKILL.md:59-62` and plan 09 § 7 make it mandatory
   for production, and the difference between the development and production commands must be
   visible in the proof rather than discovered later.
7. **Write `tests/Pegasus.Packaging.Tests/Test-InstallUninstall.ps1`.** The directory does not
   exist — `ls tests/` returns exactly three projects today — so this creates it. Follow the
   repository's script convention exactly as `scripts/Test-AzureDeploymentPlan.ps1:1-22` sets
   it: `[CmdletBinding()]`, a `param(...)` block with `[Parameter(Mandatory)]` on the `.msix`
   path, then `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'`. The
   script performs, in order: `Get-AppxPackage <Identity.Name> | Remove-AppxPackage` to clear
   a stale install; `Add-AppxPackage <msix>`; assert `Get-AppxPackage` reports the expected
   `Name`, `Publisher` and `Version`; launch via `winapp run` and assert the process starts;
   terminate it; `Remove-AppxPackage`; assert `%LOCALAPPDATA%\Packages\<PackageFamilyName>`
   no longer exists; assert the DPAPI credential-store files are gone.
8. **Make every assertion fail loudly with the actual value**, and write each result
   (command, expected, actual) to a log file the proof attaches. The specific failure to
   defend against: `Get-AppxPackage <name>` returns nothing for a package that was never
   installed, so `Get-AppxPackage … | Remove-AppxPackage` succeeds silently and a naive
   script exits 0 having proved nothing. Assert the package **is** present after
   `Add-AppxPackage` before asserting anything about its removal.
9. **Operator step — clean-machine run.** On a clean Windows 11 x64 machine (or a fresh VM
   snapshot) with the development certificate trusted, run
   `pwsh ./tests/Pegasus.Packaging.Tests/Test-InstallUninstall.ps1 -Package <path>.msix`. The
   operator hands back three things: the script log, a screenshot of the launched shell (the
   tier-7 evidence plan 02 § 4 names for that gate row), and confirmation that
   `Add-AppxPackage` itself needed **no** administrator elevation — only the one-time
   `cert install` in step 5 did. If SmartScreen warns on first download, record it: plan 09
   § 7 says it is expected on a new hash and is not a signing failure.
10. **Record the installed package identity.**
    `Get-AppxPackage <Identity.Name> | Select-Object Name, Publisher, PackageFamilyName, Version`
    into the proof. This is the value [[FND-036]]'s (plan handle `DSK-02-11`) diagnostics
    bundle manifest and [[REL-002]]'s release manifest must both agree with, and plan 09 § 3
    fixes the package identity as `CollisionEngineers.Pegasus` for both channels — confirm
    the installed `Name` matches it.
11. **Write the `docs/runbook.md` § Supported platform prerequisite paragraph.** The section
    is `:19-38`; insert after the release-operations paragraph that ends at `:34`, before the
    CI-runner paragraph at `:36`. `grep -n 'winapp\|Developer Mode\|MSIX' docs/runbook.md`
    returns nothing today, so this is the first statement of it and must be complete: the
    `winapp` CLI prerequisite (≥ 0.3, installed with `winget`), Developer Mode, and the
    development-certificate trust step for a Test/UAT machine. Match the section's existing
    voice — it states platform facts and defers route decisions to their ADR, as `:30-34`
    does for ADR-0007. **This ticket is the single owner of that sentence**: [[FND-028]] (plan
    handle `DSK-02-03`), [[FND-030]] and [[FND-048]] (plan handle `DSK-04-12`) cite it rather
    than restating it, so the section never carries four statements of one prerequisite.
12. **Hold the scope line, simplify, open the PR.** Do **not** build the `.appinstaller`
    ([[REL-003]], plan handle `DSK-09-03`), the feed ([[REL-008]], plan handle `DSK-09-10`),
    the version generator ([[REL-002]]) or any CI lane — **[[REL-005]] (plan handle
    `DSK-09-05`) owns the `desktop-package` lane**, and [[FND-040]] (plan handle `DSK-02-15`)
    owns `desktop-build`, which packages nothing. Note in this plan that this script's
    scenarios are later absorbed by `eng/packaging/Test-Package.ps1` ([[TEST-010]], plan
    handle `DSK-08-10`), so the repository does not end up with two packaging harnesses: this
    one is the Phase 1 minimum and is superseded, not extended in place. Run the
    simplification pass over this branch's own diff, record it under a dated
    `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 11 — Migration/recovery** (`docs/engineering.md:76`),
read here as the packaging/install tier — install, previous-artifact compatibility and clean
removal are *demonstrated on a real machine*, not asserted. Proof types: `command-log` and
`visual` (the shell screenshot from step 9). The tier's limit must be stated in the proof
too: a single clean-machine install proves install and uninstall; it proves **nothing** about
upgrade, downgrade, interrupted update or signature failure, which are [[TEST-010]]'s
scenarios.

| Command / observation | Expected evidence |
| --- | --- |
| `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj /p:Configuration=Release -SkipRun` | Release build succeeds; the reported build-output directory is recorded |
| `winapp cert generate --manifest src/Pegasus.Desktop --if-exists skip` | `devcert.pfx` produced; subject string recorded and identical to the manifest `Publisher` |
| `git status --porcelain` after step 4 | no `.pfx` or `.cer` listed |
| `winapp package <build-output-dir> --cert ./devcert.pfx --self-contained` | a signed `.msix` with **no** "Publisher mismatch" error; package size in bytes recorded |
| `pwsh ./tests/Pegasus.Packaging.Tests/Test-InstallUninstall.ps1 -Package <path>.msix` on a clean Windows 11 machine | exit `0` and a log showing install → identity asserted → launch → uninstall → clean state, each line carrying expected and actual |
| `Get-AppxPackage <Identity.Name>` after the run | no output — the package is gone |
| `Test-Path "$env:LOCALAPPDATA\Packages\<PackageFamilyName>"` after the run | `False` |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit `0` — the CI `documentation` job runs it over the `docs/runbook.md` change |
| `git diff --name-only` at PR time | exactly `.gitignore`, `docs/runbook.md`, `tests/Pegasus.Packaging.Tests/Test-InstallUninstall.ps1`; **no** `.github/workflows/ci.yml`, no `Package.appxmanifest` |
| Observations stated rather than inferred | the manifest `Publisher` and certificate subject; the package size; whether `Add-AppxPackage` needed elevation; whether SmartScreen warned |

## Risks / open questions

- **Risk — a script that exits 0 having proved nothing.** `Get-AppxPackage <name>` returns
  nothing for an absent package, so the stale-removal line succeeds either way. Mitigation:
  step 8 asserts presence after install before asserting absence after uninstall, and every
  assertion prints the actual value.
- **Risk — the Publisher is set wrong and discovered after users install.** `Identity.Name`
  and `Identity.Publisher` are permanent (plan 02 § 7); changing them makes a different
  application and invalidates D-002's certificate subject. Mitigation: step 3 records both
  strings verbatim in this plan, and step 10 records the identity actually installed so
  [[FND-036]] and [[REL-002]] can be checked against one measured value.
- **Risk — the development trust store is mistaken for the production one.** The skill's
  `cert install` writes to machine **Trusted Root** (`SKILL.md:37`); D-002 requires
  `LocalMachine\TrustedPeople` for production and never Trusted Root. Mitigation: step 5
  records the difference in this document, and the production rollout is [[REL-007]]'s
  ticket, not a widening of this one.
- **Risk — two packaging harnesses.** [[TEST-010]] (plan handle `DSK-08-10`) is
  `eng/packaging/Test-Package.ps1`, whose scenario list is a superset of this script's.
  Mitigation: step 12 records the intended relocation in this plan, so the later ticket moves
  the scenarios rather than duplicating them.
- **Risk — the development package's missing `--timestamp` is read as an oversight and
  copied to production.** Mitigation: step 6 records the omission and its reason explicitly;
  plan 09 § 7 and `SKILL.md:59-62` are both cited in the proof.
- **Risk — self-contained package size.** .NET plus Windows App SDK self-contained is large.
  Mitigation: step 6 measures it; plan 02 § 7 accepts the size for ten users but requires the
  number, and [[REL-002]] carries it forward into the release manifest.
- **Operator dependency, not an open question.** Steps 5 and 9 need an elevated terminal and a
  clean Windows 11 machine; the ticket carries the `needs-operator` label for exactly that.
  These are prerequisites the operator supplies, not decisions anyone still has to take —
  D-002 settled the signing route on 2026-08-23 and `docs/desktop/README.md` records that
  **no open decisions remain**.
- **Scope boundary, not an open question — the CI packaging lane.** [[REL-005]] (plan handle
  `DSK-09-05`) owns `desktop-package`; [[FND-040]] (plan handle `DSK-02-15`) owns
  `desktop-build` and packages nothing. This ticket edits no workflow file.
- **Open questions**: none. No `open-questions` document is created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds a
PowerShell script alongside documentation, so `n/a — docs-only` does not apply._

### 2026-08-29 — implementation simplification pass

- Reviewed the branch diff against the three-file scope. The implementation remains limited to `.gitignore`, `docs/runbook.md`, and the single packaging test script; no workflow, manifest, appinstaller, feed, release-manifest, or application-code changes were introduced.
- Reused one strict assertion/logging path for all expected/actual checks rather than duplicating step-specific error handling. The script performs no manual recursive deletion: package removal is limited to the exact installed package full name, and DPAPI cleanup is read-only inspection of the package-family or explicitly supplied store path.
- No behaviour-preserving simplification findings remained unapplied. The local package attempt exposed a WinApp CLI self-contained-runtime prerequisite failure; that is an environment/tooling blocker, not a reason to broaden this ticket into SDK installation or packaging infrastructure.

### Final script hardening — 2026-08-29

After independent review, commit `8a2dd5f0a1594aab5474277dc9166569bdbb3d66` hardens the smoke script. It now accepts only the package and result-log path, derives the current user's `%LOCALAPPDATA%\\Packages\\<PackageFamilyName>\\LocalState`, always launches the installed package location, captures the installed identity before cardinality assertions for failure cleanup, executes and enforces `winapp >= 0.3.0`, validates a four-part package version, checks the expected Windows App Runtime payload entries, and loads the packaged `DpapiCredentialStore` to save/read a real protected test entry before uninstall. Generated `.msix`/bundle outputs are ignored and the local fallback package remains untracked.

The payload check is necessary but does not prove that the required `winapp package ... --self-contained` command succeeded. In this environment WinApp CLI 0.3.1 still fails that exact command because `.winapp/self-contained/x64/deployment` is missing. Certificate trust, clean Windows 11 install/launch/uninstall, result log, launch screenshot, cleanup read-back, and no-elevation handback remain unfulfilled. No PR or Done claim is made.

## Independent review — 2026-08-29

Galileo (pegasus-desktop-reviewer) reviewed exact head 8a2dd5f0a1594aab5474277dc9166569bdbb3d66. The script hardening passes code review: it is bounded to the intended package/result-log inputs, validates the WinApp CLI and package shape, uses the installed package location, exercises a real DPAPI entry, and performs exact cleanup checks. Full acceptance remains **blocked** by the required winapp package ... --self-contained toolchain failure in this environment, plus certificate trust, clean Windows 11 install/launch/uninstall, result-log, launch-screenshot, cleanup read-back, and no-elevation operator evidence. A partial PR may be opened for review, but this ticket must not merge or reach Done on this evidence.

## Operator identity confirmation — 2026-08-29

The existing manifest identity values were re-read on the FND-039 branch and are used unchanged:

- `Identity.Name`: `CollisionEngineers.Pegasus`
- `Identity.Publisher`: `CN=Collision Engineers`
- `PublisherDisplayName`: `Collision Engineers`

These are the operator-confirmed permanent values. The development certificate must have the exact subject `CN=Collision Engineers` when generated from this manifest. This resolves only the identity-selection question; it does not resolve the separate self-contained WinApp CLI prerequisite failure or the required certificate-trust, clean-machine install/launch/uninstall, result-log, screenshot, cleanup, and no-elevation evidence. FND-039 remains in Review and is not mergeable or Done.

## Packaging retry — 2026-08-29

On branch `task/desktop-dev-msix`:

- `pwsh -NoProfile -File ./.codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj /p:Configuration=Release -SkipRun` — exit 0; Release x64 build succeeded and reported output `src/Pegasus.Desktop/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64`.
- `winapp cert info ./devcert.pfx` — subject `CN=Collision Engineers`, thumbprint `AC3468D9C8D1FF64FAE3980F93A0E92CC0BA3AED`, private key present.
- `winapp package ./src/Pegasus.Desktop/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64 --cert ./devcert.pfx --self-contained` — exit 1: `Runtime files not found at .winapp/self-contained/x64/deployment`.
- `dotnet publish ... --runtime win-x64 --self-contained true -p:WindowsAppSDKSelfContained=true` — exit 0 and produced the publish output, but did not populate the WinApp CLI deployment directory; no tracked files changed.

The exact package command still fails on the local toolchain. Existing identity/certificate matching is therefore not the remaining cause. FND-039 stays in Review and no package/install proof is claimed.

## Configuration-isolated packaging diagnostic — 2026-08-29

The existing manifest and certificate values were used unchanged. To isolate the separate WinApp CLI runtime-package issue, a temporary configuration was generated outside the repository at `C:/Users/PC/AppData/Local/Temp/pegasus-fnd039-winapp-config/winapp.yaml`; it was not added to the branch. The config identifies the restored `Microsoft.WindowsAppSDK` package version `2.4.0` and its runtime package graph.

With that temporary config as the working directory, the same `winapp package ... --cert ... --self-contained` operation succeeded and produced:

- `Pegasus.Desktop-test.msix`: 94,156,443 bytes;
- archive manifest identity: `Name=CollisionEngineers.Pegasus`, `Publisher=CN=Collision Engineers`, `Version=0.1.0.0`, `ProcessorArchitecture=x64`;
- signer subject and issuer: `CN=Collision Engineers`;
- signer thumbprint: `AC3468D9C8D1FF64FAE3980F93A0E92CC0BA3AED`.

Windows reports the package signature as locally untrusted because this is the development self-signed certificate, which is expected until the operator performs the documented trust step. This diagnostic proves the identity/certificate match is correct and that the remaining local failure is the standalone CLI's missing Windows App SDK package configuration/runtime staging, not identity selection. The required repo-root command still fails without that configuration, so no exact-command acceptance claim is made. No repository file, certificate, corpus, cloud resource, or upstream remote was changed.

## Prerequisite merge — 2026-08-29

PR #48 merged into `dev` after the exact reviewed head passed every applicable CI lane.

- PR: https://github.com/merceralex397-collab/pegasusDesktop/pull/48
- Reviewed head: `c586bb71fb9457db4c0f7661cfe5e89763f4ada3`
- CI: run `33269737264` — completed successfully; browser, unit, all three SQL shards, changes, documentation, local-development-scripts, reference-data, and SQL integration coverage succeeded; infrastructure was skipped by its documented path filter.
- Resulting `origin/dev`: `e071d3ca43e70fd695c1f9907856d61d5b189685`

This is a prerequisite merge only. FND-039 remains in Review and is not Done: the exact repo-root self-contained packaging command still lacks the CLI runtime package configuration, and the operator certificate-trust plus clean-machine install/launch/uninstall, result-log, screenshot, cleanup, and no-elevation evidence remain outstanding.

## Exact package repair and revalidation — 2026-08-30

The operator-confirmed identity values were used unchanged: `Identity.Name=CollisionEngineers.Pegasus`, `Identity.Publisher=CN=Collision Engineers`, and `PublisherDisplayName=Collision Engineers`. The dev certificate reports subject `CN=Collision Engineers` and thumbprint `AC3468D9C8D1FF64FAE3980F93A0E92CC0BA3AED`.

The previously diagnosed standalone WinApp CLI failure was repaired in the owned branch by adding a pinned `winapp.yaml` for the already-restored Windows App SDK/Windows SDK package set and ignoring the generated `.winapp/` staging directory. `winapp restore . --config-dir .` completed successfully. This is repository-local packaging setup; no upstream, cloud, deployment, or certificate-store write was performed.

Validation on branch `task/desktop-dev-msix`:

- `pwsh -NoProfile -File ./.codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj /p:Configuration=Release -SkipRun`: passed, x64 Release, 0 warnings/errors.
- `winapp cert info ./devcert.pfx`: passed; subject `CN=Collision Engineers`, thumbprint `AC3468D9C8D1FF64FAE3980F93A0E92CC0BA3AED`, private key present.
- `winapp package ./src/Pegasus.Desktop/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64 --cert ./devcert.pfx --self-contained`: passed; signed package `CollisionEngineers.Pegasus_0.1.0.0_x64.msix` produced, 94,569,334 bytes.
- Archive inspection: `AppxManifest.xml` and `resources.pri` present; manifest identity `CollisionEngineers.Pegasus`, publisher `CN=Collision Engineers`, version `0.1.0.0`, architecture `x64`; signer subject/thumbprint match the certificate. `Get-AuthenticodeSignature` reports `UnknownError` because this development certificate is not yet trusted locally; `winapp` itself reports the package signed.
- `git diff --check`: passed before commit. Commit `a8c4abf9` pushed to `origin/task/desktop-dev-msix`.

This repair clears the missing local runtime-package configuration, not the operator gates. Certificate trust, clean Windows 11 install/launch/uninstall, result log, screenshot, post-uninstall package-family/DPAPI cleanup readback, and no-elevation confirmation remain outstanding. No merge or Done claim is made.

## Independent review — 2026-08-30

An independent `codex review --commit a8c4abf9` session reviewed the changed `.gitignore` and `winapp.yaml` files without modifying the repository. The review found no actionable regressions: the WinApp CLI configuration restores successfully and pins the Windows App SDK runtime required by self-contained MSIX packaging. The reviewer did not treat the package as fully accepted; certificate trust, clean Windows 11 install/launch/uninstall, result log, screenshot, cleanup readback, no-elevation confirmation, PR CI, merge, proof, and Kanmer closeout remain separate requirements.

## Prerequisite merge — 2026-08-30

PR #53 (`a8c4abf97be7dbcbd0be51dd662322c7c7a90d3f`) merged into `dev` as `3454afe1f7b0249ed505a20d47fd392b22c7bb6d` after exact-head CI run `33289309561` completed successfully and the independent review found no actionable regressions. This merge carries only the pinned local WinApp runtime configuration and generated-staging ignore rule. It is a prerequisite merge, not FND-039 delivery acceptance; the operator certificate-trust and clean-machine install/launch/uninstall evidence remain required before proof and Done. No `dev` to `main` promotion was performed.
