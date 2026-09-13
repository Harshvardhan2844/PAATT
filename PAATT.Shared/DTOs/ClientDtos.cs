using System.ComponentModel.DataAnnotations;
using PAATT.Shared.Validation;

namespace PAATT.Shared.DTOs;

public sealed record ClientDto(int Id, string Name, string? ContactName, string? ContactEmail, bool IsActive);

public class CreateClientDto
{
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    [StringLength(150)] public string? ContactName { get; set; }
    [EmailAddress, ValidEmail, StringLength(256)] public string? ContactEmail { get; set; }
}

public sealed class UpdateClientDto : CreateClientDto
{
    public bool IsActive { get; set; } = true;
}
