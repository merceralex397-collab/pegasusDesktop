# Plan — FND-048: `teststack` `.appinstaller` (2021 schema) and the local Test/UAT update feed

**Diff estimate: ~5 files, ~110 lines.**

## Measured file-and-line inventory

Profile `chore` owes no `research` and no `files` document, so this plan
carries the surface-area burden alone —
`.grok/skills/kanmer-plan/assets/plan-template.md`'s "written FROM the ticket's
`research` and `files` documents" precondition does not apply to `chore`. Every
row below was measured against the fork working tree on 2026-08-24 with
`wc -l`, `sed -n` and `grep -n`; the estimate above is the sum, not an
assertion.

The body's step 2 makes this ticket's shape conditional on whether
[[REL-003]] (plan handle `DSK-09-03`) has landed. **Case A is the estimate
above.**

### Case A — [[REL-003]] has landed (the expected case)

| Path | Measured now | Change | Lines |
| --- | --- | --- | --- |
| `eng/packaging/New-AppInstaller.ps1` | Created by [[REL-003]] with a `-Channel` parameter; today `ls eng` returns *No such file or directory* — the folder does not exist yet | **Edit.** Accept `teststack` as a third channel value and map it to the `teststack` feed path. No second generator. | +8 |
| `eng/packaging/Test-AppInstaller.ps1` | Created by [[REL-003]]; its check 2 compares `Uri` to `<feed>/<channel>/Pegasus.appinstaller` for the channel (`docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Validator outline, check 2) | **Edit.** Add `teststack` to the accepted channel set so check 2 does not reject the new channel as unknown. | +6 |
| `eng/packaging/fixtures/appinstaller/valid-teststack.xml` | The fixture directory is created by [[REL-003]] with ten files; there is no `teststack` fixture | **New.** A passing `teststack` fixture so [[REL-003]]'s `Test-TestAppInstaller.ps1` covers the third channel. | ~36 |
| `scripts/Invoke-LocalDevelopment.ps1` | **1583 lines.** `[ValidateSet('Start', 'Status', 'Smoke', 'Stop', 'Reset')]` at `:3`; `switch ($Action)` at `:1496` with arms `'Start'` `:1497`, `'Status'` `:1501`, `'Smoke'` `:1528`, `'Stop'` `:1532`, `'Reset'` `:1545`, closing `:1571`; `$mutex = $null` at `:1494` and the `finally { $mutex.ReleaseMutex() }` at `:1573-1583`. `grep -n 'Publish-Feed' scripts/Invoke-LocalDevelopment.ps1` returns **nothing**. | **Edit, under [[TEST-017]]'s (plan handle `DSK-08-17`) contract.** Extend the `ValidateSet` at `:3` with `'Publish-Feed'` and add one arm before `:1571`. If [[TEST-017]] has landed, supply only the `teststack` channel path and change no behaviour (+6 instead of +55). | +55 (or +6) |
| `docs/desktop/08-testing/test-uat-stack.md` | **186 lines.** The `Update feed` component row is `:32`; the `Desktop client` row naming the `teststack` channel is `:33`; the `Publish-Feed` verb row is `:84`; the ticket table naming `DSK-08-17` as its owner is `:167`. | **Edit.** Record the actual `teststack` feed path on `:32`, and nothing else — the `Publish-Feed` verb and its contract belong to [[TEST-017]] (body § Documentation changes). | +6 |

**Sum: 5 files, ~111 lines → ~110.**

### Case B — [[REL-003]] has not landed

| Path | Change | Lines |
| --- | --- | --- |
| `eng/packaging/teststack/Pegasus.appinstaller` | **New.** The `teststack` file itself, copied literally from `appinstaller-template.md` § Template with the `teststack` values substituted by hand. **No generator and no template file** — both belong to [[REL-003]], and writing one here is the "second template" the body's step 2 forbids. | ~36 |
| `scripts/Invoke-LocalDevelopment.ps1` | As Case A | +55 |
| `docs/desktop/08-testing/test-uat-stack.md` | As Case A | +6 |

**Sum: 3 files, ~97 lines.** Record which case applied in the plan under a
dated note before writing any file (body step 2).

### Measured and deliberately not touched

| Path | Measured now | Why not |
| --- | --- | --- |
| `scripts/Build-ReleaseArtifacts.ps1` | 130 lines | The gateway release route; the body's Guardrails exclude it. |
| `docs/runbook.md` § Supported platform | Section runs `:19-38`; `grep -n 'winapp\|Developer Mode\|MSIX' docs/runbook.md` returns **nothing** today | [[FND-039]] (plan handle `DSK-02-14`) step 11 is the **single owner** of that paragraph and names this ticket among those that cite rather than restate it. Touch it only if [[FND-039]] has not landed, and then say so in the proof. |
| `docs/adr/0014-local-to-production-deployment.md` | 28 lines, status `accepted` | ADR bodies are immutable (`AGENTS.md` § ADR conventions). This ticket is governed by it, not an amendment to it. |
| `.github/workflows/ci.yml` | The `windows-latest` jobs begin at `:76` | The desktop packaging lane is [[REL-005]]'s (plan handle `DSK-09-05`). |
| `src/Pegasus.Web` | — | Guardrails. |

## Approach

**Own one channel and nothing else, and make the local feed the real SMB
mechanism rather than a convenient stand-in.** The `teststack` channel is added
to the packaging assets [[REL-003]] already owns — its generator, its validator
and its fixture set — and the feed is a folder **share**, published through the
`Publish-Feed` verb [[TEST-017]] owns in `scripts/Invoke-LocalDevelopment.ps1`.

The alternative rejected is **a small local HTTP server for the test feed**
(`dotnet serve`, a Python one-liner, or IIS Express). It is easier to stand up,
it needs no share permissions, and it is wrong: D-003 put the production feed
on a UNC share over SMB, `docs/desktop/08-testing/test-uat-stack.md:32` says
the stack uses "the same SMB mechanism as production … so the stack rehearses
the real path rather than an HTTP substitute", and an HTTP feed would rehearse
a transport that will never be used — a green test that proves nothing about
the production path. The one thing HTTP would buy (MIME and byte-range
verification) is explicitly *not applicable* over SMB
(`appinstaller-template.md` § Hosting requirements, the "Decided host (D-003,
2026-08-23)" paragraph), so it would not even buy that.

The second alternative rejected is **a sibling `Publish-TestFeed.ps1` script**.
`docs/desktop/08-testing/test-uat-stack.md:71-75` says in as many words:
"Extend `scripts/Invoke-LocalDevelopment.ps1` with a `TestStack` mode rather
than adding a sibling script — it already owns `Start`, `Status`, `Smoke`,
`Stop`, `Reset` … and the runbook already documents it." The body's Traps
section makes a second verb or a sibling script a **stop condition**.

## Governing docs

### Linked `refs`

| Ref | Requirement | Meets |
| --- | --- | --- |
| `docs/adr/0014-local-to-production-deployment.md` (accepted 2026-07-31) | "Pegasus has two environments: isolated local development and production. There is **no** Azure development, test, integration, or staging environment." And: "No document, capability, infrastructure declaration, or future change may assume or create a non-production Azure environment without a new accepted decision and exact external authority." | **Meets** — the whole ticket. Steps 7 and 9–11 stand the update feed up as a **local folder share** on the test workstation. No storage account, no static website, no Azure resource of any kind is created, which is exactly what ADR-0014 forbids being created. The Guardrails' "do not create a storage account 'just for the test feed'" is this ADR restated at the point of temptation. |

ADR-0014 is **not** superseded and is not edited: `docs/desktop/README.md`
§ Locked decisions records under L-02 that "ADR-0014 stands".

### `docs_todo: true`

`get_doc_gates FND-048` reports `docs_todo: true`, so no conversion ADR governs
this yet.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a
> gateway minimum-version gate), authored by [[REL-001]] (plan handle
> `DSK-09-01`); [[FND-005]] (plan handle `DSK-00-05`) and [[FND-042]] (plan
> handle `DSK-04-01`) also claim ADR-0105 — see [[REL-001]]'s plan for the
> ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/README.md` § Locked decisions (L-02, D-002, D-003) and
> `docs/desktop/09-release-update-and-distribution/appinstaller-template.md`;
> if ADR-0105 lands differently this plan is revised before implementation.

The reserved block ADR-0100…ADR-0110 does not exist yet: `docs/adr/` holds
ADR-0001…ADR-0029 and the block is reserved in
`docs/desktop/00-governance-and-workflow/README.md:140-165`.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| **L-02** (`docs/desktop/README.md` § Locked decisions) | Test/UAT is a local production-mimicking stack; no Azure test environment; ADR-0014 stands | Steps 7–11 |
| **D-002** (2026-08-23) | Self-managed certificate; the certificate subject must equal the manifest `Publisher` **exactly** | Step 5 |
| **D-003** (2026-08-23) | The feed is a UNC share served over SMB | Steps 4 and 7 — a folder share, never an HTTP substitute |
| **C-01** (`docs/desktop/README.md` § Constraints) | The repositories become private; the feed must not depend on anonymous HTTPS | Step 7 — SMB carries Windows authentication, which is why the share is the rehearsal target |
| Proposal § 9.1 Two-layer enforcement | The package mechanism performs the trusted installation | Steps 3, 10, 11 |
| Proposal § 9.3 Operational controls | Interrupted updates, unavailable feeds and rollback are tested | Step 12 (fail-open recorded) and the handover to [[TEST-010]] (plan handle `DSK-08-10`) for the rest |
| `appinstaller-template.md` § Template (2021 schema) | The literal XML, including its three comment blocks | Step 3 — copied, never paraphrased |
| `appinstaller-template.md` § Rules the template encodes | `Uri` equals the exact served path; `.appinstaller` `Version` increases on **every** publish including a rollback; `MainPackage Version` is `1.<minor>.<build>.0`; `Publisher` matches the certificate subject; only the 2021 namespace supports the enforcing attributes | Steps 4, 6, 5, 3 |
| `appinstaller-template.md` § Hosting requirements | Over SMB the MIME / `Content-Length` / byte-range rows do **not** apply; path stability is absolute | Steps 4 and 7; the Out-of-scope note on header checks |
| `appinstaller-template.md` § Known behaviours | Fail-open when the feed is unreachable; `ms-appinstaller:` dead since Dec 2023; CSP overrides PowerShell overrides `.appinstaller`; `CheckUpdateAvailabilityAsync` returns `Unknown` for a side-loaded package | Steps 11, 12 |
| `docs/desktop/09-release-update-and-distribution/README.md:319-355` § 7 | The trap list, including "2017/2 schema silently ignores `ShowPrompt`/`UpdateBlocksActivation`" and "`UpdateBlocksActivation` needs `ShowPrompt`" | Step 3 and the first Verification row |
| `docs/desktop/08-testing/test-uat-stack.md:71-75` | Extend `Invoke-LocalDevelopment.ps1`, do not add a sibling script | Step 8 |
| `docs/desktop/08-testing/test-uat-stack.md:84` | `Publish-Feed` copies the `.msix` and the `.appinstaller` for the `teststack` channel into the feed folder, bumping the version | Step 8 |
| `docs/engineering.md:76` § Required evidence tiers, tier 11 | "every supported prior schema, idempotent migration scripts, **previous-artifact compatibility**, restore into a new database" — read here as the packaging tier: install v1, upgrade to v2 through the real path | Steps 10–11 and Verification |
| `docs/engineering.md:201-203` § Plan sizing | A plan states its diff estimate first, from a real number | The first line and the measured inventory above |
| `AGENTS.md` § Simplicity rails (one list per concept) | The `winapp`/Developer Mode prerequisite is stated **once**, in `docs/runbook.md`, by [[FND-039]] | The "measured and deliberately not touched" row |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR, under a dated heading in the plan | Step 13 and the `## Simplification pass` heading below |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing → Reviewer |

## Routing

Copied from the ticket body's `## Routing` block; required in the plan document
by `docs/desktop/00-governance-and-workflow/README.md` § Ticket template.

- **Subagent**: `pegasus-release-packager` —
  `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-packaging`
  (`.codex/skills/winui-packaging/SKILL.md`, `microsoft/win-dev-skills` v0.5.0
  `f1028dd5`, vendored and confirmed present 2026-08-24) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, with `BuildAndRun.ps1` beside
  it) for `BuildAndRun.ps1 -SkipRun`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_docs_fetch`) for the App Installer
  update-settings schema
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates FND-048` before every move; a move crosses at most one gated
  boundary). `chore` owes `plan` at `leave-preparing` and `proof` at
  `enter-done`; no `research`, `files` or `checklist`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's thirteen implementation steps in the same order,
