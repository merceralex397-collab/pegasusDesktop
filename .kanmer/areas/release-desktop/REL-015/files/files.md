# Files — REL-015

Surveyed on 2026-08-24 against the fork at branch `task/desktop-plan-segmentation`.
`.github/workflows/` contains exactly one file today, `ci.yml`; this ticket adds the second.

## Where the change lands

| Path | Why |
|---|---|
| `.github/workflows/desktop-release.yml` | **New**, and this ticket's main deliverable. `on: push: tags: ['desktop/v*']`, `permissions: contents: read`, one job on the self-hosted signing-host runner behind a repository `environment:` requiring a reviewer. A **new** file rather than a trigger added to `ci.yml`, because `ci.yml`'s block is `pull_request` + `push: branches: [main]` and adding `push: tags:` there would run all nine existing jobs on every tag. Breaks if it grows a `prod` path: an explicit guard must refuse `-Channel prod` so a future edit cannot quietly widen the lane. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` | **Edited, two changes.** R1 gains a note saying which of its steps this lane now performs automatically and which remain manual — otherwise a runbook step a lane already performs gets executed twice. R2 gains the plain statement that **production publication is never automated**; it stays the terminal step with `FEED PUBLISH GRANTED prod <ver>`. |
| `docs/engineering.md` § Branches and delivery | **Edited, coordinated.** The `desktop/v<M.m.b>` tag now triggers a lane. `DSK-00-09` (board `FND-009`) owns the tag convention itself ("Record the release-tag convention (`gateway/r<N>`, `desktop/v<M.m.b>`) in `docs/engineering.md` § Branches and delivery and the `pegasus-release` skill"), so coordinate rather than writing a competing paragraph — add only the sentence that the tag now triggers `desktop-release.yml`. |

## Context files

Read these before writing the workflow. Each carries a rule that decides the lane's shape.

| Path | What it tells the implementer |
|---|---|
| `.github/workflows/ci.yml:1-11` | The existing trigger block — `pull_request:` plus `push: branches: [main]` — and `permissions: contents: read`. **No tag trigger exists**, and adding one here is the mistake this ticket avoids. Copy the `permissions` line; do not widen it. |
| `.github/workflows/ci.yml:12-68` | The `changes` job, for reference only: the tag lane does **not** need path detection, because a tag is already a deliberate act. Do not import the `changes` dependency. |
| `.github/workflows/ci.yml:179` | `actions/upload-artifact@v6` — match the version already in the repository rather than introducing a second major version. |
| `.github/actions/dotnet-build/action.yml` | The composite action: pinned SDK `10.0.x`, NuGet cache keyed on lock files, `dotnet restore ./Pegasus.slnx --locked-mode`, `dotnet build … --no-restore`. Use it so the tag lane's cache key matches every other lane's; a second inline SDK pin is the drift it exists to prevent. |
| `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md` § D-002 | The chosen shape's third bullet decides this ticket's `runs-on`: the `.pfx` "stays on the signing host with an ACL limited to the publisher account. It is **not** stored as a GitHub secret: with the repositories going private (constraint C-01) the natural signing host is the same always-on machine that serves the share and hosts the self-hosted CI runner, so the key never leaves the estate." A hosted runner therefore cannot sign at all. |
| `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md` § How the decisions interact | "**One machine carries it all**" — share, runner and `.pfx` on one host — "the design's main operational risk … a single point of failure for publishing (not for running: installed clients keep working) and a single high-value target." The runner's isolation and permissions are `DSK-08-19`'s (board `TEST-019`) to specify and this lane's to respect. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 3 "Signing" and "Publication" | Signing happens only in a protected CI job (tag-triggered) or on the authorised release terminal, **never on a PR build**; timestamping is mandatory. Pilot publication may be automated once D-002 is decided; **production publish stays a runbook-controlled terminal step with explicit operator approval**, mirroring the `MERGE AUTH GRANTED` culture without extending that phrase. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R1 steps 3–6 | Exactly what this lane automates: sign with a timestamp and verify; generate and validate the `.appinstaller`; obtain the approval; publish package-first and manifest-last. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R2 | Exactly what it must **not** do. R2's preconditions — R1 completed for the same `<ver>`, and pilot users having run it "through the normal workflows for the agreed soak period" — are conditions no workflow can evaluate. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § Conventions | The approval phrase is confirmed by `DSK-09-11` (board `REL-009`) step 2, and `MERGE AUTH GRANTED` keeps its single meaning (the `dev` → `main` promotion). |
| `docs/desktop/00-governance-and-workflow/README.md:203-211` | The tag convention: `gateway/r<N>` and `desktop/v<M.m.b>` (= MSIX version), "CI builds an unsigned MSIX on every PR and builds + signs on `main` tags only; publishing to the production feed stays a runbook-controlled step … pilot-feed publishing to the decided UNC share (D-003) may be automated once D-002 settles how packages are signed." This ticket is the sentence's execution. |
| `docs/runbook.md:903` | "GitHub Actions/OIDC deployment is `Not planned`." This lane authenticates to no cloud service and must not become a deployment path. |
| `scripts/Build-DesktopRelease.ps1` (created by `DSK-09-04`, board `REL-004`) | The `-Sign` route and its parameter pairing: `-Sign` requires both `-CertificatePath` and `-TimestampUrl`, and the script fails immediately without them. The workflow is that script's caller. |
| `eng/packaging/Publish-DesktopRelease.ps1` (created by `DSK-09-10`, board `REL-008`) | The publish script: package first, `.appinstaller` last, never overwriting a published `.msix`. The lane calls it with `-Channel pilot` only. |
| `eng/packaging/Test-AppInstaller.ps1` (created by `DSK-09-03`, board `REL-003`) | The eight-check validator whose non-zero exit must abort the job **before** anything reaches the feed. |
| `.codex/skills/winui-packaging/SKILL.md` § Key Rules | `--timestamp` is "critical for production — without it, signatures expire with the cert"; the certificate subject must match `Identity.Publisher`. |

## Ripple effects

- **`DSK-09-05` (board `REL-005`)** owns the PR-time `desktop-package` lane in `ci.yml`. This
  ticket must not modify it, and must not duplicate its work beyond what signing requires —
  C-01's 2× Windows multiplier makes duplicated build minutes a real cost.
- **`DSK-08-19` (board `TEST-019`)** decides the self-hosted runner's isolation, permissions
  and labels. Take the labels from there rather than inventing them, and hand back this
  lane's measured duration.
- **`DSK-09-11` (board `REL-009`)** confirms the approval phrase; this lane enforces it
  alongside the environment approval.
- **`DSK-00-09` (board `FND-009`)** owns the tag convention in `docs/engineering.md`;
  coordinate the one added sentence rather than writing a competing paragraph.
- **`DSK-09-16` (board `REL-014`)** may decide the SBOM runs only on this tag lane rather than
  on every PR; leave the artifact upload step easy to extend.
- **`runbooks.md` R1 and R2** must both be updated in this task, or the next operator runs a
  step the lane already performed.
- **No OpenAPI, generated-client or build ripple.** No endpoint, no contract, no package
  reference; `dotnet restore ./Pegasus.slnx --locked-mode` is unaffected.

## Out of scope

Recorded so the reviewer sees these were decisions, not oversights. Each is a Guardrail in the
ticket body.

- **A tag trigger on `ci.yml`.** It would run all nine existing jobs on every tag. The lane
  lives in its own workflow.
- **Automating production publication.** R2 stays a runbook-controlled terminal step with
  `FEED PUBLISH GRANTED prod <ver>`. The lane carries an explicit guard refusing
  `-Channel prod`.
- **Signing on a hosted runner.** It would require the `.pfx` as a GitHub secret, which D-002
  forbids. If the self-hosted runner does not exist yet, land the workflow **disabled** with a
  comment rather than inventing a hosted-signing route.
- **Any `secrets.` reference to certificate material.**
  `grep -n "secrets\." .github/workflows/desktop-release.yml` must show no certificate or
  password secret.
- **Modifying the gateway release route, `scripts/Build-ReleaseArtifacts.ps1`, `azd` or
  `infra/`.** Untouched; GitHub Actions Azure deployment stays `Not planned`
  (`docs/runbook.md:903`).
- **Publishing an `ms-appinstaller:` protocol link.** The protocol has been disabled by
  default since December 2023.
- **Specifying the runner's isolation and permissions.** `DSK-08-19` (board `TEST-019`) owns
  that; this lane respects it.
