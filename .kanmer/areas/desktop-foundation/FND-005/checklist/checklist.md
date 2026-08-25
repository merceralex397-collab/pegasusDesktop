# Checklist — FND-005

One box per implementation obligation. The original checklist's stale filenames and `origin/dev` assumption are reconciled below with the operator's ADR-0105 ownership decision and the live repository state.

- [x] Read the governance ADR table, the cloud-justification table, the repository ADR conventions, and existing ADR house style.
- [x] Call `get_doc_gates FND-005`, move Backlog → Preparing, create `.worktrees/fnd-005`, and take the ticket on `fnd-005-foundation-adrs`.
- [x] Check the documented base for ADR-0100/0101/0103/0104/0105/0110 before authoring; none existed.
- [x] Record FND-005 as the sole author for the six canonical paths, including the operator-assigned ADR-0105 ownership.
- [x] Author ADR-0101: local execution / cloud authority, the six-question placement rule, and ADR-0014's continuing local Test/UAT boundary.
- [x] Author ADR-0103: `Pegasus.Web` evolves in place as the gateway and workstations never access the database directly.
- [x] Author ADR-0104: online-required operation with bounded non-authoritative local state and no offline replication.
- [x] Author ADR-0105: signed MSIX/App Installer, approved UNC feed, workstation certificate trust, and fail-closed gateway minimum-version gate.
- [x] Author ADR-0110: immutable skill pins, vendoring/lock evidence, and independent invocation/review protocol.
- [x] Author ADR-0100: native WinUI 3 in the fork, the reserved block, ADR-0009's narrow clause relation, D-001, and the prior-document boundary.
- [x] Give every ADR valid eight-key frontmatter, accepted status, Status / Context / Decision / Consequences / Options considered / Links headings, and six answered cloud-justification rows inside Context.
- [x] State the actual host for every affirmative cloud-justification answer without turning a positive answer into a new Azure requirement.
- [x] Add six accepted-decision rows in `docs/adr/README.md`, correct the stale index-shape statement in `AGENTS.md`, and correct the ADR-0009 wording in plan 00.
- [x] Verify documentation links and allowed Markdown placement on the committed branch.
- [ ] Link each ADR to FND-005 and affected tickets, then clear only the corresponding `docs_todo` values. This must wait until the committed files are merged into the MCP repository root, where `link_doc` can validate them.
- [ ] Re-probe one cleared ticket's gates after the links exist.
- [x] Obtain an independent review, address its findings, and pass focused re-review.
- [x] Correct the PR review finding: App Installer's update attributes are not universal activation enforcement for packaged desktop apps; record their documented shortcut/taskbar limit and make the gateway version gate authoritative.
- [x] Opened [PR #1](https://github.com/merceralex397-collab/pegasusDesktop/pull/1) against `dev`; awaiting independent review and required CI before the Review move.
- [ ] After merge, write Tier-1 proof against the merged result and close out the ticket.

## Progress notes

- 2026-08-24 — source base: `task/desktop-plan-segmentation` at `ecb9b7b4`; `origin/main` did not contain `docs/desktop/` and `origin/dev` does not exist.
- 2026-08-24 — pre-authoring ADR existence check found no matching reserved-block ADR files.
- 2026-08-24 — Microsoft Learn verified App Installer `UpdateBlocksActivation` / `ShowPrompt` behavior and LocalMachine TrustedPeople certificate import requirements.
- 2026-08-24 — committed `fb634d1c docs: add desktop foundation ADRs` and `79bb5860 docs: clarify foundation ADR evidence`.
- 2026-08-24 — `pwsh ./scripts/Test-DocumentationLinks.ps1` passed (232 files); `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base ecb9b7b4 -Head HEAD` passed.
- 2026-08-24 — committed `d22c39dde51f087620e30ac1c343a2896585b114 docs: reconcile foundation ADR review`; link, placement, and placement-regression checks passed; focused independent re-review passed.

## Closeout — FND-005

- [x] PR merge verified: PR #1 is MERGED into dev at 2026-08-25T00:12:46Z.
- [x] proof.md finalised with PR URL, merge commit and validation evidence.
- [x] Moved to final stage: Kanmer Done at 2026-08-25T04:57:28.410Z.
- [x] Outcome recorded in ticket body with PR #1, merge SHA, no deployment, and downstream follow-ups.
- [x] Main checkout used for cleanup; .worktrees/fnd-005 removed.
- [x] Local and origin/fnd-005-foundation-adrs branches deleted after merged-branch verification.
- [x] git fetch --prune origin and git worktree prune completed.
- [x] Ticket claim released after cleanup.
