using System.ComponentModel.DataAnnotations;

namespace App.Shared.Dtos.Requests;

/// <summary>
/// Edits an entry that's still open — today's entry, or one a manager
/// rejected. Date isn't editable: an entry belongs to the day it was logged.
/// </summary>
public class UpdateEntryRequest
{
    [Range(0.01, 24, ErrorMessage = "Hours must be between 0.01 and 24.")]
    public decimal HoursWorked { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}
