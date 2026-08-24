# Files — REL-003

Surveyed on 2026-08-24 against the fork at branch `task/desktop-plan-segmentation`.
`eng/` does **not** exist yet (`ls eng` returns nothing); every path under
`eng/packaging/` below is created by this ticket unless another ticket is named.

## Where the change lands

| Path | Why |
|---|---|
| `eng/packaging/Pegasus.appinstaller.template.xml` | **New.** The 2021-schema template, copied verbatim from `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Template including its three comment blocks. Breaks if reformatted: the `UpdateBlocksActivation` requires-`ShowPrompt` rule exists only in that comment at the point of use. |
| `eng/packaging/New-AppInstaller.ps1` | **New.** Substitutes `<feed>`, `<channel>`, `<appinstaller-version>`, `<ver>` and `<publisher-subject-from-signing-certificate>` from `desktop-release-manifest.json` and writes `Pegasus.appinstaller` for the requested channel. Under D-003 the `<feed>/<channel>` substitution produces `\\<host>\<share>\<channel>`; there is no HTTP variant to support. Breaks if the substitution leaves a `<…>` placeholder behind — check 2 must then catch it. |
| `eng/packaging/Test-AppInstaller.ps1` | **New.** The validator: `-AppInstallerPath`, `-Channel`, `-ManifestPath`, `-PreviousAppInstallerPath`, `-Rollback`. Eight checks, eight named failures, non-zero exit on any. Two consumers gate on its exit code, so a check that warns instead of failing is a silent regression. |
| `eng/packaging/fixtures/appinstaller/` | **New.** Ten fixture files: `schema-2017.xml`, `wrong-uri.xml`, `version-not-monotonic.xml`, `publisher-mismatch.xml`, `hash-mismatch.xml`, `missing-showprompt.xml`, `unexpected-dependencies.xml`, `downgrade-without-rollback.xml`, `valid-pilot.xml`, `valid-prod.xml`, plus the paired fixture manifests each check compares against. |
| `eng/packaging/Test-TestAppInstaller.ps1` | **New.** Regression test of the validator: each fixture produces exactly its named failure; both valid fixtures pass; exit non-zero if any expectation is unmet. This is the file CI runs (`DSK-09-05`, board `REL-005`, step 8). |

## Context files

Read these before writing anything. Each one carries a constraint that is not obvious
from the paths above.

| Path | What it tells the implementer |
|---|---|
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` | **The whole ticket in one page.** § Template is the literal XML to copy; § Validator outline is the eight checks in order with their exact failure conditions; § Per-channel variants says both channels point at the **same package identity** so a ring change is a reinstall; § Known behaviours records that the launch check **fails open**, that a downgrade needs `ForceUpdateFromAnyVersion`, and that `ms-appinstaller:` links have been dead since December 2023. Do not re-derive any of it. |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Hosting requirements | The "Decided host (D-003, 2026-08-23)" paragraph is the one that changes this ticket's shape: over SMB the MIME / `Content-Length` / byte-range rows do **not** apply. The `Uri` values read `\\<host>\<share>\<channel>\Pegasus.appinstaller` and `\\<host>\<share>\<channel>\Pegasus_<ver>_x64.msix`. Building header checks here is out of scope and unrunnable. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 7 | The trap list this validator exists to enforce, including the two that are easy to get backwards: 2017/2 silently ignores the enforcing attributes, and `UpdateBlocksActivation` needs `ShowPrompt`. Also records that `HoursBetweenUpdateChecks="0"` checks every launch and costs launch time on a slow network. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 3 "Desktop release manifest" | The thirteen fields the validator reads back — in particular `version`, `packageSha256`, `signerSubject`, `channel` and `appInstallerVersion`, which are checks 4, 5 and 2's inputs. |
| `scripts/Test-CiChangeFlags.ps1` | The repository's script-test shape, in 30 lines: `Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`, a local `Assert-Flags` helper that `throw`s a message beginning with the case name, then a flat list of cases. Copy this shape for `Test-TestAppInstaller.ps1`; do not introduce Pester. |
| `scripts/Test-PegasusPlatform.ps1:1-9` | Confirms the same header convention and shows the repository's habit of `throw`ing a sentence that names the precondition (`'Test-PegasusPlatform.ps1 must run on Windows because…'`). Failure messages are sentences, not codes. |
| `scripts/Get-CiChangeFlags.ps1:11` | `$buildPattern` — the CI change classifier. It does **not** match `^eng/`, so a change to these files triggers **no** CI lane today. `DSK-09-05` (board `REL-005`) step 3 resolves that; this ticket must not edit the classifier, but must know its tests do not run in CI until that ticket lands. |
| `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md` § D-002 | Why `MainPackage/@Publisher` must equal the certificate subject **exactly — same fields, same order, same spacing and case** — or packaging fails with `0x8007000B`. Check 4 is the build-time guard against that runtime failure. |
| `.codex/skills/winui-packaging/SKILL.md` | The Key Rules table: "Publisher must match between certificate and manifest `Identity.Publisher`"; `--timestamp` is critical for production; `--self-contained` bundles the Windows App SDK runtime, which is why the template carries **no** `Dependencies` element (check 7). |
| `docs/desktop/08-testing/test-uat-stack.md:84` | `Publish-Feed` — the Test/UAT stack verb that "copies a freshly packaged `.msix` and the `.appinstaller` for the `teststack` channel into the feed folder, bumping the version". It is the mechanism the packaging tests use to simulate mandatory updates and rollbacks, and it is why check 3's monotonic compare must be real. |

