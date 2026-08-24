# Plan — FND-043: Desktop session client — sign in, in-memory token, DPAPI refresh store, silent refresh, sign out

**Diff estimate: ~9 files, ~640 lines.** Derived from the files document file by file, not
asserted: `Session/ISessionClient.cs` ~35; `Session/SessionClient.cs` ~180 (two form-encoded
grant calls, response parsing, and the mapping from OAuth `error` /
`error_description` and problem slugs onto `SessionFailure`); `Session/SessionFailure.cs` ~45
(seven values, two of them carrying data); `Session/SessionAuthorizationHandler.cs` ~70 (the
bearer attach, the refresh-once/retry-once counter, and the second-401 path);
`src/Pegasus.Desktop/App.xaml.cs` +~15 (three registrations plus the named-client handler
wiring); the log redactor +~10 (five literals and its own test); and ~285 lines of tests
across `tests/Pegasus.Desktop.ViewModelTests/Session/` — six behaviour tests at ~35 lines
each, plus the DPAPI round-trip (~40) and the log-redaction assertion (~35). Two conditional
documentation edits add ~6 lines **only if** their gating tickets have landed.
`docs/engineering.md:201` § Plan sizing requires the estimate first.

## Approach

**Write the client as a thin, testable caller of a contract that does not exist yet: two
form-encoded grant calls, one closed enum of seven failure values, one `DelegatingHandler`
with a retry counter — and prove every branch against a fake `HttpMessageHandler` rather
than a running gateway.** The alternative rejected is **waiting for [[GWY-019]] (plan handle
`DSK-04-02`) to land the server half first**: the board records this ticket blocking seven
others including [[FND-044]] and [[FND-045]], and the contract it codes against is fully
specified in plan 04 § 3 decisions 1–3 and the session-failure matrix, so serialising them
buys nothing and stalls Phase 2. The second alternative rejected is **an
`AuthenticationHandler`/`IAuthenticationService` abstraction from
`Microsoft.AspNetCore.Authentication`**: [[FND-037]]'s `ForbiddenDesktopDependencyPrefixes`
fact forbids `Microsoft.AspNetCore.*` in this project outright, and parsing
`WWW-Authenticate: Bearer error="invalid_token"` directly is a handful of lines. The third
alternative, **enforcing the 8-hour absolute cap client-side**, is rejected because
`StaffSessionPolicy.cs:10` is the gateway's contract — a client-side timer would be a second
policy owner that silently drifts from the server's.

Two properties carry the ticket and neither is provable by reading the code. **The refresh
handle must not appear in plaintext on disk** and **no token literal may reach the rolling
log** — so both get an assertion that reads the artefact back (steps 6 and 9), in the style
`docs/engineering.md` § Lessons from the predecessor asks for: a guard that has never fired
is a guard nobody can trust.

## Governing docs

The ticket's `refs` list is **not** empty — it carries
`docs/frd/frd-04-parties-accounts-and-access.md` — and its frontmatter also carries
`docs_todo: true`, so both halves of this section apply.

**Meets** — for the one entry in `refs`:

| FRD-04 requirement | Where it says so | Met by |
| --- | --- | --- |
| "Staff accounts use Pegasus-managed usernames and passwords with non-reversible password hashes **until a separately accepted identity change supersedes that route**" | `docs/frd/frd-04-parties-accounts-and-access.md:15` | Steps 4–5. The desktop posts the **same** username and password to `/connect/token`; no Microsoft or Entra identity is introduced, and no MSAL package enters the desktop projects. This FRD sentence is the authority for that, and plan 04 § 6 lists `entra-app-registration` under "Not applicable" for the same reason. |
| "Authorization is enforced in Core use cases and at every caller boundary. It fails closed without revealing case or source data." | `:25` | Step 8. The client **reports** failure states and enforces none: `AccountDisabled` and `PasswordChangeRequired` come from the gateway's problem types, and step 7's second-`401` rule surfaces a failure rather than retrying into a bypass. Enforcement itself is [[GWY-021]]'s (plan handle `DSK-04-04`). |
| "Sign-ins and authentication failures remain in the **security log**" (not permanent business history) | `:31` | Step 9. This ticket writes no business-history entry for a sign-in; the security event is the gateway's, written by the path [[GWY-019]] and [[GWY-020]] extend. The desktop's rolling log records the ordered steps and a correlation id, with every token literal redacted. |
| "No identity design, app registration, scope declaration, role table, file, or registration proves that a live caller exists or is accepted." | `:33` | The Verification section, which states plainly that the fake-handler tests prove the client's branches and **not** that the flow works against the gateway — step 13's local-stack run is the only live evidence, and it is the local Test/UAT stack (L-02), not production. |