with the same ownership and the same file paths. Each names a measured current
value where one exists.

1. **Orient and take.** Read
   `docs/desktop/09-release-update-and-distribution/appinstaller-template.md`
   end to end — it is the whole ticket in one page: § Template (2021 schema) is
   the literal XML, § Rules the template encodes is the five invariants,
   § Hosting requirements carries the D-003 paragraph that deletes every HTTP
   row, and § Known behaviours lists the four surprises. Then read
   `docs/desktop/08-testing/test-uat-stack.md` § Components (`:18-34`) and
   § Lifecycle (`:70-88`). Call `get_doc_gates FND-048`, then `take_ticket`;
   branch `task/<slug>` from `origin/dev` with a worktree under
   `../pegasus-worktrees/<slug>` (`AGENTS.md` § Repository task workflow steps
   1–2). Load `pegasus-desktop`, then `winui-packaging`.
2. **Resolve Case A or Case B before writing anything.** `ls eng` returns *No
   such file or directory* today, so [[REL-003]] (plan handle `DSK-09-03`) has
   not landed as of 2026-08-24. Re-check at execution. If
   `eng/packaging/New-AppInstaller.ps1` exists, take **Case A**: add the
   `teststack` channel to that generator and to
   `eng/packaging/Test-AppInstaller.ps1`'s accepted channel set, and add
   `eng/packaging/fixtures/appinstaller/valid-teststack.xml`. If it does not,
   take **Case B**: write `eng/packaging/teststack/Pegasus.appinstaller`
   directly and write **no** template file and **no** generator — both belong
   to [[REL-003]]. **Record which case applied under a dated note in this
   document.** This ticket owns only the `teststack` channel; pilot and prod
   are [[REL-003]]'s and the production feed is [[REL-008]]'s (plan handle
   `DSK-09-10`).
