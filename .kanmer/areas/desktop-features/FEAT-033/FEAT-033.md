---
id: FEAT-033
type: ticket
title: >-
  DSK-07-07 · Spike: can Box.Sdk.Gen 1.12 issue a short-lived, file-scoped
  downscoped token for direct desktop transfer?
status: backlog
area: desktop-features
assignee: ''
profile: spike
labels:
  - desktop-conversion
  - plan-07
  - phase-6
  - tier-3
groups:
  - EPIC-008
  - HZN-007
links: []
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
archived: false
created: '2026-08-24T08:24:13.878Z'
updated: '2026-08-24T08:24:13.878Z'
---

## What

A timeboxed, written answer to one question: can the Box SDK already referenced by `Pegasus.Infrastructure` exchange the organisational service token for a **short-lived, single-file-scoped** downscoped token that a desktop could use to move bytes directly to and from Box, while the gateway still records canonical metadata and audit? The spike changes no production code; it produces evidence and a recommendation.

## Why

Proposal § 12.2 says file bytes *should* travel directly between desktop and Box **when the current Box authentication model can issue a suitably short-lived, constrained transfer URL**, and otherwise must stream through the gateway — and it explicitly forbids putting a long-lived Box service token on the desktop to save gateway bandwidth. This area's § 2 records the capability as **unverified** and names this ticket as the check; § 3 records the deviation that gateway streaming is the default until the spike says otherwise. [[DSK-07-06]] cannot choose its transfer mode until this answer exists, so this spike is on the Phase 6 critical path.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-07`
- Plan context: `docs/desktop/07-integrations/README.md` § 2 Assumptions (the unverified `Box.Sdk.Gen` 1.12 token-exchange assumption), § 3 Deviations (Box direct transfer)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.2 Box (the "do not place a long-lived Box service token in the desktop" sentence), § 4 cloud-justification test
- Repository evidence: `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` (`Box.Sdk.Gen` 1.12.0); `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs:116-150` (`BoxJwtAuthorizationHeaderProvider`, `JwtConfig`, `BoxJwtAuth.RetrieveAuthorizationHeaderAsync`), `:150-500` (`BoxContentClient` and its root-fenced descendant check); `infra/modules/platform.bicep:382-398,555-556` (where the credentials live)
- Binding decisions: **ADR-0107** — Box credentials stay behind the gateway; nothing this spike recommends may put a provider secret in the desktop package. L-01 — whatever is adopted is brokered by `Pegasus.Web`, not a new deployment unit. L-02 — any experiment runs locally or against the approved live-work profile in `docs/runbook.md` § Optional approved live-work profile; there is no Azure test environment.
- Depends on: `DSK-07-05` the broker endpoints whose default this spike either confirms or challenges

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `microsoft-code-reference` (Microsoft Learn plugin) → `kanmer-research` (`.grok/skills/kanmer-research/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search` — for .NET `HttpClient` token-handling patterns only; Box is not a Microsoft product, so the SDK evidence must come from the assembly and Box's own documentation)
- **Kanmer pipeline** for profile `spike`: `kanmer-research` → `kanmer-verify` → `kanmer-closeout` (the only gate is `enter-done`: `research` plus `questions-resolved`; call `get_doc_gates <id>` before every move)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, this area's § 2 Assumptions and § 3 Deviations, and proposal § 12.2. Call `get_doc_gates <this ticket id>`, then `take_ticket`. Set an explicit timebox in the `research` document (recommend one working session) and record it before starting.
2. Establish what the referenced SDK actually offers. Inspect the assembly in the local NuGet cache: `%USERPROFILE%\.nuget\packages\box.sdk.gen\1.12.0\lib\net6.0\Box.Sdk.Gen.dll`. A `DownscopeTokenAsync` member is present in that build — confirm it, record the declaring type, the full parameter list (scopes, resource, shared link, subject token) and the return type verbatim in `research`. Do not paraphrase the signature.
3. Record the exact scope strings Box supports for a single file (for example the item-read / item-upload family) and whether a `resource` URL may pin the token to one file id. Cite Box's own developer documentation with the fetch date beside each claim, and mark anything you could not confirm as an assumption, per `docs/engineering.md` § Plan sizing.
4. Determine the token lifetime the exchange returns and whether it can be shortened. State the measured or documented value; "short-lived" without a number is not an answer.
5. Write a throwaway probe **outside the tracked source tree** (a scratch console project, deleted before the PR) that calls `BoxJwtAuth` exactly as `BoxJwtAuthorizationHeaderProvider` does (`src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs:119-133`) and then attempts the downscope for one file id. Run it only under the approved live-work profile in `docs/runbook.md` § Optional approved live-work profile, and only if that approval already exists — otherwise record the step as untested and say so.
6. **Operator step** — if the probe needs live Box credentials the agent does not hold, the operator runs it and hands back: the HTTP status, the returned scope list, the `expires_in` value, and confirmation that the token refuses a sibling file in the same folder. No credential value is ever pasted into the ticket.
7. Answer the audit question: if the desktop moves bytes directly, can the gateway still record canonical metadata, the SHA-256 and the action-history entry for that transfer, and can it detect a transfer that Box accepted but the client never reported? Describe the mechanism or state that it cannot be preserved.
8. Answer the security question against ADR-0107: does anything in the proposed flow place a long-lived secret, a reusable URL or a broad-scope token on the workstation or in a log? A "yes" ends the spike with a recommendation of gateway streaming.
9. Run the six-question cloud-justification test from `docs/desktop/00-governance-and-workflow/README.md` § 3 over "Box byte transfer" and record the six answers with evidence, not prose.
10. Write the recommendation into the `research` document with one of exactly two outcomes: **(a) confirm the default** — gateway streaming stands, and record why; or **(b) raise a follow-up ticket** in area 07 for direct transfer, naming the token lifetime, the scope, the audit mechanism and the rollback. Do not implement either outcome in this ticket.
11. Delete the scratch probe, confirm `git status` is clean apart from the ticket documents, and record the timebox actually spent.

## Acceptance criteria

- [ ] A written answer exists with the SDK evidence quoted verbatim (declaring type, member signature, parameters) and each documentation claim dated.
- [ ] Token lifetime and scope are stated as concrete values, not adjectives.
- [ ] The audit implications of direct transfer are stated: what the gateway can and cannot still record.
- [ ] The six cloud-justification answers are recorded for Box byte transfer.
- [ ] The outcome is either "gateway streaming confirmed" or a named follow-up ticket — no third, ambiguous state.
- [ ] No production file changed; `git status` shows only ticket documents.

## Verification

- [ ] `get_ticket_doc <this ticket id> research` — expected: the document contains the SDK signature, the lifetime, the scope list, the six cloud-test answers and the recommendation.
- [ ] `git status --porcelain` — expected: no production source or test file modified.
- [ ] `git diff --stat origin/dev -- src tests` — expected: empty output.

## Evidence tier

Tier 3 — Parser/adapter contracts.
Tier 3 obliges evidence about the adapter contract itself: stable contract codes, deterministic external failures and the exact provider semantics — which here means the token exchange's real signature, scope and lifetime rather than an assumption about them.

## Documentation changes

- None. The answer lives in the ticket's `research` document; a decision that survives becomes an ADR consequence or a follow-up ticket, not a plan edit.

## Guardrails

- **Azure**: no write. Key Vault reads are name-only.
- **Scope boundary**: this ticket writes **no** production code. It may read anything under `src/` and may create a scratch probe outside the tracked tree, deleted before completion.
- **Traps**: ADR-0107 is not negotiable — a recommendation that puts a Box secret, a broad-scope token or a reusable URL on a workstation is a refusal, not a finding; a "temporary" long-lived URL in a log is the same defect; do not enable direct transfer here even if the answer is favourable — that needs its own ticket with its own audit tests; the live-work profile is approval-gated (`docs/runbook.md` § Optional approved live-work profile) and an unapproved live Box call is out of bounds.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only`.

## Outcome

_Filled at closeout._
