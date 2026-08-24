---
id: CASE-002
type: ticket
title: >-
  upstream:CASE-022 · Deliver public upload links (INT-31) to the operator's
  accepted limits
status: backlog
area: case-reference-workflow
assignee: ''
profile: feature
labels:
  - found-during-qa
  - ui
  - design
  - upstream-carryover
  - upstream-CASE-022
  - desktop-screen-spec
  - needs-operator
groups:
  - EPIC-014
links: []
blocks:
  - GWY-011
  - FEAT-014
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
archived: false
created: '2026-08-24T11:42:25.804Z'
updated: '2026-08-24T11:57:54.256Z'
---

## What

Activate the INT-31 public upload-link capability to the limits the operator accepted on 2026-08-24, which means changing the policy contract, not supplying eight configuration numbers. Two of the accepted answers — a **per-link** expiry (a chosen date, or open until cancelled) and **no rate limiting** — are inexpressible in `RequestUploadPolicy`/`RequestUploadLimits` as built and are refused by construction. This ticket owns that Core change, the real byte ceiling (Kestrel's unconfigured `MaxRequestBodySize`, not `IntakeEnvelopeLimits.MaximumContentLength`), joining the anonymous upload route to the existing Box case-document custody path, and the composition switch that takes the capability from `UnavailableDocumentRequestStore` to live.

## Why

The desktop conversion needs this because the capability is composed **closed** and every seeded ticket that would surface it is forbidden from opening it.

The operator's report was *"no method to create an upload link on frontend at all seemingly"*, and the cause is not a missing button. `src/Pegasus.Infrastructure/DependencyInjection.cs:433-441` registers `UnavailableDocumentRequestStore` for all four upload-link ports; that store throws; `src/Pegasus.Web/Program.cs:205-210` only builds a real `RequestUploadLimits` when `DocumentRequests:AcceptedLimitsVersion` is configured, which production does not set; and `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:116` and `:130` pin that closed as a deliberate fact.

The seeded board has already made this ticket its owner and cannot proceed without it:

- [[DSK-03-11]]'s endpoint-map row promises `POST /cases/{id}/request-upload-links` returns "link id + expiry" over a store that throws. Its step 7 resolves that by returning a named `urn:pegasus:problem:provider-unavailable` problem and recording the routes as **inert until this ticket lands**, and its scope boundary states outright that `src/Pegasus.Core/Intake/IntakeContracts.cs` and `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` are *"owned by the imported upstream CASE-022 ticket"* — it reads those constants and does not edit them.
- [[DSK-05-14]]'s acceptance criterion *"Request-upload links can be created and revoked"* cannot be true while the store throws, and [[DSK-03-11]] step 7 requires the same inert-until-CASE-022 statement to be mirrored into its traps so the desktop does not render a working-looking command.

So the operator's complaint survives the conversion untouched unless this row exists. `docs/desktop/06-ui-design/screen-specs.md:230-231` lists CASE-022 among the "Upstream carry-over absorbed" ids as *"make the public upload link findable — Documents tab command"*, which is a plan defect on two counts: the ticket was **retitled and rescoped on 2026-08-24** and is no longer a findability fix, and a screen specification cannot deliver a Core policy contract or a Kestrel request-size limit. The triage disposition on this ticket's labels (`desktop-screen-spec`, from `upstream-kanmer-carryover.md:111`) is carried for provenance and is **stale for the same reason** — treat the fork area (`case-reference-workflow`) and this body as authoritative.

Under **D-001** the fork becomes the single release source at the first production gateway change and upstream is frozen, so nobody upstream will do this work.

## Source of truth

- **Upstream provenance** — ticket `CASE-022`, upstream area `case-reference-workflow`, upstream status `backlog` (unassigned), upstream profile `feature`, upstream labels `found-during-qa`, `ui`, `design`. Created 2026-08-23T15:19:54Z, **updated 2026-08-24T09:46:04Z — the retitle and rescope**. Read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111` on **2026-08-24**. Upstream carries `docs_todo: true`, `links: []`, `deployment: not-deployed`.
- **Upstream pipeline documents**: **none exist upstream** — no `research`, `files`, `plan`, `checklist` or `open-questions`. None was invented, so this ticket enters Backlog with its whole pipeline still to be done. That matters for profile `feature`, which needs `research`, `files`, `plan`, `checklist` and `questions-resolved` to leave Preparing.
- **Triage row**: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:111` — disposition `desktop-screen-spec`, plan area `06 (Documents tab commands)`, fork area `desktop-ui`, title *"Make creating a public upload link findable"*. All four are **stale**: they predate the 2026-08-24 rescope. The import decision places it in `case-reference-workflow` because the deliverable is a Core policy contract plus a Kestrel limit.
- **Import decision**: `FND-022` (`DSK-01-09`) step 3 — *"`case-reference-workflow` (2): … `CASE-022` (`feature`)"*, and the consuming statement already written into [[DSK-03-11]] Source of truth, step 3, step 7, acceptance and scope boundary.
- **Repository evidence (this fork, read 2026-08-24)**:
  - `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:28-80` — the `RequestUploadLimits` constructor: one global `Lifetime` validated `ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lifetime, TimeSpan.Zero)` (`:42`) and `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rateLimit)` (`:46`). "No rate limit" is unrepresentable.
  - `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:440-452` — `HasAcceptedLifetime`, returning `link.ExpiresAtUtc == link.CreatedAtUtc.Add(limits.Lifetime)`. A per-link expiry is refused by construction; `:378` is the redemption path that calls it.
  - `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:6-25` — `RequestUploadStatus` (`Pending`, `Active`, `Expired`, `Exhausted`, `Revoked`, `Failed`) and `RequestUploadDecision` (`Accepted`, `Replay`, `Unavailable`, `RateLimited`, `InvalidFile`, `LimitExceeded`, `OperationConflict`) — the vocabulary an "open until cancelled" link must fit without inventing a state.
  - `src/Pegasus.Web/Program.cs:203-239` — the activation gate: `DocumentRequests:AcceptedLimitsVersion`, the exact-match check against `DocumentRequests:LimitsVersion` (`:224`), and the eight values read from the `DocumentRequests` section (`LifetimeHours`, `MaximumFileCount`, `MaximumFileBytes`, `MaximumRequestBytes`, `AllowedMediaTypes`, `RateLimit`, `RateLimitWindowMinutes`).
  - `src/Pegasus.Web/Program.cs:529` — `options.MultipartBodyLengthLimit = IntakeEnvelopeLimits.MaximumBatchContentLength;`
  - `src/Pegasus.Core/Intake/IntakeContracts.cs:13` — `MaximumContentLength = 10 * 1024 * 1024`; `:34` — `MaximumMailboxContentLength = 750L * 1024 * 1024` with the doc comment recording the 16.69 MB QDOS forward refused as `message_too_large` on 2026-08-05; `:42` — `MaximumBatchFileCount = 20`; `:49-50` — `MaximumBatchContentLength`.
  - `MaxRequestBodySize` — **grepped across `src/` and `infra/` and configured nowhere**, so Kestrel's ~30 MB default is the real ceiling, refusing an oversized request before `MultipartBodyLengthLimit` is consulted.
  - `src/Pegasus.Infrastructure/DependencyInjection.cs:433-441` — `UnavailableDocumentRequestStore` registered for all four ports.
  - `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs:6` — the store that throws.
  - `src/Pegasus.Infrastructure/Persistence/EfDocumentRequestStore.cs` — the real store, already written and unreachable.
  - `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:108-118` — `ProductionProfileKeepsUploadLinksUnavailableWithoutAcceptedLimits`, whose comment reads *"INT-31 is not on the alpha path and its limits are an open decision"*; `:130` — the same assertion in `ProfileWithoutDurableStorageStillFailsClosed`.
  - `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:22-23`, `:186` (`OnPostCreateRequestUploadLinkAsync`), `:237` (`OnPostRevokeRequestUploadLinkAsync`) — the staff commands, over `ICreateRequestUploadLink` / `IRevokeRequestUploadLink`.
  - `src/Pegasus.Web/Pages/Cases/Shared/_CaseDocuments.cshtml:130-160` — the dead controls the upstream body cites as `_CaseDocuments.cshtml:136-167`; the file is at `Pages/Cases/Shared/`, not `Pages/Cases/Documents/`. Cite the verified path.
  - `src/Pegasus.Web/Pages/Uploads/Request.cshtml` and `Request.cshtml.cs` — the anonymous external-audience upload page.
  - `docs/open-decisions.md:59-72` — § *QDOS alpha activation details* item 1, `INT-31` upload-link limits, the item this closes; `docs/open-decisions.md:311-326` — § *Manual upload in a deployed environment*, the ADR-0003 contradiction the Box answer resolves for this route.
  - `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Cases — the `POST /cases/{id}/request-upload-links` and `DELETE …/{linkId}` rows; `docs/desktop/03-gateway-api-and-data/README.md` § 3 row *Bytes & uploads* — *"`Uploads/Request` stays an anonymous Razor page (external audience)"*.
  - `docs/desktop/06-ui-design/screen-specs.md:230-231` — the incorrect "Upstream carry-over absorbed" line naming CASE-022 with its pre-rescope title.
- **Governing document**: `docs/frd/frd-05-documents-extraction-and-custody.md` — the canonical owner the accepted limits move to, per `docs/open-decisions.md`'s own rule that accepted decisions leave the register. A conversion ADR is also owed for the desktop's document surfaces, so `docs_todo` stays `true`.
- **Binding decisions**:
  - **L-01** — the gateway is `Pegasus.Web` evolved in place; the staff create/revoke half reaches the desktop through the `/api/v1` routes [[DSK-03-11]] builds, and no new deployment unit is introduced for uploads.
  - **L-02** — Test/UAT is the local production-mimicking stack; a real large-file upload is proven there, against Azurite and the local custody adapter, not in Azure.
  - **D-001** — the fork is the single release source at the first production gateway change; nobody upstream will do this work, and activation is itself a production gateway change to sequence against the freeze.
  - **C-01** — the repositories become private on completion; anything published about the anonymous upload endpoint must not assume a public GitHub surface.
- **Depends on**: nothing on the fork board blocks it. It is a **precondition** of [[DSK-03-11]]'s request-upload-link routes going live and of [[DSK-05-14]]'s create/revoke acceptance criterion.

### Upstream ticket CASE-022 (verbatim)

Copied exactly from `.kanmer/areas/case-reference-workflow/CASE-022/CASE-022.md` at clone commit `a5b28111`, read 2026-08-24. Not paraphrased, not corrected — where a path in it differs from this fork's tree (`_CaseDocuments.cshtml` lives at `src/Pegasus.Web/Pages/Cases/Shared/`) the Repository evidence above is the authority. The `[[DOCS-012]]` reference inside it is an **upstream** ticket id and does not resolve to any fork ticket.

````markdown
## What the operator saw

> *"**Issue 3** — No method to create an upload link on frontend at all seemingly."*

Correct, and not for the reason first assumed. The capability is composed as a
null implementation that **throws** (`UnavailableDocumentRequestStore`), `/uploads`
returns 404 in production, and a composition test pins it closed. Verified
against the deployed container: no `DocumentRequests__AcceptedLimitsVersion` is
set — only `Runtime__Profile` and `Features__AutomationMcp`. This was never a
missing button.

## The operator has now accepted the limits, 2026-08-24

> *"token lifetime - configurable upon generation. user enters expiration date
> or leaves open (permanent/until cancellation pegasus-side).*
>
> *file size: these limits are too light (10mb too small by far).*
>
> *content type: any standard files we would receive: images, documents, videos
> (rare but still happens), email file types*
>
> *most of this is over-engineering and assuming that our customers are going to
> send us a virus or something which is absurd.*
>
> *box is the destination storage as with all other storage/files/evidence."*

| Question | Answer |
| --- | --- |
| Token lifetime | Chosen per link at generation: an expiry date, **or open** — permanent until cancelled in Pegasus |
| One-time vs reuse | Reuse. A link lives until its expiry or cancellation |
| Revocation | Exists — "until cancellation pegasus-side" |
| Content types | Images, documents, videos, email files — the standard set |
| Destination | **Box**, like all other evidence |
| Byte limits | Far above 10 MB. Exact figure below |
| Rate limits | Over-engineering; not wanted |

## Two things the built policy cannot express

The `RequestUploadPolicy`/`RequestUploadLimits` code is complete and has been
waiting on these values. **Two of the answers contradict its design**, so this is
not a matter of supplying eight numbers.

**1. Per-link expiry is refused by construction.** `RequestUploadLimits` takes a
single global `Lifetime` (`TimeSpan`, validated `> Zero`), and
`HasAcceptedLifetime` rejects any link whose expiry is not *exactly*
`CreatedAtUtc + limits.Lifetime`. An operator-chosen date, and an open-ended
link, are both actively refused today. Making the expiry per-link is a change to
the policy contract.

**2. A rate limit is mandatory.** The constructor throws on a non-positive
`rateLimit`, so "no rate limiting" is not expressible either.

## The size ceiling is not where the constant says

Raising `IntakeEnvelopeLimits.MaximumContentLength` alone will not work.

- `Program.cs` sets `MultipartBodyLengthLimit` to `MaximumBatchContentLength`
  (20 files × 10 MiB + overhead ≈ **200 MiB**).
- **`MaxRequestBodySize` is configured nowhere** in `src/` or `infra/`, so
  Kestrel's ~30 MB default is the real ceiling. A request over that is refused
  before the multipart limit is ever consulted.

So the two limits already disagree, and the effective cap today is ~30 MB — below
anything that would carry a video. Container Apps ingress may impose its own; to
be established rather than assumed.

Precedent for a generous bound exists and is documented:
`MaximumMailboxContentLength` is **750 MB**, deliberately permissive, after a
16.69 MB QDOS forward was refused outright as `message_too_large`.

**Proposed, for correction rather than debate:** per file 250 MB, per request
1 GB, 50 files. These are bounds that stop a runaway request, not a judgement
about senders. The real constraint to establish at plan time is whether the
upload path streams to Box or materialises in memory — that, not a policy
number, decides what is safe.

## Box as destination closes the other open decision

`docs/open-decisions.md` § *Manual upload in a deployed environment* records that
ADR-0003 forbids a deployed upload route until **authenticated intake and
approved durable source custody** exist, and that only the first was met — the
upload path retaining assets *"in ignored local content-addressed storage… not
production Blob staging, Box custody, backup, or retention"*.

The operator's answer settles the custody half: **Box**, the same destination as
every other case file. An upload link is created against a case, and that case
already has a Box folder, so this reuses the existing case-document custody path
rather than inventing a second one. Confirm at plan time that the anonymous
upload route actually joins that path.

## Scope note

The dead upload-request controls at `_CaseDocuments.cshtml:136-167` belong to
this ticket, not [[DOCS-012]]. They stop being dead once this ships.

## Documents

`docs/open-decisions.md` item 1 under *QDOS alpha activation details* closes; the
accepted limits move to their canonical owner (FRD-05), per the register's own
rule that accepted decisions leave it. The § *Manual upload in a deployed
environment* contradiction is resolved for this route and should say so.
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (Core policy contract, composition and the Kestrel limit); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (the policy, persistence, composition and large-file facts); `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only, for the Container Apps ingress ceiling in step 5); `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (independent review; must not be the implementing agent).
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-research` (`.grok/skills/kanmer-research/SKILL.md`) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`) → `kanmer-execute` (`.grok/skills/kanmer-execute/SKILL.md`) → `kanmer-review` (`.grok/skills/kanmer-review/SKILL.md`) at review. No WinUI skill applies — this ticket writes no XAML; the desktop affordance is [[DSK-05-14]]'s.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for `KestrelServerLimits.MaxRequestBodySize` and `IHttpMaxRequestBodySizeFeature` per-endpoint override semantics, and for Container Apps ingress request-size behaviour); Azure MCP **read-only** (`containerapps` to read the deployed ingress and app settings — read only, never write).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. **No upstream pipeline document exists for this ticket**, so `research`, `files`, `plan` and `checklist` are all written from scratch, and the two open questions in step 3 become an `open-questions` document that must be resolved before Preparing is left. Call `get_doc_gates <this ticket id>` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5).

## Implementation steps

1. **Orient.** Read this body in full, including the verbatim upstream block. Read `docs/frd/frd-05-documents-extraction-and-custody.md`, `docs/open-decisions.md:59-72` and `:311-326`, and every repository path listed under Source of truth — in particular `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` end to end, because the policy is complete and the change is to its contract, not to its logic. Then `get_doc_gates <this ticket id>` and `take_ticket` with branch `task/upstream-case-022-upload-links` and worktree `../pegasus-worktrees/upstream-case-022-upload-links` from `origin/dev`.
2. **Establish the real ceiling by measurement, not by reading a constant.** `MaxRequestBodySize` is configured nowhere in `src/` or `infra/`, so Kestrel's ~30 MB default refuses an oversized request before `MultipartBodyLengthLimit` (~200 MiB) is consulted. Prove it: POST a 40 MB body to the upload route on the local stack and record the status and where it was refused. Use `microsoft_docs_search` for `KestrelServerLimits.MaxRequestBodySize` and the per-endpoint `IHttpMaxRequestBodySizeFeature` override before choosing between a global raise and a route-scoped one. Then use the Azure MCP `containerapps` **read** tool to record the deployed ingress request-size behaviour. Done looks like: the effective ceiling written into the research document with the evidence for each layer.
3. **Record the two open questions and get them answered before planning ends** — profile `feature` needs `questions-resolved` to leave Preparing, so these belong in an `open-questions` document, not in prose. **(a)** Does the upload path stream to Box or materialise in memory? The upstream body is explicit that this, not a policy number, decides what is safe; answer it by reading the write path from `Pages/Uploads/Request.cshtml.cs` through to the custody adapter and record which it is. **(b)** Confirm the byte figures. The upstream proposal is 250 MB per file, 1 GB per request, 50 files, offered *"for correction rather than debate"* — put those three numbers to the operator with the measured ceiling from step 2 beside them, and record the answer.
4. **Change the policy contract so the accepted answers are expressible.** In `src/Pegasus.Core/Documents/RequestUploadPolicy.cs`: make the expiry **per link** — a link carries its own `ExpiresAtUtc`, chosen at generation, or **none at all** for an open link that lives until cancelled — replacing `HasAcceptedLifetime`'s exact-equality rule at `:440-452` with a validity rule that accepts a stored per-link expiry and treats a null expiry as unexpiring; and make the rate limit **optional**, replacing `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rateLimit)` at `:46` with a representation of "no rate limit" so the guard at `:389-390` is skipped rather than tripped. Do not invent a new `RequestUploadStatus` value for the open case — `Active`, `Expired` and `Revoked` already carry it. Keep every other guard (media types, file count, byte bounds, replay, operation conflict) exactly as it is. Done looks like: `dotnet build --configuration Release` succeeds and every existing `RequestUploadPolicy` fact still compiles or has been deliberately and visibly updated.
5. **Raise the byte bounds where they actually bind.** Apply the figures confirmed in step 3(b): the per-file and per-request values through the `DocumentRequests` configuration section that `Program.cs:229-238` reads, and the request-body ceiling by configuring `MaxRequestBodySize` — globally, or scoped to the upload endpoints if step 2's documentation lookup shows the per-endpoint override is the safer shape. `IntakeEnvelopeLimits.MaximumContentLength` (10 MiB) governs the **staff multipart form**, not this route; change it only if step 3 shows the two genuinely share a path, and record the reason if you do. Do not copy any limit into an endpoint — [[DSK-03-11]]'s acceptance requires the values to be readable from the constants so a later raise takes effect without editing endpoint code.
6. **Join the anonymous upload route to the real Box custody path.** The operator's answer settles ADR-0003's outstanding condition: Box, the same destination as every other case file. An upload link is created against a case and that case already has a Box folder, so reuse the existing case-document custody path — confirm the anonymous route actually reaches it, and do **not** build a second custody path. Assert the round trip: a file uploaded through a link becomes a case document version with confirmed custody, visible through the same document projection the case surfaces read.
7. **Open the composition, honestly.** Register `EfDocumentRequestStore` in place of `UnavailableDocumentRequestStore` (`src/Pegasus.Infrastructure/DependencyInjection.cs:433-441`) for the profile in which the accepted limits are set. Update `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:108-118` so `ProductionProfileKeepsUploadLinksUnavailableWithoutAcceptedLimits` still asserts fail-closed **without** an accepted limits version — that guarantee must survive — and add the mirror fact that **with** one, the real store composes. Leave `ProfileWithoutDurableStorageStillFailsClosed` (`:130`) untouched: without durable storage it must still fail closed.
8. **Re-express the operator-facing half against the desktop, because the upstream ticket's scope note points at Razor markup the conversion deletes.** The upstream scope note claims `_CaseDocuments.cshtml:136-167` (verified in this fork at `src/Pegasus.Web/Pages/Cases/Shared/_CaseDocuments.cshtml:130-160`). Those dead controls are not brought back to life here: the **staff** create/revoke half reaches the operator through the `/api/v1` routes [[DSK-03-11]] builds and the Documents tab [[DSK-05-14]] renders, so this ticket delivers the capability behind those routes and records in its plan that the affordance is theirs. The **external** half stays where it is — `docs/desktop/03-gateway-api-and-data/README.md` § 3 keeps `Uploads/Request` as an anonymous Razor page for an external audience, which the desktop conversion does not replace. State this substitution explicitly in the plan document so a reader of the verbatim upstream text is not misled into editing the partial.
9. **Prove the policy in Core.** Add facts to `tests/Pegasus.Core.Tests`: a link with an operator-chosen expiry redeems before it and is refused after; an open link with no expiry redeems indefinitely and is refused once revoked; with no rate limit configured, repeated redemptions are not `RateLimited`; with one configured, the existing behaviour is unchanged; each of `InvalidFile`, `LimitExceeded`, `Replay` and `OperationConflict` still returns its own decision. Done looks like: every new fact fails against `origin/dev` and passes on this branch.
10. **Prove it end to end on the local stack (L-02).** In `tests/Pegasus.IntegrationTests`: create a link over a real case, redeem it anonymously with a file at the new per-file bound, and assert the file lands as a case document version with confirmed custody; assert a file over the bound is refused with the named decision and not a raw framework error; assert a revoked link is refused. There is no Azure test environment — this is the only environment the behaviour is proven in.
11. **Operator step — ⚠ Azure write, and it is the last step, not the first.** Activation in production needs `DocumentRequests__AcceptedLimitsVersion` plus the `DocumentRequests` section (and, if step 5 chose a global raise, the request-body limit) set on the deployed Container App. That is a production configuration write: it needs exact-target approval under `docs/runbook.md` § *Live-operation approval matrix* and must be mirrored in `docs/desktop/11-azure-disposition/README.md`. Under **D-001** it is also a production gateway change, so sequence it against the freeze deliberately. The operator must hand back: the approval text naming the exact resource, the values set, the date, and a post-change check that `/uploads` no longer returns 404. **Do not perform this write from an agent.** If approval is not given, the ticket still lands the code and the composition, with production activation recorded as pending.
12. **Documentation.** Close `docs/open-decisions.md` item 1 under § *QDOS alpha activation details* and move the accepted limits to their canonical owner `docs/frd/frd-05-documents-extraction-and-custody.md`, per the register's own rule that accepted decisions leave it; and record in § *Manual upload in a deployed environment* that the ADR-0003 durable-custody condition is met for this route by Box. Do **not** edit ADR-0003 — an ADR body is immutable and amendable only on an explicit operator instruction. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, then open the PR into `dev`.

## Acceptance criteria

- [ ] A link can be created with an operator-chosen expiry date, **or** with no expiry at all (open until cancelled in Pegasus), and both are accepted by `RequestUploadPolicy` rather than refused by `HasAcceptedLifetime`.
- [ ] "No rate limiting" is representable and honoured; a configured rate limit still behaves exactly as it does today.
- [ ] A link is reusable until its expiry or its revocation, and revocation works — the four accepted answers on lifetime, reuse, revocation and content types are each asserted by a test.
- [ ] The effective request ceiling is measured across Kestrel, the multipart limit and Container Apps ingress, recorded with its evidence, and raised to the figures confirmed with the operator; a file at the new per-file bound uploads successfully.
- [ ] A file uploaded through a public link lands in **Box** through the existing case-document custody path, as a case document version with confirmed custody — no second custody path exists.
- [ ] `ProductionCompositionTests` still proves the capability fails closed **without** an accepted limits version, and additionally proves the real `EfDocumentRequestStore` composes **with** one; `ProfileWithoutDurableStorageStillFailsClosed` is unchanged.
- [ ] The two open questions — streaming versus in-memory, and the confirmed byte figures — are answered and recorded, not assumed.
- [ ] The plan document records that the staff affordance is delivered by [[DSK-03-11]] and [[DSK-05-14]] and that `Uploads/Request` stays an anonymous Razor page, so no dead Razor control is revived.
- [ ] `docs/open-decisions.md` item 1 is closed and the accepted limits live in FRD-05; ADR-0003 is not edited.
- [ ] Production activation is either performed under recorded exact-target approval or explicitly recorded as pending — never silently skipped.

## Verification

- [ ] `dotnet build ./Pegasus.slnx --configuration Release` — expected: build succeeds with no new warnings.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build` — expected: the per-link expiry, open-link, no-rate-limit and unchanged-guard facts pass; `QdosBoundaryContractTests` stays green.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: the create/redeem/revoke round trip lands a custody-confirmed case document version; `ProductionCompositionTests` passes with both the fail-closed and the composes-when-accepted facts.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: green; the one-Core-owner rule still holds and no second custody path was introduced.
- [ ] A manual large-file upload on the local Test/UAT stack at the new per-file bound — expected: accepted; and one byte over the request bound — expected: refused with the named decision, not an unhandled framework error. Record the observed status codes.
- [ ] `grep -rn "MaxRequestBodySize" src/ infra/` — expected: the limit is now configured in exactly one place, with a comment naming this ticket and the measured ceiling.
- [ ] Operator record in the ticket `proof` — expected: the confirmed byte figures, and either the exact-target approval text with the date for the production configuration write, or an explicit "activation pending".

