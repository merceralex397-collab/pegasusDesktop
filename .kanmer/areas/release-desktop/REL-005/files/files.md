# Files — REL-005

Surveyed on 2026-08-24 against the fork at branch `task/desktop-plan-segmentation`.
Two of the three files below are edited only in the **fallback branch** the plan's step 3
selects; in the preferred branch this ticket touches `.github/workflows/ci.yml` alone.

## Where the change lands

| Path | Why |
|---|---|
| `.github/workflows/ci.yml` | **Edited — the only file this ticket certainly touches.** One `desktop-package` job added after `unit` (which ends at `:148`), with `needs: changes`, the `if:` clause step 3 selects, `runs-on: windows-latest`, `timeout-minutes: 30`, and a leading comment. Breaks if the trigger block or any existing job is touched: the nine existing jobs must report the same status as on `dev`, and `git diff` must show additions plus the `changes`-output reuse and nothing else. |
| `scripts/Get-CiChangeFlags.ps1` | **Edited only in the fallback branch.** If `DSK-08-13`'s (board `TEST-013`) `desktop` flag has **not** landed, extend `$buildPattern` at `:11` with `\|^eng/packaging/`. If it has landed, this file is not touched at all — adding a second pattern for paths that flag already classifies is a stop condition. |
| `scripts/Test-CiChangeFlags.ps1` | **Edited only in the fallback branch**, in the same commit as the pattern above: one positive case (`eng/packaging/Test-AppInstaller.ps1` → `Build $true`) and one negative case (a path under `eng/` that must not match, so the pattern is not over-broad). The `Assert-Flags` helper at `:9-21` already takes `-Build` and `-Infrastructure`; adding cases needs no signature change, whereas adding a **third output** would. |

## Context files

Read these before touching the workflow. Each carries a rule that is written down exactly
once, at the point of use.

| Path | What it tells the implementer |
|---|---|
| `.github/workflows/ci.yml:1-11` | The trigger block — `pull_request:` plus `push: branches: [main]`, and `permissions: contents: read`. **There is no tag trigger**, and this ticket must not add one: `DSK-09-17` (board `REL-015`) deliberately puts the tag lane in a separate workflow so a tag does not run all nine existing jobs. |
| `.github/workflows/ci.yml:12-53` | The `changes` job: `ubuntu-latest`, and the only place job-level `outputs` are declared — `build` at `:17`, `infrastructure` at `:18`, computed at `:33-53` from `./scripts/Get-CiChangeFlags.ps1`. Any new flag must be declared here, which is why `DSK-08-13` owns that edit. |
| `.github/workflows/ci.yml:131-148` | The `unit` job — the exact shape to copy for a gated Windows build lane: `needs: changes`, `if: needs.changes.outputs.build == 'true'`, `runs-on: windows-latest`, `timeout-minutes: 20`, `actions/checkout@v7`, `uses: ./.github/actions/dotnet-build`, then a `run: >` step. Its comment at `:145-147` states the rule that costs people green builds: pwsh reports only the last command's exit code, so chained `dotnet test` calls need `&&` on one line. |
| `.github/workflows/ci.yml:179` | `uses: actions/upload-artifact@v6` — the version already in the workflow. Match it; a second major version of the same action in one file is avoidable churn. |
| `.github/workflows/ci.yml:185-205` | `sql-integration-coverage` runs on `ubuntu-latest` and consumes the shard artifacts. It is one of the two non-Windows jobs and must stay green and unmodified. |
| `.github/actions/dotnet-build/action.yml` | The composite action: `actions/setup-dotnet@v6` pinned to `10.0.x`, `cache: true` keyed on `global.json` + `src/**/packages.lock.json` + `tests/**/packages.lock.json`, then `dotnet restore ./Pegasus.slnx --locked-mode` and `dotnet build ./Pegasus.slnx --configuration Release --no-restore`. Its description says sharing one definition "keeps the cache key from drifting between lanes" — so **do not re-pin the SDK inline** in the new job; that is exactly the drift this action prevents. |
| `scripts/Get-CiChangeFlags.ps1:11` | `$buildPattern`. Read it before deciding anything: it matches `^(src|tests)/`, project and props files, lock files, `global.json`, four named `scripts/*.ps1` files, `^\.github/workflows/ci\.yml$` and `^\.github/actions/` — and **not** `^eng/`. So `eng/packaging/**` changes trigger no lane today. |
| `scripts/Test-CiChangeFlags.ps1:9-30` | The classifier's test: an `Assert-Flags` helper with mandatory `[bool] $Build` and `[bool] $Infrastructure`, then eight flat cases. Adding a **third** output means changing the helper signature and all eight calls — a concrete reason to prefer gating on `DSK-08-13`'s flag over adding one here. |
| `.codex/skills/winui-packaging/SKILL.md` | § End-to-End Workflow step 3 states plainly that `winapp cert install` "Adds cert to machine Trusted Root store. Persists across reboots" — which is why it must **not** run on a shared runner. § CI/CD gives the sample this lane follows: `microsoft/setup-WinAppCli@v0.1`, `winapp cert generate --if-exists skip --quiet`, `winapp package … --quiet`. Step 2 explains that `--manifest` auto-matches the certificate subject to `Package.appxmanifest`'s `Publisher`, which is the fix for the `0x8007000B` Publisher-mismatch failure. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 7 | "CI runner has **no production certificate**: signing only in the protected tag job or the release terminal; PR builds use the dev cert." Also the reminder that the Linux publish of Web and Worker must stay green. |
| `docs/desktop/README.md` § Constraints recorded after planning began | Constraint C-01(b): private-repository Windows runners bill at a **2× multiplier** against a monthly allowance, and this repository already runs seven of nine jobs on `windows-latest`. That is why the lane must be change-gated and time-boxed, and why a duplicate packaging lane bills twice. The cost plan is `DSK-08-19` (board `TEST-019`). |
| `docs/runbook.md:903` | "GitHub Actions/OIDC deployment is `Not planned`." The lane authenticates to nothing and must not become a deployment path. |

