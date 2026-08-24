# Research — DUI-004 Authenticated shell

## Question

How can the shell reproduce the authority's navigation, counts and status context while keeping data behind the gateway?

## Verified findings

- The ticket body fixes the 236px NavigationView rail, authority route order, centred 1280px content region, and the no-placeholder-count rule.
- Rail counts come from the `/api/v1` dashboard query, not a desktop database query.
- The existing web layout and RailCountsPageFilter are reference evidence; web mechanics are not copied.

## Implications

Implement the smallest shared WinUI slice stated in the ticket body. Reuse the existing project, generated gateway client and authority documents; do not create a WebView shell, direct data access, a second vocabulary/resource list, or an Azure dependency. The implementation agent is `winui-dev`; independent review is by `pegasus-desktop-reviewer`.

## Dependency / decision handling

DSK-02-08 and DSK-02-10 provide shell/lifecycle foundations; FND-033 owns the outstanding environment-badge wording.
