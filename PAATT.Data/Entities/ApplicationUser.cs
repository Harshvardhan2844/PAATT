using Microsoft.AspNetCore.Identity;

namespace PAATT.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
    public ICollection<TimesheetReview> Reviews { get; set; } = new List<TimesheetReview>();
}
