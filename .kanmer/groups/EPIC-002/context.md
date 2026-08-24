# EPIC-002 — Area 01, inventory and parity (Phase 0)

Read once before working any ticket in this epic. Ticket-specific evidence lives in the ticket.

## What this area delivers

Phase 0 discovery: nothing here changes runtime behaviour, Azure state, or the web application.
It produces four documents and one board outcome — a repository-derived parity matrix with one row
per observable capability, six closed current-flow records, a verified read-only Azure resource
register, the upstream Kanmer carry-over executed onto the fork board, plus the first upstream code
sync, the web-app performance baseline, and the characterization-gap and dependency-rule targets
that areas 02 and 08 build from. Tickets DSK-01-01…DSK-01-12 (board ids FND-014…FND-025).

## Proposal coverage

`docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` §13.1–13.11 (capability groups, and the
rule that future channels are not "parity"), §23 and §23.1 (matrix columns, status ladder, required
conversion evidence), §24 Phase 0 and its four exit-gate items, §29 items 2–5, §4/§4.1 (the
placement column each row carries), and §19's first action — inventory and identify the code path
that uses each Azure resource.

## Decisions, assumptions and deviations that bind every ticket here

- **L-01** the gateway is `Pegasus.Web` evolved in place; every `~/api/v1` name in the matrix lives
  inside that project. **L-02** Test/UAT is a local production-mimicking stack; ADR-0014 stands and
  requesting an Azure test resource is out of bounds. **L-03** report rendering moves to an isolated
  non-UI WebView2 path (ADR-0108) — record it, do not design it in Phase 0. **L-05** the board is
  seeded from these plans, so the carry-over table is the seed list for upstream work.
- **D-001** the fork becomes the single release source at the first production gateway change;
  until then the one-way upstream sync continues. **D-002**/**D-003** keep signing and the update
  feed in-house, so no Azure signing service, certificate or feed may appear in the register.
  **C-01** the repositories become private and private Windows runners bill at 2×.
- ⚠ **Azure writes: none in this epic.** Every Azure action is a read. Applying the recorded tags is
  a write, needs exact-target approval (`docs/runbook.md` § Live-operation approval matrix) and is
  listed in area 11. Nothing is deprovisioned before cutover, observed use and rollback approval.
- **Deviations recorded in the plan**: the matrix adds a `legacy path retained` status the proposal's
  ladder lacks (for `PAR-31` and `PAR-42`, which stay server-side); the matrix lives at
  `docs/desktop/01-inventory-and-parity/parity-matrix.md`, not the proposal's `docs/features/` path,
  because that folder does not exist and the placement gate allows `docs/desktop`.
- **Assumptions**: the 53 page models are the complete staff surface until DSK-01-01 re-derives it;
  the operator names one UAT owner per capability group, so that column stays blank; the upstream
  board keeps moving, so the triage is dated and a re-triage is a new ticket.

## Exit gate and what proves it

1. Every current production capability has an inventory row. 2. Every Azure resource has an
owner/use statement. 3. No unresolved uncertainty around authentication, database or Graph intake.
4. Target dependency rules exist as architecture-test targets or documented checks.
Proof: the four documents updated in the same PR as the tickets that filled them, plus the attached
read-only command output — Azure MCP results, `git ls-files`/`git grep` enumerations, test
enumeration — and `pwsh ./scripts/Test-DocumentationLinks.ps1` and
`pwsh ./scripts/Test-MarkdownPlacement.ps1` green.

## Routing for this area

| Work | Subagent (`.codex/agents/<name>.toml`) | Skills, in load order | MCP |
| --- | --- | --- | --- |
| Parity rows, flow records, carry-over | `pegasus-parity-researcher` (read-only) | `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-research` / `kanmer-tickets` / `kanmer-groom` (`.grok/skills/<name>/SKILL.md`, Kanmer 0.1.0) → `microsoft-docs`, `microsoft-code-reference` (Microsoft Learn plugin) | Kanmer; Microsoft Learn `microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search` |
| Azure register | `pegasus-azure-auditor` (read-only) | `pegasus-desktop` → `azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost` (`microsoft/azure-skills` `1a03acfb`) | Azure MCP list/show only: `subscription_list`, `group_list`, `group_resource_list`, `storage`, `keyvault` (names only), `sql`, `containerapps`, `functionapp`, `monitor`, `applicationinsights`, `acr`, `role`, `pricing`, `advisor` |
| Upstream sync PR | `pegasus-gateway-dev` | `pegasus-desktop` → `run-tests` (`dotnet/skills` `98f84851`) | Kanmer |
| Performance baseline | `pegasus-ui-verifier` | `pegasus-desktop` → `analyzing-dotnet-performance`, `dotnet-trace-collect` (`dotnet/skills` `98f84851`) | Kanmer |
| Characterization gaps | `pegasus-test-engineer` | `pegasus-desktop` → `test-gap-analysis`, `assertion-quality` (`dotnet/skills` `98f84851`) | Kanmer |
| Independent review of every ticket | `pegasus-desktop-reviewer` (read-only) | `pegasus-desktop` → `kanmer-review` | Kanmer `get_ticket_doc` |

Do **not** load `azure-deploy`, `azure-prepare`, `azure-app-onboard`, `azure-cloud-migrate`,
`azure-enterprise-infra-planner`, `entra-app-registration`, `entra-agent-id`, `winui-wpf-migration`,
`dotnet-aot-compat` or `configuring-opentelemetry-dotnet`
(`docs/desktop/12-agent-tooling/skill-routing.md` § Not applicable to this conversion).
`get_doc_gates <id>` is authoritative for every move, never `board.yml`; a move crosses at most one
gated boundary, and an unticked `open-questions/` item blocks it.

## Traps (area plan § 7)

Inventory by page count misses behaviour — one handler can dispatch 13 commands. The web app keeps
moving, so every row records the commit it was inventoried at. Documentation drift already exists:
`docs/operations.md:295` contradicts its own release table at `:311-332` — the table is
authoritative. Capability IDs and ticket IDs collide in appearance (`CASE-17` vs `CASE-017`); never
abbreviate. Azure tagging is a write. Never fabricate domain data — fixtures come from `reference/`
or the ignored, immutable `corpus/`. Application Insights is capped at 0.1 GB/day, so an empty
telemetry query is not evidence of no traffic (upstream `PLAT-034`). A local full-privilege run
proves nothing about deployed permissions (upstream `PLAT-035`).

## Read before starting any ticket in this epic

- `docs/desktop/README.md` (decisions, routing legend)
- `docs/desktop/00-governance-and-workflow/README.md` (board shape, ticket template, phase map)
- `docs/desktop/01-inventory-and-parity/README.md` (this area's §§ 2–5, 7, 8)
- `docs/desktop/01-inventory-and-parity/parity-matrix.md` · `flow-records.md` ·
  `azure-resource-register.md` · `upstream-kanmer-carryover.md`
- `docs/desktop/12-agent-tooling/skill-routing.md` (exact names, pins, do-not-load table)
- `AGENTS.md` (Simplicity rails, Safety rails, Product invariants, Repository task workflow) and
  `docs/engineering.md` (§ Branches and delivery, § Required evidence tiers, § Engineering invariants)