**New documents this ticket is written to**, because `docs_todo: true`:

> **New ADR** — ADR-0102 (existing Pegasus credentials with a desktop token session),
> authored by [[FND-042]] (plan handle `DSK-04-01`); [[FND-006]] (plan handle `DSK-00-06`)
> also claims ADR-0102 — see [[FND-042]]'s plan for the ownership reconciliation.
> **New ADR** — ADR-0100 (native WinUI 3 desktop client inside this fork, which authorises
> `src/Pegasus.Desktop.Infrastructure`), authored by [[FND-026]] (plan handle `DSK-02-01`);
> [[FND-005]] (plan handle `DSK-00-05`) also claims it — see [[FND-026]]'s plan for the
> ownership reconciliation.
> **New FRD** — FRD-13 "Desktop operator experience", authored by [[FND-008]] (plan handle
> `DSK-00-08`), which will own the login, session-restore and blocked-state behaviour this
> client feeds.
> This plan is written to the decisions as recorded in
> `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 (decisions 1–3 and 8, and
> the session failure matrix) and `docs/desktop/02-architecture-and-foundation/README.md`
> § 3 decision 6; if ADR-0102 lands differently this plan is revised before implementation.

The programme-level authorities that also bind, each with the step that satisfies it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 8.2 Protocol | Post credentials over TLS; short-lived access token plus rotated refresh token; access token in memory; **do not store the user's password**; client version and correlation id on every request | Steps 4–7 |
| Proposal § 8.4 Session failure handling | Each failure condition has a defined desktop behaviour | Step 8 |
| Proposal § 11.1 What may be cached locally | Only the refresh handle, protected | Step 6 |
| Proposal § 17.1 Required controls | Token storage review; no secrets in the package | Steps 6 and 9 |
| Plan 04 § 3 decision 1 | Password + refresh grants, public client `pegasus-desktop`, scopes `pegasus.desktop offline_access`, **no browser round trip** | Step 4 |
| Plan 04 § 3 decision 2 | Rolling refresh — persist the **new** handle every time | Step 5 |
| Plan 04 § 3 decision 3 and the failure matrix | `password-change-required`; revocation is the gateway's | Step 8 |
| Plan 04 § 3 decision 8 | No secrets in the package — only base URL, feed URL and channel | Nothing in this diff adds a secret; asserted by the redaction and package scans |
| Plan 04 § 7 last trap | No password storage, not even "remember me" | Steps 5–6, and the on-disk assertion |
| Plan 02 § 3 decision 6 | DPAPI `DataProtectionScope.CurrentUser` under `ApplicationData.Current.LocalFolder`, **not** `PasswordVault`; access token in memory | Steps 3 and 6 |
| Plan 03 § *Correlation & client version* (`README.md:168`) | `X-Correlation-Id` and `X-Pegasus-Client-Version` on every `/api/v1` request | Step 7 — consumed from [[FND-031]]'s handler, not re-implemented |
| Plan 03 § *Problem details* (`README.md:167`) | Thirteen stable problem slugs | Step 8 — four of them map; no fourteenth invented |
| **L-01** | The gateway is `Pegasus.Web` evolved in place; `/connect/token` is the existing endpoint | Step 4 |
| **L-02** | Test/UAT is the local stack; no Azure test environment | Step 13 |
| `docs/engineering.md:76` tier 2 | Positive, contradictory, ambiguous **and** failure cases | Steps 11–12 |
| `docs/engineering.md:106-111` § Capability organization | No `Common`/`Helpers`/`Utilities`/`Services` folder | Step 3's `Session/` folder |
| `docs/engineering.md:194-199` § Test support | One fake per concept | Step 11 reuses [[FND-038]]'s three fakes |
| `Directory.Build.props:6-7` | `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended` | Step 3's "done when it compiles" |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff | Step 14 |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing, reviewer `pegasus-desktop-reviewer` |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, vendored from `microsoft/win-dev-skills`
  v0.5.0 `f1028dd5`, verified present with `BuildAndRun.ps1` beside it) →
  `microsoft-code-reference` (Microsoft Learn plugin).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_code_sample_search`) for `System.Security.Cryptography.ProtectedData` and
  `DelegatingHandler`.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-043` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's fourteen implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient and take.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` § 3
   (decisions 1–3 and 8, and the *Session failure matrix*) and § 7 in full. Call
   `get_doc_gates FND-043`, then `take_ticket FND-043`. Load `pegasus-desktop`, then
   `winui-dev-workflow`.
