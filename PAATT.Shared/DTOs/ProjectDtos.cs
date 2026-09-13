using System.ComponentModel.DataAnnotations;

namespace PAATT.Shared.DTOs;

public sealed record ProjectDto(int Id, int ClientId, string ClientName, string ProjectManagerId, string ProjectManagerName, string Name, decimal BudgetedHours, bool IsActive, IReadOnlyList<EmployeeDto> Consultants);

public class CreateProjectDto
{
    [Range(1, int.MaxValue)] public int ClientId { get; set; }
    [Required] public string ProjectManagerId { get; set; } = string.Empty;
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    [StringLength(2000)] public string? Description { get; set; }
    [Range(typeof(decimal), "0", "99999999")] public decimal BudgetedHours { get; set; }
    public IReadOnlyList<string> ConsultantIds { get; set; } = [];
}

public sealed class UpdateProjectDto : CreateProjectDto
{
    public bool IsActive { get; set; } = true;
}

public sealed record ProjectAssignmentDto(int Id, int ProjectId, string ConsultantId, string ConsultantName);

public sealed class CreateProjectAssignmentDto
{
    [Range(1, int.MaxValue)] public int ProjectId { get; set; }
    [Required] public string ConsultantId { get; set; } = string.Empty;
}
