# Research — FND-006: the four flow-derived ADRs (0102, 0106, 0107, 0109)

## Question

What does the repository actually do today for staff authentication, Microsoft
Graph intake, provider credentials (Box, DVLA/DVSA) and telemetry — and is that
evidence complete enough for four ADRs to state those boundaries as settled
without inventing a requirement?

## Current behaviour

Measured on 2026-08-24 from the working tree at `origin/main` `191ddf3342…`,
and from
`docs/desktop/01-inventory-and-parity/flow-records.md` (433 lines, pre-filled
from the 2026-08-23 code inspection and completed by [[FND-019]] (plan handle
`DSK-01-06`) and [[FND-020]] (plan handle `DSK-01-07`)).

- **Authentication (ADR-0102's subject)** — flow record 1 at `:21-105`. ASP.NET
  Identity with `AddIdentity<PegasusIdentityUser, IdentityRole<Guid>>`
  (`src/Pegasus.Web/Program.cs:262-274`); sign-in through
  `CheckPasswordSignInAsync(..., lockoutOnFailure: false)`
  (`Pages/Account/SignIn.cshtml.cs:63`) — ADR-0013 clause 12 chose throttling
  over lockout; rate limiting policies `StaffSignIn` and `AutomationMcp` plus a
  singleton global limiter (`Program.cs:275-327`, `:797-817`); cookie
  `__Host-Pegasus` with `OnValidatePrincipal` re-checking `user.IsEnabled` on
  **every** request (`SecurityStampValidatorOptions.ValidationInterval =
  TimeSpan.Zero`, `Program.cs:353`, `:368-457`); a 12-value fail-closed rights
  matrix at `src/Pegasus.Core/Identity/StaffAuthorization.cs`; OpenIddict 7.6
  with **one seeded Automation client only** (`Mcp/AutomationMcp.cs`,
  `Mcp/AutomationMcpExtensions.cs:134`).
- **Graph intake (ADR-0106)** — flow record 3 at `:170-241`. Timer functions
  `InboxPollFunction` (`src/Pegasus.Worker/MailboxFunctions.cs:15`, schedule
  `%ApprovedInboxPollSchedule%`) and `SentEvidencePollFunction`
  (`EmailEvidenceFunctions.cs:16`); queue functions in `IntakeFunctions.cs:13,33,50,75`;
  adapter `src/Pegasus.Infrastructure/Email/GraphApprovedSources.cs` (1,125
  lines); Azure Storage queues declared at `infra/modules/platform.bicep:129-152`.
- **Provider credentials (ADR-0107)** — flow records 4 and 5 at `:242-361`. Box:
  `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` (1,016 lines) with
  `Box__ConfigJson` / `Box__ClientSecret` supplied as Key Vault references on the
  Worker and Container App secrets on the Web (`platform.bicep:382-398`,
  `:555-563`); an unresolved `@Microsoft.KeyVault(` placeholder fails with a
  named error (`BoxCaseCustody.cs:82-84`). DVLA/DVSA:
  `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs` (412 lines)
  with `Dvla__ApiKey` and `Dvsa__ClientId/ClientSecret/ApiKey` as Key Vault
  references on the Worker (`platform.bicep:555-563`); the Worker owns the live
  adapter and the Web records the request.
- **Telemetry (ADR-0109)** — `builder.Services.AddApplicationInsightsTelemetry()`
  at `src/Pegasus.Web/Program.cs:196` (package
  `Microsoft.ApplicationInsights.AspNetCore` 2.23.0,
  `Pegasus.Web.csproj:38`) and
  `.AddApplicationInsightsTelemetryWorkerService().ConfigureFunctionsApplicationInsights()`
  at `src/Pegasus.Worker/Program.cs:14-15`. The backing Log Analytics workspace
  is capped at 0.1 GB/day
  (`docs/desktop/01-inventory-and-parity/azure-resource-register.md:36`), a
  limitation tracked as **upstream PLAT-034** and recorded as open at
  `docs/current-architecture.md:175`.

**No parity-matrix row covers this ticket, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds **46** rows
(`grep -c '^| PAR-'` → 46), each keyed to a Razor page model under
`src/Pegasus.Web/Pages/` with its handlers (`parity-matrix.md:36-38`). Writing
an ADR is a documentation mechanism, not an operator-visible surface. The
closest existing repository mechanism — the thing this ticket must not break —
is the ADR index (`docs/adr/README.md:16-41`) plus the CI `documentation` job
(`.github/workflows/ci.yml:71-87`), which runs
`scripts/Test-TestMarkdownPlacement.ps1` and `scripts/Test-DocumentationLinks.ps1`
on every change set.

## Findings

- **The flow records are pre-filled, not empty.** `flow-records.md:1-8` states
  they were pre-filled from the 2026-08-23 inspection at `191ddf33` and that
  `DSK-01-06` and `DSK-01-07` complete them, answer the open questions and
  attach the read-only command outputs. So this ticket is not blocked on
  discovery; it is blocked on *closure*.
- **Thirteen flow-record open questions bear on these four ADRs.** Counted with
  `grep -n '^- Q[0-9]' docs/desktop/01-inventory-and-parity/flow-records.md`:
  Q1.1–Q1.4 (record 1, `:90-99`) for ADR-0102; Q3.1–Q3.3 (`:227-234`) for
  ADR-0106; Q4.1–Q4.3 (`:296-303`) and Q5.1–Q5.3 (`:350-354`) for ADR-0107. The
  record's own closure rule is at `:7-8`: "A record is closed when every open
  question has a code citation or a line in `docs/open-decisions.md`"
  (that file exists).
- **ADR-0109 has no flow record of its own.** Records 1–6 are authentication,
  database/migrations, Graph intake, Box custody, DVLA/DVSA and report
  rendering; telemetry is not among them. Its evidence therefore comes from the
  two `Program.cs` registrations above, the Azure resource register, and
  upstream PLAT-034 — not from `flow-records.md`.
- **`PLAT-034` is an upstream id, not a fork board id.** The board's
  `platform-operations` area tops out at `PLAT-029` (`ls` on the board:
  PLAT-025…PLAT-029); there is no board `PLAT-034`. Per the Kanmer group
  document `HZN-001/board-conventions.md` § *Upstream ids versus board ids*, a
  bare `<PREFIX>-<nnn>` on this board is a **fork board id**, so this one must be
  written **`upstream PLAT-034`** everywhere — in the ADR, in `## Links`, and in
  the plan. Writing it bare points ADR-0109 at a live conversion ticket about
  performance-regression reporting.
- **The related decisions all exist and are accepted.** ADR-0004 (provider API
  and staff MCP authentication), ADR-0011 (restrict MCP to a vendor-neutral
  Automation Actor), ADR-0024 (stable approved-mailbox identity), ADR-0027
  (authorization code with PKCE for external MCP connectors) are all in
  `docs/adr/README.md`'s accepted table.
- **ADR-0102 is co-claimed, and four more tickets extend the same file.**
  [[FND-042]] (plan handle `DSK-04-01`) also authors ADR-0102, and [[GWY-019]]
  (`DSK-04-02`), [[GWY-020]] (`DSK-04-03`), [[GWY-021]] (`DSK-04-04`) and
  [[GWY-022]] (`DSK-04-05`) each name the same file as the one they extend. The
  one agreed path is `docs/adr/0102-existing-pegasus-credentials-token-session.md`
  — the only ADR-0102 path the plan set itself names, at
  `docs/desktop/04-auth-session-update-and-startup/README.md:296`.
- **The `AGENTS.md` index-shape sentence is wrong and is not this ticket's to
  fix.** `AGENTS.md:114-117` describes `ID | Title | Status | Superseded-by |
  Owner capability`; `docs/adr/README.md:18-19` is `ADR | Title | Related FRD`.
  The file wins; [[FND-005]] (plan handle `DSK-00-05`) owns the correction. This
  ticket's own body draws its `AGENTS.md` citation range to `:113` deliberately
  for that reason.
- **House form: `## Status` first, and lowercase `related_frd` stems.**
  `docs/adr/0028-*.md:13` and `0029-*.md:13` open at `## Status`; the older
  0014/0015/0025 do not. Every `related_frd:` value in `docs/adr/*.md` is a
  lowercase stem (`[frd-08]`, `[frd-10, frd-11]`) — the display form `[FRD-08]`
  appears nowhere.
- **None of these four files exists.** `ls docs/adr/010*` returns nothing; the
  highest ADR in the tree is 0029.

### Facts

| Fact | Source |
| --- | --- |
| Identity registration and password options | `src/Pegasus.Web/Program.cs:262-274` |
| Sign-in uses throttling, not lockout | `Pages/Account/SignIn.cshtml.cs:63`; ADR-0013 clause 12 |
| `IsEnabled` re-checked on every request | `Program.cs:353`, `:368-457` |
| Fail-closed 12-right staff matrix | `src/Pegasus.Core/Identity/StaffAuthorization.cs` |
| OpenIddict 7.6, one seeded Automation client | `Mcp/AutomationMcp.cs`; `Mcp/AutomationMcpExtensions.cs:134` |
| Graph poll is a Worker timer | `src/Pegasus.Worker/MailboxFunctions.cs:15`; `EmailEvidenceFunctions.cs:16` |
| Graph adapter, 1,125 lines | `src/Pegasus.Infrastructure/Email/GraphApprovedSources.cs` |
| Box secrets are Key Vault references / Container App secrets | `infra/modules/platform.bicep:382-398`, `:555-563` |
| Unresolved Key Vault placeholder fails by name | `BoxCaseCustody.cs:82-84` |
| DVLA/DVSA secrets on the Worker; Worker owns the live adapter | `platform.bicep:555-563`; flow record 5 `:320-327` |
| App Insights registered in Web and Worker | `src/Pegasus.Web/Program.cs:196`; `src/Pegasus.Worker/Program.cs:14-15` |
| Log Analytics capped at 0.1 GB/day | `azure-resource-register.md:36`; `docs/current-architecture.md:175` |
| No board `PLAT-034` exists | board `areas/platform-operations/` tops out at `PLAT-029` |
| 13 open questions bear on these four ADRs | `flow-records.md:90-99, 227-234, 296-303, 350-354` |
| Record closure rule | `flow-records.md:7-8` |
| ADR index columns | `docs/adr/README.md:18-19` |
| No 01xx ADR exists yet | `ls docs/adr/010*` |

### Assumptions

- **A-00-7 — [[FND-019]] and [[FND-020]] will close their records' open
  questions with code citations rather than by deferring them all to
  `docs/open-decisions.md`.** *Confirmed by:* reading each record's Open
  questions section after those tickets reach `done` and finding a citation or a
  named `open-decisions.md` line against each. *Breaks if:* a question like Q5.1
  ("does the provider contract allow a direct public/native client call")
  resolves to "unknown", in which case ADR-0107's "public callback" row cannot be
  answered honestly and the ADR must state the boundary conditionally or wait.
- **A-00-8 — no new telemetry service is needed, so ADR-0109 is a
  do-not-add decision.** *Confirmed by:* the two App Insights registrations
  above plus the desktop diagnostics-bundle design in plan 02/plan 10. *Breaks
  if:* the 0.1 GB/day cap (upstream PLAT-034) makes desktop-originated telemetry
  unusable in practice, which would turn ADR-0109 from "no new fleet" into a
  decision about where desktop diagnostics actually land.
- **A-00-9 — the desktop never holds a Graph, Box, DVLA or DVSA credential.**
  *Confirmed by:* the secret inventory above showing every provider secret bound
  to the Worker or the Web container, never to a client. *Breaks if:* Q4.1
  ("can the Box SDK issue short-lived, constrained upload/download tokens")
  resolves negative **and** the alternative chosen is a long-lived token on the
  workstation — which ADR-0107 exists to forbid, so that would be a decision to
  escalate rather than absorb.

## Execution placement

**This ticket places no responsibility anywhere: it authors documents.** The one
placement it assumes is that the four ADR files live in this repository under
`docs/adr/`. The six-question cloud-justification test is answered *inside each
ADR*, per decision, and the whole point of the ticket is that those answers are
evidence-backed. Provisional answers from the pre-filled flow records, to be
re-confirmed against the completed records at execution:

**ADR-0102 — existing Pegasus credentials, desktop session = short-lived access
token + rotated refresh token**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **yes** | Identity tables in Azure SQL are the single account store (flow record 1 § Data owned) |
| Unattended execution | no | Sign-in is interactive by definition |
| Protected credentials | **yes** | OpenIddict signing/encryption material and the Data Protection ring blob `authentication-ring/keys.xml` (`Program.cs:172-176`) must not sit on a workstation |
| Public callback | no | Password + refresh grants; no external service calls back |
| Central enforcement | **yes** | `OnValidatePrincipal` re-checks `IsEnabled` every request (`Program.cs:368-457`); revocation on disable/password change is a gateway responsibility |
| Measured operational advantage | no | Not claimed; the placement follows from the four answers above |

Lands on the gateway (`Pegasus.Web` evolved in place, L-01) — not a new service.

**ADR-0106 — Graph intake worker stays central**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **yes** | Per-mailbox lease, cursor and last-failure code are Core-owned and database-backed (ADR-0022; flow record 3) |
| Unattended execution | **yes** | `InboxPollFunction` is a timer on `%ApprovedInboxPollSchedule%` (`MailboxFunctions.cs:15`); intake must continue with every desktop closed |
| Protected credentials | **yes** | Graph application credentials are Worker-bound |
| Public callback | no | Polling, not webhooks |
| Central enforcement | **yes** | Duplicate suppression and cursor advance are single-writer invariants |
| Measured operational advantage | no | Not claimed |

Lands in the existing Worker. This is the clearest "yes" set in the conversion
and the reason the desktop only *shows* intake status.

**ADR-0107 — Box and DVLA/DVSA credentials stay behind the gateway**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **yes** | Durable lookup request rows are idempotent per case and registration (flow record 5 § Data owned) |
| Unattended execution | **yes** | `ExternalWorkFunction` performs custody and lookup work (`Functions/ExternalWorkFunctions.cs:9`) |
| Protected credentials | **yes** | `Box__ConfigJson`, `Box__ClientSecret`, `Dvla__ApiKey`, `Dvsa__ClientId/ClientSecret/ApiKey` are Key Vault references bound to the Worker (`platform.bicep:382-398`, `:555-563`) — **no long-lived provider secret ever ships in the MSIX** |
| Public callback | no | Outbound calls only |
| Central enforcement | **yes** | Provider rate limits are coordinated once, not per workstation (Q5.3) |
| Measured operational advantage | no | Not claimed |

**ADR-0109 — desktop diagnostics bundle beside the existing App Insights**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | no | A diagnostics bundle is one workstation's own record |
| Unattended execution | no | Produced on demand by the operator |
| Protected credentials | no | Redacted by construction; no secret leaves the desktop |
| Public callback | no | — |
| Central enforcement | **yes**, for the *gateway* half only | Server-side telemetry already exists (`Program.cs:196`, Worker `:14-15`); the decision is to reuse it rather than add a fleet |
| Measured operational advantage | no — and the measurement points the other way | The Log Analytics workspace is capped at 0.1 GB/day (`azure-resource-register.md:36`, upstream PLAT-034), so more central telemetry would be blinded, not better |

That last row is the honest reason ADR-0109 adds nothing central: the existing
capped workspace is evidence *against* centralising desktop diagnostics, not for
it. "It is already in Azure" would not have been an answer.

## Implications

1. **This ticket is gated on evidence, not on effort.** Step 2's gate —
   [[FND-019]] and [[FND-020]] `done`, with every record question carrying a
   code citation or an `docs/open-decisions.md` line — is the difference between
   an ADR that records a boundary and one that invents it. An ADR body is
   immutable once merged (`docs/adr/README.md:12-14`), so a question left open
   cannot be patched afterwards.
2. **ADR-0109 must be sourced differently from the other three.** There is no
   flow record for telemetry; its evidence is the two `Program.cs` lines, the
   Azure register and upstream PLAT-034. Do not wait for a record that does not
   exist, and do not invent one.
3. **Write `upstream PLAT-034`, never `PLAT-034`.** The bare form is a fork board
   id and points at live conversion work.
4. **The negative decisions are the load-bearing part.** Later tickets rely on
   "no long-lived provider secret in the package" (ADR-0107), "intake continues
   with every desktop closed" (ADR-0106), "no Microsoft-account or Entra login
   for staff" (ADR-0102) and "no OpenTelemetry collector fleet" (ADR-0109). State
   them in `## Decision` or `## Consequences`, not in passing.
5. **Follow the file on the index shape and the newest ADRs on the heading set** —
   three cells, `## Status` first, lowercase `related_frd` stems — and leave the
   `AGENTS.md` correction to [[FND-005]].

## Open questions

- **The thirteen flow-record questions** (Q1.1–Q1.4, Q3.1–Q3.3, Q4.1–Q4.3,
  Q5.1–Q5.3). They are owned by [[FND-019]] and [[FND-020]], which is a scope
  boundary rather than an open question for this ticket — but step 2 must
  *verify* they are closed, and this ticket stops if they are not. Q5.1 and Q4.1
  are the two whose answers could change an ADR's content rather than just
  confirm it.
- **Which ticket authors ADR-0102** — this one or [[FND-042]]. Settled by the
  agreed rule: one filename, and whichever is worked first authors it while the
  other verifies and extends in place. No operator question outstanding (unlike
  ADR-0105, which is [[FND-005]]'s and [[REL-001]]'s to reconcile).
- Not open, and not to be reopened: the reserved block ADR-0100…ADR-0110
  (operator, 2026-08-23, `AGENTS.md:84-90`); L-01, L-02, C-01; and the recorded
  Send-to-AI exclusion, which is a recorded exclusion with a reactivation
  condition and not a conflict.
