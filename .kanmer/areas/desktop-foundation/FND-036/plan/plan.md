# Plan — FND-036: Unhandled-exception path and the exportable diagnostics bundle

**Diff estimate: ~8 files, ~430 lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the `files`
document, file by file, measured 2026-08-24:
`Diagnostics/DiagnosticsBundleManifest.cs` ~55 (the record, `SchemaVersion`, the entry-name
constants);
`Diagnostics/DiagnosticsBundleBuilder.cs` ~170 (collect, re-redact, zip, prune);
`src/Pegasus.Desktop/App.xaml.cs` ~+60 (three handlers, the bounded crash path and its fallback);
`src/Pegasus.Desktop/Hosting/PegasusHost.cs` ~+10 (the registration and the cached package identity);
`src/Pegasus.Desktop/Views/Settings/` Diagnostics section ~70 (two commands, XAML plus view-model);
`docs/runbook.md` ~+18.
The three test groups land in `tests/Pegasus.Desktop.ViewModelTests` (~150 lines) and are counted
against that project. Nothing under `src/Pegasus.Core`, `src/Pegasus.Infrastructure`,
`src/Pegasus.Web` or `src/Pegasus.Worker` is touched.

## Approach

Build **one** `DiagnosticsBundleBuilder` that both paths call — the crash handler with reason
`crash`, the settings command with reason `user-export` — so there is a single collection rule, a
single redaction pass and a single retention bound. The alternative is a lightweight crash writer
plus a fuller export builder, on the reasoning that a crash path should do less. It is rejected
because the crash bundle is the one that matters most and the one nobody exercises in normal use: two
implementations means the path with the fewest witnesses is also the one with its own untested
collection logic. Instead the *same* builder is called with a **bound** (step 6), and when the bound
truncates it, the manifest records that it did.

Three decisions follow, and each is written down because the body leaves them open:

1. **Copy `EvaBundleSchema.cs`'s shape.** This repository has already built a versioned, hashed zip
   artefact: `src/Pegasus.Core/Eva/EvaBundleSchema.cs:523` declares a named
   `public const string SchemaVersion = "eva-handoff-v2"`, `:524-525` fix entry names as constants,
   `:737` writes the version *into* the JSON so the artefact carries it, `:809` builds the archive
   with `new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8)`, and
   `:823-825` funnels every entry through one `WriteEntry` helper. Following it means the manifest,
   the schema test and the support runbook all reference one set of literals, and it settles that a
   versioned zip is an accepted pattern here rather than an invention.
2. **Capture package identity once at host build, not during a crash.** The manifest wants
   `Package.Current.Id.FamilyName`, `Name`, `Publisher` and `Version`. Calling into WinRT while the
   process unwinds is how a crash becomes a hang. Caching it in [[FND-032]] (plan handle
   `DSK-02-07`)'s host costs nothing and removes a failure mode (A-FND036-3).
3. **The bound beats completeness.** Step 4 re-redacts every collected file and step 6 caps the
   handler's time; on a large rolling log they conflict. When they do, truncate and say so in the
   manifest. The body's own words are the reason: "a crash handler that hangs looks identical to a
   hung application", and a hung application is the worse outcome.

**The ownership reconciliation the Guardrails require, settled here before any code is written.**
[[PLAT-009]] (plan handle `DSK-10-09`) is titled "Desktop diagnostics: bounded redacted rolling logs,
session and correlation ids, exportable bundle", sits in area `platform-operations`, group
`EPIC-011` / `HZN-002`, and has no documents.

