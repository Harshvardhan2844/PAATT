namespace App.Shared.Enums;

/// <summary>
/// Status of a single TimesheetEntry. A PM can reject one entry without
/// rejecting the whole timesheet; that reopens just this entry for the
/// consultant to edit/remove, even if its date has otherwise locked.
/// </summary>
public enum EntryStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
