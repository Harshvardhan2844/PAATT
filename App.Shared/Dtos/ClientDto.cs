using System.ComponentModel.DataAnnotations;

namespace App.Shared.Dtos;

public class ClientDto
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Count of projects for this client — for list screens. Not editable.</summary>
    public int ProjectCount { get; set; }
}