> **[[FND-036]] builds it; [[PLAT-009]] hardens and audits it.** This ticket owns
> `src/Pegasus.Desktop.Infrastructure/Diagnostics/**`, the three unhandled-exception handlers in
> `App.xaml.cs`, and the Diagnostics section of the settings route. [[PLAT-009]] owns the
> security-and-observability review of that result — the tier-9 posture question of whether the logs,
> ids and bundle actually meet proposal § 18.1 in practice.
>
> **The board already records this and it is not a judgement call**: this ticket's `blocks` array
> lists `PLAT-009`, so FND-036 is the prerequisite, not the duplicate. Plan 02 § 3 decision 10 says
> the same in prose — the bundle is "a foundation feature, not a Phase 8 afterthought".
>
> **This is a two-sided agreement and one side is written here.** Before writing code, confirm
> [[PLAT-009]] has not been taken, and record the same split in its plan. If it has already been
> taken and started, **stop and reconcile with its holder** rather than building the bundle twice —
> the Guardrails say "do not build the bundle twice", and a merge conflict is the good outcome.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-036` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — **ADR-0109** (desktop diagnostics bundle plus the existing Application Insights; no
> new telemetry fleet) is the ADR this ticket implements almost line for line: it is why the bundle
> exists, why nothing is uploaded, and why the correlation id is the join to gateway-side telemetry
> rather than a desktop collector. It is authored by [[FND-006]] (plan handle `DSK-00-06`).
> **ADR-0104** (online-required; bounded local cache only) bounds what may sit on the workstation and
> is why step 8's retention cap is a requirement rather than tidiness; it has two claimants —
> [[FND-005]] (plan handle `DSK-00-05`) and [[FND-026]] (plan handle `DSK-02-01`) — see [[FND-026]]'s
> plan for the ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 10; if either ADR lands
> differently this plan is revised before implementation.

Because `refs` is empty, the authorities that actually bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 16.3 Crash recovery | Crash handling **never** swallows exceptions and continues in a corrupted state; a bundle can be exported by the user or administrator | Steps 5, 7 — and § Out of scope, which refuses `e.Handled = true` |
| Proposal § 18.1 Desktop diagnostics | Structured rolling local logs, per-launch session identifier, API correlation identifiers, **redaction by default**, **bounded size and retention**, app/Windows/package/dependency versions | Steps 2, 3, 4, 8 |
| Proposal § 18.2 Central telemetry | No collector fleet | § Out of scope — nothing is uploaded and no OpenTelemetry collector is added |
| Plan 02 § 3 decision 10 | The bundle is a foundation feature, not a Phase 8 afterthought: redacted rolling logs, app/package/Windows/dependency versions, last compatibility response, and the single-instance/activation log | Steps 2, 3 |
| Plan 02 § 4 exit-gate table | "Diagnostics bundle exports — bundle zip contains the documented manifest (tier 9)" | § Verification; [[FND-041]] (plan handle `DSK-02-16`) consumes it |
| `screen-specs.md:116-125` § Diagnostics and settings | Route: user menu → Diagnostics; "Export diagnostics bundle — **primary**"; "Open logs folder"; **"Sections render only when populated"**; Developer is non-production only | Step 7 |
| `screen-specs.md:124-125` | AutomationIds `Settings.ExportDiagnostics` and `Settings.OpenLogs`, verbatim | Step 7 |
| `screen-specs.md:31-39` | The repository-wide AutomationId convention and its 100 % coverage audit | Step 7 |
| `docs/design/README.md` § No explanatory copy and page economy (`:422`) | Labels and values; at most one consequence sentence | Step 7 — the export confirmation is **one sentence** naming the path |
| **ADR-0104** (via `docs/desktop/README.md` and the § 3 ADR table) | Bounded local state | Step 8's count and size caps |
| `AGENTS.md` § Simplicity rails — one list per concept | A redaction rule lives in exactly one place | Step 4 calls [[FND-031]] (plan handle `DSK-02-06`)'s hook; it does not re-implement the regex set |
| `docs/engineering.md` § Plan sizing (`:201`) | Diff estimate first, from a measured inventory | The estimate above |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 9 | "role matrix, secure cookies, … **redaction**, and bounded failure metrics" — demonstrated, not asserted | § Verification V3 and V4 |
| **L-04** (locked) | Every ticket names its subagent, skills and MCP tools | § Routing below |
| **C-01** (constraint) | The repositories become private; Actions minutes stop being free | This ticket adds no CI job — [[FND-040]] (plan handle `DSK-02-15`) owns the lane |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search` for `Application.UnhandledException` WinUI
  semantics, `Package.Current.Id`, `ZipFile.CreateFromDirectory`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve implementation steps: same order, same ownership, same file
paths, adding the *how* the body leaves out.

1. **Orient, and settle ownership first.** Read plan 02 § 3 decision 10 and § 4's exit-gate table,
   `docs/desktop/06-ui-design/screen-specs.md:116-125` § Diagnostics and settings, and
   `src/Pegasus.Core/Eva/EvaBundleSchema.cs` (the precedent). Confirm [[PLAT-009]] has not been taken,
   apply the reconciliation recorded in § Approach, and record the same split in [[PLAT-009]]'s plan.
   Confirm [[FND-031]], [[FND-032]] and [[FND-035]] (plan handle `DSK-02-10`) have landed — the
   writer, the host and session identifier, and the activation log. Then `get_doc_gates FND-036` and
   `take_ticket` on branch `task/desktop-diagnostics-bundle` from `origin/dev`.
