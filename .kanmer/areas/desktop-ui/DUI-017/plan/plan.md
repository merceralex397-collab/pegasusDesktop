# Plan — DUI-017 Capture prototype-to-page screen map

## Governing documents

Docs/design/README.md remains the authority. The new screen map is a durable reference artefact under docs/design/references/, linked from docs/index.md; it must not become a competing design specification.

## Steps

1. Retrieve the cited Claude Design project github.md using authorised project access and preserve source/provenance date.
2. Map each approved prototype to the corresponding replaces Pages heading in screen-specs.md; flag any unmapped/ambiguous row rather than guessing.
3. Add docs/design/references/screen-map.md with mapping, source identity and reference-only status.
4. Add the single index link, validate markdown/documentation checks, and tell DUI-013 exactly what reference it can consume.

## Verification

- [ ] Every claimed prototype/page mapping has an identified source line.
- [ ] The map is linked from the documentation index and has no normative behaviour.
- [ ] Documentation checks pass and no unrelated design authority was edited.

## Risks

Authorised access to the correct Claude Design project is required; .design-sync/config.json identifies a different project and is not a substitute.
