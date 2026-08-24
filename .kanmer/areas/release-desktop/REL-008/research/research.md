# Research — REL-008: standing up the decided UNC update feed, and what makes its path permanent

## Question

What must exist on the in-house Windows host — path, layout, ACLs, publish order, backup —
so that App Installer can update every workstation over SMB, and what is the recorded
answer to whether `UpdateUris` accepts a UNC fallback?

## Current behaviour

**There is no update feed of any kind.** The web application is served from a Container App
and a browser reload is the update; nothing distributes a client. Verified on 2026-08-24:
`ls eng` returns nothing; `.github/workflows/ci.yml` (234 lines) has no publish lane;
`scripts/` holds no upload or copy step for client artefacts.

Two storage accounts exist and are **not** the feed:
`infra/modules/platform.bicep:100` (`transportStorage`, tag `purpose: transport-deployment`,
`Standard_LRS`) and `:154` (`custodyStorage`, tag `purpose: custody-protection`,
`Standard_LRS`). D-003 withdrew both blob options, so neither is touched.

**No parity-matrix row covers this.** `docs/desktop/01-inventory-and-parity/parity-matrix.md`
runs `PAR-01`…`PAR-46` over Razor page models; hosting a client update feed is not an
observable web capability and has no row. It is new desktop responsibility under proposal
§ 9.1.

## Findings

- **D-003 is decided, and the § 5 row's acceptance text predates it.** The area plan's § 5
  row for `DSK-09-10` still reads "Read-only checks done (`allowBlobPublicAccess`, RBAC,
  costs); MIME/Range/Content-Length verification procedure written; ⚠ writes enumerated with
  approval text" — wording that belongs to the **withdrawn** Azure blob options. The
  decision matrix and § 3 of the same plan supersede it, and the ticket body requires the
  row's wording to be corrected in the same task.
- **What decided it was constraint C-01, not a preference.**
  `signing-and-hosting-decision-matrix.md` § D-003: the repositories become private on
  completion; "App Installer performs plain, unauthenticated GETs and cannot send an
  `Authorization` header, so every GitHub-hosted feed (Releases, Pages) would stop working
  the day the repository flips — and the feed is permanent infrastructure that every
  installed client re-reads on every launch. Any option whose viability depends on the
  repository staying public was therefore **excluded, not merely ranked lower**."
- **Option E is recorded as excluded so it is not re-proposed.** "**E · GitHub Releases /
  GitHub Pages — evaluated and excluded** … private-repository release assets require an
  authenticated request that App Installer cannot make, and GitHub Pages on private
  repositories is an Enterprise feature."
- **SMB is documented as supported.** The matrix quotes
  <https://learn.microsoft.com/windows/msix/app-installer/app-installer-file-overview>
  (fetched 2026-08-23): "App Installer file downloads and updates support **https, http and
  smb** protocols", and notes UNC/share hosting has been available since Windows 10 build
  17134 (1803), well below the Windows 11 baseline.
- **MIME, `Content-Length` and byte ranges are HTTP-only** and do not apply over SMB —
  `appinstaller-template.md` § Hosting requirements and area plan § 2. The equivalent
  controls over SMB are share ACLs and a permanently stable path.
