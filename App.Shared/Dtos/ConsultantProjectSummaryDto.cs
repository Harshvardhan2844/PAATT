namespace App.Shared.Dtos;

/// <summary>
/// One row of the consultant's "My projects" screen: the project they're
/// assigned to, their own budgeted hours on it, and their own logged hours.
/// Deliberately never a project-wide total — a consultant sees their own
/// numbers only, same principle as the PM's per-consultant view.
/// </summary>
public class ConsultantProjectSummaryDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public bool IsBillable { get; set; }

    /// <summary>Who approves this project's timesheets. Null if Admin hasn't assigned a manager yet.</summary>
    public string? ManagerName { get; set; }

    /// <summary>This consultant's cap on this project, from their assignment row.</summary>
    public decimal? BudgetedHours { get; set; }

    /// <summary>Everything this consultant has logged on this project, whatever the entry status.</summary>
    public decimal LoggedHours { get; set; }
}
