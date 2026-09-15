using System.ComponentModel.DataAnnotations;

namespace App.Shared.Dtos.Requests;

/// <summary>
/// Feedback for a rejection, whether it's a whole week or a single entry.
/// The service layer refuses a rejection with no feedback — a consultant
/// being sent work back with no reason is the thing this app most wants to
/// avoid.
/// </summary>
public class RejectionRequest
{
    [Required(ErrorMessage = "Tell them what needs to change.")]
    [MaxLength(1000)]
    public string Feedback { get; set; } = string.Empty;
}
