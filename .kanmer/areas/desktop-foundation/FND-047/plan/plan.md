# Plan — FND-047: connectivity state — disconnected indicator, automatic recheck, saves disabled

**Diff estimate: ~11 files, ~760 lines.**

Derived from the files document's *Where the change lands* table, not asserted:
6 new files (`IConnectivityState.cs` ~30, `ConnectivityState.cs` ~110,
`ConnectivityRecheckService.cs` ~95, `ConnectivityAwareCommand.cs` ~70,
`ConnectivityStateTests.cs` ~160, `ConnectivityCommandGatingTests.cs` ~120 =
~585) and 5 edits (the existing `DelegatingHandler` ~25, `ShellViewModel.cs`
~55, `ShellPage.xaml` ~30, `App.xaml.cs` ~6, `ui-tests.ps1` ~60 = ~176).
`docs/engineering.md:201-203` § Plan sizing requires the estimate first.

## Approach

One state object, written by the real HTTP pipeline and read by everything
else. The desktop's existing `DelegatingHandler` classifies each attempt —
transport exception or TLS failure sets `Disconnected`, any response at all
sets `Connected` — and a poll of the anonymous
`GET /api/v1/client-compatibility` runs **only** while `Disconnected`, so the
app is silent on the wire whenever it is working. Command gating hangs off the
same object through one base command behaviour, so "saves are disabled while
offline" is a single rule with a single test.

The alternative considered and rejected was a dedicated availability probe —
subscribe to `NetworkInformation.NetworkStatusChanged` and/or ping on a timer
regardless of state — with the pipeline merely reading the result. It was
rejected for three reasons. It answers the wrong question: Windows reporting a
network is not the gateway being reachable, and an operator on a live VPN with
a dead gateway is precisely the case § 11.3 is written for. It creates a second
signal source, which the ticket's Traps section forbids in as many words. And
it makes the state a guess rather than a fact: the pipeline's own exception is
evidence of the exact failure the operator just experienced, whereas a probe
result is evidence about a different request at a different moment. The chosen
shape also matches the repository's own habit — `site.js:663` reports what the
request that just failed proved, and nothing more.

## Governing docs

### Linked `refs`

| Ref | Requirement | Meets |
| --- | --- | --- |
| `docs/frd/frd-12-operator-experience.md:20-22` | The operator-visible state vocabulary — "loading, empty, current, stale, **unavailable**, partial, **failed**, validation, conflict, and access-denied states" — with exact state labels | **Meets** — Steps 5 and 6 render *unavailable* as the settled words "Disconnected — reconnecting" and make *failed* the only outcome a command can reach while disconnected. No new state word is invented. |
| `docs/frd/frd-12-operator-experience.md:96-98` | "`0`, loading, current, **stale-with-last-good-time**, partial, unavailable, and failed are distinct outcomes. A refresh never replaces a [good value with nothing]" | **Meets** — Step 5 keeps the last successful sync time visible beside the disconnected words (that *is* stale-with-last-good-time), and step 7 keeps on-screen content rendered rather than cleared. |
| `docs/frd/frd-12-operator-experience.md:112` | "The UI never infers state from colour alone" | **Meets** — Step 5 requires text plus glyph for both forms; step 11's assertion reads the **text**, so a colour-only regression fails the test rather than passing review. |

No FRD text is modified by this ticket.

### `docs_todo: true`

`get_doc_gates FND-047` reports `docs_todo: true`, so no conversion FRD
governs this yet.

> **New FRD** — FRD-13 "Desktop operator experience", authored by [[FND-008]]
> (plan handle `DSK-00-08`).
> This plan is written to the connectivity behaviour as recorded in
> `docs/desktop/06-ui-design/screen-specs.md:74-78` and the *Session failure
> matrix* row at
> `docs/desktop/04-auth-session-update-and-startup/README.md:230`; if FRD-13
> lands differently this plan is revised before implementation.

