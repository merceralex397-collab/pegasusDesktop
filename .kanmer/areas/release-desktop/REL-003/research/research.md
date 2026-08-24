# Research — REL-003: the `.appinstaller` update contract and what a validator must refuse

## Question

What exactly must the two `.appinstaller` files (pilot and prod) contain for App Installer
to enforce a mandatory update, and which violations must
`eng/packaging/Test-AppInstaller.ps1` refuse before a release can be published?

## Current behaviour

**There is none.** The web application has no client to update: it is served from a
Container App and a browser reload is the update. Nothing in the repository mentions
MSIX, App Installer, `signtool` or a code-signing certificate — confirmed by reading
`.github/workflows/ci.yml` (234 lines, nine jobs, no publish/sign/deploy lane) and
`scripts/` (21 scripts; `Build-ReleaseArtifacts.ps1` publishes `linux-x64` Web and Worker
plus a `win-x64` EF migration bundle and knows nothing about packages). `eng/` does not
exist; `ls eng` returns nothing.

**No parity-matrix row covers this.** `docs/desktop/01-inventory-and-parity/parity-matrix.md`
runs `PAR-01`…`PAR-46`, keyed to Razor page models under `src/Pegasus.Web/Pages/`. The
closest rows are `PAR-45` (health and `GET /diagnostics/version`,
`src/Pegasus.Web/Program.cs:939-950`, `:954`) and `PAR-43` (web shell error pages);
neither is about distribution. Update and install are **new desktop responsibility**, not
parity work, so this ticket has no "current behaviour" to match — only a proposal § 9
design to implement.

## Findings

- The `.appinstaller` file is the update contract every installed workstation re-reads on
  every launch — `docs/desktop/09-release-update-and-distribution/appinstaller-template.md`
  § Template, and its rule "`Uri` must equal the exact URL (or UNC path) the file is
  served from; App Installer records it at install time and re-reads it on every check".
  - Consequence for this ticket: the `Uri` substitution in `New-AppInstaller.ps1` is the
    single most irreversible line in the file. Changing host, share or channel folder
    later forces a reinstall on every workstation.
- Only the **2021** namespace supports `ShowPrompt`, `UpdateBlocksActivation` and
  `HoursBetweenUpdateChecks`; Visual Studio emits the 2017/2 schema by default and
  **silently ignores** those settings — area plan § 2 (Official documentation, fetched
  2026-08-23), citing
  <https://learn.microsoft.com/windows/apps/package-and-deploy/distribution-feature-status>.
  - This is why validator check 1 exists and why it must reject *any* namespace other
    than 2021 rather than warn.
- `UpdateBlocksActivation` **requires** `ShowPrompt="true"` — area plan § 2 and § 7,
  citing <https://learn.microsoft.com/windows/msix/app-installer/update-settings>.
  A file with `UpdateBlocksActivation="true"` and `ShowPrompt="false"` is the
  configuration that looks enforcing and is not.
- `HoursBetweenUpdateChecks` is `0`–`255` with default `24`; `0` means check every launch
  — same source. Area plan § 7 records the accepted cost: acceptable for ten users, but
  watch the launch-time budget on a slow network.
- Downgrade requires `ForceUpdateFromAnyVersion`; without it App Installer only moves to
  higher package versions — `appinstaller-template.md` § Known behaviours. This is why
  validator check 8 (`-Rollback` mode) exists at all: rollback is the only case where the
  package version goes down while the manifest version goes up.
- The `ms-appinstaller:` protocol has been disabled by default since December 2023 —
  area plan § 7 and `appinstaller-template.md` § Known behaviours. Publish the file path,
  never a protocol link.
- The eight validator checks are already written out, in order, in
  `appinstaller-template.md` § Validator outline. They are not to be re-derived; they are
  to be implemented one function per check with one named failure each.
- The repository's script-test convention is a plain PowerShell script with a local
  `Assert-…` helper that `throw`s a case-named message, not a test framework —
  `scripts/Test-CiChangeFlags.ps1:9-21` (the `Assert-Flags` helper) followed by a flat
  list of `Assert-Flags -Case '…'` calls at `:23-30`. Every script opens with
  `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'`
  (`scripts/Test-PegasusPlatform.ps1:4-5`, `scripts/Get-CiChangeFlags.ps1:8-9`).