## Evidence tier

Tier 2 — Core/domain. Tier 4 — LocalDB persistence. Tier 5 — Web/API/MCP caller. Tier 9 — Security/observability. Tier 10 — Performance/concurrency.
Tier 2 obliges positive, contradictory and failure cases for the changed policy contract — chosen expiry, open expiry, no rate limit, and every guard that must not change. Tier 4 obliges the link and its redemptions surviving as real rows with their constraints and idempotency intact. Tier 5 obliges the anonymous route reaching Core with validation, idempotency and exception translation observable, and the composition switch proven at the route. Tier 9 obliges the anonymous surface being examined deliberately: no case disclosure through the token, denial before any client is constructed, and the fail-closed guarantee preserved when limits are not accepted. Tier 10 obliges evidence at the raised bounds — a file at the new per-file limit and a request at the new request limit — since the whole point of the change is that 10 MB was too small.

## Documentation changes

- `docs/open-decisions.md` — close item 1 under § *QDOS alpha activation details* (`:59-72`); record in § *Manual upload in a deployed environment* (`:311-326`) that the ADR-0003 durable-custody condition is met for this route by Box.
- `docs/frd/frd-05-documents-extraction-and-custody.md` — the accepted limits move here as their canonical owner, with the lifetime, reuse, revocation, content-type, destination and byte rules stated.
- `docs/desktop/11-azure-disposition/README.md` — mirror the conditional production configuration write from step 11 with its approval text and rollback.
- Ticket `plan` document — the measured ceiling, the two answered open questions, the desktop/gateway substitution recorded in step 8, and the dated `## Simplification pass`.
- `docs/desktop/06-ui-design/screen-specs.md:230-231` — the "Upstream carry-over absorbed" line must stop naming CASE-022 and stop describing it by its pre-rescope title. **Not changed here**: [[DSK-03-07]] owns that edit and [[DSK-06-13]] adopts the section into FRD-13.
- `docs/adr/0003-pdfpig-for-first-qdos-slice.md` — **no change**. An ADR body is immutable and amendable only on an explicit operator instruction.

