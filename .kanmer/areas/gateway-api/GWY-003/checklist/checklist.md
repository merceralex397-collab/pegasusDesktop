# Checklist — GWY-003

One box per plan step, in plan order. The last box produces `proof`.

- [x] Read the governing gateway/auth documents and Core `StaffAuthorization`; called `get_doc_gates GWY-003`; took the ticket on `task/gateway-staff-authorization` from `origin/dev`.
- [x] Confirmed GWY-021 is merged at `0e7fa423` with the bearer gateway resolver and GWY-002's gateway/problem mappings are present; no second token pipeline was invented.
- [x] Created `src/Pegasus.Web/Api/StaffActorAccessor.cs` as the single Web claims-to-actor factory caller beside `Pages/StaffPageModel.cs`.
- [x] Added fail-closed unauthenticated/Automation-audience/non-staff refusal and persisted `Denied` token events using the gateway correlation id.
- [x] Created `RequireStaffRightFilter` and the single-argument `RouteGroupBuilder.RequireStaffRight(StaffAccessRight)` extension, delegating to Core authorization.
- [x] Documented the filter as a fail-fast boundary; business-state preconditions remain in Core use cases.
- [x] Registered the accessor and stored the resolved actor under `DesktopGateway.ActorItemKey` without changing the gateway group return type.
- [x] Preserved GWY-021's existing account/password problem mappings and added token denial evidence for disabled, invalid-stamp, and absolute-expiry account refusals without querying Identity from the right filter.
- [x] Added `DesktopGatewayAuthorizationTests` over the real `/api/v1` group: 24 right cases plus disabled-account, Automation-audience, and anonymous cases. The two permanent-refusal rights each exercise Administrator, Engineer, and User.
- [x] Asserted disabled-account and Automation-audience `Denied` security events with the response correlation id.
- [x] Focused authorization test passed 27/27 with 0 skipped; the factory call-site grep returned exactly two; the audience literal grep returned exactly one.
- [x] Completed the four-lens simplification pass and recorded findings/dispositions in the plan.
- [ ] **Verification on merged `main` (this box produces `proof`)** — local locked restore, Release solution build (0 warnings/0 errors), full IntegrationTests (1,075 passed, 0 failed, 16 existing corpus-gated skips), and focused authorization evidence are recorded in the post-implementation report. Remaining proof requires the reviewed PR, green exact-head CI, merge to `dev`, exact-SHA promotion to `main`, main CI, then `proof.md` and Kanmer closeout.

## Progress notes

- 2026-08-30: GWY-021 dependency merged at main/dev `0e7fa423`; this ticket's branch starts from that exact head.
- 2026-08-30: Initial independent review identified missing Engineer coverage in the two permanent-refusal cases; both cases now exercise all three staff roles while retaining the required 27 facts.
- 2026-08-30: Final local focused authorization gate is 27 passed, 0 failed, 0 skipped. Full IntegrationTests is 1,075 passed, 0 failed, 16 skipped, 1,091 total; skips are pre-existing corpus-gated tests outside this ticket.
