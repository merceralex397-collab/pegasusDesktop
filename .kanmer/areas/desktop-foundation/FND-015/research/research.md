# Research — FND-015: Parity rows for §13.1 access and session and §13.2 dashboard and work queues

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This is a `spike`. Its `research` document is the spike's **output**, not an input to it, and
its existence alone satisfies the `enter-done` gate (`get_doc_gates FND-015`: `enter-done`
needs `research` + `questions-resolved`, and it is this profile's only gated boundary). This
file was written **before the spike ran**, as the pre-work scaffold the authoring contract
requires. Everything under **Facts** is verified against the repository with the command that
produced it. Everything marked `NOT YET CAPTURED` is still owed and has an unticked box in this
ticket's `open-questions` document — those boxes, not this banner, are what block `enter-done`.

Baseline: `git rev-parse HEAD` → `bbd1c54959e8c3a361d3f73965b61d6e4aff59ec`, read 2026-08-24.
Re-stamp every number against the head you actually work at.

**Dependency, not an open question:** this ticket is written against the confirmed skeleton of
[[FND-014]] (plan handle `DSK-01-01`); the board already records that edge (`FND-014` blocks
`FND-015`). Read [[FND-014]]'s `research` with `get_ticket_doc` before writing a cell.

## Question

For the eight parity rows this ticket owns — `PAR-01`…`PAR-06`, `PAR-42`, `PAR-44` — what are
the exact entry points, handlers, `path:line` behaviour evidence, FRD owners and test files
that let each row move to `inventoried` (or, for `PAR-42`, stay `legacy path retained`), and
where does no test exist so the cell must say `gap:` instead of inventing one?

## Current behaviour

The rows this ticket owns, with the status each carries today
(`grep -n '^| PAR-0[1-6] \|^| PAR-42 \|^| PAR-44 ' docs/desktop/01-inventory-and-parity/parity-matrix.md`):

| Row | §13.x | Entry point | Status today |
| --- | --- | --- | --- |
| `PAR-01` | 13.1 Access and session | `Account/SignIn.cshtml.cs` | `inventoried` |
| `PAR-02` | 13.1 | `Account/SignOut.cshtml.cs` | `not inventoried` |
| `PAR-03` | 13.1 | `Account/PasswordChange.cshtml.cs` | `not inventoried` |
| `PAR-04` | 13.1 | `Account/AccessDenied.cshtml.cs` | `inventoried` |
| `PAR-05` | 13.2 Dashboard and work queues | `Index.cshtml.cs` + `Presentation/RailCountsPageFilter.cs` | `inventoried` |
| `PAR-06` | 13.2 | `Search/Index.cshtml.cs` | `inventoried` |
| `PAR-42` | 13.1 (external connectors) | `Connect/Authorize.cshtml.cs` | `legacy path retained` |
| `PAR-44` | 13.1 | cross-cutting: fallback policy + `[Authorize(Roles=…)]` | `not inventoried` |

How the web application does this today, end to end:

1. **Sign in.** `POST /Account/SignIn` → `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:54`
   `OnPostAsync`. The account is checked for `IsEnabled` *before* the password
   (`:62-64`), and the password check runs with `lockoutOnFailure: false` (`:64`) — Pegasus
   throttles, it does not lock out. Two rate limits guard the route: the named policy
   `"StaffSignIn"` (`src/Pegasus.Web/Program.cs:42`, registered `:298`, per-client permit
   `StaffSessionPolicy.SignInAttemptsPerClientPerMinute` at `:304`) and a global fixed-window
   limiter applied by explicit middleware on `POST /Account/SignIn`
   (`Program.cs:797-817`, permit `SignInAttemptsGlobalPerMinute` at `:324`).
2. **Session.** Cookie `__Host-Pegasus`, `HttpOnly`, `SameSite=Strict`, `SecurePolicy=Always`
   (`Program.cs:368-376`), lifetimes from `src/Pegasus.Core/Actors/StaffSessionPolicy.cs`.
3. **Forced password change.** Middleware between `UseAuthentication()` and `UseAuthorization()`
   (`Program.cs:875-899`) redirects any authenticated non-anonymous request to
   `/Account/PasswordChange` when `user.MustChangePassword` is true, with a small allow-list
   (`PasswordChange`, `SignOut`, `/css`, `/js`, `/lib`, `/favicon.ico`) at `:884-890`.
4. **Deny by default.** `Program.cs:517-522` sets a fallback policy of `RequireAuthenticatedUser()`
   and adds the `"Administrator"` role policy; the Core rights matrix is
   `src/Pegasus.Core/Identity/StaffAuthorization.cs`.
5. **Dashboard and rail counts.** `Pages/Index.cshtml.cs:27` `OnGetAsync` renders the dashboard;
   `Presentation/RailCountsPageFilter.cs` supplies the rail counts and is registered as a
   **global** MVC filter at `Program.cs:260-261`, so it runs on every Razor page, not only the
   dashboard.
6. **Search.** `Pages/Search/Index.cshtml.cs:27` `OnGet()` is a redirect into Cases — there is
   no separate search surface.
7. **External connector consent.** `Pages/Connect/Authorize.cshtml.cs` (`:46` `OnGetAsync`,
   `:80` `OnPostAcceptAsync`, `:130` `OnPostDenyAsync`) is the OpenIddict consent page for
   external MCP connectors, governed by
   `docs/adr/0027-authorization-code-for-external-mcp-connectors.md`.

## Findings

- Every `Program.cs` line range the ticket body cites is **exact at `bbd1c549`** — F-4.
- `PAR-01`'s behaviour cell is **incomplete, not wrong**: it names the `StaffSignIn` policy and
  the global 100/min limiter but omits the per-client limit of 10/min — F-5.
- The body cites `SignIn.cshtml.cs:63` for the `lockoutOnFailure: false` call; the call is on
  `:64` and the statement spans `:62-64` — F-6. A one-line citation correction.
- `PAR-44`'s test-evidence guess (`tests/Pegasus.ArchitectureTests`?) is **wrong**: no
  architecture test mentions `StaffAccessRight` — F-10.
- The rail-count filter is **global, not dashboard-scoped** — F-11. Acceptance criterion 3
  ("rail counts are covered by the `PAR-05` row") is satisfiable, but the cell must say so.
- **The matrix has no cloud-placement column**, though body step 7 and area 01 § 1 both assume
  one — F-13. The default taken is recorded below and the schema question is parked.

### Facts

Verified at `bbd1c549` on 2026-08-24, each with its command.

- **F-1 — Row set and current statuses**: the table under *Current behaviour*, read from
  `docs/desktop/01-inventory-and-parity/parity-matrix.md`. Three of the eight rows are already
  `inventoried` (`PAR-01`, `PAR-04`, `PAR-05`, `PAR-06` — four), one is `legacy path retained`
  (`PAR-42`), three are `not inventoried` (`PAR-02`, `PAR-03`, `PAR-44`). The already-inventoried
  rows are re-verified, not assumed.
- **F-2 — Handlers on the owned surface.**
  `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Account' 'src/Pegasus.Web/Pages/Index.cshtml.cs' 'src/Pegasus.Web/Pages/Search' 'src/Pegasus.Web/Pages/Connect'`
  → **11 handlers across 6 files**, exactly matching the body's step-3 expectation:

  | File | Handlers (line) |
  | --- | --- |
  | `Account/SignIn.cshtml.cs` | `OnGet` (`:44`), `OnPostAsync` (`:54`) |
  | `Account/SignOut.cshtml.cs` | `OnGet` (`:10`, `=> RedirectToPage("/Index")`), `OnPostAsync` (`:12`) |
  | `Account/PasswordChange.cshtml.cs` | `OnGetAsync` (`:53`), `OnPostAsync` (`:59`) |
  | `Account/AccessDenied.cshtml.cs` | **none** — 7 lines, no handler (see F-3) |
  | `Pages/Index.cshtml.cs` | `OnGetAsync` (`:27`) |
  | `Search/Index.cshtml.cs` | `OnGet` (`:27`) |
  | `Connect/Authorize.cshtml.cs` | `OnGetAsync` (`:46`), `OnPostAcceptAsync` (`:80`), `OnPostDenyAsync` (`:130`) |
- **F-3 — `AccessDenied` declares no handler.** `wc -l src/Pegasus.Web/Pages/Account/AccessDenied.cshtml.cs`
  → 7. It is the only one of the 53 page models with zero handlers ([[FND-014]] F-7). `PAR-04`
  records it as `Account/AccessDenied.cshtml.cs (7)` with the behaviour "Static" — consistent.
  The row must state "no handler" explicitly rather than leaving the reader to wonder whether
  the enumeration missed it.
- **F-4 — Every cited `Program.cs` range is exact.** `sed -n` over each:
  `:262-274` Identity options (`AddIdentity` at `:262`; `Lockout.AllowedForNewUsers = false` at
  `:270`; `SignIn.RequireConfirmedAccount = false` at `:271`); `:275-327` `AddRateLimiter`;
  `:368-457` `ConfigureApplicationCookie` (`__Host-Pegasus` at `:370`, `SameSite=Strict` `:374`,
  `SecurePolicy=Always` `:375`, `ExpireTimeSpan = StaffSessionPolicy.IdleLifetime` `:376`);
  `:517-522` `AddAuthorizationBuilder().SetFallbackPolicy(… RequireAuthenticatedUser …)` plus the
  `"Administrator"` role policy; `:797-817` the explicit `POST /Account/SignIn` limiter
  middleware; `:875-899` the `MustChangePassword` redirect middleware.
- **F-5 — The session and throttling constants, and what `PAR-01` omits.**
  `src/Pegasus.Core/Actors/StaffSessionPolicy.cs` is 14 lines:
  `IdleLifetime = 2 h` (`:9`), `AbsoluteLifetime = 8 h` (`:10`),
  `SignInAttemptsPerClientPerMinute = 10` (`:12`),
  `SignInAttemptsGlobalPerMinute = 100` (`:13`).
  `git grep -n "StaffSignIn\|SignInAttempts" src/Pegasus.Web/Program.cs` →
  `:42 const string StaffSignInRateLimitPolicy = "StaffSignIn";`, `:298` policy registration,
  `:304 PermitLimit = StaffSessionPolicy.SignInAttemptsPerClientPerMinute`,
  `:324 PermitLimit = StaffSessionPolicy.SignInAttemptsGlobalPerMinute`.
  **`PAR-01`'s cell names the policy and the global 100/min but not the per-client 10/min.**
  That is the number that decides whether a desktop retry loop trips the limiter, so it belongs
  on the row.
- **F-6 — Lockout is off; the citation is one line out.**
  `sed -n '62,64p' src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs` shows the statement
  `var result = user is null || !user.IsEnabled ? SignInResult.Failed : await signInManager.CheckPasswordSignInAsync(user, Password, lockoutOnFailure: false);`
  spanning `:62-64`, with the call on **`:64`**. The body (and the matrix) cite `:63`, which is
  the ternary's `Failed` branch. Cite `:62-64` for the statement or `:64` for the call.
  `Program.cs:270` (`Lockout.AllowedForNewUsers = false`) is the other half of "throttle, do not
  lock out" and belongs on the same cell.
- **F-7 — The rights matrix has exactly 12 values, fail-closed.**
  `src/Pegasus.Core/Identity/StaffAuthorization.cs:7` `public enum StaffAccessRight`, values at
  `:9-20`: `AccessStaffApplication`, `PerformCasework`, `ManageStaffAccounts`,
  `ReviewStaffAccess`, `AssignStaffRoles`, `ManageOrganizationsAndPrincipals`,
  `ManageWorkflowConfiguration`, `ManageApprovedMailboxes`, `ManageApprovedOutlookCategories`,
  `ManageAutomationClients`, `ExecuteSystemWork`, `SubmitRequestUpload`. The file's own summary
  (`:23-26`) states the boundary is "shared by Web, Worker and later authenticated transports"
  and that "unknown actor/permission combinations fail closed" — that sentence is the `PAR-44`
  behaviour evidence, and it is also why the desktop can hide commands without weakening
  enforcement.
- **F-8 — `MustChangePassword` allow-list.** `Program.cs:884-890` permits
  `/Account/PasswordChange`, `/Account/SignOut`, `/css`, `/js`, `/lib`, `/favicon.ico`; every
  other authenticated non-`[AllowAnonymous]` request redirects at `:892-893`. The desktop
  equivalent (proposal §8.4, "password reset required") must route the forced-change state
  *before* the shell, and `SignOut` must stay reachable from it — that pair is the behaviour
  `PAR-03` has to record.
- **F-9 — `PAR-42` still matches the code.** `src/Pegasus.Web/Pages/Connect/Authorize.cshtml.cs`
  is 177 lines with the three handlers of F-2; ADR-0027 exists at
  `docs/adr/0027-authorization-code-for-external-mcp-connectors.md`. It has **not** become a
  staff surface, so `legacy path retained` stands. One code-hygiene oddity to state on the row
  rather than act on: `:24` reads `public sealed class AuthorizeModel : AdministrationPageModel`
  — an external-audience consent page inheriting the administration base
  ([[FND-014]] F-9a). It changes no observable behaviour for this row.
- **F-10 — Step 5's test searches, run.**
  - `git grep -rln "SignOut\|PasswordChange\|MustChangePassword" tests/` → **7 files**:
    `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs`,
    `ShellAndStatusPageWebTests.cs`, `AdministrationSearchAccountWebTests.cs`,
    `QdosCustodialWebTests.cs`, `MailWorkspaceWebTests.cs`, `CaseWorkflowPersistenceTests.cs`,
    `Browser/AccessibilityTests.cs`.
  - `git grep -rln "StaffAccessRight" tests/` → **4 files**:
    `tests/Pegasus.Core.Tests/Identity/AutomationActorTests.cs`,
    `tests/Pegasus.Core.Tests/Lifecycle/CaseEditLeaseTests.cs`,
    `tests/Pegasus.Core.Tests/Workflow/CaseEditAuthorityTests.cs`,
    `tests/Pegasus.IntegrationTests/QdosCustodialWebTests.cs`.
    **None is under `tests/Pegasus.ArchitectureTests`**, so `PAR-44`'s current cell
    ("`tests/Pegasus.ArchitectureTests`? to locate") points at a directory that does not test
    this. Replace it with a real path or a `gap:`.
  - `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs` contains **one** fact,
    `DeniedAttemptIsRetainedAndSuccessfulCookieSignInWritesOneSuccessEvent` (`:18`), which
    covers **security-event writing**, not the rate limit and not the forced-change redirect.
    Whether that counts as `PAR-01`/`PAR-03` evidence is a judgement the spike must make **by
    opening the file**, not from the name — recorded as a candidate, not a conclusion (U-3).
- **F-11 — Rail counts are global, not dashboard-scoped.**
  `src/Pegasus.Web/Program.cs:260-261`:
  `builder.Services.AddRazorPages().AddMvcOptions(options => options.Filters.Add<Pegasus.Web.Presentation.RailCountsPageFilter>());`
  — a **global** MVC filter, so `Presentation/RailCountsPageFilter.cs` (51 lines) runs on every
  Razor page. `Pages/Index.cshtml.cs` is 43 lines with one handler. Acceptance criterion 3
  ("rail counts are covered by the `PAR-05` row") is met by naming the filter *and* its global
  registration — otherwise a desktop implementer will build rail counts as a dashboard call and
  find them missing on every other screen.
- **F-12 — FRD owners exist.** `docs/frd/frd-04-parties-accounts-and-access.md` and
  `docs/frd/frd-12-operator-experience.md` are this ticket's `refs`
  (`get_doc_gates FND-015`); `docs/frd/frd-10-mcp-automation-and-actor-boundary.md` is the
  additional owner the body names for `PAR-42`. All three are tracked files.
- **F-13 — The matrix has no cloud-placement column, and proposal §4.1 has the answers.**
  The matrix header is ten columns — `ID | Capability group (§13.x) | FRD owner | Current entry
  point | Current behaviour evidence | Native screen/use case | API/data dependency | Test
  evidence | UAT owner | Status`. There is **no placement column**, although body step 7 says
  "answer the cloud-placement column" and
  `docs/desktop/01-inventory-and-parity/README.md:37-38` says "each inventory row carries the
  placement column". Proposal § 4.1 (`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:140-162`)
  gives the three values this ticket needs, verbatim:
  `Native UI, navigation and state` → **Desktop**; `User login screen` → **Split**;
  `Case/query presentation` → **Split**.
  **Default taken** (rather than asking): record the §4.1 value inside the existing
  "Native screen/use case" cell as a leading `Placement: <value> (proposal §4.1)` clause. That
  answers step 7 without a schema change that would touch all 46 rows and every sibling ticket
  mid-fill. The schema question itself is parked in `open-questions`.
- **F-14 — Both documentation gates take no parameters.** `scripts/Test-DocumentationLinks.ps1:9`
  and `scripts/Test-MarkdownPlacement.ps1` are `[CmdletBinding()] param()`. The body's
  Verification commands are runnable as written.

### Assumptions

- **A-01-02-1 — The eight rows are the complete §13.1/§13.2 surface.** Confirmed by F-2: the
  11 handlers across `Account/`, `Index.cshtml.cs`, `Search/` and `Connect/` map onto exactly
  these rows with none left over. Breaks if [[FND-014]]'s difference list (a) turns up a
  §13.1/§13.2 page model with no row — body step 2 then requires adding the row here, using the
  matrix's exact ten columns. **Confirm by reading [[FND-014]]'s difference lists**, which are
  not yet written.
- **A-01-02-2 — `StaffSignInSecurityTests` is not rate-limit evidence.** Based on its single
  fact name (F-10), which is about security-event writing. Breaks if the file asserts a 429 or
  a `Retry-After` internally. **Confirm by opening the file** — not yet done (U-3). If it is
  not evidence, `PAR-01`'s rate-limit behaviour is a `gap:` for [[FND-025]] (plan handle
  `DSK-01-12`), and that is a genuinely untested production behaviour worth surfacing.
