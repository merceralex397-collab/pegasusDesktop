# Open questions — FND-021

These boxes are the gate. For profile `spike` an unticked `- [ ]` line **above** the
`## Parked` heading blocks `enter-done` — and only `enter-done`; it never gates
`leave-backlog`. Verified with `get_doc_gates` (no id): `spike` resolves to
`enter-done: [research, questions-resolved]` and nothing else.

Every box corresponds to a `NOT YET CAPTURED` block in the `research` document.
Nothing below can be answered from the repository: each needs a **read-only** Azure
MCP call, and the permitted set is exactly `subscription_list`, `group_list`,
`group_resource_list`, `storage`, `keyvault` (names only), `sql`, `containerapps`,
`functionapp`, `monitor`, `applicationinsights`, `acr`, `role`, `pricing`, `advisor`,
`resourcehealth` — list/show only. Tick a box only when the raw output is attached to
the ticket and the register row is written.

- [ ] **U-1 · authenticated session and role.** **Operator step.** The session must be
      authenticated against subscription `e6076573-23a5-46a8-acef-7e22d264e5db` in
      tenant `858cf5b3-aa0a-47a6-9b40-4851fd0afa94`
      (`.azure/deployment-plan.md:24-27`) with a **reader**-level role. Verified with
      `subscription_list` and `group_list`. Evidence to hand back: the subscription id
      and the role the session holds. No credential is stored in the repository.
- [ ] **U-2 · `group_resource_list` for `rg-pegasus-prod`, raw JSON saved to scratch.**
      This is the set every register row is compared against; without it steps 5, 8
      and 9 are guesses.
- [ ] **U-3 · one saved show/list output per resource type**, each named after the tool
      that produced it: `storage` (both accounts plus container and queue lists),
      `keyvault` (secret **names** only), `sql`, `containerapps`, `functionapp`,
      `monitor`, `applicationinsights`, `acr`, `role`. Acceptance requires the raw
      output attached, not a summary (tier 9).
- [ ] **U-4 · drift comparison, recorded and never removed.** Every resource in Azure
      but not in `infra/`, and every register row with no live resource, goes into a
      new dated "Drift observed" subsection. **Expected finding:** research F-4 shows
      **no Key Vault Secrets User role assignment is declared anywhere in `infra/`**,
      yet the Web container app resolves three Key Vault-backed secrets by
      `keyVaultUrl` + `identity` (`infra/modules/platform.bicep:382-397`). Check the
      `role` assignment list for those grants and record them as drift. "A service is
      not unused merely because no developer remembers it."
- [ ] **U-5 · `allowBlobPublicAccess` on both storage accounts and the Log Analytics
      `workspaceCapping` state**, both recorded verbatim. Named explicitly by the
      register's own verification procedure step 3; they feed D-003 and upstream
      `PLAT-036`. The 0.1 GB/day cap is also why an empty telemetry query is **not**
      evidence of no traffic (upstream `PLAT-034`).
- [ ] **U-6 · a real `path:line` "Used by (code path)" for every register row.**
      Approximate citations are replaced, each proved with `git grep -n`. Three are
      already verified in research F-9 and can be written straight in: Application
      Insights → `src/Pegasus.Web/Program.cs:193-199`; Data Protection key ring →
      `:172-176`; pinned `DefaultAzureCredential` client ids → `:158-171`.
- [ ] **U-7 · a proposal §19 target position for every row**, drawn only from that
      section's vocabulary (*Retain*, *Retain, simplified*, *Consolidate into
      gateway*, *Retain or repurpose*, *Reassess after stabilization*, *Deprovision
      candidate*), with "Deprovision candidate" used only where §19 itself uses it, and
      every row keeping "not before cutover, observed use and rollback approval" as its
      removal condition.
- [ ] **U-8 · "Declared absent" re-confirmed with a dated check per entry** — all nine
      (research F-6). Recorded as "absent, verified `<date>`" or as a drift finding.
      D-002 and D-003 mean no signing service, Key Vault certificate or App Installer
      feed exists or will; confirm none has appeared, and do not re-open the decision.
- [ ] **U-9 · cost context recorded read-only** against the `pegasus-prod-monthly`
      budget (`infra/main.bicep:114`, cap 75/month, alerts at 50/80/100 % plus
      forecast): current spend and forecast, with no change proposed.
- [ ] **U-10 · the tagging approval text written and the ticket stopped there.** For
      each intended tag (`desktop-conversion=phase0-inventory`, `owner=<name>`,
      `codepath=<file>`): target resource id, exact change, rollback, approver.
      Applying a tag is an Azure **write** and belongs to area 11 behind exact-target
      approval (`docs/runbook.md` § Live-operation approval matrix). Tick this box by
      writing the text, never by applying a tag.
- [ ] **U-11 · the register written back, the proof attached, the link check green.**
      `pwsh ./scripts/Test-DocumentationLinks.ps1` exits 0, and the ticket carries the
      raw outputs of `group_resource_list`, `storage`, `keyvault`, `monitor`,
      `applicationinsights`, `sql`, `containerapps` and `functionapp`.

## Parked (explicitly deferred)

Everything below this heading is **not** counted by the gate.

- [ ] Correcting the "34 declared resources/assignments" figure in
      `docs/desktop/01-inventory-and-parity/README.md` § 2 — the real count is 41
      `resource` declarations in `infra/modules/platform.bicep` plus 2 in
      `infra/main.bicep` (research F-2). Safe to defer: that file is not in this
      ticket's editable set (only `azure-resource-register.md` and, on drift,
      `docs/desktop/11-azure-disposition/README.md`), and the register itself never
      quotes the figure. Reopened if a later ticket cites 34 as a fact.
- [ ] Whether the "10 role assignments" register row should also list the eleventh by
      name. Default taken: correct the count to **11** and keep the existing summary
      of role kinds; naming all eleven inflates a table that already cites
      `platform.bicep:276-352`. Reopened if the reviewer wants the full list.
