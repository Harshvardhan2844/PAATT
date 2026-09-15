using App.Shared;
using App.Shared.Dtos;
using App.Shared.Dtos.Requests;
using App.Shared.Enums;

namespace App.WebClient.Services;

/// <summary>
/// What the Project Manager Portal talks to. Same two-implementation shape as
/// the other portals: ServerManagerApi for prerender and server-interactive
/// renders, HttpManagerApi for the browser.
///
/// As with IConsultantApi, no method takes a person's id — "which projects do
/// I manage" is resolved server-side from the signed-in user. Manager is a
/// per-project role held through a ProjectAssignment row, not an Identity
/// role, so every read and write is scoped by that assignment rather than by
/// anything the browser claims.
/// </summary>
public interface IManagerApi
{
    /// <summary>Projects where this person holds the Manager assignment.</summary>
    Task<List<ProjectDto>> GetMyProjectsAsync();

    /// <summary>One managed project, or null if they don't manage it.</summary>
    Task<ProjectDto?> GetProjectAsync(int projectId);

    /// <summary>
    /// Each consultant on the project against their own budget. There is no
    /// project-wide total here by design.
    /// </summary>
    Task<List<BudgetVsActualDto>> GetProjectBudgetsAsync(int projectId);

    /// <summary>
    /// The review queue across every project they manage. Pass null for all
    /// buckets. Draft weeks never appear — they haven't been sent yet.
    /// </summary>
    Task<List<TimesheetDto>> GetQueueAsync(TimesheetStatus? status = null);

    /// <summary>One timesheet, or null if it isn't on a project they manage.</summary>
    Task<TimesheetDto?> GetTimesheetAsync(int timesheetId);

    /// <summary>
    /// Full submit/approve/reject history for the timesheet, oldest first —
    /// including rejections whose feedback has since been overwritten.
    /// </summary>
    Task<List<TimesheetReviewEventDto>> GetHistoryAsync(int timesheetId);

    /// <summary>Approves the whole week. Entries already rejected individually stay rejected.</summary>
    Task<ServiceResult> ApproveTimesheetAsync(int timesheetId);

    /// <summary>Rejects the whole week, reopening every entry for editing.</summary>
    Task<ServiceResult> RejectTimesheetAsync(int timesheetId, RejectionRequest request);

    /// <summary>Rejects one entry, reopening only that entry. The rest of the week is untouched.</summary>
    Task<ServiceResult> RejectEntryAsync(int entryId, RejectionRequest request);
}
