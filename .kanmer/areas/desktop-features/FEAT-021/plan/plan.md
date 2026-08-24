# Plan — FEAT-021: S21 Password change and account lifecycle

**Diff estimate: ~17 files, ~1,100 lines.** Derived from the files document: 3 contracts DTO files
(~120), 3 desktop view-model/XAML files (~300), 2 desktop-infrastructure files for the session
client and the DPAPI clear (~150), 3 gateway endpoint files for the three session routes (~200),
6 test files — 2 contract, 2 view-model, 2 contributed to the security set (~280), and
3 documentation files (~60). Small because the surface is three endpoints and one screen; the cost
is in the evidence, not the code.

## Approach

Drive the whole flow from **typed problems**, not from navigation: `GET /api/v1/session/me` carries
the must-change-password flag and any `/api/v1` call may return `password-change-required`, so the
startup orchestrator from [[FND-045]] (plan handle `DSK-04-09`) shows the change screen and no other
navigation is possible. The five settled `StaffPasswordChangeError` messages
(`src/Pegasus.Web/Pages/Account/PasswordChange.cshtml.cs:94-120`) are carried across verbatim and
mapped one-to-one from problem types; `StaffAccountNotFound` continues to produce no message at all.
Sign-out calls the logout endpoint and clears **both** the in-memory token cache and the DPAPI
refresh store.

Rejected: **reproducing the web's redirect gate as a navigation guard**. The web's allow-list has
six entries (`Program.cs:883-889`), four of which are browser asset paths; translating it would
carry web mechanics the desktop does not need and would make the forced state a routing accident
rather than a stated state. Also rejected: **one generic "the password could not be changed"
message** — that is precisely the defect the comment at `PasswordChange.cshtml.cs:91-93` records
being fixed.

## Governing docs

The ticket's `refs` is `docs/frd/frd-04-parties-accounts-and-access.md`, which exists.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-04 § `Parties, principals, organisations, accounts, and access` (`:13`ff) | Staff accounts use Pegasus-managed usernames and passwords with non-reversible hashes until a separately accepted identity change supersedes that route | Steps 3, 5 (Pegasus credentials only; no Entra route) |
| FRD-04 § `Staff role access matrix` (`:24`) | Authorization is enforced in Core use cases **and at every caller boundary**, and "fails closed without revealing case or source data" | Steps 5, 9 (a wrong current password returns a problem that does not reveal whether the account exists) |
| FRD-04 § `Permanent action history` (`:33`) | Sign-ins and authentication failures remain in the **security log**; routine mechanics remain content-safe telemetry | Step 9 (the contract facts assert against the security log, not the action history) |
| FRD-04 § `Permanent action history` (`:29`) | A history write is part of the mutable business transaction; a failed write cannot leave an unrecorded successful mutation | Step 9 (a successful change is recorded, and the record is asserted) |

`docs_todo: true`, confirmed in `get_doc_gates FEAT-021` — the `governing-doc` requirement at
`leave-backlog` reads `satisfied: true`.

> **New ADR** — ADR-0102 (existing Pegasus credentials and identity store; desktop session =
> short-lived access token plus rotated refresh token), authored by [[FND-005]] (plan handle
> `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:157`); if the ADR lands
> differently this plan is revised before implementation. ADR-0104 (online-required; bounded local
> cache only) is authored by the same ticket and bounds what the desktop may hold. ADR-0004,
> ADR-0011 and ADR-0027 already exist and are related, not re-authored.

Programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 8.4 Session failure handling | Session failures are handled as states, with no Microsoft login | Steps 5–7 |
| Proposal § 13.1 Access and session | Change password including must-change-on-next-login; understand a disabled account; sign out | Steps 5–7 |
| Proposal § 17.1 Required controls | Credentials are protected; failures are bounded and redacted | Step 10 |
| L-01 | The gateway owns credentials, revocation and audit | Steps 3, 8 |
| L-02 | Security verification on the local Test/UAT stack | Step 10 |
| L-04 | Routing named on the ticket | § Routing |
| `docs/engineering.md` § Required evidence tiers (5, 9) | Tier 5 obliges route-level evidence with validation, idempotency and exception translation; **tier 9** obliges role-matrix, transient-authentication-throttling, denial-before-client-construction, redaction and bounded-failure evidence for the credential path | Steps 9–10, § Verification |
| `docs/design/README.md:432-445` | No hint sentence under a field, no format guidance, no how-it-works copy | Step 5 |
| `docs/desktop/12-agent-tooling/skill-routing.md` | `entra-app-registration` and `entra-agent-id` are on the do-not-load list | § Routing, Step 1 |
| `StaffSessionPolicy` (`src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13`) | 2 h idle / 8 h absolute lifetimes; 10 sign-in attempts per client per minute, 100 global | Step 3 (the desktop honours, never re-implements, these) |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` —
  `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` —
  `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`)
  → `winui-design` (`.codex/skills/winui-design/SKILL.md`, dialog and focus rules) →
  `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `run-tests` →
  `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for OpenIddict token
  revocation semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refining the ticket body's twelve steps. Body step numbers in brackets.

1. **[body 1] Orient and take.** Read the plan row, `vertical-slices.md` § S21, the screen spec
   Change-password section and `docs/desktop/04-auth-session-update-and-startup/README.md` for the
   session-failure matrix. Do **not** load `entra-app-registration` or `entra-agent-id` — both are
   on the do-not-load list. Call `get_doc_gates FEAT-021`, then `take_ticket` with branch
   `task/dsk-05-21-account-lifecycle` and worktree
   `../pegasus-worktrees/dsk-05-21-account-lifecycle` from `origin/dev`.
2. **[body 2] Read and record.** Read `PasswordChange.cshtml.cs` and `Program.cs:875-899`. Record
   in `research`: which password validation rules the **Core** use case applies versus which live
   only in the page's `DataAnnotations` (`[MinLength(8)]` at `:28` is the one to settle); exactly
   what the middleware allows while a change is required — the six-entry allow-list at `:883-889`,
   of which four are browser asset paths; the five settled `StaffPasswordChangeError` messages at
   `:94-120`; and the current disabled-account handling. Record the SHA read.
   *If the eight-character minimum is only in the page*, it is a page-model rule and moves into
   Core with a characterization test before the desktop mirrors it (plan 05 § 3).
3. **[body 3] Confirm the endpoints and problem types.** With [[GWY-021]] (plan handle `DSK-04-04`)
   and the endpoint map: `POST /api/v1/session/password-change` (idempotent by operation key,
   revoking refresh tokens on success, returning 204), `POST /api/v1/session/logout`, and
   `GET /api/v1/session/me` carrying the must-change-password flag. Confirm the
   `password-change-required` and disabled-account problem types exist and are **typed, not prose**.
   All three carry the right `AccessStaffApplication`, which resolves to `actor.Kind ==
   ActorKind.Staff` with no role requirement (`StaffAuthorization.cs:35`).
4. **[body 4] Add the DTOs.** In `src/Pegasus.Contracts`. No password value is logged, cached or
   included in a diagnostics bundle — mark the DTO and test the redaction. Regenerate
   `openapi/pegasus-v1.json` and the generated client in this change.
5. **[body 5] Implement `PasswordChangeViewModel`.** Immediate field validation using the Core
   rules, a deliberate submit, and a one-to-one map from each typed problem to its **settled
   message** (`PasswordChange.cshtml.cs:94-120`). Reproduce the field-level attachment: the
   current-password error attaches to the current-password field, the two new-password errors to
   the new-password field, the operation conflict to the form. Clear the password fields on every
   failure path, as `ResetSensitiveInput()` (`:163`) does. There is no hint text and no
   password-policy prose on the screen; the eight-character rule appears only as a validation
   outcome, never before the operator types (`docs/design/README.md:432-445`).
6. **[body 6] Route the forced state before the shell.** When `GET /api/v1/session/me` or any
   `/api/v1` call returns `password-change-required`, [[FND-045]]'s startup orchestrator shows the
   change screen and no other navigation is possible. The desktop's allow-list is two entries — this
   screen and sign out — not the web's six. Carry the `Forced` distinction from
   `PasswordChange.cshtml.cs:41-50` as a view-model state: under the gate the screen renders without
   navigation and states the consequence; a voluntary change keeps the application around it. **Do
   not attempt to reproduce a redirect.**
7. **[body 7] Disabled account and sign out.** Render the disabled-account state with the exact
   settled message from [[FND-044]]'s session-failure matrix (plan handle `DSK-04-08`) and no
   further navigation — this slice renders it, it does not invent the text. Sign out calls the
   logout endpoint, clears the in-memory token cache and clears the DPAPI refresh store from
   [[FND-031]] (plan handle `DSK-02-06`). The signed-out confirmation is a one-time state, not a
   screen (`SignOut.cshtml.cs:16-19`).
8. **[body 8] Invalidate other sessions.** On a successful change the desktop discards its cached
   tokens and re-authenticates; other devices fail their next refresh with `invalid_grant`. The
   revocation itself is [[GWY-022]]'s (plan handle `DSK-04-05`) — confirm it has landed before
   claiming the acceptance criterion.
9. **[body 9] Contract tests.** In `tests/Pegasus.Api.ContractTests`: a successful change revokes
   refresh tokens; a wrong current password returns a problem **without revealing whether the
   account exists**; replay of the same operation key is safe; a disabled account is refused on the
   next request; logout revokes the refresh token. Assert the security-log record, not an action-
   history record (FRD-04 `:33`). Enable `Features:DesktopGateway` explicitly.
10. **[body 10] Security tests with [[TEST-011]] (plan handle `DSK-08-11`).** No password or token
    appears in any log file or diagnostics bundle; the DPAPI store file has restrictive ACLs; a
    revoked refresh token cannot be reused; changing the password on one session logs out the
    other. Tier 9 obliges this evidence for the credential path — it is not satisfied by review.
11. **[body 11] View-model tests.** Validation, forced-change routing before the shell, the
    disabled-account state, and sign-out clearing **both** caches.
12. **[body 12] Documentation, simplification, PR.** Update `parity-matrix.md` rows `PAR-02`
    (`:47`) and `PAR-03` (`:48`), reconciling `PAR-03`'s `~POST /api/v1/session/password` with the
    endpoint map's `POST /session/password-change`; add the account-lifecycle section to
    `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-04; add the `DSK` rows to
    `docs/capabilities.md`; run the simplification pass over the branch diff under a dated
    `## Simplification pass` heading; open the PR into `dev`.

