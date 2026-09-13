namespace PAATT.Data.Entities;

public class ProjectAssignment
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ConsultantId { get; set; } = string.Empty;
    public DateTime AssignedUtc { get; set; } = DateTime.UtcNow;
    public Project Project { get; set; } = null!;
    public ApplicationUser Consultant { get; set; } = null!;
}
