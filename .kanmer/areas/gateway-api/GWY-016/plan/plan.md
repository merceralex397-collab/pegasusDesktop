# Plan — GWY-016: DSK-03-16 · Relocate `OperatorLabels` to `Pegasus.Contracts` as one shared vocabulary list

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Chosen approach

Move the pure code → operator-label map out of `src/Pegasus.Web/Presentation/OperatorLabels.cs` into `src/Pegasus.Contracts` so the web app and the desktop consume one vocabulary list, and re-point the twenty-four `.cshtml` consumers without any behaviour change.

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.


## Ordered implementation steps

1. Orient. Read `docs/desktop/03-gateway-api-and-data/README.md` § 3 row *Operator vocabulary* and `docs/design/README.md` § Core outcome to operator label and persistence. Then `get_doc_gates <this ticket id>` and `take_ticket`.
2. Read `src/Pegasus.Web/Presentation/OperatorLabels.cs` end to end and classify every member into two lists in the ticket plan: (a) pure code → string maps that depend only on a value's identity, and (b) members that need an ASP.NET or Web-only type. Only list (a) moves.
3. Resolve the enum problem and record the decision in the plan before writing code: `Pegasus.Contracts` may not reference `Pegasus.Core` (area 02 plan, line 205), so the relocated map must key on the **string form** of the code, matching the "enums-as-strings" contract convention. The Web wrapper passes `value.ToString()`; the desktop passes the string it received in a DTO. If a better option is found, record why before choosing it — do not add a `Pegasus.Core` reference to `Pegasus.Contracts` without changing area 02's plan first.
4. Create `src/Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs` holding list (a) verbatim — the same words, the same fallthrough to a `Humanise` equivalent, the same reserved meanings for Audit, Triage, Unidentified and Blocked, and the same `CaseStage` lifecycle names. Copy the class remarks with it; they are the rule, not decoration.
5. Turn `src/Pegasus.Web/Presentation/OperatorLabels.cs` into a thin adapter over `OperatorVocabulary` that keeps its current public signatures (Core enums in, string out) so no `.cshtml` file changes. Delete the moved bodies; a copy left behind is the exact duplication this ticket exists to remove.
6. Confirm no `.cshtml` change is needed: `grep -rn "OperatorLabels\." src/Pegasus.Web` and check every call still compiles against the unchanged signatures. Where a signature genuinely cannot be preserved, change the call sites in the same commit and list them in the plan.
7. Extend `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` with a fact that fails if a second label map exists: assert that no type outside `Pegasus.Contracts.Vocabulary` declares a method returning a string for a `CaseStage`/state code, or — more simply and more robustly — that `OperatorLabels` contains no string literal that also appears in `OperatorVocabulary`.
8. Add a characterization test before the move if one does not exist: capture the current output of every public `OperatorLabels` member for every enum value into a test, run it against the pre-move code, then against the post-move code. Identical output is the definition of "no behaviour change" here (proposal § 22.1 characterization before refactoring).
9. Run `dotnet build Pegasus.slnx -c Release` — expected zero warnings under `TreatWarningsAsErrors`.
10. Run the whole existing web test set that renders labels: `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~LabelTests|FullyQualifiedName~MailClassificationLabelTests|FullyQualifiedName~AutomationActorLabelTests"`. Done means green with no assertion text changed.
11. Run `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release` and confirm the new no-second-map fact passes.
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the ticket plan.

## Acceptance conditions

