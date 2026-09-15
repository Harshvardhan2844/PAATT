# Time Tracking App — Master Plan (v1)

Status: **PLANNING — decisions confirmed, not yet built**
Stack: ASP.NET Core Blazor (Web App, hosted, using Blazor Server or Auto render mode — confirmed in Phase 0) + ASP.NET Core Identity
Solution layout (as requested):

```
TimeTracker.sln
 ├─ App.Data      → Entities/, ApplicationDbContext, Migrations/
 ├─ App.Shared    → Dtos/, Enums/
 ├─ App.Service   → Interfaces/, Implementations/
 ├─ App.Web       → server (API/Blazor host, Identity, auth)
 └─ App.WebClient → client (Blazor components/pages, calls App.Web)
```

---

## 1. Consolidated Business Rules (original spec + your amendments merged)

### Roles
- **Admin**: creates employees (Consultants by default), creates Clients, creates Projects, assigns Manager + Consultants to a Project (via a "Project Assignments" tab), views employees split into two sub-tabs: **Managers** and **Consultants**, each sorted by name and showing the projects assigned to that person.
- **Project Manager (PM)**: sees only the project(s) assigned to them; sees project details; reviews timesheets in three buckets — **Approved / Pending / Rejected**, including full rejection history per timesheet; can reject **individual entries** (not just the whole timesheet) with feedback text; consultant then edits/removes just those entries.
- **Consultant**: sees only projects assigned to them; creates a timesheet; logs entries **only for the current calendar day** (no future or past entries) — only exception is editing an entry that a PM has explicitly rejected, which reopens just that entry; once a day passes, that day's entries lock automatically unless a PM rejects them; total hours per day across all projects capped at 24, enforced client-side with a visible error.

### Budgeted Hours (important deviation)
- Do **not** show a PM the sum of every consultant's hours against one shared project total.
- A PM must be able to see **each consultant's own hours vs. that consultant's own budget** on the project.
- ⇒ This means "Budgeted Hours" needs to live at the **assignment level** (per consultant, per project), not only at the project level. See open question Q2 below — this changes the schema, so I want your confirmation before generating migrations.

### Timesheet shape
- Original spec: one weekly timesheet per consultant, containing daily entries that can reference *any* of that consultant's assigned projects.
- Your amendment talks about "create timesheet for the project they've been assigned to," which could mean either (a) still one weekly timesheet, entries just filtered to assigned projects (matches original), or (b) one timesheet per project per week. This materially changes the schema (see Q3 below).

### Entry-level rejection (new, replaces whole-timesheet-only rejection)
- Original spec only had Timesheet-level Approve/Reject.
- Your amendment adds: PM can reject a **specific entry** with feedback → that entry becomes editable/removable by the consultant → consultant resubmits.
- Design implication: `TimesheetEntry` needs its own `Status` (Pending/Approved/Rejected) and `ManagerFeedback`, in addition to (or instead of) a single timesheet-level status. Whole-timesheet Approve/Reject from the original spec is kept as a bulk action; entry-level reject is an additional, finer-grained action.

---

## 2. Draft Entity Model (subject to Q2/Q3 answers below)

**ApplicationUser** (extends IdentityUser)
- Name, Email (Identity handles Email/UserName)
- Role → Identity Role (Admin / ProjectManager / Consultant)

**Client**
- Id, CompanyName

**Project**
- Id, Name, ClientId (FK), IsBillable
- BudgetedHours *(project-level total — kept for reference/reporting even if PM view is per-consultant, pending Q2)*

**ProjectAssignment**
- Id, ProjectId, ConsultantId, ManagerId
- BudgetedHours *(per-consultant cap on this project — new field per your rule)*
- (Admin creates/edits this row in the "Project Assignments" tab)

**Timesheet**
- Id, ConsultantId, WeekStartingDate, Status (Draft/Submitted/Approved/Rejected), ManagerFeedback (overall, optional if using entry-level feedback instead)

**TimesheetEntry**
- Id, TimesheetId, ProjectId, Date, HoursWorked, Description
- Status (Pending/Approved/Rejected) — **new**
- ManagerFeedback (per entry) — **new**
- IsLocked (computed: true once Date < today, unless Status = Rejected)

---

## 3. Confirmed Decisions (replaces the draft assumptions in §2)

