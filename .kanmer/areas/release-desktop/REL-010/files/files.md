# Files — REL-010

Surveyed on 2026-08-24 against the fork at branch `task/desktop-plan-segmentation`. This is
mostly a **proving** ticket: two documentation edits plus one scenario added to an existing
test script, and the rest is observed behaviour captured as evidence.

## Where the change lands

| Path | Why |
|---|---|
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R3 | **Edited, four changes.** Step 1 names the **real** admin control `DSK-04-06` (board `GWY-023`) ships, replacing "the gateway administration surface"; the recorded `CheckUpdateAvailabilityAsync` `Required` vs `Available` semantics are added so the two layers are not confused in the write-up; the "does not prove" section names the CSP/Group Policy override case with `Get-AppxPackageAutoUpdateSettings` as the check; and R3 is marked **proven** with its date. Breaks if step 1 stays generic: someone guesses at the control during an incident. |
| `eng/packaging/Test-Package.ps1` (created by `DSK-08-10`, board `TEST-010`) | **Extended, one scenario.** Set the minimum version, launch the old client, assert the update-required screen by `AutomationId` (`Update.Required.Now`), and assert that every `/api/v1` call from that client returns `urn:pegasus:problem:client-unsupported`. Breaks if it asserts only the screen: a cosmetic screen with a working API is the failure this scenario exists to catch. |
| `docs/operations.md` | **Edited, within the existing desktop release row.** The minimum-version change is recorded with who, when and reason — R3 step 4. The `### Desktop releases` table is created by `DSK-09-18` (board `REL-016`) and the first row by `DSK-09-11` (board `REL-009`); this ticket adds to that row rather than creating a table. |

## Context files

Read these before running anything. Each names a value or a rule the test depends on.

