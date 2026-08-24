# Plan — FND-050: Phase 2 exit review and UAT script

**Diff estimate: ~1 file, ~4 lines.**

The repository diff is deliberately almost nothing. This ticket's output is
**evidence**, not code: a UAT script that lives in this plan document, an
executed run of it, and a `proof` document with one row per exit gate. The only
tracked file it edits is `docs/desktop/README.md` § Status. A large diff here
would mean the review had started fixing things, which its Guardrails forbid.

## Measured file-and-line inventory

Profile `chore` owes no `research` and no `files` document, so this plan carries
the surface-area burden alone —
`.grok/skills/kanmer-plan/assets/plan-template.md`'s "written FROM the ticket's
`research` and `files` documents" precondition does not apply to `chore`. Every
row was measured against the fork working tree on 2026-08-24 with `wc -l`,
`sed -n` and `grep -n`.

| Path | Measured now | Change | Lines |
| --- | --- | --- | --- |
| `docs/desktop/README.md` | **142 lines.** `## Status` at `:138`; the table header at `:140-141`; exactly **one** row at `:142` — `\| 00–12 \| Drafted 2026-08-23 \| Awaiting first ticket creation on the fork's Kanmer board (see 00) \|` | **Edit.** Split the single row so area 04 carries its own state — "Phase 2 exit gate evidenced `<date>`" — leaving the remaining areas' row intact. | +4 |
| *(this plan document)* | — | The four-scenario UAT script, written under § UAT script below. Ticket-transient documents live in Kanmer, **not** in the tree (`AGENTS.md` § New Markdown placement; body § Documentation changes). | 0 |
| *(this ticket's `proof`)* | — | One row per gate from `docs/desktop/04-auth-session-update-and-startup/README.md:214-224` § 4, each with a verdict and a named artefact. Written at `enter-done`. | 0 |

**Sum: 1 file, ~4 lines.**

### Measured and deliberately not touched

| Path | Measured now | Why not |
| --- | --- | --- |
| `docs/operations.md` | The production-environment record | Edited **only** if the minimum-version setting was applied **outside** the local stack — which under L-02 it will not be. Otherwise [[REL-016]]'s (plan handle `DSK-09-18`) desktop release table records it. Body § Documentation changes. |
| `src/`, `tests/` | — | **Read-only** (Guardrails). This ticket produces evidence; every finding becomes a new `fix` ticket in the owning area. |
| `docs/desktop/04-auth-session-update-and-startup/README.md:214-224` § 4 | The six gate rows | The gate table is the standard being measured against; narrowing it to fit what shipped is the failure the body's *Concern to record* names. |

## Approach

**Measure the phase against the table that already exists, and treat a missing
artefact as a failed gate rather than a judgement call.** The six rows of
`docs/desktop/04-auth-session-update-and-startup/README.md:214-224` § 4 are the
standard; each is closed by naming an artefact and where it lives, and a row
with no artefact is recorded `FAILED` with a follow-up ticket. The UAT script's
four scenarios are the human half of that evidence and live in this plan
document; the automated half is re-run, not quoted from the tickets that wrote
it.

The alternative rejected is **assembling the proof from the fourteen dependency
tickets' own proofs** — reading [[GWY-019]] through [[FND-049]]'s evidence and
citing it. It is faster and it does not close the gate the phase exists for:
each of those tickets proved its own slice against its own fixtures, and the
risk Phase 2 guards against is precisely that the slices do not compose — a
token that works in an integration test and not through an installed package,
an update that blocks in a packaging test and not against the real gateway
gate. `docs/desktop/00-governance-and-workflow/README.md:293` names area 04 as
the Phase 2 exit-gate owner for that reason. Step 2 still *reads* those
tickets, but to confirm they are `done`, not to inherit their conclusions.

The second alternative rejected is **running the review against production**.
ADR-0014 gives Pegasus two environments and no third; L-02 puts Test/UAT on the
local stack. Raising the minimum client version against the local gateway is an
authenticated administrative action; against production it is a change to a
live system needing exact-target approval under `docs/runbook.md` § Live
operation approval matrix — a different ticket, as the Guardrails say.

## Governing docs

### Linked `refs`

| Ref | Requirement | Meets |
| --- | --- | --- |
| `docs/frd/frd-12-operator-experience.md:20-22` | The operator-visible state vocabulary, with exact state labels mapped to Core decisions | **Meets** — scenarios A–D exercise the operator-visible states of the session-failure matrix (`04/README.md:224-231`) and record the label the operator actually read, not the one the code intended. |
| `docs/frd/frd-12-operator-experience.md:112` | "The UI never infers state from colour alone" | **Meets** — the screenshots captured in scenarios B, C and D are the tier-7 record of that; a state legible only by colour is a finding. |

No FRD text is modified.

### `docs_todo: true`

`get_doc_gates FND-050` reports `docs_todo: true`.

> **New ADR** — ADR-0102 (existing Pegasus credentials with a token session)
> and **ADR-0105** (signed MSIX / App Installer distribution with a gateway
> minimum-version gate). ADR-0102 is authored by [[FND-042]] (plan handle
> `DSK-04-01`); [[FND-005]] (plan handle `DSK-00-05`) and [[FND-006]] (plan
> handle `DSK-00-06`) also claim the 0100-block ADRs, so see [[FND-042]]'s plan
> for the ownership reconciliation. ADR-0105 is authored by [[REL-001]] (plan
> handle `DSK-09-01`); [[FND-005]] and [[FND-042]] also claim it — see
> [[REL-001]]'s plan for the ownership reconciliation.
> This review is written to the decisions as recorded in
> `docs/desktop/04-auth-session-update-and-startup/README.md:139-213` § 3 and
> `docs/desktop/README.md` § Locked decisions; if either ADR lands differently
> this plan is revised before the review runs.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 24 Phase 2 exit gate | The five conditions before any feature slice ships | The whole review; gate rows 1–5 |
| Proposal § 27 items 3, 4, 13 (`00/README.md:304-322`) | 3 "Existing Pegasus credentials and permissions work; no Microsoft login"; 4 "Unsupported versions cannot proceed"; 13 "Install, mandatory update and rollback proven" | Steps 5–7; step 12 records which programme-exit items this review advances and which it does not |
| `docs/desktop/00-governance-and-workflow/README.md:293` § Phase map | Phase 2's exit-gate owner is area 04 | This ticket is that gate |
| `docs/desktop/04-auth-session-update-and-startup/README.md:214-224` § 4 | The six gate rows and the evidence each demands | Steps 5–10, one step per row |
| `docs/desktop/04-auth-session-update-and-startup/README.md:259-293` § 7 | The traps, in particular the fail-open feed and the side-loaded-MSIX trap | Scenario B's design; the Risks section |
| `docs/desktop/08-testing/test-uat-stack.md:130` scenario 12 | "Obsolete desktop version blocked and updates successfully"; evidence "Update-required screen, `Get-AppxPackage` version after update" | Scenario B |
| `docs/desktop/08-testing/test-uat-stack.md:147-162` § Evidence capture | `winapp ui screenshot` per state; TRX under `artifacts/test-results/`; `Get-AppxPackage` transcripts before/after; **"Evidence is filed in the Kanmer ticket (`proof`, `reference/`), never in the repository tree"** | Steps 4–13 and the Verification table |
| **L-02** (`docs/desktop/README.md` § Locked decisions) | Test/UAT is the local production-mimicking stack; ADR-0014 stands | Step 4; the Guardrails restated in Risks |
| **D-002 / D-003** | Sign in-house → copy to the share → App Installer over SMB; no Azure resource in the install path | Scenario B installs from the `teststack` feed [[FND-048]] (plan handle `DSK-04-12`) built |
| `docs/engineering.md:76-79` tiers 5, 7, 11 | All three owed; **none substitutes for another** | Verification — the table is grouped by tier so a missing tier is visible |
| `docs/engineering.md:201-203` § Plan sizing | A plan states its diff estimate first, from a real number | The first line and the inventory above |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Step 11 and Routing; `.codex/agents/pegasus-desktop-reviewer.toml` is `sandbox_mode = "read-only"` and its own instructions open "You must not have implemented the change you review; if you did, say so and stop" |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass under a dated heading; `n/a — docs-only` for a documentation-only branch | Step 13 and the heading below |

## Routing

Copied from the ticket body's `## Routing` block; required in the plan document
by `docs/desktop/00-governance-and-workflow/README.md` § Ticket template.

- **Subagent**: `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml` (confirmed present 2026-08-24;
  `sandbox_mode = "read-only"`, so it **cannot** write repository files — the
  one tracked edit in the inventory is made outside that sandbox). It must not
  have implemented any ticket under review.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md`, `microsoft/win-dev-skills`
  v0.5.0 `f1028dd5`) → `kanmer-verify` (`.grok/skills/kanmer-verify/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `get_item`,
  `get_ticket_doc`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search`) **only** to check a
  platform claim under dispute
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates FND-050` before every move; a move crosses at most one gated
  boundary). `chore` owes `plan` at `leave-preparing` and `proof` at
  `enter-done`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's thirteen implementation steps in the same order,
