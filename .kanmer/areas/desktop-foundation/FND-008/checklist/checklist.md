# Checklist — FND-008

One box per plan step, in plan order. Every box is independently tickable.

- [ ] Read `docs/frd/README.md` in full, `docs/frd/frd-12-operator-experience.md`, `docs/design/README.md` and `docs/desktop/06-ui-design/README.md`; call `get_doc_gates FND-008` and `take_ticket`
- [ ] Confirm [[FND-005]] has merged and that every ADR path FRD-13 will cite exists in the tree
- [ ] Write the FRD-12 / FRD-13 boundary into `plan` — FRD-12 keeps the web operator experience until cutover; FRD-13 owns shell and navigation, session and first run, keyboard completion, accessibility baseline, error and empty states, update-required behaviour
- [ ] Create `docs/frd/frd-13-desktop-operator-experience.md` using the template at `docs/frd/README.md:35-58`, with the owner line, `## Purpose`, `## Behaviour`, `## States and transitions`, `## Edge cases and fail-closed behaviour`, `## Acceptance evidence` and `## Links`
- [ ] Record in `plan`, and flag in the PR description, that no existing FRD follows this template (measured `##` counts 2,1,2,1,1,2,1,2,2,3,1,1) and that the house owner line reads `· UI behaviour: docs/design/README.md`
- [ ] Write `## Behaviour` as normative rules using "must" / "never" / "fails closed", including: an unsupported client version must not proceed; every critical workflow must be completable from the keyboard; no colour-only state
- [ ] Confirm FRD-13 cites `docs/desktop/06-ui-design/screen-specs.md` for per-screen detail and copies none of it
- [ ] Confirm FRD-13 cites `docs/design/README.md` as binding UI authority rather than restating it
- [ ] Cite ADR-0100, ADR-0102, ADR-0104 and ADR-0105 by relative path in `## Links`
- [ ] Cite ADR-0108 and mark it explicitly as still `status: proposed`
- [ ] Add the FRD-13 row to the `## Documents` table in `docs/frd/README.md` with families `DSK` (and `UI` where it extends FRD-12's domain)
- [ ] Add the `DSK` family rows to `docs/capabilities.md` under `## Capabilities`, one per durable desktop outcome, in the existing six-column order, each with a resolving `Canonical owner`
- [ ] Add the family note row stating that a capability ID is `FAMILY-NN` and is not the plan handle `DSK-<area>-<nn>`
- [ ] Recompute the horizon table at `docs/capabilities.md:31-36` so its column sums to the stated total
- [ ] Recompute the `Total: **N capabilities; N unique IDs**.` line at `:38`
- [ ] Recompute the target-release table at `:40-54` so its column sums to the same total
- [ ] Confirm the `OPS-10` row at `docs/capabilities.md:73` is untouched (`git diff -- docs/capabilities.md | grep 'OPS-10'` is empty) — it belongs to [[REL-016]] under D-004
- [ ] Add the native Windows desktop client to `docs/prd/pegasus-product.md` § Purpose, users, and outcomes
- [ ] Record the web front end's post-cutover retirement in the PRD as scope, not as a schedule, in the section that fits after reading `## Permanent boundaries`
- [ ] Add the FRD-13 question row to the question→file table in `docs/index.md` (`:7-30`), leaving § New Markdown files unedited
- [ ] Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both exit 0
- [ ] `link_doc` `docs/frd/frd-13-desktop-operator-experience.md` to the area 05 and area 06 tickets it now governs, including [[DUI-013]]
- [ ] Clear `docs_todo` only where it was set for FRD-13 alone, and only after each `link_doc` exists
- [ ] Open the PR against `dev` and take the independent review from `pegasus-desktop-reviewer`
- [ ] Record the simplification pass under a dated `## Simplification pass` heading in `plan` (`n/a — docs-only`)
- [ ] Verification run — the two gate scripts, `grep -c '^| DSK-' docs/capabilities.md`, the two allocation-summary column sums, `grep -n 'FRD-13' docs/frd/README.md docs/index.md`, the `DSK-` id-form grep, the `OPS-10` diff check, the ADR-0108 `proposed` grep and the FRD-13 heading count — all as the plan's Verification table states; **this box produces `proof`**

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