| Path | What it tells the implementer |
|---|---|
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R3 | The four steps, the preconditions, the rollback (lower the minimum through the same admin setting) and the "does not prove" clause this ticket must extend. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R6 step 1 | The **same** admin action used as an emergency block. Whatever R3 records about the control, R6 inherits — so name it once, correctly. `DSK-09-14` (board `REL-012`) owns R6. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 3 | "Two-layer enforcement": the package layer uses the 2021-schema `OnLaunch` attributes, the application layer is the gateway compatibility gate; "**The package check fails open when the feed is unreachable; the gateway gate fails closed after a short cached window. Both are required.**" Also "Order of deployment" — the minimum client version is raised **last** — and "Emergency path" — a defective client is blocked by raising the minimum, and "there is no secret bypass". |
| `docs/desktop/04-auth-session-update-and-startup/README.md:175-179` | The startup sequence's step 5: `GET /api/v1/client-compatibility` (anonymous), and the `/api/v1` group rejecting requests whose `X-Pegasus-Client-Version` is below the minimum with problem type `urn:pegasus:problem:client-unsupported`. These are the literal strings the scripted assertion checks. |
| `docs/desktop/04-auth-session-update-and-startup/README.md:210` | The state table row: "Client unsupported | problem `urn:pegasus:problem:client-unsupported` (+ `minimumVersion`) | Update-required screen; launch `.appinstaller`; no work". The `minimumVersion` field is part of the response and is worth asserting. |
| `docs/desktop/04-auth-session-update-and-startup/README.md:237` | `DSK-04-06`'s (board `GWY-023`) acceptance: the minimum client version is a **DB-backed Administrator setting with an audit entry** and a config bootstrap fallback; the setting change is audited. This is the control R3 step 1 must name. |
| `docs/desktop/06-ui-design/screen-specs.md:99-107` | The "Update required / Blocked" screen: full-window, no rail; title "Update required"; the current and minimum versions shown **as values**; primary "Update now", secondary "Sign out"; AutomationIds `Update.Required.Now`, `Update.Required.SignOut`, `Blocked.Reason`. The scripted assertion locates `Update.Required.Now`. |
| `docs/desktop/06-ui-design/screen-specs.md:95` | The sign-in screen's own state list, including "client unsupported → Update required screen" — the path the test drives. |
| `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Known behaviours | Three rules this ticket depends on: the launch check **fails open** when the feed is unreachable; **settings precedence** is CSP > PowerShell/App Installer file > embedded file, with `Get-AppxPackageAutoUpdateSettings` as the diagnostic; and `Package.CheckUpdateAvailabilityAsync` must be called on the package from `PackageManager.FindPackageForUser`, not `Package.Current`, with `Required` meaning the `.appinstaller` policy blocks activation. |
| `docs/desktop/08-testing/test-uat-stack.md:131` | UAT scenario 12, "Obsolete desktop version blocked and updates successfully", to be run on the **stack with a local feed** *and* on the **pilot with the real feed**, with evidence "Update-required screen, `Get-AppxPackage` version after update". |
| `docs/desktop/08-testing/test-uat-stack.md:84` and § Lifecycle | `Publish-Feed` bumps the version and is "used by the packaging tests to simulate mandatory updates and rollbacks"; and "Failure injection already in the script (gateway unavailable, slow responses) is reused for the connectivity … scenarios" — the precedent for making the feed unreachable in a controlled way. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R9 step 7 | "Do not raise the gateway minimum version while a pilot user is known to be away, or they are locked out of work until they return." D-003's feed is LAN/VPN-only, so this is an operational hazard, not a theoretical one. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 7 | The App Insights 0.1 GB/day cap (PLAT-034) can hide blocked-client telemetry for most of the day — so the evidence for this ticket is screens and API responses, not dashboards. |

## Ripple effects

- **`DSK-09-14` (board `REL-012`)** owns R6, whose step 1 is the same admin action. Whatever
  this ticket records about the control and its audit entry, R6 reuses; hand it the exact
  screen or endpoint name.
- **`DSK-09-13` (board `REL-011`)** depends on the **rollback** half proven here: R4 step 2
  lowers the minimum before publishing a rollback, and a rollback published while the gateway
  still rejects the older version leaves every workstation blocked.
- **`DSK-08-10` (board `TEST-010`)** owns `eng/packaging/Test-Package.ps1`; this ticket adds
  a scenario to it rather than creating a second script.
- **`DSK-09-15` (board `REL-013`)** writes the operator-facing sentence for the blocked
  state ("If you see 'Update required', close and reopen"); the screen name and the single
  operator action must match what this ticket observed.
- **`DSK-04-06` (board `GWY-023`)** and **`DSK-04-09` (board `FND-045`)** own the endpoint,
  middleware, setting and screen. A defect found here is a `fix` ticket against them, not a
  patch from this ticket.
- **`docs/operations.md`** desktop release row gains the minimum-version change record; the
  table is `DSK-09-18`'s (board `REL-016`).
- **No OpenAPI or generated-client ripple from this ticket.** The compatibility endpoint and
  its problem type are `DSK-04-06`'s contribution to `openapi/pegasus-v1.json` and the
  generated client; this ticket only asserts against them.

## Out of scope

Recorded so the reviewer sees these were decisions, not oversights. Each is a Guardrail in
the ticket body.

- **Any Azure write.** The minimum-version setting is **application data in the gateway's
  database**, changed through the audited admin surface — not an Azure resource change. Do
  not touch Container App configuration.
- **The gateway's compatibility endpoint or middleware.** `DSK-04-06` (board `GWY-023`) owns
  them; a defect found here is raised as a `fix` ticket, not patched from this ticket.
- **The update-required screen itself.** `docs/desktop/06-ui-design/screen-specs.md:99-107`
  is area 06's specification and `DSK-04-09` (board `FND-045`) implements it. If the shipped
  AutomationIds differ, record the real ones — do not change the screen from here.
- **Raising the minimum version in production while a pilot user is off-network.** Forbidden
  by R9 step 7.
- **A secret bypass.** Area plan § 3 Emergency path: "there is no secret bypass". Neither
  R3 nor its test may introduce one.
- **Creating a second packaging test script.** The scenario extends
  `eng/packaging/Test-Package.ps1` (`DSK-08-10`, board `TEST-010`).
- **Relying on telemetry as evidence.** The App Insights 0.1 GB/day cap (PLAT-034) can hide
  the window; the evidence is screens and API responses.
