# Phase 6 — Polish & Hardening

Completed:

- Added cross-role in-app attention banners. Consultants see rejected timesheets/entries; managers see submitted weeks waiting for review.
- Added role-aware manager navigation. Manager links are now shown only when the signed-in user holds at least one Manager assignment.
- Added a responsive application shell and navigation styling.
- Hardened service validation for entry hours, description length, rejection-feedback length, and normalized descriptions before persistence.
- Added an anonymous `/health` endpoint and `scripts/SmokeTest.ps1`, which builds and verifies application startup through the health endpoint.
- Added and validated the initial SQL Server Express migration, including Phase 5 review-history schema.

Security model retained:

- Admin API routes require `Admin`.
- Consultant and manager API routes require a signed-in `Consultant`; ownership/manager-assignment checks remain in the server/service layer for every resource read or write.
- Manager navigation is cosmetic only; the service-layer assignment checks are the security boundary.

Run locally:

```powershell
dotnet ef database update --project App.Data --startup-project App.Data
dotnet run --project App.Web
```

For a quick validation:

```powershell
.\scripts\SmokeTest.ps1
```

Database configuration is in `App.Web/appsettings.json` and defaults to the local
`Server=.\SQLEXPRESS`, `TimeTracker` database using Windows authentication.
