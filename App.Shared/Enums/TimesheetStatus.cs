namespace App.Shared.Enums;

/// <summary>
/// Whole-timesheet status. A Timesheet is per (Employee, Project, Week).
/// This is the bulk-action status; individual entries also carry their
/// own EntryStatus for entry-level rejection.
/// </summary>
public enum TimesheetStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}
