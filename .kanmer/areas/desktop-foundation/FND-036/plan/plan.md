# Plan — FND-036: Unhandled-exception path and the exportable diagnostics bundle

**Diff estimate: ~7 files, ~400 lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the files
document: `Diagnostics/DiagnosticsBundleManifest.cs` ~55; `Diagnostics/DiagnosticsBundleBuilder.cs`
~165 (collection, redaction re-run, zip, retention); `App.xaml.cs` +70 (three handlers, the bounded
crash path and its fallback); the Diagnostics settings section ~60 (two commands plus the one-sentence
path report); `docs/runbook.md` ~+18. The two tests land in `tests/Pegasus.Desktop.ViewModelTests` and
are counted against that project; the temporary fault-injection command used at step 11 is removed
before the PR and is not in the diff.

## Approach

Treat the bundle as a **closed list** and the crash path as code that must not fail, then prove both
with tests rather than with intentions. The closed list is the design: the archive contains the
manifest, the redacted rolling logs, the last compatibility response and the activation log, and
"nothing outside that list may enter" (proposal § 18.1). The rejected alternative — a "collect
everything under the local folder and let redaction sort it out" builder — is easier to write, would
survive a schema test that only checks for required entries, and is exactly how attachment content or
a cached document reaches a support e-mail. So the schema test asserts the archive contains the
allowed entries **and nothing else**.

The crash path is written defensively because it runs when nothing else can be trusted: a short
explicit timeout, a handler that cannot throw, and a fallback of one plain-text line. A crash handler
that hangs is indistinguishable from a hung application — which is the failure the operator is already
trying to report.

**The overlap with [[PLAT-009]] (plan handle `DSK-10-09`) needs no negotiation: its own body settles
it.** [[PLAT-009]]'s § Source of truth reads "the first bundle export and unhandled-exception handler
from `DSK-02-11` — **this ticket completes them rather than starting again**", and its steps 8 and 10
say "extend the export from `DSK-02-11`". So **this ticket owns the first bundle, the manifest schema
and the crash path**; [[PLAT-009]] later adds the shared secret/PII pattern list, the fuller version
block, the last-N API failures section and the fuller runbook procedure. `schemaVersion` (step 2) is
the mechanism that lets it do so without breaking a bundle already sent to support. The ticket body's
instruction to "agree one owner in the ticket plan before writing code" is discharged here, by
recording the sibling's own words rather than by re-deciding.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-036` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0109 (desktop diagnostics bundle plus the **existing** Application Insights; no
> new telemetry fleet), authored by [[FND-006]] (plan handle `DSK-00-06`). Every step below is written
> to it: the bundle is local, nothing is uploaded, and no collector is added. ADR-0104
> (online-required; bounded local state only) bounds retention and is authored by [[FND-005]] (plan
> handle `DSK-00-05`), also claimed by [[FND-026]] (plan handle `DSK-02-01`) — see [[FND-026]]'s plan
> for the ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table); if either lands differently
> this plan is revised before implementation.

