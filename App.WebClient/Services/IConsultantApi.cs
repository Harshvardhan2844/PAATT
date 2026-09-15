using App.Shared;
using App.Shared.Dtos;
using App.Shared.Dtos.Requests;

namespace App.WebClient.Services;

/// <summary>
/// What the Consultant Portal pages talk to. Same two-implementation shape as
/// IAdminApi from Phase 3: ServerConsultantApi (App.Web) calls App.Service
/// directly for prerender and server-interactive renders, HttpConsultantApi
/// calls /api/consultant/* once the page is running in the browser.
///
/// Note what isn't here: there's no employeeId parameter anywhere. "Whose
/// timesheet is this" is resolved server-side from the signed-in user, never
/// from anything the browser sends, so a consultant can't read or edit
/// somebody else's week by guessing an id.
/// </summary>
public interface IConsultantApi
{
    /// <summary>
    /// Projects this consultant is assigned to, each with their own budgeted
    /// and logged hours. Derived from ProjectAssignment rows, so it's exactly
    /// the set of projects they're allowed to log against.
    /// </summary>
    Task<List<ConsultantProjectSummaryDto>> GetMyProjectsAsync();

    /// <summary>Every timesheet this consultant owns, newest week first, optionally one project's worth.</summary>
    Task<List<TimesheetDto>> GetMyTimesheetsAsync(int? projectId = null);

    /// <summary>
    /// The timesheet for one (project, week), or null if they haven't logged
    /// anything that week yet. Viewing a week deliberately does not create a
    /// timesheet row — that happens on the first entry.
    /// </summary>
    Task<TimesheetDto?> GetTimesheetForWeekAsync(int projectId, DateOnly weekStarting);

    /// <summary>
    /// Hours logged per day across ALL of this consultant's projects for the
    /// given week. This is the total the 24-hour cap applies to, so the
    /// weekly screen can show how much room is left in a day.
    /// </summary>
    Task<List<DailyTotalDto>> GetDailyTotalsAsync(DateOnly weekStarting);

    /// <summary>
    /// Logs time. Creates the week's timesheet if this is the first entry for
    /// it. Fails if the date isn't today, the week is already submitted, or
    /// the entry would push the day past 24 hours across all projects.
    /// </summary>
    Task<ServiceResult<TimesheetEntryDto>> AddEntryAsync(AddEntryRequest request);

    /// <summary>Edits an entry that's still open — today's, or one a manager rejected.</summary>
    Task<ServiceResult<TimesheetEntryDto>> UpdateEntryAsync(int entryId, UpdateEntryRequest request);

    Task<ServiceResult> DeleteEntryAsync(int entryId);

    /// <summary>Sends the week to the manager. Closes it to further edits until it's approved or rejected.</summary>
    Task<ServiceResult> SubmitTimesheetAsync(int timesheetId);
}
