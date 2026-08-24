# Checklist — FEAT-007: S7 Parties and reference data (organizations, principals)

One box per plan step, in plan order. Each is independently tickable. The
verification box is the one that produces `proof`.

- [ ] Read the plan row (`docs/desktop/05-implementation-and-migration/README.md:216`), `vertical-slices.md` § S7, `screen-specs.md:332-341` and `docs/frd/frd-04-parties-accounts-and-access.md:19-25`
- [ ] `get_doc_gates FEAT-007`, then `take_ticket` with branch `task/dsk-05-07-parties` and worktree `../pegasus-worktrees/dsk-05-07-parties` from `origin/dev`
- [ ] Re-read the five page models at the current SHA and record that SHA in `research` (parity drift, plan 05 § 7)
- [ ] Confirm the four bounds are still 300 / 20 / **100** / 500 at `OrganizationAdministration.cs:270-275`, that `PlanPrincipalReplacement` (`:341-388`) still preserves `SequenceLineageId` and still calls `RequireUniquePrincipalCode`, and that no `IUpdatePrincipal` has appeared
- [ ] Read [[GWY-015]]'s (plan handle `DSK-03-15`) delivered route group and the committed `openapi/pegasus-v1.json` snapshot; confirm the six administration routes exist with `ManageOrganizationsAndPrincipals` and `operationKey` ≤ 100, and that replace carries `reason`
- [ ] Confirm `GET /api/v1/reference/providers` is gated on **`PerformCasework`**, not the administrator right (endpoint map `:132`); raise any gap on [[GWY-015]] rather than patching it here
- [ ] **Conditional (expected: nothing owed)** — if `GET /reference/providers` needs a new Core port because `IProviderReferenceCatalog` cannot list (`ReferenceDataContracts.cs:37-42`), write the characterization test in `tests/Pegasus.Core.Tests` **before** consuming it
- [ ] Add the five request and four response DTOs to `src/Pegasus.Contracts`, keeping organization `version` and principal `version` as separate wire fields and mirroring the four bounds as validation attributes from the Core constants
- [ ] Add the seven typed client calls to `src/Pegasus.Desktop.Infrastructure`, with page size 25 (list) and 100 (organization picker) as named constants
- [ ] Implement `OrganizationsViewModel` as the single Administration entry point for parties, on the [[DUI-007]] (plan handle `DSK-06-07`) data-table pattern, page size 25
- [ ] Implement `OrganizationDetailViewModel` hosting the organization form, that organization's principal rows and both principal commands, on the [[DUI-008]] (plan handle `DSK-06-08`) form pattern — and create **no** Principals view model, rail entry, card or route
- [ ] Surface `ActivePrincipalsRequireWorkProvider` when Work Provider is cleared on an organization holding an active principal (`Edit.cshtml.cs:130-145`)
- [ ] Reproduce the organization-picker top-up from `Principals/Create.cshtml.cs:90-119` — fetch and prepend a selected organization absent from the returned page
- [ ] Implement the `operationKey` regeneration rule: a fresh key after the operator edits a refused form, the same key when resending an unchanged request
- [ ] Render provider reference data read-only, with no edit affordance
- [ ] Implement principal replacement as an explicit command inside Organization detail through the [[DUI-009]] (plan handle `DSK-06-09`) `ReasonDialog`, defaulting `SuccessorOrganizationId` to the predecessor's organization (`Replace.cshtml.cs:163`), with its consequence sentence taken from the closed list at `docs/design/README.md:400-409`
- [ ] Confirm the principal row carries **no** edit command of any kind, and that no client-side "never reuses a reference" check was added — the rule is asserted server-side only
- [ ] Consume [[FND-046]]'s (plan handle `DSK-04-10`) role awareness so the Administration rail entry and both screens are absent for a non-administrator, adding no second visibility rule
- [ ] Add contract tests for 200 (administrator), 401 (no token) and 409 (stale version — **Edit and Replace only**) per endpoint in `tests/Pegasus.Api.ContractTests`, with `Features:DesktopGateway` enabled explicitly
- [ ] Add the two separate 403 `not-authorized` contract facts: a `PerformCasework`-only staff session, and the Automation Actor (`StaffAuthorization.cs:45-53`)
- [ ] Add the `operationKey` replay contract fact returning the same result per mutation
- [ ] Add the two negative contract facts: **no `PUT /api/v1/admin/principals/{id}` route exists**, and a replace reusing the predecessor's code is refused with `DuplicatePrincipalCode`
- [ ] Record the two [[TEST-002]] (plan handle `DSK-08-02`) matrix exemptions — the creates have no stale-version case; the reference read's 403 case is a different actor — rather than silently skipping them
- [ ] Add view-model tests for list paging, create validation against all four bounds, edit dirty state, replace refusing an empty reason, the successor-organization default and the key-regeneration rule
- [ ] Add the two structural view-model facts: the principal row exposes no edit command, and **no navigation target named Principals exists**
- [ ] Add `tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script parties` covering keyboard create-organization and replace-principal, both reached from Organization detail, and run the `axe-windows` scan over the list and the detail
- [ ] Update `parity-matrix.md` `PAR-40` (`:85`) and `PAR-41` (`:86`), moving PAR-41's native-screen cell from "Principals admin" to Organization detail
- [ ] Rewrite `screen-specs.md:332-341`: record the consolidation, re-host the `Admin.Principals.Create` and `Admin.Principals.Replace` AutomationIds on Organization detail, add the decision the carry-over line at `:340-341` omits, and correct "addresses, contacts" (no such field exists on `Organization`, `CaseContracts.cs:13-17`)
- [ ] Add the parties and reference-data section to `docs/frd/frd-13-desktop-operator-experience.md` and `DSK` rows to `docs/capabilities.md`
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Verification run — `dotnet build ./Pegasus.slnx -c Release --no-restore`; `dotnet test` for Api.ContractTests, Desktop.ViewModelTests and the filtered IntegrationTests (`--filter "FullyQualifiedName~OrganizationAdministration"`, which must pass unchanged); `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script parties`; then write `proof` with the command log and the tier-7 UI/axe artefacts
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