- [ ] The pure label map lives in `src/Pegasus.Contracts` and nowhere else.
- [ ] `Pegasus.Contracts` still references nothing beyond the BCL and `System.Text.Json`.
- [ ] No Razor page or `.cshtml` file changed behaviour; existing web tests pass with unchanged assertions.
- [ ] A characterization test proves label output is identical before and after the move.
- [ ] An architecture test fails if a second label map appears.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~MailClassificationLabelTests"` — expected: pass with no assertion text edited.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release` — expected: all facts pass, including the no-second-label-map fact.

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Contracts/Vocabulary/**`, `src/Pegasus.Web/Presentation/OperatorLabels.cs`, `tests/Pegasus.ArchitectureTests`, and `docs/design/README.md` for the path reference. Must not change any operator-facing word, and must not touch `.cshtml` files unless a signature genuinely could not be preserved.
- **Traps**: `Pegasus.Contracts` must stay dependency-free (`net10.0`, System.Text.Json only) — adding a `Pegasus.Core` reference to make the move easy contradicts area 02's project table and would fail [[DSK-03-01]]'s architecture test. Two of these maps are settled business vocabulary that must not drift (`CaseStage`, and the reserved meanings of Audit, Triage, Unidentified, Blocked). Operator copy rules under `docs/design/README.md` bind: a label that explains rather than names is a defect. **Open question to record, not invent**: whether the desktop consumes the vocabulary directly or through DTO-supplied display strings is settled by area 06; this ticket only guarantees one list exists.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Ownership resolution — 2026-08-27

- [[GWY-016]] performs the single `OperatorLabels` relocation because it is the gateway-side ticket that names the shared `Pegasus.Contracts` home and is currently unblocked by [[GWY-001]]. It absorbs the required fold-in from [[FEAT-023]]: the two page-local `IntakeDecision` maps and their `docs/design/README.md:541-542` reconciliation, plus the third `VrmRecognitionOutcomeKind` map with its existing wording.
- [[FEAT-023]] is covered by this ticket and will not perform a second move. Its downstream [[DUI-005]] relationship is retained through the completed relocation; no duplicate branch or PR is created.
- The relocation remains a string-keyed contract vocabulary plus a thin Web adapter because `Pegasus.Contracts` must not reference `Pegasus.Core`; Web signatures remain unchanged and the desktop can consume the shared string vocabulary without ASP.NET or EF dependencies.

## Implementation evidence — 2026-08-27

- Classification: pure code/name-to-operator-label members now live in `src/Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs`, keyed by stable string names so Contracts has no Core dependency. The Web adapter retains the existing Core-typed signatures and keeps the Web-only Europe/London formatter and Core-shaped provenance/configuration translation at that boundary.
- The relocated pure members cover attachment searchability, unidentified reason/state/media, e-mail handle and association, case stage/type, chase state, operational destination, repair route, estimate line type, document role/origin, image-intake state, custody/upload state, intake failure, intake decision, history event, route scope, chase reason, inspection mode, Automation actor, mileage, source channel, recognition outcome, provenance words/icons, mail classification, and humanisation.
- The two page-local `IntakeDecision` maps and the `VrmRecognitionOutcomeKind` map now delegate through `OperatorLabels`; no `.cshtml` call-site changes were required. The sanctioned FEAT-023 fold-in corrects only the two design-authority terms from `Document text required` to `Needs text extraction`, and from `Technical failure` to `Failed`.
- `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` now guards one Contract vocabulary owner and rejects the former page-local label-switch shapes. `tests/Pegasus.IntegrationTests/OperatorVocabularyTests.cs` covers all intake decisions, all recognition outcomes, and Web-to-Contract decision delegation.
- Baseline characterization: the unchanged base commit `ae66cbf6` was tested in a detached temporary worktree with the existing label suites: 8 passed, 0 failed. Post-move focused suites pass with the same existing 8 tests plus 12 new vocabulary/decision tests; the two sanctioned design wording changes are explicitly asserted.

## Simplification pass — 2026-08-27

- Reuse: one shared `OperatorVocabulary` owns every moved map; `OperatorLabels` only translates Core/Web-specific shapes and retains the two formatters that require Web/runtime types.
- Simplification: no new interface, project, package, compatibility path, or duplicate page map was introduced. The shared helper uses stable names instead of adding a Core reference to Contracts.
- Efficiency: label calls remain direct static calls; no cache, reflection, service, or extra runtime hop was added.
- Altitude: documentation names the actual as-built owner and adapter; the architecture test checks the ownership boundary without adding production infrastructure. No unapplied simplification finding remains.
