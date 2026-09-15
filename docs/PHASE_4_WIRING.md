# Phase 4 — wiring notes

Additive to the Phase 3 notes. Everything here is scaffold-side glue you own.

## 1. Replacements from earlier phases

Three files in this drop overwrite what you already have:

| File | Why |
|---|---|
| `App.Service/Interfaces/ITimesheetEntryService.cs` | Documents the new locking rule and adds `GetEntryOwnerIdAsync`. |
| `App.Service/Implementations/TimesheetEntryService.cs` | A submitted or approved week is now closed to new/edited/deleted entries, and the 24-hour constant moved to `App.Shared.TimeRules`. |
| `App.WebClient/_Imports.razor` | Points at the new `Components/Shared` namespace. |

`ResultAlert.razor` and `ConfirmButton.razor` have moved from `App.WebClient/Components/Admin/` to
`App.WebClient/Components/Shared/` — they're used by both portals now, so the Admin folder was the wrong
home. The files are otherwise unchanged. **Delete `App.WebClient/Components/Admin/` after copying this drop
in**, or you'll have two components with the same name and the build will complain about the ambiguity.
Nothing else changes: the Phase 3 pages pick them up through `_Imports.razor`.

## 2. App.Web/Program.cs

```csharp
using App.Web.Endpoints;           // MapConsultantApi()
using App.Web.Services;            // ServerConsultantApi, CurrentUserAccessor
using App.WebClient.Services;      // IConsultantApi

// --- services ---
builder.Services.AddHttpContextAccessor();                          // CurrentUserAccessor needs it
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IConsultantApi, ServerConsultantApi>();

// --- pipeline ---
app.MapConsultantApi();                                             // after UseAuthentication/UseAuthorization
```

## 3. App.WebClient/Program.cs

```csharp
builder.Services.AddScoped<IConsultantApi, HttpConsultantApi>();
```

The `HttpClient` registration from Phase 3 is reused as-is.

## 4. Nav links

```razor
<AuthorizeView Roles="Consultant">
    <Authorized>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="consultant/projects">My projects</NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="consultant/timesheets">My timesheets</NavLink>
        </div>
    </Authorized>
</AuthorizeView>
```

Every user holds the Consultant role, including Admins, so these links show for everyone. That's intended —
an Admin who is also assigned to a project needs somewhere to log their time. Someone with no assignments
sees an empty-state message rather than a broken screen.

## 5. Routes

| Route | Screen |
|---|---|
| `/consultant/projects` | Projects they're assigned to, with their own budget vs. logged hours |
| `/consultant/timesheets` | Every week they own, filterable by status; `?projectId=N` narrows it |
| `/consultant/timesheet/{projectId}` | This week for that project |
| `/consultant/timesheet/{projectId}/{yyyy-MM-dd}` | A specific week |

The week segment is a plain string rather than a route constraint, because there's no `DateOnly` constraint
in Blazor routing. `TimeRules.ParseWeekStart` snaps whatever arrives to the Monday of that week and falls
back to the current week if it's unparseable, so a mangled URL lands somewhere sensible instead of erroring.

## 6. Nothing new to migrate

Phase 4 adds no entities and no schema changes, so there's no migration to run.