## Guardrails

- **Azure**: ⚠ **Azure write** at step 11 only — setting `DocumentRequests__AcceptedLimitsVersion`, the `DocumentRequests` section and (if chosen globally) the request-body limit on the **deployed production Container App** hosting `Pegasus.Web`. Exact-target approval is required under `docs/runbook.md` § *Live-operation approval matrix* and the write is mirrored in `docs/desktop/11-azure-disposition/README.md`. It is an **operator step**: no agent performs it. Every other Azure interaction in this ticket is read-only (`containerapps` read for the ingress ceiling). Nothing is deprovisioned.
- **Scope boundary**: may touch `src/Pegasus.Core/Documents/RequestUploadPolicy.cs`, `src/Pegasus.Infrastructure/DependencyInjection.cs` (the four upload-link registrations only), `src/Pegasus.Infrastructure/Persistence/EfDocumentRequestStore.cs`, the Kestrel and `DocumentRequests` configuration in `src/Pegasus.Web/Program.cs`, `src/Pegasus.Core/Intake/IntakeContracts.cs` (only if step 3 proves the staff form and this route share a path, with the reason recorded), `tests/Pegasus.Core.Tests`, `tests/Pegasus.IntegrationTests` and the documents listed above. Must **not** touch `src/Pegasus.Web/Api/**` or `src/Pegasus.Contracts/**` — [[DSK-03-11]] owns the `/api/v1` routes; must not touch any desktop project — [[DSK-05-14]] owns the affordance; must not touch `src/Pegasus.Worker`; must not revive the dead controls in `src/Pegasus.Web/Pages/Cases/Shared/_CaseDocuments.cshtml`; must not edit `docs/adr/0003-pdfpig-for-first-qdos-slice.md`.
- **Blocks (this must land before these can correctly ship)**: [[DSK-03-11]] — its request-upload-link routes return the named `provider-unavailable` problem and are inert until this activates INT-31, and its scope boundary hands `RequestUploadPolicy.cs` and `IntakeContracts.cs` to this ticket; [[DSK-05-14]] — its acceptance criterion *"Request-upload links can be created and revoked"* cannot be true while the store throws, and its traps carry the inert-until-CASE-022 statement. A later pass wires these as `blocks` links; this ticket does not.
- **Blocked by**: nothing on the fork board. The two open questions in step 3 are the only gate, and one of them needs the operator.
- **Stale triage disposition**: the `desktop-screen-spec` label and the `upstream-kanmer-carryover.md:111` row ("Make creating a public upload link findable", plan area 06, fork area `desktop-ui`) both predate the 2026-08-24 retitle and rescope. They are carried for provenance only. This is a Core policy contract plus a Kestrel limit; it is not a screen specification and it is not `desktop-ui` work.
- **Traps**: do **not** "just supply eight numbers" — two of the accepted answers are refused by the built contract, and setting `DocumentRequests:AcceptedLimitsVersion` without the contract change activates a capability that then rejects every operator-chosen expiry. Do **not** raise `IntakeEnvelopeLimits.MaximumContentLength` expecting the ceiling to move; `MaxRequestBodySize` is the real refusal point and it is configured nowhere. Do **not** weaken the fail-closed guarantee: the capability must still be unavailable when no accepted limits version is set, and `ProfileWithoutDurableStorageStillFailsClosed` must stay untouched. Do **not** build a second custody path — Box, through the existing case-document path, is the operator's answer. Runtime-role grants: if any new table is introduced it needs a `Grant*` migration mirrored in `scripts/Invoke-AzureDatabaseBootstrap.ps1` and enforced by `scripts/Test-MigrationGrants.ps1` in CI — "works locally, fails only in production" has shipped three times (upstream PLAT-035). Anonymous surface: no case detail may be disclosed through the token.
- **Pipeline note**: upstream has **no** pipeline documents for this ticket, so `research`, `files`, `plan` and `checklist` are written from scratch and the `open-questions` document from step 3 will legitimately block the move out of Preparing until both questions are answered. That is correct, not a fault.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket plan document.

## Outcome

_Filled at closeout._
