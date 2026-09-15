using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using App.Shared.Enums;

namespace App.Data.Entities;

/// <summary>
/// No separate ProjectId here — it's inherited from the parent Timesheet,
/// since Timesheet is now project-scoped. Status/ManagerFeedback are
/// per-entry so a PM can reject a single entry without rejecting the whole
/// timesheet; that reopens just this entry for the consultant to edit/remove.
/// </summary>
public class TimesheetEntry
{
    public int Id { get; set; }

    [ForeignKey(nameof(Timesheet))]
    public int TimesheetId { get; set; }
    public Timesheet Timesheet { get; set; } = null!;

    public DateOnly Date { get; set; }

    [Column(TypeName = "decimal(4,2)")]
    public decimal HoursWorked { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public EntryStatus Status { get; set; } = EntryStatus.Pending;

    public string? ManagerFeedback { get; set; }

    /// <summary>
    /// Computed, not stored: true once Date &lt; today, UNLESS a PM has
    /// rejected this entry (Rejected entries stay editable regardless of date).
    /// Implement as a service-layer / query-time computation, e.g.:
    ///   IsLocked = Date &lt; DateOnly.FromDateTime(DateTime.Today) &amp;&amp; Status != EntryStatus.Rejected;
    /// Deliberately not a stored/mapped property so it can't drift out of sync
    /// with "today"; EF Core ignores it via [NotMapped].
    /// </summary>
    [NotMapped]
    public bool IsLocked => Date < DateOnly.FromDateTime(DateTime.Today) && Status != EntryStatus.Rejected;
}
