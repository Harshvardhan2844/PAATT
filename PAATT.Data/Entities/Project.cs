namespace PAATT.Data.Entities;

public class Project
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ProjectManagerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BudgetedHours { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public Client Client { get; set; } = null!;
    public ApplicationUser ProjectManager { get; set; } = null!;
    public ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
