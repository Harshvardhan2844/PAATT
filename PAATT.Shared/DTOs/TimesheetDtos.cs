using System.ComponentModel.DataAnnotations;
using PAATT.Shared.Enums;

namespace PAATT.Shared.DTOs;

public sealed record TimesheetDto(int Id, int ProjectId, string ProjectName, string ConsultantId, string ConsultantName, DateOnly WeekStartDate, TimesheetStatus Status, decimal TotalHours);
public sealed record TimesheetEntryDto(int Id, DateOnly WorkDate, decimal Hours, string Description, TimesheetEntryStatus Status, string? Feedback);
public sealed record TimesheetReviewDto(int Id, int? TimesheetEntryId, string ReviewerName, TimesheetReviewAction Action, string? Feedback, DateTime CreatedUtc);
public sealed record TimesheetDetailsDto(int Id, int ProjectId, string ProjectName, string ConsultantId, string ConsultantName, DateOnly WeekStartDate, TimesheetStatus Status, decimal TotalHours, IReadOnlyList<TimesheetEntryDto> Entries, IReadOnlyList<TimesheetReviewDto> Reviews);

public sealed class CreateTimesheetDto
{
    [Range(1, int.MaxValue)] public int ProjectId { get; set; }
    public DateOnly WeekStartDate { get; set; }
}

public class CreateTimesheetEntryDto
{
    public DateOnly WorkDate { get; set; }
    [Range(typeof(decimal), "0.01", "24")] public decimal Hours { get; set; }
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
}

public sealed class UpdateTimesheetEntryDto : CreateTimesheetEntryDto { }

public class RejectTimesheetDto
{
    [Required, StringLength(2000)] public string Feedback { get; set; } = string.Empty;
}

public sealed class RejectTimesheetEntryDto : RejectTimesheetDto { }