3. **Produce the `teststack` `.appinstaller` from the template, copied
   literally.** The root element must declare
   `xmlns="http://schemas.microsoft.com/appx/appinstaller/2021"`. The
   2017/2 namespace **silently ignores** `ShowPrompt` and
   `UpdateBlocksActivation`
   (`docs/desktop/09-release-update-and-distribution/README.md:320-321`) and is
   the single most common cause of a non-blocking "blocking" update. Keep
   `<MainPackage Name="CollisionEngineers.Pegasus" ProcessorArchitecture="x64">`,
   the `<UpdateSettings>` block with
   `<OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="true" />`,
   `<AutomaticBackgroundTask />` and
   `<ForceUpdateFromAnyVersion>true</ForceUpdateFromAnyVersion>`, and **omit
   `<Dependencies>`** because the package is self-contained (proposal § 7.1;
   the template's own comment says so at the point of use). Copy the template's
   three comment blocks with it — they carry the `UpdateBlocksActivation`
   requires-`ShowPrompt` rule where it is needed.
4. **Set the `teststack` `Uri` values to the exact served paths.** Both
   `AppInstaller/@Uri` and `MainPackage/@Uri` read
   `\\<host>\<share>\teststack\Pegasus.appinstaller` and
   `\\<host>\<share>\teststack\Pegasus_<ver>_x64.msix`
   (`appinstaller-template.md` § Hosting requirements, D-003 paragraph). App
   Installer **records the `Uri` at install time and re-reads it on every
   check**, so a later change breaks updates for every installed machine —
   never a machine name that may be replaced, never a mapped drive letter.
5. **Set `MainPackage/@Publisher` to the exact certificate subject.** The
   development certificate is produced by [[FND-039]] (plan handle
   `DSK-02-14`) with `winapp cert generate --manifest`, which auto-matches
   `Package.appxmanifest`'s `Identity.Publisher`
   (`.codex/skills/winui-packaging/SKILL.md:29-31`, rule at `:55`). A mismatch
   fails **installation** with a signature error, not a schema error — the
   first row of the skill's troubleshooting table (`:109`). Copy the string
   character for character from [[FND-039]]'s recorded value; do not retype it.
6. **Give the `.appinstaller` its own monotonically increasing `Version`,
   independent of `MainPackage/@Version`,** and record in this document that it
   must increase on **every** publish **including a rollback publish**
   (`appinstaller-template.md` § Rules the template encodes). This is the
   invariant [[REL-003]]'s validator check 3 enforces and [[REL-011]]'s (plan
   handle `DSK-09-13`) rollback test depends on.
7. **Create the local feed as a folder share.** Per-channel subfolders, at
   minimum `teststack/`; read and execute for the test user, write for the
   publishing account only. **Do not substitute an HTTP server**: D-003 puts
   production on SMB and `test-uat-stack.md:32` says the stack rehearses the
   real path rather than an HTTP substitute. If the local host cannot share a
   folder, **stop and record it as an operator blocker** rather than switching
   transport — switching transport would make every subsequent packaging test
   evidence about a mechanism that will never ship.
8. **Publish through the one verb, under [[TEST-017]]'s contract.** Measured:
   `grep -n 'Publish-Feed' scripts/Invoke-LocalDevelopment.ps1` returns
   nothing; the `[ValidateSet('Start', 'Status', 'Smoke', 'Stop', 'Reset')]` is
   at `:3` and the `switch ($Action)` at `:1496` has exactly five arms
   (`:1497`, `:1501`, `:1528`, `:1532`, `:1545`) closing at `:1571`. If
   [[TEST-017]] (plan handle `DSK-08-17`) has landed, **supply only the
   `teststack` channel path and change no behaviour**. If it has not, add
   `'Publish-Feed'` to the `ValidateSet` — **extend, never narrow**, because
   that set is the contract other runbooks call — and add one arm before
   `:1571` under [[TEST-017]]'s contract, restated verbatim so the two cannot
   drift: copy the freshly packaged `.msix` and its `.appinstaller` into the
   channel folder, bump the `.appinstaller` `Version` on every publish
   including a rollback publish, leave `Uri` equal to the served path, and keep
   the existing `-Action` values working unchanged. Two mechanical
   requirements the file imposes: the script sets `Set-StrictMode`-style
   discipline with `$ErrorActionPreference = 'Stop'` at `:15`, and every
   state-changing arm takes the lifecycle mutex (`$mutex = Enter-LifecycleMutex`,
   released in the `finally` at `:1573-1583`) — a new arm that writes to the
   feed must take it too, or two publishes can interleave. **Record in this
   document which case applied.**
9. **Build and package v1.**
   `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj /p:Configuration=Release -SkipRun`
   (`BuildAndRun.ps1:26` declares `[switch]$SkipRun`), then
   `winapp package <build-output-dir> --cert ./devcert.pfx --self-contained`
   (`SKILL.md:16`). Publish it to the `teststack` folder and confirm the
   `.msix` SHA-256 matches what [[REL-002]]'s (plan handle `DSK-09-02`) release
   manifest records. Use the build-output directory the script **reports**;
   guessing it is the most common way this step fails.
10. **Operator step — trust and install.** On a Windows 11 x64 workstation,
    trust the development certificate once
    (`winapp cert install ./devcert.pfx`, elevated — `SKILL.md:35-37`), then
    open the `teststack` `.appinstaller` **from the share** and install. Hand
    back `Get-AppxPackage CollisionEngineers.Pegasus` and
    `Get-AppxPackageAutoUpdateSettings CollisionEngineers.Pegasus` showing
    on-launch checks. Installing from the share rather than from a local copy
    is what makes `Package.CheckUpdateAvailabilityAsync` return anything other
    than `Unknown` — a side-loaded `.msix` is invisible to it
    (`appinstaller-template.md` § Known behaviours), which is why [[FND-045]]'s
    (plan handle `DSK-04-09`) startup orchestrator cannot be exercised without
    this ticket.
11. **Operator step — the v1 → v2 mandatory update.** Build and publish v2 with
    a higher `MainPackage/@Version` **and** a higher `.appinstaller` `Version`,
    relaunch, and confirm the App Installer prompt appears and activation is
    blocked until the update is taken. Evidence: a screenshot of the prompt and
    `Get-AppxPackage` version before and after. If **no** prompt appears,
    diagnose in this order: (a) the namespace — 2021 or 2017/2; (b)
    `Get-AppxPackageAutoUpdateSettings` for a CSP or PowerShell override, which
    takes precedence over the `.appinstaller`
    (`appinstaller-template.md` § Known behaviours, "Settings precedence").
12. **Record the fail-open behaviour as expected, not as a defect.** Rename or
    remove the `.appinstaller` from the share and relaunch — **the app still
    launches**. That is documented behaviour
    (`appinstaller-template.md` § Known behaviours, "Fail-open"), and the
    fail-closed layer is the gateway minimum-version gate owned by [[GWY-023]]
    (plan handle `DSK-04-06`). Write that sentence into this document so a
    later reader does not open a defect against it.
13. **Simplification pass** over this branch's own diff, recorded under a dated
    `## Simplification pass` heading in this document, then open the PR into
    `dev`.

## Verification

Evidence tier from the body: **Tier 11 — Migration/recovery**
(`docs/engineering.md:76`), read here as the packaging tier — previous-artifact
compatibility is **demonstrated on a real machine**, not asserted. The body is
explicit: "A packaging step that only produces a file does not satisfy this
tier." Proof types: `command-log` and `visual` (the update prompt screenshot).

| Command / observation | Expected | Becomes evidence as |
| --- | --- | --- |
| `[xml]$a = Get-Content <feed>\teststack\Pegasus.appinstaller; $a.AppInstaller.xmlns` | `http://schemas.microsoft.com/appx/appinstaller/2021` | `proof` (command-log) — the first thing a reviewer should be able to see |
| `$a.AppInstaller.UpdateSettings.OnLaunch \| Select-Object HoursBetweenUpdateChecks, ShowPrompt, UpdateBlocksActivation` | `0`, `true`, `true` | `proof` (command-log) |
| `$a.AppInstaller.Uri`, `$a.AppInstaller.MainPackage.Uri` | byte-identical to the share paths the workstation installed from | `proof` (command-log) |
| `Get-AppxPackage CollisionEngineers.Pegasus \| Select-Object Name,Version,SignatureKind` | the installed v1 version, `SignatureKind` = `Developer` | `proof` (command-log) |
| `Get-AppxPackageAutoUpdateSettings CollisionEngineers.Pegasus` | on-launch checks enabled, `HoursBetweenUpdateChecks` 0, **no CSP override** | `proof` (command-log) |
| Relaunch after publishing v2 | the App Installer update prompt appears; activation is blocked until it is taken; `Get-AppxPackage` then reports v2 | `proof` (visual — the prompt screenshot — plus command-log for the two versions) |
| Rename the `.appinstaller` away and relaunch | the app **launches** (fail-open) | `proof` (command-log), recorded as expected |
| `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Status` | exit code `0`; the existing components still reported healthy | `proof` (command-log) — proves the `ValidateSet` was extended and not narrowed |
| `git diff --name-only` at PR time | exactly the files in the inventory for the case that applied; **no** `src/Pegasus.Web`, no `.github/workflows/ci.yml`, no `scripts/Build-ReleaseArtifacts.ps1`, no pilot or prod `.appinstaller` | `proof` (command-log) |

**Stated limit, required in the proof:** one v1 → v2 rehearsal proves the
schema, the prompt and the blocking. It proves **nothing** about interrupted
updates, signature failure, no-admin install or rollback — those are
[[TEST-010]]'s (plan handle `DSK-08-10`) scenarios in
`eng/packaging/Test-Package.ps1`.

## Risks / open questions

- **Risk — the 2017/2 schema is emitted by tooling and nobody notices.** Visual
  Studio emits 2017/2, which silently ignores the two enforcing attributes, so
  the file looks right and the update does not block. Mitigation: step 3 copies
  the template literally, the first Verification row asserts the namespace, and
  [[REL-003]]'s validator check 1 fails on anything else.
- **Risk — an HTTP feed is substituted for convenience.** It would be easier
  and it would invalidate every packaging result. Mitigation: step 7 makes an
  unshareable host an **operator blocker**, not a licence to switch transport.
  D-003 and `test-uat-stack.md:32` are both cited at the point of decision.
- **Risk — two `Publish-Feed` implementations.** The body's Traps make a second
  verb or a sibling script a stop condition. Mitigation: step 8 branches on
  whether [[TEST-017]] has landed and restates [[TEST-017]]'s contract verbatim
  in the else-branch so the two cannot drift.
- **Risk — a new `-Action` arm bypasses the lifecycle mutex.** Measured:
  `$mutex = $null` at `:1494`, taken per-arm, released in the `finally` at
  `:1573-1583`. A publish that skips it can interleave with a `Reset`.
  Mitigation: step 8 states the requirement explicitly.
- **Risk — the `Uri` is changed after machines have installed.** App Installer
  records it at install time; changing it silently breaks updates for every
  installed workstation. Mitigation: step 4 fixes the path shape and the
  Verification asserts byte-identity; path stability is an `appinstaller-template.md`
  hosting requirement, not a preference.
- **Scope boundary, not an open question — the ordering against [[TEST-017]].**
  The body's *Source of truth* lists `DSK-08-17` as a dependency (it owns
  `Publish-Feed`), while the board records FND-048 as **blocking** [[TEST-017]]
  (it supplies the `teststack` channel that verb publishes into). Both readings
  are true and the body's step 8 already handles either order. No question is
  opened; the execution note is simply to run step 2 and step 8's checks and
  record which case applied.
