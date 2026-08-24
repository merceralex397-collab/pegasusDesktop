# Checklist — DUI-009 ReasonDialog

- [ ] Read the authority reason-dialog contract and established dialog-service API.
- [ ] Implement the narrow model: requirement, identity, optional approved consequence, reason and verb.
- [ ] Enforce initial reason focus, explicit Cancel, safe Escape, focus containment and invoking-control restoration.
- [ ] Exercise a representative Hold, Block or Unlink flow without inventing new consequence copy.
- [ ] Verify: UI automation proves the primary action is verb-labelled and does not fire on an accidental Enter.
- [ ] Verify: Focus starts in the reason input and returns to the invoker after Cancel or complete.
- [ ] Verify: No dialog body contains unapproved explanatory prose.
- [ ] Record simplification and independent review evidence.