Because `refs` is empty, these are the authorities that actually bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 16.3 Crash recovery | Crash handling never swallows exceptions and continues in a corrupted state; a bundle can be exported by the user or administrator | Steps 5, 6, 7 |
| Proposal § 18.1 Desktop diagnostics | Structured rolling local logs, per-launch session identifier, API correlation identifiers, redaction by default, bounded size and retention, and app/Windows/package/dependency versions | Steps 2, 3, 4, 8 |
| Proposal § 18.2 Central telemetry | No collector fleet | Nothing is uploaded; `configuring-opentelemetry-dotnet` stays on the do-not-load list |
| Plan 02 § 3 decision 10 | The bundle is a **foundation** feature, not a Phase 8 afterthought, and lists its contents | Steps 2, 3 |
| Plan 02 § 4 exit-gate table | "Diagnostics bundle exports — bundle zip contains the documented manifest (tier 9)" | § Verification |
| `docs/desktop/06-ui-design/screen-specs.md` § Diagnostics and settings | The route, the four sections (Developer non-production only) and the AutomationIds `Settings.ExportDiagnostics`, `Settings.OpenLogs` | Step 7 |
| `docs/design/README.md:169` | One sentence beside a consequential control; no explanatory copy | Step 7 |
| `docs/design/README.md:172` | A capability not composed is absent, never disabled | Step 7 — the Developer section |
| **Measured production evidence** — `docs/operations.md:362-369`, `docs/current-architecture.md:160-177` | The Log Analytics workspace's 0.1 GB/day quota resets at 03:00Z and is exhausted within hours, so "every check run in a UK working hour comes back empty"; two custody failures left no trace; alert rules are blind for the same window | The reason the bundle must be *sufficient*, which is what steps 9–11 test rather than assert |
| `AGENTS.md` § Simplicity rails | One list per concept | Step 4 re-applies [[FND-031]]'s redaction hook rather than writing a second rule set |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 9 | Demonstrated redaction, correlation, bounded failure behaviour and the **absence** of credential material in the exported artefact — "not merely a file that exists" | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`).
  **Do not load `configuring-opentelemetry-dotnet`** — it is on the do-not-load table
  (`docs/desktop/12-agent-tooling/skill-routing.md` § "Not applicable — do not load") and a collector
  fleet contradicts ADR-0109.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search` for `Application.UnhandledException` WinUI
  semantics, `Package.Current.Id`, `ZipFile.CreateFromDirectory`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve steps: same order, same ownership, same paths.

1. **Orient.** Read `docs/desktop/06-ui-design/screen-specs.md` § Diagnostics and settings, and read
   `docs/operations.md:362-369` and `docs/current-architecture.md:160-177` so the quota gap is
   understood as the **reason** for this ticket rather than background. Then `get_doc_gates FND-036`
   and `take_ticket` on branch `task/desktop-diagnostics-bundle` from `origin/dev`.
   Record here, before writing code, the [[PLAT-009]] ownership reconciliation quoted in § Approach —
   the ticket body requires the agreement to be in the plan.
2. **Define the manifest as a versioned schema** in
   `src/Pegasus.Desktop.Infrastructure/Diagnostics/DiagnosticsBundleManifest.cs`, with
   `schemaVersion` and exactly these fields: app version; package identity
   (`Package.Current.Id.FamilyName`, `Name`, `Publisher`, `Version`); Windows version; Windows App SDK
   and dependency versions; channel; per-launch session identifier; bundle creation timestamp; reason
   (`crash` | `user-export`). **Write the schema into this plan document too**, so the schema test and
   the support runbook agree with one another and with [[PLAT-009]], which extends it. Package
   identity must be *tolerated as absent* rather than throwing — a crash-path builder that throws is
   forbidden by step 6.
3. **`DiagnosticsBundleBuilder`** in the same folder: collect the manifest, the redacted rolling logs
   from [[FND-031]]'s (plan handle `DSK-02-06`) writer, the last compatibility response held in the
   bounded cache, and the single-instance/activation log from [[FND-035]] (plan handle `DSK-02-10`),
   then write a zip. **Nothing outside that list may enter** — no attachment content, no case data, no
   credentials (proposal § 18.1). Implement it as an explicit allow-list of sources, not as a
   directory sweep with exclusions: a sweep passes a test that only checks required entries are
   present.
4. **Re-apply redaction at collection, not only at write.** Run [[FND-031]]'s **existing** redaction
   hook over every file copied into the bundle, so a log written before a redaction-rule fix cannot
   leak. Reuse that processor; do not write a second rule set here — one list per concept
   (`AGENTS.md` § Simplicity rails), and in security code a second copy drifts silently.
5. **Register the three handlers** in `src/Pegasus.Desktop/App.xaml.cs`:
   `Application.Current.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException` and
   `TaskScheduler.UnobservedTaskException`. Each writes a `crash` bundle, flushes the log sink, then
   exits the process. **Do not set `e.Handled = true` and continue** — continuing in a corrupted state
   is explicitly forbidden by proposal § 16.3. Note while implementing that the three do not offer the
   same guarantees: `AppDomain.CurrentDomain.UnhandledException` runs during teardown and
   `TaskScheduler.UnobservedTaskException` is raised on finalisation, so the proof must say which were
   demonstrated and which were only registered.
6. **Bound the crash path.** The handler completes within a short, explicit timeout and **must never
   itself throw**: wrap the bundle write in a try/catch whose fallback is a single plain-text line to
   the log directory. A crash handler that hangs looks identical to a hung application.
7. **Add the commands to the Diagnostics section of the settings route**: "Export diagnostics" with
   `AutomationProperties.AutomationId="Settings.ExportDiagnostics"` and "Open logs folder" with
   `Settings.OpenLogs` (`screen-specs.md` § Diagnostics and settings). The export writes the same
   bundle with reason `user-export` and shows the operator the produced path — **one sentence**, no
   explanatory copy (`docs/design/README.md:169`). Sections render only when populated, and the
   Developer section is non-production only (`docs/design/README.md:172`: absent, never disabled).
8. **Bound retention.** Bundles are written under the packaged app's local folder with a maximum count
   **and** a maximum total size, oldest deleted first, so the bundle mechanism cannot itself fill a
   workstation disk (ADR-0104; proposal § 18.1 "bounded size and retention").
9. **Bundle schema test** in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle
   `DSK-02-13`): build a bundle from a fixture directory, open the zip, assert the manifest parses and
   carries every required field, assert the archive contains the log and activation-log entries **and
   nothing else**, and assert the total size is bounded. The "and nothing else" assertion is the one
   that catches a directory sweep.
10. **Fault-injection test**: register the handlers against a fake application surface, raise an
    unhandled exception, and assert a `crash` bundle was written and the process-exit action was
    invoked **exactly once**. Also assert a planted bearer token in a fixture log written **without**
    redaction is absent from the bundle — that is what proves step 4's re-run rather than step 4's
    intention. For `TaskScheduler.UnobservedTaskException`, force a collection and wait for pending
    finalizers; if it still cannot be made deterministic, say so in the proof rather than claiming all
    three handlers were demonstrated.
11. **Prove it on a real launch.** Run the packaged app, trigger the export command, and confirm the
    zip contents against the manifest. Then trigger a deliberate unhandled exception from a
    **temporary debug-only command**, confirm a crash bundle appears and the app exits, and **remove
    the temporary command before the PR** — a shipped "crash now" command is a defect and the ticket's
    Traps name it.
12. **Documentation and close.** Add the support entry to `docs/runbook.md` — where the logs live, how
    an operator exports a bundle, what it contains — written as the minimum that is true today so
    [[PLAT-009]] step 12 can extend it **in place** rather than adding a second heading ("coordinate
    with area 09 runbooks so the instruction lives once"). Run the simplification pass, record it under
    a dated heading below, and open the PR into `dev`.

## Verification

Evidence tier **9 — Security/observability** (`docs/engineering.md` § Required evidence tiers, `:72`),
as the ticket body states: demonstrated redaction, correlation, bounded failure behaviour and the
**absence** of credential material in the exported artefact — "not merely a file that exists".

The `proof` document is produced from these:

1. `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~Diagnostics"`
   — expected: schema, fault-injection and redaction tests pass. Name them individually, and state
   which of the three exception sources were **demonstrated** versus **registered only**.
