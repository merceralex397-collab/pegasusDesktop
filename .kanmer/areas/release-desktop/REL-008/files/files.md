# Files — REL-008

Surveyed on 2026-08-24 against the fork at branch `task/desktop-plan-segmentation`.
`eng/` does not exist yet (`ls eng` returns nothing); `eng/packaging/` is created by
whichever of `DSK-09-02` (board `REL-002`), `DSK-09-03` (board `REL-003`) or `DSK-09-06`
(board `REL-006`) lands first. Much of this ticket is **operator configuration of a
Windows host**, which produces evidence rather than diff.

## Where the change lands

| Path | Why |
|---|---|
| `eng/packaging/Publish-DesktopRelease.ps1` | **New.** Implements `runbooks.md` § R9 steps 1–2 verbatim and in order: `robocopy <staging> \\<host>\<share>\<channel> Pegasus_<ver>_x64.msix /Z /R:2 /W:5`, then `desktop-release-manifest.json`, then `Pegasus.appinstaller` **last**. Breaks if the order is reversed: a client reading the manifest mid-publish finds a package that is not there yet. Must refuse to overwrite an existing `.msix` — a new version always means a new file name — while replacing `Pegasus.appinstaller` in place. |
| `eng/packaging/Test-FeedShare.ps1` | **New.** Implements R9 step 4 exactly, and is run **as an ordinary staff user from a non-publisher workstation**: `Test-Path` on the channel path; `Select-Xml -XPath /*` for the expected `Version` and `Uri`; `Get-FileHash` against `desktop-release-manifest.json`'s `packageSha256`; `Get-Acl` proving the staff group has **no** write permission. Exit non-zero on any failure. Breaks if run as the publisher: the ACL check then proves nothing. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R9 | **Edited.** Records the agreed UNC root, the ACL group and publisher account names, the backup job and last successful run, and the **resolved** `UpdateUris`-over-UNC answer at step 6 — as a stated fact, including "the feed has no fallback" if that is the answer. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 5 row `DSK-09-10` | **Edited.** The row's acceptance text predates D-003 and still names `allowBlobPublicAccess`, RBAC and MIME/Range verification — wording that belongs to the withdrawn Azure blob options. Correct it in the same task, per the ticket's "Stale row wording" guardrail. |
| `docs/desktop/11-azure-disposition/README.md` | **Edited.** Mirror the statement that this area's ⚠ Azure writes are withdrawn and the feed needs none. |

**Operator artefacts that are not repository files** and must be captured in the ticket
proof: the agreed UNC root (step 3); the `Get-ChildItem` listing of the two channel folders
(step 4); the `Get-Acl … | Format-List` output (step 5); the backup job name and last
successful run (step 9); and the before/after Azure inventory (step 11).

## Context files

Read these before anything is created on the host. Each carries a rule whose violation is
either permanent or invisible.

