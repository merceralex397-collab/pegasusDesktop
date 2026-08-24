# Files — FND-006

Surveyed 2026-08-24 against the working tree at `origin/main` `191ddf3342…`.
Every path was confirmed with `ls`, `grep -n` or `sed -n`; the four ADR files
are new and are marked as such.

## Where the change lands

| Path | Why |
| --- | --- |
| `docs/adr/0102-existing-pegasus-credentials-token-session.md` | **New.** Existing Pegasus credentials and identity store; desktop session = short-lived access token plus rotated refresh token. **The filename is not a free choice**: it is the only ADR-0102 path the plan set names (`docs/desktop/04-auth-session-update-and-startup/README.md:296`) and the path [[FND-042]] (plan handle `DSK-04-01`), [[GWY-019]] (`DSK-04-02`), [[GWY-020]] (`DSK-04-03`), [[GWY-021]] (`DSK-04-04`) and [[GWY-022]] (`DSK-04-05`) already name as the file they author or extend. Co-claimed by [[FND-042]] — one number, one file |
| `docs/adr/0106-graph-intake-worker-stays-central.md` | **New.** The Graph intake worker stays central and unattended; relates ADR-0024. Must state that intake continues with every desktop closed |
| `docs/adr/0107-provider-credentials-behind-the-gateway.md` | **New.** Box and DVLA/DVSA credentials stay behind the gateway. Must state that **no long-lived provider secret is ever placed in the MSIX package or on a workstation** |
| `docs/adr/0109-desktop-diagnostics-bundle-and-existing-app-insights.md` | **New.** Desktop diagnostics bundle beside the existing Application Insights; no new telemetry fleet. Relates **upstream PLAT-034** — written that way, never bare |
| `docs/adr/README.md` | 4 new rows in the `## Current architecture decisions (`status: accepted`)` table (heading `:16`, header `:18-19`), in ID order, **three cells each**: `ADR \| Title \| Related FRD`. Do not touch the `## Superseded and relocated` table at `:43-52` |

Nothing else in the repository is edited. In particular `AGENTS.md` is **not**
touched here — see Out of scope.

## Context files

What the implementer must read to avoid a trap, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `docs/desktop/01-inventory-and-parity/flow-records.md:1-20` | That the six records are **pre-filled** from the 2026-08-23 inspection at `191ddf33`, that `DSK-01-06` and `DSK-01-07` complete them, and — at `:7-8` — the closure rule this ticket gates on: "A record is closed when every open question has a code citation or a line in `docs/open-decisions.md`" |
| `flow-records.md:21-105` (record 1) | The whole of ADR-0102's Context: Identity registration (`Program.cs:262-274`), throttle-not-lockout sign-in (`SignIn.cshtml.cs:63`), per-request `IsEnabled` re-check (`Program.cs:353`, `:368-457`), the 12-right fail-closed matrix, OpenIddict 7.6 with **one** seeded Automation client, and the Data Protection ring blob. Its § "What the desktop needs" at `:79-86` is the decision ADR-0102 records |
| `flow-records.md:90-99` | Q1.1–Q1.4 — the four questions that must carry a citation or an `open-decisions.md` line before ADR-0102 can claim the boundary is settled |
| `flow-records.md:170-241` (record 3) | ADR-0106's Context: the timer functions and their schedule setting, the 1,125-line Graph adapter, the per-mailbox lease/cursor/failure-code model, and the queue names declared in `infra/modules/platform.bicep:129-152`. Q3.1–Q3.3 at `:227-234` |
| `flow-records.md:242-309` (record 4) | ADR-0107's Box half: where `Box__ConfigJson` and `Box__ClientSecret` actually live (Key Vault references on the Worker, Container App secrets on the Web — `platform.bicep:382-398`, `:555-563`), and that an unresolved `@Microsoft.KeyVault(` placeholder fails with a named error (`BoxCaseCustody.cs:82-84`). Q4.1–Q4.3 at `:296-303` — Q4.1 (can the Box SDK issue short-lived constrained tokens) is the one whose answer could change the ADR's content |
| `flow-records.md:310-361` (record 5) | ADR-0107's vehicle half: `DvlaDvsaProductionAdapter.cs` (412 lines), the Worker-bound `Dvla__ApiKey` / `Dvsa__*` secrets, and that the Worker owns the live adapter while the Web records the request. Q5.1–Q5.3 at `:350-354` — Q5.1 (does the provider contract allow a direct native-client call) is the other content-changing one |
| `src/Pegasus.Web/Program.cs:196` | `builder.Services.AddApplicationInsightsTelemetry()` — half of ADR-0109's evidence. There is **no flow record** for telemetry, so this is where its Context comes from |
| `src/Pegasus.Worker/Program.cs:14-15` | `.AddApplicationInsightsTelemetryWorkerService().ConfigureFunctionsApplicationInsights()` — the other half |
| `docs/desktop/01-inventory-and-parity/azure-resource-register.md:36` | That the Log Analytics workspace is `PerGB2018`, 31-day retention, **capped at 0.1 GB/day**, and that the cap "blinds working-hour queries (PLAT-034)". This is ADR-0109's "measured operational advantage" evidence — and it points *against* centralising more |
| `docs/current-architecture.md:175` | Records upstream PLAT-034 as open. Confirms the id is upstream's, not the board's |
| Kanmer group document `HZN-001/board-conventions.md` § *Upstream ids versus board ids* | The absolute rule: a bare `<PREFIX>-<nnn>` is a **fork board id**; an upstream id is always `upstream <ID>`. The board's `platform-operations` area tops out at `PLAT-029`, so a bare `PLAT-034` in ADR-0109 would point at nothing on the board and at a live conversion ticket in the reader's head |
| `AGENTS.md:77-113` | ADR conventions and the frontmatter block. **The range stops at `:113` deliberately** — the bullet at `:114-117` describes a five-column index that `docs/adr/README.md:18-19` contradicts, and it is not authority for the index rows. [[FND-005]] (plan handle `DSK-00-05`) owns that correction |
| `docs/adr/README.md:16-41` | The real index shape, three cells, and the related accepted decisions ADR-0004, ADR-0011, ADR-0024 and ADR-0027 that these four relate to |
| `docs/adr/0029-image-initiated-case-projection.md:1-30` | The current house form to copy: frontmatter, `# ADR-00NN: <title>`, `## Status` opening with "Accepted.", then `## Context`. Also shows `related_frd: [frd-01, frd-02, …]` — **lowercase stems**, which is the repository-wide convention |
| `docs/adr/0028-run-integrated-renderer-in-web-container-app.md` | 84 lines with the full heading set including `## Options considered` and `## Links` — the length and shape to land near |
| `docs/adr/0024-stable-approved-mailbox-identity-and-explicit-baseline.md` | The decision ADR-0106 relates to; read it so the relation is stated accurately rather than asserted |
| `docs/open-decisions.md` | Exists. The alternative destination for a flow-record question that cannot be answered with a code citation — step 2's escape hatch, and the only honest one |
| `.github/workflows/ci.yml:71-87` | The `documentation` job that runs on every change set — `Test-TestMarkdownPlacement.ps1` at `:84`, `Test-DocumentationLinks.ps1` at `:87` |
| `.codex/agents/pegasus-parity-researcher.toml` | The read-only subagent this ticket routes evidence gathering to. It **cannot write files**; its answer is pasted into the ADR by the ticket owner |

