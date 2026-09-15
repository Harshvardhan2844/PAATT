using App.Shared.Enums;

namespace App.Shared.Dtos;

/// <summary>
/// One line of a timesheet's history, oldest first. Both the manager's review
/// screen and (for rejections) the consultant's week view read from this.
/// </summary>
public class TimesheetReviewEventDto
{
    public int Id { get; set; }

    public int TimesheetId { get; set; }

    /// <summary>Set only when Action = EntryRejected. Points at the entry as it was then; the entry may since have been edited or removed.</summary>
    public int? TimesheetEntryId { get; set; }

    /// <summary>Date of the entry this event was about, captured at the time so the history still reads correctly if the entry is gone.</summary>
    public DateOnly? EntryDate { get; set; }

    public ReviewAction Action { get; set; }

    /// <summary>Who did it, as their name was at the time.</summary>
    public string ActorName { get; set; } = string.Empty;

    public string? Feedback { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