No ADR is authored here. The decision this plan rests on — online-required, no
offline replication, bounded local cache only — is **ADR-0104**, authored by
[[FND-005]] (plan handle `DSK-00-05`); ADR-0105 has more than one claimant, so
where this plan's update-adjacent statements touch it, read
`authored by [[FND-005]]; see [[FND-005]]'s plan for the ownership
reconciliation`. ADR-0104 does not exist yet: `docs/adr/` holds
ADR-0001…ADR-0029 and the block ADR-0100…ADR-0110 is reserved
(`docs/desktop/00-governance-and-workflow/README.md:140-165`).

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 11.3 Connectivity handling | On loss: existing data stays visible; new authoritative saves disabled or queued only as an explicit draft; the status area clearly says Pegasus is disconnected; automatic recheck; nothing presented as complete until the server confirms it | Steps 5, 6, 7, 4, 8 respectively |
| Proposal § 8.4 Session failure handling | Server-unreachable is never presented as invalid credentials | Step 3's classification (transport exception only) and step 10's fourth test case |
| Proposal § 16.1 Operation model | No command reports success without server confirmation | Steps 6 and 8; the gating test in step 10 |
| Proposal § 11.2 | No offline replication and therefore no conflict resolution to import | The Out-of-scope entry forbidding any queue or pending-command store |
| `docs/desktop/04-auth-session-update-and-startup/README.md:230` | Transport exception → disconnected in the status bar; periodic recheck; never bad credentials | Steps 3, 4, 5 |
| `docs/desktop/04-auth-session-update-and-startup/README.md:178-181` | `GET /api/v1/client-compatibility` is anonymous, no rate-limit bypass | Step 4 — the recheck sends no bearer token |
| `docs/desktop/06-ui-design/screen-specs.md:74-78` | The settled status-bar contents and the literal string "Disconnected — reconnecting" | Step 5 |
| `docs/desktop/06-ui-design/screen-specs.md:31-39, :85-86` | AutomationId convention; `Shell.Status.Connection`; state never colour alone | Steps 5 and 11 |
| `docs/desktop/06-ui-design/tokens-and-theme.md:184`; `06-ui-design/README.md:165` | Thin indeterminate `ProgressBar` only; no ring or full-page spinner; honour `UISettings.AnimationsEnabled` with a static "Working" text equivalent | Step 9 |
| `docs/engineering.md:78` (tier 7) | "keyboard, focus and error behavior, semantic labels, **text-plus-colour states**" | Steps 5, 11, 12 |
| `docs/engineering.md` § One Core owner | A rule has exactly one implementation | Step 6 — one base command behaviour, never a per-view-model check |
| **L-02** (`docs/desktop/README.md` § Locked decisions) | Test/UAT is the local production-mimicking stack; no Azure test environment | Step 11 produces "offline" with `Invoke-LocalDevelopment.ps1 -Action Stop` |
| **ADR-0104** (pending, [[FND-005]]) | Online-required; no offline replication; bounded local cache only | The Out-of-scope entry and step 6's disable-rather-than-queue |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over the branch's own diff, recorded under a dated heading in the plan | Step 13, and the `## Simplification pass` heading below |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing → Reviewer |

## Routing