## Ripple effects

- **`docs/adr/README.md`** gains four rows; it is the only ADR index in the
  repository. `scripts/Test-DocumentationLinks.ps1` resolves each new relative
  link and fails the CI `documentation` job if a filename and its row disagree.
- **`docs_todo` on other tickets.** Step 11 clears it for the area 04 auth
  tickets (ADR-0102), the area 07 integration tickets (ADR-0106, ADR-0107) and
  the area 10 observability tickets (ADR-0109). `docs_todo: true` is what
  satisfies `leave-backlog` for every `feature` ticket, so `link_doc` first and
  clear second — never the other way round.
- **Sibling ADR tickets edit the same index table.** [[FND-005]] adds six rows,
  [[FND-007]] adds none (its ADR merges `proposed`), [[FND-042]], [[REL-001]],
  [[FND-026]] and [[TOOL-008]] add their own. Whoever merges second rebases.
- **Area 04, 07 and 10 plans cite these ADRs as settled authority.** Once merged,
  [[GWY-019]]…[[GWY-022]] and [[FND-043]] build the token path *to* ADR-0102; a
  later change to the decision costs a superseding ADR, not an edit.
- **No test, no contract, no generated client.** This change touches no code;
  `openapi/pegasus-v1.json` and the generated client are unaffected. Say so in
  the post-implementation report so a reviewer does not go looking.

## Out of scope

- **`AGENTS.md`** — not edited here at all, including the wrong index-shape
  sentence at `:114-117`. [[FND-005]] owns that one-line correction; this
  ticket's own body draws its citation range to `:113` for exactly that reason.
- **ADR-0100, 0101, 0103, 0104, 0105, 0110** — [[FND-005]]'s six.
- **ADR-0108** — [[FND-007]] authors it `proposed`; [[FEAT-038]] (plan handle
  `DSK-07-12`) flips it to `accepted`.
- **`docs/frd/`, `docs/prd/`, `docs/capabilities.md`** — [[FND-008]] (plan handle
  `DSK-00-08`).
- **`src/`** — no code. This ticket cites `Program.cs`, `GraphApprovedSources.cs`,
  `BoxCaseCustody.cs` and `DvlaDvsaProductionAdapter.cs`; it edits none of them.
- **`docs/desktop/01-inventory-and-parity/flow-records.md`** — read and gated on,
  never edited. [[FND-019]] and [[FND-020]] own it.
- **Azure** — no write, **and no read**. Every Azure fact cited here comes from
  the read-only register at
  `docs/desktop/01-inventory-and-parity/azure-resource-register.md` and from
  `infra/modules/platform.bicep`, not from a live call made by this ticket.
- **A new telemetry service, collector or workspace** — ADR-0109's whole point is
  that none is added; proposing one here would contradict the decision being
  written.
