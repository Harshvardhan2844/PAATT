using App.Service.Interfaces;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Dtos.Requests;
using App.Shared.Enums;
using App.WebClient.Services;

namespace App.Web.Services;

/// <summary>
/// Server-side implementation of IManagerApi, and what the /api/manager
/// endpoints delegate to.
///
/// Where Phase 4 had to invent ownership checks at this boundary, Phase 5
/// pushed them down: ApproveTimesheetAsync, RejectTimesheetAsync and
/// RejectEntryAsync all take a managerId and verify the Manager assignment
/// themselves, and GetTimesheetForManagerAsync returns null rather than
/// someone else's week. So this class resolves *who* is calling and otherwise
/// stays out of the way — the one check left here is on the read paths that
/// take a project id, which are scoped against the caller's managed projects.
/// </summary>
public class ServerManagerApi : IManagerApi
{
    private const string NotSignedIn = "You need to be signed in to do that.";

    private readonly ICurrentUserAccessor _currentUser;
    private readonly IProjectService _projects;
    private readonly IProjectAssignmentService _assignments;
    private readonly ITimesheetService _timesheets;
    private readonly ITimesheetEntryService _entries;

    public ServerManagerApi(
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

    public async Task<List<ProjectDto>> GetMyProjectsAsync()
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return new List<ProjectDto>();

        return await _projects.GetProjectsManagedByAsync(me);
    }

    public async Task<ProjectDto?> GetProjectAsync(int projectId)
    {
        var mine = await GetMyProjectsAsync();
        return mine.FirstOrDefault(p => p.Id == projectId);
    }

    public async Task<List<BudgetVsActualDto>> GetProjectBudgetsAsync(int projectId)
    {
        // Scoped through the managed-project list rather than trusting the id.
        if (await GetProjectAsync(projectId) is null) return new List<BudgetVsActualDto>();

        return await _assignments.GetBudgetVsActualAsync(projectId);
    }

    public async Task<List<TimesheetDto>> GetQueueAsync(TimesheetStatus? status = null)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return new List<TimesheetDto>();

        return await _timesheets.GetTimesheetsForManagerAsync(me, status);
    }

    public async Task<TimesheetDto?> GetTimesheetAsync(int timesheetId)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return null;

        return await _timesheets.GetTimesheetForManagerAsync(timesheetId, me);
    }

    public async Task<List<TimesheetReviewEventDto>> GetHistoryAsync(int timesheetId)
    {
        // History is only readable through a timesheet the caller can already see.
        if (await GetTimesheetAsync(timesheetId) is null) return new List<TimesheetReviewEventDto>();

        return await _timesheets.GetReviewHistoryAsync(timesheetId);
    }

    public async Task<ServiceResult> ApproveTimesheetAsync(int timesheetId)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return ServiceResult.Fail(NotSignedIn);

        return await _timesheets.ApproveTimesheetAsync(timesheetId, me);
    }

    public async Task<ServiceResult> RejectTimesheetAsync(int timesheetId, RejectionRequest request)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return ServiceResult.Fail(NotSignedIn);

        return await _timesheets.RejectTimesheetAsync(timesheetId, me, request.Feedback);
    }

    public async Task<ServiceResult> RejectEntryAsync(int entryId, RejectionRequest request)
    {
        var me = await _currentUser.GetUserIdAsync();
        if (me is null) return ServiceResult.Fail(NotSignedIn);

        return await _entries.RejectEntryAsync(entryId, me, request.Feedback);
    }
}
