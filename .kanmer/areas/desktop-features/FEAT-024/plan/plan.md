# Plan — FEAT-024: Retire `CaseMutationPageModel` state machine for desktop paths

**Diff estimate: ~7 files, ~520 lines.** Derived from the inventory below: 3 new reflection facts in
`tests/Pegasus.ArchitectureTests` (~220 lines, in the style of `DependencyDirectionTests.cs`),
2 view-model test files in `tests/Pegasus.Desktop.ViewModelTests` (~180), 1 documentation file
(`docs/frd/frd-13-desktop-operator-experience.md`, ~40), 1 `reuse-map.md` row edit (~5), plus the
removal of any equivalent the step-3 audit finds in `src/Pegasus.Desktop*` (~75, and legitimately
zero if the audit finds none — in which case the estimate falls to ~445 lines). **No file in
`src/Pegasus.Web` is edited.**

**Chore inventory** — this profile owes no `research` or `files` document, so the measured surface
area is stated here (`docs/engineering.md` § plan sizing: a real inventory, not an assertion). All
measurements at `bbd1c549` (`git rev-parse --short HEAD`, 2026-08-24).

| Path | Measured today | Role in this ticket |
| --- | --- | --- |
| `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` | **339 lines** (`wc -l`). `public abstract partial class CaseMutationPageModel(ILogger logger) : StaffPageModel` at **`:18`**; its own class comment at `:13` names "the CASE-27 edit-mode state that travels through TempData". | **Read only.** Not modified, not deleted — [[FEAT-026]] (plan handle `DSK-05-26`) deletes it after cutover. |
| — the two budgets | `MaximumRetainedProposedCharacters = 8000` at **`:38`**, `MaximumRetainedProposedValueCharacters = 2000` at **`:39`**, with the rationale at `:31-37`: cookie `TempData` chunks across cookies, "so the ceiling is a deliberate budget rather than a hard 4 KB wall". Enforced at `:218`, `:226` and `:236-240`. | The shapes the architecture facts must recognise. |
| — the field allow-list | `private static readonly FrozenSet<string> RetainableFormFields` at **`:46`**, closing at `:91` (`.ToFrozenSet(StringComparer.Ordinal)`), containing **43 field names** (`grep -c '^        "'` over the initializer). Consumed at `:204` (`.Where(field => RetainableFormFields.Contains(field.Key))`). *(The area plan describes it as "about thirty names"; the measured count at this revision is 43.)* | The second shape. |
| — the **second** allow-list | `protected static readonly FrozenSet<string> BooleanFormFields` at **`:97`**, closing at `:106`, containing **7 checkbox field names**, consumed at `:210`. The ticket body names only `RetainableFormFields`; this one is the same shape and must not slip past the fact. | A third shape the architecture fact must catch. |
| — `TempData` writes | `TempData["CaseStatus"]` at `:158`, `TempData["CaseError"]` at `:170`, `TempData[LeaseCaseIdKey]` / `[LeaseTokenKey]` at `:186-187`, `TempData[ProposedValuesCaseIdKey]` at `:234`, and the drop/shorten keys at `:238-240`. | The status-passing mechanic the desktop replaces with view-model state. |
| PRG across the page estate | **65** `RedirectToPage` calls across **27** page models, and `TempData` in **29** page models — measured with `grep -rho "RedirectToPage" src/Pegasus.Web/Pages --include=*.cshtml.cs \| wc -l` (65), `grep -rl … \| wc -l` (27), and the same for `TempData` (29). These reproduce plan 05 § 2's figures exactly. | Context for the two-column table at step 2. **Untouched.** |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | **520 lines**, reflection-based, in a project of 11 `.cs` files | The style to extend; not modified. |
| `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure` | **Do not exist yet** (`ls src/` returns four projects). Created by [[FND-030]] and [[FND-031]] (plan handles `DSK-02-05`, `DSK-02-06`). | The audit target at step 3 and the subject of the new facts. |
| `tests/Pegasus.Desktop.ViewModelTests` | Does not exist yet; scaffolded by [[FND-038]] (plan handle `DSK-02-13`) and [[TEST-004]] (plan handle `DSK-08-04`) | Gains the two positive facts. |

## Approach

Write the rule down as a **two-column table** (web mechanism → desktop equivalent), audit the merged
desktop code for any equivalent that crept in, remove what is found, and then make the rule
enforceable with reflection facts whose failure messages name the offending type. Two positive
view-model facts prove the intended behaviour rather than only its absence: after a failed save the
proposed values are still in the view model untruncated, and a save outcome is rendered from
view-model state rather than carried across a navigation.

Rejected: **documenting the rule in FRD-13 and relying on review.** The ticket's own Why states the
failure mode — "an agent implementing a later editing slice will reinvent a retention budget by
analogy" — and a review that must catch an analogy on every future slice will eventually miss one.
Also rejected: **deleting `CaseMutationPageModel.cs` now.** The web keeps its state machine until
cutover; deleting it here would strand 27 page models and pre-empt [[FEAT-026]].

## Governing docs

