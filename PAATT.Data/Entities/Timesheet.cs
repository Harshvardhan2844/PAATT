using PAATT.Shared.Enums;

namespace PAATT.Data.Entities;

public class Timesheet
{
    public int Id { get; set; }
    public string ConsultantId { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    public DateTime? SubmittedUtc { get; set; }
    public ApplicationUser Consultant { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
    public ICollection<TimesheetReview> Reviews { get; set; } = new List<TimesheetReview>();
}
