# Verification status

The reference `app.py` was used for request numbering, state transitions, field validation, country and phone rules, role access, badge labels, reception completion, role-specific navigation, dashboard counts, reception visitor index, and analytics filters.

The workspace used for this port did not contain the .NET SDK or cached frontend dependencies. `npm install --offline` failed because packages were not cached. Consequently, the .NET tests, TypeScript build, browser checks, database migration, and end-to-end role workflows have not run here. Static checks confirmed balanced C# punctuation, valid country data and logo XML, and no whitespace errors. These checks cannot prove compilation or workflow parity.

Before production use, run:

```bash
dotnet restore RRVMS.sln
dotnet build RRVMS.sln -c Release --no-restore
dotnet test RRVMS.sln -c Release --no-build
cd apps/web && npm install && npm run build && npm test
```

Then verify the three roles against a test database, the full correction and Reception flows, CSV/XLSX exports, and visual behavior at desktop and mobile widths. The single-file role selector was intentionally retained only as the architecture's development authentication mode; deployed authentication uses Entra ID.

Known differences from the reference remain: the React screens use a recreated component and CSS system rather than the source HTML/CSS. Reports now paginate unique visitors while exports retain all matching visit-day rows. Reception's visitor index includes the main actions but does not reproduce every source column or mobile card detail. Pixel-level and end-to-end replica status is therefore unverified.
