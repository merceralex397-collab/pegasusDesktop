# Research — FND-047: connectivity state in the desktop client

## Question

What does the Pegasus web client do today when the server becomes unreachable,
what does the desktop have to do instead under proposal § 11.3 and § 8.4, and
which existing repository mechanisms (HTTP pipeline, status bar, local stack,
UI harness) does the connectivity state have to be built out of rather than
beside?

## Current behaviour

**There is no connectivity state in the web application.** The browser owns
it, and the application's own scripts hand failures back to the browser:

- `src/Pegasus.Web/wwwroot/js/site.js:298-318` — the upload form posts with
  `fetch(...)` and its `.catch(function () { form.submit(); })` at `:317`
  re-submits natively on a transport failure. The operator then sees the
  browser's own error page, not a Pegasus message.
- `src/Pegasus.Web/wwwroot/js/site.js:437-447` — the same shape on the
  type-ahead lookup: `.catch(function () { ... })` at `:446`.
- `src/Pegasus.Web/wwwroot/js/site.js:644-664` — the only place that renders a
  failure in Pegasus's own words: the mail quick-preview sets
  `status.textContent = 'Quick preview unavailable. Open the message for full
  detail.'` at `:663`. It is per-panel, not global, and it does not disable
  anything.
- `src/Pegasus.Web/Pages/Shared/_Layout.cshtml` (135 lines) has no status bar
  and no connection region — searching it for `offline`, `connection` or
  `navigator.onLine` returns nothing.

Every other page is a full server round trip, so "unreachable" is simply a
page that does not load. **Nothing in the web application disables a save
because the server is unreachable**, because a save that cannot reach the
server never renders a success page in the first place. The desktop keeps its
window open across the failure, which is what creates the requirement.

**Parity-matrix row: `PAR-45`.** `docs/desktop/01-inventory-and-parity/parity-matrix.md`
row `PAR-45` (13.10 Operations (health)) is the row that covers this: its
current entry point is `/health/live`, `/health/ready`
(`src/Pegasus.Web/Program.cs:939-950`) and `GET /diagnostics/version`
(`Program.cs:954`); its **native design column already reads "Status bar
health (§18.3), About/version"** and its API column already names
`GET /api/v1/client-compatibility` (new). Status `inventoried`.

`PAR-01` (13.1 Access and session) is adjacent, not the owner: its native
design column names "connectivity and update-required states" but scopes them
to the **login screen**, which is [[FND-044]] (plan handle `DSK-04-08`), not
the shell status bar.