**Roles are simpler than originally assumed.** There is no separate "ProjectManager" Identity role. Everyone non-Admin is just an **Employee**. Whether someone acts as a Manager or a Consultant is determined **per project**, at the moment Admin does the assignment:

- On the **Project Assignments** screen for a given Project, Admin picks **one Manager** and any number of **Consultants** from the employee list, in the same step.
- An employee **cannot be both Manager and Consultant on the same project** (enforced by the assignment form/service).
- An employee *can* be a Manager on one project and a Consultant on a different project — nothing stops that across projects.
- The Admin's "Managers" / "Consultants" sub-tabs are therefore **derived views**: an employee shows up under "Managers" for any project where they hold the Manager assignment, and under "Consultants" for any project where they hold a Consultant assignment (possibly both tabs, for different projects).

**Budgeted Hours live only on the assignment**, per-consultant. There is no project-level total field at all. A Project's "budget picture" is really just the sum of its individual assignment caps, but the app never displays that sum to a PM — only each consultant's own cap vs. their own logged hours.

**Timesheets are per-project.** A Consultant gets a separate Timesheet per (Project, Week) pair, not one combined weekly timesheet. Practically: if a consultant works on 2 projects in the same week, that's 2 separate Timesheet records, each with their own status/lock/approval lifecycle.

⚠️ **Cross-project consequence of this**: the "max 24 hours/day" rule from the original spec is *across all of a consultant's projects*, not just within one timesheet. Since timesheets are now split by project, the service layer must sum a consultant's `TimesheetEntry.HoursWorked` for a given date **across every one of their timesheets, regardless of project**, before allowing a new/edited entry to save. This is now flagged explicitly as a cross-cutting validation in the Service Layer (Phase 2), not something a single Timesheet can check in isolation.

---

## 4. Revised Entity Model (final, supersedes §2 draft)

**ApplicationUser** (extends IdentityUser) — Name, Email; Identity Role is just `Admin` or `Consultant` (renamed from `Employee` per your correction — default non-Admin role is `Consultant`).

**Client** — Id, CompanyName

**Project** — Id, Name, ClientId (FK), IsBillable
*(no BudgetedHours field — removed per confirmed decision)*

**ProjectAssignment** — Id, ProjectId (FK), EmployeeId (FK), AssignmentType (enum: `Manager` | `Consultant`), BudgetedHours (nullable — only set/used when AssignmentType = Consultant)
- Unique constraint on (ProjectId, EmployeeId): one assignment row per employee per project.
- Service-layer rule: at most one `Manager`-type row per Project.

**Timesheet** — Id, EmployeeId (FK, the Consultant), ProjectId (FK), WeekStartingDate, Status (Draft/Submitted/Approved/Rejected), OverallManagerFeedback (nullable, used only for whole-timesheet rejection)
- Unique on (EmployeeId, ProjectId, WeekStartingDate).

**TimesheetEntry** — Id, TimesheetId (FK), Date, HoursWorked, Description, Status (Pending/Approved/Rejected), ManagerFeedback (nullable)
*(no separate ProjectId — inherited from parent Timesheet, since Timesheet is now project-scoped)*
- `IsLocked` is computed, not stored: `true` when `Date < Today` **and** `Status != Rejected`.

**TimesheetReviewEvent** *(added in Phase 5 — the one change to this section since it was locked)* — Id, TimesheetId (FK, cascade), TimesheetEntryId (nullable, **not** a FK), EntryDate (nullable snapshot), Action (enum `ReviewAction`: `Submitted` | `Approved` | `Rejected` | `EntryRejected`), ActorId (**not** a FK) + ActorName (snapshot), Feedback (nullable), OccurredAtUtc.
- Append-only: nothing ever updates a row.
- Exists because the feedback fields above only hold the *current* reason — a second rejection overwrites the first, and editing a rejected entry clears its feedback entirely. §1's "full rejection history per timesheet" needs its own table.
- The two non-FKs are deliberate: a consultant may delete a rejected entry, and history must outlive it; and history must never block deleting a person or stop reading correctly once they're gone.

---

## 5. Phase Breakdown

Each phase ends with me writing a `PHASE_N_CONTEXT.md` file summarizing what was decided/built, so the next phase (even in a new session) has full context without re-reading everything.

