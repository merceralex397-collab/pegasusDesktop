# Proof — FND-029

## Merged-main evidence

- PR #26 was independently reviewed **PASS**, had exact-head CI run `33014659206` green across the required repository-check lanes, and merged to `dev` at `b5a3a6e87388db20d4c38226b4a5297e8f400145`.
- `git ls-remote origin refs/heads/dev refs/heads/main` after promotion reported `dev=b5a3a6e87388db20d4c38226b4a5297e8f400145` and `main=b5a3a6e87388db20d4c38226b4a5297e8f400145`.
- The update was a non-force fast-forward from `3b1737de2a27f84aa1bea03bf2c34d41d5a8006a`; the first attempt was correctly rejected because it used a stale local `dev` ref, then the exact `origin/dev` ref was pushed.
- `git rev-parse refs/remotes/origin/main^{tree}` and `git rev-parse 0a3d23becc5a1038ab166effafd5203847bc3b5c^{tree}` both returned `2927d80b20d97fbbf92380bacd6814b6e6f3f848`. The reviewed PR tree is therefore the tree now on `main`.

## Acceptance evidence

- `src/Pegasus.Contracts` is present in the solution and builds with zero package, project, and framework references.
- The thirteen `urn:pegasus:problem:` slugs, paging/concurrency/header/compatibility contracts, and one shared camelCase JSON configuration are present as reviewed.
- The architecture and serialization facts are present; no Core record or `ActionActor` is declared in Contracts.
- The no-total paging boundary is verified by the exact case-sensitive search: `rg -n --case-sensitive 'Total' src/Pegasus.Contracts/Paging` returned zero matches.
- The exact case-sensitive searches returned zero dependency-reference matches and zero `ActionActor` matches; `rg -n --case-sensitive 'record PagedResult'` found the single declaration.

## Current validation

Executed in the clean recorded worktree `.worktrees/fnd-029` at reviewed head `0a3d23becc5a1038ab166effafd5203847bc3b5c`:

- `dotnet restore ./Pegasus.slnx --locked-mode` — exit 0.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — exit 0, 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — 110 passed, 0 failed, 0 skipped.
- Worktree status remained clean after validation.

No deployment, Azure write, credential change, or upstream operation was performed.
