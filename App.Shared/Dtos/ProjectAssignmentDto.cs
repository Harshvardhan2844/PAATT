using System.ComponentModel.DataAnnotations;
using App.Shared.Enums;

namespace App.Shared.Dtos;

/// <summary>
/// Used by the Admin's "Project Assignments" screen — one Manager + N
/// Consultants per project, each Consultant with their own BudgetedHours.
/// BudgetedHours should be required when AssignmentType = Consultant and
/// ignored/blank when AssignmentType = Manager (enforce in the form + service
/// layer, not just here).
/// </summary>
public class ProjectAssignmentDto
{
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public AssignmentType AssignmentType { get; set; }

    /// <summary>Per-consultant cap on this project. Required when AssignmentType = Consultant.</summary>
    [Range(0, 10000)]
    public decimal? BudgetedHours { get; set; }
}
