# Research — FND-046: role-aware shell driven by `StaffAccessRight`

## Question

How does the web application decide today whether an operator sees the
Administration surface, which Core code owns that decision, and what must the
desktop shell reuse so that hiding a command stays a usability affordance and
never becomes the security boundary?

## Current behaviour

The web application makes the decision in two independent places, and the
split is the precedent this ticket must preserve.

- **Render-side hide.** `src/Pegasus.Web/Pages/Shared/_Layout.cshtml:93-98`
  wraps the Administration rail link in
  `@if (User.IsInRole(Pegasus.Core.Identity.StaffRoleNames.Administrator))`.
  When the test fails the anchor is not emitted at all — the item is **absent
  from the DOM**, not disabled. The rail order above it is fixed by the design
  authority comment at `_Layout.cshtml:53-55`: Dashboard, Inbox, Upload,
  Queues, Cases, Operations, Administration, then the user group at
  `_Layout.cshtml:101-110`.
- **Server-side enforcement.** `src/Pegasus.Web/Pages/Administration/Index.cshtml.cs:9`
  carries `[Authorize(Policy = StaffRoleNames.Administrator)]` and
  `:27-33` re-derives the actor and calls
  `StaffAuthorization.Require(actor, StaffAccessRight.ManageStaffAccounts)`,
  returning `Forbid()` when the actor cannot be built at all.
- **The claims→actor seam.** `src/Pegasus.Web/Pages/StaffPageModel.cs:11-15`
  is `TryGetActor`, which calls
  `StaffActorFactory.TryCreate(User.FindFirstValue(ClaimTypes.NameIdentifier),
  User.FindAll(ClaimTypes.Role).Select(c => c.Value), out actor)`. This is the
  exact shape the desktop session must reproduce from the access token.

