# Research — FEAT-034 Box conflict and version handling

## Question

Detect a newer canonical document version before overwrite and present a safe, explicit conflict path through the gateway.

## Findings

- Canonical document version/custody remains authoritative in the gateway/Core.
- The desktop must surface a newer version; it cannot silently overwrite or resolve by retrying blindly.
- Existing problem-details and ReasonDialog patterns provide presentation, not policy.

## Implication

Keep the desktop on the existing gateway/Core boundary; implement only a caller-backed contract or native client slice. No Azure write, direct provider access, secret or compatibility path is in scope.

## Dependencies

Builds on FEAT-031 broker operations and the shared problem/dialog components.
