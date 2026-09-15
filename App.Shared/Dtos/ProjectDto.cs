using System.ComponentModel.DataAnnotations;

namespace App.Shared.Dtos;

/// <summary>
/// No BudgetedHours here — matches the entity: budget lives only on
/// ProjectAssignment, per consultant. ManagerName is a convenience read
/// field (derived from the project's single Manager-type assignment, if any).
/// </summary>
public class ProjectDto
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public bool IsBillable { get; set; }

    /// <summary>Read-only convenience field; null if no Manager assigned yet.</summary>
    public string? ManagerName { get; set; }

    /// <summary>Read-only convenience field; count of Consultant-type assignments.</summary>
    public int ConsultantCount { get; set; }
}
