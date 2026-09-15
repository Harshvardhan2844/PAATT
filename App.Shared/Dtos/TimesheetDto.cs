using App.Shared.Enums;

namespace App.Shared.Dtos;

/// <summary>
/// One per (Employee, Project, Week) — matches the entity. ProjectName /
/// EmployeeName are read-only convenience fields for list/queue screens
/// (e.g. the PM's Approved/Pending/Rejected buckets).
/// </summary>
public class TimesheetDto
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public DateOnly WeekStartingDate { get; set; }

    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

    /// <summary>Only used for whole-timesheet (bulk) rejection.</summary>
    public string? OverallManagerFeedback { get; set; }

    public List<TimesheetEntryDto> Entries { get; set; } = new();

    /// <summary>Sum of Entries[].HoursWorked — convenience for list screens.</summary>
    public decimal TotalHours { get; set; }
}
