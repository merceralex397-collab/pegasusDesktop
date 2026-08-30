# Plan — DUI-005 Shared operator vocabulary consumption

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can every desktop-facing state, time, size and identifier be presented through one shared label map? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Resolve the current owner of the shared-label relocation by reading FEAT-023's recorded decision before code work.
2. Route all desktop display values through the shared map and one Europe/London formatter.
3. Replace raw-key display/input paths with named picker/display models.
4. Add view-model tests for unmapped values, raw identifiers and formatting regressions.

## Verification

- Focused view-model tests fail for raw enum/GUID/hash display and pass for approved labels.
- Dates display with the stated Europe/London/UTC fallback semantics.
- No second desktop label table exists.

## Risks and dependencies

FEAT-023's unresolved ownership split must remain unresolved here; this plan consumes its eventual decision rather than duplicating the question.

The implementation worktree must record its simplification pass and independent desktop review before merge.

## Dependency resolution — 2026-08-28

- Read-only Kanmer checks confirm [[GWY-001]] is `done` and [[GWY-016]] is `done` at merged commit `67109b45066648b3256eff8d4bc3491a18bfeb7d`; it owns the single `Pegasus.Contracts/Vocabulary/OperatorVocabulary` implementation.
- [[FEAT-023]] is archived with documented duplicate/coverage rationale and will not be implemented separately. Its required relocation decision is therefore resolved by GWY-016; no duplicate map or dependency edit is introduced here.
- This ticket may proceed against the current `origin/dev` contract owner, with desktop-side formatting/test work only. No product decision, Azure write, upstream synchronization, or shared-map modification is required for this ticket.

## Blocker — 2026-08-28

- The coordinator-specified worktree `C:\\Users\\PC\\Documents\\GitHub\\pegasus-worktrees\\dui-005-operator-vocabulary` is absent (`Test-Path` returned `False`); Git has no `task/dui-005-operator-vocabulary` local ref and no registered DUI-005 worktree.
- Read-only `origin/dev` inspection finds `src/Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs`, but no `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, or `tests/Pegasus.Desktop.ViewModelTests` paths. The targeted build and test therefore fail with MSB1009 because their project files do not exist; the required grep cannot scan `src/Pegasus.Desktop` (rg exit 2, path not found).
- The missing prerequisites are owned by `FND-038` / `DSK-02-13` and `TEST-004` / `DSK-08-04` (both `preparing`, `blocked`). `TEST-004` explicitly depends on the absent DSK-02-05 desktop scaffold. No desktop architecture or project was invented.

## Simplification pass — 2026-08-28

- N/A — blocked before implementation; there is no DUI-005 branch diff to simplify. No code, tests, documentation paths, commit, push, or Kanmer stage mutation was made for the product change.

## Execution revalidation — 2026-08-30

- The dependency decision is resolved: [[GWY-016]] is done and owns `src/Pegasus.Contracts/Vocabulary/OperatorVocabulary.cs`; [[FEAT-023]] is archived as duplicate/covered. The desktop consumes this owner through a direct `Pegasus.Contracts` project reference. No shared map copy or edit was made.
- Added `src/Pegasus.Desktop/Presentation/OperatorText.cs`. Its vocabulary methods delegate to `OperatorVocabulary`; its only local presentation rules are Europe/London date/time conversion with an explicitly labelled UTC fallback, invariant count formatting, and one-decimal megabyte formatting.
- The current desktop scaffold exposes the counter as a formatted `string` to XAML. It has no identifier-entry controls, Target/reference columns, enum properties, GUID/hash/version properties, or byte-count properties. The guard tests fail if those raw forms are introduced.
- Added `tests/Pegasus.Desktop.ViewModelTests/OperatorVocabularyTests.cs` covering the current displayed enums, shared-map labels, Europe/London summer/winter conversion, preformatted numeric values, raw presentation types, typed identifier inputs, raw Target/reference identifiers, and banned operator words.
- Added the direct `Pegasus.Core` test reference required by the enum coverage and updated the locked test dependency graph. Updated the two named desktop design/reuse documents to record the facade and second-consumer boundary.
- Scope check: `git diff --stat origin/dev -- src/Pegasus.Worker` is empty. No Azure, deployment, upstream, corpus, or transient repository planning change was made.

## Simplification pass — 2026-08-30

- Reuse: the facade delegates the existing `OperatorVocabulary` owner instead of introducing a second label table or wrapper hierarchy.
- The scaffold originally exposed an `int Counter` directly to XAML; it now stores the numeric state privately and exposes the required formatted string. This is the smallest change that enforces the display boundary.
- No new screen, service, interface, cache, compatibility path, or feature flag was introduced. The direct `Pegasus.Contracts` reference is required by the facade; the direct `Pegasus.Core` reference is test-only for enum coverage.
- The repository-wide grep reports only diagnostic logging `ToString()` calls in `Logging/DiagnosticsLoggerProvider.cs`; the ViewModels/Presentation paths have no `ToString()`, `ToLocalTime()`, or `DateTime.Now`. Those logging calls are not operator display bindings and are outside this ticket's scope.
- The current XAML has no identifier TextBox or Target/reference column. The companion tests enforce the rule for future bound surfaces without inventing a screen or picker flow that the scaffold does not contain.
