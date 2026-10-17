# Reference parity audit

`upload/app.py` is the behavioral and visual reference. This layered solution preserves the archive's .NET API, domain, infrastructure, SQL Server, and React architecture.

| Area | Current port | Verification |
| --- | --- | --- |
| Request creation, visitor forms, screening, corrections, reception actions | Mapped into domain services and role-specific screens | Static review only; exercise all transitions against a database |
| Role navigation and search | Dashboard, request/history/pending/report paths follow the source role menus; global search routes into requests | Browser and keyboard checks pending |
| Dashboard | Host and Export Control card sets and pending lists mapped; Reception has six cards and a searchable visitor index | Business counts require seeded end-to-end comparison |
| Request list | Role views, history/pending filters, visitor names, and multi-visitor groups | Search, pagination, and status combinations require integration checks |
| Analytics | Nine source filters, all-row counts, visitor-level pagination, daily CSV/XLSX rows, and Reception serial redaction | Build, SQL translation, and export checks pending |
| Visuals | Brand palette, horizontal navigation, responsive tables, and forms recreated in React | Exact layout, typography, spacing, mobile cards, and interactive states have not been visually compared |

This is not a certified replica. See `VERIFICATION.md` for build and workflow gates.
