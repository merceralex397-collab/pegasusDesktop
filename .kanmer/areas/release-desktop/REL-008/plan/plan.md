# Plan — REL-008: DSK-09-10 · Stand up the decided UNC update feed (D-003): path, channels, ACLs, backup

**Diff estimate: ~5 files, ~200 lines.** Two new scripts —
`eng/packaging/Publish-DesktopRelease.ps1` (~80 lines: parameter block, ordered
`robocopy` calls, never-overwrite guard, hash confirmation) and
`eng/packaging/Test-FeedShare.ps1` (~80 lines: four checks with named failures) — plus
~25 lines added to `docs/desktop/09-release-update-and-distribution/runbooks.md` § R9, a
~5-line correction to the § 5 row of the area plan, and a ~5-line mirror in
`docs/desktop/11-azure-disposition/README.md`. The provisioning itself is **operator work
on a Windows host** and produces evidence, not diff. `docs/engineering.md:201-207` § Plan
sizing requires the estimate first.

## Approach

**Build the feed the decision already specifies, and put its two counter-intuitive rules
into scripts rather than into habits.** D-003 is decided; the shape — a permanently stable
UNC root, `prod/` and `pilot/` folders, staff read+execute and publisher modify, at least
the previous package retained per channel — is written out in
`signing-and-hosting-decision-matrix.md` § D-003 and `runbooks.md` § R9, and this ticket
executes it. The two rules that a hurried release will otherwise break are the publish
order (package first, `.appinstaller` last) and the never-overwrite rule, so both go into
`Publish-DesktopRelease.ps1` where they cannot be forgotten. Verification goes into
`Test-FeedShare.ps1` and is run **as an ordinary staff user from a non-publisher machine**,
because a publisher-run ACL check proves nothing about the account that will actually read
the feed.

Two families of alternatives are already rejected by the operator and are **not re-argued
here**: the Azure blob options (a new container in an existing account, or a dedicated
account), withdrawn by D-003; and GitHub Releases / GitHub Pages, excluded **permanently**
by constraint C-01 because App Installer performs plain unauthenticated GETs and cannot
send an `Authorization` header, so a private repository kills both.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-008`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). Its **Consequences**
> section records D-003: the feed is a UNC share served to App Installer over SMB, and the
> whole distribution path therefore touches no Azure resource. This ticket is the execution
> of that consequence. This plan is written to the decision as recorded in
> `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md`
> § D-003; if ADR-0105 lands differently, this plan is revised before implementation.

Existing ADRs and rules this plan **meets**:

- **ADR-0014** (`docs/adr/0014-local-to-production-deployment.md`) — local and production
  only, no Azure dev/test/staging. **Meets**: the rehearsal at step 10 runs on the local
  Test/UAT stack, whose feed is the same SMB mechanism; no environment is created.
- **`docs/runbook.md:776-781` § Live-operation approval matrix.** **Meets**: this ticket
  uses only the "Read Azure state … Permitted — no per-target approval" row, and only to
  evidence that nothing changed.

Binding operator decisions and constraints, written to as settled:

- **D-003** (2026-08-23) — the feed is an in-house **UNC file share** over SMB;
  **no Azure write and no recurring cost**.
- **C-01** — the repositories become private; **GitHub Releases and GitHub Pages are ruled
  out permanently** and must not be re-proposed as a fallback or a mirror.
- **D-002** — the signing certificate never lives on the share.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present); `pegasus-azure-auditor`
  (`.codex/agents/pegasus-azure-auditor.toml`, verified present) only if a read-only Azure
  confirmation is wanted for the record that nothing was provisioned.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`,
  `microsoft/win-dev-skills` v0.5.0 `f1028dd5`, verified present).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`) for `s4:UpdateUri` and the App Installer file overview; Azure MCP
  **read-only** (`group_resource_list`, `storage`) only to record that no Azure resource was
  created.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates REL-008` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps in the same order, with the same
ownership. Steps marked **Operator step** require administrative access to the in-house
host or the directory service; an agent writes and tests the scripts and records the
evidence.

1. **Orient and take.** Read the area plan § 5 row `DSK-09-10`,
   `signing-and-hosting-decision-matrix.md` § D-003 in full, `runbooks.md` § R9, and
   `appinstaller-template.md` § Hosting requirements. `get_doc_gates REL-008`, then
   `take_ticket REL-008`. **D-003 is decided** — build the UNC share. The blob-era
   acceptance wording still in the § 5 row (`allowBlobPublicAccess`, RBAC, MIME/Range
   verification) belongs to the withdrawn options and does not apply; step 1 of the
   correction is knowing that before reading the row.