- **Scope boundary, not an open question — the pilot and prod templates, the
  production feed, the version generator and the CI lane.** Owned by
  [[REL-003]], [[REL-008]], [[REL-002]] and [[REL-005]] respectively. This
  ticket edits none of them.
- **Scope boundary, not an open question — the `winapp` / Developer Mode
  prerequisite in `docs/runbook.md`.** [[FND-039]] step 11 is its single owner
  and names this ticket among those that cite rather than restate it. Add it
  here **only** if [[FND-039]] has not landed, and say so in the proof.
- **Operator dependency, not an open question.** Steps 10 and 11 need an
  elevated terminal and a dedicated Windows 11 x64 workstation
  (`test-uat-stack.md` § Machine prerequisites); the ticket carries the
  `needs-operator` label for exactly that. D-002 and D-003 were both decided on
  2026-08-23 and `docs/desktop/README.md` records that **no open decisions
  remain**.
- **Open questions**: none. No `open-questions` document is created — the
  ticket body does not instruct one, and every unknown above is a scope
  boundary owned by a named sibling ticket, which
  `docs/desktop/00-governance-and-workflow/README.md` § 3 makes a boundary
  rather than a question.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass
over this branch's own diff before the PR, recorded here under a dated heading.
This branch adds an XML asset and PowerShell alongside documentation, so
`n/a — docs-only` does not apply._
