# Checklist — DUI-001 Theme resource dictionaries

- [ ] Read DSK-02-09's delivered file set and the token tables; reuse its merge order and guard test rather than creating new infrastructure.
- [ ] Transcribe each approved token into the existing Light, Dark and HighContrast dictionaries, using system colours only for HighContrast.
- [ ] Fill typography, spacing, shape and focus resources; keep the approved 2px radii and 3px focus treatment.
- [ ] Build and run the shell, exercise all three themes, and add the guard-test and screenshot evidence.
- [ ] Verify: `dotnet build --configuration Release` is warning-free.
- [ ] Verify: The existing `StylesAreTheOnlySourceOfColourAndType` test passes.
- [ ] Verify: Light, Dark and Contrast screenshots show legible panels and visible focus.
- [ ] Record the simplification pass and independent review in the plan before merge.