with the same ownership and the same file paths.

1. **Orient and take.** Read
   `docs/desktop/04-auth-session-update-and-startup/README.md:214-224` § 4 (the
   six gate rows) and `:259-293` § 7 (the traps) in full, then
   `docs/desktop/08-testing/test-uat-stack.md:112-134` § UAT scripts and
   `:147-162` § Evidence capture. Call `get_doc_gates FND-050`, then
   `take_ticket`.
2. **Confirm the fourteen dependencies are `done`, not merely merged.** Call
   `get_item` and `get_ticket_doc` on each: [[FND-042]] (`DSK-04-01`),
   [[GWY-019]] (`DSK-04-02`), [[GWY-020]] (`DSK-04-03`), [[GWY-021]]
   (`DSK-04-04`), [[GWY-022]] (`DSK-04-05`), [[GWY-023]] (`DSK-04-06`),
   [[FND-043]] (`DSK-04-07`), [[FND-044]] (`DSK-04-08`), [[FND-045]]
   (`DSK-04-09`), [[FND-046]] (`DSK-04-10`), [[FND-047]] (`DSK-04-11`),
   [[FND-048]] (`DSK-04-12`), [[FND-049]] (`DSK-04-13`), [[GWY-024]]
   (`DSK-04-14`). List any not in `done` with its missing evidence. **If any is
   incomplete, record the blockers and stop** — the body says so, and the
   *Concern to record* adds that the gate table must not be narrowed to fit
   what shipped.
