using System.ComponentModel.DataAnnotations;
using App.Shared.Enums;

namespace App.Shared.Dtos;

public class TimesheetEntryDto
{
    public int Id { get; set; }

    public int TimesheetId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Range(0.01, 24)]
    public decimal HoursWorked { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public EntryStatus Status { get; set; } = EntryStatus.Pending;

    public string? ManagerFeedback { get; set; }

    /// <summary>
    /// Mirrors the entity's computed rule: true once Date &lt; today, unless
    /// Status = Rejected (a PM rejection reopens the entry regardless of date).
    /// Set by the service layer when mapping to this DTO — not user-editable.
    /// </summary>
    public bool IsLocked { get; set; }
}
