using PAATT.Shared.Enums;

namespace PAATT.Data.Entities;

public class TimesheetEntry
{
    public int Id { get; set; }
    public int TimesheetId { get; set; }
    public DateOnly WorkDate { get; set; }
    public decimal Hours { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimesheetEntryStatus Status { get; set; } = TimesheetEntryStatus.Pending;
    public string? Feedback { get; set; }
    public Timesheet Timesheet { get; set; } = null!;
    public ICollection<TimesheetReview> Reviews { get; set; } = new List<TimesheetReview>();
}
