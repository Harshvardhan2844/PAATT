using System.ComponentModel.DataAnnotations;

namespace PAATT.Shared.DTOs;

public sealed record EmployeeDto(string Id, string Name, string Email, bool IsActive, IReadOnlyList<string> Roles);

public sealed class CreateEmployeeDto
{
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    public IReadOnlyList<string>? Roles { get; set; }
}

public sealed class UpdateEmployeeRolesDto
{
    [Required] public IReadOnlyList<string> Roles { get; set; } = [];
}