- **A-01-02-3 — `Browser/AccessibilityTests.cs` exercises the sign-in page** (it appears in the
  F-10 search). Breaks if the match is incidental (for example a `SignOut` link in a shell
  assertion). **Confirm by opening the file.** An accessibility test that covers the login page
  is real `PAR-01` evidence for the desktop's a11y parity target; a shell-link match is not.
- **A-01-02-4 — The already-`inventoried` rows (`PAR-01`, `PAR-04`, `PAR-05`, `PAR-06`) were
  filled from the same code and only need completing, not rewriting.** Breaks where a cell is
  actively wrong — F-5 (missing per-client limit) and F-6 (one-line citation drift) are already
  two such cases, so treat "already inventoried" as "already drafted", never as "already
  verified".
- **A-01-02-5 — Recording placement in-cell (F-13) is acceptable to the reviewer.** Breaks if
  `pegasus-desktop-reviewer` or the operator wants an eleventh column. The cost of being wrong
  is one later edit pass over 46 rows; the cost of adding the column now is a schema change
  landing in the middle of four concurrent row-population tickets. **Confirm at review.**

## Execution placement

**This ticket places no responsibility anywhere.** It is read-only inspection of
`src/Pegasus.Web`, `src/Pegasus.Core` and `tests/`, plus edits to
`docs/desktop/01-inventory-and-parity/parity-matrix.md` and possibly one line in
`docs/open-decisions.md`. It starts no process, carries no credential, publishes no artefact,
and makes no Azure call (Guardrails: "no write. This ticket makes no Azure call."). The
six-question cloud-justification test of `docs/desktop/00-governance-and-workflow/README.md`
§ 3 is therefore not answered here.

