# Closeout checklist

## Closeout — TEST-003

- [x] PR merge verified (`gh pr view --json state,mergedAt`)
- [x] proof.md finalised
- [x] Moved to final stage
- [ ] Outcome recorded in ticket body
- [ ] cd out of worktree; remove only TEST-003 worktree
- [ ] delete only TEST-003 branch
- [ ] fetch --prune origin + worktree prune
- [ ] release TEST-003 claim

## Closeout progress

- [x] PR merge verified
- [x] proof.md finalised with PR URL and merge date
- [x] Moved to final stage
- [x] Outcome recorded in ticket body
- [x] Git worktree deregistered and its contents removed; an empty exact-path directory remains after Git returned a Windows permission error
- [x] Local branch deleted
- [x] `git fetch --prune origin` and `git worktree prune` completed
- [ ] Empty leftover directory removal — not attempted through a destructive workaround; no tracked or user data remains
- [ ] Kanmer claim release
