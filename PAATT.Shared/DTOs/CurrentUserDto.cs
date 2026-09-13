using System.ComponentModel.DataAnnotations;
using PAATT.Shared.Validation;

namespace PAATT.Shared.DTOs;

public sealed record CurrentUserDto(string Id, string Name, IReadOnlyList<string> Roles);
public sealed class LoginDto
{
    [Required, EmailAddress, ValidEmail] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
