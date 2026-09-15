# Phase 5 — wiring notes

## 1. There is a migration this time

Phase 5 adds one table. Nothing else in the schema changes.

```bash
dotnet ef migrations add TimesheetReviewHistory --project App.Data --startup-project App.Web
dotnet ef database update --project App.Data --startup-project App.Web
```

The new table is `TimesheetReviewEvents`: an append-only log of submit / approve / reject / entry-reject.
It has exactly one foreign key — `TimesheetId`, cascade. `TimesheetEntryId` and `ActorId` are plain
columns on purpose:

- A consultant is allowed to delete a rejected entry, and the record that it *was* rejected has to outlive
  it. An FK would cascade the history away or block the delete.
- History must never be the reason you can't delete a person, and it should still read correctly after
  they're gone — hence the `ActorName` snapshot alongside the id.

Existing timesheets get no history rows. Their past decisions weren't recorded anywhere, so there's nothing
to backfill; history starts from the first submission after you deploy this.

## 2. Replacements from earlier phases

| File | Change |
|---|---|
| `App.Data/ApplicationDbContext.cs` | New `DbSet` and config for `TimesheetReviewEvents`. |
| `App.Data/Entities/Timesheet.cs` | Adds the `ReviewEvents` navigation; clarifies that `OverallManagerFeedback` is only ever the latest reason. |
| `App.Service/Interfaces/ITimesheetService.cs` + `Implementations/TimesheetService.cs` | Manager-scoped approve/reject, manager-scoped single read, history read and writes, Draft weeks excluded from the manager queue. |
| `App.Service/Interfaces/ITimesheetEntryService.cs` + `Implementations/TimesheetEntryService.cs` | `RejectEntryAsync` now takes a `managerId` and checks it. |
| `App.Service/Interfaces/IProjectAssignmentService.cs` + `Implementations/ProjectAssignmentService.cs` | Adds `GetBudgetVsActualAsync`. |
| `App.Shared/Dtos/BudgetVsActualDto.cs` | Adds `ApprovedHours` and pins down what `ActualHours` means. |

**Three service signatures changed.** `ApproveTimesheetAsync`, `RejectTimesheetAsync` and
`RejectEntryAsync` all take a manager id now. Nothing outside the manager portal calls them, so the Admin
and Consultant portals compile untouched — but if you've written anything of your own against those
methods, that's where the build will break.

## 3. App.Web/Program.cs

```csharp
using App.Web.Endpoints;           // MapManagerApi()
using App.Web.Services;            // ServerManagerApi
using App.WebClient.Services;      // IManagerApi

builder.Services.AddScoped<IManagerApi, ServerManagerApi>();

app.MapManagerApi();               // after UseAuthentication/UseAuthorization
```

`ICurrentUserAccessor` and `AddHttpContextAccessor()` are already registered from Phase 4 and are reused
as-is.

## 4. App.WebClient/Program.cs

```csharp
builder.Services.AddScoped<IManagerApi, HttpManagerApi>();
```

## 5. Nav links

```razor
<AuthorizeView Roles="Consultant">
    <Authorized>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="manager/timesheets">Review queue</NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="manager/projects">Projects I manage</NavLink>
        </div>
    </Authorized>
</AuthorizeView>
```

There's no role that means "manager" — it's a per-project assignment — so `AuthorizeView` can't hide these
from people who don't manage anything. They'll see the links and get an empty state that explains why
there's nothing there. If that bothers you, the fix is a cascading value carrying "manages at least one
project", computed once at layout level; it's cosmetic, so it's left for Phase 6.

## 6. Routes

| Route | Screen |
|---|---|
| `/manager/timesheets` | Review queue, bucketed Pending / Approved / Rejected / All |
| `/manager/timesheet/{id}` | One week: entries, approve, reject week, reject single entries, full history |
| `/manager/projects` | Projects they manage |
| `/manager/project/{id}` | Per-consultant budget vs. actual, plus that project's submitted weeks |
