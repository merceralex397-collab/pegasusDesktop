# Files — REL-004

Surveyed on 2026-08-24 against the fork at branch `task/desktop-plan-segmentation`.
Paths under `eng/packaging/` are created by `DSK-09-02` (board `REL-002`) and
`DSK-09-03` (board `REL-003`); `src/Pegasus.Desktop/` is created by `DSK-02-05` (board
`FND-030`). `ls src/` today shows only `Pegasus.Core`, `Pegasus.Infrastructure`,
`Pegasus.Web`, `Pegasus.Worker`, and `ls eng` returns nothing.

## Where the change lands

| Path | Why |
|---|---|
| `scripts/Build-DesktopRelease.ps1` | **New**, and this ticket's whole deliverable. Placed in `scripts/` beside `Build-ReleaseArtifacts.ps1` per the area plan § 5 row, resolving the contradiction with § 4. Breaks if a guard is dropped: without the exact-HEAD and clean-tree checks a release artefact can be built from an uncommitted tree and its recorded `sourceCommit` becomes a lie. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 4 | **Edited**, one path corrected: `eng/packaging/Build-DesktopRelease.ps1` → `scripts/Build-DesktopRelease.ps1`. Required by the body's step 11 and its `## Documentation changes`, in the same task, so the plan set stops contradicting itself. |

## Context files

Read these before writing anything. Each carries a constraint or precedent the paths
above depend on.