The one placement it **assumes**: the enumeration runs on a developer workstation against a
local checkout and its output is a repository document.

The *rows*' placement values are a different thing and are **recorded, not decided**, here —
they are read verbatim from proposal § 4.1 (F-13). Body step 7 is explicit: "Record the answer,
not an opinion; six-question tables belong to the ADRs authored by [[FND-005]] (plan handle
`DSK-00-05`)."

## Implications

1. **Two cells are wrong before the spike starts** — `PAR-01`'s missing per-client rate limit
   (F-5) and `PAR-44`'s architecture-test guess (F-10). Both would have been copied forward.
2. **The per-client limit of 10/min is a desktop design constraint, not trivia.** A desktop
   client that retries sign-in on failure will hit it long before the global 100/min. It belongs
   on `PAR-01` and it is the kind of number area 04's session client must respect.
3. **The forced-change state must be routed before the shell** and must keep sign-out reachable
   (F-8). That is a two-state requirement for area 04's login screen, derivable only from the
   allow-list.
4. **The global rail-count filter (F-11) changes the desktop shape**: rail counts are ambient,
   not a dashboard payload. `~GET /api/v1/rail-counts` in `PAR-05` is therefore a shell-level
   call in area 03's endpoint map, not a dashboard-only one.
5. **`PAR-42` closes cleanly.** Nothing about it has changed; one sentence citing ADR-0027 and
   its external audience is the whole job, plus the inheritance note.
