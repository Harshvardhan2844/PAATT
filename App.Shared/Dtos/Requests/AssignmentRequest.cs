using System.ComponentModel.DataAnnotations;
using App.Shared.Enums;

namespace App.Shared.Dtos.Requests;

/// <summary>
/// One row of the Admin's Project Assignments screen. BudgetedHours is
/// required when AssignmentType = Consultant and ignored when Manager — the
/// service layer is the authority on that, this is just the wire shape.
/// </summary>
public class AssignmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a project.")]
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Select an employee.")]
    public string EmployeeId { get; set; } = string.Empty;

    public AssignmentType AssignmentType { get; set; }

    [Range(0.01, 10000, ErrorMessage = "Budgeted hours must be greater than 0.")]
    public decimal? BudgetedHours { get; set; }
}
