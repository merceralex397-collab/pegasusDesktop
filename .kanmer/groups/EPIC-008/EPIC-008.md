---
id: EPIC-008
kind: epic
title: Area 07 - integrations
archived: false
created: '2026-08-24T07:26:08.409Z'
updated: '2026-08-24T07:26:08.409Z'
---
Seeds from `docs/desktop/07-integrations/README.md` (handles DSK-07-01…DSK-07-19). Decides and delivers each external seam - Microsoft Graph intake (stays in the unattended Worker), Box custody, DVLA/DVSA, outbound mail, OCR/image preprocessing - and the isolated non-UI WebView2 HTML-to-PDF report renderer that locked decision L-03 and ADR-0108 place on the desktop. No long-lived provider secret ever ships in the package. Board area: `desktop-features` (FEAT).
