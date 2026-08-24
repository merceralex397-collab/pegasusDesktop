# Checklist — FND-005

One box per plan step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")` as you go; append progress notes below rather
than rewriting.

- [ ] Read `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR set table and the cloud-justification table), `AGENTS.md:77-116`, and the two house-style ADRs `docs/adr/0015-host-web-on-container-apps-consumption.md` and `docs/adr/0029-image-initiated-case-projection.md`
- [ ] Call `get_doc_gates FND-005` and confirm `leave-backlog: [governing-doc]` satisfied by `docs_todo`, then `take_ticket` with a real branch and worktree cut from `origin/dev`
- [ ] Run `ls docs/adr/010*` **before writing any file** and record its output verbatim in the ticket
- [ ] Record in the plan's step 2 which ticket authors each co-claimed ADR (0100/0104 vs [[FND-026]]; 0105 vs [[REL-001]] and [[FND-042]]; 0110 vs [[TOOL-008]]), and for any file that already exists take the extend-in-place route instead of creating a second one
- [ ] Write `docs/adr/0101-local-execution-cloud-authority-split.md` — the local-execution / cloud-authority split, adoption of the six-question test, relates ADR-0002
- [ ] Write `docs/adr/0103-gateway-not-direct-database-access.md` — workstations never reach the database; gateway is `Pegasus.Web` evolved in place (L-01); cites `src/Pegasus.Infrastructure/DependencyInjection.cs:53`, `src/Pegasus.Web/Program.cs:549`, `src/Pegasus.Worker/WorkerDependencyInjection.cs:150`
- [ ] Write `docs/adr/0104-online-required-no-offline-replication.md` — online-required, bounded local cache, no replication
- [ ] Write `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` — the two-layer enforcement (App Installer `UpdateBlocksActivation` plus a fail-closed gateway minimum-client-version gate), the D-002 self-managed certificate in `LocalMachine\TrustedPeople`, the D-003 in-house UNC feed over SMB; relates ADR-0007 unchanged
- [ ] Write `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md` — pinning by revision, the vendored tree, the invocation/review protocol; describes the existing `skills-lock.json` mechanism rather than inventing a second one
- [ ] Write `docs/adr/0100-native-winui3-desktop-client.md` **last**, restating the reserved block ADR-0100…ADR-0110 and its 2026-08-23 operator confirmation (`AGENTS.md:84-89`)
- [ ] In ADR-0100's `## Context`, record the ADR-0009 **deferral-clause** supersession as a prose sentence quoting `docs/adr/0009-…:74-75`, with `supersedes: []` left empty in frontmatter and ADR-0009 not edited at all
- [ ] In ADR-0100's `## Consequences`, include the decided D-001 text ([[FND-010]]) and the "prior documents are not in the repository and are not an input" sentence ([[FND-013]]) — or record here the decision not to, and its cost
- [ ] Give all six files the eight-key YAML frontmatter from `AGENTS.md:95-105` with `status: accepted`, a real `date`, and house-idiom `tags` — no tabs, no smart quotes
- [ ] Give all six files the heading set `Status · Context · Decision · Consequences · Options considered · Links`, with Status first
- [ ] Put the Appendix A six-question table inside each ADR's `## Context` with **every cell filled**, transcribing the worked answers from this ticket's `research` document and re-verifying each cited `path:line`
- [ ] Confirm every "yes" in those six tables names the host it lands on, and that no "yes" has been turned into an Azure justification (D-002's in-house signing host and D-003's in-house UNC host are the answers for ADR-0105)
- [ ] State explicitly in both ADR-0101 and ADR-0103 that **ADR-0014 is not superseded** — Test/UAT stays local under L-02 and no Azure dev/test/staging is created
- [ ] Append six three-cell rows to the accepted table in `docs/adr/README.md`, in ID order after the ADR-0029 row at `:41`, matching the header at `:18` and linking by bare relative filename; leave `## Superseded and relocated` (`:43-52`) untouched
- [ ] Correct `AGENTS.md:114-116` so the index-shape sentence reads `ADR | Title | Related FRD`, and confirm `grep -n 'Owner capability' AGENTS.md` then returns no match
- [ ] Correct `docs/desktop/00-governance-and-workflow/README.md:423` so the § 8 row no longer instructs an ADR-0009 `superseded_by` frontmatter note
- [ ] Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both exit 0, and every `## Links` entry is outside a fenced block so the gate actually checks it
- [ ] `link_doc` each new ADR path to this ticket, and clear `docs_todo` **only** on tickets whose governing ADR is now one of these six
- [ ] Re-probe `get_doc_gates` on at least one ticket whose `docs_todo` was cleared and record that its `leave-backlog` is still `passable: true`
- [ ] Open the PR against `dev` with `gh pr create --base dev`, take the independent review from `pegasus-desktop-reviewer`, and record `n/a — docs-only` under a dated `## Simplification pass` heading in the plan
- [ ] Verification run: after merge, capture the full command table from the plan's `## Verification` section as the `proof` `command-log` (tier 1 — static/build/architecture)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
