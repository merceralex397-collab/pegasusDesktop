# Checklist — FND-008

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")` as you go; append progress notes below rather
than rewriting.

- [ ] Read `docs/frd/README.md` in full (definition `:3-13`, documents table `:16-32`, template `:35-58`), `docs/frd/frd-12-operator-experience.md` in full, `docs/design/README.md`, and `docs/desktop/06-ui-design/README.md`
- [ ] Call `get_doc_gates FND-008`, confirm `leave-backlog: [governing-doc]` is satisfied by `docs_todo`, then `take_ticket` with a real branch and worktree cut from `origin/dev`
- [ ] **Gate:** run `ls docs/adr/010*` and confirm ADR-0100, ADR-0102, ADR-0104, ADR-0105 and ADR-0108 all exist — stop and wait for [[FND-005]], [[FND-006]] and [[FND-007]] if any is missing
- [ ] Classify every bullet of `docs/frd/frd-12-operator-experience.md:4-20` as *web only until cutover* or *restated for the desktop*, and write the classification into the plan's step 2
- [ ] Record the FRD-12 / FRD-13 boundary rule in the plan, including the shared-behaviour resolution (it stays in FRD-12 and FRD-13 cites it)
- [ ] Create `docs/frd/frd-13-desktop-operator-experience.md` with the `docs/frd/README.md:35-58` owner line and the six template headings
- [ ] Raise the template-versus-house-form divergence with `pegasus-desktop-reviewer` in the PR (no existing FRD follows the template; the house owner line reads "UI behaviour" with a bare design path) and record their answer
- [ ] Write `## Behaviour` as normative rules: unsupported client version must not proceed; every critical workflow completable from the keyboard; a field is a label and a control and operator-facing explanation is a defect; no colour-only state; a disconnected client must not silently queue
- [ ] Cite `docs/desktop/06-ui-design/screen-specs.md` for per-screen detail instead of copying it
- [ ] Cite ADR-0100, ADR-0102, ADR-0104, ADR-0105 and ADR-0108 — and mark ADR-0108 as `status: proposed` wherever it is cited
- [ ] Append the FRD-13 row to `docs/frd/README.md`'s documents table (`:16-32`), three cells, families `DSK` (plus `UI` where it extends FRD-12's domain)
- [ ] Append the `DSK` rows to `docs/capabilities.md` under `## Capabilities` (`:69`) in the six-column order at `:71`, one row per **durable outcome** and not per screen, each with a `Canonical owner` link to FRD-13 or the owning ADR
- [ ] Add the `DSK` family note row separating the three namespaces: capability `DSK-01`, plan handle `DSK-00-01`, board id [[FND-001]]
- [ ] Recompute the horizon table at `docs/capabilities.md:31-36`
- [ ] Recompute the `Total: **N capabilities; N unique IDs**.` line at `docs/capabilities.md:38`
- [ ] Recompute the target-release table at `docs/capabilities.md:40-54`
- [ ] Confirm all three totals reconcile with each other and with `grep -c '^| [A-Z]*-[0-9][0-9] |' docs/capabilities.md` (231 before this ticket)
- [ ] Confirm no existing capabilities row was edited — in particular the `OPS-10` row at `docs/capabilities.md:73`, whose note change belongs to [[REL-016]] under D-004
- [ ] Add the native Windows desktop client to `docs/prd/pegasus-product.md` `## Purpose, users, and outcomes` (`:4-18`) as an outcome, not a mechanism
- [ ] Record the post-cutover web retirement in the PRD as **scope, not schedule**, and say in the plan which section carries it and why
- [ ] Add one desktop-experience question row to `docs/index.md`'s question→file table (`:7-30`), leaving § New Markdown files (`:41-53`) unedited
- [ ] Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both exit 0
- [ ] `link_doc` FRD-13 to the area 05 and area 06 tickets it now governs, including [[DUI-013]], and clear `docs_todo` **only** where it was set for FRD-13 alone
- [ ] Re-probe `get_doc_gates` on at least one ticket whose `docs_todo` was cleared and record that its `leave-backlog` is still `passable: true`
- [ ] Open the PR against `dev` with `gh pr create --base dev`, take the independent review from `pegasus-desktop-reviewer`, and record `n/a — docs-only` under a dated `## Simplification pass` heading in the plan
- [ ] Verification run: after merge, capture the full command table from the plan's `## Verification` section as the `proof` `command-log` (tier 1 — static/build/architecture)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
