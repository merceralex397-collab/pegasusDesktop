# Research — REL-010: which of the two enforcement layers actually stops an obsolete client

## Question

What does raising the gateway's minimum client version actually do to a workstation left
behind, how is that proven rather than assumed, and which layer fires under which network
condition?

## Current behaviour

**Neither layer exists yet, and the web application has no equivalent.** A browser always
runs the version the Container App serves, so "an obsolete client" is not a state the
current system can be in. Verified on 2026-08-24: `.github/workflows/ci.yml` has no
packaging lane, `ls eng` returns nothing, and there is no client-version header or
compatibility endpoint in the repository.

The two layers this ticket exercises are built elsewhere and are named precisely:

- **Gateway side** — `DSK-04-06` (board `GWY-023`), "Minimum client version as a DB-backed
  Administrator setting (+ audit) with config bootstrap fallback;
  `GET /api/v1/client-compatibility`; `/api/v1` middleware on `X-Pegasus-Client-Version`"
  (`docs/desktop/04-auth-session-update-and-startup/README.md:237`). Its § 5 startup
  sequence at `:175-179` records that the `/api/v1` group "rejects requests whose
  `X-Pegasus-Client-Version` is below the minimum with problem type
  `urn:pegasus:problem:client-unsupported`".
- **Desktop side** — `DSK-04-09` (board `FND-045`), the startup orchestrator with the
  update check, the fail-closed compatibility cache and session restore. The screen it
  shows is specified at `docs/desktop/06-ui-design/screen-specs.md:99-107`: "Update
  required / Blocked — new (proposal §9)", full-window with no rail, title "Update
  required", the current and minimum versions **as values**, one primary action "Update
  now" and a secondary "Sign out", with AutomationIds `Update.Required.Now`,
  `Update.Required.SignOut` and `Blocked.Reason`.

**No parity-matrix row covers this.** `docs/desktop/01-inventory-and-parity/parity-matrix.md`
runs `PAR-01`…`PAR-46` over Razor page models. The nearest is `PAR-01` (sign-in), whose
desktop column already anticipates "connectivity and update-required states" — but there is
no row for version enforcement, because the web application has nothing to enforce.

## Findings

- **The two layers fail in opposite directions, and that is the design.** Area plan § 3:
  "The package check fails open when the feed is unreachable; the gateway gate fails closed
  after a short cached window. Both are required." So a test run with a reachable feed
  proves the **package** layer, and a test run with an unreachable feed proves the
  **gateway** layer. Testing only one proves half the design.
- **R3 has four steps and one precondition set**, and its rollback is the same admin
  setting lowered — `runbooks.md` § R3. Its "does not prove" is specific: "App Installer
  behaviour on a machine whose policy overrides update settings (see R7)".
- **CSP and Group Policy override the `.appinstaller`.** `appinstaller-template.md`
  § Known behaviours: "**Settings precedence**: CSP (Intune) settings override PowerShell
  and `.appinstaller` settings, which override an embedded `.appinstaller`; check
  `Get-AppxPackageAutoUpdateSettings` when an update does not apply." This is exactly the
  case R3 cannot prove and must therefore name.
- **`Package.Current.CheckUpdateAvailabilityAsync` fails with access denied** — area plan
  § 7 and `appinstaller-template.md` § Known behaviours. The client must call it on the
  package returned by `PackageManager.FindPackageForUser`. `Required` means the
  `.appinstaller` policy blocks activation; `Available` means an update exists but does not.
  `DSK-04-09` (board `FND-045`) owns that code; R3's write-up must record the semantics so
  the two layers are not confused.
- **The admin control is a DB-backed Administrator setting with an audit entry**, changed
  with a reason — not a configuration file and not an Azure resource. Its exact surface is
  `DSK-04-06`'s (board `GWY-023`) to deliver, and R3 step 1 must name the real screen or
  endpoint rather than "the admin surface".
- **The Test/UAT stack already has the scenario.**
  `docs/desktop/08-testing/test-uat-stack.md:131` scenario 12: "Obsolete desktop version
  blocked and updates successfully | Stack (local feed), **pilot** (real feed) |
  Update-required screen, `Get-AppxPackage` version after update". The stack's `Publish-Feed`
  verb (`:84`) bumps the version and is "used by the packaging tests to simulate mandatory
  updates and rollbacks".
