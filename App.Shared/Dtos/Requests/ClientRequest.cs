using System.ComponentModel.DataAnnotations;

namespace App.Shared.Dtos.Requests;

public class ClientRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;
}