Copied from the ticket body's `## Routing` block; required in the plan
document by `docs/desktop/00-governance-and-workflow/README.md` § Ticket
template.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`, `microsoft/win-dev-skills` v0.5.0
  `f1028dd5`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`)
  for the offline UI script
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`) for `UISettings.AnimationsEnabled`
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan`
  → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
  (call `get_doc_gates <id>` before every move; a move crosses at most one
  gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

`.codex/skills/winui-ui-testing/SKILL.md` and `.codex/skills/winui-design/`
were confirmed present on 2026-08-24 (`ls .codex/skills/`).

## Steps

These refine the ticket body's implementation steps 1–13 in the same order,
with the same ownership and the same file paths; they add the *how* and change
nothing the body decided.

1. **Orientation.** Read
   `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 (the session
   failure matrix, `:214-231`) and
   `docs/desktop/06-ui-design/screen-specs.md:41-86` (§ Shell). Call
   `get_doc_gates FND-047`, then `take_ticket` with the real branch and
   worktree (`AGENTS.md` § Repository task workflow steps 1–2: branch
   `task/<slug>` from `origin/dev`, worktree under
   `../pegasus-worktrees/<slug>`). Load `pegasus-desktop`, then `winui-design`.
2. **Add `IConnectivityState` and `ConnectivityState`** under
   `src/Pegasus.Desktop.Infrastructure/Connectivity/`. Surface exactly three
   things: a `Connected | Disconnected` value, the `DateTimeOffset` of the last
   successful server response, and a change event. Constructor-inject
   `TimeProvider` in the repository's established shape — a `readonly` field
   assigned in the constructor, as
   `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:341-343` and
   `src/Pegasus.Core/Cases/CaseNotes.cs:33` do. **Do not introduce an
   `IClock`**: `grep -rn "interface IClock" src` returns nothing, so an
   `IClock` would be a second concept for one job. Raise the change event only
   on an actual transition, never on a repeat of the same value, or the status
   bar re-renders on every request.
3. **Set the state from the existing `DelegatingHandler`** created by
   [[FND-031]] (plan handle `DSK-02-06`) — edit it; do **not** add a second
   handler. In `SendAsync`: on a successful round trip (any response object,
   including a `4xx` or `5xx`) record the timestamp and set `Connected`; on
   `HttpRequestException` or `TaskCanceledException` from the transport, and on
   an `AuthenticationException` inner (TLS failure), set `Disconnected` and
   **re-throw**. The handler is the only place that classifies, and it
   classifies on the *exception*, never on a status code: `401`, `429` and the
   problem types are owned by the five rows above this one in
   `README.md:224-231` and belong to [[FND-043]] (plan handle `DSK-04-07`) and
   [[FND-044]] (plan handle `DSK-04-08`). `Directory.Build.props:8` sets
   `TreatWarningsAsErrors=true`, so a swallowed exception or an unobserved task
   here is a build failure.
4. **Add the recheck, and only while disconnected.**
   `ConnectivityRecheckService` starts a loop on the transition to
   `Disconnected` and cancels it on the transition to `Connected`. It calls
   `GET /api/v1/client-compatibility` — **anonymous**, no bearer token
   (`README.md:178-181`), so it still works when the session itself is dead —
   and any response at all is success, since step 3 already treats a response
   as reachability. **The interval is 15 seconds**, declared once as
   `internal const int RecheckIntervalSeconds = 15;` on this class and
   referenced nowhere else. *Recorded as the body's step 4 requires; taken as a
   trivial default rather than asked (contract § 7): it is short enough that
   "within one recheck interval" is not a visible wait for an operator, long
   enough that ten clients on a dead VPN cost nothing, and it sets step 11's
   `wait-for` timeout at `-t 20000` with margin.* The endpoint is defined by
   [[GWY-023]] (plan handle `DSK-04-06`); if it has not landed, the loop targets
   it anyway and simply stays disconnected — which is the correct behaviour, not
   a fallback.
5. **Bind the status bar.** In [[FND-033]]'s (plan handle `DSK-02-08`)
   `ShellViewModel` and `ShellPage.xaml`, render the connected form as the
   connection word plus the last sync time in **Europe/London**
   (`screen-specs.md:74`, matching the sketch at `:63`) and the disconnected
   form as the literal string **"Disconnected — reconnecting"**
   (`screen-specs.md:77-78`) with the last-good sync time still shown beside
   it — that is FRD-12's *stale-with-last-good-time*
   (`frd-12-operator-experience.md:96-98`), and dropping it would replace a
   good value with nothing. Set
   `AutomationProperties.AutomationId="Shell.Status.Connection"`. **Text plus
   glyph, never colour alone** (`screen-specs.md:38`,
   `frd-12-operator-experience.md:112`, tier 7 at `docs/engineering.md:78`).
   Marshal the change event onto the UI thread; unsubscribe when the shell
   window closes.
6. **One command behaviour, not twenty checks.** Add
   `ConnectivityAwareCommand` in `src/Pegasus.Desktop/Commands/`, whose
   `CanExecute` returns `false` while `IConnectivityState` is `Disconnected`
   and which raises `CanExecuteChanged` on the state's change event. Every
   authoritative save and command view model derives from or composes it. **Do
   not implement a queue**: proposal § 11.3 allows only an explicit draft,
   drafts are owned by area 05, and ADR-0104 is the decision behind it. A
   per-view-model `if` here would be the third-copy defect
   `docs/engineering.md` § One Core owner exists to prevent, and would make
   acceptance criterion 5 unauditable.
7. **Keep the window usable.** No page clears itself on disconnection, no
   navigation is blocked, read-only content stays readable, and **sign-out and
   token clearing stay available** (§ 11.3). Assert the sign-out path is not
   routed through `ConnectivityAwareCommand`; it is local work, not an
   authoritative save.
8. **Audit for silent success.** Sweep every command path added to
   `src/Pegasus.Desktop` so far and confirm none reports completion on
   anything but a server response. Where one does, route it through step 6's
   behaviour. Record the swept list in the post-implementation report so the
   reviewer can see the audit happened rather than taking it on trust.
9. **Reduced motion.** The reconnecting indicator is a **thin indeterminate
   `ProgressBar`** in the status bar (`tokens-and-theme.md:184`) with a static
   "Working" text equivalent when
   `Windows.UI.ViewManagement.UISettings.AnimationsEnabled` is false. No ring
   spinner, no full-page spinner, no animated transition
   (`06-ui-design/README.md:165`). Read `AnimationsEnabled` once on the UI
   thread during shell construction and cache it (assumption A-04-11-5).
10. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` (project from
    [[FND-038]], plan handle `DSK-02-13`), using that project's fake API client
    and fake clock. Four independently failing cases:
    (a) a transport exception flips the state to `Disconnected` within one
    handler pass; (b) save commands report `CanExecute == false` while
    disconnected; (c) a successful recheck flips back to `Connected` and
    re-enables the commands, driven by advancing the fake `TimeProvider` past
    the 15-second interval rather than by waiting; (d) a transport failure
    produces the disconnected state and **never** an invalid-credentials
    message — assert on the message the view model surfaces, not merely on the
    state, because § 8.4 is about what the operator reads.
11. **Load `winui-ui-testing`, then contribute two cases** to
    `tests/Pegasus.Desktop.UITests/ui-tests.ps1`. **Do not author a second
    harness.** That file's contract is owned and pinned by [[TEST-006]] (plan
    handle `DSK-08-06`): the signature is
    `param([Parameter(Mandatory)][int]$AppPid)` — never `$Pid`, which is
    read-only in PowerShell — and the pass/fail counter is its `Test-UI`
    helper (`.codex/skills/winui-ui-testing/SKILL.md:47, :57`). Each case is a
    `Test-UI` block: start the local stack, sign in, then
    `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Stop`
    (`Stop` is a real value —
    `scripts/Invoke-LocalDevelopment.ps1:3`), and assert
    `winapp ui wait-for "Shell.Status.Connection" -a $AppPid --value
    "Disconnected" --contains -t 20000` — `--contains` is required because the
    text also carries a timestamp (`SKILL.md:138`) — plus a save control
    reporting disabled. Then `-Action Start` and assert the state returns.
    **Use `wait-for`, never `Start-Sleep`.** If [[TEST-006]] has not landed,
    create the file from the `winui-ui-testing` script template with exactly
    that signature and that helper so the two cannot fork, and record here that
    [[TEST-006]] takes ownership of the skeleton when it lands.