- **The stack's failure injection is reusable.** `test-uat-stack.md` § Lifecycle: "Failure
  injection already in the script (gateway unavailable, slow responses) is reused for the
  connectivity and provider-timeout scenarios." Making the **feed** unreachable is the
  analogous manoeuvre this ticket needs and should be recorded as how it was done.
- **Off-network users must not be locked out.** R9 step 7 and area plan § 7: "Do not raise
  the gateway minimum version while a pilot user is known to be away, or they are locked out
  of work until they return."
- **Area 04's own exit gate already states the pair of assertions** this ticket proves —
  `docs/desktop/04-auth-session-update-and-startup/README.md:220`: "Packaging test: install
  v1, publish v2 `.appinstaller` with `UpdateBlocksActivation`, relaunch → prompt → updated;
  gateway test: old `X-Pegasus-Client-Version` → `client-unsupported`".

### Facts

Verified by reading this repository on 2026-08-24.

| Fact | Source |
| --- | --- |
| The gateway gate's problem type is `urn:pegasus:problem:client-unsupported`, carried on `/api/v1` via `X-Pegasus-Client-Version` middleware; the endpoint is `GET /api/v1/client-compatibility` | `docs/desktop/04-auth-session-update-and-startup/README.md:175-179`, `:210`, `:237` |
| The minimum client version is a DB-backed Administrator setting with an audit entry and a config bootstrap fallback | same file, `:237` (`DSK-04-06`, board `GWY-023`) |
| The update-required screen's title, content, actions and AutomationIds (`Update.Required.Now`, `Update.Required.SignOut`, `Blocked.Reason`) | `docs/desktop/06-ui-design/screen-specs.md:99-107` |
| Sign-in state "client unsupported → Update required screen" | same file, `:95` |
| Package layer fails open, gateway gate fails closed after a short cached window; both required | `docs/desktop/09-release-update-and-distribution/README.md` § 3 |
| R3's four steps, rollback and "does not prove" | `docs/desktop/09-release-update-and-distribution/runbooks.md` § R3 |
| R6 step 1 uses the same admin action as an emergency block | same file, § R6 |
| CSP/Intune overrides PowerShell overrides `.appinstaller` overrides embedded; `Get-AppxPackageAutoUpdateSettings` is the check | `appinstaller-template.md` § Known behaviours |
| `Package.Current.CheckUpdateAvailabilityAsync` fails with access denied; use `PackageManager.FindPackageForUser`; `Required` means the policy blocks activation | same file, § Known behaviours; area plan § 7 |
| UAT scenario 12 runs on the stack with a local feed **and** on the pilot with the real feed | `docs/desktop/08-testing/test-uat-stack.md:131` |
| `Publish-Feed` bumps the version and is used to simulate mandatory updates and rollbacks | same file, `:84` |
| Do not raise the minimum version while a pilot user is off-network | `runbooks.md` § R9 step 7; area plan § 7 |

### Assumptions

- **A-09-25 — the minimum-version setting can be changed and reverted freely on the Test/UAT
  stack.** The stack runs a local gateway process with its own LocalDB database
  (`test-uat-stack.md` § Components), so the setting is stack-local.
  *Confirmed by*: raising it, observing the refusal, lowering it, and observing recovery at
  step 10.
  *Breaks if wrong*: the rehearsal cannot be repeated and the pilot becomes the first
  attempt — exactly what L-02 exists to prevent.
- **A-09-26 — the feed can be made unreachable on the stack without breaking the gateway.**
  The two must be independently controllable, or the fail-open and fail-closed halves cannot
  be tested separately.
  *Confirmed by*: renaming or ACL-denying the stack's feed folder while the gateway keeps
  answering `GET /api/v1/client-compatibility`; record the exact manoeuvre used.
  *Breaks if wrong*: the test proves the wrong layer. Mitigation: assert in the same run
  that the gateway is still reachable, so an unreachable-feed test cannot be confused with
  an unreachable-gateway test.