2. **Read the two projects this builds on before writing anything.** In
   `src/Pegasus.Desktop.Infrastructure` ([[FND-031]], plan handle `DSK-02-06`), find the
   credential-store interface and the `DelegatingHandler` that **already** adds
   `X-Pegasus-Client-Version` and `X-Correlation-Id`; in `src/Pegasus.Contracts`
   ([[FND-029]], plan handle `DSK-02-04`), find the problem-details envelope. Research
   assumption A-04-07-1 says the names may differ from the ones written here — **use the
   names that exist and record the difference in this document under a dated note**. If
   either project is missing, stop: this ticket is blocked behind those two.
3. **Add the session contract under a `Session/` folder.** `ISessionClient` with
   `SignInAsync(string userName, string password, CancellationToken)`,
   `RefreshAsync(CancellationToken)`, `SignOutAsync(CancellationToken)` and a read-only
   `CurrentAccessToken` that is **only ever a field in memory** — no property with a setter
   that could be serialised, no backing configuration entry. `Session/` is a capability
   folder; `docs/engineering.md:106-111` forbids `Common`, `Helpers`, `Utilities` and
   undifferentiated `Services`. Done when the project compiles under
   `TreatWarningsAsErrors=true` (`Directory.Build.props:6-7`).
4. **Implement the password grant.** `POST /connect/token`
   (`src/Pegasus.Web/Mcp/AutomationMcp.cs:25`) with
   `Content-Type: application/x-www-form-urlencoded` and exactly these fields:
   `grant_type=password`, `username`, `password`, `client_id=pegasus-desktop`,
   `scope=pegasus.desktop offline_access`. Parse `access_token`, `refresh_token`,
   `expires_in`, and on failure the OAuth `error` and `error_description`. **Confirm the
   field names with `microsoft_docs_search` for the OAuth 2.0 resource-owner password grant
   before writing them** (research assumption A-04-07-2) — a guessed field name produces
   `invalid_request` for every login and looks like a credential problem. There is **no**
   browser round trip: plan 04 § 3 decision 1 keeps the login screen native.
5. **Implement `RefreshAsync` as `grant_type=refresh_token`** with the stored handle, and
   **persist the new handle every time** — the grant is rolling (plan 04 § 3 decision 2), so a
   client that keeps the original handle stops working after the first refresh. Never persist
   the password, not under any name and not behind any "remember me" affordance (proposal
   § 8.2; plan 04 § 7). Do **not** implement the 8-hour absolute cap here: it is
   `StaffSessionPolicy.cs:10` and the gateway's token handler enforces it; the client treats
   `invalid_grant` as the end of the session.
6. **Persist the refresh handle only through [[FND-031]]'s DPAPI store** (`ProtectedData`,
   `DataProtectionScope.CurrentUser`, file under `ApplicationData.Current.LocalFolder` —
   plan 02 § 3 decision 6). Add a unit test that writes a handle, reads it back, **and asserts
   the on-disk bytes do not contain the plaintext handle**. That last assertion is the point:
   a round-trip test alone passes just as happily against a plaintext file.
7. **Add `SessionAuthorizationHandler : DelegatingHandler`** to the pipeline. It attaches
   `Authorization: Bearer <access token>` and nothing else — the version and correlation
   headers are already added by [[FND-031]]'s handler on every call, including
   unauthenticated ones, and duplicating them here would break [[FND-047]]'s (plan handle
   `DSK-04-11`) connectivity path. On a `401` carrying
   `WWW-Authenticate: Bearer error="invalid_token"`, call `RefreshAsync` **once**, then retry
   the original request **exactly once**. A second `401` surfaces as a session failure. Parse
   the header directly — do **not** reach for `Microsoft.AspNetCore.Authentication`, which
   [[FND-037]]'s (plan handle `DSK-02-12`) forbidden-prefix fact would turn red.
8. **Map the matrix to one closed enum of seven values.** `AccessTokenExpired`,
   `RefreshRevoked` (token endpoint `invalid_grant`), `AccountDisabled`
   (`urn:pegasus:problem:account-disabled`), `PasswordChangeRequired`
   (`urn:pegasus:problem:password-change-required`), `ClientUnsupported`
   (`urn:pegasus:problem:client-unsupported`, carrying `minimumVersion`), `Unreachable`
   (transport exception), `RateLimited` (HTTP `429`, carrying the `Retry-After` seconds).
   **Do not add a value the matrix does not list** — the slug list is closed at thirteen
   (`docs/desktop/03-gateway-api-and-data/README.md:167`) and seven view models switch on
   this enum. `Unreachable` and a bad credential are different values and must stay so: the
   matrix says a transport failure is "never shown as bad credentials".
