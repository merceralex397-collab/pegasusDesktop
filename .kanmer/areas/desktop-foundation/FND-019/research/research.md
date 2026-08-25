# Research — FND-019: flow records 1–3 (staff authentication, database and migrations, Graph intake)

> **STATUS — COMPLETE as of 2026-08-25.**

This document is the spike's **output**, not an input to it. `get_doc_gates FND-019`
resolves profile `spike` to one gated boundary — `enter-done` needs `research` and
`questions-resolved` — so the existence of this file is what would let the ticket
close. It is a pre-work scaffold: everything under **Facts** was verified by a
read-only command that is quoted beside it, and every answer this ticket owes is a
literal `Resolved question record` block. The `open-questions` document carries one unticked
`- [ ]` box per uncaptured item, and those boxes are the actual gate.

## Question

For the three flows the desktop conversion depends on most — staff authentication
and session, the database and migration bundle, and Microsoft Graph intake — what
does the code do today, and what must be answered before area 04 builds the token
flow and before [[FND-006]] (plan handle `DSK-00-06`) can author ADR-0102, ADR-0106
and ADR-0109? Proposal §24 Phase 0 exit-gate item 3 states the bar verbatim: "No
unresolved uncertainty exists around authentication, database or Graph intake."

## Current behaviour

### Record 1 — staff authentication and session

The web application signs an operator in through a Razor page and keeps the session
in a cookie:

- `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:63` —
  `CheckPasswordSignInAsync(user, Password, lockoutOnFailure: false)`. ADR-0013
  clause 12: throttling, not lockout.
- `src/Pegasus.Web/Program.cs:263` — `AddIdentity<PegasusIdentityUser, IdentityRole<Guid>>`;
  `:265` `Password.RequiredLength = 8`, `:266` `Password.RequireDigit = false`.
- `src/Pegasus.Web/Program.cs:275` — `AddRateLimiter`; `:277`
  `RejectionStatusCode = StatusCodes.Status429TooManyRequests`. The `StaffSignIn`
  policy and the global fixed-window limiter are applied to `POST /Account/SignIn`
  at `:797-817`.
- `src/Pegasus.Web/Program.cs:328-332` — `AddAuthentication` with the policy scheme
  that forwards to `DevelopmentOfflineAuthenticationHandler` in the DevelopmentOffline
  runtime profile and to the Identity application cookie otherwise.
- `src/Pegasus.Web/Program.cs:353` — `options.ValidationInterval = TimeSpan.Zero`,
  i.e. the security stamp (and therefore `IsEnabled`) is re-validated on **every**
  request; `:368-372` `ConfigureApplicationCookie`, cookie `__Host-Pegasus`.
- `src/Pegasus.Web/Program.cs:517-522` — fallback policy `RequireAuthenticatedUser()`
  plus the named `Administrator` policy over `StaffRoleNames.Administrator`.
- `src/Pegasus.Web/Program.cs:875-880` — the `MustChangePassword` redirect middleware
  (`app.Use(...)`, skipping endpoints carrying `IAllowAnonymous`).
- `src/Pegasus.Core/Actors/StaffActorFactory.cs:8-40` —
  `TryCreate(string? subjectId, IEnumerable<string> roleNames, out ActionActor?)`.
  It parses `subjectId` as a non-empty `Guid`, parses every role name as a
  `StaffRole` **case-sensitively**, refuses an unknown role, refuses an empty role
  set, and only then produces `ActionActor.Staff(staffId, roles)`. This is the
  transport-neutral seam a token client must satisfy.
- `src/Pegasus.Web/Program.cs:172-176` — the Data Protection key ring is persisted to
  the blob `authentication-ring/keys.xml` on the custody storage account.

Parity rows that cover this: **`PAR-01`** (`Account/SignIn.cshtml.cs`),
**`PAR-02`** (`Account/SignOut.cshtml.cs`), **`PAR-03`**
(`Account/PasswordChange.cshtml.cs`) and **`PAR-04`**
(`Account/AccessDenied.cshtml.cs`) —
`docs/desktop/01-inventory-and-parity/parity-matrix.md:46-49`.

### Record 2 — database and migration bundle