| Phase | Scope | Output |
|---|---|---|
| **0 — Solution Scaffold** | Create the 5-project solution, wire up project references, install NuGet packages (EF Core, Identity, etc.), configure `App.Web` as the Identity + hosting layer, confirm Blazor render mode (Server vs. Auto/WASM) | Buildable empty solution |
| **1 — Data Layer** | `App.Data`: all entities, `ApplicationDbContext`, Identity integration, initial migration. `App.Shared`: enums (`UserRole`, `TimesheetStatus`, `EntryStatus`) and DTOs for each entity | Solution builds, DB migrates, seed data for 1 Admin |
| **2 — Service Layer** | `App.Service`: interfaces + implementations for Users, Clients, Projects, Assignments, Timesheets, Entries. All business rules live here (24-hr/day cap, current-day-only entry, locking logic, assignment-scoped project dropdowns, budget calculations) | Fully unit-testable service layer, no UI yet |
| **3 — Admin Portal** | Employee CRUD (everyone starts as plain Employee, no role picker), Client CRUD, Project CRUD, Project Assignments tab (single screen: pick one Manager + N Consultants per project, each Consultant gets their own Budgeted Hours; blocks same person as both roles on one project), "View Assigned Employees" → Managers / Consultants sub-tabs, sorted by name, showing the project(s) tied to each role | Working Admin UI |
| **4 — Consultant Portal** | View assigned projects, create a Timesheet per (Project, Week), log **today-only** entries against that project's timesheet, edit/delete same-day entries, auto-lock past days, 24-hr/day validation **summed across all of the consultant's projects**, submit (locks that project's timesheet for that week), see PM feedback and re-edit rejected entries | Working Consultant UI |
| **5 — Project Manager Portal** | View assigned project details, timesheet queue split into Approved/Pending/Rejected with full rejection history, approve/reject a whole timesheet, reject individual entries with feedback (reopens just that entry), per-consultant budget-vs-actual view (no combined total) | Working PM UI |
| **6 — Polish & Hardening** | Cross-role notifications (e.g. "timesheet rejected" banner), validation edge cases, authorization checks per route, basic styling pass, smoke test scripts | Demo-ready app |

I'll build strictly in this order and won't jump ahead — each phase's context file becomes the input for the next.

---

## 6. Status

All three open questions are resolved and folded into §3/§4 above. The plan is locked.
Render mode confirmed: **Interactive Auto**.

**Solution/project setup (`.sln`, `.csproj`, NuGet, `dotnet new`) is being handled by you, not generated here.** Claude is providing code files only, matching the folder layout in the header of this doc, for you to drop into your own scaffold.

### Phase 0 — Solution Scaffold
Owned by you. Not applicable to Claude's output.

### Phase 1 — Data Layer: **DONE (App.Data + App.Shared enums delivered)**
Delivered files:
- `App.Shared/Enums/AssignmentType.cs`
- `App.Shared/Enums/TimesheetStatus.cs`
- `App.Shared/Enums/EntryStatus.cs`
- `App.Data/Entities/ApplicationUser.cs`
- `App.Data/Entities/Client.cs`
- `App.Data/Entities/Project.cs`
- `App.Data/Entities/ProjectAssignment.cs`
- `App.Data/Entities/Timesheet.cs`
- `App.Data/Entities/TimesheetEntry.cs`
- `App.Data/ApplicationDbContext.cs` (Identity + Fluent API: unique constraints on `(ProjectId, EmployeeId)` and `(EmployeeId, ProjectId, WeekStartingDate)`, cascade rules, index on `TimesheetEntry.Date` for the cross-project 24-hr/day check)

Now also delivered:
- `App.Shared/Dtos/EmployeeDto.cs`
- `App.Shared/Dtos/ClientDto.cs`
- `App.Shared/Dtos/ProjectDto.cs`
- `App.Shared/Dtos/ProjectAssignmentDto.cs`
- `App.Shared/Dtos/TimesheetEntryDto.cs`
- `App.Shared/Dtos/TimesheetDto.cs`
- `App.Shared/Dtos/BudgetVsActualDto.cs` (bonus — backs the Phase 5 per-consultant budget view; not in the original phase list but tied directly to the "no combined total" rule in §1)
- `App.Data/SeedData.cs` — seeds `Admin` + `Employee` Identity roles and one Admin user. Reads `Seed:AdminEmail` / `Seed:AdminPassword` from config (falls back to `admin@timetracker.local` / `ChangeMe123!` for local dev only). Call `await SeedData.SeedAsync(scope.ServiceProvider)` from your `Program.cs` after building the app — wiring that call in is on you along with the rest of the scaffold.

Not delivered (owned by you):
- Initial EF Core migration (`dotnet ef migrations add InitialCreate`) — Claude has no SDK/network in this sandbox to run it.

**Phase 1 is now fully done from Claude's side.**

### Phase 2 — Service Layer: **DONE (App.Service delivered)**
Delivered files:
- `App.Shared/ServiceResult.cs` — non-throwing success/error wrapper (`ServiceResult` and `ServiceResult<T>`) used by every service method that can fail a business rule, so the UI gets validation messages back instead of exceptions.
- `App.Service/Interfaces/IEmployeeService.cs`, `IClientService.cs`, `IProjectService.cs`, `IProjectAssignmentService.cs`, `ITimesheetService.cs`, `ITimesheetEntryService.cs`
- `App.Service/Implementations/EmployeeService.cs`, `ClientService.cs`, `ProjectService.cs`, `ProjectAssignmentService.cs`, `TimesheetService.cs`, `TimesheetEntryService.cs`
- `App.Service/ServiceCollectionExtensions.cs` — optional `AddAppServices()` DI helper for `Program.cs`

Business rules implemented in this layer:
- **Assignment-scoped project dropdowns**: `IProjectService.GetProjectsManagedByAsync` / `GetProjectsConsultedOnByAsync` derive a person's visible projects entirely from their `ProjectAssignment` rows — this is also what should back the Consultant/PM portals "only sees projects assigned to them" rule.
- **Manager/Consultant exclusivity**: `ProjectAssignmentService.AssignAsync` blocks a second Manager on a project and updates-in-place rather than creating a conflicting second row for the same (project, employee) pair.
- **Budgeted hours only on Consultant rows**: enforced in `AssignAsync` (required + >0 when `AssignmentType = Consultant`, cleared when `Manager`).
- **Timesheets are assignment-gated**: `TimesheetService.GetOrCreateTimesheetAsync` fails if the employee isn't a Consultant on that project.
- **Today-only entries, with the PM-rejection exception**: `TimesheetEntryService.AddEntryAsync` requires `date == today`; `UpdateEntryAsync` / `DeleteEntryAsync` allow edits when `Date == today` OR `Status == Rejected`; editing a rejected entry resets it to `Pending` for re-review.
- **Cross-project 24-hr/day cap**: `TimesheetEntryService.CheckDailyCapAsync` sums `HoursWorked` for the employee across *every* timesheet (any project) for that date, not just the current one — matches the §3 cross-project consequence called out in the plan.
- **Entry-level vs. whole-timesheet rejection**: `TimesheetService.RejectTimesheetAsync` (bulk) reopens every entry; `TimesheetEntryService.RejectEntryAsync` reopens only the one entry — two distinct code paths, matching §1.
- **PM scoping**: `GetTimesheetsForManagerAsync` and `GetProjectsManagedByAsync` both filter to projects where the caller holds a Manager assignment.

⚠️ One known gap, flagged rather than silently decided: the plan's §1 PM rule says "full rejection history per timesheet," but the locked §4 entity model only stores the *current* rejection feedback (`Timesheet.OverallManagerFeedback`, `TimesheetEntry.ManagerFeedback`) — there's no history table. If you want actual history (not just the latest rejection reason), that needs a new entity (e.g. `TimesheetStatusChange`) added to §4 and a migration — flag it before Phase 5 if you want it built in.

**Correction applied (for real this time):** an earlier version of this doc claimed the default Identity role had already been renamed from `Employee` to `Consultant`, but the delivered code still said `Employee` — the rename itself hadn't actually been made. It's now genuinely fixed: `SeedData.EmployeeRole` → `SeedData.ConsultantRole = "Consultant"`, updated everywhere it was referenced (`App.Data/SeedData.cs`, `App.Data/Entities/ApplicationUser.cs` comment, `App.Service/Interfaces/IEmployeeService.cs` comment, `App.Service/Implementations/EmployeeService.cs`). Note: `AssignmentType.Consultant` (per-project role) and the `ConsultantRole` Identity role are two different things that happen to share a name — the former is per-project (from ProjectAssignment), the latter is the one Identity role every user gets.

**Role model clarified further — Admin is now assignable, not just seeded:** confirmed with you that the role model is: everyone is a Consultant, permanently, with no exceptions — that never changes and is never removed. On top of that permanent Consultant role, Admin can additionally:
- Assign someone as **Manager** or **Consultant** on a given project (already existed — per-project, via `ProjectAssignment.AssignmentType`, unchanged by this update).
- Grant or revoke the **Admin** Identity role itself on any existing user (new). This was previously only possible by seeding — there was no way for an Admin to promote another Consultant to Admin. Admin stacks on top of Consultant rather than replacing it, so a promoted Admin can still be assigned to projects like anyone else.

Changes made to support this:
- `App.Shared/Dtos/EmployeeDto.cs` — added `IsAdmin` bool.
- `App.Service/Interfaces/IEmployeeService.cs` — added `GetAdminsAsync()` and `SetAdminAsync(employeeId, isAdmin)`.
- `App.Service/Implementations/EmployeeService.cs` — implemented both; `SetAdminAsync` refuses to revoke Admin from the last remaining Admin (so the app can't lock everyone out), and never touches the Consultant role either way.
- `App.Data/SeedData.cs` — the seeded Admin now explicitly also gets the `Consultant` role (previously only got `Admin`), consistent with "everyone is a Consultant."

This doesn't add a UI yet — `SetAdminAsync` is ready for Phase 3 (Admin Portal) to wire up, e.g. a toggle next to each person in the employee list.

### Phase 3 — Admin Portal: **DONE (App.Web API surface + App.WebClient UI delivered)**

**Shape of the solution, decided here:** the portal runs under Interactive Auto, so the same page has to work in two places — prerendered/server-interactive (where a `DbContext` exists) and WebAssembly (where it doesn't). Rather than making every page aware of that, there's one interface, `IAdminApi`, with two implementations:

- `ServerAdminApi` (App.Web) calls `App.Service` directly — no HTTP hop on server renders.
- `HttpAdminApi` (App.WebClient) calls `/api/admin/*` — used once the page is running in the browser.

The minimal-API endpoints in `AdminEndpoints` delegate to `IAdminApi` too, so "what the UI can ask for" is defined once. No business rules were added in this phase; everything still lives in `App.Service`.

Delivered files:
- `App.Shared/Dtos/Requests/CreateEmployeeRequest.cs`, `UpdateEmployeeRequest.cs`, `SetAdminRequest.cs`, `ClientRequest.cs`, `ProjectRequest.cs`, `AssignmentRequest.cs` — wire shapes for the forms, with DataAnnotations for first-pass client-side validation.
- `App.WebClient/Services/IAdminApi.cs`, `HttpAdminApi.cs`
- `App.Web/Services/ServerAdminApi.cs`
- `App.Web/Endpoints/AdminEndpoints.cs` — `/api/admin/*`, whole group behind `RequireRole(SeedData.AdminRole)`
- `App.WebClient/_Imports.razor`
- `App.WebClient/Components/Admin/ResultAlert.razor`, `ConfirmButton.razor`
- `App.WebClient/Pages/Admin/Employees.razor` — `/admin/employees`
- `App.WebClient/Pages/Admin/Clients.razor` — `/admin/clients`
- `App.WebClient/Pages/Admin/Projects.razor` — `/admin/projects`
- `App.WebClient/Pages/Admin/ProjectAssignments.razor` — `/admin/assignments` and `/admin/assignments/{ProjectId:int}`
- `App.WebClient/Pages/Admin/AssignedEmployees.razor` — `/admin/assigned-employees`
- `PHASE_3_WIRING.md` — Program.cs registrations, nav links, and the auth-state requirement (all scaffold-side, owned by you)

**Phase 2 amendment, made here because Phase 3 needed it:** the plan's Phase 3 scope says "Employee CRUD", but `IEmployeeService` only had create + reads + `SetAdminAsync` — no update, no delete. Added:
- `UpdateEmployeeAsync(employeeId, name, email)` — email doubles as the Identity UserName, so both move together.
- `DeleteEmployeeAsync(employeeId)` — refuses when the person holds project assignments or owns timesheets (deleting would destroy logged time), and refuses to delete the last remaining Admin.

Updated files: `App.Service/Interfaces/IEmployeeService.cs`, `App.Service/Implementations/EmployeeService.cs`. Both are full replacements of the Phase 2 versions.

How the confirmed rules show up in the UI:
- **No role picker at creation** — the Add Employee form is name/email/password only. The page says in as many words that Manager vs. Consultant is decided per project.
- **Admin is additive** — a switch per row on the Employees screen, calling `SetAdminAsync`. An Admin can't toggle or delete themselves (the UI greys it out via `ClaimTypes.NameIdentifier`; the service independently refuses to strip the last Admin).
- **One Manager + N Consultants, one screen** — `/admin/assignments/{id}` has a Manager card and a Consultants table on the same page, per the confirmed design.
- **Nobody holds both roles on one project** — anyone already assigned is filtered out of both pickers, so the exclusivity rule is visible rather than just being a server-side rejection. To switch someone's role you remove their assignment first.
- **Budget is per consultant** — budgeted hours are an editable column on each consultant row; there is no project-level total anywhere in the Admin UI, and the Projects screen has no budget field.
- **Managers/Consultants sub-tabs are derived** — `/admin/assigned-employees` reads `GetManagersAsync`/`GetConsultantsAsync`, which compute purely from `ProjectAssignment` rows. Someone can legitimately appear under both tabs for different projects.

Deletes throughout are two-click confirmations rather than `confirm()` dialogs, so nothing depends on JS interop being available during prerender.

⚠️ **Not built, flagged rather than assumed:**
- **No password reset / "send them a new password".** Admin sets an initial password at creation and that's it. If an employee forgets theirs, right now the only route is Identity's own forgot-password flow. Say the word if you want an Admin-triggered reset — it's a new `IEmployeeService` method plus a button.
- **No deactivate/disable.** Delete is blocked once someone has assignments or timesheets, which is the safe behaviour, but it means there's currently no way to retire a departed employee. An `IsActive` flag on `ApplicationUser` would be the usual answer, and it touches §4 plus a migration — worth deciding before Phase 6.
- The **rejection-history gap** from Phase 2 is still open and still only matters from Phase 5 onwards.

### Phase 4 — Consultant Portal: **DONE (App.Web API surface + App.WebClient UI delivered)**

Same two-implementation shape as Phase 3: `IConsultantApi` with `ServerConsultantApi` (direct service calls, used for prerender and server-interactive) and `HttpConsultantApi` (`/api/consultant/*`, used once the page is in the browser), with the endpoints delegating to the server implementation.

**The one design decision that shaped everything else:** no method on `IConsultantApi` takes an employee id. The signed-in user is resolved server-side by `ICurrentUserAccessor` and passed down. This matters because the service layer addresses timesheets and entries by id alone — `UpdateEntryAsync(entryId, ...)` has no idea who's asking — so if the browser could name an employee, any consultant could read or edit anyone's week by guessing ids. Phase 3 never hit this because everything was Admin-only.

Delivered files:
- `App.Shared/TimeRules.cs` — the week start day, the 24-hour cap, and the week-date helpers, shared by both layers so there's one definition.
- `App.Shared/Dtos/ConsultantProjectSummaryDto.cs`, `DailyTotalDto.cs`
- `App.Shared/Dtos/Requests/AddEntryRequest.cs`, `UpdateEntryRequest.cs`
- `App.Web/Services/CurrentUserAccessor.cs` — resolves the caller from `HttpContext` (endpoints, prerender) or Blazor's `AuthenticationStateProvider` (interactive-server circuits, where there is no `HttpContext`).
- `App.Web/Services/ServerConsultantApi.cs` — identity + ownership gating, no business rules.
- `App.Web/Endpoints/ConsultantEndpoints.cs` — `/api/consultant/*`
- `App.WebClient/Services/IConsultantApi.cs`, `HttpConsultantApi.cs`
- `App.WebClient/Pages/Consultant/MyProjects.razor` — `/consultant/projects`
- `App.WebClient/Pages/Consultant/MyTimesheets.razor` — `/consultant/timesheets`
- `App.WebClient/Pages/Consultant/WeeklyTimesheet.razor` — `/consultant/timesheet/{projectId}` and `/consultant/timesheet/{projectId}/{yyyy-MM-dd}`
- `App.WebClient/Components/Shared/ResultAlert.razor`, `ConfirmButton.razor` — moved from `Components/Admin/`, unchanged otherwise; both portals use them now. **Delete the old `Components/Admin/` folder** or the build sees two components with the same name.
- `App.WebClient/_Imports.razor` — updated for that move.
- `PHASE_4_WIRING.md`

**Phase 2 amendment, again made because Phase 4 exposed it:** the plan says submitting "locks that project's timesheet for that week", but nothing enforced it — `AddEntryAsync` never looked at the parent timesheet's status, so a consultant could keep adding time to a Submitted or even Approved week, and could still edit today's entry after sending it off. Now:
- Adding requires the timesheet to be Draft or Rejected.
- Editing/deleting requires the entry to be Rejected (a manager reopening it always wins), or today's entry on a Draft/Rejected week.
- `GetEntryOwnerIdAsync(entryId)` added so the portal boundary can check ownership cheaply.
- The `MaxHoursPerDay` constant moved to `TimeRules` so the UI and the service can't disagree about the limit.

Updated files: `App.Service/Interfaces/ITimesheetEntryService.cs`, `App.Service/Implementations/TimesheetEntryService.cs` (full replacements).

How the confirmed rules show up in the UI:
- **Only their own projects** — `/consultant/projects` is built from `GetProjectsConsultedOnByAsync`, so it's exactly the set they can log against. Wandering to a project id they aren't assigned to gets a plain "you're not assigned to this project" panel rather than a form that fails on submit.
- **Today only** — the log-time form appears on today's card and nowhere else. Past days render as Closed, future days as Not yet.
- **Auto-lock** — past entries show as Locked with no Edit/Remove, matching `IsLocked` on the DTO.
- **Rejection reopens just that entry** — a rejected entry keeps its Edit/Remove buttons whatever its date, shows the manager's feedback, and editing it says out loud that it's going back for review.
- **Whole-week rejection** — a red banner with the manager's overall feedback, and the week is editable again.
- **24 hours a day, across projects** — the week screen shows how much of today's 24 is gone *across every project*, warns as it gets close, and blocks an over-cap entry before the round trip. `TimesheetEntryService` re-checks it server-side, which is the check that counts. Per-day totals read "3 h here · 8 h all projects" when they've worked elsewhere that day.
- **A timesheet per (project, week)** — the week screen is scoped to one project, with previous/next week navigation.

Design note: browsing to a week does **not** create a timesheet row. The row is created on the first entry, so paging through future weeks doesn't litter the database with empty Draft timesheets.

⚠️ **Flagged, not decided:**
- **Phase 5 has the same ownership problem, worse.** `ApproveTimesheetAsync`, `RejectTimesheetAsync` and `RejectEntryAsync` take an id and no manager, and none of them check that the caller manages that project. The PM portal will need the same gating `ServerConsultantApi` does — probably a `ServerManagerApi` that verifies the caller holds the Manager assignment on the timesheet's project before calling through. Worth deciding then whether these checks belong in `App.Service` instead; putting them there means changing signatures both portals call.
- **Week starts Monday.** Never specified; picked here and pinned to `TimeRules.FirstDayOfWeek`. Changing it later means migrating existing `WeekStartingDate` values, so settle it before there's real data.
- **"Today" is the server's local date.** Fine in one timezone. With consultants spread across several, someone's day will close earlier or later than they expect. Phase 6 material if it's a real scenario.
- **Rejections are passive.** A consultant finds out by opening the screen — the list shows "2 entries need your attention", the week shows a banner. Actual notification is the Phase 6 cross-role banner item.
- **No password reset, no deactivate** (from Phase 3), and **no rejection history** (from Phase 2) — all still open.

### Phase 5 — Project Manager Portal: **DONE (schema + service changes + App.Web API + App.WebClient UI delivered)**

Two open questions were closed to build this, both in the direction the plan already implied.

**1. Manager scoping moved into the service layer, not the portal boundary.** Phase 4 put ownership checks in `ServerConsultantApi` because changing service signatures would have rippled into both portals. That reasoning doesn't hold here: `ApproveTimesheetAsync`, `RejectTimesheetAsync` and `RejectEntryAsync` are only ever called by the PM portal, so they now take a `managerId` and verify the Manager assignment themselves. `GetTimesheetForManagerAsync` does the same for reads and returns null — not an error — for a week on someone else's project, so the portal can't be used to probe for what exists. `ServerManagerApi` is thin as a result: it resolves who's calling and delegates.

**2. Rejection history is a real table now.** §1 asked for "full rejection history per timesheet" and the locked §4 model couldn't deliver it — `OverallManagerFeedback` holds only the latest reason, and `TimesheetEntry.ManagerFeedback` is *cleared* the moment the consultant edits the entry, so the old wording was being destroyed by design. New entity `TimesheetReviewEvent`, append-only, one row per submit / approve / reject / entry-reject. **This needs a migration** — see `PHASE_5_WIRING.md`.

The entity has one foreign key, `TimesheetId`. `TimesheetEntryId` and `ActorId` are deliberately plain columns with an `ActorName` snapshot: a consultant may delete a rejected entry and the fact it was rejected has to outlive it, and history must never be the reason a person can't be deleted.

Delivered files:
- `App.Shared/Enums/ReviewAction.cs`
- `App.Shared/Dtos/TimesheetReviewEventDto.cs`
- `App.Shared/Dtos/BudgetVsActualDto.cs` — updated: adds `ApprovedHours`, pins down `ActualHours`
- `App.Shared/Dtos/Requests/RejectionRequest.cs`
- `App.Data/Entities/TimesheetReviewEvent.cs`, `Entities/Timesheet.cs` (updated), `ApplicationDbContext.cs` (updated)
- `App.Service/Interfaces/ITimesheetService.cs`, `ITimesheetEntryService.cs`, `IProjectAssignmentService.cs` (all updated)
- `App.Service/Implementations/TimesheetService.cs`, `TimesheetEntryService.cs`, `ProjectAssignmentService.cs` (all updated)
- `App.Web/Services/ServerManagerApi.cs`, `App.Web/Endpoints/ManagerEndpoints.cs`
- `App.WebClient/Services/IManagerApi.cs`, `HttpManagerApi.cs`
- `App.WebClient/Pages/Manager/ReviewQueue.razor` — `/manager/timesheets`
- `App.WebClient/Pages/Manager/TimesheetReview.razor` — `/manager/timesheet/{id}`
- `App.WebClient/Pages/Manager/ManagedProjects.razor` — `/manager/projects`
- `App.WebClient/Pages/Manager/ProjectDetail.razor` — `/manager/project/{id}`
- `PHASE_5_WIRING.md`

How the confirmed rules show up:
- **Only their projects** — every read and write is scoped through the Manager assignment. Typing another project's id into the URL gets "you don't manage this project", not data.
- **Approved / Pending / Rejected buckets** — the queue's default tab is Pending, since that's what's waiting on them. "Pending" is the `Submitted` status relabelled for humans. **Draft weeks never appear**: an unsubmitted week is still the consultant's workspace. That's a behaviour change to `GetTimesheetsForManagerAsync`, which previously returned drafts too.
- **Reject the whole week** — reopens every entry regardless of date, and the form says so, with a nudge toward entry-level rejection when only one line is wrong.
- **Reject a single entry** — reopens just that entry and leaves the week's status alone, so the consultant fixes one line rather than resubmitting everything. Allowed on a Submitted week and also on an Approved one, because spotting a bad entry after sign-off is exactly what entry-level rejection is for. Blocked on a Draft: there's nothing to reject on a week nobody sent.
- **Approving doesn't wave through rejections** — entries already sent back stay rejected when the week is approved.
- **Per-consultant budget, no combined total** — `GetBudgetVsActualAsync` returns one row per consultant and no summed figure at all, so a caller can't display a project total by accident. Rejected hours are excluded from "actual" (the manager already said those aren't real); approved hours are shown separately so they can see how much of the burn is signed off.
- **Full history** — a timeline on the review screen, oldest first, showing a week that bounced as submit → reject → submit → approve, with the original wording of each rejection intact.

⚠️ **Still open, deliberately not built:**
- **No notifications.** A consultant learns about a rejection by opening the app. The Phase 6 cross-role banner is the fix.
- **Nav links can't be role-hidden.** "Manager" isn't an Identity role, so everyone sees the manager links and non-managers get an empty state. Cosmetic; Phase 6.
- **Approved weeks can be edited via entry rejection, indefinitely.** There's no cut-off — a manager can reject an entry on a week approved six months ago, which reopens it for editing. If the business needs a lock-after-N-days or a payroll-closed flag, that's a new rule and a new field.
- **No password reset, no deactivate** (Phase 3) — still the two gaps most likely to bite in real use.

### Phase 6 — Polish & Hardening: not started

**Next step:** Phase 6 — cross-role notifications, per-route authorization review, validation edge cases, styling pass, smoke tests. Worth folding in the two Phase 3 gaps (password reset, deactivate) at the same time, since both are small once the Admin screens already exist.

