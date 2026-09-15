using App.Service.Interfaces;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Dtos.Requests;
using App.Shared.Enums;
using App.WebClient.Services;

namespace App.Web.Services;

/// <summary>
/// Server-side implementation of IConsultantApi. Calls App.Service directly,
/// and is also what the /api/consultant endpoints delegate to, so the rules
/// about whose data this is are written once.
///
/// Two things happen here that the service layer can't do on its own:
///
///  - **Identity.** Every call resolves the signed-in user through
///    ICurrentUserAccessor and passes that id down. Nothing the browser sends
///    is trusted as an employee id.
///  - **Ownership.** The entry- and timesheet-level service methods are
///    addressed by id alone (UpdateEntryAsync(entryId, ...)), which means on
///    their own they'd let any signed-in consultant edit any entry. Each of
///    those calls is gated here on "does this belong to you" first.
///
/// Business rules themselves — today-only entries, the 24-hour cap, locking,
/// assignment gating — stay in App.Service and are not duplicated here.
/// </summary>
public class ServerConsultantApi : IConsultantApi
{
    private const string NotSignedIn = "You need to be signed in to do that.";
    private const string NotYours = "That timesheet doesn't belong to you.";

    private readonly ICurrentUserAccessor _currentUser;
    private readonly IProjectService _projects;
    private readonly IProjectAssignmentService _assignments;
    private readonly ITimesheetService _timesheets;
    private readonly ITimesheetEntryService _entries;

    public ServerConsultantApi(
        ICurrentUserAccessor currentUser,
        IProjectService projects,
        IProjectAssignmentService assignments,
        ITimesheetService timesheets,
        ITimesheetEntryService entries)
    {
        _currentUser = currentUser;
        _projects = projects;
        _assignments = assignments;
        _timesheets = timesheets;
        _entries = entries;
    }

    public async Task<List<ConsultantProjectSummaryDto>> GetMyProjectsAsync()
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return new List<ConsultantProjectSummaryDto>();

        var projects = await _projects.GetProjectsConsultedOnByAsync(me);
        var myAssignments = await _assignments.GetAssignmentsForEmployeeAsync(me);
        var myTimesheets = await _timesheets.GetTimesheetsForEmployeeAsync(me);

        return projects.Select(project =>
        {
            var assignment = myAssignments.FirstOrDefault(a =>
                a.ProjectId == project.Id && a.AssignmentType == AssignmentType.Consultant);

            return new ConsultantProjectSummaryDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ClientName = project.ClientName,
                IsBillable = project.IsBillable,
                ManagerName = project.ManagerName,
                BudgetedHours = assignment?.BudgetedHours,
                // This consultant's own hours on this project — never a
                // project-wide total across everyone.
                LoggedHours = myTimesheets.Where(t => t.ProjectId == project.Id).Sum(t => t.TotalHours)
            };
        })
        .OrderBy(p => p.ProjectName)
        .ToList();
    }

    public async Task<List<TimesheetDto>> GetMyTimesheetsAsync(int? projectId = null)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return new List<TimesheetDto>();

        return await _timesheets.GetTimesheetsForEmployeeAsync(me, projectId);
    }

    public async Task<TimesheetDto?> GetTimesheetForWeekAsync(int projectId, DateOnly weekStarting)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return null;

        // Snap to the real first-day-of-week. Without this a hand-built
        // request could create a second, overlapping "week" for the same
        // project starting on a Wednesday.
        var week = TimeRules.StartOfWeek(weekStarting);

        var forProject = await _timesheets.GetTimesheetsForEmployeeAsync(me, projectId);
        return forProject.FirstOrDefault(t => t.WeekStartingDate == week);
    }

    public async Task<List<DailyTotalDto>> GetDailyTotalsAsync(DateOnly weekStarting)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return new List<DailyTotalDto>();

        var week = TimeRules.StartOfWeek(weekStarting);

        // Every project, deliberately — the 24-hour cap is a per-person,
        // per-day rule that cuts across projects.
        var all = await _timesheets.GetTimesheetsForEmployeeAsync(me);
        var entries = all.SelectMany(t => t.Entries).ToList();

        return TimeRules.DaysOfWeek(week)
            .Select(day => new DailyTotalDto
            {
                Date = day,
                Hours = entries.Where(e => e.Date == day).Sum(e => e.HoursWorked)
            })
            .ToList();
    }

    public async Task<ServiceResult<TimesheetEntryDto>> AddEntryAsync(AddEntryRequest request)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return ServiceResult<TimesheetEntryDto>.Fail(NotSignedIn);

        var week = TimeRules.StartOfWeek(request.WeekStarting);

        // Creates the week's timesheet on first use. This also carries the
        // assignment check — it fails if the caller isn't a Consultant on
        // this project, so there's no separate permission check here.
        var timesheet = await _timesheets.GetOrCreateTimesheetAsync(me, request.ProjectId, week);
        if (!timesheet.Success)
            return ServiceResult<TimesheetEntryDto>.Fail(timesheet.Errors);

        return await _entries.AddEntryAsync(
            timesheet.Data!.Id, request.Date, request.HoursWorked, Trim(request.Description));
    }

    public async Task<ServiceResult<TimesheetEntryDto>> UpdateEntryAsync(int entryId, UpdateEntryRequest request)
    {
        var owned = await OwnsEntryAsync(entryId);
        if (!owned.Success) return ServiceResult<TimesheetEntryDto>.Fail(owned.Errors);

        return await _entries.UpdateEntryAsync(entryId, request.HoursWorked, Trim(request.Description));
    }

    public async Task<ServiceResult> DeleteEntryAsync(int entryId)
    {
        var owned = await OwnsEntryAsync(entryId);
        if (!owned.Success) return owned;

        return await _entries.DeleteEntryAsync(entryId);
    }

    public async Task<ServiceResult> SubmitTimesheetAsync(int timesheetId)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return ServiceResult.Fail(NotSignedIn);

        var timesheet = await _timesheets.GetByIdAsync(timesheetId);
        if (timesheet is null) return ServiceResult.Fail("Timesheet not found.");
        if (timesheet.EmployeeId != me) return ServiceResult.Fail(NotYours);

        return await _timesheets.SubmitTimesheetAsync(timesheetId);
    }

    /// <summary>
    /// Entry-level service calls are addressed by entry id alone, so this is
    /// the gate that keeps one consultant out of another's entries.
    /// </summary>
    private async Task<ServiceResult> OwnsEntryAsync(int entryId)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return ServiceResult.Fail(NotSignedIn);

        var ownerId = await _entries.GetEntryOwnerIdAsync(entryId);
        if (ownerId is null) return ServiceResult.Fail("Entry not found.");
        if (ownerId != me) return ServiceResult.Fail("That entry doesn't belong to you.");

        return ServiceResult.Ok();
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