## Ripple effects

- **`DSK-09-04` (board `REL-004`)** calls `New-AppInstaller.ps1` and then
  `Test-AppInstaller.ps1` inside `scripts/Build-DesktopRelease.ps1` and throws on a
  non-zero validator exit. The parameter names fixed here are that script's call site.
- **`DSK-09-05` (board `REL-005`)** runs `pwsh ./eng/packaging/Test-TestAppInstaller.ps1`
  as a step of the single `desktop-package` CI lane. The file name fixed here is the one
  in the workflow.
- **`DSK-08-10` (board `TEST-010`)**, `eng/packaging/Test-Package.ps1`, is the packaging
  scenario suite that proves App Installer's real behaviour; it lives in the same folder
  and shares the fixture directory's conventions.
- **`DSK-09-10` (board `REL-008`)** consumes the `Uri` shape decided here: the feed
  layout must serve exactly the paths the substitution produces, and
  `eng/packaging/Test-FeedShare.ps1` re-reads the published `Uri` with `Select-Xml`.
- **`DSK-09-13` (board `REL-011`)** depends on check 8: `-Rollback` is the only way a
  lower `MainPackage/@Version` passes, and its runbook R4 test calls the validator both
  with and without the switch.
- **Documentation.** If the implementation deviates from
  `appinstaller-template.md` — a different parameter name, an extra check, a different
  fixture set — that file is updated in the same task, per the ticket's
  `## Documentation changes`.
- **No OpenAPI or generated-client ripple.** This ticket adds no endpoint and no
  contract: `openapi/pegasus-v1.json` and the generated client are untouched, and
  `dotnet restore ./Pegasus.slnx --locked-mode` is unaffected because no package
  reference changes.

## Out of scope

Recorded so the reviewer sees these were decisions, not oversights. Each is a Guardrail
in the ticket body.

- **HTTP hosting checks** — MIME types, `Content-Length`, byte ranges. HTTP-only concerns
  that do not apply over SMB (D-003). No header check is added to this validator.
- **`src/`, `infra/`, and the gateway release scripts.** `eng/packaging/**` only. In
  particular `scripts/Build-ReleaseArtifacts.ps1` is not touched.
- **`scripts/Get-CiChangeFlags.ps1`.** The change-flag contract is owned by `DSK-08-13`
  (board `TEST-013`) and consumed by `DSK-09-05` (board `REL-005`); this ticket adds no
  pattern.
- **The `Publisher` string itself.** Fixed by `DSK-09-08` (board `REL-007`) as the
  self-managed certificate subject; this ticket only compares the `.appinstaller` against
  the manifest's recorded value.
- **The feed's real UNC root.** Provisioned by `DSK-09-10` (board `REL-008`); fixtures use
  their own values.
- **Any `ms-appinstaller:` protocol link.** Permanently excluded — the protocol has been
  disabled by default since December 2023.
- **An `UpdateUris` fallback.** The template carries it commented out. Whether a second
  **UNC** path is accepted as a fallback is `DSK-09-10`'s question to answer from official
  documentation (the element is documented as "Web URI as a string"); this ticket does
  not guess, and check 6 does not require the element.
