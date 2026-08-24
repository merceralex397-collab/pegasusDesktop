# Research — DUI-007 Virtualized data table pattern

## Question

How can shared tabular screens remain accessible and server-paged without adopting a non-x:Bind DataGrid?

## Verified findings

- WinUI guidance selects `ListView` + Grid item template + header Grid; CommunityToolkit DataGrid is explicitly unsuitable for this app's x:Bind approach.
- The authority fixes 32px rows, dropdown filters, newest-first sort and keyboard/paging requirements.
- Sorting, filtering and paging remain server-side through `/api/v1`.

## Implications

Implement the smallest shared WinUI slice stated in the ticket body. Reuse the existing project, generated gateway client and authority documents; do not create a WebView shell, direct data access, a second vocabulary/resource list, or an Azure dependency. The implementation agent is `winui-dev`; independent review is by `pegasus-desktop-reviewer`.

## Dependency / decision handling

DUI-001 supplies table tokens; gateway list contracts are a prerequisite.
