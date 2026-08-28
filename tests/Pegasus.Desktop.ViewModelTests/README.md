# Pegasus desktop view-model tests

This project runs desktop-side tests without a packaged identity, a
`DispatcherQueue`, a UI thread, a database, or a network endpoint.

`Support/FixedTimeProvider.cs` is the single shared desktop-test clock. Its
default instant is `2026-01-01T00:00:00Z`; tests advance or set that clock
explicitly and must not add private clock copies.

The gateway, credential-store, and navigation support types are hand-written
test seams. They remain transport-free until the production ports land in the
desktop foundation tickets.
