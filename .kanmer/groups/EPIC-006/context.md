# EPIC-006 — Area 05: implementation and migration (vertical slices)

Read this once before working any `DSK-05-nn` ticket. It carries what binds every
ticket in the epic; the ticket body carries the rest.

## What this area delivers

The route from the Razor Pages web application to the native desktop client: what is
reused, extracted, replaced and cut, and the twenty-two ordered vertical slices S1–S22
that carry every §13.1–13.10 capability across with parity evidence. Twenty-two slice
tickets (`DSK-05-01`…`DSK-05-22`) plus four cross-cutting tickets: the `OperatorLabels`
extraction (`-23`), the desktop edit-state rule (`-24`), parity-matrix maintenance
(`-25`) and the post-cutover cut list (`-26`). Board area `desktop-features` (FEAT).

Slices span phases 3 to 8, one horizon each: S1–S3 → HZN-004, S4–S8 → HZN-005,
S9–S13 → HZN-006, S14–S16 → HZN-007, S17–S18 → HZN-008, S19–S22 → HZN-009.
`-26` sits in HZN-011 (Phase 10).

## Proposal coverage

§3.1 (what "core" means), §4.1 (placement per slice), §5.3 (dependency direction),
§6.1 (fork controls), §13 (every §13.1–13.10 group owned by at least one slice;
§13.11 explicitly out of scope), §22.1 (characterization before refactoring),
§24 phases 3–8, §25 (each slice written in the twelve-section shape).

## What binds every ticket here

- **L-01** — the gateway is `Pegasus.Web` evolved in place: versioned `/api/v1` route
  groups beside the Razor Pages, same Container App, no new deployment unit.
- **L-02** — Test/UAT is the local production-mimicking stack (local gateway and
  Worker, Azurite, LocalDB or a SQL container, replay adapters). ADR-0014 stands.
  Asking for an Azure test resource is out of bounds.
- **L-03** — report rendering moves to an isolated non-UI WebView2 HTML→PDF path;
  the gateway renderer is retained until golden-file parity passes (ADR-0108).
- **L-04** — every ticket names its subagent, skills and MCP tools.
- **Deviation: `Pegasus.Core` is not split** into Domain and Application (proposal
  §5.4). Core already has zero package dependencies and transport-neutral actors;
  the split is ceremony. Recorded in ADR-0100.
- **Deviation: slices replace pages, they do not translate them.** Each slice is
  specified from the business capability, the Core use cases and the design
  authority; the Razor page model is behavioural evidence only.
- **Characterization before moving any rule.** A rule found only in a page model is
  moved into Core with a test *first*. A second implementation is a stop condition.
- **Web stays live until cutover.** No Razor page is removed before its parity row
  reaches `UAT passed`; `/api/v1` groups ship feature-gated beside it.