2. **Put the protocol support on the record.** `microsoft_docs_fetch`
   <https://learn.microsoft.com/windows/msix/app-installer/app-installer-file-overview>
   and quote the sentence stating that App Installer downloads and updates support
   **https, http and smb**. Paste it, with the fetch date, into this ticket's `research`
   scratch so the decision is evidenced rather than asserted.
3. **Operator step — fix the permanent path.** Choose a DFS namespace or a CNAME'd host,
   for example `\\pegasus-files\apps`. **Never** a raw machine name that may be replaced,
   and **never** a mapped drive letter — mapped drives are per-session and are not
   guaranteed to exist in App Installer's context. Hand back the agreed UNC root; it is
   written into every `.appinstaller` `Uri` from then on and cannot be changed without a
   reinstall on every workstation. If neither DFS nor a CNAME can be created, record that
   explicitly in R9 as a recognised debt with the reinstall cost stated — do not quietly
   use the machine name as if it were permanent.
4. **Operator step — create the layout.** `\\<host>\<share>\prod\` and
   `\\<host>\<share>\pilot\`, each to hold `Pegasus.appinstaller`, the versioned
   `Pegasus_<ver>_x64.msix` files (at least the previous one retained) and
   `desktop-release-manifest.json`. Hand back a `Get-ChildItem` listing.
5. **Operator step — set the ACLs.** Read and execute for the staff group; **modify** for
   the publisher account only; nobody else writes; remove inherited broad grants. Hand back
   `Get-Acl \\<host>\<share>\prod | Format-List`. Record the **group and account names** in
   R9 so the next reader sets the same ones rather than granting individuals. The signing
   certificate never lives on the share.
6. **Write `eng/packaging/Test-FeedShare.ps1`.** Repository script header
   (`[CmdletBinding()]`, `Set-StrictMode -Version Latest`,
   `$ErrorActionPreference = 'Stop'`), parameters `-FeedRoot` and `-Channel`
   (`ValidateSet 'pilot','prod'`). Implement R9 step 4 exactly, four checks with named
   failures: `Test-Path` resolves the channel path; `Select-Xml -Path <path>\Pegasus.appinstaller
   -XPath /*` shows the expected `Version` and `Uri`; `Get-FileHash` of the package equals
   `desktop-release-manifest.json`'s `packageSha256`; `Get-Acl` shows the staff group has
   **no** write permission. Exit non-zero on any failure. Put a header comment saying the
   script must be run **from a workstation that is not the publisher, signed in as an
   ordinary staff user** — run as the publisher, the ACL check proves nothing.
7. **Write `eng/packaging/Publish-DesktopRelease.ps1`**, implementing R9 steps 1–2 verbatim
   and in order: `robocopy <staging> \\<host>\<share>\<channel> Pegasus_<ver>_x64.msix
   /Z /R:2 /W:5` **first**, then `desktop-release-manifest.json`, then
   `Pegasus.appinstaller` **last** — because a client that reads the manifest mid-publish
   must never find a package that is not there yet. Add a guard that **refuses to overwrite
   an existing `.msix`** with a named failure (a new version always means a new file name);
   only `Pegasus.appinstaller` is replaced in place, and its `Version` must increase every
   time. After the copy, re-hash the published `.msix` and compare it against the staging
   copy, so a truncated `robocopy` is caught before the manifest is published.
8. **Resolve the `UpdateUris` caveat and record the answer as a fact.**
   `microsoft_docs_fetch`
   <https://learn.microsoft.com/uwp/schemas/appinstallerschema/element-s4-updateuri> and
   establish whether a second **UNC** path is accepted as a fallback — the element is
   documented as "Web URI as a string", which suggests it is not. Write the finding into
   `runbooks.md` § R9 step 6 as a stated fact with the URL and fetch date. **If UNC
   fallbacks are not accepted, state plainly that the feed has no fallback and that share
   availability is the single point of failure for updates** — acceptable because updates
   are not time-critical and the gateway gate holds the safety line, but stated rather than
   assumed.
9. **Operator step — backup.** Confirm the share is covered by the host's backup and record
   the restore path. A lost share means republishing from the CI artifacts, not a client
   migration. Hand back the backup job name and its last successful run.
10. **Rehearse the whole client side on the Test/UAT stack before touching the estate.**
    Its feed is "the same SMB mechanism as production (D-003), so the stack rehearses the
    real path rather than an HTTP substitute"
    (`docs/desktop/08-testing/test-uat-stack.md` § Components). Publish a package with
    `Publish-DesktopRelease.ps1`, install from
    `\\<stack host>\<share>\teststack\Pegasus.appinstaller`, then publish a higher version
    and confirm an update is detected on relaunch. This is the **L-02 rehearsal that must
    pass before the pilot**.
11. **Record that no Azure resource was created.** Optionally run Azure MCP
    `group_resource_list` for `rg-pegasus-prod` **read-only** before and after and attach
    the identical inventories to the ticket proof — permitted with no per-target approval
    (`docs/runbook.md:776-781`). Mirror the "no ⚠ write required" statement in
    `docs/desktop/11-azure-disposition/README.md`. Also correct the stale § 5 row wording
    in the area plan, and run
    `grep -rn "allowBlobPublicAccess\|desktop-releases container" docs/desktop/` to catch
    any other blob-era text in area 09.
12. **Simplification pass.** Record it under a dated `## Simplification pass` heading in
    this document (`AGENTS.md` § Repository task workflow step 4). This branch adds scripts
    and edits documentation, so `n/a — docs-only` does not apply.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**, as the plan row
assigns. The obligation is a verified configuration — paths, ACLs, hashes, documented
fallback answer — plus the Test/UAT client-side rehearsal. Pilot-ring behaviour is proven
later by `DSK-09-11` (board `REL-009`) and this ticket must not claim it. `proof` is the
command output and operator transcripts as proof type `command-log`.

| Command / observation | Expected evidence |
| --- | --- |
| `pwsh ./eng/packaging/Test-FeedShare.ps1 -FeedRoot \\<host>\<share> -Channel pilot`, run **as an ordinary staff user** | exit `0`; path resolves, manifest `Version`/`Uri` as expected, hash matches, staff group has no write right |
| `Get-Acl \\<host>\<share>\prod | Format-List` | publisher account `Modify`, staff group `ReadAndExecute`, no `Everyone` and no other write entry |
| Test/UAT rehearsal: install from the stack feed, publish a higher version, relaunch | App Installer prompts and the new version installs; record `Get-AppxPackage CollisionEngineers.Pegasus` before and after |
| Azure MCP `group_resource_list` for `rg-pegasus-prod`, before and after | identical resource lists |

Behaviours to observe rather than infer, and to state in the proof: the agreed UNC root is
a DFS namespace or a CNAME and is recorded as permanent; the `.appinstaller` `Uri` on the
share is **byte-identical** to the path clients install from (compare the strings, do not
eyeball them); the publish order was package → manifest → `.appinstaller`; and the
`UpdateUris` answer with its source URL and fetch date.

## Risks / open questions

- **Risk — the path is not permanent.** The `Uri` is baked into every installation, so
  changing host, share or channel folder breaks updates for every existing installation and
  forces a reinstall. Mitigation: step 3's DFS/CNAME requirement, and the recorded-debt
  fallback if neither is available.
- **Risk — a manifest published before its package.** A client checking mid-publish finds a
  package that is not there. Mitigation: step 7 puts the order in the script, and the
  re-hash after copy catches a truncated transfer.
- **Risk — a published `.msix` is overwritten.** Rollback (`DSK-09-13`, board `REL-011`)
  depends on the previous package still being present. Mitigation: step 7's never-overwrite
  guard with a named failure.
- **Risk — the ACL check is run as the publisher.** It then proves nothing. Mitigation:
  step 6's header comment and the verification table's explicit "as an ordinary staff user".
- **Risk — header checks are built by habit.** MIME, `Content-Length` and byte ranges are
  HTTP-only and do not exist over SMB; such checks would be unrunnable against the real
  feed. Mitigation: it is a Guardrail in the body and an Out-of-scope entry in the files
  document.
- **Risk — an off-network client is locked out.** R9 step 7: off-network clients do not
  check for updates until they return, which is expected — but do **not** raise the gateway
  minimum version while a pilot user is known to be away. Mitigation: stated in R9 and
  carried into `DSK-09-12` (board `REL-010`).
- **Risk — the share is a single point of failure for publishing.** Installed clients keep
  running; only updates stop. Mitigation: step 9's backup, and step 8's stated fallback
  answer so the risk is written down rather than discovered.
- **Open question — does `UpdateUris` accept a UNC fallback?** Answered in step 8 from
  official documentation, and **either answer is acceptable**: the negative answer becomes a
  stated single point of failure in R9. Not blocking; no `open-questions` document is
  created.
- **Open question — which host, and what DFS/CNAME name?** Operator-supplied at step 3. Not
  blocking for the scripts, which take `-FeedRoot` as a parameter and can be written, tested
  against the Test/UAT stack share and reviewed before the estate path exists.
- **Not open, and not to be re-opened**: the hosting route. D-003 is decided; both blob
  options are withdrawn; GitHub Releases and Pages are permanently excluded by C-01.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds
scripts and edits documentation, so `n/a — docs-only` does not apply._