6. **A likely genuine test gap sits on the most security-sensitive row.** If A-01-02-2 holds,
   nothing tests the sign-in rate limit — worth a `gap:` line and a mention to [[FND-025]].

---

## NOT YET CAPTURED — the spike's remaining work

Each block names the exact command and the question its output must answer; each has one
unticked box in `open-questions`.

### NOT YET CAPTURED — U-1: the row-by-row citation table

**Command:** none — assembly. One table for the eight rows:
`PAR id → entry point → handlers (path:line) → behaviour evidence (path:line only) → FRD owner
→ capability group → test file or gap: → placement (§4.1) → inventoried-at SHA`, written into
this document (body step 11).
**Question it must answer:** can area 04 build the login screen, the forced-change state and
the role-aware shell from this table alone?

### NOT YET CAPTURED — U-2: handler-to-row mapping proof

**Command:** `git grep -n "public .*On\(Get\|Post\)" -- 'src/Pegasus.Web/Pages/Account' 'src/Pegasus.Web/Pages/Index.cshtml.cs' 'src/Pegasus.Web/Pages/Search' 'src/Pegasus.Web/Pages/Connect'`
**Question it must answer:** does every one of the 11 printed handlers (F-2) appear in exactly
one of this ticket's rows — the ticket's first Verification item?