- **No Azure writes in this area.** Reads are free; the ⚠ items (compatibility
  setting, feed, signing, the renderer's Container App uplift) belong to areas 04,
  09 and 11.

## Exit gate and what proves it

Every §13.1–13.10 capability has a native screen backed by a gateway endpoint and a
Core use case, with its row in `docs/desktop/01-inventory-and-parity/parity-matrix.md`
at `UAT passed` or better and the Razor page it replaces marked `cut over`. Proof per
slice is the Kanmer `proof` document: commands run, test output, screenshots and the
parity-matrix row update. The per-phase gates are in the area plan §4; `DSK-05-25`
owns the matrix discipline and produces the Phase 9 completeness report.

## Routing for this area

| Need | Subagent (`.codex/agents/<name>.toml`) | Skills (pinned source) | MCP |
| --- | --- | --- | --- |
| Screens, view models, navigation | `winui-dev` | `winui-dev-workflow`, `winui-design`, `winui-code-review` (`.codex/skills/<name>/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`) | Microsoft Learn, Kanmer |
| Gateway endpoints and contracts | `pegasus-gateway-dev` | `dotnet-webapi`, `minimal-api-file-upload`, `optimizing-ef-core-queries`, `microsoft-code-reference` (dotnet/skills `98f84851`; Learn plugin) | Microsoft Learn, Kanmer |
| Characterization, VM, contract tests | `pegasus-test-engineer` | `code-testing-agent`, `run-tests`, `test-gap-analysis`, `assertion-quality` (dotnet/skills `98f84851`) | Microsoft Learn |
| Independent review (boundaries, XAML, a11y) | `pegasus-desktop-reviewer` | `winui-code-review`, `winui-design` | Microsoft Learn |
| UI automation, accessibility, performance | `pegasus-ui-verifier` | `winui-ui-testing`, `analyzing-dotnet-performance` | — |
| Parity rows and page-model evidence | `pegasus-parity-researcher` | `kanmer-research`, `kanmer-verify` (`.grok/skills/`) | Kanmer |
| Ticket pipeline | the owning agent | `kanmer-plan`, `kanmer-execute`, `kanmer-review`, `kanmer-verify`, `kanmer-closeout` | Kanmer (`get_doc_gates`, `take_ticket`, `set_ticket_doc`, `move_item`) |

Load `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) before any
row above. Never load a skill on the "do not load" table in
`docs/desktop/12-agent-tooling/skill-routing.md`. Record the skills consulted, with
their commit SHAs, in the post-implementation report.

## Traps (area plan §7)

- **§13.11 scope creep.** AI assistants, WhatsApp, Audatex/Tractable, EVA replacement,
  provider APIs and MI reporting are not parity. A slice that needs one stops.
- **Parity drift.** Upstream keeps fixing the web app. Re-read the page model after
  the latest sync and record the revision characterized.
- **The two giants.** `Pages/Mail/Message.cshtml.cs` (1,025) and
  `Pages/Cases/Assessment/Index.cshtml.cs` (740) split into S10a/b/c and S17a/b/c and
  never land as one PR.
- **Page-model logic that is really business logic** moves into Core with a test; a
  second implementation is a stop condition.
- **Design authority is a merge rule.** No field hints, no how-it-works copy, only
  populated sections render, filters are dropdowns, tables newest first; the banned
  words (`intake`, `lease`, `artifact`, `projection`, `bytes`, …) never reach the UI;
  every state and date renders through the shared vocabulary list (Europe/London).
- **Do not reproduce web mechanics.** TempData budgets, PRG, antiforgery,
  `IAsyncPageFilter` injection and `ViewData` are web-only.
- **Feature gates.** `/api/v1` sits behind `Features:DesktopGateway`; a gated-off
  endpoint returns 404, so integration tests must enable the gate explicitly.
- **Binary endpoints and limits** are enforced server-side; the desktop streams,
  never buffers.
- **`TreatWarningsAsErrors=true`** and `AnalysisLevel=latest-recommended` apply; fix
  `WUI*` analyzer warnings rather than suppressing them wholesale.
- **Recorded repository traps**: a new table needs runtime role GRANT migrations
  (PLAT-035); App Insights quota hides failures (PLAT-034) — pilot evidence is the
  desktop diagnostics bundle.

## Read before starting any ticket in this epic

1. `docs/desktop/05-implementation-and-migration/README.md` (the plan row in § 5)
2. `docs/desktop/05-implementation-and-migration/vertical-slices.md`
   — § `Common to every slice` first, then this ticket's slice section
3. `docs/desktop/05-implementation-and-migration/reuse-map.md`
4. `docs/desktop/03-gateway-api-and-data/endpoint-map.md` (authoritative for routes)
5. `docs/desktop/06-ui-design/screen-specs.md` and `keyboard-and-accessibility.md`
6. `docs/design/README.md` — the binding UI authority (hard rules, banned words,
   approved necessary copy, status vocabulary)
7. `AGENTS.md` — Simplicity rails, Product invariants, Repository task workflow
8. `docs/engineering.md` — One Core owner, plan sizing, required evidence tiers
9. `docs/desktop/12-agent-tooling/skill-routing.md` — exact skill names and pins
10. `docs/desktop/01-inventory-and-parity/parity-matrix.md` — this slice's rows
