using System.ComponentModel.DataAnnotations;

namespace App.Data.Entities;

public class Client
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