- **A-09-27 — the update-required screen is reachable by `AutomationId` in the packaging
  suite.** `winui-ui-testing` drives it, and the ids are fixed at
  `screen-specs.md:106` (`Update.Required.Now`, `Update.Required.SignOut`,
  `Blocked.Reason`).
  *Confirmed by*: the scripted scenario locating `Update.Required.Now`.
  *Breaks if wrong*: the check degrades to a screenshot a human must read. Mitigation: if
  the ids differ in the shipped app, record the real ones here rather than changing the
  screen — the screen spec is area 06's.
- **A-09-28 — a "routine workflow" for step 8 is agreed.** R3 step 3 says only "Confirm a
  current client logs in normally".
  *Confirmed by*: naming the workflow in this document before the run — sign in, open a
  case, save an edit — so the evidence is comparable between runs.
  *Breaks if wrong*: the positive case becomes "it looked fine", which proves nothing.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered for the responsibility
this ticket exercises: *refusing work to a client version below the minimum*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | The minimum client version is one setting that binds every client and that several administrators can see and change. `DSK-04-06` (board `GWY-023`) makes it a DB-backed Administrator setting with an audit entry, changed with a reason — which is only possible centrally. |
| Unattended execution — must it run with every desktop closed? | **no** | Raising or lowering the minimum is an attended admin action; the enforcement itself runs inside request handling, not as a background job. |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | No secret is involved. The client sends `X-Pegasus-Client-Version`, which is not a credential. |
| Public callback — must an external service call a stable public endpoint? | **no** | `GET /api/v1/client-compatibility` is called **by** the client, anonymously, before a session exists. Nothing external calls in. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | This is the definitional case. The package layer **fails open**, and CSP or Group Policy can override App Installer's settings entirely (`appinstaller-template.md` § Known behaviours), so a rule enforced only in the client is a rule a managed workstation can switch off. The gate must hold independently of the client. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement exists and none is claimed. The case rests on questions 1 and 5, not on performance. |

Two "yes" answers — shared authority and central enforcement — which is exactly why the
minimum-version gate lives in the gateway (ADR-0103, ADR-0105) and not in the package. This
ticket **confirms an existing placement**; it makes no new one and requires no Azure write:
the setting is application data in the gateway's database, changed through the audited admin
surface.

## Implications

- **Test both network conditions or prove nothing.** With the feed reachable, App Installer
  updates and the gateway gate never fires; with the feed unreachable, the launch check is
  skipped and the gateway's refusal is the only thing standing. Step 4 runs both, and
  asserts in the unreachable-feed case that the **gateway** is still reachable so the two
  failure modes are not conflated.
- **Name the real admin control.** R3 step 1 currently says "In the gateway administration
  surface (area 04 admin setting, DB-backed, audited)". Replace it with the actual screen or
  endpoint `DSK-04-06` (board `GWY-023`) ships; a runbook that says "the admin surface" is a
  runbook someone will guess at under pressure.
- **Assert the refusal at both levels.** The screen (`Update.Required.Now` by
  `AutomationId`) **and** the API response (`urn:pegasus:problem:client-unsupported` on every
  `/api/v1` call from that client). A screen without the refusal could be cosmetic.
- **Script it, then repeat it by hand.** Tier 7 evidence is observed application behaviour;
  the scripted scenario in `DSK-08-10` (board `TEST-010`) catches regressions, and the
  manual pilot confirmation is what satisfies the tier. Neither replaces the other.
- **Prove the recovery, and time it.** Step 10's lowering of the minimum is the rollback
  path R6 depends on; record the observed recovery time rather than asserting one.
- **Name the case R3 cannot prove.** CSP/Group Policy override is real and is R7's to
  record; `Get-AppxPackageAutoUpdateSettings` is the check, and an override found is
  **recorded**, not worked around.
- **Do not raise the minimum while a pilot user is off-network.** They are locked out until
  they return — a real operational hazard, not a theoretical one, because D-003's feed is
  LAN/VPN-only.

## Open questions

- **None that block.** The gateway surface, the problem type, the header name and the
  AutomationIds are all already specified in areas 04 and 06 and are read at implementation
  time; the four assumptions above are each settled by running the rehearsal. If a defect is
  found in the compatibility endpoint or middleware, the correct action is a `fix` ticket
  against `DSK-04-06` (board `GWY-023`) — not a patch from here, and not an open question.
  **No `open-questions` document is created.**