The ticket's `refs` is `docs/frd/frd-01-case-identity-and-lifecycle.md`, which exists.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-01 § case editing and versioning | The authoritative guard on a concurrent edit is the server's version and edit lease, not a client-side retention of what was typed | Step 4 (view-model state plus the server lease and version replace every retained-proposed-value mechanism), Step 6 |
| FRD-01 § case identity | A case's recorded state is what the server holds; a client-held draft is never authority | Step 4 (a genuine draft need uses the encrypted local draft with a documented lifetime, not a character budget) |

`docs_todo: true`, confirmed in `get_doc_gates FEAT-024`. Profile `chore`: `leave-preparing`
requires `plan` and `questions-resolved`; `enter-done` requires `proof`.

> **New ADR** — ADR-0104 (online-required; no offline replication; bounded local cache only),
> authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:159`); if the ADR lands
> differently this plan is revised before implementation. ADR-0103 (gateway; the server lease and
> version are the authority for edit safety) is authored by the same ticket and is the reason the
> desktop needs no retention budget at all.

Programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 11.1 What may be cached locally | Local drafts are bounded and encrypted | Step 4 |
| Proposal § 11.2 What should not become a local database initially | No local replication of case state | Steps 3–5 |
| Proposal § 14.5 Case workspace | Deliberate save; dirty state held in the view model | Steps 6–7 |
| Plan 05 § 3 ("Characterization before moving any rule") | TempData-retained proposed values, PRG and antiforgery are deliberately **not** preserved — they are web mechanics, not business behaviour | Step 2 |
| `reuse-map.md` (`Pages/Cases/CaseMutationPageModel.cs` row) | REPLACE; the web keeps it until cutover | Step 8, § Approach's rejected alternative |
| `docs/engineering.md` § Required evidence tiers (1, 7) | Tier 1 proves consistency only — exactly what this rule needs; tier 7 obliges evidence from a real run that the edit and error behaviour the rule protects actually holds | Steps 5–7, § Verification |
| `AGENTS.md` § Simplicity rails | Do not reproduce a mechanism by analogy where the reason for it has gone | § Approach |
| L-01 | The server lease and version are the authority for edit safety | Step 4 |
| L-04 | Routing named on the ticket | § Routing |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml` (independent review of the boundary)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` →
  `kanmer-closeout` (call `get_doc_gates <id>` before every move; `chore` needs `plan` and
  `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refining the ticket body's nine steps. Body step numbers in brackets.

1. **[body 1] Orient and take.** Read the plan row, the `reuse-map.md`
   `CaseMutationPageModel.cs` row and § 3 of the area plan. Call `get_doc_gates FEAT-024`, then
   `take_ticket` with branch `task/dsk-05-24-retire-mutation-state` and worktree
   `../pegasus-worktrees/dsk-05-24-retire-mutation-state` from `origin/dev`.
2. **[body 2] Write the two-column table into this plan.** Read
   `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` in full and record the SHA read. The
   table, with the measured evidence already gathered:

   | Web mechanism | Where | Desktop equivalent |
   | --- | --- | --- |
   | Cookie `TempData` retention of proposed values | `:186-187`, `:234`, `:238-240` | View-model state, held for the lifetime of the edit session |
   | `MaximumRetainedProposedCharacters = 8000` | `:38`, enforced `:236-240` | *(nothing — the constraint was the cookie, and there is no cookie)* |
   | `MaximumRetainedProposedValueCharacters = 2000` | `:39`, enforced `:218`, `:226` | *(nothing; values are not truncated)* |
   | `RetainableFormFields` allow-list, **43 names** | `:46-91`, consumed `:204` | *(nothing — every bound field is already in memory)* |
   | `BooleanFormFields` allow-list, **7 names** | `:97-106`, consumed `:210` | *(nothing — a bound `bool` needs no trailing hidden field)* |
   | PRG redirect after a command | 65 `RedirectToPage` calls across 27 page models | The navigation guard plus an in-place command result |
   | Antiforgery token | `[ValidateAntiForgeryToken]` on the mutation pages | The bearer token from area 04 |
   | `TempData["CaseStatus"]` / `["CaseError"]` status passing | `:158`, `:170` | An `InfoBar` outcome rendered from view-model state |
   | `TempData[LeaseCaseIdKey]` / `[LeaseTokenKey]` | `:186-187` | The server lease and version from [[FEAT-005]] (plan handle `DSK-05-05`) |

3. **[body 3] Audit the merged desktop code.** Search `src/Pegasus.Desktop` and
   `src/Pegasus.Desktop.Infrastructure` for: a retention character budget (an `int` constant whose
   name contains `Retain`, `Max…Characters` or similar); a field allow-list of the
   `FrozenSet<string>` / `HashSet<string>`-of-field-names shape — **both** the `RetainableFormFields`
   and the `BooleanFormFields` shape; a redirect-style navigation after a save; and an outcome
   passed through navigation parameters instead of view-model state. Record **every** hit, including
   none.