- PowerShell's `[version]` cast gives a correct four-part numeric compare; a string
  compare puts `1.0.10.0` below `1.0.9.0`. The body's step 6 requires the cast on both
  sides.

### Facts

Verified by reading this repository on 2026-08-24 unless a URL and fetch date is given.

| Fact | Source |
| --- | --- |
| No packaging, signing or App Installer artefact exists in the repository | `ls eng` → nothing; `.github/workflows/ci.yml` (234 lines, jobs `changes`, `documentation`, `local-development-scripts`, `reference-data`, `infrastructure`, `unit`, `sql-integration` ×3, `sql-integration-coverage`, `browser`); `ls scripts/` |
| Script conventions: strict mode, stop on error, throw-with-case-name, non-zero exit | `scripts/Test-CiChangeFlags.ps1:1-30`, `scripts/Test-PegasusPlatform.ps1:1-9` |
| The verbatim 2021-schema template this ticket must copy | `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Template |
| The eight validator checks, in order, with their failure conditions | same file, § Validator outline |
| Per-channel `Uri` shape under D-003: `\\<host>\<share>\<channel>\Pegasus.appinstaller` | same file, § Hosting requirements, "Decided host (D-003, 2026-08-23)" |
| The thirteen manifest fields the validator compares against | `docs/desktop/09-release-update-and-distribution/README.md` § 3 "Desktop release manifest"; implemented by `DSK-09-02` (board `REL-002`) |
| 2021 namespace is required for `ShowPrompt` / `UpdateBlocksActivation` / `HoursBetweenUpdateChecks`; VS emits 2017/2 by default; `ms-appinstaller:` disabled since Dec 2023 | area plan § 2, citing <https://learn.microsoft.com/windows/apps/package-and-deploy/distribution-feature-status> (fetched 2026-08-23) |
| `OnLaunch` attributes and `UpdateBlocksActivation` requires `ShowPrompt` | area plan § 2, citing <https://learn.microsoft.com/windows/msix/app-installer/update-settings> (fetched 2026-08-23) |
| App Installer supports **https, http and smb** for downloads and updates | `signing-and-hosting-decision-matrix.md` § D-003, quoting <https://learn.microsoft.com/windows/msix/app-installer/app-installer-file-overview> (fetched 2026-08-23) |
| MIME types, `Content-Length` and byte ranges are HTTP-only and do **not** apply over SMB | `appinstaller-template.md` § Hosting requirements; area plan § 2 |

### Assumptions

- **A-09-1 — the packaging build produces `Pegasus_<ver>_x64.msix` at a path the
  validator can hash at build time.** Check 5 compares `MainPackage/@Uri`'s target against
  the manifest hash, and at build time that target is a local file, not a UNC path.
  *Confirmed by*: running `scripts/Build-DesktopRelease.ps1` (`DSK-09-04`, board
  `REL-004`) once and seeing the `.msix` beside the generated `.appinstaller`.
  *Breaks if wrong*: check 5 has to resolve a UNC path during a build that has no feed,
  and the validator becomes unrunnable in CI. Mitigation already in the body: the
  validator takes `-ManifestPath` and resolves `MainPackage/@Uri` **relative to the
  `.appinstaller`'s own directory** when the file is local.
- **A-09-2 — the previous published `.appinstaller` is available to compare against.**
  Check 3 needs the last published version for the channel.
  *Confirmed by*: the body's `-PreviousAppInstallerPath` parameter being satisfiable from
  the feed (read-only) or from the release record.
  *Breaks if wrong*: check 3 cannot run on a first-ever publish. The correct behaviour on
  a first publish is to **pass with a recorded note**, not to fail — a channel with no
  previous file is the base case, not a violation.
- **A-09-3 — `Select-Xml` with a namespace prefix is the reading mechanism.** The
  document is namespaced, so bare XPath (`/AppInstaller`) matches nothing and would make
  every check silently pass on an empty node set.
  *Confirmed by*: `Select-Xml -Namespace @{ ai = 'http://schemas.microsoft.com/appx/appinstaller/2021' } -XPath '/ai:AppInstaller'` returning a node for `valid-pilot.xml`.
  *Breaks if wrong*: the validator reports "pass" on files it never read — the single
  worst failure mode for this ticket.
- **A-09-4 — the Publisher subject string is not yet fixed.** It is the subject of the
  self-managed certificate (D-002), fixed by `DSK-09-08` (board `REL-007`) and used as a
  stable placeholder CN by `DSK-02-05` (board `FND-030`).
  *Confirmed by*: reading `Identity/@Publisher` from `src/Pegasus.Desktop/Package.appxmanifest`
  once that project exists.
  *Breaks if wrong*: the fixtures encode a subject that never matches reality; check 4
  then passes in tests and fails in production. Mitigation: fixtures must take the subject
  from their paired fixture **manifest**, never from a literal in the validator.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered for the responsibility
this ticket places: *deciding, at launch, whether the installed client may run* (the
package layer), and *validating a release artefact at build time*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | The `.appinstaller` is written by one publisher and read by every workstation; no user updates it. ACLs in `runbooks.md` § R9 step 3 give the staff group read+execute only. |
| Unattended execution — must it run with every desktop closed? | **no** | `OnLaunch` runs in the launching user's session; `AutomaticBackgroundTask` runs on the workstation while it is on. Neither needs a server process. (The *share's* availability does — that is `DSK-09-10`, board `REL-008`, not this ticket.) |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | The file carries no secret. SMB read uses the staff member's own Windows identity; the signing `.pfx` never leaves the signing host (D-002) and never appears in this file — only the certificate **subject** does. |
| Public callback — must an external service call a stable public endpoint? | **no** | The client polls the share; nothing calls in. This is precisely why SMB survives constraint C-01 while GitHub Releases and Pages do not. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **no** | This layer explicitly **fails open**: when the feed is unreachable App Installer launches the app (`appinstaller-template.md` § Known behaviours). The fail-closed layer is the gateway minimum-version gate, owned by `DSK-04-06` (board `GWY-023`) — not by this ticket. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement exists and none is claimed. A central validation service for ten workstations would add a deployment unit to run a script that takes milliseconds locally. |

All six "no" → the package-side update decision belongs on the workstation and the
validator belongs in the build. That is the two-layer design of proposal § 9.1, and it is
why this ticket must **not** try to make the package layer enforcing on its own.

## Implications

- **The template is copied verbatim, not composed.** The body's step 3 quotes the exact
  document, comments included. Re-typing it risks losing `ForceUpdateFromAnyVersion` or
  the `UpdateBlocksActivation`-requires-`ShowPrompt` comment, which is the only place the
  rule is written down at the point of use.
- **Eight checks, eight named failures, one exit code.** CI (`DSK-09-05`, board `REL-005`)
  and `scripts/Build-DesktopRelease.ps1` (`DSK-09-04`, board `REL-004`) both gate on the
  exit code, so a check that prints a warning and returns 0 is a no-op.
- **The namespace-aware read is a correctness prerequisite, not a detail** (A-09-3). Write
  the namespace-prefixed XPath first, and let the `schema-2017.xml` fixture prove the
  reader is actually reading.
- **No HTTP header checks.** D-003 is decided; MIME, `Content-Length` and byte ranges do
  not exist over SMB. The body's "Not this ticket" guardrail is a scope boundary, and
  adding header checks would make the validator unrunnable against the real feed.
- **Two channels, one template.** The per-channel difference is only the `Uri` and the
  independent `.appinstaller` `Version` counter, so a single template plus a substitution
  script is right, and two hand-maintained templates would drift.
- **First-publish base case.** Check 3 must define its behaviour when no previous file
  exists (A-09-2), or the first pilot release cannot be validated.

## Open questions

- None that block. The two decisions this ticket depends on are settled: D-002 fixes the
  signer subject as the `Publisher` (this ticket only compares against it, and takes the
  value from the manifest, never from a literal), and D-003 fixes the `Uri` form as a UNC
  path. The concrete Publisher string and the concrete UNC root are **inputs supplied by**
  `DSK-09-08` (board `REL-007`) and `DSK-09-10` (board `REL-008`); until they land, the
  fixtures use their own paired fixture manifests, so the validator is fully testable
  without them. No `open-questions` document is created.
