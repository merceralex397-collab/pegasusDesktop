# Files — FND-046

Surface area for the role-aware shell. Every desktop path below is created by
a **named** earlier ticket; nothing in `src/Pegasus.Desktop*` or
`tests/Pegasus.Desktop.*` exists in the tree today (`ls src`, `ls tests`,
2026-08-24).

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop.Infrastructure/Session/ICurrentActor.cs` (new; folder created by [[FND-031]], plan handle `DSK-02-06`) | The service contract: the `ActionActor` for the signed-in session, plus a change notification. New file so the shell has one place to ask, and so a sign-out can null it. |
| `src/Pegasus.Desktop.Infrastructure/Session/CurrentActor.cs` (new) | Builds the actor from the session's access-token claims with `StaffActorFactory.TryCreate`. Could break: caching the actor across sign-out would leak an administrator rail to the next user of the same workstation. |
| `src/Pegasus.Desktop.Infrastructure/Session/` — the session client from [[FND-043]] (plan handle `DSK-04-07`) | Raises the sign-in / refresh / sign-out transitions `CurrentActor` subscribes to, and is the only holder of the access token. Edited only to publish the transition if it does not already. |
| `src/Pegasus.Desktop/Shell/ShellViewModel.cs` (created by [[FND-033]], plan handle `DSK-02-08`) | Rail-item collection is filtered here by `StaffAuthorization.IsAuthorized`. Could break: rebuilding the collection out of order changes the settled rail order. |
| `src/Pegasus.Desktop/Shell/ShellPage.xaml` / `ShellPage.xaml.cs` ([[FND-033]]) | `NavigationView.MenuItems` must reflect removal, and each present item keeps `AutomationProperties.AutomationId = Shell.Rail.<Route>`. Could break: a `Visibility` binding instead of removal leaves the node in the automation tree and fails the UI assertion. |
| `src/Pegasus.Desktop/Navigation/NavigationService.cs` ([[FND-033]]) | The deep-link / restored-state guard from step 8. Could break: guarding only rail clicks leaves the restored-state path open. |
| `src/Pegasus.Desktop/App.xaml.cs` (host and DI from [[FND-032]], plan handle `DSK-02-07`) | One registration line for `ICurrentActor`. |
| `tests/Pegasus.Desktop.ViewModelTests/` (created by [[FND-038]], plan handle `DSK-02-13`) | The tier-2 half: Administrator/Engineer/User visibility, rail order after removal, `TryCreate` failure → session failure. |
| `tests/Pegasus.IntegrationTests/` (exists today) | The tier-5 half: a non-administrator bearer token calling an Administrator-only `/api/v1` route is refused with `urn:pegasus:problem:not-authorized`. |

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:44-52` | The Administrator-only arm is **one** switch case covering eight rights, all requiring `Kind == Staff && IsInRole(Administrator)`. Binding the rail to `ManageStaffAccounts` therefore also settles every other Administration screen — you do not need a right per rail item. `:56` `_ => false` is the fail-closed default; `:31` throws on a null actor, so the caller must handle "no session" before calling, not by passing null. |
| `src/Pegasus.Core/Actors/StaffActorFactory.cs:15-35` | `TryCreate` returns `false` on three distinct causes: a non-`Guid`/empty subject (`:15-18`), **any** role name that is not exactly `Administrator`/`Engineer`/`User` (`:23-27`, `ignoreCase: false`), and an empty role set (`:32-35`). The case-sensitivity and the all-or-nothing behaviour are the trap: one unknown role name in the token denies the whole session, it does not degrade to the known roles. |
| `src/Pegasus.Core/Identity/IdentityContracts.cs:52-73` | `ActionActor.Staff` **throws** where `TryCreate` returns false. Never call it directly from the desktop — always go through `TryCreate`, or an unexpected token shape becomes an unhandled exception in the shell instead of a clean return to sign-in. |
| `src/Pegasus.Web/Pages/StaffPageModel.cs:11-15` | The exact claims→actor projection the gateway uses today: subject from `ClaimTypes.NameIdentifier`, roles from **all** `ClaimTypes.Role` claims. Reproduce this shape, do not invent claim names. |
| `src/Pegasus.Web/Pages/Shared/_Layout.cshtml:53-55, :93-98` | The settled rail order as a comment, and the current hide: `@if (User.IsInRole(StaffRoleNames.Administrator))` around the anchor, so the element is never emitted. Note it checks the **role**, not the right — a Razor shortcut, not the pattern to copy; the desktop has a real `ActionActor`. |
| `src/Pegasus.Web/Pages/Administration/Index.cshtml.cs:9, :27-33` | Both halves of the server side in seven lines: the `[Authorize(Policy = StaffRoleNames.Administrator)]` attribute *and* an explicit `StaffAuthorization.Require(actor, ManageStaffAccounts)` after a `TryGetActor` that `Forbid()`s on failure. Belt and braces on the server is the house style; the client gets neither. |
| `docs/desktop/06-ui-design/screen-specs.md:27-30` | The absence rule, and its exception. "Absent, not disabled" applies to a capability the actor/deployment does not carry. The "visible and disabled with the condition named" form is only for an action the *record* will offer later ("Available in Review"). A missing right is the first case — remove it. |
| `docs/desktop/06-ui-design/screen-specs.md:41-63, :80-82` | The shell wireframe, `NavigationView` settings (`PaneDisplayMode=Left`, `OpenPaneLength=236`, `IsPaneToggleButtonVisible=False`), the sentence "`Administration` is present only for the Administrator role", and the AutomationId list including `Shell.Rail.<Route>` and `Shell.Title.User`. |
| `docs/desktop/06-ui-design/README.md:228` | Row `DSK-06-04`, owned by [[DUI-004]] (plan handle `DSK-06-04`) — the shell ticket's own acceptance already says "Administration absent (not disabled) for non-admins". If the two implementations disagree, [[DUI-004]]'s row governs the shell's *shape* and this ticket's steps govern the *right computation*. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 decision 3 | "every `/api/v1` request re-checks `IsEnabled` and the security stamp … a disabled account therefore stops within one request". This is why the client may safely hold the actor for the session: staleness is bounded by the server, not by the client. |
| `docs/engineering.md:74, :78` | Tier 2 and tier 5 definitions. Tier 5 requires the *actual route*; a `WebApplicationFactory` call through the real endpoint qualifies, a handler unit test does not. |
| `scripts/Invoke-TestShard.ps1:20, :35-36, :42` | The worked `-VerifyPartition` example, `-ShardCount` mandatory in **every** parameter set (no `ParameterSetName` on that attribute), and `-ArtifactRoot` defaulting to `artifacts/test-shards`. This is why the body's bare `-VerifyPartition` cannot run. |

