2026-08-26 — Independent review NEEDS CHANGES: missing Relates entries for ADR-0014 and future FRD-13/DSK-00-08, plus ambiguous “package manifest” wording. Fixed in 17c87e51; local link and placement gates passed. PR #22 now needs exact-head CI and passing re-review.

## Independent review — 2026-08-26

The second independent reviewer returned NEEDS CHANGES at commit 62e8e680, then identified the same scope on the final PR head. Blocking findings:

1. The branch edits a published status: accepted ADR in place, contrary to AGENTS.md and docs/adr/README.md, which require immutable accepted bodies and a new superseding ADR for a changed decision. This requires an explicit governance amendment or a valid superseding-ADR route; no merge is authorized under the current record.
2. The six-row cloud-justification table is not scoped to feed versus gateway. The next revision must make the feed answers and the gateway central-enforcement answer explicit, without inventing an Azure requirement.
3. ForceUpdateFromAnyVersion must be described as the App Installer XML element/value form, matching the canonical template, not as an attribute.

The review also confirmed the Relates section and file-name wording were fixed, exact local documentation checks passed, the one-file scope is otherwise correct, and no runtime/cloud/packaging claim is made. PR #22 remains open and must not be merged until the governance conflict is resolved.

Second independent review (2026-08-26) returned NEEDS CHANGES. Blocking: accepted ADR body was edited in place against AGENTS.md/docs/adr/README.md immutability; cloud table needs feed-versus-gateway scope; ForceUpdateFromAnyVersion must be stated as the canonical XML element/value form. Relates and package-manifest wording fixes are in 17c87e51. PR #22 stays unmerged pending governance resolution.

2026-08-26 — Governance route selected from AGENTS.md: restore ADR-0105 body unchanged, mark it superseded, create next-free ADR-0031 as the complete corrected release decision, and update the ADR index. No reserved 0100–0110 slot is free; no new ADR-0105 will be created.