4. **[body 4] Remove each hit.** Replace it with view-model state plus the server lease and version
   from [[FEAT-005]] and the recovery pattern from [[FEAT-008]] (plan handle `DSK-05-08`). Where an
   unsaved-draft need is genuine, it uses the encrypted local draft from [[FND-031]] (plan handle
   `DSK-02-06`) with an explicit, documented lifetime — **not** a character budget copied from the
   web. Proposal §11.1 bounds and encrypts local drafts; §11.2 forbids local replication of case
   state.
5. **[body 5] The architecture facts.** Extend `tests/Pegasus.ArchitectureTests` with
   reflection-based facts in the style of `DependencyDirectionTests.cs` (520 lines), asserting that
   no type in `Pegasus.Desktop` or `Pegasus.Desktop.Infrastructure`:
   - references an ASP.NET `TempData` / `ViewData` type;
   - declares a member whose name matches a retained-proposed-value budget pattern;
   - declares a field allow-list constant of the `RetainableFormFields` **or** `BooleanFormFields`
     shape.
   Each fact **fails with a message naming the offending type and pointing at this ticket** — an
   architecture test that cannot name what broke is not useful.
6. **[body 6] Prove the intended behaviour positively.** A view-model test in
   `tests/Pegasus.Desktop.ViewModelTests`: after a failed save the proposed values are **still
   present in the view model** — not re-fetched, not truncated — and after a deliberate discard they
   are gone. This is the fact that distinguishes "we removed the budget" from "we removed the
   retention".
7. **[body 7] Prove the no-PRG rule positively.** A view-model test that a save outcome is rendered
   from view-model state, not carried across a navigation: navigating away and back **re-queries**
   rather than restoring a message.
8. **[body 8] Confirm the web is untouched.** `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs`
   and all 27 PRG page models keep working exactly as before; the 65 `RedirectToPage` calls and the
   `TempData` use in 29 page models are unchanged. Deletion is [[FEAT-026]]'s (plan handle
   `DSK-05-26`) job after cutover. `git diff --stat` must show no file under `src/Pegasus.Web/`.
9. **[body 9] Record the rule, simplify, PR.** Write the rule and its rationale into
   `docs/frd/frd-13-desktop-operator-experience.md` so a later slice author reads it before
   inventing a retention budget, mark the `CaseMutationPageModel.cs` row in
   `docs/desktop/05-implementation-and-migration/reuse-map.md` as enforced for desktop paths, run
   the simplification pass over the branch diff under a dated `## Simplification pass` heading, and
   open the PR into `dev`.

## Verification

Evidence tiers from the body: **1** (Static/build/architecture) and **7** (Browser/accessibility).

- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`
  — the new no-`TempData`, no-budget and no-allow-list facts pass, and the existing
  dependency-direction facts stay green.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — the failed-save retention fact and the no-PRG-outcome fact pass.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"`
  — the existing web mutation tests are **unchanged** and green.
- **Deliberate-regression check, recorded in the proof** — temporarily add a budget constant to a
  desktop type; the new architecture fact must fail **with the actionable message**; revert before
  commit. Record the failure message verbatim: a fact that fails without naming the type is the
  defect this check exists to catch.
- `git diff --stat` — no file under `src/Pegasus.Web/`.

Evidence that becomes `proof`: the three test outputs, the deliberate-regression failure message,
and the `git diff --stat` showing the web untouched.

## Risks / open questions

- **The audit may find nothing, and that is a valid outcome.** Mitigation: step 3 records "none"
  explicitly rather than leaving the section blank; the facts are the deliverable either way. If
  `src/Pegasus.Desktop` does not exist yet, the ticket blocks on [[FND-030]] and [[FND-031]] rather
  than writing facts against nothing.
- **The ticket body names one allow-list; there are two.** `RetainableFormFields` (43 names,
  `:46-91`) and `BooleanFormFields` (7 names, `:97-106`) are the same shape and the second is
  `protected`, so a subclass could reintroduce it. Mitigation: step 5's fact covers both shapes; the
  measured counts are in the inventory so the reviewer can check the fact's pattern against real
  data.
- **The area plan says the allow-list has "about thirty names"; it has 43.** Mitigation: the
  inventory states the measured count and the command that produced it. Not a body error — the body
  quotes the plan — but a fact pattern written to "about thirty" would be written to the wrong data.
- **An over-broad architecture fact will block legitimate desktop code.** A bounded cache or a
  bounded log buffer legitimately has a size constant. Mitigation: the facts target the *proposed
  value retention* shape specifically — a field-name allow-list, and a budget named for retained
  proposed values — not every numeric constant. The reviewer checks the pattern, not just the pass.
- **A genuine draft need may appear later.** Mitigation: step 4 routes it to [[FND-031]]'s encrypted
  local draft with a documented lifetime; proposal §11.1 bounds and encrypts it. Owner of the draft
  store: [[FND-031]] (plan handle `DSK-02-06`).
- **Deleting the web type early would strand 27 page models.** Mitigation: step 8's
  `git diff --stat` check; [[FEAT-026]] (plan handle `DSK-05-26`) owns the deletion after cutover.
- **Ordering.** This ticket is only meaningful once [[FEAT-005]] and [[FEAT-008]] have established
  the desktop edit and recovery model. Running it before them would write facts against an empty
  namespace.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
