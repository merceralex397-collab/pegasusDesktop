# Files — REL-011

Surveyed on 2026-08-24 against the fork at branch `task/desktop-plan-segmentation`. Like
`REL-010`, this is mostly a **proving** ticket: two documentation edits and one scenario
added to an existing script; the rest is observed behaviour captured as evidence.

## Where the change lands

| Path | Why |
|---|---|
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R4 | **Edited, four changes.** Step 2 is written as the **first** operational action (lower the minimum before publishing); step 3 gains the quoted `ForceUpdateFromAnyVersion` sentence from official documentation; step 5's uninstall/reinstall fallback records that it was proven once on a test machine; R4 is marked **proven** with its date, and its "does not prove" clause is stated verbatim in substance. Breaks if step 2 stays third: a rollback published while the gateway still rejects the older version leaves every workstation blocked. |
| `eng/packaging/Test-Package.ps1` (created by `DSK-08-10`, board `TEST-010`) | **Extended, one scenario.** The downgrade path: publish a rollback manifest for `<prev-ver>` with a **higher** `.appinstaller` `Version` and `ForceUpdateFromAnyVersion="true"`, relaunch, assert `Get-AppxPackage` moves down. Breaks if omitted: a regression in the downgrade path is found during an incident instead of in CI. |
| `docs/operations.md` | **Edited, one row added.** A rollback row in the desktop release table — channel, `.appinstaller` version, package version, who approved, when. The table is created by `DSK-09-18` (board `REL-016`); add a row, do not create a second table. |

## Context files

Read these before rehearsing anything. Each carries a rule that inverts against intuition or
a value the assertions depend on.

| Path | What it tells the implementer |
|---|---|
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R4 | The six steps and the "does not prove" clause. The two that matter most: step 2 — "If the minimum client version was raised to the defective version, **lower it first** … so downgraded clients are accepted" — and step 3 — republish the previous signed `.msix`, already on the feed, under a **new, higher** `.appinstaller` `Version` with `ForceUpdateFromAnyVersion="true"` and `MainPackage Version=<prev-ver>`. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R7 | The uninstall/reinstall fallback R4 step 5 delegates to: Settings → Apps → Pegasus → Uninstall (or `Remove-AppxPackage`), then repeat the install steps. It also records that "local preferences live in the package's `ApplicationData` and are removed with the package" — which is what makes the fallback safe to rehearse. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § Conventions | The approval phrase `FEED PUBLISH GRANTED <channel> <ver>` is **proposed** and is confirmed by `DSK-09-11` (board `REL-009`) step 2. `MERGE AUTH GRANTED` keeps its single meaning — the `dev` → `main` promotion — and must not be reused for a rollback publish. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R9 step 2 | "Keep at least the previous package per channel; **never overwrite a published `.msix`** (a new version always means a new file name). Only `Pegasus.appinstaller` is replaced in place, and its `Version` attribute must increase every time." This rule is the only reason a rollback is possible at all. |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Template rules | "`Version` of the `.appinstaller` must increase on every publish, **including a rollback publish**; it is independent of the package version." The template's own comment says it plainly at the `ForceUpdateFromAnyVersion` line: "Allow the feed to move a workstation to a LOWER version (rollback)." |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Validator outline check 8 | "(Rollback mode) `MainPackage/@Version` lower than the previous is allowed **only** when the invocation passes `-Rollback` and `ForceUpdateFromAnyVersion` is `true`." The switch is the audit trail that a human meant the downgrade. |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Known behaviours | "**Downgrade** needs `ForceUpdateFromAnyVersion`; without it App Installer only moves to higher versions" — the rule that makes a rollback silently do nothing when it is missing. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 3 "Known-good previous package" | The previous package is retained on the feed for every channel, and rollback republishes it with a higher `.appinstaller` `Version` and `ForceUpdateFromAnyVersion="true"`. |
| `docs/desktop/08-testing/test-uat-stack.md:84` and § Lifecycle | `Publish-Feed` bumps the version and is "used by the packaging tests to simulate mandatory updates **and rollbacks**" — the mechanism step 6's stack rehearsal drives. |
| `docs/desktop/04-auth-session-update-and-startup/README.md:175-179`, `:210` | `GET /api/v1/client-compatibility`, the `X-Pegasus-Client-Version` middleware and the `urn:pegasus:problem:client-unsupported` problem type — needed to confirm that a **downgraded** client is now accepted rather than refused. |
| `eng/packaging/Test-AppInstaller.ps1` (created by `DSK-09-03`, board `REL-003`) | The validator and its `-Rollback` switch; run it **both** ways so the refusal without the switch is captured as evidence that the guard exists. |
| `eng/packaging/Publish-DesktopRelease.ps1` (created by `DSK-09-10`, board `REL-008`) | The publish script: package first, `.appinstaller` last, never overwriting a published `.msix`. A rollback republishes only the manifest, because the package is already there. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R9 step 7 | Off-network clients will not see the rollback until they return to the LAN or VPN — a direct consequence of D-003's feed, and something to state rather than discover. |

## Ripple effects

- **`DSK-09-12` (board `REL-010`)** owns R3 and its rollback path; R4 step 2 calls into it.
  The recovery time measured there is the number this ticket quotes rather than re-measuring.
- **`DSK-09-14` (board `REL-012`)** owns R6, whose step 1 offers rollback as the option when
  a fix is not yet built ("publish the last good package as a rollback (R4) so clients can
  still work"). R6 inherits whatever R4 records.
- **`DSK-08-10` (board `TEST-010`)** owns `eng/packaging/Test-Package.ps1`; the downgrade
  scenario extends it rather than creating a second script.
- **`DSK-09-10` (board `REL-008`)** must have held the never-overwrite rule for the previous
  package to exist; a rollback rehearsal that finds only one `.msix` on the feed is evidence
  of a defect there.
- **`DSK-09-15` (board `REL-013`)** documents the uninstall/reinstall path in operator words;
  the transcript from step 8 is what makes that page accurate.
- **`docs/operations.md`** gains a rollback row; the table is `DSK-09-18`'s (board `REL-016`).
- **No OpenAPI, generated-client or build ripple.** No endpoint, no contract, no package
  reference; the rollback ships bytes that already exist.

## Out of scope

Recorded so the reviewer sees these were decisions, not oversights. Each is a Guardrail in
the ticket body.

- **Rebuilding or re-signing the previous package.** Rollback republishes the **same signed
  artefact**; the hash must match the original release manifest. No `-Sign` run happens here.
- **Overwriting a published `.msix`.** Never. Only `Pegasus.appinstaller` is replaced in
  place, with a higher `Version`.
- **Owning the minimum-version control.** `DSK-04-06` (board `GWY-023`) owns the setting and
  `DSK-09-12` (board `REL-010`) owns R3; this ticket calls the lowering path, it does not
  implement it.
- **Any Azure write.** The rollback is a file copy to the in-house share and an application
  setting change in the gateway's database.
- **Fixing the defect that caused the rollback.** Step 9 opens a `fix` ticket with the R10
  diagnostics bundle attached; the fix itself is that ticket's.
- **Claiming the data is correct.** R4 does not prove that data written by the defective
  version is correct — that is an audit/history question for area 10.
- **Reusing `MERGE AUTH GRANTED`.** It has one meaning; a rollback publish uses
  `FEED PUBLISH GRANTED <channel> <prev-ver>`.