The matrix holds 46 rows, `PAR-01`…`PAR-46` (`grep -c '^| PAR-'
docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`, run 2026-08-24),
all keyed to page models under `src/Pegasus.Web/Pages/**` except the two
cross-cutting rows `PAR-44` and `PAR-45`. The shell itself has no row because
it replaces `Pages/Shared/_Layout.cshtml`, which is not a page model
(`docs/desktop/06-ui-design/screen-specs.md:41` says the shell "replaces
`Pages/Shared/_Layout.cshtml`, `_LayoutAuth.cshtml`").

## Findings

### Facts

Verified by reading the repository or the pinned documentation on 2026-08-24.

- **The disconnected wording and the AutomationIds are already settled.**
  `docs/desktop/06-ui-design/screen-specs.md:74-76`: "Status bar: connection
  state, last sync time (Europe/London), background transfer summary …, update
  availability"; ":77-78": "Connectivity state: 'Disconnected — reconnecting'
  in the status bar; saves disabled; existing content visible (proposal
  §11.3)"; ":85-86" fixes the ids `Shell.Status.Connection` and
  `Shell.Status.Update`. The connected form is shown in the shell sketch at
  `:63` as `Status: Connected · Last synced 14:02 · Pilot channel · Update
  available`.
  - The title bar carries a second, smaller connection affordance —
    "connection glyph + word" (`screen-specs.md:73`). Glyph **plus word**, not
    glyph alone: `screen-specs.md:38` states "State is never colour alone" as
    a rule binding every block.
- **The session-failure matrix already assigns this row.**
  `docs/desktop/04-auth-session-update-and-startup/README.md:230` — "Server
  unreachable / TLS failure | transport exception | Disconnected state in
  status bar; periodic recheck; never shown as bad credentials". The signal is
  a **transport exception**, not an HTTP status code: a `401`, a `429` and a
  `503` are all *reachable*.
- **The recheck endpoint exists as a decided design, not yet as code.**
  `docs/desktop/04-auth-session-update-and-startup/README.md:178-188` decision
  5 defines `GET /api/v1/client-compatibility` as **anonymous**, with no
  rate-limit bypass, returning `minimumVersion`, `currentVersion`, `channel`,
  `maintenanceMessage`, `validForSeconds`. It is authored by [[GWY-023]]
  (plan handle `DSK-04-06`), not here. Anonymity is what makes it safe to poll
  while the session may itself be dead.
- **The progress affordance is constrained.**
  `docs/desktop/06-ui-design/tokens-and-theme.md:184` — "thin indeterminate
  `ProgressBar` at the top of the content region or in the status bar; no ring
  spinners; honours `UISettings.AnimationsEnabled` with a static 'Working'
  text equivalent". `docs/desktop/06-ui-design/README.md:165` repeats it and
  adds "no full-page spinners; no animated transitions".
- **`TimeProvider` is this repository's clock abstraction, not a new idea.**
  Real uses: `src/Pegasus.Core/Cases/CaseNotes.cs:33`,
  `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:341-343`,
  `src/Pegasus.Core/Custody/CustodyContracts.cs:543`,
  `src/Pegasus.Core/AiWork/AiWorkOperations.cs:140`. Injecting `TimeProvider`
  for the recheck interval matches the existing pattern exactly and makes the
  interval testable without a real wait.
- **`tests/Pegasus.Desktop.ViewModelTests` is scheduled to ship with a fake
  clock.** `docs/desktop/02-architecture-and-foundation/README.md:254` row
  `DSK-02-13`: "(xunit, fakes for API client/clock/credential store)", and its
  first tests are specified as covering "shell navigation and **status-bar
  state**". Board id [[FND-038]].
- **`Pegasus.Desktop.Infrastructure` is a planned project, not an existing
  one.** `docs/desktop/02-architecture-and-foundation/README.md:210` defines
  it as new, `net10.0-windows10.0.26100.0`, referencing Core and Contracts
  only, holding "Generated API client + HTTP pipeline, credential store,
  bounded cache, diagnostics, Windows integration". Confirmed absent today:
  `ls src` → `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`,
  `Pegasus.Worker` (2026-08-24). `Pegasus.slnx` lists the same four plus three
  test projects.
- **`TreatWarningsAsErrors` is on solution-wide.**
  `Directory.Build.props:8`, together with `Nullable=enable` (`:3`) and
  `AnalysisLevel=latest-recommended` (`:6`). An unobserved `async void`, an
  unawaited task in the recheck loop, or a nullable-annotation slip is a build
  failure, not a warning.
- **The local stack can be stopped and started, which is how "offline" is
  produced.** `scripts/Invoke-LocalDevelopment.ps1:3` declares
  `[ValidateSet('Start', 'Status', 'Smoke', 'Stop', 'Reset')] [string]$Action
  = 'Status'`. `Stop` and `Start` are both real values, so the ticket's UI
  script step is runnable as written.
- **The UI harness contract is real and pinned in the skill.**
  `.codex/skills/winui-ui-testing/SKILL.md:47` —
  `param([Parameter(Mandatory)][int]$AppPid)`; `:57` — the `Test-UI` helper;
  `:74` — `Test-UI "…" { winapp ui wait-for "NavHome" -a $AppPid -t 3000 }`.
  Critically, `:138` documents the exact form this ticket needs for a status
  bar: `wait-for "StatusBar" --value "words" --contains` — "substring match
  for dynamic content". The harness file itself is owned by [[TEST-006]]
  (plan handle `DSK-08-06`).
- **Screenshots have a real command.** `.codex/skills/winui-ui-testing/SKILL.md:115`
  — `winapp ui screenshot -a $AppPid -o "screenshots/01-initial.png"`.
- **ADR-0104 does not exist yet.** `docs/adr/` holds ADR-0001…ADR-0029; the
  desktop block ADR-0100…ADR-0110 is reserved
  (`docs/desktop/00-governance-and-workflow/README.md:140-165`) and ADR-0104
  ("Online-required; no offline replication; bounded local cache only") is
  listed at `:159` as still to be authored.

### Assumptions

- **A-04-11-1** — [[FND-031]] (plan handle `DSK-02-06`) ships exactly one
  `DelegatingHandler` on the desktop's `IHttpClientFactory` pipeline, so there
  is a single place to set the state from.
  *Confirms it:* reading the delivered
  `src/Pegasus.Desktop.Infrastructure/Http/` folder when [[FND-031]] lands.
  *If wrong:* the state must be set from whichever handler is outermost, and
  if there are several, from a new outermost one — but there is still exactly
  one `IConnectivityState`, because the body's Traps section forbids a second
  signal source.
- **A-04-11-2** — the desktop's HTTP failures surface as
  `HttpRequestException` (with `SocketException`/`AuthenticationException`
  inners) and `TaskCanceledException` on timeout, rather than as a
  library-specific exception type from the generated client.
  *Confirms it:* the generated client from [[FND-029]] (plan handle
  `DSK-02-04`) / [[FND-031]]; NSwag- and Kiota-generated clients both wrap but
  do not swallow the transport exception.
  *If wrong:* the classification predicate in the handler widens; the state
  machine and every test are unchanged, because the handler is the only place
  that classifies.
- **A-04-11-3** — the shell's status bar from [[FND-033]] (plan handle
  `DSK-02-08`) exposes a bindable text region rather than a fixed
  `TextBlock` per field.
  *Confirms it:* reading `ShellPage.xaml` when [[FND-033]] lands.
  *If wrong:* this ticket adds the binding target itself, inside the status
  bar [[FND-033]] created; the `Shell.Status.Connection` AutomationId is
  unchanged either way, which is what the UI assertion keys on.
