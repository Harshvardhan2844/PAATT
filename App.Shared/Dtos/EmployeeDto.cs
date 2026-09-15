namespace App.Shared.Dtos;

/// <summary>
/// Represents an employee in listings (Admin's Managers/Consultants sub-tabs,
/// assignment pickers, etc). ManagedProjectNames / ConsultantProjectNames are
/// derived views built from that employee's ProjectAssignment rows — an
/// employee can appear in both lists (Manager on one project, Consultant on
/// another), never both roles on the same project.
/// </summary>
public class EmployeeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// True if this person has been additionally granted the Admin role.
    /// Every user is a Consultant regardless of this flag — Admin stacks on
    /// top, it doesn't replace the Consultant role.
    /// </summary>
    public bool IsAdmin { get; set; }

    public List<string> ManagedProjectNames { get; set; } = new();
    public List<string> ConsultantProjectNames { get; set; } = new();
}
