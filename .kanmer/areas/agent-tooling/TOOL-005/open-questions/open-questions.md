# Open questions — TOOL-005 (plan handle `DSK-12-05`): the roster and the project skill

**Why this document exists.** Two ticket bodies route a question here.
[[TOOL-001]] (plan handle `DSK-12-01`) § Guardrails, "Open question to carry", says: *"if the
installed build honours neither `model_reasoning_effort` nor `sandbox_mode`, the read-only
guarantee for `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and
`pegasus-azure-auditor` rests on the agent text alone — record it as an open question for
[[DSK-12-05]] rather than assuming enforcement."* This ticket's own body step 11 says the same
from the other side: *"If it ignores `model_reasoning_effort` or `sandbox_mode`, write that
down … that becomes an open question rather than an assumption."*

**Why the entry is parked rather than unticked.** The question is not yet *unresolved* — it is
**unobserved**, and this ticket's own step 11 is what observes it. An unticked box above
`## Parked` would block `leave-preparing`, which for a `chore` is the boundary between
Preparing and Implementing — that is, it would block the very step that produces the answer.
Parking records the question and its trigger without that circularity, and leaves
`questions-resolved` satisfied.

Note what parking does **not** rest on. The earlier plan gave as part of its reason that
opening an item "would block every stage move". That is false: an unticked `- [ ]` line blocks
exactly `leave-preparing`, `enter-review` and `enter-done`, never `leave-backlog`, and for a
`chore` the board declares only `leave-preparing` and `enter-done`
(`get_doc_gates` with no id). The reason for parking is the circularity above, not the cost.

## Parked (explicitly deferred)

- **Does the installed Codex build actually honour `sandbox_mode` and
  `model_reasoning_effort`, or is the read-only guarantee only prose?**
  *Deferred until this ticket's step 11 observes it — and promoted to an unticked box on this
  same document if the answer is "it honours neither".*

  What is at stake: `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and
  `pegasus-azure-auditor` are declared `sandbox_mode = "read-only"` in their TOMLs (verified
  2026-08-24). If the runtime ignores the field, "read-only" is enforced by nothing but the
  sentence in each agent's `developer_instructions`. That is material twice over: it is the
  guardrail [[TOOL-006]] (plan handle `DSK-12-06`) relies on for its Azure MCP wiring, which
  has no per-tool permission behind it either; and it is what makes
  [[TOOL-009]] (plan handle `DSK-12-09`)'s read-only reviewer safe to point at a real diff.

  How it gets answered: [[TOOL-001]]'s spike captures it first — it is an unticked box on that
  ticket's `open-questions` ("Optional TOML fields honoured"). This ticket's step 11 records
  it against the real config, either confirming enforcement or finding it absent.

  **The promotion rule, and it is not optional:** if the build honours neither field, move
  this entry **above** the `## Parked` heading as an unticked `- [ ]` box at that moment. It
  then blocks `enter-done` on this ticket until the consequence is written down and
  [[TOOL-006]] is told — which is the correct outcome, because shipping an `[agents]` table
  that advertises unenforced sandboxes is worse than shipping none.

- **D-004 — the OPS-10 acceptance folding into the desktop pilot approval.** Not open, and
  must not be re-opened: it is a **decided** operator decision (2026-08-24), owned by plan 09
  and recorded against [[REL-009]] (plan handle `DSK-09-11`). It reaches this ticket only as a
  forward dependency: when D-004 lands in `docs/desktop/README.md` § Locked decisions, the
  project skill needs the same one-line reconcile that plan step 3 does for L-04 and L-05. Do
  **not** add it to `.agents/skills/project/pegasus-desktop/SKILL.md` before it is in the
  README.

- **Whether `create-custom-agent` should be loaded for this ticket.** Not open: it is on the
  do-not-load table in `docs/desktop/12-agent-tooling/skill-routing.md` because it targets the
  VS Code `.agent.md` format rather than Codex TOML, and `EPIC-013/context.md` records that
  where the per-area routing index and the do-not-load table disagree, **the do-not-load table
  wins**. Note that it *is* vendored — "do not load" is not "do not vendor"; see
  [[TOOL-002]] (plan handle `DSK-12-02`) step 10.

## Observation — 2026-08-25

The fresh Codex probe listed all eight custom agents after the `[agents]` table was added. It did not expose runtime enforcement of `sandbox_mode` or `model_reasoning_effort`; the parked question therefore remains parked and no enforcement claim is made.
