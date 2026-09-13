using PAATT.Shared.Enums;

namespace PAATT.Data.Entities;

public class TimesheetReview
{
    public int Id { get; set; }
    public int TimesheetId { get; set; }
    public int? TimesheetEntryId { get; set; }
    public string ReviewerId { get; set; } = string.Empty;
    public TimesheetReviewAction Action { get; set; }
    public string? Feedback { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public Timesheet Timesheet { get; set; } = null!;
    public TimesheetEntry? TimesheetEntry { get; set; }
    public ApplicationUser Reviewer { get; set; } = null!;
}