**No parity row covers this, and none should.** The matrix holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`),
every row keyed to a page model under `src/Pegasus.Web/Pages/**`
(`parity-matrix.md:36-38`, "Current entry point — page model path under
`src/Pegasus.Web/Pages/`"). The migration stream is infrastructure, not an
observable capability, so the closest existing repository mechanisms are:

- `scripts/Build-ReleaseArtifacts.ps1:70` — builds the migration bundle
  (`dotnet ef migrations bundle --self-contained -r win-x64 --project
  src/Pegasus.Infrastructure --startup-project src/Pegasus.Web`).
- `scripts/Test-MigrationGrants.ps1` — the CI gate that every created table carries a
  `GRANT` or an explicit opt-out, run by the `.github/workflows/ci.yml` grant job.
- `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs` and the pinned
  migration census in `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs`.

### Record 3 — Microsoft Graph intake

The Worker owns every poll; the staff surface is read-only over what the Worker
retained:

- `src/Pegasus.Worker/MailboxFunctions.cs:15` — `[Function(nameof(InboxPollFunction))]`.
- `src/Pegasus.Worker/EmailEvidenceFunctions.cs:16` — `SentEvidencePollFunction`;
  `:53` — `DueWorkSweepFunction`.
- `src/Pegasus.Worker/IntakeFunctions.cs:13` — `PendingWorkDispatchFunction`; `:33` —
  `IntakeWorkFunction`; `:50` — `IntakePoisonFunction`; `:75` —
  `StagedArtifactReconciliationFunction`.
- `src/Pegasus.Worker/Functions/ExternalWorkFunctions.cs:9` — `ExternalWorkFunction`;
  `:27` — `ExternalPoisonFunction`.
- Graph adapter `src/Pegasus.Infrastructure/Email/GraphApprovedSources.cs`;
  retained read model `src/Pegasus.Core/Intake/RetainedMail.cs`.
- Queue transport and fail-closed activation: `infra/modules/platform.bicep:129-152`
  (the four queues), `:36` (`workerActivationApproved`), `:531-539` (the nine
  `AzureWebJobs.<function>.Disabled` settings).

Parity rows that cover the staff-visible half: **`PAR-21`** (`Mail/Index.cshtml.cs`),
**`PAR-22`** (`Mail/Message.cshtml.cs`), **`PAR-27`** (`Operations/Index.cshtml.cs`)
and **`PAR-35`** (`Administration/Mailboxes.cshtml.cs`) — `parity-matrix.md:66-67`,
`:72`, `:80`. The polling itself has no row, correctly: it is not a screen.

## Findings

- The `spike` gate is unusual and is the main trap on this ticket: `research` is owed
  at **`enter-done`**, not at `leave-preparing`, so a half-written research document
  makes an unfinished spike closable. Verified with `get_doc_gates FND-019` →
  `boundaries: [{ boundary: "enter-done", requirements: ["research",
  "questions-resolved"] }]`. Nothing gates `leave-backlog`.
- Two of the ticket's own verification commands do not run as written against this
  tree. Both are recorded under **Facts** with their real output and their corrected
  form; correcting the ticket text is owned by [[FND-052]], not by this ticket.
- Record 2's headline number is still correct at this head, but the ticket's
  verification command is not the command that produces it — see Fact F-2.
- Record 3's nine-function claim is correct; the command printed in the ticket's
  Verification block matches nothing, because the source uses `nameof`.

### Facts

Each fact carries the command that produced it. Commands were run in
`C:\Users\PC\Documents\GitHub\pegasusDesktop` on 2026-08-24 at
`main`-descendant `bbd1c549` (branch `task/desktop-plan-segmentation`).

- **F-1 — the parity matrix holds 46 rows, none of them for records 2 or 3's
  server-side halves.**
  `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`.
- **F-2 — there are 64 migrations, and the ticket's Verification command returns
  104, not 64.**
  `git ls-files src/Pegasus.Infrastructure/Persistence/Migrations | grep -c "\.cs$"`
  → `104`, because the folder also holds 39 `*.Designer.cs` files and
  `PegasusDbContextModelSnapshot.cs`
  (`... | grep -c "Designer.cs"` → `39`; `... | grep -c "ModelSnapshot"` → `1`).
  The migration count itself is **64**:
  `git ls-files src/Pegasus.Infrastructure/Persistence/Migrations | grep "\.cs$" | grep -v "\.Designer\.cs$" | grep -v "ModelSnapshot" | wc -l`
  → `64`. First and last, in id order:
  `20260724104624_InitialProviderNeutralIntake` and
  `20260822044425_GrantWorkerCaseDocuments` — exactly the range
  `flow-records.md:113-116` records. Record 2's own "Read-only verification" block
  uses `grep -c "_.*\.cs$"`, which returns `103`; neither published command yields
  64. **Default taken:** this document states the corrected command above and the
  implementer should use it; the wording fix in the ticket and in the record is a
  parked item for [[FND-052]].
- **F-3 — the Worker declares exactly nine functions, and the ticket's Verification
  command matches none of them.**
  `git grep -n "Function(\"" src/Pegasus.Worker` → **no output**, because every
  attribute is written `[Function(nameof(X))]`, not `[Function("X")]`. The command
  that works is `grep -rn '\[Function(' src/Pegasus.Worker --include=*.cs`, which
  returns nine attributes: `SentEvidencePollFunction`
  (`EmailEvidenceFunctions.cs:16`), `DueWorkSweepFunction`
  (`EmailEvidenceFunctions.cs:53`), `ExternalWorkFunction`
  (`Functions/ExternalWorkFunctions.cs:9`), `ExternalPoisonFunction`
  (`Functions/ExternalWorkFunctions.cs:27`), `PendingWorkDispatchFunction`
  (`IntakeFunctions.cs:13`), `IntakeWorkFunction` (`IntakeFunctions.cs:33`),
  `IntakePoisonFunction` (`IntakeFunctions.cs:50`),
  `StagedArtifactReconciliationFunction` (`IntakeFunctions.cs:75`),
  `InboxPollFunction` (`MailboxFunctions.cs:15`). That set matches the nine
  `AzureWebJobs.*.Disabled` names at `infra/modules/platform.bicep:531-539`
  one for one.
- **F-4 — `IsEnabled` is re-checked on every request today.**
  `sed -n '353p' src/Pegasus.Web/Program.cs` → `options.ValidationInterval = TimeSpan.Zero;`.
  This is the guarantee record 1 says must survive the move to tokens, and it is the
  reason `Q1.2` is a decision rather than a lookup.
- **F-5 — `StaffActorFactory.TryCreate` needs exactly two inputs, and is
  fail-closed on both.** `sed -n '1,40p' src/Pegasus.Core/Actors/StaffActorFactory.cs`:
  a `Guid`-parsable non-empty `subjectId`, and one or more role names that each parse
  as `StaffRole` with `ignoreCase: false`. An unknown role name, a lower-case role
  name, or an empty role set all return `false`. Any token claim design that carries
  role display names, or a single comma-joined roles claim, fails here.
- **F-6 — the DevelopmentOffline profile is Development-only.**
  `sed -n '104,110p' src/Pegasus.Web/Program.cs` — the profile throws
  `"The DevelopmentOffline runtime profile is permitted only in the Development
  environment."` outside Development. `git grep -ln "DevelopmentOfflineAuthenticationHandler" src/`
  → `src/Pegasus.Web/Program.cs` only. `Q1.4` is therefore about a local mechanism
  by construction; requesting an Azure test resource is out of bounds under L-02 and
  ADR-0014.
- **F-7 — OpenIddict already has migration history, so `Q1.1` is a grant question,
  not an existence question.**
  `git grep -ln "OpenIddict" src/Pegasus.Infrastructure/Persistence/Migrations`
  returns, among others,
  `20260730203833_RemoveDormantOpenIddict.cs`,
  `20260803151159_AutomationActorOpenIddict.cs`,
  `20260729176000_AzureSqlRuntimeLeastPrivilege.cs` and
  `20260729199000_RuntimeRoleReconciliation.cs`. The tables exist and the runtime
  roles exist; what is unproved is the per-table `GRANT` coverage for the
  refresh-token rotation path.
- **F-8 — the Data Protection ring and the token path share one dependency.**
  `sed -n '172,176p' src/Pegasus.Web/Program.cs` — `AddDataProtection()
  .SetApplicationName("Pegasus").PersistKeysToAzureBlobStorage(new
  Uri(custodyServiceUri, "authentication-ring/keys.xml"), credential)`. The register
  row for the `authentication-ring` container already says so
  (`azure-resource-register.md`, `authentication-ring` row).
- **F-9 — the repository has no `dev` branch and no `upstream` remote yet.**
  `git branch -a` → `kanmer-board`, `main`, `task/desktop-plan-segmentation`,
  `remotes/origin/main`; `git remote -v` → `origin` only. Any record correction this
  ticket writes lands on a task branch cut from `dev` once [[FND-001]] (plan handle
  `DSK-00-01`) has created it.

### Assumptions

- **A-01-1 — the `Q1.2` claim set is `sub` plus one `role` claim per `StaffRole`
  name, spelled exactly as the enum member.** Confirmed by: reading
  `src/Pegasus.Core/Identity/StaffAuthorization.cs` for the twelve
  `StaffAccessRight` values and the `StaffRole` enum, then proving a token principal
  through `StaffActorFactory.TryCreate` in a test. Breaks if the gateway instead
  projects rights (not roles) into the token — then `TryCreate` is bypassed and the
  fail-closed rights matrix has a second implementation, which
  `docs/engineering.md` § Engineering invariants ("one Core owner") forbids.
- **A-01-2 — no new table is needed for Phases 0–4 (`Q2.1`).** The plan's own
  expected answer. Confirmed by enumerating the desktop-held state ADR-0104 will
  permit (proposal §11.1) and showing each item is local or already persisted.
  Breaks if the compatibility gate of area 04 needs a server-side record of client
  versions; that would be a new table with a new grant, and
  `scripts/Test-MigrationGrants.ps1` would then be the first thing to satisfy.
- **A-01-3 — the Web runtime role's existing grants already cover the gateway's new
  retained-mail read endpoints (`Q3.2`).** The record itself says "they should — same
  Web role". Confirmed by reading the `Grant*` migrations for the retained-mail and
  search-projection tables and checking the reader is `pegasus_web_runtime_role`.
  Breaks in exactly the way upstream `PLAT-035` describes: a local full-privilege run
  proves nothing, and grants have shipped wrong three times
  (`20260814092852`, `20260821095500`, `20260822044425`).
- **A-01-4 — the desktop OpenIddict client is seeded the same way the Automation
  client is (`Q2.2`).** Confirmed by reading `src/Pegasus.Web/Mcp/AutomationMcp.cs`
  for the current seeding mechanism. Breaks if the Automation client is seeded by a
  path that assumes a single client, in which case the seeding shape is a decision
  for area 04, not a copy.

## Execution placement

This ticket writes documents and places no responsibility itself. The six-question
test below is answered for the **responsibilities the three records describe**, so
that [[FND-006]] (plan handle `DSK-00-06`) can lift the answers straight into
ADR-0102 (identity and desktop session), ADR-0106 (Graph intake stays central) and
ADR-0109. A "yes" names *where* the responsibility lands, which on this programme is
often an in-house or already-existing host rather than a new Azure resource.

### Responsibility A — issuing and validating the staff session token

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **Yes** | One identity store holds every operator's roles and `IsEnabled`; ten workstations must see the same answer. `src/Pegasus.Web/Program.cs:353`, `src/Pegasus.Core/Identity/StaffAuthorization.cs`. Lands in the evolved gateway `Pegasus.Web` (L-01), not a new unit. |
| Unattended execution | **No** | The token endpoint answers a client call; nothing issues tokens with every desktop closed. |
| Protected credentials | **Yes** | Password hashes, the OpenIddict signing material and the Data Protection ring (`Program.cs:172-176`) must never sit on a workstation. Lands where they already are: Azure SQL plus the `authentication-ring` blob container, both behind the gateway. |
| Public callback | **No** | The desktop calls out over the password and refresh-token grants. The auth-code + PKCE callback path exists only for external MCP connectors (ADR-0027), a different client. |
| Central enforcement | **Yes** | Revocation on disable and on password change, the sign-in rate limiter (`Program.cs:275-277`, `:797-817`) and the security-event audit are all independent of the client. Lands in the gateway. |
| Measured operational advantage | **No measured evidence** | None was collected, and none is needed: the three "yes" answers above already place it. Recording "no" here would be dishonest only if it were used to argue for a desktop-side token issuer, and it is not. |

**Placement:** the evolved gateway `src/Pegasus.Web` (L-01). No new deployment unit,
no Azure write.

### Responsibility B — the database of record and the migration stream

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **Yes** | One `pegasus` database is the record for every operator (`infra/modules/platform.bicep:214`). |
| Unattended execution | **Yes** | The Worker writes intake receipts and retained mail with every desktop closed (`src/Pegasus.Worker/IntakeFunctions.cs:33`). |
| Protected credentials | **Yes** | SQL is Entra-only with managed identity and runtime roles (`platform.bicep:195`, `20260729176000_AzureSqlRuntimeLeastPrivilege.cs`); a connection string on a workstation is exactly what ADR-0103 forbids. |
| Public callback | **No** | Nothing external calls the database. |
| Central enforcement | **Yes** | Runtime-role grants, concurrency tokens (`ExpectedVersion`) and the migration census are enforced server-side. |
| Measured operational advantage | **No measured evidence** | Not needed; four "yes" answers place it. |

**Placement:** Azure SQL behind the gateway; the desktop never connects (ADR-0103).
Register position: *Retain*.

### Responsibility C — the Microsoft Graph intake poll

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **Yes** | The approved-mailbox estate, its per-mailbox lease and cursor are shared state (`src/Pegasus.Core/Intake/RetainedMail.cs`, ADR-0022/ADR-0024). |
| Unattended execution | **Yes** | Proposal §12.1's defining requirement: intake continues with every desktop closed. An always-on host satisfies this; today that host is the existing Function App `pegasus-prod-worker-252ow37gij` (`platform.bicep:489`), already *Retain* in the register. This is not a request for a new Azure resource. |
| Protected credentials | **Yes** | Graph access is the Worker's managed identity with Exchange Application RBAC; it cannot be handed to ten workstations. |
| Public callback | **No** | The Worker polls on a timer (`MailboxFunctions.cs:15`); there is no Graph subscription webhook in the estate. |
| Central enforcement | **Yes** | One poller holds the cursor and the lease per mailbox; ten desktops polling would double-process and break the retained read model's write-once rule. |
| Measured operational advantage | **No measured evidence** | Not needed. |

**Placement:** the existing Worker, unchanged (ADR-0106). The desktop gets read-only
status through the gateway, and no Graph credential ships in the package.

## Implications

1. **`Q1.2` is a decision, not a lookup, and this ticket must not invent its value.**
   The code proves only what happens today (F-4: every request). The refresh interval
   for a token client is an area 04 decision; state the options and the cost of each
   (a longer interval means a disabled account keeps working for that long) and hand
   it over rather than choosing.
2. **`Q1.1` narrowed to a grant question** (F-7). The OpenIddict tables and the
   runtime roles both exist; what has to be proved is `GRANT` coverage on the token
   tables for rotation, and the class of defect is upstream `PLAT-035` — carried on
   this board by [[PLAT-018]] (plan handle `DSK-10-18`).
3. **`Q1.3` is very likely a `docs/open-decisions.md` line, not a citation.** The
   middleware at `Program.cs:875-880` redirects a browser; it says nothing about what
   a token client should receive. The ticket's step 5 already instructs: add one line
   to `docs/open-decisions.md` and stop. Do not invent a problem type or a claim name.
4. **`Q1.4` has a hard boundary.** F-6 shows the offline handler is Development-only
   and lives entirely in `Program.cs`. The answer must be a local mechanism; L-02 and
   ADR-0014 make "add an Azure test environment" out of bounds.
5. **Two ticket verification commands are broken as written** (F-2, F-3). Run the
   corrected commands, state the real numbers, and leave the ticket wording to
   [[FND-052]].
6. **The Azure interaction is a single read.** The only permitted Azure call is the
   read-only Azure MCP `functionapp` show of `pegasus-prod-worker-252ow37gij` for
   app-setting **names** (`AzureWebJobs.*.Disabled`). Never read a secret value; never
   call a `create|update|delete` tool. The expected nine names are already known from
   `platform.bicep:531-539`, so the show is a confirmation, not a discovery.
7. **A Worker function reported disabled is the designed state.** The activation gate
   is fail-closed (`platform.bicep:36`); do not record it as a fault.
8. **Nothing in these answers may be built here.** The scope boundary is read-only
   over `src/`, `tests/`, `scripts/`, `infra/`; the only editable files are
   `docs/desktop/01-inventory-and-parity/flow-records.md` and
   `docs/open-decisions.md`.

## Resolved question record

Each block names the exact command and the question its output must answer. Each has
a matching unticked box in `open-questions`.

### Resolved question record — U-1 · `Q1.1` OpenIddict token-table grants

```
git grep -n "OpenIddict" src/Pegasus.Infrastructure/Persistence/Migrations
git grep -n "GRANT" src/Pegasus.Infrastructure/Persistence/Migrations/*OpenIddict*.cs
pwsh ./scripts/Test-MigrationGrants.ps1
```

Must answer: list every OpenIddict table the EF store creates; for each, state
whether `pegasus_web_runtime_role` holds the `SELECT`/`INSERT`/`UPDATE`/`DELETE`
needed for refresh-token rotation, with the migration `path:line` that grants it; end
with a yes/no.

### Resolved question record — U-2 · `Q1.2` token claim set and `IsEnabled` re-check interval

```
sed -n '1,80p' src/Pegasus.Core/Actors/StaffActorFactory.cs
git grep -n "enum StaffRole" -A 20 src/Pegasus.Core/Identity/
sed -n '353p;368,457p' src/Pegasus.Web/Program.cs
```

Must answer: the exact claim set (claim type and value spelling) that satisfies
`TryCreate`, and a **proposed** re-check interval offered to area 04 as a decision,
with the operator-visible cost of each option stated. Do not choose a value here.

### Resolved question record — U-3 · `Q1.3` `MustChangePassword` for a token client

```
sed -n '875,899p' src/Pegasus.Web/Program.cs
```

Must answer: whether the code settles problem-type versus claim. Expected outcome:
it does not, so add one named line to `docs/open-decisions.md` and stop.

### Resolved question record — U-4 · `Q1.4` DevelopmentOffline token equivalent

```
git grep -n "DevelopmentOfflineAuthenticationHandler" src/Pegasus.Web
sed -n '104,140p' docs/runbook.md      # § Offline development profile
```

Must answer: whether the local Test/UAT stack needs a token equivalent, and what the
local mechanism is. Constraint L-02: an Azure test resource is not an answer.

### Resolved question record — U-5 · official-documentation citations (step 7)

```
microsoft_docs_search "OpenIddict refresh token rotation ASP.NET Core"
microsoft_docs_search "Windows.Security.Credentials.PasswordVault"
microsoft_docs_search "ProtectedData DataProtectionScope CurrentUser"
microsoft_docs_fetch <the page each search returns>
```

Must answer: the rotation semantics and the client-side storage limits, each recorded
as a Microsoft Learn URL **plus the fetch date**. `AGENTS.md` and this programme both
forbid answering an API question from memory.

### Resolved question record — U-6 · `Q2.1` new tables in Phases 0–4

```
git grep -rn "DbSet<" src/Pegasus.Infrastructure/Persistence/PegasusDbContext.cs
```

Must answer: the list of desktop-held state (proposal §11.1) with, for each item,
where it lives instead of a new table; then the yes/no.

### Resolved question record — U-7 · `Q2.2` desktop OpenIddict client seeding and grants

```
git grep -n "class AutomationMcp" -A 60 src/Pegasus.Web/Mcp/AutomationMcp.cs
sed -n '1,60p' scripts/Test-MigrationGrants.ps1
```

Must answer: how the Automation client is seeded today (migration data seed versus
bootstrap command), which of the two the desktop client should use, and which runtime
role needs which grant on the token tables.

### Resolved question record — U-8 · `Q2.3` upstream `PLAT-035` ordering

```
grep -n "PLAT-035" docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md
```

Must answer: whether upstream `PLAT-035`'s build-time grant check — carried on this
board by [[PLAT-018]] (plan handle `DSK-10-18`) — lands before the first gateway
schema change, and what the ordering constraint is if it does not.

### Resolved question record — U-9 · re-run of record 2's real migration count

```
git ls-files src/Pegasus.Infrastructure/Persistence/Migrations | grep "\.cs$" \
  | grep -v "\.Designer\.cs$" | grep -v "ModelSnapshot" | wc -l
```

Must answer: the actual migration count at the head this ticket runs on, and the
first and last migration ids. `64` at this document's head (F-2); state the value
observed, never the one copied from the plan.

### Resolved question record — U-10 · `Q3.1` per-mailbox "last successful cycle" fields

```
git grep -rn "class .*Snapshot\|LastSuccess\|Cursor\|Lease" src/Pegasus.Core/Operations/
git grep -rn "ApprovedMailbox" src/Pegasus.Core/
```

Must answer: which fields already exist per mailbox and which the gateway's
`~GET /api/v1/integrations/status` would have to add, each with `path:line`.

### Resolved question record — U-11 · `Q3.2` Web runtime-role grants for the new read endpoints

```
git grep -ln "pegasus_web_runtime_role" src/Pegasus.Infrastructure/Persistence/Migrations
git grep -n "RetainedMail\|IntakeSearchDocuments" src/Pegasus.Infrastructure/Persistence/Migrations
```

Must answer: for each table the new read endpoints touch, the migration that grants
the Web role `SELECT`, with `path:line`. Assumption A-01-3 is *not* an answer.

### Resolved question record — U-12 · `Q3.3` ADR-0024 timing

```
sed -n '1,60p' docs/adr/0024-stable-approved-mailbox-identity-and-explicit-baseline.md
grep -n "ADR-0024\|TICK-" docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md
```

Must answer: before the Phase 5 Inbox slice, or design the desktop against the
current key — with the consequence of each stated.

### Resolved question record — U-13 · read-only Azure confirmation of the activation gates

```
Azure MCP functionapp  (show / list-settings only)
  target: pegasus-prod-worker-252ow37gij
```

Must answer: that the nine `AzureWebJobs.<function>.Disabled` **setting names** are
present and match `infra/modules/platform.bicep:531-539`. **Names only.** No value is
changed, no secret value is read, and no other Azure tool is called. A function
reported disabled is the designed fail-closed state, not a fault.

### Resolved question record — U-14 · records 1–3 written back and closed

```
pwsh ./scripts/Test-DocumentationLinks.ps1
pwsh ./scripts/Test-MarkdownPlacement.ps1
```

Must answer: that every `Q1.x`/`Q2.x`/`Q3.x` heading in
`docs/desktop/01-inventory-and-parity/flow-records.md` now reads
`Answered <date>: …` or `Moved to docs/open-decisions.md <date>`, that both scripts
exit 0, and that Phase 0 exit-gate item 3 is therefore satisfied.

## Open questions

The eleven flow-record questions (`Q1.1`–`Q1.4`, `Q2.1`–`Q2.3`, `Q3.1`–`Q3.3`) are
this spike's subject, not a defect in it; they are tracked as U-1…U-14 above and as
boxes in `open-questions`.

Two items are genuinely open beyond that scope and are **parked** in
`open-questions` rather than blocking:

- The ticket's Verification commands for the migration count and the Worker function
  list do not run as written (F-2, F-3). Owner: [[FND-052]] (board grooming —
  unrunnable verification commands). This document records the corrected commands and
  the real numbers, so the ticket is executable as it stands.
- Whether `docs/desktop/01-inventory-and-parity/flow-records.md` record 2's own
  "Read-only verification" block should be corrected in the same edit as the answers.
  Default taken: yes, because step 11 of the ticket already authorises "a short
  'What the desktop needs' correction to each record where the code disagreed", and a
  command that returns the wrong number is a disagreement.

Nothing here re-opens a settled operator decision. D-004 (`OPS-10` folds into the
desktop pilot approval) and the Send-to-AI recorded exclusion are not touched by these
three records.


## Closure evidence — 2026-08-25

The three flow records were re-checked on branch `fnd-019-flow-records`, based on
`origin/dev` at `5770eb21c0d03620a6a6d99e0431bde91ec2ad6a`. The repository documents
were updated only in the ticket-owned files `docs/desktop/01-inventory-and-parity/flow-records.md`
and `docs/open-decisions.md`.

### Q1.1–Q1.4

- OpenIddict's EF store owns `OpenIddictApplications`,
  `OpenIddictAuthorizations`, `OpenIddictScopes`, and `OpenIddictTokens`.
  The Web runtime role has SELECT/INSERT/UPDATE on the application,
  authorization, and token tables and SELECT on scopes, while DELETE is
  explicitly denied by `20260803151159_AutomationActorOpenIddict.cs:195-207).
  This is sufficient for the current refresh-token create/read/update path;
  runtime deletion is intentionally not part of the current policy.
- `StaffActorFactory.TryCreate` requires a non-empty Guid subject and one or
  more exact-case `StaffRole` values. The transport mapping is `sub` to
  subject and one `role` claim per exact enum name; the bearer re-check
  interval is moved to the named area-04 decision in `docs/open-decisions.md`.
- `MustChangePassword` is settled only for the browser: the current middleware
  redirects to `/Account/PasswordChange`. Token-client problem-type versus claim
  remains a named decision and is not invented here.
- DevelopmentOffline remains a Development-only local authentication handler
  over the deterministic local Identity fixture. No Azure test resource or
  second local token issuer is required by this spike.

### Q2.1–Q2.3

- Proposal §11.1's local state is access-token memory, Windows credential storage
  for the refresh/session token, preferences, small reference snapshots,
  thumbnails, temporary working copies, optional encrypted drafts, a short
  compatibility cache, and redacted diagnostic logs. No new database table is
  required in Phases 0–4; a future minimum-client-version table remains an
  area-04 decision.
- The Automation client is idempotently created/reconciled by
  `AutomationClientRegistry.EnsureRegisteredAsync` through
  `IOpenIddictApplicationManager` at the token/authorize boundary; it is not
  migration data seed. A future desktop client must use that gateway bootstrap
  pattern, with only the Web role granted OpenIddict table access.
- PLAT-035's board carry-over is [[PLAT-018]], currently `preparing` and
  blocked. Its grant-coverage gate must land before GWY-002/DSK-03-02's first
  gateway schema change; local full-privilege success does not clear that
  dependency.

### Q3.1–Q3.3

- Per-mailbox state already has mailbox identity/address, cursor, due time,
  lease token/expiry, last completion, and last failure. Core's
  `MailPollHealth` and the read-only EF query expose the completion, failure,
  and due fields; the gateway needs a projection/count over existing rows,
  not new state columns.
- Web SELECT grants already cover `RetainedMailboxMessages`,
  `RetainedMailboxAttachments`, `IntakeSearchDocuments`, and the mailbox
  status tables in the runtime-role reconciliation tuples. Worker write grants
  remain separate.
- Accepted ADR-0024 requires stable `ApprovedMailbox.Id` as the Pegasus
  source identity and a per-mailbox fresh start before the Phase-5 Inbox slice.
  Designing against the current Graph-coordinate key would preserve obsolete
  cursor adoption and risk replay/duplicate receipts.

### Verification and external evidence

- `pwsh ./scripts/Test-MigrationGrants.ps1` → exit 0:
  `Test-MigrationGrants: 64 migration files checked, every created table is granted or exempted.`
- Corrected migration census at this branch → 64 non-designer, non-snapshot
  migrations; first `20260724104624_InitialProviderNeutralIntake`; last
  `20260822044425_GrantWorkerCaseDocuments`.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` and the repository's markdown
  placement check are required ticket validation; their exact final outcomes
  are recorded after the document patch verification step.
- Azure MCP read-only `functionapp_get` succeeded for
  `pegasus-prod-worker-252ow37gij` in `rg-pegasus-prod`: Running, UK South,
  plan `pegasus-prod-worker-plan-252ow37gij`, release `0.1.0-alpha.1`.
  A read-only setting-name query returned the nine
  `AzureWebJobs.<function>.Disabled` names plus ordinary non-secret settings;
  no setting values were read and no Azure write was performed. The nine names
  match `infra/modules/platform.bicep:531-539`.
- Microsoft Learn pages were searched and fetched on 2026-08-25 for access-token
  expiry/rotation, OIDC `offline_access`, Credential Locker, and DPAPI; their
  official URLs are recorded in `docs/open-decisions.md`. These platform
  references do not choose Pegasus's unresolved token problem or validation interval.
- Simplification pass: n/a — docs-only spike; no product code or repository
  architecture was changed.