9. **Extend [[FND-032]]'s log redactor** (plan handle `DSK-02-07`) so `access_token`,
   `refresh_token`, `password`, `Authorization` and `Set-Cookie` values are never written to
   the rolling log — extend the existing list, do not add a second redactor
   (`docs/engineering.md:194-199`). Add a test that runs a **full sign-in** against a stubbed
   handler and asserts the log file contains none of those five literals.
10. **Register in the generic host.** In `src/Pegasus.Desktop/App.xaml.cs` (composed by
    [[FND-032]]) register `ISessionClient`, the DPAPI store and `SessionAuthorizationHandler`,
    wiring the handler into the named `HttpClient` that talks to the gateway. Registration
    only — the host itself is not this ticket's.
11. **Write the tier-2 test set** in `tests/Pegasus.Desktop.ViewModelTests` against a fake
    `HttpMessageHandler`, reusing [[FND-038]]'s (plan handle `DSK-02-13`) `FixedTimeProvider`,
    `FakeGatewayClient` and `InMemoryCredentialStore` rather than adding a fourth fake:
    successful sign-in stores a refresh handle and **no password**; an expired access token
    triggers **exactly one** refresh and **one** retry; `invalid_grant` clears the store and
    reports `RefreshRevoked`; `429` reports `RateLimited` with the `Retry-After` value; a
    transport exception reports `Unreachable` and **never** invalid credentials. Assert the
    counts, not just the outcomes — "exactly one" is the property that prevents the loop.
12. **Run the suite.** `dotnet test tests/Pegasus.Desktop.ViewModelTests` — expected: all
    green, including the redaction test and the DPAPI round-trip.
13. **Prove it against the local stack.**
    `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start` (the script's `-Action`
    `ValidateSet` is `Start`, `Status`, `Smoke`, `Stop`, `Reset` — `Invoke-LocalDevelopment.ps1:3`),
    note the printed Web readiness URL, point the desktop `local` channel configuration at it,
    build and launch with `.codex/skills/winui-dev-workflow/BuildAndRun.ps1`, and sign in with
    the local Administrator the initialization step creates. Capture the correlation id from
    the rolling log as `command-log` proof. **If [[GWY-019]] has not landed, the password
    grant will not exist** — record that the live check could not run, rather than reporting a
    partial result as a pass. Never use the plaintext `Bootstrap:VerificationAccount` from
    `src/Pegasus.Web/appsettings.json` as a production login (plan 04 § 7).
14. **Documentation, simplification pass, PR.** Check both conditional documentation edits
    before writing either: `docs/current-architecture.md` § Authentication and authorization
    boundary waits for [[GWY-019]]'s server half, and the `docs/capabilities.md` `DSK-03` row
    waits for [[FND-008]] (plan handle `DSK-00-08`) to create the `DSK` family — if either
    gate is unmet, write nothing there and record why here. Run the simplification pass over
    this branch's own diff, record it under a dated `## Simplification pass` heading below,
    and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 2 — Core/domain** (`docs/engineering.md:76`). The tier
obliges "positive, contradictory, ambiguous, and failure cases" for the session state machine
— valid credentials, expired access token, revoked refresh, disabled account, rate limit and
transport failure — each asserted in a view-model test **without the dispatcher**. State the
limit plainly in the proof: these tests prove the client's branches against a fake handler,
**not** that the flow works against the gateway; the only live evidence is step 13's local
Test/UAT run, and `docs/frd/frd-04-parties-accounts-and-access.md:33` says no design or
registration proves a live caller exists. Proof types: `test-output` and `command-log`.