| Path | What it tells the implementer |
|---|---|
| `scripts/Build-ReleaseArtifacts.ps1` | **The template for this script, read end to end.** `:1-9` parameter validation with an explanatory comment on the non-obvious parameter; `:16-19` exact-HEAD guard (`git rev-parse HEAD` must equal `-SourceRevision`); `:20-23` clean-tree guard (`git status --porcelain=v1 --untracked-files=all` must be empty) with the message `'Release artifacts require a clean exact source revision.'`; `:25-33` output-root escape guard using `[IO.Path]::GetFullPath` + `StartsWith` + delete-and-recreate; `:46-71` the per-step `if ($LASTEXITCODE -ne 0) { throw '…' }` habit; `:51-59` the build proving its own identity rather than asserting it; `:92-126` per-artifact `Get-FileHash` SHA-256 into an `[ordered]` manifest serialised `utf8NoBOM`; `:126` a single stdout result; `:128-130` `finally { Pop-Location }`. |
| `.codex/skills/winui-packaging/SKILL.md` | § Quick Reference and § Key Rules: `winapp package <dir> --cert <pfx>` is preferred over a separate `winapp sign`; `--timestamp` is **critical for production** because "without it, signatures expire with the cert"; `--self-contained` bundles the Windows App SDK runtime; the certificate subject must match `Identity.Publisher`. § CI/CD shows `--if-exists skip --quiet` for repeatable runs. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R1 | Steps 1–4 are the acceptance shape: clean checkout of the tagged commit → this script → sign with a timestamp and `signtool verify /pa /v` → generate and validate the `.appinstaller`. Steps 5–7 (approval, publish, feed verification) are deliberately **after** the script, which is why it must publish nothing. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 3 | The three subsections that bind this script: "Signing" (only in a protected CI job or on the authorised terminal, never on a PR build; timestamping mandatory; D-002's self-managed certificate confined to the signing host), "Self-contained" (no `Dependencies` element), "SBOM and vulnerability report" (produced per release; the generator is chosen by `DSK-09-16`). |
| `docs/desktop/09-release-update-and-distribution/README.md` § 7 | The traps this script must not fall into, including "signing only in the protected tag job or the release terminal", "certificate expiry without timestamping invalidates every installed package's signature path for new installs", and "Linux publish of Web/Worker must stay green". |
| `docs/adr/0007-direct-terminal-azure-deployment.md` | Why the release runs from an authorised Windows terminal at all, and why an unattended hosted build is not the default. |
| `.agents/skills/pegasus-release/SKILL.md` | The release-terminal conventions and the approval culture this script inherits: read-only Azure checks need no approval, every write needs explicit operator approval for the exact target, and `MERGE AUTH GRANTED` has exactly one meaning (the `dev` → `main` promotion) which must not be extended to publishing. |
| `.gitignore:20-21` | `**/artifacts/` and `/artifacts/` — the release root `artifacts/desktop-releases/<ver>/` is ignored, so nothing this script writes can be committed by accident. Verified with `git check-ignore -v artifacts/devcert.cer` → `.gitignore:21:/artifacts/`. |
| `Directory.Build.props` | 19 lines. `<Version>0.1.0-alpha.1</Version>` at `:9` is the **gateway's** identity — never passed to this script; `<Deterministic>true</Deterministic>` at `:6` is why the managed compile is reproducible even though the package container is not; `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` at `:8` means the desktop build fails on a warning, which is a real cause of a red release run. |
| `global.json` | SDK pinned to `10.0.302`, `rollForward: latestFeature`. The release terminal must satisfy it; a different SDK is a build failure, not a warning. |
| `eng/packaging/New-DesktopReleaseManifest.ps1` (created by `DSK-09-02`, board `REL-002`) | The thirteen-field manifest contract and its parameter names — this script's call site at step 9. |
| `eng/packaging/New-AppInstaller.ps1` and `eng/packaging/Test-AppInstaller.ps1` (created by `DSK-09-03`, board `REL-003`) | The `.appinstaller` generation and the eight-check validator whose **exit code** is this script's gate. The validator prints a pass/fail list and exits non-zero on any failure. |

## Ripple effects

- **Runbook R1 step 2** becomes a real command; `DSK-09-11` (board `REL-009`) runs it for
  the first pilot release and its proof quotes this script's output.
- **`DSK-09-16` (board `REL-014`)** extends this script: it fills the `-SbomPath`
  pass-through with a chosen, pinned generator, and replaces the inline vulnerability text
  check with `scripts/Test-DependencyVulnerabilities.ps1`. Add the parameter now so that
  extension is additive.
- **`DSK-09-17` (board `REL-015`)** invokes this script with `-Sign -CertificatePath
  <host path> -TimestampUrl <service>` from a self-hosted runner on the signing host; the
  parameter names fixed here are that workflow's call site.
- **`DSK-09-13` (board `REL-011`)** relies on the fact that this script **rebuilds
  nothing** during a rollback: R4 republishes the same signed `.msix`, so the hash written
  here must remain the hash of record.
- **`docs/desktop/09-release-update-and-distribution/README.md` § 4** is corrected by this
  ticket; anything else in the plan set that names `eng/packaging/Build-DesktopRelease.ps1`
  must be corrected with it — `grep -rn "eng/packaging/Build-DesktopRelease" docs/` before
  the PR.
- **No CI ripple from this ticket.** `scripts/Get-CiChangeFlags.ps1:11`'s `$buildPattern`
  matches `^(src|tests)/` and a named set of `scripts/*.ps1` files, but **not** this new
  script by name, so adding it changes no lane's trigger. `DSK-09-05` (board `REL-005`)
  owns the CI side.
- **No OpenAPI or generated-client ripple.** No endpoint, no contract, no package
  reference changes; `openapi/pegasus-v1.json` and `dotnet restore ./Pegasus.slnx
  --locked-mode` are unaffected.

## Out of scope

Recorded so the reviewer sees these were decisions, not oversights. Each is a Guardrail in
the ticket body.

- **Publishing.** The script writes to `artifacts/desktop-releases/<ver>/` and stops.
  Copying to the feed is `eng/packaging/Publish-DesktopRelease.ps1`, owned by `DSK-09-10`
  (board `REL-008`), and R1 puts an approval phrase between the two.
- **`scripts/Build-ReleaseArtifacts.ps1`, `infra/`, and every gateway release step.** The
  gateway keeps the existing `pegasus-release` procedure unchanged.
- **Any Azure call.** The desktop release path touches no Azure resource at all
  (D-002 + D-003). No Azure MCP tool is used.
- **Choosing an SBOM generator.** `DSK-09-16` (board `REL-014`) is the single owner of the
  generator choice, the vulnerability-gate contract and the suppression register. This
  ticket adds `-SbomPath` and invents nothing. This is a scope boundary, not an open
  question, and no `open-questions` document is created for it.
- **Signing on a PR build.** Never. Signing happens only with an explicit `-Sign` plus a
  certificate route, on the authorised terminal or in the protected tag job
  (`DSK-09-17`, board `REL-015`).
- **Asserting bit-for-bit reproducibility.** Step 12 records what was observed, including
  instability; it does not assert a property that was not measured.