- **The path must be permanent from day one.** The `.appinstaller` `Uri` is baked into every
  installed client and re-read on every launch: "use a DFS namespace or a CNAME'd host
  (`\\pegasus-files\apps\...`), never a machine name that may be replaced, and never a
  mapped drive letter (mapped drives are per-session and are not guaranteed to exist in App
  Installer's context)."
- **Publish order is package first, `.appinstaller` last** — `runbooks.md` § R9 step 1 —
  "a client that reads the manifest mid-publish must never find a package that is not there
  yet". The literal command is
  `robocopy <staging> \\<host>\<share>\<channel> Pegasus_<ver>_x64.msix /Z /R:2 /W:5`, then
  `desktop-release-manifest.json`, then `Pegasus.appinstaller`.
- **Never overwrite a published `.msix`.** A new version always means a new file name; only
  `Pegasus.appinstaller` is replaced in place, and its `Version` attribute must increase
  every time — R9 step 2.
- **ACLs**: staff group read + execute, publisher account modify, nobody else writes; the
  signing certificate never lives on the share — R9 step 3 and the matrix's chosen shape.
- **Verification is done from a non-publisher account** — R9 step 4: `Test-Path` resolves
  the channel path; `Select-Xml -XPath /*` shows the expected `Version` and `Uri`;
  `Get-FileHash` matches `desktop-release-manifest.json`; `Get-Acl` shows the staff group has
  no write permission.
- **The `UpdateUris` caveat is a real, unanswered question with a recorded starting point.**
  The matrix records that the element is documented as *"Web URI as a string"*
  (<https://learn.microsoft.com/uwp/schemas/appinstallerschema/element-s4-updateuri>,
  fetched 2026-08-23), "so a second **UNC** path may not be accepted as a fallback. If it is
  not, the feed has no fallback and share availability is the single point of failure for
  updates — acceptable (updates are not time-critical; the gateway gate holds the safety
  line), but it must be **stated in the runbook rather than assumed**."
- **Off-network clients simply do not update** — R9 step 7 — "That is expected. Do not raise
  the gateway minimum version while a pilot user is known to be away, or they are locked out
  of work until they return."
- **The Test/UAT stack rehearses the real mechanism, not a substitute.**
  `docs/desktop/08-testing/test-uat-stack.md` § Components: the update feed is "A **file
  share or local folder share** — the same SMB mechanism as production (D-003), so the stack
  rehearses the real path rather than an HTTP substitute", and `Publish-Feed` (`:84`)
  "Copies a freshly packaged `.msix` and the `.appinstaller` for the `teststack` channel into
  the feed folder, bumping the version".
- **Read-only Azure checks need no approval.** `docs/runbook.md:776-781` § Live-operation
  approval matrix: "Read Azure state (inventory, config, diagnostics) … **Permitted — no
  per-target approval.**" This is why step 11's inventory record is free, and why it is
  evidence of *nothing changing* rather than a write.

### Facts

Verified by reading this repository on 2026-08-24 unless a URL and fetch date is given.

| Fact | Source |
| --- | --- |
| No update feed, publish lane or client-artefact copy step exists | `ls eng`; `.github/workflows/ci.yml`; `ls scripts/` |
| Two storage accounts exist, tagged `transport-deployment` and `custody-protection`, both `Standard_LRS`; neither is the feed | `infra/modules/platform.bicep:100`, `:154` |
| Read-only Azure state reads are permitted with no per-target approval | `docs/runbook.md:776-781` |
| D-003's chosen shape: stable DFS/CNAME path, `prod/` and `pilot/` layout, staff read+execute / publisher modify ACLs, publisher copies with `robocopy`, `UpdateUris` caveat, backup | `signing-and-hosting-decision-matrix.md` § D-003 |
| GitHub Releases and Pages are permanently excluded by C-01 | same file, "E · GitHub Releases / GitHub Pages — evaluated and excluded" |
| App Installer supports https, http and **smb**; UNC hosting since Windows 10 1803 | same file, quoting <https://learn.microsoft.com/windows/msix/app-installer/app-installer-file-overview> (fetched 2026-08-23) |
| `s4:UpdateUri` is documented as "Web URI as a string" | same file, citing <https://learn.microsoft.com/uwp/schemas/appinstallerschema/element-s4-updateuri> (fetched 2026-08-23) |
| R9 steps 1–7: publish order, no overwrite, ACLs, non-publisher verification, `Uri` byte-identity, single point of failure, off-network behaviour | `docs/desktop/09-release-update-and-distribution/runbooks.md` § R9 |
| MIME / `Content-Length` / byte ranges are HTTP-only and do not apply over SMB | `appinstaller-template.md` § Hosting requirements |
| The Test/UAT stack's feed is the same SMB mechanism, with a `Publish-Feed` verb | `docs/desktop/08-testing/test-uat-stack.md` § Components, `:84` |
| The estate is ten Windows 11 x64 workstations | area plan § 2 Assumptions |

### Assumptions

- **A-09-17 — an always-on in-house Windows host exists, or can be nominated.** D-003 names
  one, and the matrix says the same machine serves the share, custodies the signing `.pfx`
  and would host a self-hosted CI runner. Nothing in this repository names the machine.
  *Confirmed by*: the operator naming it at step 3, together with the DFS namespace or CNAME.
  *Breaks if wrong*: there is no feed and no signing host, and both D-002 and D-003 need
  re-opening — which is an operator decision, not an agent's. Raise it; do not improvise a
  host.
- **A-09-18 — a DFS namespace or a CNAME can be created.** The matrix forbids a raw machine
  name because it may be replaced, and forbids a mapped drive letter because mapped drives
  are per-session.
  *Confirmed by*: `Test-Path \\<name>\<share>` resolving from a workstation that has never
  had the host's real name.
  *Breaks if wrong*: the `Uri` is bound to a machine name, and replacing that machine forces
  a reinstall on every workstation. Mitigation: if neither DFS nor a CNAME is available,
  record that explicitly as a recognised debt in R9 with the reinstall cost stated — do not
  quietly use the machine name as if it were permanent.
- **A-09-19 — `robocopy` over the share is reliable enough for a package of this size.**
  R9 step 1 fixes the flags `/Z /R:2 /W:5` (restartable mode, two retries, five-second
  waits).
  *Confirmed by*: a publish to the Test/UAT stack share at step 10, followed by a
  `Get-FileHash` comparison against the staging copy.
  *Breaks if wrong*: a truncated `.msix` on the feed with a correct-looking `.appinstaller`
  pointing at it — which is exactly why the hash check at R9 step 4 exists and why the
  `.appinstaller` is copied last.
- **A-09-20 — the staff group and publisher account already exist as directory objects.**
  R9 step 3's ACL assumes both.
  *Confirmed by*: `Get-Acl` output at step 5 naming them.
  *Breaks if wrong*: the ACL is set on individuals and drifts as staff change. Mitigation:
  record the group and account names in R9 so the next reader sets the same ones.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered for the responsibility
this ticket places: *serving the update feed every installed client re-reads on every
launch*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | The feed is written by one publisher account and read by everyone else. R9 step 3's ACL is exactly this shape: staff read + execute, publisher modify, nobody else writes. |
| Unattended execution — must it run with every desktop closed? | **yes** | `AutomaticBackgroundTask` checks roughly every eight hours and `OnLaunch` fires whenever any workstation starts, so the share must answer when the publisher's own desktop is closed. **D-003 satisfies this with an always-on in-house Windows host, not a cloud service** — the requirement is availability, not internet reachability. |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | SMB carries the staff member's own Windows identity; no secret is stored in the client and no anonymous endpoint exists. The signing certificate never lives on the share (R9 step 3). |
| Public callback — must an external service call a stable public endpoint? | **no** | Clients poll; nothing calls in. This is precisely why SMB survives constraint C-01 while GitHub Releases and Pages do not — App Installer cannot send an `Authorization` header, and over SMB it does not need to. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **no** | Share ACLs restrict **writes**, not client behaviour. The launch check **fails open** when the feed is unreachable; an obsolete client is stopped by the gateway minimum-version gate, `DSK-04-06` (board `GWY-023`), not by the feed. |
| Measured operational advantage — measured evidence central is materially better? | **no** | The matrix records the opposite: the Azure options' only advantage over a share was internet reachability for users away from the network, "which the operator does not need". |

One "yes" — unattended availability — and D-003 satisfies it **in-house**. **No Azure
resource is created, changed or deleted by this ticket**, and step 11 records that with a
read-only inventory that is free under `docs/runbook.md:776-781`.

## Implications

- **Getting the path right is the whole ticket.** Every other decision here is recoverable;
  the `Uri` is not. Step 3 is the one place a permanent commitment is made, and it must be
  a DFS namespace or a CNAME.
- **Write the publish order into a script, not into a habit.** R9 steps 1–2 are implemented
  verbatim as `eng/packaging/Publish-DesktopRelease.ps1` so a hurried release cannot copy
  the manifest first.
- **Verify from the account that will actually read the feed.** `Test-FeedShare.ps1` run as
  the publisher proves nothing about the staff group's access; R9 step 4 says "from a
  workstation that is **not** the publisher, signed in as an ordinary staff user", and the
  script's `Get-Acl` check is meaningless otherwise.
- **Answer the `UpdateUris` question from documentation and write the answer down.** If UNC
  fallbacks are not accepted, the correct outcome is a **stated** single point of failure in
  R9, not silence. The gateway gate holds the safety line, so it is acceptable — but only
  once it is written.
- **Correct the stale § 5 row in the same task.** Leaving blob-era acceptance wording in the
  plan is how a later reader reintroduces an option C-01 permanently excluded.
- **Rehearse on the Test/UAT stack first.** Its feed is the same SMB mechanism, so step 10
  is a real rehearsal rather than a simulation, and it is the L-02 gate before the pilot.

## Open questions

- **Does `UpdateUris` accept a second UNC path as a fallback?** The element is documented as
  "Web URI as a string", which suggests not. It is answered in step 8 by fetching
  <https://learn.microsoft.com/uwp/schemas/appinstallerschema/element-s4-updateuri> and
  writing the finding into R9 step 6 as a stated fact. It is **not blocking**: either answer
  is acceptable, and the negative answer simply becomes a recorded single point of failure.
  No `open-questions` document is created.
- **Which host, and what DFS/CNAME name?** (A-09-17, A-09-18.) Operator-supplied at step 3.
  Not blocking for the scripts, which take `-FeedRoot` as a parameter, so
  `Publish-DesktopRelease.ps1` and `Test-FeedShare.ps1` can be written, tested against the
  Test/UAT stack share, and reviewed before the estate path exists.
- **Not open, and not to be re-opened**: the hosting route. D-003 is decided; the two blob
  options and GitHub Releases/Pages are excluded, the latter permanently by C-01.