3. **Write the UAT script.** It is written below, under § UAT script, in this
   plan document — **not** in the tree (`AGENTS.md` § New Markdown placement;
   body § Documentation changes). Four scenarios, each with steps, expected
   result and the evidence to capture.
4. **Bring up the stack.**
   `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start`, then
   `-Action Status`. Both values are real —
   `scripts/Invoke-LocalDevelopment.ps1:3` declares
   `[ValidateSet('Start', 'Status', 'Smoke', 'Stop', 'Reset')]`. Record the run
   id and the printed URLs as the **first** evidence item; every later artefact
   is correlated to it. Note that `-Action Status` refuses a `-RunId`
   (`:1490-1492` throws "Status enumerates all owned runs and does not accept
   -RunId"), so call it bare.
5. **Operator step — gate row "Current user credentials work".** Install the
   signed package from the `teststack` feed [[FND-048]] built, on a Windows 11
   workstation, and sign in with an existing Identity account. **Install from
   the feed, not by side-loading the `.msix`** — a side-loaded package makes
   `CheckUpdateAvailabilityAsync` return `Unknown`
   (`04/README.md:271-274`), and scenario B would then prove nothing. Evidence:
   a screenshot of the shell after login, and the rolling-log line carrying the
   startup correlation id.
6. **Gate row "Microsoft login is not required" — prove it twice.** Run
   `dotnet test tests/Pegasus.ArchitectureTests` and confirm the fact that
   forbids MSAL/Entra packages in the desktop projects is green — that fact is
   added to `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` by
   [[FND-037]] (plan handle `DSK-02-12`), so if it is absent the gate is
   unproved and step 2's stop condition applies. Then confirm from the UI
   script that **no browser window is launched anywhere in the login path**.
   Record both; neither alone closes the row.
7. **Operator step — gate row "Obsolete package is blocked and updates".**
   Raise the minimum client version through the administrator setting from
   [[GWY-023]], relaunch the installed old client, confirm the update-required
   screen (`screen-specs.md:99-106`: title "Update required", primary "Update
   now", secondary "Sign out"), take the update from the feed, and confirm the
   app reaches the shell. **Prove the block through the gateway gate, not
   through the feed**: App Installer fails open when the feed is unreachable
   (`04/README.md:279-282`), so a missing feed is not evidence of a block.
   Evidence: `Get-AppxPackage CollisionEngineers.Pegasus` version before and
   after, screenshots of both states, and the gateway audit entry for the
   setting change.
8. **Gate row "Disabled account is rejected".** Disable a test staff account
   while a desktop session is live, make the next `/api/v1` call, and confirm
   it is refused **within one request** — not within one access-token lifetime.
   That guarantee is not aspirational: `src/Pegasus.Web/Program.cs:353` sets
   `options.ValidationInterval = TimeSpan.Zero` on
   `SecurityStampValidatorOptions`, which is what makes `IsEnabled` re-checked
   every request for the cookie path, and `04/README.md:167-176` decision 3
   extends the same guarantee to `/api/v1`. Also confirm a refresh is refused
   with `invalid_grant`. Evidence: the integration-test output from [[GWY-022]]
   plus a live screenshot of the desktop's disabled-account state.
9. **Gate row "Tokens/secrets pass storage review".** Walk § 4's checklist: the
   access token is never written to disk; the refresh handle exists only in the
   DPAPI store; the MSIX contains no secret. Run the package secret scan from
   [[PLAT-003]] (plan handle `DSK-10-03`) **if it exists** — it is a phase-8
   ticket, so it probably does not — otherwise extract the package and inspect
   it and the embedded `appsettings.<channel>.json` by hand, and **record what
   was checked** rather than that a check happened. Note the standing trap:
   `04/README.md:288-291` records a plaintext `Bootstrap:VerificationAccount`
   in `src/Pegasus.Web/appsettings.json` that "must never be the desktop test
   login in production" — confirm the review's test account is not it.
10. **Gate row "Startup sequence observable".** Export a diagnostics bundle
    from the running app ([[FND-036]], plan handle `DSK-02-11`) and confirm it
    contains the ordered startup steps under **one** correlation id and **no**
    token literal. Attach the redacted excerpt.
    `docs/desktop/09-release-update-and-distribution/runbooks.md:333-352` § R10
    lists what the bundle is expected to contain; check against that list.
11. **Independent review.** Run `winui-code-review` over the Phase 2 desktop
    diff. The agent doing it must have implemented **none** of the tickets
    under review — `.codex/agents/pegasus-desktop-reviewer.toml`'s own
    instructions open with that rule and tell it to stop if it did, and
    `AGENTS.md` § Repository task workflow step 5 requires it. Record findings
    as fixed-before-close **or** as new `fix` tickets; **do not fix them inside
    this ticket** (Guardrails: read-only over `src/` and `tests/`).
12. **Assemble the proof.** One row per gate from § 4, each with a verdict, the
    evidence artefact and where it lives. **A gate with no artefact is a failed
    gate** — record it `FAILED`, raise the follow-up ticket, and leave Phase 2
    open rather than passing it on judgement. Then edit
    `docs/desktop/README.md` § Status (`:138-142`) to give area 04 its own row
    with the date; leave the remaining areas' row intact.
13. **Tear down and close.**
    `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Stop` and record the
    final state. Write the proof with `set_ticket_doc`, and record
    `## Simplification pass` as **`n/a — docs-only`** with the date.

## UAT script

Written here rather than in the tree, per step 3 and the body's § Documentation
changes. Each scenario is reproducible from this text alone. Run in order:
A establishes the session, B needs the installed package from A, C needs a live
session, D needs a distinct account.

**Preconditions for all four.** The stack is up (step 4) with its run id
recorded. The package is installed **from the `teststack` `.appinstaller` on
the feed share**, never side-loaded. Three staff accounts exist in the local
Identity store — one ordinary, one to be disabled, one with
`MustChangePassword` set — and none is `Bootstrap:VerificationAccount`. Capture
every screenshot with
`winapp ui screenshot -a $AppPid -o "screenshots/<scenario>-<state>.png"`.

### Scenario A — Login with an existing Pegasus account

| | |
| --- | --- |
| **Steps** | 1. Launch the installed package. 2. Observe the startup sequence reach the login screen. 3. Enter the existing account's user name and password. 4. Submit. |
| **Expected** | The shell appears. **No** Microsoft-account prompt and **no** browser window at any point. The status bar reads the connected form. The rail shows the items that account's role allows ([[FND-046]], plan handle `DSK-04-10`). |
| **Evidence** | Shell screenshot; the rolling-log line carrying the startup correlation id; the gateway's `/connect/token` audit row. |
| **Fails if** | A browser opens; the login screen offers a Microsoft option; the access token is written anywhere on disk. |

### Scenario B — Obsolete client blocked, then updated

| | |
| --- | --- |
| **Steps** | 1. With the old client installed, raise the minimum client version through [[GWY-023]]'s administrator setting. 2. Record `Get-AppxPackage CollisionEngineers.Pegasus \| Select-Object Version`. 3. Relaunch the client. 4. Observe the update-required screen. 5. Take the update from the feed. 6. Relaunch and reach the shell. 7. Record `Get-AppxPackage` again. |
| **Expected** | Step 4 shows the screen `screen-specs.md:99-106` defines — title "Update required", current and minimum versions **as values**, primary "Update now", secondary "Sign out" — and no other route forward. Step 7's version is higher than step 2's. |
| **Evidence** | Both `Get-AppxPackage` transcripts; screenshots of the update-required screen and of the shell after the update; the gateway audit entry for the setting change. |
| **Fails if** | The block came from an unreachable feed rather than the gateway gate (App Installer **fails open**, `04/README.md:279-282`); the old client reaches the shell; the update-required screen offers a bypass. |

### Scenario C — Disabled account rejected

| | |
| --- | --- |
| **Steps** | 1. Sign in as the second account and leave the session live. 2. Disable that account through the administration path. 3. Make the next `/api/v1` call from the desktop (any navigation that fetches). 4. Attempt a token refresh. |
| **Expected** | Step 3 is refused on the **next request**, not after the access token expires (`Program.cs:353`, `ValidationInterval = TimeSpan.Zero`; `04/README.md:167-176` decision 3). The desktop shows the account-disabled state and **does not** retry in a loop (`04/README.md:227`). Step 4 returns `invalid_grant`. |
| **Evidence** | The integration-test output from [[GWY-022]]; a live screenshot of the desktop's disabled-account state; the timestamp gap between step 2 and step 3 showing it was under one request, not one lifetime. |
| **Fails if** | The session survives until token expiry; the desktop retries; the message reads as invalid credentials. |

### Scenario D — Password-change-required account routed correctly

| | |
| --- | --- |
| **Steps** | 1. Set `MustChangePassword` on the third account. 2. Sign in from the desktop. 3. Observe the routing. 4. Complete the change. 5. Continue into the shell. |
| **Expected** | Problem type `urn:pegasus:problem:password-change-required` (`04/README.md:228`) routes to the change-password screen and **blocks other work**. `screen-specs.md:108-114` fixes that screen: Current password, New password, Confirm new password; Save primary; validation messages attach to the field; minimum length shown **only** as a validation outcome and never as hint text; AutomationIds `Password.Current`, `Password.New`, `Password.Confirm`, `Password.Save`. |
| **Evidence** | Screenshot of the change-password screen; the problem-type value from the diagnostics log; a screenshot of the shell after the change. |
| **Fails if** | Any other screen is reachable before the change; the minimum length appears as hint text; the state is shown by colour alone (`frd-12-operator-experience.md:112`). |

## Verification

Evidence tiers from the body: **Tier 5** (the token and compatibility routes
through the real gateway), **Tier 7** (the login, blocked and update-required
screens on a real workstation with screenshots) and **Tier 11** (install,
forced upgrade, and the proven ability to return to the previous package).
"All three are owed; none substitutes for another" — so the table is grouped by
tier, and a tier with no row is a failed review.

| Tier | Command / observation | Expected | Evidence |
| --- | --- | --- | --- |
| — | `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start`, then `-Action Status` (bare — it refuses `-RunId`, `:1490-1492`) | exit `0`; every component healthy; run id recorded | `proof` (command-log), the first artefact |
| 5 | `dotnet test tests/Pegasus.IntegrationTests` | `Passed!`, including the disabled-account and revocation tests from [[GWY-021]] and [[GWY-022]] | TRX under `artifacts/test-results/`, summary into `proof` (test-output) |
| 1 | `dotnet test tests/Pegasus.ArchitectureTests` | `Passed!`; the no-MSAL/Entra and no-WebView desktop facts from [[FND-037]] are green | TRX, summary into `proof` (test-output) |
| 7 | Scenarios A–D executed once end to end | each scenario's Expected met; each Fails-if absent | `proof` (visual) — the screenshot set — plus the scenario table with verdicts |
| 11 | `Get-AppxPackage CollisionEngineers.Pegasus \| Select-Object Version` before and after the forced update | the version increases, **and** the app reaches the shell only after the update | `proof` (command-log) |
| 9 | The storage-review checklist from step 9 | recorded item by item — what was checked, not that checking happened | `proof` (command-log) |
| — | `git diff --name-only` at PR time | exactly `docs/desktop/README.md`; **no** `src/`, **no** `tests/`, **no** `.github/` | `proof` (command-log) — this is what makes a review that started fixing things visible |
| — | `get_doc_gates FND-050` after the proof is written | no unmet requirement for `enter-done` | the gate output itself |

**Stated limit, required in the proof:** this review runs on the local Test/UAT
stack. `docs/desktop/08-testing/test-uat-stack.md:173-186` § Known gaps is
explicit that the stack proves App Installer mechanics but **not** the
production host's configuration, the production certificate or the real share,
and gives no Azure SQL, Blob, Key Vault, Container App probe or App Insights
behaviour. Those are pilot-ring checks owned by [[REL-009]] (plan handle
`DSK-09-11`). Passing this gate does not claim them.

## Risks / open questions

- **Risk — the gate table is narrowed to fit what shipped.** The body records
  this as a *Concern*: the plan row's dependency is "all above", making this
  ticket the join point for fourteen others. Mitigation: step 2 lists every
  incomplete dependency and stops; step 12 records a missing artefact as
  `FAILED` rather than waived; the § 4 table is read from the file, not
  transcribed here.
- **Risk — scenario B proves a fail-open, not a block.** App Installer launches
  the app when the feed is unreachable (`04/README.md:279-282`), so an absent
  feed looks like a pass. Mitigation: scenario B's Fails-if row makes the
  gateway gate the required cause, and the setting-change audit entry is part
  of the evidence.
- **Risk — the package is side-loaded and the update path is untestable.**
  `CheckUpdateAvailabilityAsync` returns `Unknown` for a package not installed
  from an `.appinstaller` (`04/README.md:271-274`). Mitigation: step 5 and the
  scenario preconditions require installation from the `teststack` feed
  [[FND-048]] built.
- **Risk — the "independent" review is the implementer re-reading its own
  diff.** Mitigation: `.codex/agents/pegasus-desktop-reviewer.toml` is
  `sandbox_mode = "read-only"` and its instructions require it to stop if it
  implemented the change; step 11 restates it; `AGENTS.md` step 5 is cited in
  the Governing docs table.
- **Risk — the review starts fixing things.** The diff estimate is four lines
  for a reason. Mitigation: Guardrails make `src/` and `tests/` read-only and
  every finding a new `fix` ticket; the `git diff --name-only` row in
  Verification makes a widened diff visible.
- **Risk — the plaintext `Bootstrap:VerificationAccount` becomes the review's
  test login.** `04/README.md:288-291` records it in
  `src/Pegasus.Web/appsettings.json` and says it must never be the desktop test
  login in production. Mitigation: the UAT script's preconditions exclude it
  explicitly.
- **Scope boundary, not an open question — the package secret scan.**
  [[PLAT-003]] (plan handle `DSK-10-03`) is phase 8 and will probably not exist
  when this runs. Step 9 carries the manual fallback and requires the checked
  items to be named.
- **Scope boundary, not an open question — anything the local stack cannot
  prove.** Azure SQL semantics, Blob and Key Vault behaviour, Container App
  probes, App Insights, the production certificate and the real share are
  [[REL-009]]'s pilot-ring checks (`test-uat-stack.md:173-186`). Recording the
  limit is this ticket's job; closing it is not.
- **Scope boundary, not an open question — running against production.** A
  separate ticket with exact-target approval under `docs/runbook.md` § Live
  operation approval matrix, mirrored in
  `docs/desktop/11-azure-disposition/README.md`. ADR-0014 gives two
  environments and no third.
- **Operator dependency, not an open question.** Steps 5 and 7 need a Windows
  11 workstation and an operator; the ticket carries `needs-operator` for
  exactly that. Every decision this review depends on was settled on
  2026-08-23; `docs/desktop/README.md` records that **no open decisions
  remain**.
- **Open questions**: none. No `open-questions` document is created — the
  ticket body does not instruct one, and every unknown above is a scope
  boundary owned by a named sibling ticket, which
  `docs/desktop/00-governance-and-workflow/README.md` § 3 makes a boundary
  rather than a question.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading. This
branch changes Markdown only, so the expected record is `n/a — docs-only`._
