# Files — GWY-005: DSK-03-05 · Kiota client generation script, tool pin, committed output and CI no-op check

## Change surface

| Path | Purpose / risk |
| --- | --- |
| `docs/desktop/03-gateway-api-and-data/README.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` | Authoritative context; update only if the ticket's accepted scope explicitly calls for it. |
| `openapi/pegasus-v1.json` | Versioned HTTP contract snapshot; change only with matching contract-test and client-generation evidence. |
| `src/Pegasus.Desktop.Infrastructure` | Named by the ticket as an implementation or verification dependency. |
| `eng/api/Generate-ApiClient.ps1` | Engineering tool or generation script; keep it deterministic and repository-owned. |
| `src/Pegasus.Desktop.Infrastructure/Api/Generated` | Named by the ticket as an implementation or verification dependency. |
| `eng/api/Export-OpenApiDocument.ps1` | Engineering tool or generation script; keep it deterministic and repository-owned. |
| `src/Pegasus.Desktop.Infrastructure/Api/Generated/Directory.Build.props` | Named by the ticket as an implementation or verification dependency. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | Focused verification surface; extend the stated success, failure and regression coverage. |

## Context files

- `docs/desktop/03-gateway-api-and-data/README.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` — Authoritative context; update only if the ticket's accepted scope explicitly calls for it.
- `openapi/pegasus-v1.json` — Versioned HTTP contract snapshot; change only with matching contract-test and client-generation evidence.
- `src/Pegasus.Desktop.Infrastructure` — Named by the ticket as an implementation or verification dependency.
- `eng/api/Generate-ApiClient.ps1` — Engineering tool or generation script; keep it deterministic and repository-owned.
- `src/Pegasus.Desktop.Infrastructure/Api/Generated` — Named by the ticket as an implementation or verification dependency.
- `eng/api/Export-OpenApiDocument.ps1` — Engineering tool or generation script; keep it deterministic and repository-owned.
- `src/Pegasus.Desktop.Infrastructure/Api/Generated/Directory.Build.props` — Named by the ticket as an implementation or verification dependency.
- `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` — Focused verification surface; extend the stated success, failure and regression coverage.

## Ripple and out-of-scope boundary

- **Azure**: no write.
- **Scope boundary**: may touch `.config/dotnet-tools.json`, `eng/api/**`, `src/Pegasus.Desktop.Infrastructure/Api/**`, `tests/Pegasus.ArchitectureTests`, central package management and the `unit` job in `.github/workflows/ci.yml`. Must not touch `src/Pegasus.Web` or `openapi/pegasus-v1.json` (that file is [[DSK-03-04]]'s output).
- **Traps**: never lower `TreatWarningsAsErrors` or `AnalysisLevel` at the repository root to make generated code compile — scope any suppression to the generated folder. Adding a CI job on a private repository bills Windows minutes at 2× (C-01), so extend the existing `unit` job. The desktop projects come from area 02; if `src/Pegasus.Desktop.Infrastructure` does not exist yet, stop rather than creating it here.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
