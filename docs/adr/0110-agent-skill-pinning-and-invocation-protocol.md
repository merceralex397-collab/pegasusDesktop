---
id: ADR-0110
status: accepted
date: 2026-08-24
supersedes: []
superseded_by: []
related_capabilities: []
related_frd: []
tags: [agent-tooling, skills, reproducibility]
---

# ADR-0110: Agent-skill pinning and invocation protocol

## Status

Accepted on 2026-08-24.

## Context

Agent skills are development playbooks, not application libraries or policy
owners. Fetching mutable instructions from a moving upstream branch makes a
reviewed change irreproducible: two agents can follow different guidance while
claiming the same skill. The project needs one reviewed record of exact sources,
selected paths, and content hashes, plus a consistent invocation and independent
review protocol.

The conversion plan contains a draft lockfile and routing material. The accepted
decision does not claim that the future vendored tree, lockfile, or CI verifier
already exists; their implementation and proof remain the owned follow-up work.

### Cloud-justification test

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | No | Skills are reviewed text files in the repository, not runtime shared state. |
| Unattended execution — must it run with every desktop closed? | No | Agents read skills during local task execution. |
| Protected credentials — long-lived secret that must not sit on workstations? | No | A skill lock records public revision and content information, not service credentials. |
| Public callback — must an external service call a stable public endpoint? | No | Skill use has no inbound endpoint. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | No | Git review and CI verification enforce the repository policy; no cloud service is needed. |
| Measured operational advantage — measured evidence central is materially better? | No | There is no demonstrated runtime benefit to a hosted skill service. |

All six answers are no, so skills remain vendored repository material and local
tooling, not a cloud dependency.

## Decision

Agent skill sources are pinned by full commit SHA and vendored only through a
reviewed change. The lock record must name source repository, commit, selected
skill path, local destination, content hash, review date, owner, and reason.
Agents load only the project skill and the applicable vendored `SKILL.md` files;
they do not clone or fetch a moving upstream branch at execution time.

The initial source pins are:

- `dotnet/skills` — `98f848512e9ee4877e399a0ae367bb5e4a193144`;
- `microsoft/win-dev-skills` — `f1028dd5bb19af59df400cb4a2ab867e40a40a4a`;
- `microsoft/azure-skills` — `1a03acfb9ac1a1a05518bf7420d4618cc41847be`.

For each implementation ticket, the agent reads the project skill, reads the
exact relevant vendored skills, records only applicable guidance in its plan,
identifies conflicts with Pegasus ADRs, implements the smallest vertical slice,
runs prescribed and project verification, and records skills/SHAs, commands,
results, artifacts, and deviations. The reviewer independently loads the same
skills and checks dependency boundaries, native/XAML use, async/UI-thread safety,
accessibility, packaging/update implications, API/data compatibility, test
evidence, and cloud-placement justification.

## Consequences

- A skill update is a reviewed change that updates the pin and reruns the
  synchronization and hash verification; it is never an implicit background
  refresh.
- The future `eng/skills/skills.lock.json` and vendored tree are implementation
  evidence for this decision, not runtime dependencies and not substitutes for
  product requirements or ADRs.
- The project skill routes work to the smallest relevant upstream skill set;
  unrelated generic guidance cannot override Pegasus authority.
- No Azure resource or hosted instruction service is created by this decision.

## Options considered

- **Fetch latest skills during every task:** rejected because it makes review and
  reproduction dependent on a changing external branch.
- **Vendor skills without immutable pins or content hashes:** rejected because
  a local copy cannot then be tied to reviewed source material.
- **Treat skills as application dependencies:** rejected because they are
  development instructions and must never own runtime policy.

## Links

- [Agent-tooling conversion plan](../desktop/12-agent-tooling/README.md)
- [Draft skill lockfile](../desktop/12-agent-tooling/skills.lock.draft.json)
- [Skill routing](../desktop/12-agent-tooling/skill-routing.md)
- [Conversion governance and ADR set](../desktop/00-governance-and-workflow/README.md)
- [Repository ADR conventions](../../AGENTS.md#adr-conventions)
