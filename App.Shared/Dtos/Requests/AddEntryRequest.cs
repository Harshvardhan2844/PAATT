using System.ComponentModel.DataAnnotations;

namespace App.Shared.Dtos.Requests;

/// <summary>
/// Logs time against a (project, week). The timesheet itself is created on
/// demand by the server if this is the first entry for that week, so the
/// client never has to juggle a timesheet id — and can't invent one belonging
/// to somebody else.
/// </summary>
public class AddEntryRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a project.")]
    public int ProjectId { get; set; }

    public DateOnly WeekStarting { get; set; }

    /// <summary>Must be today. The service layer rejects anything else.</summary>
    public DateOnly Date { get; set; }

    [Range(0.01, 24, ErrorMessage = "Hours must be between 0.01 and 24.")]
    public decimal HoursWorked { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