- **A-04-11-4** — no command view model exists yet that performs an
  authoritative save, so step 6's base behaviour is applied to the base type
  and not retrofitted across a dozen call sites.
  *Confirms it:* `grep -rn "RelayCommand\|AsyncRelayCommand" src/Pegasus.Desktop`
  at implementation time.
  *If wrong:* the sweep in step 8 grows; the base behaviour does not change.
- **A-04-11-5** — `Windows.UI.ViewManagement.UISettings.AnimationsEnabled` is
  readable from a packaged WinUI 3 desktop process without a dispatcher hop
  on the property read itself.
  *Confirms it:* Microsoft Learn `UISettings.AnimationsEnabled`, and the
  running app.
  *If wrong:* read it once during shell construction on the UI thread and
  cache it; the reduced-motion requirement is met either way.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md:166-178`, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The state is one workstation's view of its own link to the gateway, derived from that process's own HTTP responses (body step 3). Two operators can legitimately disagree about it at the same instant; a shared value would be wrong for one of them. |
| Unattended execution — must it run with every desktop closed? | **No** | It exists solely to tell a signed-in operator that the window in front of them cannot reach the server. With every desktop closed there is no one to tell and nothing to disable. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No** | The recheck target `GET /api/v1/client-compatibility` is defined **anonymous** (`docs/desktop/04-auth-session-update-and-startup/README.md:178-181`). No credential is used, which is deliberate: the poll must work while the session itself is dead. |
| Public callback — must an external service call a stable public endpoint? | **No** | The desktop polls outward on a fixed interval (body step 4). Nothing calls in; no listener, no inbound port, no webhook. |
| Central enforcement — revocation, permissions, audit or an invariant that must hold independently of the client? | **No**, for what this ticket places | The indicator and the disabled command are affordances. The invariant the body cares about — "no command reports success without server confirmation" — is enforced by the server's response existing or not; the client cannot manufacture one. The one genuinely central responsibility nearby, the minimum-version gate and its endpoint, is **already placed in the gateway** and owned by [[GWY-023]] (plan handle `DSK-04-06`); this ticket only consumes it. Consuming a centrally-placed endpoint is not placing a responsibility. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | No measurement exists, and none is possible in the direction claimed: a server-side connectivity service cannot report on a link it is itself on the far side of. `docs/engineering.md:203-207` § Plan sizing forbids defending an assumption a query would settle; here the query is unanswerable by construction. |

**All six "no" — the responsibility belongs in the desktop.** The verification
lands there too: L-02 (`docs/desktop/README.md` § Locked decisions) makes the
offline scenario a **stopped local gateway**
(`pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Stop`), not an Azure
outage, so proving this ticket needs no Azure resource and no Azure read.

## Implications

1. **The signal is a transport exception, and only a transport exception.**
   `README.md:230`'s matrix row is explicit. Treating `401`, `429`, `503` or a
   problem-details body as "disconnected" would collide with the five rows
   above it in the same matrix, each of which already owns its own desktop
   behaviour. The handler classifies; nothing downstream re-classifies.
2. **The state object is set by real traffic and only *rechecked* by a poll.**
   Body step 3 forbids a ping loop as the primary signal. That inverts the
   usual shape: the poll exists only in the `Disconnected` branch, and stops
   the moment a response arrives. This is also why the poll can be cheap — it
   runs at most while nothing else is working.
3. **"Saves disabled" must be one behaviour, not a per-view-model check.**
   Body step 6 says one base command behaviour whose `CanExecute` returns
   false while disconnected. A per-command `if` would be the third-copy defect
   `docs/engineering.md` § One Core owner exists to prevent, and would make
   acceptance criterion 5 unprovable — you cannot audit a rule that is written
   in twenty places.
4. **A queue is forbidden, and the ticket must say so in the diff.** Proposal
   § 11.3 allows only an explicit draft, drafts belong to area 05, and ADR-0104
   (online-required) is the decision behind it. "No silent queueing" is
   therefore an *absence* to be proved, which is why the Out-of-scope section
   of the files document and a test that a transport failure yields a failure
   state (body step 8) both matter more than usual.
5. **The reconnecting indicator is constrained by two documents at once** —
   thin indeterminate `ProgressBar` only, and a static "Working" text
   equivalent when animations are off. There is no design freedom here to
   spend.
6. **The UI evidence is a contribution to someone else's file.** The harness
   skeleton belongs to [[TEST-006]]; this ticket adds `Test-UI` cases and
   nothing else in that folder (body Guardrails). `wait-for … --value …
   --contains` (`SKILL.md:138`) is the assertion form for a status bar whose
   text also carries a timestamp.
7. **The recheck interval is a number this plan must state**, because the UI
   assertion's timeout is derived from it and the acceptance criterion is
   written as "within one recheck interval".

## Open questions

None that block. Every unknown above is either an assumption with a named
confirming action (A-04-11-1…5) or a scope boundary owned by a named sibling
ticket — [[GWY-023]] for the compatibility endpoint, [[TEST-006]] for the UI
harness skeleton, [[FND-031]] for the HTTP pipeline, [[FND-033]] for the
status bar, [[FND-008]] (plan handle `DSK-00-08`) for FRD-13. Those belong in
the plan's *Risks / open questions* section, not in an `open-questions`
document (`docs/desktop/00-governance-and-workflow/README.md` § 3). The ticket
body does not instruct one to be opened.

The recheck interval is a **trivial default taken rather than asked**: 15
seconds, recorded in the plan as the body's step 4 requires.
