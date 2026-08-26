## 2026-08-26 implementation checkpoint

- Highest current production release is `Release 20`; first future gateway tag would be `gateway/r21`.
- Added the tag convention/CI rule to `docs/engineering.md` and the post-promotion immutable tag step to both existing byte-identical `pegasus-release` skill copies.
- `git diff --check` passed; both skill copies remain hash-identical. `git tag --list 'gateway/*' 'desktop/*'` is empty because no next authorized production release occurred. The first tag and its `docs/operations.md` record remain an explicit future release-time acceptance condition; they are not claimed as done.
- No Azure/cloud/deployment/external-environment write was performed.
