---
id: CASE-003
type: ticket
title: >-
  Triage authority · Define the T- aggregate, custody transfer, and Case
  conversion
status: done
area: case-reference-workflow
order: 170
assignee: codex-mcp-client
profile: feature
stageEntered:
  implementing: '2026-08-25T12:10:29.252Z'
  review: '2026-08-25T12:15:11.021Z'
  verifying: '2026-08-25T14:44:44.466Z'
  done: '2026-08-26T15:46:02.897Z'
labels:
  - triage
  - governing-docs
  - operator-decision
  - desktop-conversion
links:
  - FEAT-011
  - INTK-007
blocks:
  - FEAT-011
  - INTK-007
refs:
  - docs/prd/pegasus-product.md
  - docs/frd/frd-01-case-identity-and-lifecycle.md
  - docs/frd/frd-03-triage.md
commits:
  - 57619531835f58c2ea04f887c8131e5098e9f750
prs:
  - '5'
archived: false
created: '2026-08-25T12:04:56.725Z'
updated: '2026-08-30T04:01:23.484Z'
---

## What

Reconcile the authoritative Triage model before any Triage implementation: Triage is a separate aggregate and a case in its own product right, with immutable `T-00001`-style identity, its own evidence/custody and distinct setup requirements. It is not the normal `Case` aggregate and receives no Case/PO or Principal allocation until a later accepted formal instruction.

On normal acceptance, principal and allocation gates, create a linked standard Case and transfer the Triage evidence into that Case's custody. Retain an immutable transfer record containing source Triage identity, transfer time, actor/system, destination Case identity, and transferred content/version identities. Do not retain duplicate evidence copies.

## Operator decision

The operator approved this target state on 2026-08-25:

- Triage identity is a separate immutable `T-00001`, `T-00002`, ... sequence.
- The Core model is a separate aggregate sharing services where appropriate.
- Triage evidence begins in Triage custody and moves into normal Case custody on conversion.
- A formal instruction follows the normal acceptance, principal and allocation gates before conversion.
- The conversion preserves an immutable transfer record, not duplicate copies.

This ticket owns the resulting change to protected operator truth and the authoritative product/functional documents. It does not implement the aggregate, migration, gateway, desktop UI, or any external operation.

## Sources and conflicts

- `docs/operator-notes.md` Stage 0 currently says Triage does not technically count as a Case and is pre-Case.
- `docs/frd/frd-03-triage.md` currently says Triage creates no Case and is a separate pre-Case workflow.
- `docs/prd/pegasus-product.md` and `docs/frd/frd-01-case-identity-and-lifecycle.md` establish the normal Case/PO identity/allocation boundary.
- [[FEAT-011]] and [[INTK-007]] rely on FRD-03, so they must not implement against the contradictory pre-Case model.

## Acceptance criteria

- [ ] `docs/operator-notes.md`, `docs/prd/pegasus-product.md`, `docs/frd/frd-03-triage.md`, and affected FRD-01 identity/lifecycle text express one non-contradictory Triage model.
- [ ] The documents define the immutable Triage identity, its separate aggregate/custody boundary, and conversion only after normal formal-instruction acceptance.
- [ ] The documents define the immutable, non-duplicating custody-transfer record and its required fields.
- [ ] The documents identify implementation follow-ups without smuggling code, schema, or desktop behaviour into this documentation task.
- [ ] Required link/placement validation and independent review pass.

## Scope and guardrails

Documentation only. This ticket has explicit operator authority to amend the meaning of `docs/operator-notes.md`; preserve the historic decision context while recording the new binding target state. Do not make an Azure, mailbox, Box, release, repository-setting, or branch write.
