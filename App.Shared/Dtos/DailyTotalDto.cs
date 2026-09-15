namespace App.Shared.Dtos;

/// <summary>
/// Hours this consultant has logged on one date across EVERY project, which
/// is the number the 24-hour cap actually applies to. The weekly timesheet
/// screen uses it to show how much room is left in the day before the service
/// layer would reject the entry.
/// </summary>
public class DailyTotalDto
{
    public DateOnly Date { get; set; }

    /// <summary>Total across all projects, not just the timesheet being viewed.</summary>
    public decimal Hours { get; set; }
}
