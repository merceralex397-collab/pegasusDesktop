## Operator identity confirmation — 2026-08-29

The existing manifest identity values were re-read on the FND-039 branch and are used unchanged:

- `Identity.Name`: `CollisionEngineers.Pegasus`
- `Identity.Publisher`: `CN=Collision Engineers`
- `PublisherDisplayName`: `Collision Engineers`

These are the operator-confirmed permanent values. The development certificate must have the exact subject `CN=Collision Engineers` when generated from this manifest. This resolves only the identity-selection question; it does not resolve the separate self-contained WinApp CLI prerequisite failure or the required certificate-trust, clean-machine install/launch/uninstall, result-log, screenshot, cleanup, and no-elevation evidence. FND-039 remains in Review and is not mergeable or Done.
