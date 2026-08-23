# 09 · App Installer file template and feed requirements

The `.appinstaller` file is the update contract between every workstation
and the feed. This page holds the template, its per-channel variants, the
validator outline, the hosting requirements, and the behaviours the
implementing agent must not be surprised by. Sources (fetched 2026-08-23):
[update settings](https://learn.microsoft.com/windows/msix/app-installer/update-settings),
[distribution feature status](https://learn.microsoft.com/windows/apps/package-and-deploy/distribution-feature-status),
[auto-update and repair](https://learn.microsoft.com/windows/msix/app-installer/auto-update-and-repair--overview),
[create an App Installer file manually](https://learn.microsoft.com/windows/msix/app-installer/how-to-create-appinstaller-file),
[MSIX troubleshooting guide](https://learn.microsoft.com/windows/msix/msix-troubleshooting-guide).

## Template (2021 schema)

Placeholders in angle brackets are filled by `Build-DesktopRelease.ps1`
from the desktop release manifest. Do not hand-edit published files.

```xml
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller
    xmlns="http://schemas.microsoft.com/appx/appinstaller/2021"
    Uri="<feed>/<channel>/Pegasus.appinstaller"
    Version="<appinstaller-version>">

  <MainPackage
      Name="CollisionEngineers.Pegasus"
      Publisher="<publisher-subject-from-signing-certificate>"
      Version="<ver>"
      ProcessorArchitecture="x64"
      Uri="<feed>/<channel>/Pegasus_<ver>_x64.msix" />

  <!-- No <Dependencies>: the package is self-contained for .NET and the
       Windows App SDK (proposal §7.1). If a framework-dependent package is
       ever chosen, add the Microsoft.WindowsAppRuntime.<major> dependency
       here and host its MSIX alongside. -->

  <UpdateSettings>
    <!-- Check on every launch, show the prompt, and block activation until
         the update is taken. UpdateBlocksActivation requires ShowPrompt. -->
    <OnLaunch HoursBetweenUpdateChecks="0"
              ShowPrompt="true"
              UpdateBlocksActivation="true" />
    <!-- Also check roughly every 8 hours in the background (no UI). -->
    <AutomaticBackgroundTask />
    <!-- Allow the feed to move a workstation to a LOWER version (rollback). -->
    <ForceUpdateFromAnyVersion>true</ForceUpdateFromAnyVersion>
  </UpdateSettings>

  <!-- Optional fallbacks (max 10) used only if the Uri above is unreachable.
       Use a second host or path only if D-003 provides one. -->
  <!--
  <UpdateUris>
    <UpdateUri><feed-fallback>/<channel>/Pegasus.appinstaller</UpdateUri>
  </UpdateUris>
  -->
</AppInstaller>
```

Rules the template encodes:

- `Uri` must equal the exact URL (or UNC path) the file is served from;
  App Installer records it at install time and re-reads it on every check.
- `Version` of the `.appinstaller` must increase on every publish, including
  a rollback publish; it is independent of the package version.
- `MainPackage Version` is the package version `1.<minor>.<build>.0`.
- `Publisher` must match the certificate subject used to sign the `.msix`
  (D-002); a mismatch fails installation.
- Only the 2021 namespace supports `ShowPrompt`, `UpdateBlocksActivation`
  and `HoursBetweenUpdateChecks`; Windows 11 supports it on every build.

## Per-channel variants

| Channel | `Uri` | Package file name | `.appinstaller` `Version` series | Who |
| --- | --- | --- | --- | --- |
| pilot | `<feed>/pilot/Pegasus.appinstaller` | `Pegasus_<ver>_x64.msix` (same file as prod later) | independent counter, starts at `1.0.0.0` | 1–2 internal users |
| prod | `<feed>/prod/Pegasus.appinstaller` | same `.msix` that pilot proved | independent counter | everyone else |

Both channels point at the **same package identity**, so a user moves ring
by reinstalling from the other `.appinstaller` (runbook R7). The pilot
`.appinstaller` may advertise a newer `MainPackage Version` than prod.

## Validator outline — `eng/packaging/Test-AppInstaller.ps1`

Inputs: path to the generated `.appinstaller`, the channel, the desktop
release manifest, the last published `.appinstaller` for that channel
(downloaded read-only from the feed or from the release record).

Checks (each a named failure):

1. Namespace is `http://schemas.microsoft.com/appx/appinstaller/2021`.
2. `Uri` equals `<feed>/<channel>/Pegasus.appinstaller` for the channel.
3. `Version` is strictly greater than the last published version for the
   channel (four-part numeric compare).
4. `MainPackage/@Name` is `CollisionEngineers.Pegasus`;
   `@ProcessorArchitecture` is `x64`; `@Version` equals the manifest
   version; `@Publisher` equals the signer subject recorded in the manifest.
5. `MainPackage/@Uri` resolves to a file whose SHA-256 equals the manifest
   hash (local file at build time; HTTP `HEAD` + ranged `GET` at publish
   time is runbook R9's job).
6. `UpdateSettings/OnLaunch` has `HoursBetweenUpdateChecks="0"`,
   `ShowPrompt="true"`, `UpdateBlocksActivation="true"`;
   `ForceUpdateFromAnyVersion` is `true`; `AutomaticBackgroundTask` present.
7. No `Dependencies` element unless the manifest says framework-dependent.
8. (Rollback mode) `MainPackage/@Version` lower than the previous is allowed
   only when the invocation passes `-Rollback` and `ForceUpdateFromAnyVersion`
   is `true`.

Output: pass/fail list; exit code non-zero on any failure; used by CI
(DSK-09-05) and by `Build-DesktopRelease.ps1` (DSK-09-04). Tests for the
validator live with the script (fixture files for each failure).

## Hosting requirements

| Requirement | Value | Why |
| --- | --- | --- |
| Transport | HTTPS (or UNC file share); no authentication | App Installer does not authenticate; TLS protects integrity in transit, the signature protects the package |
**Decided host (D-003, 2026-08-23): a UNC share.** Over SMB none of the
HTTP requirements below apply; they are kept only in case a future decision
moves the feed to an HTTP host. For the share the requirements are: a
permanently stable UNC path (DFS namespace or CNAME'd host, never a machine
name that may be replaced, never a mapped drive letter), read and execute
for the staff group, write for the publisher only, and a `Uri` attribute
byte-identical to the path clients installed from. The `Uri` values then
read `\\<host>\<share>\<channel>\Pegasus.appinstaller` and
`\\<host>\<share>\<channel>\Pegasus_<ver>_x64.msix`.

| MIME `.msix` | `application/msix` | HTTP hosts only; App Installer rejects or misroutes wrong types |
| MIME `.appinstaller` | `application/appinstaller` | HTTP hosts only |
| MIME manifest/SBOM | `application/json` | HTTP hosts only; tooling |
| `Content-Length` | present on every `GET`/`HEAD` | HTTP hosts only; required by App Installer |
| Byte ranges | HTTP/1.1 range requests honoured (`206`) | HTTP hosts only; App Installer streams packages |
| Cache-Control | short on `.appinstaller` (≈60 s); long on `.msix` | updates are detected promptly; packages are immutable per version |
| Retention | current + previous package per channel, never overwrite | rollback (R4) |
| Path stability | `<feed>/<channel>/Pegasus.appinstaller` never changes | the `Uri` is baked into every installation |
| Availability | best effort, no SLA needed for ten users; fallback `UpdateUri` optional | fail-open check; gateway gate fails closed |

D-003 decides which host provides these; the checks are identical for all
options (runbook R9 step 4).

## Known behaviours

- **Protocol link disabled**: `ms-appinstaller:?source=` does nothing on
  most devices since December 2023 — publish the `.appinstaller` file URL.
- **Prompt and blocking**: with `ShowPrompt="true"` and
  `UpdateBlocksActivation="true"`, the user sees an update dialog on launch
  and can only update or close; with `UpdateBlocksActivation="false"` the
  user may start the old version and the update is applied later.
- **Fail-open**: if the feed is unreachable the check is skipped and the app
  launches; the gateway minimum-version gate (area 04) is the fail-closed
  layer.
- **Downgrade** needs `ForceUpdateFromAnyVersion`; without it App Installer
  only moves to higher versions.
- **Settings precedence**: CSP (Intune) settings override PowerShell and
  `.appinstaller` settings, which override an embedded `.appinstaller`;
  check `Get-AppxPackageAutoUpdateSettings` when an update does not apply.
- **In-app check**: `Package.CheckUpdateAvailabilityAsync` works only for
  packages installed through an `.appinstaller`; call it on the package from
  `PackageManager.FindPackageForUser`, not `Package.Current` (known
  access-denied issue); `Required` means the `.appinstaller` policy blocks
  activation.
- **Version bump needed**: Windows requires a higher package version to
  update an installed package; the CI run number guarantees it.
- **SmartScreen** may warn on first download of a new package hash; this
  is reputation, not a signature error (distribution feature status page).
