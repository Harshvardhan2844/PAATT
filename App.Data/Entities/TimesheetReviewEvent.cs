using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Shared.Enums;

namespace App.Data.Entities;

/// <summary>
/// An append-only log of what happened to a timesheet: submitted, approved,
/// rejected, or one entry rejected. Nothing ever updates a row here.
///
/// This exists because Timesheet.OverallManagerFeedback and
/// TimesheetEntry.ManagerFeedback only hold the *latest* feedback — a second
/// rejection overwrites the first, and editing a rejected entry clears it
/// entirely. The plan asks for full rejection history, which needs its own
/// table.
///
/// Two deliberate non-relationships:
///  - TimesheetEntryId is a plain column, not a foreign key, and EntryDate is
///    a snapshot. A consultant is allowed to delete a rejected entry, and the
///    fact that it was rejected should outlive it.
///  - The actor is stored as an id plus a name snapshot rather than a FK to
///    ApplicationUser, so history never blocks deleting a person and still
///    reads correctly after they're gone.
/// </summary>
public class TimesheetReviewEvent
{
    public int Id { get; set; }

    [ForeignKey(nameof(Timesheet))]
    public int TimesheetId { get; set; }
    public Timesheet Timesheet { get; set; } = null!;

    /// <summary>Set only for EntryRejected. Not a foreign key — see the class remarks.</summary>
    public int? TimesheetEntryId { get; set; }

    /// <summary>The date the entry covered, captured when the event was written.</summary>
    public DateOnly? EntryDate { get; set; }

    public ReviewAction Action { get; set; }

    /// <summary>Identity id of whoever did this. Not a foreign key — see the class remarks.</summary>
    [MaxLength(450)]
    public string ActorId { get; set; } = string.Empty;

    /// <summary>Their display name at the time.</summary>
    [MaxLength(200)]
    public string ActorName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Feedback { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
