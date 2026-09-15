# TimeTracker

TimeTracker is an ASP.NET Core Blazor Web App with SQL Server Express,
ASP.NET Core Identity, and separate admin, consultant, and project-manager
workflows.

## Start the application

The application is configured for the local SQL Server Express instance:

```text
Server=.\SQLEXPRESS;Database=TimeTracker;Trusted_Connection=True;TrustServerCertificate=True
```

The initial database migration has already been applied. For a fresh machine
or database, run:

```powershell
dotnet ef database update --project App.Data --startup-project App.Data
dotnet run --project App.Web
```

On first startup, the app seeds an Admin user:

```text
Email: admin@timetracker.local
Password: ChangeMe123!
```

Change this password immediately outside local development. You can override
the seed credentials with `Seed:AdminEmail` and `Seed:AdminPassword` settings.

## Verify

```powershell
dotnet build TimeTracker.slnx
.\scripts\SmokeTest.ps1
```