2. **Manual export from the running packaged app** — expected: a zip whose manifest matches the
   documented schema and whose entries match the allowed list exactly. Paste the manifest and the
   archive entry listing.
3. **Planted-token check**: `Select-String -Path <extracted bundle>\* -Pattern 'Bearer '` — expected:
   no matches. Run it against a bundle built from a log that deliberately contained one.
4. Additionally, and not in the body — three checks that make acceptance criteria executable:
   - `grep -rn 'e.Handled' src/Pegasus.Desktop/App.xaml.cs` — expected: either no match, or a match
     that is provably setting it to `false`. `e.Handled = true` is a single-token defect with no
     compiler signal, so it deserves a grep rather than a review pass.
   - A **retention** demonstration: write more bundles than the count cap and show the oldest deleted,
     with the folder's total size under the cap.
   - `grep -rniE 'crash-?now|force-?crash|throw new .*TestException' src/Pegasus.Desktop/` after
     step 11 — expected: no matches, proving the temporary fault-injection command did not survive.
5. The **deliberate crash walkthrough**: the crash bundle produced, the app's exit, and the elapsed
   time of the handler against its stated timeout.
6. The measured bundle size, for [[PLAT-009]] and for the retention bound.

## Risks / open questions

- **Settled here, not an open question — the [[PLAT-009]] overlap.** [[PLAT-009]]'s own body says it
  "completes them rather than starting again" and its steps say "extend the export from `DSK-02-11`".
  This ticket owns the first bundle, the manifest schema and the crash path; [[PLAT-009]] adds the
  shared secret/PII pattern list ([[PLAT-001]], plan handle `DSK-10-01`), the fuller version block,
  the last-N API failures section and the fuller runbook procedure. `schemaVersion` makes that
  additive. Building the bundle twice is a stop condition for both tickets.
