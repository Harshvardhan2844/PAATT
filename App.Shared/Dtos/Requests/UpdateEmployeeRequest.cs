using System.ComponentModel.DataAnnotations;

namespace App.Shared.Dtos.Requests;

/// <summary>
/// Edits a person's display name and email. Email doubles as the Identity
/// UserName, so changing it changes what they log in with. Roles are not
/// touched here — use SetAdminRequest for the Admin role, and the Project
/// Assignments screen for per-project Manager/Consultant.
/// </summary>
public class UpdateEmployeeRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}
