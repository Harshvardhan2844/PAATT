using App.Shared;
using App.Shared.Dtos;

namespace App.Service.Interfaces;

public interface ITimesheetEntryService
{
    /// <summary>
    /// Adds an entry to a timesheet. Enforces:
    ///  - Date must be today (no future/past entries on create).
    ///  - The parent timesheet must still be open (Draft or Rejected). Once
    ///    it's Submitted or Approved the week is closed to new time.
    ///  - Cross-project 24-hr/day cap: this employee's total HoursWorked for
    ///    Date, summed across EVERY one of their timesheets (any project),
    ///    plus this new entry, must not exceed TimeRules.MaxHoursPerDay.
    /// </summary>
    Task<ServiceResult<TimesheetEntryDto>> AddEntryAsync(
        int timesheetId, DateOnly date, decimal hoursWorked, string? description);

    /// <summary>
    /// Edits an existing entry. Allowed when the entry's Status is Rejected
    /// (a manager rejection reopens it regardless of date or timesheet
    /// status), or when Date == today AND the parent timesheet is still open
    /// (Draft or Rejected). Re-applies the 24-hr/day cap check, excluding this
    /// entry's own current hours from the running total. Editing a Rejected
    /// entry resets its Status to Pending for re-review.
    /// </summary>
    Task<ServiceResult<TimesheetEntryDto>> UpdateEntryAsync(
        int entryId, decimal hoursWorked, string? description);

    /// <summary>Same open/locked rule as UpdateEntryAsync.</summary>
    Task<ServiceResult> DeleteEntryAsync(int entryId);

    /// <summary>
    /// Manager action: rejects a single entry with feedback, reopening just
    /// that entry for the consultant to edit or remove even after its day has
    /// otherwise locked. Fails unless managerId holds the Manager assignment
    /// on the timesheet's project (Phase 5 — previously unchecked), and
    /// unless the week has actually been submitted: there's nothing to reject
    /// on a draft. Logged to the timesheet's review history.
    /// </summary>
    Task<ServiceResult> RejectEntryAsync(int entryId, string managerId, string feedback);

    /// <summary>
    /// Id of the employee who owns the timesheet this entry belongs to, or
    /// null if the entry doesn't exist. The entry-level calls above are
    /// addressed by entry id alone, so the portal boundary needs a cheap way
    /// to confirm the caller owns the entry before letting them touch it.
    /// </summary>
    Task<string?> GetEntryOwnerIdAsync(int entryId);
}
