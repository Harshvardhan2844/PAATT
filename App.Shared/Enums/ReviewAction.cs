namespace App.Shared.Enums;

/// <summary>
/// What happened to a timesheet, recorded once per event so the history
/// survives later edits. Added in Phase 5 to satisfy the "full rejection
/// history per timesheet" rule — the Timesheet and TimesheetEntry rows only
/// ever hold the *current* feedback, so without this a second rejection
/// overwrites the first and the trail is gone.
/// </summary>
public enum ReviewAction
{
    /// <summary>Consultant sent the week to their manager. Logged on every resubmission, so the back-and-forth is visible.</summary>
    Submitted = 0,

    /// <summary>Manager approved the whole week.</summary>
    Approved = 1,

    /// <summary>Manager rejected the whole week, reopening every entry.</summary>
    Rejected = 2,

    /// <summary>Manager rejected one entry, reopening just that one.</summary>
    EntryRejected = 3
}
