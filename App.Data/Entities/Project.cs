using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Data.Entities;

/// <summary>
/// No BudgetedHours field here — per the confirmed decision, budgeted hours
/// live only on ProjectAssignment (per consultant, per project). The app
/// never surfaces a summed project-level total to a PM.
/// </summary>
public class Project
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [ForeignKey(nameof(Client))]
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public bool IsBillable { get; set; }

    public ICollection<ProjectAssignment> Assignments { get; set; } = new List<ProjectAssignment>();

    // A Timesheet is per (Employee, Project, Week) — see Timesheet entity.
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