2. **Define the manifest, and write the schema into this plan as well as into code.**
   `src/Pegasus.Desktop.Infrastructure/Diagnostics/DiagnosticsBundleManifest.cs` with a named
   `SchemaVersion` constant (follow `EvaBundleSchema.cs:523`'s form, e.g.
   `"pegasus-desktop-diagnostics-v1"`) and fixed entry-name constants (`:524-525`'s form). Fields:
   app version; package identity `FamilyName` / `Name` / `Publisher` / `Version`; Windows version;
   Windows App SDK and dependency versions; channel; per-launch session identifier; bundle creation
   timestamp; reason (`crash` \| `user-export`); and — added by this plan — a `truncated` flag with
   the reason, so step 6's bound is visible in the artefact rather than silent. Writing the schema
   into the plan is what lets the schema test and the support runbook be checked against each other;
   a schema that lives only in a `.cs` file cannot be.
3. **Implement `DiagnosticsBundleBuilder`.** It collects the manifest, the redacted rolling log files
   from [[FND-031]]'s writer, the last compatibility response from the bounded cache, and the
   activation log from [[FND-035]] — then writes a zip through a single `WriteEntry`-style helper
   (`EvaBundleSchema.cs:823-825`). **The allowed-contents list is closed**: no attachment content, no
   case data, no credentials (proposal § 18.1). Choose a bundle file name carrying **no** case
   reference, operator name or VRM — [[PLAT-007]] (plan handle `DSK-10-07`) owns "no PII in file
   names" and inherits whatever is chosen here.
4. **Re-apply redaction at collection, not only at write.** Run [[FND-031]]'s redaction hook over
   every file copied into the bundle, so a log written *before* a redaction-rule fix cannot leak
   through an export made after it. Call the hook; do **not** re-implement the regex set —
   `grep -rln "Redact\|redact" src/ --include=*.cs` returns nothing today, so there is exactly one
   implementation and it must stay that way.
5. **Register the three handlers** in `src/Pegasus.Desktop/App.xaml.cs`:
   `Application.Current.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException` and
   `TaskScheduler.UnobservedTaskException`. Each writes a `crash` bundle, flushes the log sink, then
   **exits the process**. **Do not set `e.Handled = true` and continue** — continuing in a corrupted
   state is explicitly forbidden by proposal § 16.3, and it is the single failure this step exists to
   prevent. If `microsoft_docs_search` shows one source is unreachable in a packaged WinUI app,
   register it anyway and record that it was never observed firing, rather than removing a handler on
   an assumption (A-FND036-1).
6. **Bound the crash path.** The handler completes within a short **explicit** timeout and **must
   never itself throw**: wrap the bundle write in a try/catch whose fallback is a single plain-text
   line to the log directory. Read package identity from the value cached at host build (step 1's
   prerequisite), not by calling into WinRT while unwinding. Where the timeout truncates collection,
   set the manifest's `truncated` flag rather than producing a silently short bundle.
7. **Add the Diagnostics section commands.** "Export diagnostics" with
   `AutomationProperties.AutomationId="Settings.ExportDiagnostics"` — **primary** in that section per
   `screen-specs.md:122` — and "Open logs folder" (`Settings.OpenLogs`). Export writes the same bundle
   with reason `user-export` and shows the operator the produced path in **one sentence**, no
   explanatory copy (`docs/design/README.md:422`). Honour `screen-specs.md:118`'s "**Sections render
   only when populated**": the Diagnostics section appears because these two commands exist, and no
   empty section is added for Preferences, About or Developer — those belong to other tickets.
8. **Bound retention.** Bundles are written under the packaged app's local folder with a maximum
   **count** and a maximum **total size**, oldest deleted first. Take these defaults and say in the
   plan that they were taken: **5 bundles or 50 MB, whichever is reached first.** The body requires
   the bounds to be explicit but names no numbers, and `docs/engineering.md` § Plan sizing prefers a
   stated default to a deferred decision. Prune on **every** write, including the crash path — a crash
   loop writes one bundle per crash, and pruning only on export lets the mechanism fill the disk
   (A-FND036-5), which is the failure ADR-0104 and proposal § 18.1's "bounded size and retention"
   exist to prevent.
9. **Write the schema test** in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle
   `DSK-02-13`): build a bundle from a fixture directory, open the zip, assert the manifest parses and
   carries **every** required field, and assert the archive contains the log and activation-log
   entries **and nothing else**. The "and nothing else" half is the one that matters: "contains the
   logs" is satisfiable by a bundle that also contains a case attachment, and that distinction is what
   separates a tier-9 claim from a tier-1 one.