| Command / observation | Expected evidence |
| --- | --- |
| `dotnet test tests/Pegasus.Desktop.ViewModelTests` | `Passed!` with the sign-in, refresh-once-and-retry, `invalid_grant`, `429`/`Retry-After`, transport-failure, redaction and DPAPI round-trip tests all green, zero skipped |
| `dotnet build Pegasus.slnx -c Release` on Windows | `Build succeeded` with `0 Warning(s)` — `TreatWarningsAsErrors=true` makes anything else a failure |
| `dotnet test tests/Pegasus.ArchitectureTests` | `Passed!` — [[FND-037]]'s desktop boundary facts stay green; no `Pegasus.Infrastructure`, EF, Azure or `Microsoft.AspNetCore.*` reference reached this project |
| The DPAPI round-trip test's on-disk assertion | the stored file's bytes do **not** contain the plaintext handle |
| The redaction test's log assertion | the rolling log contains none of `access_token`, `refresh_token`, `password`, `Authorization`, `Set-Cookie` |
| `grep -rn 'password' src/Pegasus.Desktop.Infrastructure/Session` reviewed by hand | the password appears only as a method parameter and a form field — never assigned to a field, property or store |
| `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start`, then a manual sign-in from the running desktop app | the shell opens and the rolling log records the ordered sign-in lines with **one** correlation id and no token literal |
| `git diff --name-only` at PR time | `src/Pegasus.Desktop.Infrastructure/**`, `src/Pegasus.Desktop/App.xaml.cs`, `tests/Pegasus.Desktop.ViewModelTests/**`, and the two `docs/` files only if their gates were met; **no** `src/Pegasus.Web/**`, `src/Pegasus.Core/**`, `src/Pegasus.Infrastructure/**`, `src/Pegasus.Worker/**` |
| Observations stated rather than inferred | whether the [[FND-031]] type names matched (A-04-07-1); whether [[GWY-019]] had landed, and so whether step 13's live check ran at all; whether either conditional `docs/` edit was written |

## Risks / open questions

- **Risk — an infinite refresh/retry loop.** A revoked refresh token plus a `401` produces a
  cycle that looks to the operator like a hang. Mitigation: step 7's counter, and step 11's
  test asserting **exactly one** refresh and **one** retry rather than merely "a retry
  happened".
- **Risk — a transport failure reported as a bad credential.** Both states show "you cannot
  sign in", which is why this is the defect most likely to survive review. Mitigation: the
  matrix makes it explicit ("never shown as bad credentials"), `Unreachable` is a distinct
  enum value, and step 11 tests it by name.
- **Risk — a rolling refresh that keeps the original handle.** Works once, then fails
  silently at the first refresh. Mitigation: step 5 states it and step 11's `invalid_grant`
  test only passes if the new handle is the one stored.
- **Risk — a guessed form-field name.** Produces `invalid_request` for every login and reads
  as a credential problem. Mitigation: step 4 confirms the field names with
  `microsoft_docs_search` first, and the ticket body's own instruction is "do not invent a
  field".
- **Risk — the plaintext handle or a token literal reaching disk or the log.** Invisible
  until a security review. Mitigation: steps 6 and 9 each add an assertion that reads the
  artefact back; a round-trip test alone would not catch it.
- **Risk — a second header handler, credential store or redactor.** Mitigation: step 2 reads
  [[FND-031]]'s project first, and steps 7 and 9 say explicitly to extend rather than add.
  `docs/engineering.md:194-199` is the authority.
- **Risk — `Microsoft.AspNetCore.*` pulled in to parse `WWW-Authenticate`.** Mitigation: step
  7 parses the header directly, and [[FND-037]]'s architecture fact fails the build if not.
- **Scope boundary, not an open question — the server half.** [[GWY-019]] (plan handle
  `DSK-04-02`) registers the client and the grants; [[GWY-020]] (`DSK-04-03`) applies the
  rate limiters to `/connect/token`; [[GWY-021]] (`DSK-04-04`) does bearer authentication and
  the per-request enabled/stamp check; [[GWY-022]] (`DSK-04-05`) does revocation. This ticket
  changes no file under `src/Pegasus.Web`.
- **Scope boundary, not an open question — the UI.** [[FND-044]] (plan handle `DSK-04-08`)
  owns the login screen and the states it shows; [[FND-045]] (`DSK-04-09`) owns the startup
  orchestrator and the compatibility gate; [[FND-047]] (`DSK-04-11`) owns connectivity. This
  ticket owns the seven `SessionFailure` values they switch on.
- **Scope boundary, not an open question — the two conditional documentation edits.**
  `docs/current-architecture.md` waits on [[GWY-019]]; the `docs/capabilities.md` `DSK-03` row
  waits on [[FND-008]]. Step 14 checks rather than assumes.
- **Recorded gap, not an open question — `PAR-02`.** The sign-out parity row is marked **not
  inventoried** with its test column reading `to locate`
  (`docs/desktop/01-inventory-and-parity/parity-matrix.md:47`), so the sign-out half has less
  recorded evidence behind it than sign-in. Say so in the proof rather than implying parity
  was fully mapped; filling the row is [[FND-015]]'s (plan handle `DSK-01-02`) work.
- **Open questions**: none. The four research assumptions are settled by reading a project or
  running one documentation query, and every undecided item is owned by a named sibling
  ticket. No `open-questions` document is created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds C# and
tests, so `n/a — docs-only` does not apply._