### NOT YET CAPTURED — U-3: test evidence resolved for `PAR-01`, `PAR-02`, `PAR-03`, `PAR-44`

**Commands:** open each candidate from F-10 and read what it asserts —
`tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs`,
`ShellAndStatusPageWebTests.cs`, `AdministrationSearchAccountWebTests.cs`,
`Browser/AccessibilityTests.cs`, and the four `StaffAccessRight` files.
**Question it must answer:** for each row, is there a test that actually asserts the behaviour
the cell claims? Where there is not, write `gap: <what is untested>` — never a test name that
does not assert it. Settles A-01-02-2 and A-01-02-3.

### NOT YET CAPTURED — U-4: `gap:` lines handed to [[FND-025]]

**Command:** none — copy each `gap:` line written in U-3 into this research document under a
`### Gap list for DSK-01-12` heading (body step 5).
**Question it must answer:** does [[FND-025]] (plan handle `DSK-01-12`) receive every gap this
ticket found, in a form it can consume without re-deriving it?

### NOT YET CAPTURED — U-5: the matrix edits

**Command:** none — the edit itself. Advance `PAR-01`…`PAR-06` and `PAR-44` to `inventoried`,
keep `PAR-42` at `legacy path retained` with its ADR-0027 sentence, stamp the inventoried-at
SHA on every touched row, leave every `~`-prefixed endpoint name and every blank UAT owner
untouched.
**Question it must answer:** does the diff change only the eight owned rows — and no `~` name,
no UAT owner, and no row belonging to a sibling?