12. **Screenshots.** `winapp ui screenshot -a $AppPid -o
    "screenshots/connectivity-connected.png"` and `…-disconnected.png`
    (`SKILL.md:115`); both attach to the ticket proof as the tier-7 visual
    evidence.
13. **Simplification pass** over this branch's diff (four lenses), recorded
    under a dated `## Simplification pass` heading in this document, then open
    the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 7 — Browser/accessibility**
(`docs/engineering.md:78`). The body is explicit that this ticket owes
UI-driven evidence: the disconnected state readable as **text**, not colour
alone; keyboard reachability of the status area; and screenshots of both
states through `winapp ui`.

| Command | Expected | Becomes evidence as |
| --- | --- | --- |
| `dotnet test tests/Pegasus.Desktop.ViewModelTests` | `Passed!`, with cases (a)–(d) from step 10 green | TRX under `artifacts/test-results/`, summary into `proof` (test-output) |
| `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid>` with the gateway stopped mid-run | every assertion PASS; `Shell.Status.Connection` contains "Disconnected — reconnecting" within 20 s; the save control reports disabled | the harness transcript into `proof` (command-log) |
| `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Stop` then `-Action Start` during the UI script | the state returns to connected **without restarting the desktop app** | the same transcript, with the two script invocations visible in order |
| `grep -rn "Start-Sleep" tests/Pegasus.Desktop.UITests` | no matches | console output into `proof` (command-log) |
| `winapp ui screenshot` for both states | two files under `screenshots/` showing the words, not just a colour | `proof` (visual) |

