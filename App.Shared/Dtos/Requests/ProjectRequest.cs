using System.ComponentModel.DataAnnotations;

namespace App.Shared.Dtos.Requests;

/// <summary>
/// No BudgetedHours — budget lives only on ProjectAssignment, per consultant.
/// </summary>
public class ProjectRequest
{
    [Required(ErrorMessage = "Project name is required.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Select a client.")]
    public int ClientId { get; set; }

    public bool IsBillable { get; set; } = true;
}
