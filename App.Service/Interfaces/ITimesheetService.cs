using App.Shared;
using App.Shared.Dtos;
using App.Shared.Enums;

namespace App.Service.Interfaces;

public interface ITimesheetService
{
    /// <summary>
    /// Returns the existing Timesheet for (employeeId, projectId, weekStartingDate)
    /// or creates a new Draft one. Fails if the employee doesn't hold a
    /// Consultant assignment on that project (assignment-scoped — a
    /// consultant can only create timesheets for projects they're assigned to).
    /// </summary>
    Task<ServiceResult<TimesheetDto>> GetOrCreateTimesheetAsync(
        string employeeId, int projectId, DateOnly weekStartingDate);

    /// <summary>Unscoped read. Callers are responsible for checking who's allowed to see it.</summary>
    Task<TimesheetDto?> GetByIdAsync(int timesheetId);

    /// <summary>
    /// Manager-scoped read: returns the timesheet only if this person holds
    /// the Manager assignment on its project, otherwise null. Added in Phase 5
    /// so the PM portal can't be walked into someone else's project by id.
    /// </summary>
    Task<TimesheetDto?> GetTimesheetForManagerAsync(int timesheetId, string managerId);

    /// <summary>All timesheets for this consultant, optionally filtered to one project.</summary>
    Task<List<TimesheetDto>> GetTimesheetsForEmployeeAsync(string employeeId, int? projectId = null);

    /// <summary>
    /// Timesheets on projects this employee manages, bucketed by status —
    /// backs the PM's Approved / Pending / Rejected tabs. Pass null for all
    /// statuses. Draft timesheets are never returned: an unsubmitted week is
    /// the consultant's private workspace, not the manager's business.
    /// </summary>
    Task<List<TimesheetDto>> GetTimesheetsForManagerAsync(string managerId, TimesheetStatus? status = null);

    /// <summary>
    /// The full submit/approve/reject log for a timesheet, oldest first.
    /// Unlike OverallManagerFeedback this survives later rejections and edits.
    /// </summary>
    Task<List<TimesheetReviewEventDto>> GetReviewHistoryAsync(int timesheetId);

    /// <summary>Draft/Rejected -> Submitted. Fails if the timesheet has no entries. Logged to the review history.</summary>
    Task<ServiceResult> SubmitTimesheetAsync(int timesheetId);

    /// <summary>
    /// Bulk-approve: Timesheet -> Approved, and every non-rejected entry ->
    /// Approved. Fails unless managerId holds the Manager assignment on the
    /// timesheet's project.
    /// </summary>
    Task<ServiceResult> ApproveTimesheetAsync(int timesheetId, string managerId);

    /// <summary>
    /// Bulk-reject: Timesheet -> Rejected with feedback, and every entry's
    /// Status is set to Rejected too, which reopens all of them for editing
    /// (per the IsLocked rule) regardless of date. Fails unless managerId
    /// manages the project. Distinct from RejectEntryAsync on
    /// ITimesheetEntryService, which rejects one entry only.
    /// </summary>
    Task<ServiceResult> RejectTimesheetAsync(int timesheetId, string managerId, string feedback);
}
