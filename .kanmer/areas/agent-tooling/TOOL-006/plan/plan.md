# Plan — TOOL-006 (plan handle `DSK-12-06`): Wire the Azure MCP server for the read-only auditor agent

**Diff estimate: ~3 files, ~20 lines.** `.codex/config.toml` +4 to +6 (the
`[mcp_servers.azure]` entry replacing the commented placeholder),
`docs/desktop/12-agent-tooling/README.md` § 2 ~4 lines (assumption becomes recorded fact
with the verified command and date), `docs/desktop/12-agent-tooling/subagents.md`
§ `.codex/config.toml` additions ~8 lines (placeholder replaced by the verified entry with
its date). No script, no test, no infrastructure file.

## Approach

Add the server **disabled first, enable only after a read-only probe succeeds**, and make
the probe itself the evidence. The reason for the two-phase shape is that
`.codex/config.toml` is loaded at session start: a bad command line in an enabled MCP entry
degrades every subsequent Codex session on the workstation, including the sessions that
would be used to fix it. Adding it disabled costs one extra restart and removes that class
of failure entirely.

The second choice is to **copy the server block from the pinned upstream file, not from the
plan document**. `docs/desktop/12-agent-tooling/subagents.md` prints a block that is
explicitly labelled a placeholder ("copy the server entry from microsoft/azure-skills
`.mcp.json` at the pinned commit `1a03acfb` and keep it disabled until DSK-12-06 records the
command"). Typing the placeholder in as if it were the answer is the failure this ticket
exists to prevent. The alternative — inventing an `npx @azure/mcp` invocation from memory —
is rejected for the same reason and because §20.2's whole point is that agent tooling comes
from a pinned revision.

The third choice is to treat the **read-only guarantee as text plus recorded tool use**,
because that is all that exists: `docs/desktop/12-agent-tooling/README.md` § 7 states there
is no per-tool permission in the agent TOML. So the acceptance is not "no writes happened"
but "here is the list of tool names that were called, and none of them writes".

## Governing docs

The ticket carries `refs: []` and **`docs_todo: true`**.

> **New ADR** — ADR-0110 (agent-skill pinning and the invocation protocol), authored by
> [[TOOL-008]] (plan handle `DSK-12-08`), filename
> `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`. This plan is written to the
> decision as recorded in `docs/desktop/12-agent-tooling/README.md` § 3 and § 7. The Azure
> *placement* decisions this ticket serves belong to a different ADR — **ADR-0101**
> (local-execution / cloud-authority split and the six-question cloud-justification test),
> reserved in `docs/desktop/00-governance-and-workflow/README.md` § 3 — which this ticket
> does not author and must not claim to meet. If either ADR lands differently this plan is
> revised before implementation.

Programme-level authorities this plan meets:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Azure rule (`docs/desktop/README.md`) | Reads are free; every write is ⚠ and needs exact-target approval | Steps 6–8, 10 and the Guardrails |
| `docs/runbook.md:776` § Live-operation approval matrix | "Read Azure state (inventory, config, diagnostics) — **Permitted, no per-target approval**" for read-only reads that change no state and incur no material cost | Step 6's probe is exactly that row |
| L-02 (locked) / ADR-0014 | Local + production only; no Azure dev/test/staging | Step 6 reads production and creates nothing |
| L-04 (locked) | Every ticket names its subagent, skills and MCP tools | Routing block below |
| Proposal §19 / §20.4 | Azure inventory routes to a read-only auditor with read-only tools | Steps 6–9 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml`
  (`sandbox_mode = "read-only"`, `model_reasoning_effort = "medium"`). Strictly read-only;
  it refuses writes by design and returns approval text instead. It cannot write files, so
  the ticket owner transcribes its output.
- **Skills**, in load order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md`
  2. `azure-resource-lookup` — `.agents/skills/vendor/azure/azure-resource-lookup/`, from
     `microsoft/azure-skills` `1a03acfb9ac1a1a05518bf7420d4618cc41847be` (created by
     [[TOOL-002]], plan handle `DSK-12-02`)
  3. `kanmer-plan`, `kanmer-execute` — `.grok/skills/<name>/SKILL.md`

  **Do not load** `azure-deploy`, `azure-prepare`, `azure-app-onboard`,
  `azure-app-onboard-prereq`, `azure-cloud-migrate`, `azure-enterprise-infra-planner` or
  `python-appservice-deploy` — all are on the do-not-load table in
  `docs/desktop/12-agent-tooling/skill-routing.md`, and none of them is vendored. Do not run
  `azure-validate` in any mode that changes state; `docs/desktop/11-azure-disposition/README.md:343`
  permits it for what-if/Bicep validation **only when a write is already approved**.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Azure MCP **read-only** (`subscription_list`,
  `group_list`, `group_resource_list`); Microsoft Learn (`microsoft_docs_search`) for any
  Azure CLI or MCP fact.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates confirmed by
  `get_doc_gates TOOL-006`: `leave-preparing` needs `plan` + `questions-resolved`;
  `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates TOOL-006` before
  every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

Refines the body's 11 steps in the same order.

1. **Orientation.** Read `EPIC-013/context.md`, then the plan sections in the body's
   **Source of truth**, plus `docs/desktop/11-azure-disposition/README.md` § 6 routing and
   `docs/runbook.md` § Live-operation approval matrix (`docs/runbook.md:776`).
   `get_doc_gates TOOL-006`, then `take_ticket`.
2. **Get the server entry from the pinned file, not from memory.** Read `.mcp.json` in
   `microsoft/azure-skills` at `1a03acfb9ac1a1a05518bf7420d4618cc41847be` and copy its Azure
   server block verbatim. If the pinned file and the `subagents.md` placeholder differ, the
   pinned file wins and the difference is the thing to record.
3. **Add the entry disabled**, replacing the commented placeholder, in the same shape as the
   two existing servers. `.codex/config.toml` today is 15 lines: `[features]` (`:1`),
   `[mcp_servers.mcp_microsoftdocs]` (`:5`, with `enabled = true` at `:7`),
   `[mcp_servers.kanmer]` (`:9`), `[mcp_servers.kanmer.env]` (`:13`) — note the existing
   servers use `command` + `args` (kanmer) and `url` + `enabled` (microsoftdocs), so match
   whichever transport the pinned file uses:

   ```toml
   [mcp_servers.azure]
   command = "<from the pinned .mcp.json>"
   args = [ "<from the pinned .mcp.json>" ]
   enabled = false
   ```

   **Decide and record the moving-reference question.** If the upstream entry names
   `@azure/mcp@latest`, that contradicts §20.2's whole premise — agent tooling is pinned.
   **Recommended default: pin the version** to the newest release available on the day, and
   record the version and the date; the reason is that a moving MCP package can change or
   remove a tool name between sessions, and the read-only guarantee here is a *list of tool
   names* (step 7), so a moving tool surface makes that list unverifiable. If the
   implementer instead tolerates `@latest`, they must write down why a moving MCP package is
   acceptable where a moving skill is not, and how a surprise tool-surface change would be
   noticed.
4. **Operator step — authenticate the workstation.**
   `az login --tenant 858cf5b3-aa0a-47a6-9b40-4851fd0afa94`, then
   `az account set --subscription e6076573-23a5-46a8-acef-7e22d264e5db`, then
   `az account show`. Hand back the output with the subscription and tenant ids visible.
   These ids are independently recorded at `docs/operations.md:287-289` (production target:
   that subscription, that tenant, resource group `rg-pegasus-prod`, region `uksouth`) —
   confirm they match rather than trusting one source. The operator must confirm the
   identity used is read-capable and **not** an owner credential reserved for release work.
5. **Flip `enabled = true`, restart Codex, confirm the `azure` server appears** in the
   session's MCP list. Commit the enable **only after** the step 6 probe succeeds; if the
   probe fails, leave the entry disabled and record why — a disabled entry plus a recorded
   reason is an acceptable outcome for this ticket, a broken enabled entry is not.
6. **Delegate a single read-only probe** to `pegasus-azure-auditor`: "list every resource in
   `rg-pegasus-prod` with its type, using `group_resource_list`; do not call any other
   tool." Capture its full output into `append_scratch`. Expected against
   `infra/modules/platform.bicep` (read 2026-08-24, top-level declarations): a Log Analytics
   workspace (`:46`), an Application Insights component (`:56`), an action group (`:68`), a
   Key Vault (`:85`), **two** storage accounts — transport (`:100`) and custody (`:154`) —
   a SQL server (`:195`) and database (`:214`), a container registry (`:229`), a Container
   Apps managed environment (`:241`), and user-assigned managed identities (`:264` onward).
   The agent must flag anything present live but absent from Bicep, or vice versa.
7. **Record the tool list actually exercised, and assert the negative.** Copy the exact tool
   names into the proof and state that no create, update, delete, role assignment, setting
   change, deployment, scale or restart tool was called. "No writes happened" without the
   list is not evidence — it is a claim.
8. **Reconcile the auditor's allowed-tool paragraph — the three lists do not currently
   agree, and that is the hole the body asks you to find.** Measured 2026-08-24:
   - `.codex/agents/pegasus-azure-auditor.toml:14` allows: `group_resource_list`,
     `group_list`, `subscription_list`, `storage` list/show, `keyvault` list/show (never
     secret values), `monitor`, `applicationinsights`, `sql` show, `containerapps` show,
     `functionapp` show, `pricing`, `advisor`, `resourcehealth`.
   - `docs/desktop/11-azure-disposition/README.md:345` lists: `group_resource_list`,
     `storage`, `keyvault`, `monitor`, `applicationinsights`, `sql`, `containerapps`,
     `functionapp`, `pricing`, `advisor`, `resourcehealth`, **`role`**, `subscription_list`.
   - `docs/desktop/12-agent-tooling/skill-routing.md` § Work type routing, "Azure inventory
     / cost / health (read-only)" lists: `group_resource_list`, `storage`, `keyvault`,
     `monitor`, `applicationinsights`, `sql`, `containerapps`, `functionapp`, `pricing`,
     `advisor`.

   So `role` is permitted by area 11 but not by the TOML, and `group_list` is permitted by
   the TOML but not by area 11; `skill-routing.md` omits `subscription_list`, `group_list`,
   `resourcehealth` and `role`. Since there is **no per-tool permission in the TOML**, this
   prose *is* the guardrail — pick one canonical list, make the three agree, and say which
   one you took as canonical and why. (Area 11 is the register's owner and is the natural
   canonical source; `role` read-only is `role list`/`show`, which is inventory, not a role
   assignment.) This is a wording change with a real effect, not a nit.
9. **Confirm the do-not-load skills were not vendored.** `ls .agents/skills/vendor/azure/`
   must contain exactly the eight lockfile entries: `azure-resource-lookup`,
   `azure-resource-visualizer`, `azure-cost`, `azure-diagnostics`, `azure-compliance`,
   `azure-validate`, `azure-storage`, `appinsights-instrumentation` (verified against
   `docs/desktop/12-agent-tooling/skills.lock.draft.json`, 8 azure entries, 2026-08-24) —
   and nothing from the do-not-load table.
10. **Write the guardrail sentence** into this plan and the post-implementation report: the
    Azure MCP entry exists for read-only inventory, health and cost; any write requires
    exact-target approval text (target resource id, exact change, rollback, approver)
    produced by the auditor and approved per `docs/runbook.md:776` § Live-operation approval
    matrix **before any other agent acts**. Nothing is deprovisioned before cutover,
    observed use and rollback approval.
11. **Record the Appendix C evidence**: the pinned `.mcp.json` source and its commit, the
    `.codex/config.toml` diff, the `az account show` output, the `group_resource_list`
    output, and the tool list from step 7.

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states — and the body's own
caveat matters: area 11's Azure rows are tier 9, so **this ticket proves the wiring only and
makes no claim about the estate itself.** `proof` is a `command-log`.

1. `grep -n 'mcp_servers.azure' -A 4 .codex/config.toml` → the server entry with the command
   and args from the pinned file.
2. `python -c "import tomllib, sys; tomllib.load(open(sys.argv[1], 'rb'))" .codex/config.toml`
   → exit 0, no output.
3. `az account show` (operator) → `"id": "e6076573-23a5-46a8-acef-7e22d264e5db"` and
   `"tenantId": "858cf5b3-aa0a-47a6-9b40-4851fd0afa94"`.
4. The recorded `group_resource_list` output for `rg-pegasus-prod` → a resource list
   consistent with `infra/modules/platform.bicep`, with any difference flagged.
5. `ls .agents/skills/vendor/azure/` → exactly the eight vendored azure skill folders.
6. The recorded tool-name list → no write tool present.

## Risks / open questions

| Risk | Mitigation |
| --- | --- |
| **There is no per-tool permission in the TOML**, so "read-only" is prose plus the sandbox. If [[TOOL-005]] step 11 finds the installed build ignores `sandbox_mode`, even the sandbox half is gone. | Step 7's recorded tool list is the compensating control; step 8 makes the prose canonical and consistent; step 10 restates the approval matrix. Read [[TOOL-005]]'s honoured-fields finding before enabling. |
| An enabled MCP entry with a bad command degrades every later Codex session on the workstation. | Two-phase enable (steps 3 and 5); leave it disabled and record the reason rather than shipping a broken enabled entry. |
| A moving `@latest` package reference contradicts §20.2 and makes the tool-name list unverifiable. | Step 3 forces a recorded decision with a recommended default (pin the version). |
| `azure-validate` has modes that change state. | Routing block and step 8's canonical list; permitted only for what-if when a write is already approved (`docs/desktop/11-azure-disposition/README.md:343`). |
| `keyvault` reads could expose secret values. | The TOML already says list/show metadata but **never** secret values; keep that wording exactly when making the three lists agree. |
| The auditor is read-only and cannot write files, so its findings evaporate if not transcribed. | Step 6 captures into `append_scratch` immediately; this is the same failure mode [[TOOL-009]] (`DSK-12-09`) step 9 exists to measure. |

Open questions: **none opened as a blocking document.** The one "decide and record" item
(moving package reference) has a recommended default with its reason in step 3, and the
three-list divergence in step 8 is a finding with a stated resolution, not an unanswered
question.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. The diff is
configuration plus two documentation edits; record the four lenses' dispositions rather than
writing `n/a — docs-only`, since `.codex/config.toml` is configuration._
