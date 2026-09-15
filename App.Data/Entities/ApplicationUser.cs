using Microsoft.AspNetCore.Identity;

namespace App.Data.Entities;

/// <summary>
/// Extends IdentityUser with a display name. Identity handles Email/UserName.
/// Every user is a "Consultant" Identity role by default — that role is
/// permanent and never removed. "Admin" is a second Identity role an Admin
/// can additionally grant to (or revoke from) any Consultant; it stacks on
/// top of Consultant rather than replacing it, so a promoted Admin can still
/// be assigned to projects like anyone else. Whether a Consultant acts as a
/// Manager or Consultant on a given project is a separate, per-project thing
/// determined via ProjectAssignment.AssignmentType, not an Identity role.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;

    // Navigation: assignments where this user is either the Manager or a Consultant.
    public ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();

    // Navigation: timesheets this user (as Consultant) has created.
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
