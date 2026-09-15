using System.ComponentModel.DataAnnotations.Schema;
using App.Shared.Enums;

namespace App.Data.Entities;

/// <summary>
/// Links one Employee to one Project as either Manager or Consultant.
/// Unique on (ProjectId, EmployeeId) — one assignment row per employee per
/// project (an employee can't be both Manager and Consultant on the same
/// project, enforced here by the constraint plus a service-layer check).
/// BudgetedHours is only meaningful when AssignmentType = Consultant.
/// Service layer additionally enforces at most one Manager-type row per Project.
/// </summary>
public class ProjectAssignment
{
    public int Id { get; set; }

    [ForeignKey(nameof(Project))]
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [ForeignKey(nameof(Employee))]
    public string EmployeeId { get; set; } = string.Empty;
    public ApplicationUser Employee { get; set; } = null!;

    public AssignmentType AssignmentType { get; set; }

    /// <summary>
    /// Per-consultant cap on this project. Nullable/unused when AssignmentType = Manager.
    /// </summary>
    public decimal? BudgetedHours { get; set; }
}
