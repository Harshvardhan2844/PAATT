namespace App.Shared.Dtos;

/// <summary>
/// Backs the manager's per-consultant budget-vs-actual view. Deliberately
/// scoped to ONE consultant on ONE project — never a project-wide total,
/// per the confirmed "no combined total" rule. One row per (Project, Consultant).
/// </summary>
public class BudgetVsActualDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public string ConsultantId { get; set; } = string.Empty;
    public string ConsultantName { get; set; } = string.Empty;

    public decimal? BudgetedHours { get; set; }

    /// <summary>
    /// Everything this consultant has logged on this project that hasn't been
    /// rejected — approved hours plus hours still awaiting review. This is the
    /// number to compare against the budget, because pending time is real time
    /// that's going to land.
    /// </summary>
    public decimal ActualHours { get; set; }

    /// <summary>The approved subset of ActualHours. Phase 5 addition, so the manager can see how much of the burn is already signed off.</summary>
    public decimal ApprovedHours { get; set; }
}