10. **Write the fault-injection and redaction tests.** Register the handlers against a fake
    application surface, raise an unhandled exception, and assert a `crash` bundle was written **and**
    that the process-exit action was invoked **exactly once** (twice would mean a double-exit path;
    zero would mean the app continued). Separately, plant a fake bearer token in a fixture log and
    assert it is absent from the built bundle **while the surrounding message survives** — redaction
    that eats the whole line is not redaction.
11. **Prove it on a real launch.** Run the packaged app via `winapp run` (never the packaged `.exe`
    directly), trigger the export command, and check the zip contents against the documented manifest.
    Then add a **temporary, debug-only** command that raises a deliberate unhandled exception, confirm
    a crash bundle appears and the app exits, measure how long the crash-bundle write took against the
    step-6 timeout (A-FND036-4), and **remove the temporary command before the PR**. A shipped debug
    crash command is a defect the Guardrails name.
12. **Document, simplify, open the PR.** Add the support entry to `docs/runbook.md` near
    § Monitoring and diagnosis (`:881`): how an operator exports a bundle and what it contains.
    Coordinate with area 09's runbooks and [[FND-049]] (plan handle `DSK-04-13`) so the instruction
    lives **once**; record in the plan which document it landed in. Then run
    `dotnet build ./Pegasus.slnx --configuration Release`, run the simplification pass over this
    branch's own diff, record it under a dated `## Simplification pass` heading in this document, and
    open the PR into `dev`.

## Verification

Evidence tier **9 — Security/observability** (`docs/engineering.md` § Required evidence tiers, `:72`),
as the ticket body states: this obliges **demonstrated** redaction, correlation, bounded failure
behaviour and the **absence** of credential material in the exported artefact — not merely a file
that exists. `docs/runbook.md:889-891` says the same thing in the repository's own voice: "Local
telemetry must be content-safe and prove correlation, attributes, health, and redaction."

The `proof` document is produced from these five outputs.

- **V1.** `dotnet build ./Pegasus.slnx --configuration Release` — expected exit 0 and
  `0 Warning(s)`. The authoritative gate: it is what `.github/actions/dotnet-build/action.yml:22-27`
  runs and, unlike `BuildAndRun.ps1`, it sees the repository-root `Directory.Build.props`.
- **V2.** `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~Diagnostics"`
  — expected: the schema test (manifest parses, every field present, **and nothing else** in the
  archive), the fault-injection test (crash bundle written, exit action invoked exactly once), the
  redaction test (planted token absent, surrounding message present), and the retention test (writing
  past 5 bundles / 50 MB leaves exactly the bound, oldest deleted first).
- **V3.** **The manual export**, from the running packaged app: the produced path as the UI reported
  it, the zip's entry listing, and the manifest's JSON pasted in full. Check the entry listing against
  the allowed-contents list line by line, not at a glance.
- **V4.** **The planted-token check on a real extracted bundle**:
  `Select-String -Path <extracted bundle>\* -Pattern 'Bearer '` — expected **no matches**. Widen it to
  `-Pattern 'Bearer |refresh_token|password'` and paste the empty result. This is the tier-9
  obligation to demonstrate absence, and it is not satisfied by the unit test alone — the unit test
  proves the builder redacts a fixture; this proves the shipped bundle from a real session carries
  nothing.
- **V5.** **The crash demonstration**: the temporary fault-injection command's output, the crash
  bundle that appeared, its manifest showing `reason: crash`, the measured write duration against the
  step-6 timeout, and confirmation that the temporary command was removed from the diff
  (`git diff` showing its absence).

**Honesty clauses for the proof.**

- Say which [[PLAT-009]] case applied — not taken and the split recorded, or taken and reconciled with
  its holder.
- If any of the three exception sources was **never observed firing**, say so and say it was
  registered anyway. "Registered" and "proven to fire" are different claims and tier 9 cares about the
  difference.
- Report the measured crash-bundle write duration and whether the `truncated` flag was ever set. If
  the bound truncated the bundle, that is a real limitation and belongs in the proof, not in a
  comment.
- Say whether the export evidence came from a real packaged launch or a fixture. Only the former
  proves package identity resolved.
- A green `BuildAndRun.ps1` is **not** the same claim as a green `dotnet build`: the script injects a
  project-level `Directory.Build.props` (`.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172`,
  its existence test at `:152` against the project directory only) that shadows the root one and drops
  `TreatWarningsAsErrors`. V1 is authoritative.