## Ripple effects

- **`DSK-08-13` (board `TEST-013`)** extends this **one** job with `desktop-ui-smoke` and
  `packaging-tests`, and owns the `desktop` change flag plus the `changes` job output that
  carries it. Its `desktop-ui-smoke` step downloads the MSIX by exactly the artifact name
  `desktop-msix-unsigned`, so that literal is a cross-ticket contract.
- **`DSK-02-15` (board `FND-040`)** owns the `desktop-build` lane. If it has landed, this
  job takes `needs: [changes, desktop-build]` and drops its own test step rather than
  paying twice for the same Windows minutes (C-01).
- **`DSK-09-03` (board `REL-003`)** supplies `eng/packaging/Test-TestAppInstaller.ps1`,
  which this lane runs; **`DSK-08-10` (board `TEST-010`)** supplies
  `eng/packaging/Test-Package.ps1`.
- **`DSK-09-16` (board `REL-014`)** adds the SBOM step to this same lane and uploads the
  SBOM alongside the MSIX in the same artifact — so leave the upload step easy to extend
  rather than pinning `path` to a single file pattern that excludes it.
- **`DSK-09-17` (board `REL-015`)** builds on the existence of this lane but lives in a
  **separate** workflow file; nothing this ticket adds may create a tag trigger.
- **`DSK-08-19` (board `TEST-019`)** consumes the measured runtime of this lane for the
  C-01 cost picture. Record the observed job duration in the ticket proof so that ticket
  has a number.
- **`scripts/Test-CiChangeFlags.ps1`** must be re-run after any classifier edit; it is
  itself matched by `$buildPattern` (`^scripts/(…|Get-CiChangeFlags)\.ps1$` covers the
  classifier, and `Test-CiChangeFlags.ps1` is matched by `$infrastructurePattern` at
  `:12`), so an edit there changes which lanes run on its own PR.
- **No OpenAPI or generated-client ripple.** No endpoint, no contract, no package
  reference; `openapi/pegasus-v1.json` and `dotnet restore ./Pegasus.slnx --locked-mode`
  are unaffected.

## Out of scope

Recorded so the reviewer sees these were decisions, not oversights. Each is a Guardrail in
the ticket body.

- **A second packaging lane.** Exactly one `desktop-package` job may exist in the whole
  workflow and this ticket owns it. `grep -c '^  desktop-package:' .github/workflows/ci.yml`
  must return `1`.
- **The `desktop-build` lane.** Owned by `DSK-02-15` (board `FND-040`). This ticket must
  not add or re-add it.
- **`desktop-ui-smoke` and `packaging-tests` lanes.** Owned by `DSK-08-13` (board
  `TEST-013`), which extends this one job with them.
- **A second change-flag pattern.** Once `DSK-08-13`'s `desktop` flag has landed this
  ticket touches neither classifier script. A second pattern covering the same paths, or
  one lane gated on two different `changes` outputs by two tickets, is a stop condition.
- **Any secret, PFX or production certificate.** `grep -n "secrets\." .github/workflows/ci.yml`
  must show no match introduced by this diff. Production signing is `DSK-09-17` (board
  `REL-015`) on the self-hosted signing host.
- **`winapp cert install` on the runner.** It writes to the machine Trusted Root store;
  packaging needs the certificate, not its trust.
- **A tag trigger on `ci.yml`.** Adding `push: tags:` here would run all nine existing
  jobs on every tag; `DSK-09-17` uses a separate workflow for that reason.
- **`infra/`, `azure.yaml`, any deployment path.** GitHub Actions Azure deployment is
  recorded as `Not planned` (`docs/runbook.md:903`).
- **`docs/engineering.md`.** CI cost and runner strategy are recorded by `DSK-08-19`
  (board `TEST-019`), not here.
