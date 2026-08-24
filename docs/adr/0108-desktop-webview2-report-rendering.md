---
id: ADR-0108
status: proposed
date: 2026-08-24
supersedes: []
superseded_by: []
related_capabilities: []
related_frd: [frd-11]
tags: []
---

# ADR-0108: Desktop WebView2 report rendering

## Status

Proposed on 2026-08-24. This proposal relates to ADR-0025 and ADR-0028 and
supersedes neither. It becomes accepted only after the Phase 7 off-screen-host
spike and the approved-fixture golden-file parity gate have both passed.

## Context

L-03 moves report rendering to the desktop through an isolated, non-UI WebView2
HTML-to-PDF path. The existing gateway renderer remains in place until parity
has been demonstrated. This is the sole permitted WebView2 use in the desktop
conversion, not a WebView shell.

Proposal section 2.1 locks the conversion to "no WebView shell". Its section
23.2 supplies the narrow exception: "An isolated WebView2 use for a third-party
login consent page or a specific document preview is not automatically a web
wrapper, but it requires an ADR and must not host Pegasus UI." The exception
does not relax the rule: this WebView2 renders a report document only, is never
visible to an operator, and must never host a Pegasus page, workflow, or other
UI.

ADR-0025 keeps the report templates as Pegasus product behaviour, co-versioned
with the FRDs and Core policy. ADR-0028 currently composes the Playwright
renderer in the existing Web Container App, which carries a pinned Chromium
build, matching Linux dependencies, fonts, and writable temporary space.
ADR-0108 adds the proposed desktop implementation of the existing renderer
port; it does not supersede either accepted decision or remove the gateway
renderer at this stage.

### Cloud-justification test

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | No | Rendering transforms one approved snapshot into document bytes. The authoritative report record and stored artefact remain behind the gateway. |
| Unattended execution — must it run with every desktop closed? | No | This renderer serves an operator-initiated assessment/report workflow; it is not a scheduled rendering service. |
| Protected credentials — long-lived secret that must not sit on workstations? | No | It consumes the approved snapshot and governed templates, not a provider secret. Box custody credentials remain behind the gateway. |
| Public callback — must an external service call a stable public endpoint? | No | No external service invokes the renderer. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | Yes — gateway | FRD-11 readiness, identity, hash, correction, approval, finality, custody, and audit remain `Pegasus.Core` policy enforced by the gateway. The desktop produces bytes; it never registers a report. |
| Measured operational advantage — measured evidence central is materially better? | No — not yet measured | Removing the Chromium stack from the Web image is a design rationale, not measured operational evidence. |

The single central-enforcement answer does not make the renderer a cloud
responsibility. It names the gateway as the authority for report registration
and audit; the operator workstation is the proposed execution host for the
render transform.

## Decision

When this proposal is accepted, `Pegasus.Desktop.Infrastructure` will implement
the existing `IAssessmentReportRenderer` port with the same governed Scriban
templates that the gateway renderer uses. The implementation will use an
isolated WebView2 solely to render the internal report HTML to PDF with
`CoreWebView2.PrintToPdfStreamAsync`. It will allow only one print operation at
a time, because the WebView2 API permits only one printing operation per
WebView.

The renderer is non-UI: the WebView2 is never visible, has one document-render
purpose, and must not host Pegasus UI. It produces document bytes only. Report
registration, custody, authorisation, and audit remain gateway responsibilities
under FRD-11. A missing or outdated WebView2 runtime must produce a named
failure and use the retained gateway renderer while the workstation is fixed.

**Off-screen host — unresolved pending Phase 7 spike:** [[FEAT-040]]
(`DSK-07-14`) must prove whether a collapsed WinUI `WebView2` control with a
XAML root or a `CoreWebView2Controller` attached to a hidden HWND is the
reliable host. The acceptance change records the proven choice here. Microsoft
Learn documents the controller's `ParentWindow` and visibility hosting APIs,
including an HWND window reference; that API availability is not evidence that
either candidate renders correctly in Pegasus without a visible window.

## Consequences

- The gateway `PlaywrightAssessmentReportRenderer` remains the renderer in use
  until [[FEAT-041]] (`DSK-07-15`) passes golden-file parity on approved
  fixtures in the local Test/UAT stack and the later acceptance change records
  the host choice. This ADR does not claim that parity has passed.
- Parity compares text, values, page count, and key positions within documented
  tolerances. It is not pixel equality: WebView2 Chromium updates independently
  while the current Playwright baseline is pinned.
- After the approved parity gate and acceptance, no required report may depend
  on the gateway renderer unless a new superseding ADR changes that decision.
- Template sharing is a single-source requirement. The desktop does not create
  another Scriban, CSS, logo, or signature asset set; [[FEAT-039]] owns the
  hash-checked embedding of the governed source.
- Acceptance of this ADR proves the architecture only. It does not prove that
  the desktop renderer is implemented, installed, parity-passing, or accepted
  for production use.

## Reversal condition

Rendering stays with, or returns to, the gateway if any of the following is
true: the WebView2 runtime is absent or cannot be maintained across the target
workstation fleet; approved fixtures have a golden-file divergence that cannot
be closed within their documented tolerance; or neither candidate off-screen
host initialises and renders without a visible window. Such evidence reopens
L-03 for an operator decision; it is not a reason to expose a WebView2-based
Pegasus UI.

## Options considered

- **Keep server-side rendering permanently:** rejected for the proposed desktop
  direction because it preserves the Web image's Chromium runtime burden solely
  because the former UI was web-based.
- **Use WebView2 as the desktop shell:** rejected because proposal section 2.1
  prohibits a WebView shell and section 23.2 allows only this isolated,
  document-rendering exception.
- **Select the off-screen host now:** rejected because the host behaviour has
  not been proven; choosing it before [[FEAT-040]] completes would turn an
  evidence gap into an immutable assertion.

## Links

- [Conversion governance and ADR set](../desktop/00-governance-and-workflow/README.md)
- [Integration plan: report rendering and the Phase 7 spike](../desktop/07-integrations/README.md)
- [Native desktop conversion proposal](../desktop/Pegasus_Native_Desktop_Design_Proposal.md)
- [ADR-0025: integrate the renderer into Pegasus](0025-integrate-renderer-and-extractor-into-the-application.md)
- [ADR-0028: current Web Container App renderer host](0028-run-integrated-renderer-in-web-container-app.md)
- [FRD-11: reports, correspondence and reviewed proposals](../frd/frd-11-reports-correspondence-and-reviewed-proposals.md)
- [Microsoft Learn: `CoreWebView2.PrintToPdfStreamAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.printtopdfstreamasync) — fetched 2026-08-24.
- [Microsoft Learn: printing from WebView2 apps](https://learn.microsoft.com/microsoft-edge/webview2/how-to/print) — fetched 2026-08-24.
- [Microsoft Learn: `CoreWebView2Controller`](https://learn.microsoft.com/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2controller) — fetched 2026-08-24.
- [Microsoft Learn: `CoreWebView2ControllerWindowReference`](https://learn.microsoft.com/microsoft-edge/webview2/reference/winrt/microsoft_web_webview2_core/corewebview2controllerwindowreference) — fetched 2026-08-24.
