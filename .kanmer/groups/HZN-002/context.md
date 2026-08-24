# Phase 1 — solution foundation

## What this phase delivers

The solution shape every later area inherits (proposal §24 Phase 1): pinned
.NET and Windows App SDK versions, the new Domain/Application-facing
`Pegasus.Contracts` project and the `Pegasus.Desktop` and
`Pegasus.Desktop.Infrastructure` projects with their test projects, Generic
Host with DI, logging and configuration, the shell with theme resources,
navigation and error handling, single-instance lifecycle, a CI Windows build,
an unsigned development MSIX, the diagnostics bundle, and the dependency-
boundary tests that keep the layering honest.

## Plan folders and ticket-handle ranges

| Plan folder | Handles | Board area |
| --- | --- | --- |
| `docs/desktop/02-architecture-and-foundation/` | DSK-02-01 … DSK-02-16 | `desktop-foundation` (FND) |

Two neighbours reach into this phase: `DSK-05-23` (extract `OperatorLabels`
into the shared assembly) depends on the Contracts project created here, and
the test-project scaffolding rows in area 08 are consumed by it.

## Entry condition and exit gate

Entry: the Phase 0 exit gate is met — the parity matrix, flow records and
Azure register are complete, and the dependency rules exist as architecture-
test targets for this area to implement.

Exit gate (proposal §24 Phase 1; **owner: plan 02**):

- A clean Windows 11 test machine launches the native shell.
- No WebView or web-application dependency in the desktop path.
- Foundation tests pass.
- Package install and uninstall work.

The dependency direction the architecture tests enforce: Desktop and
Desktop.Infrastructure must not reference `Pegasus.Infrastructure`, Entity
Framework, Azure SDKs, Box/Graph SDKs or `Microsoft.AspNetCore.*`; Contracts
references nothing but the BCL and `System.Text.Json`; Web may reference
Contracts; Core is unchanged.

## Decisions and constraints that bind this phase

- **L-01** — the gateway is `Pegasus.Web` evolved in place. This phase adds
  desktop projects to the same solution; it creates no new deployment unit.
- **L-04** — every ticket names its subagent, skills and MCP tools.
- **C-01** — private-repository Windows runners bill at 2×, and this phase is
  where the first Windows build and packaging lanes are added; keep the lane
  count deliberate (see `docs/desktop/08-testing/README.md` § 7).
- **Markdown placement** — any new `.md` outside
  `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job.

## Azure rule

Reads are free; every write is ⚠, approval-gated
(`docs/runbook.md` § Live operation approval matrix) and mirrored in plan 11;
nothing is deprovisioned before cutover, observed use and rollback approval.
**This phase performs no Azure write at all** — it is local solution work.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map
- `docs/desktop/02-architecture-and-foundation/README.md`
- `docs/desktop/08-testing/README.md`
- `docs/desktop/06-ui-design/tokens-and-theme.md`
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 5.4, § 7, § 24 Phase 1
- `docs/engineering.md` § Required evidence tiers
- `.kanmer/groups/HZN-001/board-conventions.md`