Parity-matrix rows: **PAR-32** (`13.10 Administration`, FRD-04,
`Administration/Index.cshtml.cs` (35) — `OnGet`, "Administration hub
(admin-only)", status `inventoried`) is the row that covers the Administration
entry point this ticket hides. The rail itself has no row of its own — the
matrix is keyed to page models under `src/Pegasus.Web/Pages/**` and
`_Layout.cshtml` is a shared partial, not a page model. The matrix holds
`PAR-01`…`PAR-46` (`grep -c '^| PAR-'
docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`, run
2026-08-24). The other Administration rows this ticket's rail item leads to
are PAR-33…PAR-39. Do not invent a rail row.

## Findings

### Facts

Verified by reading the fork at `main` `191ddf33` on 2026-08-24.

- `StaffAuthorization.IsAuthorized(ActionActor, StaffAccessRight)` is the
  single Core role boundary — `src/Pegasus.Core/Identity/StaffAuthorization.cs:29-58`.
  Its `_ => false` arm at `:56` is the documented fail-closed default
  (`:23-26` "Unknown actor/permission combinations fail closed").
  - The Administrator-only family is one switch arm,
    `StaffAuthorization.cs:44-52`: `ManageStaffAccounts`, `ReviewStaffAccess`,
    `AssignStaffRoles`, `ManageOrganizationsAndPrincipals`,
    `ManageWorkflowConfiguration`, `ManageApprovedMailboxes`,
    `ManageApprovedOutlookCategories`, `ManageAutomationClients`, each
    requiring `actor.Kind == ActorKind.Staff && actor.IsInRole(StaffRole.Administrator)`.
  - `AccessStaffApplication` (`:35`) is `Kind == Staff` alone;
    `PerformCasework` (`:41-42`) also admits `ActorKind.Automation`.
- There are exactly twelve `StaffAccessRight` values —
  `StaffAuthorization.cs:7-21`.
- `StaffRole` is exactly `Administrator`, `Engineer`, `User` —
  `src/Pegasus.Core/Identity/IdentityContracts.cs:5-10`.
  `StaffRoleNames.All` at `:18-19` is the string projection.
- `StaffActorFactory.TryCreate` — `src/Pegasus.Core/Actors/StaffActorFactory.cs:8-39`
  — returns `false` for a non-`Guid` or empty subject id (`:15-18`), for **any**
  unparseable or undefined role name (`:23-27`), and for an empty role set
  (`:32-35`). It returns `true` only after `ActionActor.Staff(staffId, roles)`
  succeeds.
- `ActionActor.Staff` itself throws (it does not return false) on an empty
  `Guid`, an undefined role, or an empty role collection —
  `IdentityContracts.cs:52-73`. `IsInRole` is a set lookup at `:50`.
- The screen spec requires absence, not disablement:
  `docs/desktop/06-ui-design/screen-specs.md:58-63` — "`Administration` is
  present only for the Administrator role (derived from the role matrix and
  server authorisation)". The `DSK-06-04` row at
  `docs/desktop/06-ui-design/README.md:228` repeats it as
  "Administration absent (not disabled) for non-admins".
- The AutomationId convention is `<Screen>.<Region>.<Element>[.<Key>]` —
  `screen-specs.md:31-39`; the shell's ids are listed at `:80-82`:
  `Shell.Rail.<Route>`, `Shell.Title.Environment`, `Shell.Title.User`,
  `Shell.Status.Connection`, `Shell.Status.Update`.
- The general absence rule that governs *this* case is
  `screen-specs.md:27-30`: a capability the deployment/actor does not carry is
  **absent**; the "visible and disabled with the condition named" form is
  reserved for an action the *record* will offer once a state condition is met
  ("Available in Review"). A right the actor does not hold is the first case.
- Evidence tiers 2 and 5 are defined at `docs/engineering.md:74` and `:78`.
  Tier 5 is explicit that "actual routes reach Core; authentication …
  and action-history actor are observable" — a view-model test cannot stand in
  for it.
- `pwsh ./scripts/Invoke-TestShard.ps1` declares `-ShardCount` as
  `[Parameter(Mandatory)]` with **no** `ParameterSetName`
  (`scripts/Invoke-TestShard.ps1:35-36`), so it is mandatory in the `Verify`
  set as well as `Run`. `-ArtifactRoot` defaults to `artifacts/test-shards`
  (`:42`). The runnable verification form is therefore
  `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3`,
  as the script's own example at `:20` shows.
- None of the desktop projects exist yet: `ls src` returns
  `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`;
  `ls tests` returns `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`,
  `Pegasus.IntegrationTests` (2026-08-24).

### Assumptions

- **A-04-10-1** — the `/connect/token` access token issued to the
  `pegasus-desktop` client carries the staff subject id in a claim the desktop
  can read as a `Guid` string and the role names as repeated role claims, in
  the same shape `StaffPageModel.TryGetActor` consumes from the cookie
  principal. *Confirmed by:* reading the token-handler claim projection that
  [[GWY-019]] (plan handle `DSK-04-02`) lands, or decoding one issued token in
  the local stack. *If wrong:* the desktop cannot build an `ActionActor` at
  all and step 3 becomes a gateway change in [[GWY-019]]'s scope, not this
  ticket's. Area 04's own assumption **A4**
  (`docs/desktop/04-auth-session-update-and-startup/README.md`, § 2
  *Assumptions*) states the same expectation, so it is already tracked.
- **A-04-10-2** — `Pegasus.Core` is referenced directly by `Pegasus.Desktop`,
  so `StaffAuthorization` is callable from a view model without a new
  abstraction. *Confirmed by:* the project reference that [[FND-030]]
  (plan handle `DSK-02-05`) creates, and the dependency-direction facts
  [[FND-037]] (`DSK-02-12`) adds. *If wrong:* the right computation moves
  behind a thin `Pegasus.Desktop.Infrastructure` port that still calls Core —
  it never becomes a second matrix.
- **A-04-10-3** — the shell from [[FND-033]] (plan handle `DSK-02-08`) exposes
  its rail items as an observable collection a view model can build, rather
  than as static XAML `NavigationViewItem` children. *Confirmed by:* reading
  [[FND-033]]'s shell view model when it lands. *If wrong:* removal is done by
  rebuilding `NavigationView.MenuItems` in code-behind from the same view-model
  collection; the assertion (`no Shell.Rail.Administration` node in the tree)
  is unchanged.
- **A-04-10-4** — there is at least one Administrator-only `/api/v1` endpoint
  to point the forged-call test at by the time this ticket runs.
  *Confirmed by:* [[GWY-015]] (plan handle `DSK-03-15`), which lands the
  administration endpoint family, and [[GWY-003]] (`DSK-03-03`), which lands
  the per-group `StaffAccessRight` endpoint filter. *If wrong:* the test
  targets the filter directly through a minimal test-only route registered in
  the integration-test host, and that substitution is recorded in the proof.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Role assignment is a central administrative act — `Administration/Roles/Index.cshtml.cs` (135) `OnPostAssignAsync` (parity row PAR-38). An administrator's change must bind every operator's client. **Lands on the gateway** (`Pegasus.Web` evolved in place, L-01), which already owns the Identity store; no new host. |
| Unattended execution — must it run with every desktop closed? | **no** | The computation runs once per session change inside the shell view model. Nothing about rail visibility must happen while no desktop is open. |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | The input is the access token already held in memory by [[FND-043]] (plan handle `DSK-04-07`). No secret is introduced; the package carries none (area 04 § 3 decision 8). |
| Public callback — must an external service call a stable public endpoint? | **no** | No external caller is involved. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Proposal § 8.3: "the gateway must independently enforce authorization for every data query and command". **Lands on the gateway**: the bearer actor resolution and per-group `StaffAccessRight` endpoint filter from [[GWY-021]] (plan handle `DSK-04-04`) and [[GWY-003]] (`DSK-03-03`), inside `Pegasus.Web` — not a new service. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement exists, and the opposite is true for the *visibility* half: a rail that had to round-trip to render would add a request to every navigation for ten users. |

**Conclusion.** The responsibility splits, and the split is the point of the
ticket. The **visibility computation belongs in the desktop** (four "no"). The
**enforcement stays at the gateway** — the two "yes" answers name
`Pegasus.Web`, which already exists under L-01; neither creates an Azure
resource, and neither is satisfied by the client. This ticket therefore owes
both a client-side test and a server-side test, exactly as its Evidence tier
section says.

## Implications

1. **Reuse, do not re-express.** `StaffAuthorization.IsAuthorized` is the only
   place the matrix may live (`docs/engineering.md` § Engineering invariants,
   one Core owner). The desktop view model calls it; it does not switch on
   `StaffRole` itself. The web's own rail check uses `User.IsInRole(Administrator)`
   rather than the right — that is a *render-side shortcut in Razor*, not a
   precedent to copy, because the desktop has a real `ActionActor` in hand and
   the ticket body binds the item to `ManageStaffAccounts`. The two agree for
   every actor a staff session can produce (`StaffAuthorization.cs:44-52`
   requires exactly `Kind == Staff && IsInRole(Administrator)`).
2. **`TryCreate` failure is a session failure, not an anonymous shell.** Its
   `false` return covers a bad subject id, an unknown role name and an empty
   role set (`StaffActorFactory.cs:15-35`). The web returns `Forbid()`; the
   desktop must return to sign-in. This also means **a role added server-side
   without a desktop release logs a failed session for every user who holds it**
   — that is the recorded consequence the ticket's Traps demands, and it is the
   strongest argument for the guard in step 8.
3. **Absence, not disablement, is settled** by `screen-specs.md:58-63` and the
   general rule at `:27-30`. `IsEnabled=false` is wrong here and would also
   leave `Shell.Rail.Administration` in the automation tree, which is exactly
   what the UI assertion is written to catch.
4. **Rail order survives removal.** The order is a design-authority decision
   (`_Layout.cshtml:53-55`, `screen-specs.md:41-58`), so the collection must be
   built in order and filtered, never re-sorted after a removal.
5. **Deep-link re-check is a usability guard.** Step 8's navigation guard
   prevents a restored-state navigation into a screen whose data the gateway
   will refuse anyway; it must not be described in code comments or the proof
   as a security control.
6. **The ticket's own verification line needs its mandatory argument.** The
   body's `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` cannot run:
   `-ShardCount` is mandatory in every parameter set. This is a known board
   defect owned by [[FND-052]], which names FND-046 explicitly among the four
   `-VerifyPartition` call sites to repair. The plan uses the runnable form; no
   `open-questions` item is opened for it.

## Open questions

None that block planning. A-04-10-1 through A-04-10-4 are all answered by named
sibling tickets and are recorded as assumptions rather than as questions, per
`docs/desktop/00-governance-and-workflow/README.md` § 3 (a decision another
ticket owns is a scope boundary, not an open question).