Keyboard reachability of the status area is checked in the same session with
`winapp ui get-focused` after tabbing to it, and recorded as a line in the
proof — tier 7 states plainly that automated results do not replace a manual
keyboard review (`docs/engineering.md:78`).

## Risks / open questions

- **Risk: the recheck poll keeps running after reconnection.** A leaked loop
  turns the "no ping loop as the primary signal" rule into a lie in the diff.
  Mitigation: the loop's `CancellationTokenSource` is cancelled on the
  transition to `Connected` in the same handler that raises the event, and step
  10 case (c) asserts one recheck request and no more after the flip back.
- **Risk: a `401` or a `503` is classified as disconnected.** That would
  collide with rows already owned elsewhere and would surface a session
  failure as a connectivity failure. Mitigation: step 3 classifies on the
  exception only; a response object of any status is reachability. The five
  rows above this one belong to [[FND-043]] and [[FND-044]] — a **scope
  boundary owned by named tickets**, not an open question.
- **Risk: `GET /api/v1/client-compatibility` does not exist when this ticket
  runs.** Owned by [[GWY-023]] (plan handle `DSK-04-06`). The consequence is
  benign — the app stays disconnected, which is honest — and the UI script
  proves the flip-back only once [[GWY-023]] has landed. A scope boundary, not
  an open question.
- **Risk: [[FND-031]] ships more than one `DelegatingHandler`** (assumption
  A-04-11-1). Mitigation: set the state from the outermost handler; if several
  exist, add one outermost handler that does only this. There is still exactly
  one `IConnectivityState`, because the body's Traps forbid a second signal
  source.
- **Risk: the shell status bar is fixed XAML rather than a bound region**
  (assumption A-04-11-3, owned by [[FND-033]]). Mitigation: this ticket adds
  the binding target inside the status bar [[FND-033]] created. The
  `Shell.Status.Connection` AutomationId — which is what the assertion keys on
  — is unchanged either way.
- **Risk: the 15-second interval turns out to be wrong in the pilot.**
  Mitigation: it is a single named constant in one class (step 4), so changing
  it is a one-line diff and one test constant. Recorded here rather than
  parked, because the body required the value to be stated.
- **Question — who answers:** whether `Shell.Status.Connection` joins the
  standing AutomationId coverage audit. Answered by [[DUI-015]] (plan handle
  `DSK-06-15`), which owns the audit, or by [[TEST-006]] when the harness is
  formalised. This ticket captures the evidence in-session and creates no
  harness.

No `open-questions` document is created: the ticket body does not instruct one,
the operator decisions of 2026-08-24 settle nothing this ticket touches, and
every unknown above is either an assumption with a named confirming action or a
scope boundary owned by a named sibling ticket — which
`docs/desktop/00-governance-and-workflow/README.md` § 3 makes a boundary rather
than a question.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass
over this branch's own diff before the PR, recorded here under a dated
heading._
