using System.ComponentModel.DataAnnotations;

namespace App.Shared.Dtos.Requests;

/// <summary>
/// Admin's "add employee" form. There is deliberately no role picker — every
/// new user gets the permanent Consultant Identity role, and Manager vs.
/// Consultant is decided per project on the Project Assignments screen.
/// Admin can be granted afterwards via SetAdminRequest.
/// </summary>
public class CreateEmployeeRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Initial password. Identity's own password policy is the real authority
    /// here — this attribute is just a first-pass client-side check so the
    /// form fails fast instead of round-tripping.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;
}