### NOT YET CAPTURED — U-6: the corrections this research already identified are applied

**Command:** none — apply F-5 (add the per-client 10/min to `PAR-01`), F-6 (cite `:62-64` or
`:64`, not `:63`), F-10 (`PAR-44`'s architecture-test guess replaced), F-11 (rail-count filter
named as global with its `Program.cs:260-261` registration).
**Question it must answer:** are all four corrections in the diff, each with its `path:line`?

### NOT YET CAPTURED — U-7: the documentation gates

**Commands:** `pwsh ./scripts/Test-DocumentationLinks.ps1` and
`pwsh ./scripts/Test-MarkdownPlacement.ps1`, both expected to exit 0.
**Question it must answer:** do the edits keep the CI `documentation` job green?

### NOT YET CAPTURED — U-8: reviewer spot-check

**Command:** open three cited `path:line` references from the changed rows.
**Question it must answer:** does each cited line say what its cell claims — the ticket's fourth
Verification item?

## Open questions

Tracked as unticked items in this ticket's `open-questions` document; every one must be ticked
before `enter-done`.

- U-1 … U-8 above.
- **Is the sign-in rate limit tested at all?** (A-01-02-2 / U-3). If not, `PAR-01` carries a
  `gap:` on a security-relevant behaviour and [[FND-025]] should rank it high.

**Not open questions — scope boundaries owned by named tickets:**

- The confirmed skeleton and the three difference lists: [[FND-014]] (plan handle `DSK-01-01`).
- Promoting a `~` endpoint name to a decided name: area 03's endpoint map, not this ticket
  (body step 8; `parity-matrix.md` § Notes).
- Assigning a UAT owner: the operator, per capability group, before any row passes
  `automated verification passed` (body step 10).
- Building the login screen, the session client and the role-aware shell: [[FND-043]]
  (`DSK-04-07`), [[FND-044]] (`DSK-04-08`) and [[FND-046]] (`DSK-04-10`).
- The characterization-gap list the `gap:` cells feed: [[FND-025]] (`DSK-01-12`).
- Whether the matrix moves to `docs/features/`: [[FND-012]] (plan handle `DSK-00-12`).
