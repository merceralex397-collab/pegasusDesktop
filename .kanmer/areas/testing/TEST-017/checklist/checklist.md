# Checklist — TEST-017 Test/UAT stack lifecycle

- [ ] Read local-stack definition and existing DevelopmentOffline profile; do not add a new runtime profile.
- [ ] Add bounded TestStack orchestration for local gateway/worker, Azurite, LocalDB/replay and development feed.
- [ ] Add doctor checks with actionable prerequisites and safe local cleanup.
- [ ] Exercise start, stop, feed publish and a representative test path.
- [ ] Verify: TestStack uses DevelopmentOffline plus documented feature flags.
- [ ] Verify: Doctor reports missing prerequisite precisely.
- [ ] Verify: Lifecycle does not contact production resources or require Azure.
- [ ] Record exact test command/output, simplification pass and independent review.