| Path | What it tells the implementer |
|---|---|
| `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md` § D-003 | **The specification for this ticket.** "Decision"; "What decided it (constraint C-01)" — App Installer performs plain unauthenticated GETs and cannot send an `Authorization` header, so a private repository kills every GitHub-hosted feed and the option was *excluded, not ranked lower*; "Chosen shape" — stable DFS/CNAME path, never a machine name and never a mapped drive letter (mapped drives are per-session and are not guaranteed to exist in App Installer's context); the `prod/`+`pilot/` layout; the ACL split; `robocopy` from the release terminal or a self-hosted runner; the `UpdateUris` caveat; backup. And "**E · GitHub Releases / GitHub Pages — evaluated and excluded**", recorded so it is not re-proposed. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R9 | The seven steps this ticket makes executable, including the two that are counter-intuitive: copy the package **first** and the `.appinstaller` **last**; and step 5's rule that the `Uri` attribute must be byte-identical to the path clients installed from, because "changing the host name, share name or channel folder breaks updates for every existing installation and forces a reinstall". Step 6 is the single-point-of-failure statement this ticket must complete; step 7 is the off-network rule ("Do not raise the gateway minimum version while a pilot user is known to be away"). |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Hosting requirements | The "Decided host (D-003, 2026-08-23)" paragraph: over SMB none of the HTTP requirements apply — no MIME map, no `Content-Length`, no byte ranges. What replaces them is a permanently stable UNC path, read+execute for the staff group, write for the publisher only, and a `Uri` byte-identical to the installed path. |
| `docs/desktop/08-testing/test-uat-stack.md` § Components | The stack's update feed is "A **file share or local folder share** — the same SMB mechanism as production (D-003), so the stack rehearses the real path rather than an HTTP substitute". This is why step 10 is a genuine rehearsal and not a simulation. |
| `docs/desktop/08-testing/test-uat-stack.md:84` | `Publish-Feed` — the stack verb that copies a freshly packaged `.msix` and `.appinstaller` for the `teststack` channel into the feed folder, bumping the version, "used by the packaging tests to simulate mandatory updates and rollbacks". Step 10's rehearsal drives it. |
| `docs/runbook.md:776-781` | The live-operation approval matrix. Row 1: "Read Azure state (inventory, config, diagnostics) … **Permitted — no per-target approval.**" Row 2: every change needs explicit approval for the exact target. This ticket uses only row 1, and only to evidence that nothing changed. |
| `infra/modules/platform.bicep:100`, `:154` | The two existing storage accounts, `transportStorage` (tag `purpose: transport-deployment`) and `custodyStorage` (tag `purpose: custody-protection`), both `Standard_LRS`. Recorded so a reader can see they were considered as feed hosts and are deliberately **not** used: D-003 withdrew both blob options. |
| `eng/packaging/New-AppInstaller.ps1` and `Test-AppInstaller.ps1` (created by `DSK-09-03`, board `REL-003`) | The `Uri` shape the share must serve, and validator check 2, which compares the generated `Uri` against `<feed>/<channel>/Pegasus.appinstaller` for the channel. The feed layout and the validator must agree or every publish fails validation. |
| `eng/packaging/New-DesktopReleaseManifest.ps1` (created by `DSK-09-02`, board `REL-002`) | The thirteen manifest fields, in particular `packageSha256`, which `Test-FeedShare.ps1` compares against the published `.msix`, and `channel`, which names the folder. |
| `scripts/Test-CiChangeFlags.ps1` | The repository's script-test shape (`Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`, a local `Assert-…` helper that throws a case-named sentence). Follow it for both new scripts rather than introducing a test framework. |

## Ripple effects

- **`DSK-09-11` (board `REL-009`)** publishes the first pilot release with
  `Publish-DesktopRelease.ps1` and verifies with `Test-FeedShare.ps1`; its R1 step 7 wording
  is corrected from an HTTP header check to this SMB check.
- **`DSK-09-13` (board `REL-011`)** republishes a previous package with a higher
  `.appinstaller` `Version` using the same publish script, and depends on the
  never-overwrite rule holding: the previous `.msix` must still be on the feed.
- **`DSK-09-17` (board `REL-015`)** calls `Publish-DesktopRelease.ps1 -Channel pilot` from a
  self-hosted runner on the signing host, and adds a guard refusing `-Channel prod`. The
  parameter names fixed here are that workflow's call site.
- **`DSK-09-15` (board `REL-013`)** tells operators to open "the channel's `.appinstaller`
  from the Pegasus files location" — the path agreed at step 3 is what that sentence names,
  in operator words and without explaining SMB.
- **`DSK-08-17` (board `TEST-017`)** builds the stack's local feed host and `Publish-Feed`;
  step 10's rehearsal runs against it, so the two must agree on layout.
- **`DSK-11-03` (board `PLAT-021`)** is the conditional-Azure-writes catalogue; this ticket
  contributes the "no ⚠ write required" statement mirrored into
  `docs/desktop/11-azure-disposition/README.md`.
- **Documentation consistency.** After correcting the § 5 row, run
  `grep -rn "allowBlobPublicAccess\|desktop-releases container" docs/desktop/` and fix any
  other blob-era wording in area 09 rather than only the one row.
- **No OpenAPI, generated-client or build ripple.** No endpoint, no contract, no package
  reference, no project file changes; `dotnet restore ./Pegasus.slnx --locked-mode` is
  unaffected.

## Out of scope

Recorded so the reviewer sees these were decisions, not oversights. Each is a Guardrail in
the ticket body.

- **Any Azure write.** D-003 withdrew both blob options; `infra/modules/platform.bicep:100`
  and `:154` are not touched, and no container, account, RBAC assignment or Bicep change is
  made. Read-only inventory calls are permitted with no per-target approval
  (`docs/runbook.md:776-781`) and are used **only** to evidence that nothing changed.
- **HTTP hosting configuration.** MIME types, `Content-Length` and byte ranges are HTTP-only
  and do not apply over SMB. Do not build header checks; `Test-FeedShare.ps1` has none.
- **GitHub Releases and GitHub Pages.** Permanently excluded by C-01. Not a fallback, not a
  mirror, not a "while the repository is still public" stopgap.
- **A mapped drive letter or a raw machine name as the feed root.** Both forbidden by the
  chosen shape; a mapped drive is per-session and a machine name is replaceable, and the
  `Uri` is baked into every installation.
- **Overwriting a published `.msix`.** Never. Only `Pegasus.appinstaller` is replaced in
  place, and its `Version` must increase every time.
- **Putting the signing certificate on the share.** Explicitly forbidden by R9 step 3 and by
  D-002; the `.pfx` stays on the signing host under a restricted ACL.
- **`infra/`, `azure.yaml`, any Bicep module.** Untouched.
- **Performing the provisioning.** Share creation, DFS/CNAME naming, ACL assignment and
  backup configuration require administrative access to the in-house host and the directory
  service. An agent writes and tests the scripts; the operator performs the provisioning and
  hands back the evidence.
