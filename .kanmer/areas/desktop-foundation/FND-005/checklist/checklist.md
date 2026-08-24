# Checklist — FND-005

One box per plan step, in plan order. Every box is independently tickable: a
reader can say yes or no to it without reading any other box.

- [ ] Read `docs/desktop/00-governance-and-workflow/README.md` § 3, `AGENTS.md:77-118`, `docs/adr/0028-*.md` and `docs/adr/0029-*.md`; call `get_doc_gates FND-005` and `take_ticket`
- [ ] Run `ls docs/adr/010*` **before writing anything** and record its exact output in the ticket (expected on 2026-08-24: no such file)
- [ ] Record in `plan` which of the four co-claimed numbers (0100, 0104, 0105, 0110) already had a file, and for each such number state that this ticket verifies and extends in place rather than creating a second file
- [ ] Record the ADR-0105 ownership position in `plan` — the operator's answer verbatim with its date if it has arrived, or the tie-break being relied on and a pointer to [[REL-001]]'s `open-questions` box if it has not
- [ ] Create `docs/adr/0100-native-winui3-desktop-client.md`
- [ ] Create `docs/adr/0101-local-execution-cloud-authority-split.md`
- [ ] Create `docs/adr/0103-gateway-not-direct-database-access.md`
- [ ] Create `docs/adr/0104-online-required-no-offline-replication.md`
- [ ] Create `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`
- [ ] Create `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`
- [ ] Give all six the `AGENTS.md:94-108` frontmatter block with `status: accepted`, a real `date`, and `related_frd` written as lowercase stems (`frd-11`, never `FRD-11`)
- [ ] Give all six the heading set `## Status · ## Context · ## Decision · ## Consequences · ## Options considered · ## Links`, following `docs/adr/0029-*.md:11-20`
- [ ] Put the six-question cloud-justification table inside `## Context` of each of the six ADRs, with a real answer and a real citation in every row — no blank cells
- [ ] Answer ADR-0105's "protected credentials" row as *yes* landing on the in-house signing host (D-002) and its feed on the in-house UNC share (D-003), citing both decisions — never "Azure"
- [ ] Write ADR-0100's reserved-block restatement, naming the operator confirmation of 2026-08-23 and citing `AGENTS.md:84-90`
- [ ] Write ADR-0100's ADR-0009 deferral-clause supersession as a sentence in `## Context`, citing `docs/adr/0009-*.md:73-74`, with `supersedes: []` kept in ADR-0100's frontmatter
- [ ] Confirm `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md` is untouched in body **and** frontmatter (`git diff --stat` on that path is empty)
- [ ] Write the decided D-001 into ADR-0100's `## Consequences`, coordinated with [[FND-010]], and record in `plan` who agreed it
- [ ] Write the "the proposal's three prior documents are not in the repository and are not an input" sentence into ADR-0100's `## Consequences`, coordinated with [[FND-013]]
- [ ] Write ADR-0101's decision: the local-execution / cloud-authority split, adopting the six-question test as the repository's placement rule; relates ADR-0002
- [ ] Write ADR-0103's decision: workstations never connect to the database; the gateway is `Pegasus.Web` evolved in place under L-01; relates ADR-0002 and ADR-0015
- [ ] Write ADR-0104's decision: online-required, bounded local cache, no replication
- [ ] Write ADR-0105's decision: two-layer enforcement (App Installer `UpdateBlocksActivation` plus the fail-closed gateway minimum-client-version gate), the D-002 certificate and the D-003 UNC feed; relates ADR-0007
- [ ] Write ADR-0110's decision: skill pinning by revision, the vendored tree, the invocation/review protocol; relates `skills-lock.json`
- [ ] State explicitly in ADR-0101 **and** ADR-0103 that ADR-0014 is not superseded
- [ ] Add one row per ADR to `docs/adr/README.md`'s accepted table in ID order, three cells each (`ADR | Title | Related FRD`)
- [ ] Correct `docs/desktop/00-governance-and-workflow/README.md:422` so the § 8 ADR row no longer instructs an ADR-0009 `superseded_by` frontmatter edit
- [ ] Correct `AGENTS.md:114-117` to the real index shape, and confirm `grep -n 'Owner capability' AGENTS.md` returns no match
- [ ] `link_doc` each of the six new ADR paths to this ticket
- [ ] Clear `docs_todo` only on conversion tickets whose governing ADR is one of these six, and only after its `link_doc` exists
- [ ] Open the PR against `dev` and take the independent review from `pegasus-desktop-reviewer`
- [ ] Record the simplification pass under a dated `## Simplification pass` heading in `plan` (`n/a — docs-only`)
- [ ] Verification run — `ls docs/adr/010*`, `grep -l '^id: ADR-01' docs/adr/*.md`, the two `docs/adr/README.md` row greps, `grep -n 'Owner capability' AGENTS.md`, `git diff --stat -- docs/adr/0009-*.md`, `pwsh ./scripts/Test-DocumentationLinks.ps1`, `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — all as the plan's Verification table states; **this box produces `proof`**

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