## Verification

Evidence tiers from the body: **5** (Web/API/MCP caller) and **9** (Security/observability).

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — change, revoke, replay, disabled-account and logout facts pass.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — validation, forced-change routing, disabled state and sign-out facts pass.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"`
  — the existing account web tests stay green; the middleware and the Razor pages are untouched.
- **Security-test record in the ticket proof** — a clean secret scan over logs and the diagnostics
  bundle, restrictive ACLs on the DPAPI store file, and a revoked refresh token proved unusable.

Evidence that becomes `proof`: the three test outputs (test-output tier) and the security record.

## Risks / open questions

- **The typed session problem types and the disabled-account message text** — owned by [[GWY-021]]
  (plan handle `DSK-04-04`) and [[FND-044]] (plan handle `DSK-04-08`). Scope boundaries, not open
  questions; step 3 confirms they exist and are typed before the view model maps them.
- **Refresh-token revocation** — owned by [[GWY-022]] (plan handle `DSK-04-05`). The acceptance
  criterion "the change invalidates every other session" is only true once that has landed;
  step 8 checks rather than assumes.
- **The eight-character minimum may be a page-model rule.** Mitigation: step 2 settles it by
  reading. If it is the page's `[MinLength(8)]` rather than Core's, it moves into Core with a
  characterization test first — the standing plan 05 § 3 rule, not a new decision. Mirroring a
  page-model rule in the desktop would create a second policy owner.
- **Message drift.** The five settled messages are a recorded decision with its reasoning in the
  code (`PasswordChange.cshtml.cs:91-93`). Mitigation: step 5 carries them verbatim and the
  reviewer diffs them against the source.
- **Account-existence disclosure** is the classic failure here. Mitigation: `StaffAccountNotFound`
  produces no message (the web's `Forbid()` at `:86-89`), and step 9 asserts it as a contract fact.
- **Secret leakage into the diagnostics bundle.** Mitigation: step 10's scan, run against the
  bundle from [[FND-036]] (plan handle `DSK-02-11`); the wider security set is [[TEST-011]]'s.
- **`PAR-03` names a different path** (`~POST /api/v1/session/password`) from the endpoint map
  (`POST /session/password-change`). Mitigation: step 12 reconciles the row; the endpoint map is
  authoritative for exact paths.
- **The web keeps its redirect.** `Program.cs:875-899` is not modified by this ticket; the
  middleware's removal is [[FEAT-026]]'s (plan handle `DSK-05-26`) after cutover.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