- No CI job builds a desktop project until [[FND-040]] lands, so a green `repository-check` run says
  nothing about this ticket.

## Risks / open questions

- **Risk — A-FND036-2: a bundle may not be writable from inside a crash handler.** The process may
  already have broken file I/O, allocation or the log sink. This is the assumption the whole ticket
  rests on and it is not obviously true. *Mitigation*: step 6's try/catch fallback to a single
  plain-text line, and step 11's real deliberate crash as the only honest test. *If wrong*: the crash
  path records less than the export path, and that is recorded rather than hidden.
- **Risk — A-FND036-4: the timeout and the re-redaction pass conflict on a large log.**
  *Mitigation*: the bound wins, the manifest's `truncated` flag records it, and step 11 measures the
  duration. A hung crash handler is indistinguishable from a hung application, which is the worse
  failure by a wide margin.
- **Risk — A-FND036-3: reading `Package.Current.Id` during a crash.** Calling into WinRT while the
  process unwinds can hang. *Mitigation*: cache package identity at host build ([[FND-032]]) and read
  the cached value — safer regardless of whether the risk materialises.
- **Risk — A-FND036-5: a crash loop defeats the retention bound.** If pruning runs only on export,
  each crash adds a bundle. *Mitigation*: step 8 prunes on **every** write including the crash path,
  and V2's retention test proves the bound.
- **Risk — the "and nothing else" assertion is omitted.** A schema test that only checks required
  entries passes on a bundle that also carries a case attachment. *Mitigation*: step 9 states the
  closed-set assertion explicitly and V3 checks the entry listing line by line. This is the difference
  between a tier-9 and a tier-1 claim.
- **Risk — redaction that eats the message.** A rule aggressive enough to remove every token can also
  remove the surrounding text, producing bundles that are safe and useless. *Mitigation*: step 10's
  test asserts the surrounding message **survives**.
- **Risk — the temporary fault-injection command ships.** *Mitigation*: step 11 requires its removal
  and V5 requires `git diff` evidence of the removal. The Guardrails name it as a trap.
- **Risk — two bundles get built.** [[PLAT-009]] names the same deliverable. *Mitigation*: the
  reconciliation in § Approach, applied at step 1 before any code, backed by the board's own `blocks`
  edge. If [[PLAT-009]] is already taken and started, stop and reconcile with its holder. This is a
  scope boundary with a named sibling ticket that the ticket body directs to be settled in this plan —
  it is settled here, not opened as a question.
- **Risk — the runbook instruction gets written twice.** [[FND-049]] documents the operator-facing
  side and area 09 owns release runbooks. *Mitigation*: step 12 records which document it landed in;
  one instruction, one place (`AGENTS.md` § Simplicity rails).
- **Sequencing, recorded not resolved — [[FND-031]], [[FND-032]], [[FND-035]] and [[FND-033]] (plan
  handle `DSK-02-08`) must all have landed.** The plan's dependency arrow names only [[FND-032]], but
  the writer, the activation log and the settings route reached from the user menu come from the
  other three. [[FND-038]] must land before steps 9–10.
- **Scope boundary, not an open question — draft checkpointing and draft recovery.** Proposal
  § 16.3's draft clauses belong to area 05 and [[PLAT-017]] (plan handle `DSK-10-17`). This ticket
  writes a bundle and exits; it does not try to save the operator's work.
- **Scope boundary, not an open question — uploading anything.** ADR-0109 and proposal § 18.2 keep
  central telemetry on the gateway side; adding an OpenTelemetry collector is refused by the
  Guardrails. The correlation id from [[FND-031]]'s request handler is the join to gateway telemetry,
  and it is deliberately the *only* one.
- **Defaults taken rather than asked.** The retention bounds — **5 bundles or 50 MB, whichever is
  reached first** — and the crash-path timeout are chosen here because the body requires them to be
  explicit and bounded but names no numbers. `docs/engineering.md` § Plan sizing prefers a stated
  default to a deferred decision, and [[PLAT-009]] can revise them in its hardening pass.
- **No `open-questions` document is opened on this ticket.** The body does not instruct one; nothing
  here is unsettled in a way that must be answered from outside before implementation begins. Every
  assumption names the command inside the ticket that settles it, the one overlap is settled by the
  board's own `blocks` edge, and no settled operator decision (D-002, D-003, D-004, the Send-to-AI
  exclusion) is reopened.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