- **Risk — a directory sweep instead of an allow-list.** The single most likely way case data or
  attachment content reaches a support e-mail, and it passes any test that only checks required
  entries are present. *Mitigation*: step 3's explicit allow-list and step 9's "and nothing else"
  assertion.
- **Risk — the crash handler throws or hangs.** It runs when nothing else can be trusted, and a hang
  is indistinguishable from a hung application. *Mitigation*: step 6's explicit timeout, the
  try/catch, and the single plain-text fallback line; § Verification item 5 records the elapsed time.
- **Risk — `e.Handled = true`.** One token, no compiler signal, and it silently converts a crash into
  a corrupted-state continuation that proposal § 16.3 forbids. *Mitigation*: § Verification item 4's
  grep.
- **Risk — the temporary fault-injection command ships.** *Mitigation*: § Verification item 4's grep
  after step 11.
- **Risk — redaction is implemented twice.** [[FND-031]] defines the hook, [[FND-032]] (plan handle
  `DSK-02-07`) wires it into the sink, this ticket re-applies it at collection. *Mitigation*: step 4
  reuses the processor; a second regex set is refused.
- **Risk — a handler is registered but never demonstrated.**
  `TaskScheduler.UnobservedTaskException` fires on finalisation and
  `AppDomain.CurrentDomain.UnhandledException` runs during teardown. *Mitigation*: step 10 forces a
  collection and waits for finalizers, and § Verification item 1 requires the proof to say which were
  demonstrated — an undemonstrated handler recorded as demonstrated is the defect
  `docs/engineering.md` § Lessons calls "registration is not caller proof".
- **Risk — two runbook procedures.** [[PLAT-009]] step 12 writes a fuller one. *Mitigation*: step 12
  here writes the minimum that is true today, in the shape [[PLAT-009]] extends in place.
- **Scope boundary, not an open question — draft checkpointing and draft recovery.** Proposal
  § 16.3's draft clauses belong to area 05 and [[PLAT-017]] (plan handle `DSK-10-17`).
- **Scope boundary, not an open question — the `DSK-02` capability row.** [[FND-008]] (plan handle
  `DSK-00-08`) adds the `DSK` family.
- **No `open-questions` document is opened.** Every field of the manifest is named by proposal § 18.1
  and step 2; the bundle contents are a closed list; the redaction rule is reused; and the sibling
  overlap is settled by the sibling's own body. Nothing needs an answer from outside the ticket before
  implementation begins.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
