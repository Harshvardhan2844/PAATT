using System.ComponentModel.DataAnnotations.Schema;
using App.Shared.Enums;

namespace App.Data.Entities;

/// <summary>
/// One Timesheet per (Employee, Project, WeekStartingDate) — timesheets are
/// per-project, not one combined weekly timesheet. If a consultant works on
/// 2 projects in the same week, that's 2 separate Timesheet rows, each with
/// its own status/lock/approval lifecycle. Unique on (EmployeeId, ProjectId,
/// WeekStartingDate).
///
/// The 24-hr/day cap is enforced in the service layer by summing
/// TimesheetEntry.HoursWorked across ALL of this employee's timesheets
/// (every project) for the given date — not something this entity can
/// validate in isolation.
/// </summary>
public class Timesheet
{
    public int Id { get; set; }

    [ForeignKey(nameof(Employee))]
    public string EmployeeId { get; set; } = string.Empty;
    public ApplicationUser Employee { get; set; } = null!;

    [ForeignKey(nameof(Project))]
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public DateOnly WeekStartingDate { get; set; }

    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

    /// <summary>
    /// The MOST RECENT whole-timesheet rejection reason only. Per-entry
    /// rejection feedback lives on TimesheetEntry.ManagerFeedback. Neither is
    /// a history — for that, see ReviewEvents, which keeps every rejection
    /// even after this field is overwritten or cleared.
    /// </summary>
    public string? OverallManagerFeedback { get; set; }

    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();

    /// <summary>Append-only submit/approve/reject log. Added in Phase 5.</summary>
    public ICollection<TimesheetReviewEvent> ReviewEvents { get; set; } = new List<TimesheetReviewEvent>();
}