## Ripple effects

- **Tests.** New view-model tests in `tests/Pegasus.Desktop.ViewModelTests`; a
  new authorization test in `tests/Pegasus.IntegrationTests`. The integration
  project is sharded — a new test class changes shard assignment, so
  `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3`
  must stay green and the class must land in exactly one shard
  (`scripts/Invoke-TestShard.ps1:8-10` assigns whole classes together).
- **Architecture tests.** `tests/Pegasus.ArchitectureTests` holds the
  dependency-direction facts. [[FND-037]] (plan handle `DSK-02-12`) extends
  them for the desktop boundaries; if a fact forbids `Pegasus.Desktop` →
  `Pegasus.Core.Identity`, this ticket's approach breaks and the fact — not the
  approach — is what gets discussed with [[FND-037]].
- **UI tests.** The permanent non-administrator tree assertion belongs in the
  shared harness `tests/Pegasus.Desktop.UITests/ui-tests.ps1`, whose file,
  signature and `Test-UI` helper are owned by [[TEST-006]] (plan handle
  `DSK-08-06`). This ticket's `winapp ui inspect` capture in step 11 is a
  manual proof artefact; the standing case is [[TEST-006]]'s or the
  AutomationId coverage audit in [[DUI-015]] (plan handle `DSK-06-15`).
- **No contract ripple.** Nothing here changes a request or response shape, so
  `openapi/pegasus-v1.json` and the generated client are untouched. The
  integration test *consumes* an existing Administrator-only route; it does not
  define one.
- **Documentation.** FRD-13 § role-aware navigation, when [[FND-008]] (plan
  handle `DSK-00-08`) creates it. `docs/desktop/06-ui-design/screen-specs.md`
  only if the implemented rail differs from the spec, and then as a correction
  with a reason.

## Out of scope

Recorded so the reviewer sees each was a decision, not an oversight.

- **`src/Pegasus.Core/Identity/StaffAuthorization.cs` is not modified.** The
  ticket's Guardrails forbid it: the matrix is Core's and changing it is a
  different ticket with its own ADR.
- **No role matrix in the desktop.** No `switch` on `StaffRole`, no
  `IsAdministrator` helper, no copy of the eight-right arm. One Core owner.
- **No new gateway authorization code.** The endpoint filter is
  [[GWY-021]] (plan handle `DSK-04-04`) and [[GWY-003]] (plan handle
  `DSK-03-03`); this ticket only *proves* it refuses a forged call.
- **No Administration screens.** The rail item is hidden or shown; the screens
  behind it are [[GWY-015]] (plan handle `DSK-03-15`) and
  [[FEAT-020]] (plan handle `DSK-05-20`) with area 06.
- **No `IsEnabled=false` fallback** anywhere on the rail, and no
  `Visibility.Collapsed` binding — both leave the node in the automation tree.
- **No Azure write** of any kind.
