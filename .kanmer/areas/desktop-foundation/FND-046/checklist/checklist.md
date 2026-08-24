# Checklist — FND-046

One box per plan step, in plan order. Each is independently tickable.

- [ ] Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 5 row `DSK-04-10` and `docs/desktop/06-ui-design/screen-specs.md:41-82`; run `get_doc_gates FND-046`; `take_ticket` with branch `task/<slug>` cut from `origin/dev` and worktree `../pegasus-worktrees/<slug>`
- [ ] Load `pegasus-desktop`, then `winui-design`
- [ ] Read `src/Pegasus.Core/Identity/StaffAuthorization.cs` and `IdentityContracts.cs` end to end; confirm the Administrator family is the single arm at `StaffAuthorization.cs:44-52`, the default arm at `:56` is `false`, and `:31` throws on a null actor
- [ ] Add `ICurrentActor.cs` to `src/Pegasus.Desktop.Infrastructure/Session/` with the actor property and a `Changed` event
- [ ] Add `CurrentActor.cs` building the actor via `StaffActorFactory.TryCreate`, reading the subject from `ClaimTypes.NameIdentifier` and roles from all `ClaimTypes.Role` claims, mirroring `src/Pegasus.Web/Pages/StaffPageModel.cs:11-15`
- [ ] Route a `TryCreate` `false` return into [[FND-043]]'s (plan handle `DSK-04-07`) session-failure path back to sign-in, with one diagnostics line carrying the correlation id, subject id and the count of unrecognised role names — no token, no claim values
- [ ] Register `ICurrentActor` as a singleton in `src/Pegasus.Desktop/App.xaml.cs` and raise `Changed` on sign-in, refresh and sign-out
- [ ] Null the held actor **before** raising `Changed` on sign-out, so no subscriber can read a stale actor
- [ ] Build the rail collection in `ShellViewModel` in the settled order Dashboard → Inbox → Upload → Queues → Cases → Operations → Administration → user
- [ ] Remove unauthorized items from `NavigationView.MenuItems` using `StaffAuthorization.IsAuthorized(actor, right)`, with Administration bound to `StaffAccessRight.ManageStaffAccounts`; no `IsEnabled=false`, no `Visibility` binding
- [ ] Give every admin-only title-bar and command-bar command its required right, resolved through the same call
- [ ] Rebuild the collection from the ordered source on every `ICurrentActor.Changed`, never re-sorting after a removal
- [ ] Set `AutomationProperties.AutomationId` to `Shell.Rail.<Route>` on each present rail item and `Shell.Title.User` on the user group
- [ ] Add the deep-link / restored-state guard in `NavigationService` that re-checks `IsAuthorized` and routes to the dashboard with an `InfoBar` message on failure
- [ ] Add the one-line comment on that guard stating it is a usability guard and the boundary is the `/api/v1` endpoint filter, and repeat the sentence in the PR description
- [ ] View-model test: an `Administrator` actor sees Administration
- [ ] View-model test: an `Engineer` actor does not see Administration
- [ ] View-model test: a `User` actor does not see Administration
- [ ] View-model test: the non-administrator rail order equals the settled order with Administration elided and nothing else moved
- [ ] View-model test: a role name that is not exactly `Administrator`/`Engineer`/`User` produces the session-failure state, not an empty shell
- [ ] Integration test in `tests/Pegasus.IntegrationTests`: a non-administrator bearer token calling an Administrator-only `/api/v1` endpoint is refused with problem type `urn:pegasus:problem:not-authorized`
- [ ] Confirm the new integration class lands in exactly one shard and record the substitution in the post-implementation report if a test-only route was used instead of a real Administrator-only endpoint
- [ ] Build and launch with `.\BuildAndRun.ps1` from `.codex/skills/winui-dev-workflow/`, with the local stack started by `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start`
- [ ] Capture `winapp ui inspect -a <pid> --interactive` and `winapp ui screenshot` signed in as an Administrator
- [ ] Capture `winapp ui inspect -a <pid> --interactive` and `winapp ui screenshot` signed in as a non-administrator, and confirm no `Shell.Rail.Administration` node is present
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run (this box produces `proof`): `dotnet test tests/Pegasus.Desktop.ViewModelTests`; `dotnet test tests/Pegasus.IntegrationTests --filter FullyQualifiedName~Authorization`; `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3`; attach the four evidence artefacts named in the plan's Verification table
- [ ] Open the PR into `dev` and hand review to `pegasus-desktop-reviewer`

## Progress notes
